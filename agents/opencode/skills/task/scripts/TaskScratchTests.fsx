// Deterministic black-box tests for the owned task scratch lifecycle helper
// (TaskScratch.fsx). Every command runs as a child `dotnet fsi` process with a
// fresh, GUID-isolated fixture working directory; scratch roots live under the
// canonical <system-temp>/opencode/tasks/<TASK-ID>/<RUN-ID> layout and are
// tracked so the fixture removes exactly what it created. The manifest
// contract is v2: it binds rootId (root stable identity) and per-entry fileId
// plus SHA-256 digest; every mutation is handle-relative with reparse
// traversal disabled (Windows NtCreateFile + FILE_OPEN_REPARSE_POINT).
//
// Windows-only scope (approved waiver): every helper operation refuses on
// non-Windows platforms and no weaker compatibility path exists, so clean is
// never report-only and no descriptor/flock fallback is retained. This host
// is Windows, so the non-Windows refusal branches cannot execute here; the
// fail-closed contract is pinned by static source-contract checks (see
// "platform contract static checks" below) and behavior is exercised only
// where the host can run it. Manifest mutations serialize under an exclusive
// Windows open (share mode 0); contention is exercised behaviorally.
// Reparse fixtures prefer .NET symlink APIs and fall back to
// `cmd /c mklink /J` junctions; fixtures the OS cannot create (for example
// file symlinks without Developer Mode) are skipped with a note instead of
// failing. Promotion re-derives the destination below the current working
// directory from the trusted filesystem root; a reparse point anywhere in the
// CWD path chain fails closed (the child preserves the junction spelling).

open System
open System.Diagnostics
open System.IO
open System.Security.Cryptography
open System.Text.Json
open System.Text.Json.Nodes
open System.Text.RegularExpressions

type RunResult = { ExitCode: int; Output: string }

let scratchScript = Path.Combine(__SOURCE_DIRECTORY__, "TaskScratch.fsx")
let tempBase = Path.Combine(Path.GetTempPath(), "opencode")
let fixtureRoot = Path.Combine(tempBase, $"task-scratch-tests-{Guid.NewGuid():N}")
let createdRoots = ResizeArray<string>()
let taskDirs = ResizeArray<string>()

Directory.CreateDirectory fixtureRoot |> ignore

let runScratch workingDirectory arguments =
    let start = ProcessStartInfo("dotnet", WorkingDirectory = workingDirectory)
    start.ArgumentList.Add "fsi"
    start.ArgumentList.Add "--nologo"
    start.ArgumentList.Add scratchScript
    arguments |> List.iter start.ArgumentList.Add
    start.RedirectStandardOutput <- true
    start.RedirectStandardError <- true
    start.UseShellExecute <- false
    start.CreateNoWindow <- true

    use child = Process.Start start
    let stdout = child.StandardOutput.ReadToEndAsync()
    let stderr = child.StandardError.ReadToEndAsync()
    child.WaitForExit()
    { ExitCode = child.ExitCode
      Output = stdout.Result + Environment.NewLine + stderr.Result }

let norm (text: string) = text.Replace("\\", "/")

let containsText (haystack: string) (needle: string) =
    (norm haystack).IndexOf(norm needle, StringComparison.OrdinalIgnoreCase) >= 0

let assertExit name expected (result: RunResult) =
    if result.ExitCode <> expected then
        failwithf "%s: expected exit %d, got %d. Output:\n%s" name expected result.ExitCode (result.Output)

let assertContains name expected (actual: string) =
    if not (containsText actual expected) then
        failwithf "%s: expected output containing %A, got:\n%s" name expected actual

let assertTrue name condition =
    if not condition then failwith name

let assertEqual name expected actual =
    if actual <> expected then failwithf "%s: expected %A, got %A" name expected actual

let uniqueTaskId () =
    let digits =
        Guid.NewGuid().ToString("N")
        |> Seq.filter Char.IsDigit
        |> Seq.truncate 8
        |> Seq.toArray
        |> String
    "TESTS-" + digits

let uniqueRunId () = Guid.NewGuid().ToString("N")

let createRoot taskId runId =
    let result = runScratch fixtureRoot [ "create"; taskId; "--run"; runId ]
    assertExit "create" 0 result
    let root =
        result.Output.Trim().Split([| '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.last
        |> fun line -> line.Trim()
    createdRoots.Add root
    taskDirs.Add(Path.GetDirectoryName root)
    root

let readManifest root =
    use doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "manifest.json")))
    doc.RootElement.Clone()

let editManifest root (edit: JsonObject -> unit) =
    let path = Path.Combine(root, "manifest.json")
    let node = JsonNode.Parse(File.ReadAllText path) :?> JsonObject
    edit node
    File.WriteAllText(path, node.ToJsonString())

let writeRawManifest root (text: string) =
    File.WriteAllText(Path.Combine(root, "manifest.json"), text)

let entryPath (entry: JsonElement) = entry.GetProperty("path").GetString()
let entryPromoted (entry: JsonElement) = entry.GetProperty("promoted").GetBoolean()
let entryFileId (entry: JsonElement) = entry.GetProperty("fileId").GetString()
let entryDigest (entry: JsonElement) = entry.GetProperty("digest").GetString()

// A reparse-point entry fixture (path, kind, promoted, promotedTo). Hand-edited
// v2 entries carry a plausible fileId/digest so validation reaches the intended
// fail-closed condition instead of tripping the missing-fileId-or-digest check.
let makeEntry (path: string) (kind: string) =
    let node = JsonObject()
    node.["path"] <- JsonValue.Create path
    node.["kind"] <- JsonValue.Create kind
    node.["promoted"] <- JsonValue.Create false
    node.["promotedTo"] <- JsonValue.Create ""
    node.["fileId"] <- JsonValue.Create "0:0"
    node.["digest"] <- JsonValue.Create (String.replicate 64 "0")
    node

// ---------------------------------------------------------------- reparse helpers

let tryCreateDirectoryReparse linkPath targetPath =
    try
        Directory.CreateSymbolicLink(linkPath, targetPath) |> ignore
        DirectoryInfo(linkPath).Attributes.HasFlag FileAttributes.ReparsePoint
    with _ ->
        try
            let start =
                ProcessStartInfo(
                    "cmd",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false)
            start.ArgumentList.Add "/c"
            start.ArgumentList.Add "mklink"
            start.ArgumentList.Add "/J"
            start.ArgumentList.Add linkPath
            start.ArgumentList.Add targetPath
            use child = Process.Start start
            child.WaitForExit()
            Directory.Exists linkPath && DirectoryInfo(linkPath).Attributes.HasFlag FileAttributes.ReparsePoint
        with _ ->
            false

let tryCreateFileReparse linkPath targetPath =
    try
        File.CreateSymbolicLink(linkPath, targetPath) |> ignore
        FileInfo(linkPath).Attributes.HasFlag FileAttributes.ReparsePoint
    with _ ->
        false

// Remove reparse links before recursive deletion so junction/symlink targets
// are never followed; this fixture only ever deletes what it created.
let safeDeleteRoot (root: string) =
    if Directory.Exists root then
        let rec removeLinks (dir: DirectoryInfo) =
            for info in dir.EnumerateFileSystemInfos() do
                try
                    if info.Attributes.HasFlag FileAttributes.ReparsePoint then
                        if info.Attributes.HasFlag FileAttributes.Directory then Directory.Delete(info.FullName)
                        else File.Delete(info.FullName)
                    elif info.Attributes.HasFlag FileAttributes.Directory then
                        removeLinks (DirectoryInfo info.FullName)
                with _ ->
                    ()

        try
            removeLinks (DirectoryInfo root)
            Directory.Delete(root, true)
        with _ ->
            ()

