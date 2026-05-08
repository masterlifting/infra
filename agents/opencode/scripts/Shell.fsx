(*
    OS-aware shell command execution for F# scripts.

    Usage:
      #load "Shell.fsx"

      Prelude.Shell.run "some command"           -> Result<string, string>
      Prelude.Shell.runInDir "/path" "command"   -> Result<string, string>
*)

namespace Prelude

module Shell =
    open System
    open System.Diagnostics

    let private shellArgs (command: string) =
        if OperatingSystem.IsWindows() then
            "cmd.exe", $"/c {command}"
        else
            "/bin/sh", $"""-c "{command}" """

    let private exec (psi: ProcessStartInfo) =
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true
        psi.UseShellExecute <- false
        psi.CreateNoWindow <- true

        use proc = Process.Start psi
        let stdout = proc.StandardOutput.ReadToEnd()
        let stderr = proc.StandardError.ReadToEnd()
        proc.WaitForExit()

        match proc.ExitCode with
        | 0 -> Ok(stdout.Trim())
        | code ->
            let msg =
                if stderr.Trim() <> "" then stderr.Trim()
                else $"exit {code}"

            Error msg

    let run (command: string) =
        let shell, args = shellArgs command
        ProcessStartInfo(shell, args) |> exec

    let runInDir (dir: string) (command: string) =
        let shell, args = shellArgs command
        ProcessStartInfo(shell, args, WorkingDirectory = dir) |> exec
