// Owned task scratch lifecycle helper. Temporary agent artifacts live under a
// machine-local canonical scratch root — <system-temp>/opencode/tasks/<TASK-ID>/<RUN-ID> —
// and are tracked by a versioned JSON manifest ("manifest.json") whose entry paths are
// root-relative. Nothing outside the manifest is ever deleted: `clean` deletes only
// registered, non-promoted file entries from a sealed, valid root and fails closed on
// malformed, mismatched, escaped, reparse, or unknown material. Durable evidence must be
// explicitly promoted (copy-only, byte-verified) into the current task's docs/ or scripts/.
//
// Windows-only. Mutation is bound to verified handle capabilities, never revalidated
// pathname strings. A private native SafeFs layer opens the root and every entry
// handle-relative (Windows NtCreateFile with FILE_OPEN_REPARSE_POINT), so a reparse
// point in any ancestor, target, or descendant fails closed instead of being followed.
// The manifest records stable root/file identity (volume+file index) and a SHA-256
// digest per entry; clean/promote delete/copy only bytes whose identity and digest match.
// Manifest mutations serialize under an exclusive Windows open (share mode 0). Every
// helper operation fails closed on non-Windows platforms; no compatibility fallback is
// retained.
//
// Usage:
//   dotnet fsi TaskScratch.fsx create <TASK-ID> [--run <RUN-ID>]
//   dotnet fsi TaskScratch.fsx register <ROOT> <PATH> [<PATH>...]
//   dotnet fsi TaskScratch.fsx report <ROOT>
//   dotnet fsi TaskScratch.fsx promote <ROOT> <ENTRY-PATH> --destination <docs|scripts> [--as <NAME>]
//   dotnet fsi TaskScratch.fsx seal <ROOT>
//   dotnet fsi TaskScratch.fsx clean <ROOT>
//
// Exit codes: 0 success (clean reports but may retain material), 1 fail-closed
// runtime/validation error, 2 usage error.

open System
open System.Collections.Generic
open System.IO
open System.Runtime.InteropServices
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Text.RegularExpressions

[<Literal>]
let manifestFileName = "manifest.json"

[<Literal>]
let manifestVersion = 2

let taskIdRegex = Regex(@"^[A-Za-z]+-\d+$")
let runIdRegex = Regex(@"^[A-Za-z0-9._-]+$")

let pathComparison = StringComparison.OrdinalIgnoreCase

// ---------------------------------------------------------------- manifest model

type ScratchEntry =
    { Path: string
      Kind: string
      Promoted: bool
      PromotedTo: string
      FileId: string
      Digest: string }

type ScratchManifest =
    { Version: int
      TaskId: string
      RunId: string
      Root: string
      RootId: string
      Sealed: bool
      Entries: ScratchEntry list }

// ---------------------------------------------------------------- native SafeFs layer

/// Platform for the safe filesystem primitives. Windows provides the identity +
/// no-reparse operations required for mutation; anything else is unsupported and
/// refuses mutation (fail closed).
type Platform =
    | Windows
    | Unsupported

/// An opaque filesystem capability: a verified Windows handle bound to a stable
/// identity (volume+file index). Directory and file capabilities share this shape;
/// the caller tracks which kind it holds.
type FsCap =
    { Platform: Platform
      WinHandle: nativeint
      Identity: string }

/// Outcome of opening a registered entry through the safe layer.
type EntryOpenResult =
    | OpenedFile of FsCap
    | OpenMissing
    | OpenIsDirectory
    | OpenReparseFinal
    | OpenReparseAncestor
    | OpenFailed of string