try
    printfn "[scratch] create and isolation"
    let taskId = uniqueTaskId ()
    let runA = uniqueRunId ()
    let runB = uniqueRunId ()
    let rootA = createRoot taskId runA
    let rootB = createRoot taskId runB

    let expectedRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "opencode", "tasks", taskId, runA))
    assertEqual "canonical root path" expectedRoot rootA
    // D001: the root must be physically under the OS-temp canonical opencode
    // base and its parent-of-parent must be the canonical tasks layout.
    assertTrue
        "root physically under OS temp opencode base"
        (rootA.StartsWith(Path.GetFullPath tempBase + string Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
    assertEqual
        "root parent-of-parent is the canonical tasks base"
        (Path.GetFullPath(Path.Combine(Path.GetTempPath(), "opencode", "tasks")))
        (Path.GetDirectoryName(Path.GetDirectoryName rootA))
    assertTrue "runs are isolated" (rootA <> rootB)
    assertEqual "run isolation task dir" (Path.GetDirectoryName rootA) (Path.GetDirectoryName rootB)

    let manifestA = readManifest rootA
    assertEqual "manifest version" 2 (manifestA.GetProperty("version").GetInt32())
    assertEqual "manifest taskId" taskId (manifestA.GetProperty("taskId").GetString())
    assertEqual "manifest runId" runA (manifestA.GetProperty("runId").GetString())
    assertEqual "manifest root" rootA (manifestA.GetProperty("root").GetString())
    assertTrue "manifest rootId recorded" (not (String.IsNullOrWhiteSpace(manifestA.GetProperty("rootId").GetString())))
    assertEqual "manifest sealed" false (manifestA.GetProperty("sealed").GetBoolean())
    assertEqual "manifest empty entries" 0 (manifestA.GetProperty("entries").GetArrayLength())

    let otherTask = uniqueTaskId ()
    let rootOther = createRoot otherTask (uniqueRunId ())
    assertTrue "tasks are isolated" (Path.GetDirectoryName rootA <> Path.GetDirectoryName rootOther)

    // Duplicate create fails closed on the same run.
    let duplicate = runScratch fixtureRoot [ "create"; taskId; "--run"; runA ]
    assertExit "duplicate create" 1 duplicate
    assertContains "duplicate create diagnostic" "already exists" duplicate.Output

    // Default run ID is a deterministic timestamp pattern.
    let defaultRun = runScratch fixtureRoot [ "create"; taskId ]
    assertExit "default run create" 0 defaultRun
    let defaultRoot =
        defaultRun.Output.Trim().Split([| '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.last
        |> fun line -> line.Trim()
    assertTrue "default run id is a timestamp" (Regex.IsMatch(Path.GetFileName defaultRoot, @"^\d{17}$"))
    createdRoots.Add defaultRoot
    taskDirs.Add(Path.GetDirectoryName defaultRoot)

    // Invalid task IDs fail closed.
    for bad in [ "123-ABC"; "INFRA"; "INFRA-"; "INFRA-009-1"; "-9"; "INFRA-9x" ] do
        let result = runScratch fixtureRoot [ "create"; bad; "--run"; uniqueRunId () ]
        assertExit "invalid task id" 1 result
        assertContains "invalid task id diagnostic" "task ID must match" result.Output

    // Invalid run IDs fail closed.
    for badRun in [ "a/b"; "a b"; "a*b" ] do
        let result = runScratch fixtureRoot [ "create"; taskId; "--run"; badRun ]
        assertExit "invalid run id" 1 result
        assertContains "invalid run id diagnostic" "invalid run ID" result.Output

    // Usage errors.
    let unknownFlag = runScratch fixtureRoot [ "create"; taskId; "--force" ]
    assertExit "create unknown flag" 2 unknownFlag
    let extraPositional = runScratch fixtureRoot [ "create"; taskId; "extra" ]
    assertExit "create extra positional" 2 extraPositional
    let danglingRun = runScratch fixtureRoot [ "create"; taskId; "--run" ]
    assertExit "create --run without value" 2 danglingRun

    printfn "[scratch] dot run IDs"
    // D005: run IDs may contain dots and mixed separators, but "." and ".."
    // are never usable run IDs.
    for dotRun in [ "run.1"; "v1.0.0"; "a.b-c_d.1" ] do
        let dotRoot = createRoot taskId dotRun
        assertEqual
            "dot run id root"
            (Path.GetFullPath(Path.Combine(Path.GetTempPath(), "opencode", "tasks", taskId, dotRun)))
            dotRoot

    for dotPath in [ "."; ".." ] do
        let dotReject = runScratch fixtureRoot [ "create"; taskId; "--run"; dotPath ]
        assertExit "dot path run id rejected" 1 dotReject
        assertContains "dot path run id diagnostic" "invalid run ID" dotReject.Output

    printfn "[scratch] manifest validation fails closed"
    let badJsonRoot = createRoot taskId (uniqueRunId ())
    writeRawManifest badJsonRoot "{ not json"
    let badJson = runScratch fixtureRoot [ "report"; badJsonRoot ]
    assertExit "malformed manifest json" 1 badJson
    assertContains "malformed manifest diagnostic" "JSON parse failed" badJson.Output

    let versionRoot = createRoot taskId (uniqueRunId ())
    editManifest versionRoot (fun node -> node.["version"] <- JsonValue.Create(3 : int))
    let badVersion = runScratch fixtureRoot [ "report"; versionRoot ]
    assertExit "unsupported version" 1 badVersion
    assertContains "unsupported version diagnostic" "unsupported manifest version" badVersion.Output

    // A legacy v1 manifest (no rootId, no per-entry fileId/digest) is never
    // upgraded or migrated silently; it is rejected as an unsupported version.
    let legacyV1Root = createRoot taskId (uniqueRunId ())
    editManifest legacyV1Root (fun node -> node.["version"] <- JsonValue.Create(1 : int))
    let legacyV1 = runScratch fixtureRoot [ "report"; legacyV1Root ]
    assertExit "legacy v1 manifest rejected" 1 legacyV1
    assertContains "legacy v1 manifest diagnostic" "unsupported manifest version 1" legacyV1.Output

    let noVersionRoot = createRoot taskId (uniqueRunId ())
    editManifest noVersionRoot (fun node -> node.Remove "version" |> ignore)
    let noVersion = runScratch fixtureRoot [ "report"; noVersionRoot ]
    assertExit "missing version" 1 noVersion
    assertContains "missing version diagnostic" "missing numeric 'version'" noVersion.Output

    let noEntriesRoot = createRoot taskId (uniqueRunId ())
    editManifest noEntriesRoot (fun node -> node.Remove "entries" |> ignore)
    let noEntries = runScratch fixtureRoot [ "report"; noEntriesRoot ]
    assertExit "missing entries" 1 noEntries
    assertContains "missing entries diagnostic" "missing 'entries' array" noEntries.Output

    let rootMismatchRoot = createRoot taskId (uniqueRunId ())
    editManifest rootMismatchRoot (fun node -> node.["root"] <- JsonValue.Create(Path.Combine(fixtureRoot, "elsewhere")))
    let rootMismatch = runScratch fixtureRoot [ "report"; rootMismatchRoot ]
    assertExit "root mismatch" 1 rootMismatch
    assertContains "root mismatch diagnostic" "does not match the requested scratch root" rootMismatch.Output

    let runMismatchRoot = createRoot taskId (uniqueRunId ())
    editManifest runMismatchRoot (fun node -> node.["runId"] <- JsonValue.Create "wrong-run")
    let runMismatch = runScratch fixtureRoot [ "report"; runMismatchRoot ]
    assertExit "run id mismatch" 1 runMismatch
    assertContains "run id mismatch diagnostic" "runId" runMismatch.Output

    let taskMismatchRoot = createRoot taskId (uniqueRunId ())
    editManifest taskMismatchRoot (fun node -> node.["taskId"] <- JsonValue.Create "OTHER-1")
    let taskMismatch = runScratch fixtureRoot [ "report"; taskMismatchRoot ]
    assertExit "task id mismatch" 1 taskMismatch
    assertContains "task id mismatch diagnostic" "taskId" taskMismatch.Output

    // v2-specific malformed manifests fail closed: missing or wrong rootId and
    // entries that lack the required fileId/digest binding.
    let noRootIdRoot = createRoot taskId (uniqueRunId ())
    editManifest noRootIdRoot (fun node -> node.Remove "rootId" |> ignore)
    let noRootId = runScratch fixtureRoot [ "report"; noRootIdRoot ]
    assertExit "missing rootId" 1 noRootId
    assertContains "missing rootId diagnostic" "rootId" noRootId.Output

    let rootIdMismatchRoot = createRoot taskId (uniqueRunId ())
    editManifest rootIdMismatchRoot (fun node -> node.["rootId"] <- JsonValue.Create "cafe:1")
    let rootIdMismatch = runScratch fixtureRoot [ "report"; rootIdMismatchRoot ]
    assertExit "rootId mismatch" 1 rootIdMismatch
    assertContains "rootId mismatch diagnostic" "rootId" rootIdMismatch.Output

    let noFileIdRoot = createRoot taskId (uniqueRunId ())
    editManifest noFileIdRoot (fun node ->
        let bad = makeEntry "somefile" "file"
        bad.Remove "fileId" |> ignore
        (node.["entries"] :?> JsonArray).Add bad)
    let noFileId = runScratch fixtureRoot [ "report"; noFileIdRoot ]
    assertExit "entry missing fileId" 1 noFileId
    assertContains "entry missing fileId diagnostic" "missing fileId or digest" noFileId.Output

    let nonStringFileIdRoot = createRoot taskId (uniqueRunId ())
    editManifest nonStringFileIdRoot (fun node ->
        let bad = makeEntry "somefile" "file"
        bad.["fileId"] <- JsonValue.Create(42)
        (node.["entries"] :?> JsonArray).Add bad)
    let nonStringFileId = runScratch fixtureRoot [ "report"; nonStringFileIdRoot ]
    assertExit "non-string entry fileId" 1 nonStringFileId
    assertContains "non-string entry fileId diagnostic" "missing fileId or digest" nonStringFileId.Output

    // A non-canonical task ID in an otherwise canonical layout must fail closed.
    let nonCanonicalRoot = Path.Combine(tempBase, "tasks", "123-X", uniqueRunId ())
    Directory.CreateDirectory nonCanonicalRoot |> ignore
    createdRoots.Add nonCanonicalRoot
    taskDirs.Add(Path.GetDirectoryName nonCanonicalRoot)
    let nonCanonicalManifest = JsonObject()
    nonCanonicalManifest.["version"] <- JsonValue.Create(2 : int)
    nonCanonicalManifest.["taskId"] <- JsonValue.Create "123-X"
    nonCanonicalManifest.["runId"] <- JsonValue.Create(Path.GetFileName nonCanonicalRoot)
    nonCanonicalManifest.["root"] <- JsonValue.Create nonCanonicalRoot
    nonCanonicalManifest.["rootId"] <- JsonValue.Create "0:0"
    nonCanonicalManifest.["sealed"] <- JsonValue.Create false
    nonCanonicalManifest.["entries"] <- JsonArray()
    File.WriteAllText(Path.Combine(nonCanonicalRoot, "manifest.json"), nonCanonicalManifest.ToJsonString())
    let nonCanonical = runScratch fixtureRoot [ "report"; nonCanonicalRoot ]
    assertExit "non-canonical task id" 1 nonCanonical
    assertContains "non-canonical task id diagnostic" "not a canonical ID" nonCanonical.Output

    // Malformed entry paths fail closed, and clean must not delete anything.
    let escapedEntryRoot = createRoot taskId (uniqueRunId ())
    editManifest escapedEntryRoot (fun node ->
        (node.["entries"] :?> JsonArray).Add(makeEntry "../evil" "file"))
    let escapedEntry = runScratch fixtureRoot [ "report"; escapedEntryRoot ]
    assertExit "escaped manifest entry" 1 escapedEntry
    assertContains "escaped manifest entry diagnostic" "escapes the scratch root" escapedEntry.Output
    File.WriteAllText(Path.Combine(escapedEntryRoot, "innocent.txt"), "must survive")
    let escapedEntryClean = runScratch fixtureRoot [ "clean"; escapedEntryRoot ]
    assertExit "escaped manifest entry clean" 1 escapedEntryClean
    assertTrue "escaped entry clean deleted a file" (File.Exists(Path.Combine(escapedEntryRoot, "innocent.txt")))

    let emptySegmentRoot = createRoot taskId (uniqueRunId ())
    editManifest emptySegmentRoot (fun node ->
        (node.["entries"] :?> JsonArray).Add(makeEntry "a//b" "file"))
    let emptySegment = runScratch fixtureRoot [ "report"; emptySegmentRoot ]
    assertExit "empty-segment manifest entry" 1 emptySegment
    assertContains "empty-segment manifest entry diagnostic" "malformed" emptySegment.Output

    let absEntryRoot = createRoot taskId (uniqueRunId ())
    editManifest absEntryRoot (fun node ->
        (node.["entries"] :?> JsonArray).Add(makeEntry (Path.Combine(tempBase, "evil.txt").Replace("\\", "/")) "file"))
    let absEntry = runScratch fixtureRoot [ "report"; absEntryRoot ]
    assertExit "absolute manifest entry" 1 absEntry
    assertContains "absolute manifest entry diagnostic" "absolute" absEntry.Output

    let dirKindRoot = createRoot taskId (uniqueRunId ())
    editManifest dirKindRoot (fun node ->
        (node.["entries"] :?> JsonArray).Add(makeEntry "somefile" "dir"))
    let dirKind = runScratch fixtureRoot [ "report"; dirKindRoot ]
    assertExit "unsupported entry kind" 1 dirKind
    assertContains "unsupported entry kind diagnostic" "unsupported kind" dirKind.Output

    let noPromotedRoot = createRoot taskId (uniqueRunId ())
    editManifest noPromotedRoot (fun node ->
        let bad = JsonObject()
        bad.["path"] <- JsonValue.Create "somefile"
        bad.["kind"] <- JsonValue.Create "file"
        (node.["entries"] :?> JsonArray).Add bad)
    let noPromoted = runScratch fixtureRoot [ "report"; noPromotedRoot ]
    assertExit "entry missing promoted" 1 noPromoted
    assertContains "entry missing promoted diagnostic" "missing path, kind, or promoted" noPromoted.Output

    // A valid tree relocated outside the canonical opencode/tasks layout is
    // rejected: the root anchor is opened handle-relative below the OS-temp
    // anchor chain, so a tree that is not physically inside
    // <temp>/opencode/tasks/<TASK-ID>/<RUN-ID> can never be opened.
    let copiedRoot = createRoot taskId (uniqueRunId ())
    let relocated = Path.Combine(fixtureRoot, "elsewhere", "tasks", taskId, "copy-run")
    Directory.CreateDirectory relocated |> ignore
    File.Copy(Path.Combine(copiedRoot, "manifest.json"), Path.Combine(relocated, "manifest.json"))
    editManifest relocated (fun node ->
        node.["root"] <- JsonValue.Create relocated
        node.["runId"] <- JsonValue.Create "copy-run")
    let relocatedResult = runScratch fixtureRoot [ "report"; relocated ]
    assertExit "non-canonical layout" 1 relocatedResult
    assertContains "non-canonical layout diagnostic" "cannot open directory component" relocatedResult.Output

    printfn "[scratch] duplicate manifest entries fail closed"
    // D006: duplicate entry paths make automatic deletion ambiguous and must
    // fail closed everywhere the manifest is consumed.
    let dupRoot = createRoot taskId (uniqueRunId ())
    let dupFile = Path.Combine(dupRoot, "d1.txt")
    File.WriteAllText(dupFile, "d1")
    runScratch fixtureRoot [ "register"; dupRoot; "d1.txt" ]
    |> assertExit "duplicate fixture register" 0
    editManifest dupRoot (fun node ->
        (node.["entries"] :?> JsonArray).Add(makeEntry "d1.txt" "file"))

    let dupReport = runScratch fixtureRoot [ "report"; dupRoot ]
    assertExit "duplicate entries report" 1 dupReport
    assertContains "duplicate entries diagnostic" "duplicate entry paths" dupReport.Output

    let dupClean = runScratch fixtureRoot [ "clean"; dupRoot ]
    assertExit "duplicate entries clean" 1 dupClean
    assertContains "duplicate entries clean diagnostic" "duplicate entry paths" dupClean.Output
    assertTrue "duplicate entries clean deleted nothing" (File.Exists dupFile)

    let dupSeal = runScratch fixtureRoot [ "seal"; dupRoot ]
    assertExit "duplicate entries seal" 1 dupSeal
    assertContains "duplicate entries seal diagnostic" "duplicate entry paths" dupSeal.Output

    // Case-insensitive duplicate spellings are duplicates (manifest path
    // matching is OrdinalIgnoreCase; the supported Windows-only scope also
    // matches the case-insensitive filesystem semantics).
    let dupCaseRoot = createRoot taskId (uniqueRunId ())
    File.WriteAllText(Path.Combine(dupCaseRoot, "D2.TXT"), "x")
    runScratch fixtureRoot [ "register"; dupCaseRoot; "D2.TXT" ]
    |> assertExit "duplicate-case fixture register" 0
    editManifest dupCaseRoot (fun node ->
        (node.["entries"] :?> JsonArray).Add(makeEntry "d2.txt" "file"))
    let dupCase = runScratch fixtureRoot [ "report"; dupCaseRoot ]
    assertExit "duplicate-case report" 1 dupCase
    assertContains "duplicate-case diagnostic" "duplicate entry paths" dupCase.Output

    printfn "[scratch] registration"
    let regRoot = createRoot taskId (uniqueRunId ())
    let f1 = Path.Combine(regRoot, "f1.txt")
    let f2 = Path.Combine(regRoot, "f2.txt")
    let subDir = Path.Combine(regRoot, "sub")
    Directory.CreateDirectory subDir |> ignore
    let f3 = Path.Combine(subDir, "f3.txt")
    for path in [ f1; f2; f3 ] do
        File.WriteAllText(path, "content " + Path.GetFileName path)

    let register = runScratch fixtureRoot [ "register"; regRoot; "f1.txt"; "f2.txt"; "sub/f3.txt" ]
    assertExit "register multiple" 0 register
    assertContains "register multiple diagnostic" "registered 3 path(s)" register.Output

    let entries =
        (readManifest regRoot).GetProperty("entries").EnumerateArray()
        |> Seq.map entryPath
        |> Seq.toList
    assertEqual "registered entry order" [ "f1.txt"; "f2.txt"; "sub/f3.txt" ] entries

    // Duplicate registration is idempotent.
    let reRegister = runScratch fixtureRoot [ "register"; regRoot; "f1.txt" ]
    assertExit "re-register" 0 reRegister
    assertContains "re-register diagnostic" "registered 0 path(s)" reRegister.Output
    assertEqual "re-register entry count" 3 ((readManifest regRoot).GetProperty("entries").GetArrayLength())

    // Absolute path inside the root is accepted and stored relative.
    let absRegister = runScratch fixtureRoot [ "register"; regRoot; f1 ]
    assertExit "register absolute inside root" 0 absRegister
    assertContains "absolute registration diagnostic" "registered 0 path(s)" absRegister.Output

    // Registration rejection: missing, directory, outside, traversal.
    let missingFile = runScratch fixtureRoot [ "register"; regRoot; "nope.txt" ]
    assertExit "register missing file" 1 missingFile
    let dirRegister = runScratch fixtureRoot [ "register"; regRoot; "sub" ]
    assertExit "register directory" 1 dirRegister
    let outside = Path.Combine(fixtureRoot, "outside.txt")
    File.WriteAllText(outside, "outside")
    let outsideRegister = runScratch fixtureRoot [ "register"; regRoot; outside ]
    assertExit "register outside root" 1 outsideRegister
    assertContains "register outside diagnostic" "escapes the scratch root" outsideRegister.Output
    let traversal = runScratch fixtureRoot [ "register"; regRoot; Path.Combine("..", "outside.txt") ]
    assertExit "register traversal" 1 traversal
    assertContains "register traversal diagnostic" "escapes the scratch root" traversal.Output

    // The manifest path is accepted as the root argument.
    let manifestArg = runScratch fixtureRoot [ "register"; Path.Combine(regRoot, "manifest.json"); f2 ]
    assertExit "register via manifest argument" 0 manifestArg

    // Unknown flags are usage errors (not silently ignored).
    let registerFlag = runScratch fixtureRoot [ "register"; regRoot; "f1.txt"; "--force" ]
    assertExit "register unknown flag" 2 registerFlag

    printfn "[scratch] manifest records stable identity and digest"
    // v2 contract: every registered entry records fileId (stable root/file
    // identity) and a SHA-256 digest bound at registration time; clean and
    // promote delete/copy only bytes whose identity and digest still match.
    let bindingRoot = createRoot taskId (uniqueRunId ())
    let boundFile = Path.Combine(bindingRoot, "bound.bin")
    let boundBytes = [| 0uy .. 63uy |]
    File.WriteAllBytes(boundFile, boundBytes)
    runScratch fixtureRoot [ "register"; bindingRoot; "bound.bin" ]
    |> assertExit "binding fixture register" 0
    let boundEntry =
        (readManifest bindingRoot).GetProperty("entries").EnumerateArray()
        |> Seq.find (fun entry -> entryPath entry = "bound.bin")
    assertTrue "registered fileId recorded" (not (String.IsNullOrWhiteSpace(entryFileId boundEntry)))
    let expectedDigest = Convert.ToHexString(SHA256.HashData boundBytes).ToLowerInvariant()
    assertEqual "registered digest matches content" expectedDigest (entryDigest boundEntry)

    printfn "[scratch] manifest registration rejected"
    // D004: the manifest itself is provenance, not disposable content, and can
    // never be registered — by relative or absolute path.
    let manifestRegRoot = createRoot taskId (uniqueRunId ())
    let manifestReg = runScratch fixtureRoot [ "register"; manifestRegRoot; "manifest.json" ]
    assertExit "register manifest file" 1 manifestReg
    assertContains "register manifest file diagnostic" "cannot register the manifest file itself" manifestReg.Output

    let manifestAbs = Path.Combine(manifestRegRoot, "manifest.json")
    let manifestAbsReg = runScratch fixtureRoot [ "register"; manifestRegRoot; manifestAbs ]
    assertExit "register manifest absolute" 1 manifestAbsReg
    assertContains "register manifest absolute diagnostic" "cannot register the manifest file itself" manifestAbsReg.Output

    printfn "[scratch] report"
    let reportRoot = createRoot taskId (uniqueRunId ())
    File.WriteAllText(Path.Combine(reportRoot, "r1.txt"), "r1")
    runScratch fixtureRoot [ "register"; reportRoot; "r1.txt" ] |> assertExit "report fixture register" 0

    let report = runScratch fixtureRoot [ "report"; reportRoot ]
    assertExit "report" 0 report
    assertContains "report task line" $"task: {taskId}" report.Output
    assertContains "report sealed line" "sealed: no" report.Output
    assertContains "report entry count" "registered entries: 1" report.Output
    assertContains "report disposable entry" "[disposable]" report.Output

    // Unregistered material is retained and reported.
    File.WriteAllText(Path.Combine(reportRoot, "unknown.txt"), "unknown")
    let reportUnknown = runScratch fixtureRoot [ "report"; reportRoot ]
    assertExit "report with unknown file" 0 reportUnknown
    assertContains "report unknown diagnostic" "unregistered" reportUnknown.Output
    assertContains "report unknown path" "unknown.txt" reportUnknown.Output

    // Shared roots and roots without a manifest are rejected.
    let noManifestDir = Path.Combine(fixtureRoot, "no-manifest")
    Directory.CreateDirectory noManifestDir |> ignore
    let noManifest = runScratch fixtureRoot [ "report"; noManifestDir ]
    assertExit "report without manifest" 1 noManifest
    assertContains "report without manifest diagnostic" "manifest does not exist" noManifest.Output
    let taskLevel = Path.Combine(tempBase, "tasks", taskId)
    let taskLevelResult = runScratch fixtureRoot [ "report"; taskLevel ]
    assertExit "report task-level root" 1 taskLevelResult
    assertContains "report task-level diagnostic" "manifest does not exist" taskLevelResult.Output

    printfn "[scratch] report missing and unregistered directories"
    // D007: report must surface registered targets that vanished and
    // unregistered directories so nothing is silently dropped.
    let missingRoot = createRoot taskId (uniqueRunId ())
    let goneFile = Path.Combine(missingRoot, "gone.txt")
    File.WriteAllText(goneFile, "gone")
    runScratch fixtureRoot [ "register"; missingRoot; "gone.txt" ]
    |> assertExit "missing fixture register" 0
    File.Delete goneFile

    let missingReport = runScratch fixtureRoot [ "report"; missingRoot ]
    assertExit "report missing registered target" 0 missingReport
    assertContains "report missing diagnostic" "registered target is missing" missingReport.Output
    assertContains "report missing path" "gone.txt" missingReport.Output

    let unregisteredDir = Path.Combine(missingRoot, "leftover")
    Directory.CreateDirectory(Path.Combine(unregisteredDir, "nested")) |> ignore
    File.WriteAllText(Path.Combine(unregisteredDir, "inner.txt"), "inner")

    let dirReport = runScratch fixtureRoot [ "report"; missingRoot ]
    assertExit "report unregistered directory" 0 dirReport
    assertContains "report unregistered directory diagnostic" "unregistered directory" dirReport.Output
    assertContains "report unregistered directory path" "leftover" dirReport.Output

    runScratch fixtureRoot [ "seal"; missingRoot ] |> assertExit "missing fixture seal" 0
    let dirClean = runScratch fixtureRoot [ "clean"; missingRoot ]
    assertExit "clean with unregistered directory" 0 dirClean
    assertTrue "unregistered directory preserved" (Directory.Exists unregisteredDir)
    assertTrue "unregistered nested file preserved" (File.Exists(Path.Combine(unregisteredDir, "inner.txt")))
    assertContains "clean unregistered directory diagnostic" "unregistered directory" dirClean.Output

    printfn "[scratch] promotion and survival"
    let promoteRoot = createRoot taskId (uniqueRunId ())
    let evidenceBytes = [| 0uy .. 255uy |]
    let evidence = Path.Combine(promoteRoot, "evidence.txt")
    let evidence2 = Path.Combine(promoteRoot, "evidence2.txt")
    File.WriteAllBytes(evidence, evidenceBytes)
    File.WriteAllBytes(evidence2, evidenceBytes)
    runScratch fixtureRoot [ "register"; promoteRoot; "evidence.txt"; "evidence2.txt" ]
    |> assertExit "promote fixture register" 0

    let taskDir = Path.Combine(fixtureRoot, ".tasks", taskId)
    Directory.CreateDirectory taskDir |> ignore

    let promote = runScratch fixtureRoot [ "promote"; promoteRoot; "evidence.txt"; "--destination"; "docs" ]
    assertExit "promote to docs" 0 promote
    let promotedCopy = Path.Combine(taskDir, "docs", "evidence.txt")
    assertTrue "promoted copy exists" (File.Exists promotedCopy)
    assertEqual "promoted copy byte-verified" evidenceBytes (File.ReadAllBytes promotedCopy)
    assertTrue "promotion leaves source in scratch" (File.Exists evidence)

    let promotedEntry = (readManifest promoteRoot).GetProperty("entries").EnumerateArray() |> Seq.head
    assertEqual "manifest promoted flag" true (entryPromoted promotedEntry)
    let promotedTo = norm (promotedEntry.GetProperty("promotedTo").GetString())
    assertTrue
        "manifest promotedTo recorded"
        (promotedTo.EndsWith(".tasks/" + taskId + "/docs/evidence.txt", StringComparison.OrdinalIgnoreCase))

    let reportPromoted = runScratch fixtureRoot [ "report"; promoteRoot ]
    assertExit "report promoted" 0 reportPromoted
    assertContains "report promoted entry" "promoted ->" reportPromoted.Output

    // --as rename into scripts.
    let rename = runScratch fixtureRoot [ "promote"; promoteRoot; "evidence2.txt"; "--destination"; "scripts"; "--as"; "renamed.txt" ]
    assertExit "promote --as to scripts" 0 rename
    assertTrue "renamed copy exists" (File.Exists(Path.Combine(taskDir, "scripts", "renamed.txt")))
    assertEqual "renamed copy byte-verified" evidenceBytes (File.ReadAllBytes(Path.Combine(taskDir, "scripts", "renamed.txt")))

    // Promotion rejection paths.
    let unregistered = runScratch fixtureRoot [ "promote"; promoteRoot; "nope.txt"; "--destination"; "docs" ]
    assertExit "promote unregistered" 1 unregistered
    assertContains "promote unregistered diagnostic" "not a registered scratch entry" unregistered.Output

    let alreadyPromoted = runScratch fixtureRoot [ "promote"; promoteRoot; "evidence.txt"; "--destination"; "docs" ]
    assertExit "promote already promoted" 1 alreadyPromoted
    assertContains "promote already promoted diagnostic" "already promoted" alreadyPromoted.Output

    File.WriteAllText(Path.Combine(taskDir, "docs", "taken.txt"), "occupied")
    let existingDestination = runScratch fixtureRoot [ "promote"; promoteRoot; "evidence.txt"; "--destination"; "docs"; "--as"; "taken.txt" ]
    assertExit "promote existing destination" 1 existingDestination

    let badDestination = runScratch fixtureRoot [ "promote"; promoteRoot; "evidence.txt"; "--destination"; "other" ]
    assertExit "promote bad destination" 2 badDestination
    let missingDestination = runScratch fixtureRoot [ "promote"; promoteRoot; "evidence.txt" ]
    assertExit "promote missing destination" 2 missingDestination
    let danglingAs = runScratch fixtureRoot [ "promote"; promoteRoot; "evidence.txt"; "--destination"; "docs"; "--as" ]
    assertExit "promote --as without value" 2 danglingAs

    for unsafeName in [ ".."; "sub/name" ] do
        let unsafe = runScratch fixtureRoot [ "promote"; promoteRoot; "evidence.txt"; "--destination"; "docs"; "--as"; unsafeName ]
        assertExit "promote unsafe name" 1 unsafe

    // Promotion is bound to the registered identity and digest: a modified or
    // replaced source is never copied, and a vanished source fails closed.
    let bindPromoteRoot = createRoot taskId (uniqueRunId ())
    let modifiedSource = Path.Combine(bindPromoteRoot, "mod.txt")
    let replacedSource = Path.Combine(bindPromoteRoot, "rep.txt")
    let goneSource = Path.Combine(bindPromoteRoot, "gone.txt")
    for path in [ modifiedSource; replacedSource; goneSource ] do
        File.WriteAllText(path, "original")
    runScratch fixtureRoot [ "register"; bindPromoteRoot; "mod.txt"; "rep.txt"; "gone.txt" ]
    |> assertExit "binding promote fixture register" 0

    File.WriteAllText(modifiedSource, "modified in place")
    let modifiedPromote = runScratch fixtureRoot [ "promote"; bindPromoteRoot; "mod.txt"; "--destination"; "docs" ]
    assertExit "promote modified source" 1 modifiedPromote
    assertContains "promote modified source diagnostic" "does not match the manifest" modifiedPromote.Output

    File.Delete replacedSource
    File.WriteAllText(replacedSource, "replaced")
    let replacedPromote = runScratch fixtureRoot [ "promote"; bindPromoteRoot; "rep.txt"; "--destination"; "docs" ]
    assertExit "promote replaced source" 1 replacedPromote
    assertContains "promote replaced source diagnostic" "does not match the manifest" replacedPromote.Output

    File.Delete goneSource
    let gonePromote = runScratch fixtureRoot [ "promote"; bindPromoteRoot; "gone.txt"; "--destination"; "docs" ]
    assertExit "promote missing source" 1 gonePromote
    assertContains "promote missing source diagnostic" "source file does not exist" gonePromote.Output

    // Promotion requires the current task directory in the working directory.
    let noTaskDirRoot = createRoot taskId (uniqueRunId ())
    File.WriteAllText(Path.Combine(noTaskDirRoot, "x.txt"), "x")
    runScratch fixtureRoot [ "register"; noTaskDirRoot; "x.txt" ] |> assertExit "no-taskdir fixture register" 0
    let noTaskDirCwd = Path.Combine(fixtureRoot, "no-task-dir")
    Directory.CreateDirectory noTaskDirCwd |> ignore
    let noTaskDir = runScratch noTaskDirCwd [ "promote"; noTaskDirRoot; "x.txt"; "--destination"; "docs" ]
    assertExit "promote without task directory" 1 noTaskDir
    assertContains "promote without task directory diagnostic" "cannot open directory component" noTaskDir.Output

    printfn "[scratch] mutation rejected after seal"
    // D003: seal is the authorization boundary; register and promote must be
    // rejected after it so the manifest cannot change underneath cleanup.
    let sealedRoot = createRoot taskId (uniqueRunId ())
    let sealedFile = Path.Combine(sealedRoot, "sf.txt")
    File.WriteAllText(sealedFile, "x")
    runScratch fixtureRoot [ "register"; sealedRoot; "sf.txt" ]
    |> assertExit "sealed fixture register" 0
    runScratch fixtureRoot [ "seal"; sealedRoot ] |> assertExit "sealed fixture seal" 0

    let registerAfterSeal = runScratch fixtureRoot [ "register"; sealedRoot; "sf.txt" ]
    assertExit "register after seal" 1 registerAfterSeal
    assertContains "register after seal diagnostic" "registration is rejected" registerAfterSeal.Output

    let lateFile = Path.Combine(sealedRoot, "late.txt")
    File.WriteAllText(lateFile, "late")
    let lateRegister = runScratch fixtureRoot [ "register"; sealedRoot; "late.txt" ]
    assertExit "register new file after seal" 1 lateRegister
    assertContains "register new file after seal diagnostic" "registration is rejected" lateRegister.Output

    let promoteAfterSeal = runScratch fixtureRoot [ "promote"; sealedRoot; "sf.txt"; "--destination"; "docs" ]
    assertExit "promote after seal" 1 promoteAfterSeal
    assertContains "promote after seal diagnostic" "promotion is rejected" promoteAfterSeal.Output

    runScratch fixtureRoot [ "seal"; sealedRoot ] |> assertExit "re-seal after rejected mutations" 0

    printfn "[scratch] eligible clean and closing-step behavior"
    let closeRoot = createRoot taskId (uniqueRunId ())
    let disposable = Path.Combine(closeRoot, "disposable.txt")
    let evidence3 = Path.Combine(closeRoot, "evidence3.txt")
    let unknownFile = Path.Combine(closeRoot, "unknown.txt")
    File.WriteAllText(disposable, "throwaway")
    File.WriteAllText(evidence3, "durable evidence")
    File.WriteAllText(unknownFile, "unknown")
    runScratch fixtureRoot [ "register"; closeRoot; "disposable.txt"; "evidence3.txt" ]
    |> assertExit "close fixture register" 0
    runScratch fixtureRoot [ "promote"; closeRoot; "evidence3.txt"; "--destination"; "docs" ]
    |> assertExit "close fixture promote" 0

    // Clean before seal retains everything.
    let preSeal = runScratch fixtureRoot [ "clean"; closeRoot ]
    assertExit "clean before seal" 0 preSeal
    assertContains "clean before seal diagnostic" "not sealed" preSeal.Output
    assertTrue "pre-seal clean deleted disposable" (File.Exists disposable)

    // Seal is idempotent.
    runScratch fixtureRoot [ "seal"; closeRoot ] |> assertExit "seal" 0
    let sealAgain = runScratch fixtureRoot [ "seal"; closeRoot ]
    assertExit "seal idempotent" 0 sealAgain
    assertContains "seal idempotent diagnostic" "already sealed" sealAgain.Output

    // Eligible clean deletes only the registered disposable entry through its
    // verified handle; promoted and unregistered material is retained.
    let clean = runScratch fixtureRoot [ "clean"; closeRoot ]
    assertExit "eligible clean" 0 clean
    assertTrue "unknown preserved" (File.Exists unknownFile)
    assertTrue "promoted scratch source retained (never auto-deleted)" (File.Exists evidence3)
    assertTrue "promoted durable copy survives" (File.Exists(Path.Combine(taskDir, "docs", "evidence3.txt")))
    assertContains "eligible clean retained report" "promoted ->" clean.Output
    assertContains "eligible clean unknown report" "unregistered" clean.Output
    assertContains "eligible clean deleted count" "deleted: 1" clean.Output
    assertTrue "disposable deleted" (not (File.Exists disposable))

    // Clean is idempotent on an already-clean sealed root.
    let cleanAgain = runScratch fixtureRoot [ "clean"; closeRoot ]
    assertExit "clean idempotent" 0 cleanAgain
    assertContains "clean idempotent deleted count" "deleted: 0" cleanAgain.Output

    // A registered path that became a directory is retained and reported.
    let dirPathRoot = createRoot taskId (uniqueRunId ())
    let filePath = Path.Combine(dirPathRoot, "a.txt")
    File.WriteAllText(filePath, "x")
    runScratch fixtureRoot [ "register"; dirPathRoot; "a.txt" ] |> assertExit "dir-path fixture register" 0
    File.Delete filePath
    Directory.CreateDirectory filePath |> ignore
    File.WriteAllText(Path.Combine(filePath, "inner.txt"), "inner")
    runScratch fixtureRoot [ "seal"; dirPathRoot ] |> assertExit "dir-path fixture seal" 0
    let dirPathClean = runScratch fixtureRoot [ "clean"; dirPathRoot ]
    assertExit "clean with directory at registered path" 0 dirPathClean
    assertTrue "registered path directory retained" (Directory.Exists filePath)
    assertContains "registered path directory reported" "not a file" dirPathClean.Output

    printfn "[scratch] clean retains identity and digest mismatches"
    // Clean deletes only bytes whose stable identity and digest still match the
    // manifest: an in-place edit (digest drift) or a replaced file (identity
    // drift) is retained and reported, while matching siblings are deleted.
    let mismatchRoot = createRoot taskId (uniqueRunId ())
    let editedFile = Path.Combine(mismatchRoot, "edited.txt")
    let replacedFile = Path.Combine(mismatchRoot, "replaced.txt")
    let goodFile = Path.Combine(mismatchRoot, "good.txt")
    for path in [ editedFile; replacedFile; goodFile ] do
        File.WriteAllText(path, "original")
    runScratch fixtureRoot [ "register"; mismatchRoot; "edited.txt"; "replaced.txt"; "good.txt" ]
    |> assertExit "mismatch fixture register" 0

    File.WriteAllText(editedFile, "edited content")
    File.Delete replacedFile
    File.WriteAllText(replacedFile, "replaced content")
    runScratch fixtureRoot [ "seal"; mismatchRoot ] |> assertExit "mismatch fixture seal" 0

    let mismatchClean = runScratch fixtureRoot [ "clean"; mismatchRoot ]
    assertExit "clean with mismatched entries" 0 mismatchClean
    assertContains "clean identity/digest diagnostic" "identity or digest mismatch" mismatchClean.Output
    assertTrue "edited file retained" (File.Exists editedFile)
    assertTrue "replaced file retained" (File.Exists replacedFile)
    assertContains "clean mismatched deleted count" "deleted: 1" mismatchClean.Output
    assertTrue "matching registered file deleted" (not (File.Exists goodFile))

    printfn "[scratch] cleanup failure preserves unknown"
    let failRoot = createRoot taskId (uniqueRunId ())
    let fa = Path.Combine(failRoot, "a.txt")
    let fb = Path.Combine(failRoot, "b.txt")
    let unknown = Path.Combine(failRoot, "unknown.txt")
    File.WriteAllText(fa, "a")
    File.WriteAllText(fb, "b")
    File.WriteAllText(unknown, "u")
    runScratch fixtureRoot [ "register"; failRoot; "a.txt"; "b.txt" ]
    |> assertExit "failure fixture register" 0
    runScratch fixtureRoot [ "seal"; failRoot ] |> assertExit "failure fixture seal" 0

    use locked = new FileStream(fa, FileMode.Open, FileAccess.ReadWrite, FileShare.None)
    let failClean = runScratch fixtureRoot [ "clean"; failRoot ]
    assertExit "locked-file clean must fail" 1 failClean
    assertTrue "locked file preserved" (File.Exists fa)
    assertTrue "sibling registered file preserved" (File.Exists fb)
    assertTrue "unknown file preserved" (File.Exists unknown)
    assertTrue "manifest remains readable" ((readManifest failRoot).GetProperty("sealed").GetBoolean())

    printfn "[scratch] manifest mutation serialization under contention (D005)"
    // Register/promote/seal read-modify-write the manifest under an exclusive
    // Windows open (share mode 0): a concurrent mutation loses the open and
    // fails closed without writing a partial manifest. The manifest must
    // survive intact with every successful registration present exactly once.
    let serializeRoot = createRoot taskId (uniqueRunId ())
    let concurrentFiles = [ "c1.txt"; "c2.txt"; "c3.txt" ]
    for name in concurrentFiles do
        File.WriteAllText(Path.Combine(serializeRoot, name), "concurrent " + name)

    let contentionResults =
        concurrentFiles
        |> List.map (fun name -> async { return name, runScratch fixtureRoot [ "register"; serializeRoot; name ] })
        |> Async.Parallel
        |> Async.RunSynchronously
        |> Array.toList

    let successful = contentionResults |> List.filter (fun (_, result) -> result.ExitCode = 0)
    let failed = contentionResults |> List.filter (fun (_, result) -> result.ExitCode <> 0)
    assertTrue "at least one concurrent mutation succeeds" (not successful.IsEmpty)
    for _, result in failed do
        assertEqual "concurrent mutation failure is fail-closed" 1 result.ExitCode

    let manifestAfterContention = readManifest serializeRoot
    let manifestPathsAfterContention =
        manifestAfterContention.GetProperty("entries").EnumerateArray()
        |> Seq.map entryPath
        |> Seq.toList
    assertTrue "no duplicate entries after contention" (manifestPathsAfterContention = List.distinct manifestPathsAfterContention)
    for name, _ in successful do
        assertTrue $"successful registration '{name}' present after contention" (List.contains name manifestPathsAfterContention)

    // The manifest must still be fully consumable after the contention.
    runScratch fixtureRoot [ "report"; serializeRoot ] |> assertExit "report after contention" 0

    printfn "[scratch] windows contention outcome: %d succeeded, %d failed closed" successful.Length failed.Length

    printfn "[scratch] platform contract static checks"
    // Windows-only scope: this host cannot execute the non-Windows refusal
    // branches, so the fail-closed contract is pinned from the implementation
    // source. Positive checks pin the platform split and the refusal path;
    // negative checks pin that the Unix report-only/descriptor/flock fallback
    // is fully removed and no weaker compatibility path reappears.
    let implementationSource = File.ReadAllText scratchScript
    let assertSource name (fragment: string) =
        if not (implementationSource.Contains fragment) then
            failwithf "%s: TaskScratch.fsx no longer contains %A" name fragment
    let assertSourceAbsent name (fragment: string) =
        if implementationSource.Contains fragment then
            failwithf "%s: TaskScratch.fsx must not contain %A (Windows-only scope, no compatibility fallback)" name fragment
    assertSource "platform detection is windows-or-unsupported" "if OperatingSystem.IsWindows() then Windows"
    assertSource "unsupported platform outcome" "Unsupported"
    assertSource "mutation refusal message" "mutation is refused"
    assertSource "non-Windows fail-closed header" "fails closed on non-Windows"
    assertSource "no compatibility fallback header" "no compatibility fallback is"
    assertSource "windows exclusive-open serialization" "share mode 0 (exclusive)"
    assertSourceAbsent "no unix flock" "flock"
    assertSourceAbsent "no unix advisory lock" "LOCK_EX"
    assertSourceAbsent "no unix lock release" "LOCK_UN"
    assertSourceAbsent "no report-only clean" "report-only"
    assertSourceAbsent "no unix descriptor layer" "unlink"
    assertSourceAbsent "no posix getcwd promotion" "getcwd"

    printfn "[scratch] reparse point rejection"
    let reparseTarget = Path.Combine(fixtureRoot, "reparse-target-dir")
    Directory.CreateDirectory reparseTarget |> ignore
    File.WriteAllText(Path.Combine(reparseTarget, "outside.txt"), "outside")

    let junctionRoot = createRoot taskId (uniqueRunId ())
    let junctionParent = Path.Combine(junctionRoot, "a")
    Directory.CreateDirectory junctionParent |> ignore
    let junctionLink = Path.Combine(junctionParent, "junc")

    if tryCreateDirectoryReparse junctionLink reparseTarget then
        // Registering through a junction ancestor is rejected.
        let throughJunction = runScratch fixtureRoot [ "register"; junctionRoot; "a/junc/outside.txt" ]
        assertExit "register through junction" 1 throughJunction
        assertContains "register through junction diagnostic" "traverses a reparse point" throughJunction.Output

        // Registering the junction itself is rejected (not a file).
        let junctionSelf = runScratch fixtureRoot [ "register"; junctionRoot; "a/junc" ]
        assertExit "register junction itself" 1 junctionSelf

        // Report retains and reports the reparse point.
        let reportReparse = runScratch fixtureRoot [ "report"; junctionRoot ]
        assertExit "report with junction" 0 reportReparse
        assertContains "report reparse diagnostic" "reparse point" reportReparse.Output

        // Clean deletes eligible entries and retains unregistered reparse
        // material.
        let swap = Path.Combine(junctionRoot, "swap.txt")
        File.WriteAllText(swap, "swap")
        runScratch fixtureRoot [ "register"; junctionRoot; "swap.txt" ] |> assertExit "swap register" 0
        runScratch fixtureRoot [ "seal"; junctionRoot ] |> assertExit "junction clean seal" 0
        let cleanReparse = runScratch fixtureRoot [ "clean"; junctionRoot ]
        assertExit "clean with unregistered junction" 0 cleanReparse
        assertContains "clean reparse retained" "reparse point" cleanReparse.Output
        assertTrue "unregistered junction preserved" (Directory.Exists junctionLink)
        assertTrue "junction target file intact" (File.Exists(Path.Combine(reparseTarget, "outside.txt")))
        assertTrue "registered swap deleted" (not (File.Exists swap))

        // A registered file replaced by a junction fails closed and deletes nothing.
        // D002: a reparse point already present at or above a registered path
        // before clean is a deterministic fail-closed condition; the swap that
        // happens between the pre-delete re-check and the delete itself cannot
        // be made atomic here, so no test claims atomic behavior. The re-check
        // only narrows the race window, and every detectable state keeps the
        // fail-closed guarantee (siblings are preserved because validation
        // runs for all entries before any deletion).
        let replaceRoot = createRoot taskId (uniqueRunId ())
        let replaced = Path.Combine(replaceRoot, "swap.txt")
        let replaceSibling = Path.Combine(replaceRoot, "keep.txt")
        File.WriteAllText(replaced, "swap")
        File.WriteAllText(replaceSibling, "keep")
        runScratch fixtureRoot [ "register"; replaceRoot; "swap.txt"; "keep.txt" ]
        |> assertExit "replace fixture register" 0
        File.Delete replaced
        if tryCreateDirectoryReparse replaced reparseTarget then
            runScratch fixtureRoot [ "seal"; replaceRoot ] |> assertExit "replace fixture seal" 0
            let replaceClean = runScratch fixtureRoot [ "clean"; replaceRoot ]
            assertExit "reparse at registered path fails closed" 1 replaceClean
            assertContains "reparse at registered path diagnostic" "reparse point" replaceClean.Output
            assertTrue "reparse link preserved" (Directory.Exists replaced)
            assertTrue "sibling registered file preserved after fail-closed clean" (File.Exists replaceSibling)
            assertTrue "target file intact after failed clean" (File.Exists(Path.Combine(reparseTarget, "outside.txt")))

        // A hand-edited entry traversing a real junction ancestor fails closed
        // and deletes nothing.
        let editRoot = createRoot taskId (uniqueRunId ())
        let realFile = Path.Combine(editRoot, "real.txt")
        File.WriteAllText(realFile, "real")
        runScratch fixtureRoot [ "register"; editRoot; "real.txt" ] |> assertExit "edit fixture register" 0
        let editJunctionParent = Path.Combine(editRoot, "a")
        Directory.CreateDirectory editJunctionParent |> ignore
        if tryCreateDirectoryReparse (Path.Combine(editJunctionParent, "junc")) reparseTarget then
            editManifest editRoot (fun node ->
                (node.["entries"] :?> JsonArray).Add(makeEntry "a/junc/outside.txt" "file"))
            runScratch fixtureRoot [ "seal"; editRoot ] |> assertExit "edit fixture seal" 0
            let editClean = runScratch fixtureRoot [ "clean"; editRoot ]
            assertExit "clean with junction-traversing entry" 1 editClean
            assertContains "clean junction-traversal diagnostic" "traverses a reparse point" editClean.Output
            assertTrue "valid registered file not deleted" (File.Exists realFile)
        else
            printfn "[scratch] SKIP edit-entry junction fixture: OS cannot create directory reparse points"

        // A registered entry whose parent directory is swapped for a junction
        // after registration fails closed at clean: nothing is deleted.
        let ancestorRoot = createRoot taskId (uniqueRunId ())
        let ancestorParent = Path.Combine(ancestorRoot, "dir")
        Directory.CreateDirectory ancestorParent |> ignore
        let nestedFile = Path.Combine(ancestorParent, "nested.txt")
        let siblingFile = Path.Combine(ancestorRoot, "sibling.txt")
        File.WriteAllText(nestedFile, "nested")
        File.WriteAllText(siblingFile, "sibling")
        runScratch fixtureRoot [ "register"; ancestorRoot; "dir/nested.txt"; "sibling.txt" ]
        |> assertExit "ancestor fixture register" 0
        runScratch fixtureRoot [ "seal"; ancestorRoot ] |> assertExit "ancestor fixture seal" 0
        Directory.Delete(ancestorParent, true)
        if tryCreateDirectoryReparse ancestorParent reparseTarget then
            let ancestorClean = runScratch fixtureRoot [ "clean"; ancestorRoot ]
            assertExit "clean with swapped junction ancestor" 1 ancestorClean
            assertContains "clean swapped ancestor diagnostic" "traverses a reparse point" ancestorClean.Output
            assertTrue "sibling preserved after fail-closed clean" (File.Exists siblingFile)
            assertTrue "junction target file intact" (File.Exists(Path.Combine(reparseTarget, "outside.txt")))
        else
            printfn "[scratch] SKIP ancestor-swap junction fixture: OS cannot create directory reparse points"
    else
        printfn "[scratch] SKIP junction fixtures: OS cannot create directory reparse points"

    // A junction planted at the canonical tasks/<TASK-ID> component is an
    // ancestor reparse of every run below it: report fails closed while
    // opening the root anchor, before any manifest content is trusted. The
    // task ID is unique per run so a crashed fixture can never collide with
    // the next run's junction.
    let junctionTaskId =
        let digits =
            Guid.NewGuid().ToString("N")
            |> Seq.filter Char.IsDigit
            |> Seq.truncate 8
            |> Seq.toArray
            |> String
        "JUNC-" + digits
    let junctionTarget = Path.Combine(fixtureRoot, "junction-task-target")
    Directory.CreateDirectory junctionTarget |> ignore
    let junctionRunDir = Path.Combine(junctionTarget, "run-1")
    Directory.CreateDirectory junctionRunDir |> ignore
    let junctionManifest = JsonObject()
    junctionManifest.["version"] <- JsonValue.Create(2 : int)
    junctionManifest.["taskId"] <- JsonValue.Create junctionTaskId
    junctionManifest.["runId"] <- JsonValue.Create "run-1"
    junctionManifest.["root"] <- JsonValue.Create(Path.Combine(tempBase, "tasks", junctionTaskId, "run-1"))
    junctionManifest.["rootId"] <- JsonValue.Create "0:0"
    junctionManifest.["sealed"] <- JsonValue.Create false
    junctionManifest.["entries"] <- JsonArray()
    File.WriteAllText(Path.Combine(junctionRunDir, "manifest.json"), junctionManifest.ToJsonString())
    let junctionTaskDir = Path.Combine(tempBase, "tasks", junctionTaskId)
    if tryCreateDirectoryReparse junctionTaskDir junctionTarget then
        let junctionRoot = Path.Combine(junctionTaskDir, "run-1")
        let junctionReport = runScratch fixtureRoot [ "report"; junctionRoot ]
        assertExit "report through root-level junction" 1 junctionReport
        assertContains "root-level junction diagnostic" "reparse point" junctionReport.Output
        try
            if Directory.Exists junctionTaskDir then Directory.Delete junctionTaskDir
        with _ ->
            try File.Delete junctionTaskDir with _ -> ()
    else
        printfn "[scratch] SKIP root-level junction fixture: OS cannot create directory reparse points"

    // File-symlink fixtures are skipped when the OS cannot create them.
    let linkRoot = createRoot taskId (uniqueRunId ())
    let outsideFile = Path.Combine(fixtureRoot, "outside-file.txt")
    File.WriteAllText(outsideFile, "outside")
    if tryCreateFileReparse (Path.Combine(linkRoot, "lnk.txt")) outsideFile then
        let fileReparse = runScratch fixtureRoot [ "register"; linkRoot; "lnk.txt" ]
        assertExit "register file symlink" 1 fileReparse
        assertContains "register file symlink diagnostic" "must not be a reparse point" fileReparse.Output
    else
        printfn "[scratch] SKIP file-symlink fixture: OS cannot create file reparse points"

    printfn "[scratch] promotion destination reparse rejection"
    // The durable destination is physically anchored below <workdir>/.tasks:
    // a reparse point at the namespace, at the .tasks component, or at the
    // destination file name makes promotion fail closed without copying.
    let promoTaskId = uniqueTaskId ()

    let namespaceWorkdir = Path.Combine(fixtureRoot, "promo-namespace-cwd")
    Directory.CreateDirectory namespaceWorkdir |> ignore
    let promoDocsParent = Path.Combine(namespaceWorkdir, ".tasks", promoTaskId)
    Directory.CreateDirectory promoDocsParent |> ignore
    if tryCreateDirectoryReparse (Path.Combine(promoDocsParent, "docs")) (Path.Combine(fixtureRoot, "promo-target")) then
        let promoRoot = createRoot promoTaskId (uniqueRunId ())
        let promoFile = Path.Combine(promoRoot, "p.txt")
        File.WriteAllText(promoFile, "promote me")
        runScratch fixtureRoot [ "register"; promoRoot; "p.txt" ]
        |> assertExit "destination-reparse fixture register" 0
        let namespacePromote = runScratch namespaceWorkdir [ "promote"; promoRoot; "p.txt"; "--destination"; "docs" ]
        assertExit "promote through reparse namespace" 1 namespacePromote
        assertContains "promote reparse namespace diagnostic" "reparse point" namespacePromote.Output
        assertTrue "promote reparse namespace copied nothing" (not (File.Exists(Path.Combine(promoDocsParent, "docs", "p.txt"))))
        try Directory.Delete(Path.Combine(promoDocsParent, "docs")) with _ -> ()
    else
        printfn "[scratch] SKIP promotion namespace junction fixture: OS cannot create directory reparse points"

    let tasksWorkdir = Path.Combine(fixtureRoot, "promo-tasks-cwd")
    Directory.CreateDirectory tasksWorkdir |> ignore
    let tasksTarget = Path.Combine(fixtureRoot, "promo-tasks-target")
    Directory.CreateDirectory tasksTarget |> ignore
    Directory.CreateDirectory(Path.Combine(tasksTarget, promoTaskId)) |> ignore
    if tryCreateDirectoryReparse (Path.Combine(tasksWorkdir, ".tasks")) tasksTarget then
        let promoRoot2 = createRoot promoTaskId (uniqueRunId ())
        let promoFile2 = Path.Combine(promoRoot2, "p2.txt")
        File.WriteAllText(promoFile2, "promote me too")
        runScratch fixtureRoot [ "register"; promoRoot2; "p2.txt" ]
        |> assertExit "destination-reparse fixture register 2" 0
        let tasksPromote = runScratch tasksWorkdir [ "promote"; promoRoot2; "p2.txt"; "--destination"; "docs" ]
        assertExit "promote through reparse .tasks" 1 tasksPromote
        assertContains "promote reparse .tasks diagnostic" "reparse point" tasksPromote.Output
        assertTrue "promote reparse .tasks copied nothing" (not (File.Exists(Path.Combine(tasksTarget, promoTaskId, "docs", "p2.txt"))))
        try Directory.Delete(Path.Combine(tasksWorkdir, ".tasks")) with _ -> ()
    else
        printfn "[scratch] SKIP promotion .tasks junction fixture: OS cannot create directory reparse points"

    let nameWorkdir = Path.Combine(fixtureRoot, "promo-name-cwd")
    Directory.CreateDirectory(Path.Combine(nameWorkdir, ".tasks", promoTaskId, "docs")) |> ignore
    let promoRoot3 = createRoot promoTaskId (uniqueRunId ())
    let promoFile3 = Path.Combine(promoRoot3, "p3.txt")
    File.WriteAllText(promoFile3, "third")
    runScratch fixtureRoot [ "register"; promoRoot3; "p3.txt" ]
    |> assertExit "destination-name fixture register" 0
    let destinationName = Path.Combine(nameWorkdir, ".tasks", promoTaskId, "docs", "p3.txt")
    if tryCreateDirectoryReparse destinationName (Path.Combine(fixtureRoot, "promo-name-target")) then
        let namePromote = runScratch nameWorkdir [ "promote"; promoRoot3; "p3.txt"; "--destination"; "docs" ]
        assertExit "promote onto reparse destination name" 1 namePromote
        // The create is never followed: the name is refused as a collision or
        // (because a junction is a directory) as a directory, never written
        // through the reparse point.
        assertContains "promote reparse destination name diagnostic" "cannot create destination file" namePromote.Output
        assertTrue "junction at destination name intact" (Directory.Exists destinationName)
        try Directory.Delete destinationName with _ -> ()
    else
        printfn "[scratch] SKIP promotion destination-name junction fixture: OS cannot create directory reparse points"

    printfn "[scratch] promotion CWD reparse rejection (S001)"
    // The current working directory is untrusted: promotion re-derives the
    // destination capability from the trusted filesystem root by component-wise
    // no-follow traversal, so a reparse point anywhere in the CWD path chain
    // fails closed instead of copying to the physical (external) location.
    // (S001 was resolved by the approved Windows-only scope waiver: no POSIX
    // getcwd path exists that could resolve the symlink, so no platform
    // performs the external promotion.)
    let cwdTaskId = uniqueTaskId ()
    let cwdTarget = Path.Combine(fixtureRoot, $"cwd-junc-target-{Guid.NewGuid():N}")
    Directory.CreateDirectory(Path.Combine(cwdTarget, "workdir")) |> ignore

    // CWD below a reparse ancestor: the spelled path would physically resolve
    // outside the fixture tree, so no copy may appear at the junction target.
    let cwdAncestorLink = Path.Combine(fixtureRoot, $"cwd-junc-ancestor-{Guid.NewGuid():N}")
    if tryCreateDirectoryReparse cwdAncestorLink cwdTarget then
        let ancestorCwd = Path.Combine(cwdAncestorLink, "workdir")
        let ancestorRoot = createRoot cwdTaskId (uniqueRunId ())
        File.WriteAllText(Path.Combine(ancestorRoot, "a.txt"), "promote me")
        runScratch fixtureRoot [ "register"; ancestorRoot; "a.txt" ]
        |> assertExit "cwd-ancestor fixture register" 0
        let ancestorPromote = runScratch ancestorCwd [ "promote"; ancestorRoot; "a.txt"; "--destination"; "docs" ]
        // Windows preserves the junction spelling in the child CWD, so the
        // no-follow walk reaches the reparse component and fails closed.
        assertExit "promote through CWD-ancestor reparse fails closed" 1 ancestorPromote
        assertContains "CWD-ancestor reparse diagnostic" "reparse point" ancestorPromote.Output
        assertTrue
            "CWD-ancestor reparse copied nothing external"
            (not (File.Exists(Path.Combine(cwdTarget, "workdir", ".tasks", cwdTaskId, "docs", "a.txt"))))
        try Directory.Delete cwdAncestorLink with _ -> ()
    else
        printfn "[scratch] SKIP CWD-ancestor reparse fixture: OS cannot create directory reparse points"

    // CWD itself is a reparse point (the final spelled component).
    let cwdSelfLink = Path.Combine(fixtureRoot, $"cwd-junc-self-{Guid.NewGuid():N}")
    if tryCreateDirectoryReparse cwdSelfLink cwdTarget then
        let selfRoot = createRoot cwdTaskId (uniqueRunId ())
        File.WriteAllText(Path.Combine(selfRoot, "s.txt"), "self")
        runScratch fixtureRoot [ "register"; selfRoot; "s.txt" ]
        |> assertExit "cwd-self fixture register" 0
        let selfPromote = runScratch cwdSelfLink [ "promote"; selfRoot; "s.txt"; "--destination"; "docs" ]
        assertExit "promote with reparse CWD fails closed" 1 selfPromote
        assertContains "reparse CWD diagnostic" "reparse point" selfPromote.Output
        assertTrue
            "reparse CWD copied nothing external"
            (not (File.Exists(Path.Combine(cwdTarget, ".tasks", cwdTaskId, "docs", "s.txt"))))
        try Directory.Delete cwdSelfLink with _ -> ()
    else
        printfn "[scratch] SKIP CWD-self reparse fixture: OS cannot create directory reparse points"

    printfn "[scratch] unsealed scratch is preserved"
    let unsealedRoot = createRoot taskId (uniqueRunId ())
    let u1 = Path.Combine(unsealedRoot, "u1.txt")
    File.WriteAllText(u1, "u1")
    runScratch fixtureRoot [ "register"; unsealedRoot; "u1.txt" ] |> assertExit "unsealed fixture register" 0
    File.WriteAllText(Path.Combine(unsealedRoot, "unregistered.txt"), "u")
    let unsealedClean = runScratch fixtureRoot [ "clean"; unsealedRoot ]
    assertExit "clean unsealed" 0 unsealedClean
    assertContains "clean unsealed diagnostic" "not sealed" unsealedClean.Output
    assertTrue "unsealed registered file preserved" (File.Exists u1)
    assertTrue "unsealed unregistered file preserved" (File.Exists(Path.Combine(unsealedRoot, "unregistered.txt")))

    printfn "[scratch] CLI usage contract"
    let noArgs = runScratch fixtureRoot []
    assertExit "no arguments" 2 noArgs
    let unknownCommand = runScratch fixtureRoot [ "nope" ]
    assertExit "unknown command" 2 unknownCommand
    let help = runScratch fixtureRoot [ "help" ]
    assertExit "help command" 0 help
    assertContains "help shows lifecycle surface" "create <TASK-ID>" help.Output

    let registerNoPaths = runScratch fixtureRoot [ "register"; taskLevel ]
    assertExit "register without paths" 2 registerNoPaths

    let flagRoot = createRoot taskId (uniqueRunId ())
    for command in [ [ "report"; flagRoot; "--force" ]; [ "seal"; flagRoot; "--force" ]; [ "clean"; flagRoot; "--force" ] ] do
        let flagged = runScratch fixtureRoot command
        assertExit "option-less command unknown flag" 2 flagged
        assertContains "unknown flag diagnostic" "unsupported option" flagged.Output

    printfn "OK task scratch lifecycle tests"
finally
    for root in createdRoots |> Seq.distinct do
        safeDeleteRoot root
    for dir in taskDirs |> Seq.distinct do
        try
            if Directory.Exists dir then Directory.Delete dir
        with _ ->
            ()
    if Directory.Exists fixtureRoot then
        Directory.Delete(fixtureRoot, true)
