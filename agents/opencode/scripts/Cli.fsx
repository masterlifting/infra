namespace Common

module Args =
    let ofFsi (argv: string[]) = argv |> Array.skip 1 |> Array.toList

    let get (key: string) (args: string list) =
        args
        |> List.tryFindIndex (fun a -> a = key)
        |> Option.bind (fun i -> if i + 1 < args.Length then Some args.[i + 1] else None)

    let getOrDefault (key: string) (defaultValue: string) (args: string list) =
        get key args |> Option.defaultValue defaultValue

    let getList (key: string) (args: string list) =
        get key args
        |> Option.map (fun s -> s.Split ',' |> Array.map (fun t -> t.Trim()) |> Array.toList)
        |> Option.defaultValue []

    let has (key: string) (args: string list) = args |> List.exists (fun a -> a = key)

module Shell =
    open System
    open System.Diagnostics

    [<Literal>]
    let private DefaultTimeoutMs = 600000

    let private shellArgs (command: string) =
        if OperatingSystem.IsWindows() then
            "cmd.exe", $"/c {command}"
        else
            "/bin/sh", $"""-c "{command}" """

    let private exec (timeoutMs: int) (psi: ProcessStartInfo) =
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true
        psi.UseShellExecute <- false
        psi.CreateNoWindow <- true

        use proc = new Process()
        proc.StartInfo <- psi

        if not (proc.Start()) then
            Error $"failed to start process: {psi.FileName}"
        else
            let stdoutTask = proc.StandardOutput.ReadToEndAsync()
            let stderrTask = proc.StandardError.ReadToEndAsync()

            if not (proc.WaitForExit timeoutMs) then
                try
                    proc.Kill(entireProcessTree = true)
                with _ ->
                    ()

                Error $"timeout after {timeoutMs}ms: {psi.FileName} {psi.Arguments}"
            else
                let stdout = stdoutTask.GetAwaiter().GetResult()
                let stderr = stderrTask.GetAwaiter().GetResult()

                match proc.ExitCode with
                | 0 -> Ok(stdout.Trim())
                | code ->
                    let msg =
                        if stderr.Trim() <> "" then stderr.Trim()
                        else $"exit {code}"

                    Error msg

    let run (command: string) =
        let shell, args = shellArgs command
        ProcessStartInfo(shell, args) |> exec DefaultTimeoutMs

    let runWithTimeout timeoutMs (command: string) =
        let shell, args = shellArgs command
        ProcessStartInfo(shell, args) |> exec timeoutMs

    let runInDir (dir: string) (command: string) =
        let shell, args = shellArgs command
        ProcessStartInfo(shell, args, WorkingDirectory = dir) |> exec DefaultTimeoutMs

    let runInDirWithTimeout timeoutMs (dir: string) (command: string) =
        let shell, args = shellArgs command
        ProcessStartInfo(shell, args, WorkingDirectory = dir) |> exec timeoutMs