module private Native =
    // -------- Windows: handle-relative, no-reparse --------

    [<Literal>]
    let GENERIC_READ = 0x80000000u

    [<Literal>]
    let GENERIC_WRITE = 0x40000000u

    [<Literal>]
    let DELETE = 0x00010000u

    [<Literal>]
    let SYNCHRONIZE = 0x00100000u

    [<Literal>]
    let FILE_LIST_DIRECTORY = 0x0001u

    [<Literal>]
    let FILE_TRAVERSE = 0x0020u

    [<Literal>]
    let FILE_READ_ATTRIBUTES = 0x0080u

    [<Literal>]
    let FILE_SHARE_READ = 0x1u

    [<Literal>]
    let FILE_SHARE_WRITE = 0x2u

    [<Literal>]
    let FILE_SHARE_DELETE = 0x4u

    [<Literal>]
    let FILE_OPEN = 0x1u

    [<Literal>]
    let FILE_CREATE = 0x2u

    [<Literal>]
    let FILE_OPEN_IF = 0x3u

    [<Literal>]
    let FILE_FLAG_BACKUP_SEMANTICS = 0x02000000u

    [<Literal>]
    let FILE_OPEN_REPARSE_POINT = 0x00200000u

    [<Literal>]
    let FILE_DIRECTORY_FILE = 0x00000001u

    [<Literal>]
    let FILE_NON_DIRECTORY_FILE = 0x00000040u

    [<Literal>]
    let FILE_SYNCHRONOUS_IO_NONALERT = 0x00000020u

    [<Literal>]
    let FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400u

    [<Literal>]
    let FILE_ATTRIBUTE_DIRECTORY = 0x00000010u

    [<Literal>]
    let OBJ_CASE_INSENSITIVE = 0x00000040u

    [<Literal>]
    let STATUS_OBJECT_NAME_NOT_FOUND = 0xC0000034u

    [<Literal>]
    let STATUS_OBJECT_NAME_COLLISION = 0xC0000035u

    [<Literal>]
    let STATUS_OBJECT_PATH_NOT_FOUND = 0xC000003Au

    [<Literal>]
    let STATUS_ACCESS_DENIED = 0xC0000022u

    [<Literal>]
    let STATUS_SHARING_VIOLATION = 0xC0000043u

    [<Literal>]
    let STATUS_NOT_A_DIRECTORY = 0xC0000103u

    [<Literal>]
    let STATUS_FILE_IS_A_DIRECTORY = 0xC00000BAu

    let shareAll = FILE_SHARE_READ ||| FILE_SHARE_WRITE ||| FILE_SHARE_DELETE
    let invalidHandle = nativeint -1
    let ptrSize = IntPtr.Size
    let dirAccess = FILE_LIST_DIRECTORY ||| FILE_TRAVERSE ||| FILE_READ_ATTRIBUTES ||| SYNCHRONIZE
    let fileAccess = GENERIC_READ ||| DELETE ||| SYNCHRONIZE

    [<DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)>]
    extern bool CloseHandle(nativeint hObject)

    [<DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)>]
    extern nativeint CreateFileW(
        string lpFileName,
        uint32 dwDesiredAccess,
        uint32 dwShareMode,
        nativeint lpSecurityAttributes,
        uint32 dwCreationDisposition,
        uint32 dwFlagsAndAttributes,
        nativeint hTemplateFile)

    [<DllImport("ntdll.dll")>]
    extern uint32 NtCreateFile(
        nativeint& FileHandle,
        uint32 DesiredAccess,
        nativeint ObjectAttributes,
        nativeint IoStatusBlock,
        nativeint AllocationSize,
        uint32 FileAttributes,
        uint32 ShareAccess,
        uint32 CreateDisposition,
        uint32 CreateOptions,
        nativeint EaBuffer,
        uint32 EaLength)

    [<DllImport("kernel32.dll", SetLastError = true)>]
    extern bool GetFileInformationByHandle(nativeint hFile, nativeint lpFileInformation)

    [<DllImport("kernel32.dll", SetLastError = true)>]
    extern bool SetFileInformationByHandle(
        nativeint hFile,
        int FileInformationClass,
        nativeint lpFileInformation,
        uint32 dwBufferSize)

    let winError () = Marshal.GetLastWin32Error()

    let ntStatusText (status: uint32) =
        match status with
        | 0x00000000u -> "STATUS_SUCCESS"
        | 0xC0000034u -> "STATUS_OBJECT_NAME_NOT_FOUND"
        | 0xC0000035u -> "STATUS_OBJECT_NAME_COLLISION"
        | 0xC000003Au -> "STATUS_OBJECT_PATH_NOT_FOUND"
        | 0xC0000022u -> "STATUS_ACCESS_DENIED"
        | 0xC0000043u -> "STATUS_SHARING_VIOLATION"
        | 0xC0000103u -> "STATUS_NOT_A_DIRECTORY"
        | 0xC00000BAu -> "STATUS_FILE_IS_A_DIRECTORY"
        | other -> sprintf "NTSTATUS 0x%08X" other

    // OBJECT_ATTRIBUTES / UNICODE_STRING / IO_STATUS_BLOCK are built as raw buffers so
    // layout matches the native ABI for both 32- and 64-bit processes.
    let buildObjectAttributes (rootHandle: nativeint) (unicodeStringPtr: nativeint) : nativeint =
        let size = ptrSize * 6
        let p = Marshal.AllocHGlobal size
        Marshal.Copy(Array.zeroCreate<byte> size, 0, p, size)
        Marshal.WriteInt32(p, 0, size) // Length = sizeof(OBJECT_ATTRIBUTES)
        Marshal.WriteIntPtr(p, ptrSize, rootHandle) // RootDirectory
        Marshal.WriteIntPtr(p, ptrSize * 2, unicodeStringPtr) // ObjectName
        Marshal.WriteInt32(p, ptrSize * 3, int OBJ_CASE_INSENSITIVE) // Attributes
        p

    /// Opens `name` relative to `rootHandle` through the NT object manager without
    /// traversing the final component's reparse point. `name` must be a single component.
    /// Returns the raw NTSTATUS on failure so callers can classify the outcome.
    let ntOpenRelative (rootHandle: nativeint) (name: string) (desiredAccess: uint32) (shareAccess: uint32) (createDisposition: uint32) (createOptions: uint32) : Result<nativeint, uint32> =
        let mutable fileHandle = 0n
        let stringPtr = Marshal.StringToHGlobalUni name
        let byteLength = name.Length * 2
        let unicodePtr = Marshal.AllocHGlobal(ptrSize * 2)
        Marshal.WriteInt16(unicodePtr, 0, int16 (min byteLength 32767))
        Marshal.WriteInt16(unicodePtr, 2, int16 (min (byteLength + 2) 32767))
        Marshal.WriteIntPtr(unicodePtr, ptrSize, stringPtr)

        let objectAttributesPtr = buildObjectAttributes rootHandle unicodePtr
        let ioStatusPtr = Marshal.AllocHGlobal(ptrSize * 2)

        try
            let status =
                NtCreateFile(&fileHandle, desiredAccess, objectAttributesPtr, ioStatusPtr, 0n, 0u, shareAccess, createDisposition, createOptions, 0n, 0u)

            if status = 0x00000000u then
                Ok fileHandle
            else
                if fileHandle <> 0n then CloseHandle fileHandle |> ignore
                Error status
        finally
            Marshal.FreeHGlobal stringPtr
            Marshal.FreeHGlobal unicodePtr
            Marshal.FreeHGlobal objectAttributesPtr
            Marshal.FreeHGlobal ioStatusPtr

    /// Opens an existing directory as a trusted anchor, resolving (following) any reparse
    /// points in the anchor path itself; only components below the anchor are checked.
    let openDirectoryAnchor (path: string) : Result<nativeint, string> =
        let handle = CreateFileW(path, dirAccess, shareAll, 0n, 0x3u (* OPEN_EXISTING *), FILE_FLAG_BACKUP_SEMANTICS, 0n)

        if handle = invalidHandle then
            Error $"cannot open directory anchor '{path}' (Win32 error %d{winError ()})"
        else
            Ok handle

    let fileAttributes (handle: nativeint) : Result<uint32, string> =
        let buffer = Marshal.AllocHGlobal 64
        try
            if GetFileInformationByHandle(handle, buffer) then
                Ok(uint32 (Marshal.ReadInt32(buffer, 0)))
            else
                Error $"cannot read file information (Win32 error %d{winError ()})"
        finally
            Marshal.FreeHGlobal buffer

    let fileIdentity (handle: nativeint) : Result<string, string> =
        let buffer = Marshal.AllocHGlobal 64
        try
            if GetFileInformationByHandle(handle, buffer) then
                let volume = uint64 (uint32 (Marshal.ReadInt32(buffer, 28)))
                let indexHigh = uint64 (uint32 (Marshal.ReadInt32(buffer, 44)))
                let indexLow = uint64 (uint32 (Marshal.ReadInt32(buffer, 48)))
                let index = (indexHigh <<< 32) ||| indexLow
                Ok(sprintf "%x:%x" volume index)
            else
                Error $"cannot read file identity (Win32 error %d{winError ()})"
        finally
            Marshal.FreeHGlobal buffer

    let setDeleteDisposition (handle: nativeint) : Result<unit, string> =
        let buffer = Marshal.AllocHGlobal 1
        try
            Marshal.WriteByte(buffer, 0, 1uy)
            if SetFileInformationByHandle(handle, 4 (* FileDispositionInfo *), buffer, 1u) then
                Ok()
            else
                Error $"cannot set delete disposition (Win32 error %d{winError ()})"
        finally
            Marshal.FreeHGlobal buffer

// ---------------------------------------------------------------- SafeFs operations

module private SafeFs =
    let platform =
        if OperatingSystem.IsWindows() then Windows
        else Unsupported

    let isSupported = platform = Windows

    let unsupported () = Error "safe filesystem primitives are unavailable on this platform; mutation is refused"

    let closeCap (cap: FsCap) =
        match cap.Platform with
        | Windows when cap.WinHandle <> 0n -> Native.CloseHandle cap.WinHandle |> ignore
        | _ -> ()

    let readAllBytes (cap: FsCap) : Result<byte[], string> =
        try
            let handle =
                match cap.Platform with
                | Windows -> new Microsoft.Win32.SafeHandles.SafeFileHandle(cap.WinHandle, false)
                | Unsupported -> failwith "unsupported platform"

            use safeHandle = handle
            use stream = new FileStream(safeHandle, FileAccess.Read)
            use memory = new MemoryStream()
            stream.CopyTo memory
            Ok(memory.ToArray())
        with ex ->
            Error $"cannot read file bytes: {ex.Message}"

    let digestOf (bytes: byte[]) = Convert.ToHexString(SHA256.HashData bytes).ToLowerInvariant()

    let validateSegment (segment: string) =
        if String.IsNullOrEmpty segment || segment = "." || segment = ".."
           || segment.IndexOfAny([| '/'; '\\' |]) >= 0 then
            Error $"invalid path component '{segment}'"
        else
            Ok segment

    let splitRelative (relativePath: string) : Result<string list, string> =
        let segments = relativePath.Split([| '/'; '\\' |], StringSplitOptions.RemoveEmptyEntries)
        if segments.Length = 0 then
            Error "path is empty"
        else
            let rec collect acc remaining =
                match remaining with
                | [] -> Ok(List.rev acc)
                | segment :: rest ->
                    match validateSegment segment with
                    | Ok name -> collect (name :: acc) rest
                    | Error message -> Error message

            collect [] (Array.toList segments)

    /// Opens the final entry component relative to `root`, classifying the outcome.
    /// Intermediate components are opened handle-relative with reparse traversal disabled
    /// so a junction/symlink ancestor fails closed instead of being followed.
    let openEntryWindows (root: FsCap) (relativePath: string) : EntryOpenResult =
        match splitRelative relativePath with
        | Error message -> OpenFailed message
        | Ok segments ->
            let rec walk (parent: nativeint) (segments: string list) : EntryOpenResult =
                match segments with
                | [] -> OpenFailed "unreachable"
                | [ last ] ->
                    match Native.ntOpenRelative parent last Native.fileAccess Native.shareAll Native.FILE_OPEN Native.FILE_OPEN_REPARSE_POINT with
                    | Error status when status = Native.STATUS_OBJECT_NAME_NOT_FOUND || status = Native.STATUS_OBJECT_PATH_NOT_FOUND ->
                        OpenMissing
                    | Error status ->
                        OpenFailed(sprintf "cannot open entry '%s' (%s)" last (Native.ntStatusText status))
                    | Ok handle ->
                        match Native.fileAttributes handle with
                        | Error message ->
                            Native.CloseHandle handle |> ignore
                            OpenFailed message
                        | Ok attributes ->
                            if attributes &&& Native.FILE_ATTRIBUTE_REPARSE_POINT <> 0u then
                                Native.CloseHandle handle |> ignore
                                OpenReparseFinal
                            elif attributes &&& Native.FILE_ATTRIBUTE_DIRECTORY <> 0u then
                                Native.CloseHandle handle |> ignore
                                OpenIsDirectory
                            else
                                match Native.fileIdentity handle with
                                | Error message ->
                                    Native.CloseHandle handle |> ignore
                                    OpenFailed message
                                | Ok identity -> OpenedFile { Platform = Windows; WinHandle = handle; Identity = identity }
                | next :: rest ->
                    match Native.ntOpenRelative parent next Native.dirAccess Native.shareAll Native.FILE_OPEN (Native.FILE_DIRECTORY_FILE ||| Native.FILE_OPEN_REPARSE_POINT) with
                    | Error status when status = Native.STATUS_OBJECT_NAME_NOT_FOUND || status = Native.STATUS_OBJECT_PATH_NOT_FOUND ->
                        OpenMissing
                    | Error _ ->
                        OpenReparseAncestor
                    | Ok handle ->
                        match Native.fileAttributes handle with
                        | Ok attributes when attributes &&& Native.FILE_ATTRIBUTE_REPARSE_POINT <> 0u ->
                            Native.CloseHandle handle |> ignore
                            OpenReparseAncestor
                        | Ok _ ->
                            let deeper = walk handle rest
                            Native.CloseHandle handle |> ignore
                            deeper
                        | Error message ->
                            Native.CloseHandle handle |> ignore
                            OpenFailed message

            walk root.WinHandle segments

    let openEntry (root: FsCap) (relativePath: string) : EntryOpenResult =
        match platform with
        | Windows -> openEntryWindows root relativePath
        | Unsupported -> OpenFailed "safe filesystem primitives are unavailable on this platform"

    /// Walks directory components below an anchor handle, optionally creating them.
    /// Each component is opened handle-relative with reparse traversal disabled, and the
    /// intermediate handles are closed as the walk proceeds.
    let openDirectoryChain (anchor: FsCap) (components: string list) (create: bool) : Result<FsCap, string> =
        let disposition = if create then Native.FILE_OPEN_IF else Native.FILE_OPEN

        let rec walkWindows (parent: nativeint) (components: string list) : Result<nativeint, string> =
            match components with
            | [] -> Ok parent
            | next :: rest ->
                match Native.ntOpenRelative parent next Native.dirAccess Native.shareAll disposition (Native.FILE_DIRECTORY_FILE ||| Native.FILE_OPEN_REPARSE_POINT) with
                | Error status ->
                    Error $"cannot open directory component '{next}' (%s{Native.ntStatusText status})"
                | Ok handle ->
                    match Native.fileAttributes handle with
                    | Error message ->
                        Native.CloseHandle handle |> ignore
                        Error message
                    | Ok attributes when attributes &&& Native.FILE_ATTRIBUTE_REPARSE_POINT <> 0u ->
                        Native.CloseHandle handle |> ignore
                        Error $"directory component '{next}' is a reparse point"
                    | Ok _ ->
                        let deeper = walkWindows handle rest
                        if not rest.IsEmpty then Native.CloseHandle handle |> ignore
                        deeper

        match anchor.Platform with
        | Windows ->
            match walkWindows anchor.WinHandle components with
            | Ok handle ->
                Ok { anchor with WinHandle = handle; Identity = (Native.fileIdentity handle |> Result.defaultValue "") }
            | Error message -> Error message
        | Unsupported -> unsupported ()

    /// Splits an absolute directory path into the trusted filesystem root (Windows
    /// drive/UNC share) and the path components beneath it.
    let splitTrustedPath (path: string) : Result<string * string list, string> =
        try
            match platform with
            | Windows ->
                let full = Path.GetFullPath(path)
                let root = Path.GetPathRoot(full)
                if String.IsNullOrEmpty root then
                    Error $"cannot determine filesystem root for '{path}'"
                else
                    let components =
                        full.Substring(root.Length).Split([| '\\'; '/' |], StringSplitOptions.RemoveEmptyEntries)
                        |> Array.toList

                    Ok(root, components)
            | Unsupported -> unsupported ()
        with ex ->
            Error $"cannot resolve directory path '{path}': {ex.Message}"

    /// Opens a directory capability for an absolute path by walking from the trusted
    /// filesystem root component-wise, rejecting a reparse point in every component.
    /// Opening the path as a single pathname (CreateFileW) would resolve reparse
    /// points; here each component is opened handle-relative with traversal disabled.
    let openDirectoryFromTrustedRoot (path: string) : Result<FsCap, string> =
        if not isSupported then unsupported ()
        else
            match splitTrustedPath path with
            | Error message -> Error message
            | Ok (root, components) ->
                let openRoot () : Result<FsCap, string> =
                    match platform with
                    | Windows ->
                        match Native.openDirectoryAnchor root with
                        | Error message -> Error message
                        | Ok handle -> Ok { Platform = Windows; WinHandle = handle; Identity = "" }
                    | Unsupported -> unsupported ()

                match openRoot () with
                | Error message -> Error message
                | Ok rootCap ->
                    match openDirectoryChain rootCap components false with
                    | Error message ->
                        closeCap rootCap
                        Error message
                    | Ok cap ->
                        // The walk returns the final component's handle; with an empty
                        // component list it aliases the root handle, so the anchor is
                        // closed only when the returned cap does not own it.
                        if components.IsEmpty then Ok cap
                        else
                            closeCap rootCap
                            Ok cap

    /// Opens the OS-temp anchor directory (trusted; reparse in the anchor path itself is
    /// resolved) and returns a FsCap for it.
    let openTempAnchor () : Result<FsCap, string> =
        if not isSupported then unsupported ()
        else
            match platform with
            | Windows ->
                match Native.openDirectoryAnchor (Path.GetTempPath()) with
                | Ok handle -> Ok { Platform = Windows; WinHandle = handle; Identity = "" }
                | Error message -> Error message
            | Unsupported -> unsupported ()

    /// Opens the current task scratch root, verifying physical containment below the
    /// canonical opencode/tasks base handle-relative (no reparse) and returning its
    /// bound identity. This replaces the former string-layout comparison.
    let openScratchRoot (rootPath: string) : Result<FsCap, string> =
        match openTempAnchor () with
        | Error message -> Error message
        | Ok anchor ->
            let runSegment = Path.GetFileName(rootPath)
            let taskSegment = Path.GetFileName(Path.GetDirectoryName(rootPath))
            match openDirectoryChain anchor [ "opencode"; "tasks"; taskSegment; runSegment ] false with
            | Ok cap ->
                closeCap anchor
                Ok cap
            | Error message ->
                closeCap anchor
                Error message

    /// Creates the run directory (and opencode/tasks/<TASK-ID> parents) handle-relative,
    /// failing closed if the run directory already exists.
    let createScratchRoot (taskId: string) (runId: string) : Result<FsCap * string, string> =
        if not isSupported then unsupported ()
        else
            let rootPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "opencode", "tasks", taskId, runId))
            match openTempAnchor () with
            | Error message -> Error message
            | Ok anchor ->
                match openDirectoryChain anchor [ "opencode"; "tasks"; taskId ] true with
                | Error message ->
                    closeCap anchor
                    Error message
                | Ok parent ->
                    closeCap anchor
                    let createRun () =
                        match platform with
                        | Windows ->
                            match Native.ntOpenRelative parent.WinHandle runId Native.dirAccess Native.shareAll Native.FILE_CREATE (Native.FILE_DIRECTORY_FILE ||| Native.FILE_OPEN_REPARSE_POINT) with
                            | Error status when status = Native.STATUS_OBJECT_NAME_COLLISION ->
                                Error $"scratch root already exists: {rootPath}"
                            | Error status ->
                                Error $"cannot create run directory '%s{runId}' (%s{Native.ntStatusText status})"
                            | Ok handle ->
                                match Native.fileAttributes handle with
                                | Error message ->
                                    Native.CloseHandle handle |> ignore
                                    Error message
                                | Ok attributes when attributes &&& Native.FILE_ATTRIBUTE_REPARSE_POINT <> 0u ->
                                    Native.CloseHandle handle |> ignore
                                    Error $"run directory '%s{runId}' is a reparse point"
                                | Ok _ ->
                                    Ok { parent with WinHandle = handle; Identity = (Native.fileIdentity handle |> Result.defaultValue "") }
                        | Unsupported -> unsupported ()

                    match createRun () with
                    | Ok cap -> Ok(cap, rootPath)
                    | Error message -> closeCap parent; Error message

    /// Opens the durable destination directory `<workdir>/.tasks/<TASK-ID>/<namespace>`,
    /// creating the namespace directory if needed and rejecting a reparse point in every
    /// component. `workdir` (the current working directory) is untrusted, so its
    /// capability is re-derived from the trusted filesystem root by component-wise
    /// no-follow traversal, then `.tasks`/taskId/namespace are walked handle-relative
    /// the same way before any destination file is created.
    let openDestinationDir (workdir: string) (taskId: string) (namespaceName: string) : Result<FsCap, string> =
        if not isSupported then unsupported ()
        else
            match openDirectoryFromTrustedRoot workdir with
            | Error message -> Error message
            | Ok workdirCap ->
                match openDirectoryChain workdirCap [ ".tasks"; taskId ] false with
                | Error message ->
                    closeCap workdirCap
                    Error message
                | Ok taskCap ->
                    closeCap workdirCap
                    match openDirectoryChain taskCap [ namespaceName ] true with
                    | Error message ->
                        closeCap taskCap
                        Error message
                    | Ok nsCap ->
                        closeCap taskCap
                        Ok nsCap

    /// Creates (CREATE_NEW) a destination file relative to `dir`, writes `bytes`, then
    /// re-reads and byte-verifies the result. A planted reparse point at the name fails
    /// the create, so copy never follows a swapped destination.
    let writeDestinationFile (dir: FsCap) (name: string) (bytes: byte[]) : Result<unit, string> =
        if not isSupported then unsupported ()
        else
            match platform with
            | Windows ->
                let access = Native.GENERIC_READ ||| Native.GENERIC_WRITE ||| Native.DELETE ||| Native.SYNCHRONIZE
                match Native.ntOpenRelative dir.WinHandle name access Native.shareAll Native.FILE_CREATE (Native.FILE_NON_DIRECTORY_FILE ||| Native.FILE_OPEN_REPARSE_POINT) with
                | Error status when status = Native.STATUS_OBJECT_NAME_COLLISION ->
                    Error $"destination already exists: {name}"
                | Error status -> Error $"cannot create destination file '{name}' (%s{Native.ntStatusText status})"
                | Ok handle ->
                    try
                        try
                            use safeHandle = new Microsoft.Win32.SafeHandles.SafeFileHandle(handle, false)
                            use stream = new FileStream(safeHandle, FileAccess.ReadWrite)
                            stream.Write(bytes, 0, bytes.Length)
                            stream.Flush()
                            stream.Position <- 0L
                            use memory = new MemoryStream()
                            stream.CopyTo memory
                            if memory.ToArray() <> bytes then Error "destination byte verification failed"
                            else Ok()
                        with ex ->
                            Error $"destination write failed: {ex.Message}"
                    finally
                        Native.CloseHandle handle |> ignore
            | Unsupported -> unsupported ()

    /// Deletes the file bound to `cap` by setting the delete disposition on the
    /// already-verified handle, so a rename swap can never redirect the delete.
    let deleteEntry (cap: FsCap) : Result<unit, string> =
        match platform with
        | Windows -> Native.setDeleteDisposition cap.WinHandle
        | Unsupported -> unsupported ()

// ---------------------------------------------------------------- JSON helpers

let tryGetString (name: string) (element: JsonElement) =
    let mutable value = Unchecked.defaultof<JsonElement>
    if element.TryGetProperty(name, &value) && value.ValueKind = JsonValueKind.String then
        Some(value.GetString())
    else
        None

let tryGetBool (name: string) (element: JsonElement) =
    let mutable value = Unchecked.defaultof<JsonElement>
    if element.TryGetProperty(name, &value)
       && (value.ValueKind = JsonValueKind.True || value.ValueKind = JsonValueKind.False) then
        Some(value.GetBoolean())
    else
        None

let tryGetInt (name: string) (element: JsonElement) =
    let mutable value = Unchecked.defaultof<JsonElement>
    if element.TryGetProperty(name, &value) && value.ValueKind = JsonValueKind.Number then
        match value.TryGetInt32() with
        | true, number -> Some number
        | false, _ -> None
    else
        None

let tryGetArray (name: string) (element: JsonElement) =
    let mutable value = Unchecked.defaultof<JsonElement>
    if element.TryGetProperty(name, &value) && value.ValueKind = JsonValueKind.Array then
        Some(value.EnumerateArray() |> Seq.toArray)
    else
        None

let tryParseEntry (item: JsonElement) =
    if item.ValueKind <> JsonValueKind.Object then
        Error "manifest entry must be a JSON object"
    else
        match tryGetString "path" item, tryGetString "kind" item, tryGetBool "promoted" item with
        | Some path, Some kind, Some promoted ->
            let promotedTo = tryGetString "promotedTo" item |> Option.defaultValue ""
            if kind <> "file" then
                Error $"manifest entry '{path}' has unsupported kind '{kind}'"
            else
                match tryGetString "fileId" item, tryGetString "digest" item with
                | Some fileId, Some digest ->
                    Ok
                        { Path = path
                          Kind = kind
                          Promoted = promoted
                          PromotedTo = promotedTo
                          FileId = fileId
                          Digest = digest }
                | _ -> Error $"manifest entry '{path}' is missing fileId or digest"
        | _ -> Error "manifest entry is missing path, kind, or promoted"

let tryParseManifest (json: string) =
    try
        use document = JsonDocument.Parse(json)
        let element = document.RootElement
        if element.ValueKind <> JsonValueKind.Object then
            Error "manifest root must be a JSON object"
        else
            match tryGetInt "version" element with
            | None -> Error "manifest is missing numeric 'version'"
            | Some version when version <> manifestVersion ->
                Error $"unsupported manifest version {version} (expected {manifestVersion})"
            | Some _ ->
                match tryGetString "taskId" element,
                      tryGetString "runId" element,
                      tryGetString "root" element,
                      tryGetString "rootId" element,
                      tryGetBool "sealed" element with
                | Some taskId, Some runId, Some manifestRoot, Some rootId, Some sealedFlag ->
                    match tryGetArray "entries" element with
                    | None -> Error "manifest is missing 'entries' array"
                    | Some items ->
                        items
                        |> Array.fold
                            (fun acc item ->
                                match acc with
                                | Error _ -> acc
                                | Ok entries ->
                                    match tryParseEntry item with
                                    | Ok entry -> Ok(entry :: entries)
                                    | Error message -> Error message)
                            (Ok [])
                        |> Result.map (fun entries ->
                            { Version = manifestVersion
                              TaskId = taskId
                              RunId = runId
                              Root = manifestRoot
                              RootId = rootId
                              Sealed = sealedFlag
                              Entries = List.rev entries })
                | _ -> Error "manifest is missing taskId, runId, root, rootId, or sealed"
    with ex ->
        Error $"manifest JSON parse failed: {ex.Message}"

let quote (value: string) = JsonSerializer.Serialize(value)

let serializeManifest (manifest: ScratchManifest) =
    let sealedText = if manifest.Sealed then "true" else "false"
    let builder = StringBuilder()
    builder.Append("{") |> ignore
    builder.Append($"\"version\": {manifest.Version},") |> ignore
    builder.Append($"\"taskId\": {quote manifest.TaskId},") |> ignore
    builder.Append($"\"runId\": {quote manifest.RunId},") |> ignore
    builder.Append($"\"root\": {quote manifest.Root},") |> ignore
    builder.Append($"\"rootId\": {quote manifest.RootId},") |> ignore
    builder.Append($"\"sealed\": {sealedText},") |> ignore
    builder.Append("\"entries\": [") |> ignore

    manifest.Entries
    |> List.iteri (fun index entry ->
        let promotedText = if entry.Promoted then "true" else "false"
        if index > 0 then builder.Append(",") |> ignore
        builder.Append("{") |> ignore
        builder.Append($"\"path\": {quote entry.Path},") |> ignore
        builder.Append($"\"kind\": {quote entry.Kind},") |> ignore
        builder.Append($"\"promoted\": {promotedText},") |> ignore
        builder.Append($"\"promotedTo\": {quote entry.PromotedTo},") |> ignore
        builder.Append($"\"fileId\": {quote entry.FileId},") |> ignore
        builder.Append($"\"digest\": {quote entry.Digest}") |> ignore
        builder.Append("}") |> ignore)

    builder.Append("]}") |> ignore
    builder.ToString()

// ---------------------------------------------------------------- path safety (syntax)

let isWithinRoot (root: string) (candidate: string) =
    let rootFull =
        Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
    let candidateFull = Path.GetFullPath(candidate)
    candidateFull.StartsWith(rootFull + string Path.DirectorySeparatorChar, pathComparison)
    || candidateFull.Equals(rootFull, pathComparison)

/// Reject absolute, escaping, empty-segment, and dot-segment relative paths.
let tryValidateRelativePath (path: string) =
    let normalized = path.Replace("\\", "/")
    if String.IsNullOrWhiteSpace normalized then
        Error "path is empty"
    elif Path.IsPathRooted normalized || normalized.StartsWith("/") || Regex.IsMatch(normalized, @"^[A-Za-z]:") then
        Error $"path '{path}' is absolute"
    elif normalized = "." then
        Error $"path '{path}' is not a valid relative path"
    else
        let segments = normalized.Split('/')
        if segments |> Array.exists (fun segment -> segment = "..") then
            Error $"path '{path}' escapes the scratch root"
        elif segments |> Array.exists (fun segment -> segment = "." || segment = "") then
            Error $"path '{path}' is malformed"
        else
            Ok normalized

let tryResolveEntryPath (root: string) (relativePath: string) =
    match tryValidateRelativePath relativePath with
    | Error message -> Error message
    | Ok normalized ->
        let full = Path.GetFullPath(Path.Combine(root, normalized))
        if not (isWithinRoot root full) then
            Error $"entry '{relativePath}' escapes the scratch root"
        else
            Ok full

let tryResolveInputPath (root: string) (input: string) =
    let full =
        if Path.IsPathRooted input then Path.GetFullPath input
        else Path.GetFullPath(Path.Combine(root, input))
    if not (isWithinRoot root full) then Error $"path escapes the scratch root: {input}"
    else Ok full

let isSafeFileName (name: string) =
    not (String.IsNullOrWhiteSpace name)
    && name <> "."
    && name <> ".."
    && not (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)

/// Remove known `--option <value>` pairs so the remaining non-option tokens are
/// the positional arguments (option values must not leak into positionals).
let stripValueOptions (valueOptions: string list) (args: string list) =
    let rec loop acc = function
        | [] -> List.rev acc
        | argument :: rest when List.contains argument valueOptions ->
            match rest with
            | _ :: tail -> loop acc tail
            | [] -> List.rev acc
        | argument :: rest -> loop (argument :: acc) rest
    loop [] args

// ---------------------------------------------------------------- root + manifest loading

let tryTaskId (value: string) =
    let candidate = value.Trim()
    if taskIdRegex.IsMatch candidate then Ok candidate
    else Error "task ID must match <LETTERS>-<DIGITS> (for example INFRA-009)"

/// Accepts either a scratch root directory or its manifest file, and verifies the
/// root and manifest exist (path-level only; SafeFs verifies identity and reparse).
let tryResolveRootPath (argument: string) =
    try
        let full = Path.GetFullPath(argument)
        let root =
            if File.Exists full && Path.GetFileName(full).Equals(manifestFileName, pathComparison) then
                Path.GetDirectoryName(full)
            else
                full
        let rootFull = Path.GetFullPath(root)
        let manifestPath = Path.Combine(rootFull, manifestFileName)

        if not (Directory.Exists rootFull) then
            Error $"scratch root does not exist: {rootFull}"
        elif not (File.Exists manifestPath) then
            Error $"manifest does not exist: {manifestPath}"
        else
            Ok rootFull
    with ex ->
        Error $"invalid scratch root: {ex.Message}"

/// True when any entry path appears more than once (case-insensitive), which
/// would make automatic deletion ambiguous and must fail closed.
let hasDuplicateEntryPaths (entries: ScratchEntry list) =
    let seen = HashSet<string>(StringComparer.OrdinalIgnoreCase)
    entries |> List.exists (fun entry -> not (seen.Add entry.Path))

/// Validate a parsed manifest against the requested root: identity, entry path syntax,
/// and duplicate-entry rejection. Physical containment and reparse checks are performed
/// by the SafeFs layer; this function covers the manifest contract.
let validateManifest (root: string) (manifest: ScratchManifest) =
    let rootFull = Path.GetFullPath(root)
    let runSegment = Path.GetFileName(rootFull)
    let taskDirectory = Path.GetDirectoryName(rootFull)
    let taskSegment = Path.GetFileName(taskDirectory)

    if not (String.Equals(manifest.Root, rootFull, pathComparison)) then
        Error "manifest 'root' does not match the requested scratch root"
    elif not (String.Equals(manifest.RunId, runSegment, pathComparison)) then
        Error $"manifest 'runId' does not match the scratch directory '{runSegment}'"
    elif not (String.Equals(manifest.TaskId, taskSegment, pathComparison)) then
        Error $"manifest 'taskId' does not match the task directory '{taskSegment}'"
    elif not (taskIdRegex.IsMatch manifest.TaskId) then
        Error $"manifest 'taskId' is not a canonical ID: {manifest.TaskId}"
    else
        manifest.Entries
        |> List.fold
            (fun acc entry ->
                match acc with
                | Error _ -> acc
                | Ok entries ->
                    match tryValidateRelativePath entry.Path with
                    | Ok normalized -> Ok({ entry with Path = normalized } :: entries)
                    | Error message -> Error message)
            (Ok [])
        |> Result.map (fun entries -> { manifest with Entries = List.rev entries })
        |> Result.bind (fun validated ->
            if hasDuplicateEntryPaths validated.Entries then
                Error "manifest contains duplicate entry paths"
            else
                Ok validated)

/// Opens the manifest file through the verified root handle and returns its text.
let readManifestText (root: FsCap) : Result<string, string> =
    match SafeFs.openEntry root manifestFileName with
    | OpenedFile cap ->
        let result =
            SafeFs.readAllBytes cap
            |> Result.map (fun bytes -> Encoding.UTF8.GetString bytes)
        SafeFs.closeCap cap
        result
    | OpenMissing -> Error $"manifest does not exist: {manifestFileName}"
    | OpenReparseFinal | OpenReparseAncestor -> Error $"manifest must not be a reparse point: {manifestFileName}"
    | OpenIsDirectory -> Error $"manifest is not a file: {manifestFileName}"
    | OpenFailed message -> Error message

/// Loads the manifest bound to the verified root: SafeFs opens the root handle-relative
/// below the OS-temp anchor (rejecting reparse ancestors), the manifest is read through
/// that handle, and the manifest's rootId must match the root's stable identity.
let loadManifestBound (rootPath: string) : Result<FsCap * ScratchManifest, string> =
    match SafeFs.openScratchRoot rootPath with
    | Error message -> Error message
    | Ok rootCap ->
        match readManifestText rootCap with
        | Error message ->
            SafeFs.closeCap rootCap
            Error message
        | Ok json ->
            match tryParseManifest json with
            | Error message ->
                SafeFs.closeCap rootCap
                Error message
            | Ok manifest ->
                match validateManifest rootPath manifest with
                | Error message ->
                    SafeFs.closeCap rootCap
                    Error message
                | Ok manifest ->
                    if not (String.Equals(manifest.RootId, rootCap.Identity, StringComparison.OrdinalIgnoreCase)) then
                        SafeFs.closeCap rootCap
                        Error "manifest 'rootId' does not match the verified scratch root identity"
                    else
                        Ok(rootCap, manifest)

/// Run a manifest read-modify-write while holding an exclusive lock on the manifest
/// file (opened through the verified root handle with share mode 0), so concurrent
/// register/promote/seal mutations serialize and cannot lose one another's updates.
let mutateManifest (root: FsCap) (mutate: ScratchManifest -> Result<ScratchManifest * 'a, string>) : Result<'a, string> =
    if root.Platform = Unsupported then
        Error "safe filesystem primitives are unavailable on this platform; mutation is refused"
    else
        let opened =
            match root.Platform with
            | Windows ->
                Native.ntOpenRelative root.WinHandle manifestFileName (Native.GENERIC_READ ||| Native.GENERIC_WRITE) 0u Native.FILE_OPEN (Native.FILE_NON_DIRECTORY_FILE ||| Native.FILE_OPEN_REPARSE_POINT)
                |> Result.mapError Native.ntStatusText
            | Unsupported -> Error "unsupported"

        let closeHandle (handle: nativeint) =
            match root.Platform with
            | Windows -> Native.CloseHandle handle |> ignore
            | Unsupported -> ()

        match opened with
        | Error message -> Error message
        | Ok handle ->
            // Windows opens the manifest with share mode 0 (exclusive), so concurrent
            // mutations already serialize on the open.
            try
                try
                    let readWrite (safeHandle: Microsoft.Win32.SafeHandles.SafeFileHandle) =
                        use stream = new FileStream(safeHandle, FileAccess.ReadWrite)
                        let json =
                            use reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true)
                            reader.ReadToEnd()

                        match tryParseManifest json with
                        | Error message -> Error message
                        | Ok manifest ->
                            match mutate manifest with
                            | Error message -> Error message
                            | Ok (updated, result) ->
                                stream.SetLength 0L
                                stream.Position <- 0L
                                use writer = new StreamWriter(stream, UTF8Encoding(false), 1024, true)
                                writer.Write(serializeManifest updated)
                                writer.Flush()
                                Ok result

                    match root.Platform with
                    | Windows ->
                        use safeHandle = new Microsoft.Win32.SafeHandles.SafeFileHandle(handle, false)
                        readWrite safeHandle
                    | Unsupported -> Error "unsupported"
                with ex ->
                    Error $"manifest mutation failed: {ex.Message}"
            finally
                closeHandle handle

/// Writes the initial manifest into a freshly created (empty) scratch root. The manifest
/// is created handle-relative (CREATE_NEW) so a planted reparse at the name is never
/// followed; it must not already exist.
let writeInitialManifest (root: FsCap) (manifest: ScratchManifest) : Result<unit, string> =
    let text = serializeManifest manifest
    let bytes = Encoding.UTF8.GetBytes text

    match root.Platform with
    | Windows ->
        match Native.ntOpenRelative root.WinHandle manifestFileName (Native.GENERIC_READ ||| Native.GENERIC_WRITE) Native.shareAll Native.FILE_CREATE (Native.FILE_NON_DIRECTORY_FILE ||| Native.FILE_OPEN_REPARSE_POINT) with
        | Error status -> Error $"cannot create manifest (%s{Native.ntStatusText status})"
        | Ok handle ->
            try
                try
                    use safeHandle = new Microsoft.Win32.SafeHandles.SafeFileHandle(handle, false)
                    use stream = new FileStream(safeHandle, FileAccess.Write)
                    stream.Write(bytes, 0, bytes.Length)
                    stream.Flush()
                    Ok()
                with ex ->
                    Error $"manifest write failed: {ex.Message}"
            finally
                Native.CloseHandle handle |> ignore
    | Unsupported -> Error "safe filesystem primitives are unavailable on this platform; mutation is refused"

/// Enumerate filesystem entries under the root as (root-relative path, kind), where
/// kind is "file", "dir", or "reparse". The manifest itself is excluded. Read-only:
/// used to report retained material, never to select deletion targets.
let scanRoot (root: string) =
    let manifestFull = Path.GetFullPath(Path.Combine(root, manifestFileName))
    let results = ResizeArray<string * string>()

    let rec walk (dir: DirectoryInfo) =
        for info in dir.EnumerateFileSystemInfos() do
            let relative = Path.GetRelativePath(root, info.FullName).Replace("\\", "/")
            let attributes = info.Attributes

            if String.Equals(info.FullName, manifestFull, pathComparison) then
                ()
            elif attributes.HasFlag FileAttributes.ReparsePoint then
                results.Add(relative, "reparse")
            elif attributes.HasFlag FileAttributes.Directory then
                results.Add(relative, "dir")
                walk (DirectoryInfo info.FullName)
            else
                results.Add(relative, "file")

    walk (DirectoryInfo root)
    results |> Seq.toList

/// Classify scanned material that must be retained: reparse points and
/// unregistered files or directories. The manifest file is already excluded.
let classifyRetainedMaterial (registered: Set<string>) (scan: (string * string) list) =
    [ for relative, kind in scan do
        match kind with
        | "reparse" -> yield relative, "reparse point"
        | "file" when not (registered.Contains relative) -> yield relative, "unregistered"
        | "dir" when not (registered.Contains relative) -> yield relative, "unregistered directory"
        | _ -> () ]

// ---------------------------------------------------------------- commands

let usage () =
    printfn "TaskScratch.fsx — owned task scratch lifecycle"
    printfn "usage:"
    printfn "  dotnet fsi TaskScratch.fsx create <TASK-ID> [--run <RUN-ID>]"
    printfn "  dotnet fsi TaskScratch.fsx register <ROOT> <PATH> [<PATH>...]"
    printfn "  dotnet fsi TaskScratch.fsx report <ROOT>"
    printfn "  dotnet fsi TaskScratch.fsx promote <ROOT> <ENTRY-PATH> --destination <docs|scripts> [--as <NAME>]"
    printfn "  dotnet fsi TaskScratch.fsx seal <ROOT>"
    printfn "  dotnet fsi TaskScratch.fsx clean <ROOT>"

let failUsage (message: string) =
    eprintfn "%s" message
    usage ()
    exit 2

let failClosed (message: string) =
    eprintfn "%s" message
    exit 1

let cmdCreate (rest: string list) =
    let positional = stripValueOptions [ "--run" ] rest
    let runId = rest |> List.tryFindIndex ((=) "--run") |> Option.bind (fun index -> rest |> List.tryItem (index + 1))
    let unknownFlags =
        rest |> List.filter (fun argument ->
            argument.StartsWith("--", StringComparison.Ordinal) && argument <> "--run")
    if not unknownFlags.IsEmpty then
        let joined = String.concat ", " unknownFlags
        failUsage $"unsupported option(s): {joined}"

    if List.contains "--run" rest && runId.IsNone then
        failUsage "--run requires a value"

    match positional with
    | [ taskIdArgument ] ->
        match tryTaskId taskIdArgument with
        | Error message -> failClosed message
        | Ok taskId ->
            match runId with
            | Some value when value = "." || value = ".." -> failClosed $"invalid run ID '{value}'"
            | Some value when not (runIdRegex.IsMatch value) -> failClosed $"invalid run ID '{value}'"
            | _ ->
                let run = runId |> Option.defaultValue (DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"))
                match SafeFs.createScratchRoot taskId run with
                | Error message -> failClosed message
                | Ok (rootCap, rootPath) ->
                    let manifest =
                        { Version = manifestVersion
                          TaskId = taskId
                          RunId = run
                          Root = rootPath
                          RootId = rootCap.Identity
                          Sealed = false
                          Entries = [] }

                    match writeInitialManifest rootCap manifest with
                    | Error message ->
                        SafeFs.closeCap rootCap
                        failClosed message
                    | Ok () ->
                        SafeFs.closeCap rootCap
                        printfn "%s" rootPath
                        exit 0
    | _ -> failUsage "usage: create <TASK-ID> [--run <RUN-ID>]"

let cmdRegister (rest: string list) =
    let positional = rest |> List.filter (fun argument -> not (argument.StartsWith("--", StringComparison.Ordinal)))
    let unknownFlags = rest |> List.filter (fun argument -> argument.StartsWith("--", StringComparison.Ordinal))
    if not unknownFlags.IsEmpty then
        let joined = String.concat ", " unknownFlags
        failUsage $"unsupported option(s): {joined}"
    match positional with
    | rootArgument :: paths when not paths.IsEmpty ->
        match tryResolveRootPath rootArgument with
        | Error message -> failClosed message
        | Ok rootPath ->
            match loadManifestBound rootPath with
            | Error message -> failClosed message
            | Ok (rootCap, manifest) ->
                if manifest.Sealed then
                    SafeFs.closeCap rootCap
                    failClosed "scratch root is sealed; registration is rejected"

                // Open + digest each input once, bound to its verified handle.
                let opened = ResizeArray<string * string * string>() // relative, fileId, digest
                let mutable error : string option = None

                for input in paths do
                    if error.IsNone then
                        match tryResolveInputPath rootPath input with
                        | Error message -> error <- Some message
                        | Ok full ->
                            if String.Equals(full, Path.GetFullPath(Path.Combine(rootPath, manifestFileName)), pathComparison) then
                                error <- Some "cannot register the manifest file itself"
                            else
                                let relative = Path.GetRelativePath(rootPath, full).Replace("\\", "/")
                                match SafeFs.openEntry rootCap relative with
                                | OpenedFile cap ->
                                    match SafeFs.readAllBytes cap with
                                    | Error message ->
                                        SafeFs.closeCap cap
                                        error <- Some message
                                    | Ok bytes ->
                                        opened.Add(relative, cap.Identity, SafeFs.digestOf bytes)
                                        SafeFs.closeCap cap
                                | OpenMissing | OpenIsDirectory ->
                                    error <- Some $"path does not exist or is not a file: {input}"
                                | OpenReparseFinal ->
                                    error <- Some $"path must not be a reparse point: {input}"
                                | OpenReparseAncestor ->
                                    error <- Some $"path traverses a reparse point: {input}"
                                | OpenFailed message ->
                                    error <- Some message

                match error with
                | Some message ->
                    SafeFs.closeCap rootCap
                    failClosed message
                | None ->
                    let addEntries (current: ScratchManifest) =
                        let existing = current.Entries |> List.map (fun entry -> entry.Path) |> Set.ofList
                        let added =
                            opened
                            |> Seq.filter (fun (relative, _, _) -> not (existing.Contains relative))
                            |> Seq.map (fun (relative, fileId, digest) ->
                                { Path = relative
                                  Kind = "file"
                                  Promoted = false
                                  PromotedTo = ""
                                  FileId = fileId
                                  Digest = digest })
                            |> Seq.toList

                        Ok({ current with Entries = current.Entries @ added }, added.Length)

                    match mutateManifest rootCap addEntries with
                    | Error message ->
                        SafeFs.closeCap rootCap
                        failClosed message
                    | Ok count ->
                        SafeFs.closeCap rootCap
                        printfn "registered %d path(s) in %s" count rootPath
                        exit 0
    | _ -> failUsage "usage: register <ROOT> <PATH> [<PATH>...]"

let cmdReport (rest: string list) =
    let positional = rest |> List.filter (fun argument -> not (argument.StartsWith("--", StringComparison.Ordinal)))
    let unknownFlags = rest |> List.filter (fun argument -> argument.StartsWith("--", StringComparison.Ordinal))
    if not unknownFlags.IsEmpty then
        let joined = String.concat ", " unknownFlags
        failUsage $"unsupported option(s): {joined}"
    match positional with
    | [ rootArgument ] ->
        match tryResolveRootPath rootArgument with
        | Error message -> failClosed message
        | Ok rootPath ->
            match loadManifestBound rootPath with
            | Error message -> failClosed message
            | Ok (rootCap, manifest) ->
                printfn "scratch root: %s" rootPath
                printfn "task: %s  run: %s  sealed: %s" manifest.TaskId manifest.RunId (if manifest.Sealed then "yes" else "no")
                printfn "registered entries: %d" (List.length manifest.Entries)

                for entry in manifest.Entries do
                    let status = if entry.Promoted then $"promoted -> {entry.PromotedTo}" else "disposable"
                    printfn "  - %s [%s]" entry.Path status

                let registered = manifest.Entries |> List.map (fun entry -> entry.Path) |> Set.ofList
                let retained = classifyRetainedMaterial registered (scanRoot rootPath)

                // Registered targets that no longer exist are also reported.
                let missing =
                    manifest.Entries
                    |> List.choose (fun entry ->
                        match tryResolveEntryPath rootPath entry.Path with
                        | Ok full when not (File.Exists full || Directory.Exists full) ->
                            Some(entry.Path, "registered target is missing")
                        | _ -> None)

                SafeFs.closeCap rootCap

                if not retained.IsEmpty || not missing.IsEmpty then
                    printfn "retained material:"
                    for relative, reason in retained @ missing do printfn "  - %s (%s)" relative reason
                exit 0
    | _ -> failUsage "usage: report <ROOT>"

let cmdPromote (rest: string list) =
    let positional = stripValueOptions [ "--destination"; "--as" ] rest
    let destination = rest |> List.tryFindIndex ((=) "--destination") |> Option.bind (fun index -> rest |> List.tryItem (index + 1))
    let asName = rest |> List.tryFindIndex ((=) "--as") |> Option.bind (fun index -> rest |> List.tryItem (index + 1))
    let unknownFlags =
        rest |> List.filter (fun argument ->
            argument.StartsWith("--", StringComparison.Ordinal) && argument <> "--destination" && argument <> "--as")
    if not unknownFlags.IsEmpty then
        let joined = String.concat ", " unknownFlags
        failUsage $"unsupported option(s): {joined}"

    if List.contains "--as" rest && asName.IsNone then
        failUsage "--as requires a value"

    match positional with
    | [ rootArgument; entryArgument ] ->
        match destination with
        | None -> failUsage "promote requires --destination docs|scripts"
        | Some value when value <> "docs" && value <> "scripts" -> failUsage "promote --destination must be docs or scripts"
        | Some namespaceName ->
            match tryResolveRootPath rootArgument with
            | Error message -> failClosed message
            | Ok rootPath ->
                match loadManifestBound rootPath with
                | Error message -> failClosed message
                | Ok (rootCap, manifest) ->
                    if manifest.Sealed then
                        SafeFs.closeCap rootCap
                        failClosed "scratch root is sealed; promotion is rejected"

                    match tryResolveInputPath rootPath entryArgument with
                    | Error message ->
                        SafeFs.closeCap rootCap
                        failClosed message
                    | Ok sourceFull ->
                        let sourceRelative = Path.GetRelativePath(rootPath, sourceFull).Replace("\\", "/")

                        match manifest.Entries |> List.tryFind (fun entry -> String.Equals(entry.Path, sourceRelative, pathComparison)) with
                        | None ->
                            SafeFs.closeCap rootCap
                            failClosed $"source is not a registered scratch entry: {sourceRelative}"
                        | Some entry when entry.Promoted ->
                            SafeFs.closeCap rootCap
                            failClosed $"entry is already promoted: {sourceRelative}"
                        | Some entry ->
                            match SafeFs.openEntry rootCap sourceRelative with
                            | OpenedFile cap ->
                                match SafeFs.readAllBytes cap with
                                | Error message ->
                                    SafeFs.closeCap cap
                                    SafeFs.closeCap rootCap
                                    failClosed message
                                | Ok sourceBytes ->
                                    let actualDigest = SafeFs.digestOf sourceBytes
                                    if not (String.Equals(cap.Identity, entry.FileId, StringComparison.OrdinalIgnoreCase))
                                       || not (String.Equals(actualDigest, entry.Digest, StringComparison.OrdinalIgnoreCase)) then
                                        SafeFs.closeCap cap
                                        SafeFs.closeCap rootCap
                                        failClosed $"source identity or digest does not match the manifest: {sourceRelative}"
                                    else
                                        let name = asName |> Option.defaultValue (Path.GetFileName sourceFull)
                                        if not (isSafeFileName name) then
                                            SafeFs.closeCap cap
                                            SafeFs.closeCap rootCap
                                            failClosed $"invalid destination file name: {name}"

                                        let workingDirectory = Directory.GetCurrentDirectory()
                                        match SafeFs.openDestinationDir workingDirectory manifest.TaskId namespaceName with
                                        | Error message ->
                                            SafeFs.closeCap cap
                                            SafeFs.closeCap rootCap
                                            failClosed message
                                        | Ok destCap ->
                                            match SafeFs.writeDestinationFile destCap name sourceBytes with
                                            | Error message ->
                                                SafeFs.closeCap cap
                                                SafeFs.closeCap destCap
                                                SafeFs.closeCap rootCap
                                                failClosed message
                                            | Ok () ->
                                                SafeFs.closeCap destCap
                                                SafeFs.closeCap cap

                                                let destinationFull = Path.Combine(workingDirectory, ".tasks", manifest.TaskId, namespaceName, name)
                                                let destinationRelative = Path.GetRelativePath(workingDirectory, destinationFull).Replace("\\", "/")

                                                let markPromoted (current: ScratchManifest) =
                                                    if current.Sealed then
                                                        Error "scratch root is sealed; promotion is rejected"
                                                    else
                                                        match current.Entries |> List.tryFind (fun candidate -> String.Equals(candidate.Path, sourceRelative, pathComparison)) with
                                                        | None -> Error $"source is not a registered scratch entry: {sourceRelative}"
                                                        | Some candidate when candidate.Promoted -> Error $"entry is already promoted: {sourceRelative}"
                                                        | Some candidate ->
                                                            let updatedEntry = { candidate with Promoted = true; PromotedTo = destinationRelative }
                                                            let updatedEntries =
                                                                current.Entries
                                                                |> List.map (fun item ->
                                                                    if String.Equals(item.Path, sourceRelative, pathComparison) then updatedEntry
                                                                    else item)
                                                            Ok({ current with Entries = updatedEntries }, ())

                                                match mutateManifest rootCap markPromoted with
                                                | Error message ->
                                                    SafeFs.closeCap rootCap
                                                    failClosed message
                                                | Ok () ->
                                                    SafeFs.closeCap rootCap
                                                    printfn "promoted %s -> %s" sourceRelative destinationRelative
                                                    exit 0
                            | OpenMissing ->
                                SafeFs.closeCap rootCap
                                failClosed $"source file does not exist: {sourceFull}"
                            | OpenReparseFinal ->
                                SafeFs.closeCap rootCap
                                failClosed $"source must not be a reparse point: {sourceRelative}"
                            | OpenReparseAncestor ->
                                SafeFs.closeCap rootCap
                                failClosed $"source traverses a reparse point: {sourceRelative}"
                            | OpenIsDirectory ->
                                SafeFs.closeCap rootCap
                                failClosed $"source is not a file: {sourceRelative}"
                            | OpenFailed message ->
                                SafeFs.closeCap rootCap
                                failClosed message
    | _ -> failUsage "usage: promote <ROOT> <ENTRY-PATH> --destination <docs|scripts> [--as <NAME>]"

let cmdSeal (rest: string list) =
    let positional = rest |> List.filter (fun argument -> not (argument.StartsWith("--", StringComparison.Ordinal)))
    let unknownFlags = rest |> List.filter (fun argument -> argument.StartsWith("--", StringComparison.Ordinal))
    if not unknownFlags.IsEmpty then
        let joined = String.concat ", " unknownFlags
        failUsage $"unsupported option(s): {joined}"
    match positional with
    | [ rootArgument ] ->
        match tryResolveRootPath rootArgument with
        | Error message -> failClosed message
        | Ok rootPath ->
            match loadManifestBound rootPath with
            | Error message -> failClosed message
            | Ok (rootCap, _) ->
                match mutateManifest rootCap (fun current ->
                    if current.Sealed then Ok(current, false)
                    else Ok({ current with Sealed = true }, true)) with
                | Error message ->
                    SafeFs.closeCap rootCap
                    failClosed message
                | Ok wasSealed ->
                    SafeFs.closeCap rootCap
                    if not wasSealed then
                        printfn "scratch root already sealed: %s" rootPath
                    else
                        printfn "sealed %s" rootPath
                    exit 0
    | _ -> failUsage "usage: seal <ROOT>"

let cmdClean (rest: string list) =
    let positional = rest |> List.filter (fun argument -> not (argument.StartsWith("--", StringComparison.Ordinal)))
    let unknownFlags = rest |> List.filter (fun argument -> argument.StartsWith("--", StringComparison.Ordinal))
    if not unknownFlags.IsEmpty then
        let joined = String.concat ", " unknownFlags
        failUsage $"unsupported option(s): {joined}"
    match positional with
    | [ rootArgument ] ->
        match tryResolveRootPath rootArgument with
        | Error message -> failClosed message
        | Ok rootPath ->
            match loadManifestBound rootPath with
            | Error message -> failClosed message
            | Ok (rootCap, manifest) ->
                // Validate every entry (containment + handle-relative reparse check) before
                // deleting anything, so a single escaped or reparse entry fails closed and
                // deletes nothing. Missing and directory entries are retained, not fatal.
                let opened = ResizeArray<ScratchEntry * FsCap>()
                let missing = ResizeArray<string>()
                let isDirectory = ResizeArray<string>()
                let mutable abort : string option = None

                for entry in manifest.Entries do
                    if abort.IsNone then
                        match tryResolveEntryPath rootPath entry.Path with
                        | Error message -> abort <- Some message
                        | Ok _ ->
                            match SafeFs.openEntry rootCap entry.Path with
                            | OpenedFile cap -> opened.Add(entry, cap)
                            | OpenMissing -> missing.Add entry.Path
                            | OpenIsDirectory -> isDirectory.Add entry.Path
                            | OpenReparseFinal | OpenReparseAncestor ->
                                abort <- Some $"entry '{entry.Path}' traverses a reparse point"
                            | OpenFailed message -> abort <- Some message

                let closeOpened () = for _, cap in opened do SafeFs.closeCap cap

                match abort with
                | Some message ->
                    closeOpened ()
                    SafeFs.closeCap rootCap
                    failClosed message
                | None ->
                    if not manifest.Sealed then
                        printfn "scratch root is not sealed; no entries deleted"
                        for entry in manifest.Entries do printfn "  - %s (retained, not sealed)" entry.Path
                        closeOpened ()
                        let registered = manifest.Entries |> List.map (fun entry -> entry.Path) |> Set.ofList
                        for relative, reason in classifyRetainedMaterial registered (scanRoot rootPath) do
                            printfn "  - %s (%s)" relative reason
                        SafeFs.closeCap rootCap
                        exit 0
                    else
                        // Delete only files whose identity and digest still match the
                        // manifest. Handle-relative open already rejected reparse; identity
                        // and digest binding ensure we delete exactly what was registered.
                        let deleted = ResizeArray<string>()
                        let retained = ResizeArray<string * string>()

                        for entry, cap in opened do
                            if entry.Promoted then
                                retained.Add(entry.Path, $"promoted -> {entry.PromotedTo}")
                                SafeFs.closeCap cap
                            else
                                let matches =
                                    SafeFs.readAllBytes cap
                                    |> Result.map (fun bytes ->
                                        String.Equals(cap.Identity, entry.FileId, StringComparison.OrdinalIgnoreCase)
                                        && String.Equals(SafeFs.digestOf bytes, entry.Digest, StringComparison.OrdinalIgnoreCase))
                                    |> Result.defaultValue false

                                if not matches then
                                    retained.Add(entry.Path, "identity or digest mismatch")
                                    SafeFs.closeCap cap
                                else
                                    match SafeFs.deleteEntry cap with
                                    | Ok () ->
                                        deleted.Add entry.Path
                                        SafeFs.closeCap cap
                                    | Error message ->
                                        retained.Add(entry.Path, message)
                                        SafeFs.closeCap cap

                        for path in missing do retained.Add(path, "registered target is missing")
                        for path in isDirectory do retained.Add(path, "registered path is not a file")

                        let registered = manifest.Entries |> List.map (fun entry -> entry.Path) |> Set.ofList
                        for relative, reason in classifyRetainedMaterial registered (scanRoot rootPath) do
                            retained.Add(relative, reason)

                        SafeFs.closeCap rootCap

                        printfn "scratch root: %s" rootPath
                        printfn "deleted: %d" deleted.Count
                        printfn "retained: %d" retained.Count
                        for relative, reason in retained do printfn "  - %s (%s)" relative reason
                        exit 0
    | _ -> failUsage "usage: clean <ROOT>"

let args = fsi.CommandLineArgs |> Array.skip 1 |> Array.toList

match args with
| [] ->
    usage ()
    exit 2
| "--help" :: _
| "-h" :: _
| "help" :: _ ->
    usage ()
    exit 0
| command :: rest ->
    match command with
    | "create" -> cmdCreate rest
    | "register" -> cmdRegister rest
    | "report" -> cmdReport rest
    | "promote" -> cmdPromote rest
    | "seal" -> cmdSeal rest
    | "clean" -> cmdClean rest
    | unknown ->
        eprintfn "unknown command '%s'" unknown
        usage ()
        exit 2
