open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Text.Json
open System.Text.RegularExpressions

type Severity =
    | Error
    | Warning

type Finding =
    { Severity: Severity
      Code: string
      Path: string
      Message: string }

let args = fsi.CommandLineArgs |> Array.skip 1
let selfTest = args |> Array.contains "--self-test"

let defaultRoot =
    Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "../../.."))

let root =
    args
    |> Array.tryFindIndex ((=) "--root")
    |> Option.bind (fun index -> args |> Array.tryItem (index + 1))
    |> Option.defaultValue defaultRoot
    |> Path.GetFullPath

let findings = ResizeArray<Finding>()

let relativePath path =
    Path.GetRelativePath(root, path).Replace("\\", "/")

let add severity code path message =
    findings.Add
        { Severity = severity
          Code = code
          Path = path
          Message = message }

let error code path message = add Error code path message
let warning code path message = add Warning code path message

let recursiveFiles directory pattern =
    let options = EnumerationOptions()
    options.RecurseSubdirectories <- true
    options.IgnoreInaccessible <- true
    options.AttributesToSkip <- FileAttributes.ReparsePoint
    Directory.EnumerateFiles(directory, pattern, options)

let requiredPaths =
    [ "AGENTS.md"
      "README.md"
      "package.json"
      "opencode.json"
      "agents"
      "commands"
      "lib"
      "mcp"
      "plugins"
      "rules"
      "scripts"
      "skills" ]

if not selfTest then
    for path in requiredPaths do
        let fullPath = Path.Combine(root, path)
        if not (File.Exists fullPath || Directory.Exists fullPath) then
            error "required-path" path "Required infrastructure path does not exist"

let tryProperty (name: string) (element: JsonElement) =
    let mutable value = Unchecked.defaultof<JsonElement>
    if element.TryGetProperty(name, &value) then Some value else None

let commandArguments (command: string) =
    Regex.Matches(command, "\"(?<quoted>[^\"]+)\"|(?<plain>[^\\s]+)")
    |> Seq.cast<Match>
    |> Seq.map (fun matched ->
        if matched.Groups.["quoted"].Success then matched.Groups.["quoted"].Value
        else matched.Groups.["plain"].Value)
    |> Seq.toList

let commandTargetSpecs arguments =
    let at index = arguments |> List.tryItem index
    let optionValue option =
        arguments
        |> List.tryFindIndex ((=) option)
        |> Option.bind (fun index -> at (index + 1))

    let workingDirectory = optionValue "--directory"
    let directTargets =
        arguments
        |> List.filter (fun argument ->
            [ ".fsx"; ".mjs"; ".js"; ".py"; ".toml"; ".exe" ]
            |> List.exists (fun extension -> argument.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))

    let executableTarget =
        arguments
        |> List.tryHead
        |> Option.filter Path.IsPathRooted
        |> Option.toList

    executableTarget @ directTargets
    |> List.map (fun target ->
        match workingDirectory with
        | Some directory when not (Path.IsPathRooted target) && target.EndsWith(".py", StringComparison.OrdinalIgnoreCase) ->
            Path.Combine(directory, target)
        | _ -> target)
    |> List.distinct

let isWithinRoot candidateRoot candidate =
    let fullRoot = Path.GetFullPath(candidateRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
    let fullCandidate = Path.GetFullPath(candidate)
    fullCandidate.StartsWith(fullRoot + string Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
    || fullCandidate.Equals(fullRoot, StringComparison.OrdinalIgnoreCase)

let checkCommandTargets sourcePath command =
    commandArguments command
    |> commandTargetSpecs
    |> List.iter (fun target ->
        let fullTarget =
            if Path.IsPathRooted target then Path.GetFullPath target
            else Path.GetFullPath(Path.Combine(root, target))

        if isWithinRoot root fullTarget then
            if not (File.Exists fullTarget || Directory.Exists fullTarget) then
                error "command-target" sourcePath $"In-root command target '{relativePath fullTarget}' does not exist"
        elif Path.IsPathRooted target && not (File.Exists fullTarget || Directory.Exists fullTarget) then
            warning "external-command-target" sourcePath "An external absolute command target does not exist")

let missingInRootTargets candidateRoot exists arguments =
    commandTargetSpecs arguments
    |> List.filter (fun target ->
        let fullTarget =
            if Path.IsPathRooted target then Path.GetFullPath target
            else Path.GetFullPath(Path.Combine(candidateRoot, target))
        isWithinRoot candidateRoot fullTarget && not (exists fullTarget))

let scriptHas (command: string) (fragment: string) = command.IndexOf(fragment, StringComparison.Ordinal) >= 0

let testEntryPointPath = Path.Combine(root, "skills", "audit", "scripts", "TestInfrastructure.fsx")
let testEntryPointRelative = relativePath testEntryPointPath

// Fixed step commands are triple-quoted F# string literals in the entry
// point; extract them verbatim to verify the canonical targets.
let testCommandRegex = Regex("\"\"\"(?<command>.*?)\"\"\"", RegexOptions.Singleline)

let entryPointCommands () =
    if not (File.Exists testEntryPointPath) then
        []
    else
        File.ReadAllText testEntryPointPath
        |> fun content ->
            testCommandRegex.Matches content
            |> Seq.cast<Match>
            |> Seq.map (fun matched -> matched.Groups.["command"].Value)
            |> Seq.toList

let validateTestEntryPoint () =
    if not (File.Exists testEntryPointPath) then
        error "test-entry-point" testEntryPointRelative "F# test entry point does not exist"
    else
        let commands = entryPointCommands ()
        let commandHas (fragment: string) = commands |> List.exists (fun command -> scriptHas command fragment)

        for command in commands do
            checkCommandTargets testEntryPointRelative command

        let validationCommands =
            commands
            |> List.filter (fun command -> scriptHas command "ValidateInfrastructure.fsx")

        if validationCommands.Length < 2 then
            error "test-entry-point" testEntryPointRelative "Entry point must run infrastructure validation self-test and live validation"
        elif not (scriptHas validationCommands.Head "--self-test") then
            error "test-entry-point" testEntryPointRelative "Infrastructure validation self-test must precede live validation"

        let requiredTargets =
            [ "dotnet fsi skills/task/scripts/TaskMdTests.fsx"
              "dotnet fsi skills/task/scripts/TaskWorkflowTests.fsx"
              "dotnet fsi skills/task/scripts/TaskScratchTests.fsx"
              "node lib/task-progress-core.test.mjs"
              "node lib/destructive-patterns.test.mjs"
              "node --check plugins/block-destructive.js"
              "node --check plugins/compaction-context.js"
              "node --check plugins/task-progress.js"
              "cargo test -q --manifest-path mcp/firefox/Cargo.toml"
              "uv --directory mcp/telegram run python -m pytest -q" ]

        for target in requiredTargets do
            if not (commandHas target) then
                error "test-entry-point" testEntryPointRelative $"Entry point is missing fixed target '{target}'"

        let pluginsRoot = Path.Combine(root, "plugins")
        if Directory.Exists pluginsRoot then
            for plugin in Directory.EnumerateFiles(pluginsRoot, "*.js", SearchOption.TopDirectoryOnly) do
                let target = relativePath plugin
                if not (commandHas $"node --check {target}") then
                    error "test-entry-point" testEntryPointRelative $"Entry point must check '{target}' syntax with node --check"

        if not (Directory.Exists(Path.Combine(root, "mcp", "telegram"))) then
            error "test-entry-point" testEntryPointRelative "Entry point pytest target directory mcp/telegram does not exist"

let envReference = Regex(@"^\{env:[A-Za-z_][A-Za-z0-9_]*\}$")

let validateEnvironment sourcePath (environment: JsonElement) =
    if environment.ValueKind = JsonValueKind.Object then
        for variable in environment.EnumerateObject() do
            if variable.Value.ValueKind <> JsonValueKind.String || not (envReference.IsMatch(variable.Value.GetString())) then
                error "mcp-env-reference" sourcePath $"MCP environment variable '{variable.Name}' must use a {{env:...}} reference"

let permissionAction config key expected =
    tryProperty "permission" config
    |> Option.bind (tryProperty key)
    |> Option.bind (fun value ->
        if value.ValueKind = JsonValueKind.String then Some(value.GetString())
        else None)
    |> Option.exists ((=) expected)

let permissionPatternAction config key pattern expected =
    tryProperty "permission" config
    |> Option.bind (tryProperty key)
    |> Option.bind (tryProperty pattern)
    |> Option.filter (fun value -> value.ValueKind = JsonValueKind.String)
    |> Option.exists (fun value -> value.GetString() = expected)

let officeDocumentsFsiPattern = "dotnet fsi \"C:/Users/andre/.config/opencode/skills/documents/scripts/*"
let auditValidationFsiPattern = "dotnet fsi \"C:/Users/andre/.config/opencode/skills/audit/scripts/ValidateInfrastructure.fsx*"
let auditTestFsiPattern = "dotnet fsi \"C:/Users/andre/.config/opencode/skills/audit/scripts/TestInfrastructure.fsx*"

let requiredReadDenies =
    [ "**/.env"
      "**/auth.json"
      "**/credentials.json"
      "**/secrets.json"
      "**/token.json"
      "**/id_rsa"
      "**/Mozilla/Firefox/Profiles/**/logins.json"
      "**/Mozilla/Firefox/Profiles/**/key4.db"
      "**/Mozilla/Firefox/Profiles/**/cookies.sqlite"
      "**/Google/Chrome/User Data/**/Login Data"
      "**/Google/Chrome/User Data/**/Cookies"
      "**/Microsoft/Edge/User Data/**/Login Data"
      "**/Microsoft/Edge/User Data/**/Cookies"
      "**/BraveSoftware/Brave-Browser/User Data/**/Login Data"
      "**/BraveSoftware/Brave-Browser/User Data/**/Cookies"
      "**/Opera Software/Opera Stable/**/Login Data"
      "**/Opera Software/Opera Stable/**/Cookies"
      "**/Vivaldi/User Data/**/Login Data"
      "**/Vivaldi/User Data/**/Cookies" ]

let validatePermissionMarkers config =
    for marker in requiredReadDenies do
        if not (permissionPatternAction config "read" marker "deny") then
            error "secret-read-marker" "opencode.json" $"Missing deny marker '{marker}'"

    for tool in [ "telegram_*"; "github_*"; "firefox_*" ] do
        if not (permissionAction config tool "ask") then
            error "mcp-permission" "opencode.json" $"MCP permission '{tool}' must be ask"

    if not (permissionPatternAction config "bash" officeDocumentsFsiPattern "ask") then
        error "office-fsi-permission" "opencode.json" "OfficeDocuments dotnet-fsi permission must be ask"

    if not (permissionPatternAction config "bash" auditValidationFsiPattern "allow") then
        error "audit-validation-permission" "opencode.json" "ValidateInfrastructure must be allowed"

    if not (permissionPatternAction config "bash" auditTestFsiPattern "ask") then
        error "audit-test-permission" "opencode.json" "TestInfrastructure must require confirmation"

    for tool in [ "firefox_read"; "firefox_find"; "firefox_close" ] do
        if not (permissionAction config tool "allow") then
            error "mcp-permission" "opencode.json" $"MCP permission '{tool}' must be allow"

let sourceFilesWithoutBuildArtifacts directory =
    let excluded = Set.ofList [ "target"; "build"; "cache"; ".venv"; "node_modules"; "tests" ]
    let rec collect current =
        seq {
            for file in Directory.EnumerateFiles(current) do
                let extension = Path.GetExtension(file).ToLowerInvariant()
                if extension = ".py" || extension = ".rs" then yield file

            for child in Directory.EnumerateDirectories(current) do
                if not (excluded.Contains(Path.GetFileName(child).ToLowerInvariant())) then
                    yield! collect child
        }
    if Directory.Exists directory then collect directory else Seq.empty

let validateMcpInventory config =
    let configuredPrefixes =
        tryProperty "permission" config
        |> Option.map (fun permissions ->
            permissions.EnumerateObject()
            |> Seq.choose (fun property ->
                let matched = Regex.Match(property.Name, @"^(?<prefix>[a-z]+)_\*$")
                if matched.Success then Some matched.Groups.["prefix"].Value else None)
            |> Set.ofSeq)
        |> Option.defaultValue Set.empty

    let mcpRoot = Path.Combine(root, "mcp")
    let exposedPrefixes =
        sourceFilesWithoutBuildArtifacts mcpRoot
        |> Seq.filter (fun file -> Regex.IsMatch(File.ReadAllText file, @"(?m)^\s*@\w+\.tool\b"))
        |> Seq.map (fun file ->
            Path.GetRelativePath(mcpRoot, file).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).[0].ToLowerInvariant())
        |> Set.ofSeq

    for prefix in Set.difference exposedPrefixes configuredPrefixes do
        warning "mcp-inventory-drift" "mcp" $"Heuristic tool inventory found unclassified '{prefix}_*' definitions"

let validateMcpConfiguration config =
    validatePermissionMarkers config
    validateMcpInventory config

    match tryProperty "mcp" config with
    | Some servers when servers.ValueKind = JsonValueKind.Object ->
        for server in servers.EnumerateObject() do
            let path = "opencode.json"
            match tryProperty "command" server.Value with
            | Some command when command.ValueKind = JsonValueKind.Array ->
                let arguments = command.EnumerateArray() |> Seq.choose (fun value -> if value.ValueKind = JsonValueKind.String then Some(value.GetString()) else None) |> Seq.toList
                checkCommandTargets path (String.concat " " arguments)
            | _ -> error "mcp-command" path $"MCP server '{server.Name}' has no local command array"

            tryProperty "environment" server.Value |> Option.iter (validateEnvironment path)
    | _ -> error "mcp-config" "opencode.json" "Missing MCP definitions"

let checkPluginSyntax (pluginPath: string) (exitCode: int) (output: string) =
    if exitCode <> 0 then Some $"{relativePath pluginPath}: {output.Trim()}" else None

let validatePlugins () =
    let pluginsRoot = Path.Combine(root, "plugins")
    if Directory.Exists pluginsRoot then
        for plugin in Directory.EnumerateFiles(pluginsRoot, "*.js", SearchOption.TopDirectoryOnly) do
            let startInfo = ProcessStartInfo("node")
            startInfo.ArgumentList.Add "--check"
            startInfo.ArgumentList.Add plugin
            startInfo.UseShellExecute <- false
            startInfo.RedirectStandardError <- true
            use pluginProcess = Process.Start startInfo
            let output = pluginProcess.StandardError.ReadToEnd()
            pluginProcess.WaitForExit()
            match checkPluginSyntax plugin pluginProcess.ExitCode output with
            | Some message -> error "plugin-syntax" (relativePath plugin) message
            | None -> ()

let prohibitedAgentNames =
    Set.ofList
        [ "audit-session"
          "coordinator"
          "gaps-clarifier"
          "simplifier"
          "review-arbiter"
          "audit"
          "audit-infra"
          "multimodal-lens"
          "architect-1"
          "architect-2"
          "reviewer-1"
          "reviewer-2"
          "reviewer-3"
          "sql-engineer"
          "sql-reviewer" ]

let isProhibitedAgentName (agentName: string) = prohibitedAgentNames.Contains(agentName.ToLowerInvariant())

// Capability is the quality contract a role requires; the concrete model ID is
// an explicitly approved serving channel. DeepSeek Flash/Pro permit the direct
// and OpenCode Go channels; Grok 4.5 is currently direct xAI only; OpenAI
// identities are exact (each has a single allowed ID).
type Capability =
    | DeepSeekFlash
    | DeepSeekPro
    | Grok45
    | OpenAiTerra
    | OpenAiLuna
    | OpenAiSol

let allCapabilities =
    Set.ofList [ DeepSeekFlash; DeepSeekPro; Grok45; OpenAiTerra; OpenAiLuna; OpenAiSol ]

let allowedIds =
    function
    | DeepSeekFlash -> [ "deepseek/deepseek-v4-flash"; "opencode-go/deepseek-v4-flash" ]
    | DeepSeekPro -> [ "deepseek/deepseek-v4-pro"; "opencode-go/deepseek-v4-pro" ]
    | Grok45 -> [ "xai/grok-4.6" ]
    | OpenAiTerra -> [ "openai/gpt-5.6-terra" ]
    | OpenAiLuna -> [ "openai/gpt-5.6-luna" ]
    | OpenAiSol -> [ "openai/gpt-5.6-sol" ]

let capabilityName =
    function
    | DeepSeekFlash -> "DeepSeekFlash"
    | DeepSeekPro -> "DeepSeekPro"
    | Grok45 -> "Grok45"
    | OpenAiTerra -> "openai/gpt-5.6-terra"
    | OpenAiLuna -> "openai/gpt-5.6-luna"
    | OpenAiSol -> "openai/gpt-5.6-sol"

let capabilityOfModel =
    function
    | "deepseek/deepseek-v4-flash"
    | "opencode-go/deepseek-v4-flash" -> Some DeepSeekFlash
    | "deepseek/deepseek-v4-pro"
    | "opencode-go/deepseek-v4-pro" -> Some DeepSeekPro
    | "xai/grok-4.6" -> Some Grok45
    | "openai/gpt-5.6-terra" -> Some OpenAiTerra
    | "openai/gpt-5.6-luna" -> Some OpenAiLuna
    | "openai/gpt-5.6-sol" -> Some OpenAiSol
    | _ -> None

// Every recognized concrete ID, used where a verified production ID is
// required (e.g. the global model field).
let productionModels =
    allCapabilities
    |> Seq.collect allowedIds
    |> Set.ofSeq

let productionVariants = Set.ofList [ "low"; "medium"; "high" ]

let languageTeams = [ "dotnet/csharp"; "dotnet/fsharp"; "rust" ]

let languageRoles =
    [ "architect"
      "challenger"
      "engineer"
      "tester"
      "reviewer"
      "guardian"
      "validator" ]

let languageRoleAssignment role =
    match role with
    | "architect" -> Some(OpenAiSol, "high")
    | "challenger" -> Some(Grok45, "high")
    | "engineer" -> Some(DeepSeekPro, "high")
    | "tester" -> Some(DeepSeekFlash, "high")
    | "reviewer" -> Some(OpenAiLuna, "high")
    | "guardian" -> Some(Grok45, "high")
    | "validator" -> Some(DeepSeekFlash, "high")
    | _ -> None

let sharedAgentAssignments =
    [ "agents/build.md", OpenAiTerra, "medium"
      "agents/auditor.md", OpenAiLuna, "medium"
      "agents/vision.md", OpenAiTerra, "medium"
      "agents/explorer.md", DeepSeekFlash, "medium"
      "agents/executor.md", DeepSeekFlash, "high"
      "agents/software/database/engineer.md", DeepSeekPro, "high"
      "agents/software/database/reviewer.md", OpenAiLuna, "high"
      "agents/software/devops/engineer.md", DeepSeekPro, "high"
      "agents/software/devops/reviewer.md", OpenAiLuna, "high"
      "agents/software/security/reviewer.md", OpenAiTerra, "high"
      "agents/software/performance/reviewer.md", OpenAiTerra, "high" ]

let grokDiversityRoles = Set.ofList [ "challenger"; "guardian" ]
let deepseekWorkerRoles = Set.ofList [ "explorer"; "executor"; "engineer"; "tester"; "validator" ]
let grokDiversityModel = "xai/grok-4.6"

let expectedAgentAssignments =
    [ for path, capability, variant in sharedAgentAssignments do
          yield path, capability, variant

      for team in languageTeams do
          for role in languageRoles do
              match languageRoleAssignment role with
              | Some(capability, variant) -> yield $"agents/software/{team}/{role}.md", capability, variant
              | None -> () ]

let roleUsesAssignedChannel (role: string) (model: string) =
    match capabilityOfModel model with
    | None -> false
    | Some capability ->
        if grokDiversityRoles.Contains role then capability = Grok45
        elif deepseekWorkerRoles.Contains role then
            capability = DeepSeekFlash || capability = DeepSeekPro
        else true

type RoutingAssignment =
    { Design: string list
      Implementation: string list
      Tests: string list
      Discovery: string list }

let emptyRouting =
    { Design = []
      Implementation = []
      Tests = []
      Discovery = [] }

// Exact review-profile reviewer sets per the frozen INFRA-006 routing contract.
let routineDiscoveryReviewers = [ "reviewer" ]
let contractDiscoveryReviewers = [ "reviewer"; "validator" ]
let architectureDiscoveryReviewers = [ "reviewer"; "guardian" ]
let combinedDiscoveryReviewers = [ "reviewer"; "guardian"; "validator" ]

let requiredRoutingScenarios =
    [ "routine"
      "contract"
      "architecture"
      "combined"
      "database-only"
      "app+database"
      "devops-only"
      "app+devops"
      "security-sensitive"
      "performance-sensitive"
      "unsupported-language" ]

let discoveryReviewProfiles = [ "routine"; "contract"; "architecture"; "combined" ]

let routingFor =
    function
    | "routine" ->
        { Design = []
          Implementation = [ "engineer" ]
          Tests = [ "tester" ]
          Discovery = routineDiscoveryReviewers }
    | "contract" ->
        { Design = []
          Implementation = [ "engineer" ]
          Tests = [ "tester" ]
          Discovery = contractDiscoveryReviewers }
    | "architecture" ->
        { Design = [ "architect"; "challenger" ]
          Implementation = [ "engineer" ]
          Tests = [ "tester" ]
          Discovery = architectureDiscoveryReviewers }
    | "combined" ->
        { Design = [ "architect"; "challenger" ]
          Implementation = [ "engineer" ]
          Tests = [ "tester" ]
          Discovery = combinedDiscoveryReviewers }
    | "database-only" ->
        { Design = [ "database/reviewer" ]
          Implementation = [ "database/engineer" ]
          Tests = [ "database/engineer" ]
          Discovery = [ "database/reviewer" ] }
    | "app+database" ->
        { Design = [ "database/reviewer" ]
          Implementation = [ "engineer"; "database/engineer" ]
          Tests = [ "tester" ]
          Discovery = [ "reviewer"; "database/reviewer" ] }
    | "devops-only" ->
        { Design = [ "devops/engineer" ]
          Implementation = [ "devops/engineer" ]
          Tests = [ "devops/engineer" ]
          Discovery = [ "devops/reviewer" ] }
    | "app+devops" ->
        { Design = [ "devops/engineer" ]
          Implementation = [ "engineer"; "devops/engineer" ]
          Tests = [ "tester" ]
          Discovery = [ "reviewer"; "devops/reviewer" ] }
    | "security-sensitive" ->
        { Design = [ "security/reviewer" ]
          Implementation = [ "engineer" ]
          Tests = [ "tester" ]
          Discovery = [ "reviewer"; "security/reviewer" ] }
    | "performance-sensitive" ->
        { Design = [ "performance/reviewer" ]
          Implementation = [ "engineer" ]
          Tests = [ "tester" ]
          Discovery = [ "reviewer"; "performance/reviewer" ] }
    | "unsupported-language" ->
        { Design = [ "general" ]
          Implementation = [ "executor" ]
          Tests = [ "executor" ]
          Discovery = [ "general" ] }
    | _ -> emptyRouting

let routedAgentFiles (role: string) =
    if role = "general" then
        []
    elif role = "executor" then
        [ "agents/executor.md" ]
    elif role.Contains "/" then
        [ $"agents/software/{role}.md" ]
    elif List.contains role languageRoles then
        [ for team in languageTeams -> $"agents/software/{team}/{role}.md" ]
    else
        [ $"agents/{role}.md" ]

let hasAutomaticPaidFallback (config: JsonElement) =
    let forbidden =
        [ "fallback"
          "failover"
          "fallback_model"
          "paid_fallback"
          "automatic_fallback"
          "model_fallback" ]

    let rec exists (element: JsonElement) =
        match element.ValueKind with
        | JsonValueKind.Object ->
            element.EnumerateObject()
            |> Seq.exists (fun property ->
                forbidden |> List.exists (fun name -> property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                || exists property.Value)
        | JsonValueKind.Array -> element.EnumerateArray() |> Seq.exists exists
        | _ -> false

    exists config

let obsoleteLivePatterns =
    [ "architect-1", Regex(@"architect-1")
      "architect-2", Regex(@"architect-2")
      "reviewer-1", Regex(@"reviewer-1")
      "reviewer-2", Regex(@"reviewer-2")
      "reviewer-3", Regex(@"reviewer-3")
      "sql-engineer", Regex(@"sql-engineer")
      "sql-reviewer", Regex(@"sql-reviewer")
      "multimodal-lens", Regex(@"multimodal-lens")
      "audit-infra", Regex(@"audit-infra")
      "office-documents", Regex(@"office-documents")
      "agents/audit.md", Regex(@"agents/audit\.md")
      "model-profiles", Regex(@"model-profiles")
      "mistral/", Regex(@"mistral/") ]

let obsoleteLiveMatches (content: string) =
    obsoleteLivePatterns
    |> List.choose (fun (name, pattern) -> if pattern.IsMatch content then Some name else None)

let requireSelfTest name condition =
    if not condition then failwith $"self-test failed: {name}"

if selfTest then
    requireSelfTest
        "package target failure"
        (missingInRootTargets root (fun _ -> false) [ "node"; "lib/missing.mjs" ] = [ "lib/missing.mjs" ])
    requireSelfTest
        "MCP --directory target"
        (commandTargetSpecs [ "uv"; "--directory"; "mcp/telegram"; "run"; "main.py" ] = [ Path.Combine("mcp/telegram", "main.py") ])
    requireSelfTest
        "MCP --manifest-path target"
        (commandTargetSpecs [ "cargo"; "test"; "--manifest-path"; "mcp/firefox/Cargo.toml" ] = [ "mcp/firefox/Cargo.toml" ])
    requireSelfTest "plugin syntax failure" (checkPluginSyntax (Path.Combine(root, "plugins/broken.js")) 1 "Unexpected token" |> Option.isSome)
    use fixture = JsonDocument.Parse("""{ "permission": { "telegram_*": "ask", "github_*": "ask", "firefox_*": "ask", "firefox_read": "allow", "firefox_find": "allow", "firefox_close": "allow", "bash": { "dotnet fsi \"C:/Users/andre/.config/opencode/skills/documents/scripts/*": "allow", "dotnet fsi \"C:/Users/andre/.config/opencode/skills/audit/scripts/ValidateInfrastructure.fsx*": "allow", "dotnet fsi \"C:/Users/andre/.config/opencode/skills/audit/scripts/TestInfrastructure.fsx*": "ask" }, "read": { "**/.env": "deny", "**/auth.json": "deny", "**/credentials.json": "deny", "**/secrets.json": "deny", "**/token.json": "deny", "**/id_rsa": "deny", "**/Mozilla/Firefox/Profiles/**/logins.json": "deny", "**/Mozilla/Firefox/Profiles/**/key4.db": "deny", "**/Mozilla/Firefox/Profiles/**/cookies.sqlite": "deny", "**/Google/Chrome/User Data/**/Login Data": "deny", "**/Google/Chrome/User Data/**/Cookies": "deny", "**/Microsoft/Edge/User Data/**/Login Data": "deny", "**/Microsoft/Edge/User Data/**/Cookies": "deny", "**/BraveSoftware/Brave-Browser/User Data/**/Login Data": "deny", "**/BraveSoftware/Brave-Browser/User Data/**/Cookies": "deny", "**/Opera Software/Opera Stable/**/Login Data": "deny", "**/Opera Software/Opera Stable/**/Cookies": "deny", "**/Vivaldi/User Data/**/Login Data": "deny", "**/Vivaldi/User Data/**/Cookies": "deny" } } }""")
    requireSelfTest
        "Office permission marker"
        (not (permissionPatternAction fixture.RootElement "bash" officeDocumentsFsiPattern "ask"))
    requireSelfTest "permission markers" (permissionPatternAction fixture.RootElement "read" "**/.env" "deny")
    for marker in requiredReadDenies do
        requireSelfTest $"read deny marker {marker}" (permissionPatternAction fixture.RootElement "read" marker "deny")
    requireSelfTest
        "required routing scenarios encoded"
        (requiredRoutingScenarios |> List.forall (fun scenario -> routingFor scenario <> emptyRouting))
    requireSelfTest "discovery profiles are risk-based sets" (discoveryReviewProfiles = [ "routine"; "contract"; "architecture"; "combined" ])
    requireSelfTest
        "exact profile reviewer sets"
        (routineDiscoveryReviewers = [ "reviewer" ]
         && contractDiscoveryReviewers = [ "reviewer"; "validator" ]
         && architectureDiscoveryReviewers = [ "reviewer"; "guardian" ]
         && combinedDiscoveryReviewers = [ "reviewer"; "guardian"; "validator" ])
    requireSelfTest
        "routine routing"
        (routingFor "routine" = { Design = []; Implementation = [ "engineer" ]; Tests = [ "tester" ]; Discovery = routineDiscoveryReviewers })
    requireSelfTest
        "contract routing"
        (routingFor "contract" = { Design = []; Implementation = [ "engineer" ]; Tests = [ "tester" ]; Discovery = contractDiscoveryReviewers })
    requireSelfTest
        "architecture routing"
        (routingFor "architecture" = { Design = [ "architect"; "challenger" ]; Implementation = [ "engineer" ]; Tests = [ "tester" ]; Discovery = architectureDiscoveryReviewers })
    requireSelfTest
        "combined routing"
        (routingFor "combined" = { Design = [ "architect"; "challenger" ]; Implementation = [ "engineer" ]; Tests = [ "tester" ]; Discovery = combinedDiscoveryReviewers })
    requireSelfTest
        "database-only routing"
        (routingFor "database-only" = { Design = [ "database/reviewer" ]; Implementation = [ "database/engineer" ]; Tests = [ "database/engineer" ]; Discovery = [ "database/reviewer" ] })
    requireSelfTest
        "app+database routing"
        (routingFor "app+database" = { Design = [ "database/reviewer" ]; Implementation = [ "engineer"; "database/engineer" ]; Tests = [ "tester" ]; Discovery = [ "reviewer"; "database/reviewer" ] })
    requireSelfTest
        "devops-only routing"
        (routingFor "devops-only" = { Design = [ "devops/engineer" ]; Implementation = [ "devops/engineer" ]; Tests = [ "devops/engineer" ]; Discovery = [ "devops/reviewer" ] })
    requireSelfTest
        "app+devops routing"
        (routingFor "app+devops" = { Design = [ "devops/engineer" ]; Implementation = [ "engineer"; "devops/engineer" ]; Tests = [ "tester" ]; Discovery = [ "reviewer"; "devops/reviewer" ] })
    requireSelfTest
        "security-sensitive routing"
        (routingFor "security-sensitive" = { Design = [ "security/reviewer" ]; Implementation = [ "engineer" ]; Tests = [ "tester" ]; Discovery = [ "reviewer"; "security/reviewer" ] })
    requireSelfTest
        "performance-sensitive routing"
        (routingFor "performance-sensitive" = { Design = [ "performance/reviewer" ]; Implementation = [ "engineer" ]; Tests = [ "tester" ]; Discovery = [ "reviewer"; "performance/reviewer" ] })
    requireSelfTest
        "unsupported language/executor routing"
        (routingFor "unsupported-language" = { Design = [ "general" ]; Implementation = [ "executor" ]; Tests = [ "executor" ]; Discovery = [ "general" ] })
    requireSelfTest
        "database-only specialist without language reviewer"
        ((routingFor "database-only").Discovery = [ "database/reviewer" ])
    requireSelfTest
        "devops-only specialist without language reviewer"
        ((routingFor "devops-only").Discovery = [ "devops/reviewer" ])
    requireSelfTest
        "app+database language plus specialist"
        ((routingFor "app+database").Discovery = [ "reviewer"; "database/reviewer" ])
    requireSelfTest
        "app+devops language plus specialist"
        ((routingFor "app+devops").Discovery = [ "reviewer"; "devops/reviewer" ])
    requireSelfTest
        "security reviewer additive only"
        ((routingFor "security-sensitive").Discovery = [ "reviewer"; "security/reviewer" ])
    requireSelfTest
        "performance reviewer additive only"
        ((routingFor "performance-sensitive").Discovery = [ "reviewer"; "performance/reviewer" ])
    requireSelfTest
        "tester owns app tests"
        ((routingFor "routine").Tests = [ "tester" ])
    requireSelfTest
        "database-only test fallback to implementation owner"
        ((routingFor "database-only").Tests = [ "database/engineer" ])
    requireSelfTest
        "devops-only test fallback to implementation owner"
        ((routingFor "devops-only").Tests = [ "devops/engineer" ])
    requireSelfTest
        "unsupported-language test fallback to executor"
        ((routingFor "unsupported-language").Tests = [ "executor" ])
    requireSelfTest "unknown routing is empty" (routingFor "other" = emptyRouting)
    requireSelfTest "obsolete architecture-contract is not encoded" (routingFor "architecture-contract" = emptyRouting)
    requireSelfTest "obsolete Standard profile is not encoded" (routingFor "Standard" = emptyRouting)
    requireSelfTest "challenger is grok diversity" (grokDiversityRoles.Contains "challenger")
    requireSelfTest "guardian is grok diversity" (grokDiversityRoles.Contains "guardian")
    requireSelfTest "validator is deepseek worker" (deepseekWorkerRoles.Contains "validator")
    requireSelfTest "reviewer is not grok diversity" (not (grokDiversityRoles.Contains "reviewer"))
    requireSelfTest "architect assignment" (languageRoleAssignment "architect" = Some(OpenAiSol, "high"))
    requireSelfTest "unknown role assignment" (languageRoleAssignment "reviewer-1" = None)
    requireSelfTest "obsolete architect-1" (isProhibitedAgentName "architect-1")
    requireSelfTest "semantic reviewer allowed" (not (isProhibitedAgentName "reviewer"))
    requireSelfTest "obsolete audit agent" (isProhibitedAgentName "audit")
    requireSelfTest "semantic auditor allowed" (not (isProhibitedAgentName "auditor"))
    requireSelfTest
        "obsolete live architect-1"
        (obsoleteLiveMatches "see `software/dotnet/fsharp/architect-1`" = [ "architect-1" ])
    requireSelfTest "obsolete audit-infra" (obsoleteLiveMatches "skills/audit-infra/SKILL.md" = [ "audit-infra" ])
    requireSelfTest "obsolete office-documents" (obsoleteLiveMatches "skills/office-documents/SKILL.md" = [ "office-documents" ])
    requireSelfTest "obsolete agents/audit.md" (obsoleteLiveMatches "see agents/audit.md" = [ "agents/audit.md" ])
    requireSelfTest "auditor is not obsolete" (obsoleteLiveMatches "agents/auditor.md" = [])
    requireSelfTest "skills/audit is not obsolete" (obsoleteLiveMatches "skills/audit/SKILL.md" = [])
    use fallbackFixture = JsonDocument.Parse("""{ "fallback": { "model": "openai/gpt-5.6-terra" } }""")
    requireSelfTest "automatic paid fallback detected" (hasAutomaticPaidFallback fallbackFixture.RootElement)
    use cleanFixture = JsonDocument.Parse("""{ "model": "openai/gpt-5.6-terra" }""")
    requireSelfTest "no automatic paid fallback" (not (hasAutomaticPaidFallback cleanFixture.RootElement))
    requireSelfTest "expected assignment count" (expectedAgentAssignments.Length = sharedAgentAssignments.Length + languageTeams.Length * languageRoles.Length)
    requireSelfTest "challenger channel" (roleUsesAssignedChannel "challenger" grokDiversityModel)
    requireSelfTest "engineer channel" (roleUsesAssignedChannel "engineer" "deepseek/deepseek-v4-pro")
    requireSelfTest "reviewer channel" (roleUsesAssignedChannel "reviewer" "openai/gpt-5.6-luna")
    requireSelfTest "wrong worker channel rejected" (not (roleUsesAssignedChannel "tester" grokDiversityModel))
    // Capability-versus-channel matrix (INFRA-008): channels are validated
    // separately from the capability, and each canonical capability pins its
    // allowed serving channels.
    requireSelfTest "flash direct channel" (capabilityOfModel "deepseek/deepseek-v4-flash" = Some DeepSeekFlash)
    requireSelfTest "flash opencode-go channel" (capabilityOfModel "opencode-go/deepseek-v4-flash" = Some DeepSeekFlash)
    requireSelfTest "pro direct channel" (capabilityOfModel "deepseek/deepseek-v4-pro" = Some DeepSeekPro)
    requireSelfTest "pro opencode-go channel" (capabilityOfModel "opencode-go/deepseek-v4-pro" = Some DeepSeekPro)
    requireSelfTest
        "all approved deepseek channels are production IDs"
        ([ "deepseek/deepseek-v4-flash"; "opencode-go/deepseek-v4-flash"
           "deepseek/deepseek-v4-pro"; "opencode-go/deepseek-v4-pro" ]
         |> List.forall productionModels.Contains)
    requireSelfTest
        "grok is direct xai only"
        (capabilityOfModel "xai/grok-4.6" = Some Grok45 && capabilityOfModel "opencode-go/grok-4.5" = None)
    requireSelfTest
        "openai identities are exact"
        (capabilityOfModel "openai/gpt-5.6-terra" = Some OpenAiTerra
         && capabilityOfModel "openai/gpt-5.6-luna" = Some OpenAiLuna
         && capabilityOfModel "openai/gpt-5.6-sol" = Some OpenAiSol
         && capabilityOfModel "opencode-go/gpt-5.6-sol" = None)
    requireSelfTest
        "tier rejection flash vs pro"
        (capabilityOfModel "deepseek/deepseek-v4-flash" <> Some DeepSeekPro
         && languageRoleAssignment "engineer" = Some(DeepSeekPro, "high")
         && languageRoleAssignment "tester" = Some(DeepSeekFlash, "high"))
    requireSelfTest "grok in deepseek role rejected" (not (roleUsesAssignedChannel "engineer" "xai/grok-4.6"))
    requireSelfTest "deepseek in grok role rejected" (not (roleUsesAssignedChannel "challenger" "deepseek/deepseek-v4-pro"))
    requireSelfTest
        "variant rejection"
        (productionVariants = Set.ofList [ "low"; "medium"; "high" ] && not (productionVariants.Contains "premium"))
    requireSelfTest
        "live matrix uses canonical capabilities and variants"
        (expectedAgentAssignments
         |> List.forall (fun (_, capability, variant) ->
             allCapabilities.Contains capability && productionVariants.Contains variant))
    requireSelfTest
        "engineer capability is DeepSeekPro across language teams"
        (languageTeams
         |> List.forall (fun team ->
             expectedAgentAssignments
             |> List.exists (fun (path, capability, _) ->
                 path = $"agents/software/{team}/engineer.md" && capability = DeepSeekPro)))
    requireSelfTest "small_model channel resolves to DeepSeekFlash" (capabilityOfModel "opencode-go/deepseek-v4-flash" = Some DeepSeekFlash)
    requireSelfTest
        "architect+challenger parallel wave only when both required"
        (requiredRoutingScenarios |> List.forall (fun scenario ->
            let design = (routingFor scenario).Design
            let hasArchitect = List.contains "architect" design
            let hasChallenger = List.contains "challenger" design
            (hasArchitect = hasChallenger)
            && (hasArchitect = (scenario = "architecture" || scenario = "combined"))))
    requireSelfTest
        "no duplicate agents within a parallel wave"
        (requiredRoutingScenarios |> List.forall (fun scenario ->
            let routing = routingFor scenario
            [ routing.Design; routing.Implementation; routing.Tests; routing.Discovery ]
            |> List.forall (fun agents -> List.length agents = List.length (List.distinct agents))))
    printfn "OK infrastructure validator self-test"
    exit 0

let configPath = Path.Combine(root, "opencode.json")

if File.Exists configPath then
    try
        use document = JsonDocument.Parse(File.ReadAllText configPath)
        let config = document.RootElement

        match tryProperty "$schema" config with
        | Some schema when schema.GetString() = "https://opencode.ai/config.json" -> ()
        | _ -> error "config-schema" "opencode.json" "Missing or unexpected $schema value"

        validateMcpConfiguration config

        if tryProperty "agent" config |> Option.isSome then
            error "inline-agents" "opencode.json" "Agent definitions must remain file-based under agents/"

        if hasAutomaticPaidFallback config then
            error "paid-fallback" "opencode.json" "Automatic paid-provider fallback must not be configured"

        match tryProperty "model" config with
        | Some model when model.ValueKind = JsonValueKind.String && productionModels.Contains(model.GetString()) -> ()
        | Some model when model.ValueKind = JsonValueKind.String ->
            error "production-model" "opencode.json" $"Global model '{model.GetString()}' is not a verified production ID"
        | _ -> error "production-model" "opencode.json" "Global model must be a verified production ID"

        match tryProperty "small_model" config with
        | Some model when model.ValueKind = JsonValueKind.String && model.GetString() = "opencode-go/deepseek-v4-flash" -> ()
        | Some model when model.ValueKind = JsonValueKind.String ->
            error "production-model" "opencode.json" $"Global small_model '{model.GetString()}' must be opencode-go/deepseek-v4-flash"
        | _ -> error "production-model" "opencode.json" "Global small_model must be opencode-go/deepseek-v4-flash"

        let bashRules =
            tryProperty "permission" config
            |> Option.bind (tryProperty "bash")
            |> Option.map (fun rules -> rules.EnumerateObject() |> Seq.toList)
            |> Option.defaultValue []

        match bashRules |> List.tryFindIndex (fun rule -> rule.Name = "*") with
        | Some 0 -> ()
        | Some _ -> error "permission-order" "opencode.json" "The broad bash rule must precede narrower last-match rules"
        | None -> warning "permission-default" "opencode.json" "No broad bash default rule is defined"

        let fsiAskIndex =
            bashRules
            |> List.tryFindIndex (fun rule ->
                rule.Name = "dotnet fsi *"
                && rule.Value.ValueKind = JsonValueKind.String
                && rule.Value.GetString() = "ask")

        let fsiAllows =
            bashRules
            |> List.indexed
            |> List.filter (fun (_, rule) ->
                rule.Name <> "dotnet fsi *"
                && rule.Name.StartsWith("dotnet fsi ", StringComparison.OrdinalIgnoreCase)
                && rule.Value.ValueKind = JsonValueKind.String
                && rule.Value.GetString() = "allow")

        match fsiAskIndex with
        | None when not fsiAllows.IsEmpty ->
            error "permission-order" "opencode.json" "Specific dotnet fsi allows require an earlier broad ask rule"
        | Some broadIndex ->
            for index, rule in fsiAllows do
                if index <= broadIndex then
                    error "permission-order" "opencode.json" $"Rule '{rule.Name}' is overridden by the later broad ask rule"
        | None -> ()
    with ex ->
        error "config-json" "opencode.json" $"Invalid JSON: {ex.Message}"

let packagePath = Path.Combine(root, "package.json")

if File.Exists packagePath then
    try
        use document = JsonDocument.Parse(File.ReadAllText packagePath)
        match tryProperty "scripts" document.RootElement with
        | Some scripts when scripts.ValueKind = JsonValueKind.Object && not (scripts.EnumerateObject() |> Seq.isEmpty) ->
            error "npm-test-dispatcher" "package.json" "npm test dispatcher scripts are removed; TestInfrastructure.fsx is the canonical test entry"
        | _ -> ()
    with ex ->
        error "package-json" "package.json" $"Invalid JSON: {ex.Message}"

validateTestEntryPoint ()

validatePlugins ()

let parseFrontmatter file =
    let lines = File.ReadAllLines file
    if lines.Length = 0 || lines.[0].Trim() <> "---" then
        error "frontmatter" (relativePath file) "Missing opening frontmatter delimiter"
        None
    else
        match lines |> Array.skip 1 |> Array.tryFindIndex (fun line -> line.Trim() = "---") with
        | None ->
            error "frontmatter" (relativePath file) "Missing closing frontmatter delimiter"
            None
        | Some relativeEnd ->
            let endIndex = relativeEnd + 1
            let values = Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            let mutable currentKey: string option = None

            for line in lines.[1 .. endIndex - 1] do
                let matched = Regex.Match(line, @"^(?<key>[A-Za-z_][A-Za-z0-9_-]*):\s*(?<value>.*)$")
                let isTopLevel = line.Length > 0 && not (Char.IsWhiteSpace line.[0])

                if isTopLevel && not matched.Success && not (line.TrimStart().StartsWith("#")) then
                    error "frontmatter" (relativePath file) $"Unsupported top-level frontmatter line '{line.Trim()}'"
                elif matched.Success then
                    let key = matched.Groups.["key"].Value
                    currentKey <- Some key
                    let rawValue = matched.Groups.["value"].Value.Trim()
                    let value =
                        if rawValue.Length >= 2
                           && ((rawValue.StartsWith("\"") && rawValue.EndsWith("\""))
                               || (rawValue.StartsWith("'") && rawValue.EndsWith("'"))) then
                            rawValue.Substring(1, rawValue.Length - 2)
                        else
                            rawValue

                    if values.ContainsKey key then
                        error "frontmatter" (relativePath file) $"Duplicate frontmatter key '{key}'"
                    elif (key.Equals("name", StringComparison.OrdinalIgnoreCase)
                          || key.Equals("description", StringComparison.OrdinalIgnoreCase))
                         && Regex.IsMatch(value, @"^[>|][+-]?$") then
                        error "frontmatter" (relativePath file) $"Frontmatter '{key}' must use a one-line scalar"
                    else
                        values.[key] <- value
                elif not isTopLevel && not (String.IsNullOrWhiteSpace line) then
                    match currentKey with
                    | Some key when key.Equals("name", StringComparison.OrdinalIgnoreCase)
                                    || key.Equals("description", StringComparison.OrdinalIgnoreCase) ->
                        error "frontmatter" (relativePath file) $"Frontmatter '{key}' must not have continuation lines"
                    | _ -> ()

            Some(lines, values)

let requireFrontmatterValue key file (values: Dictionary<string, string>) =
    match values.TryGetValue key with
    | true, value when not (String.IsNullOrWhiteSpace value) -> Some value
    | _ ->
        error "frontmatter" (relativePath file) $"Missing frontmatter value '{key}'"
        None

let skillsRoot = Path.Combine(root, "skills")

if Directory.Exists skillsRoot then
    for file in recursiveFiles skillsRoot "SKILL.md" do
        match parseFrontmatter file with
        | None -> ()
        | Some(lines, values) ->
            let expectedName = Directory.GetParent(file).Name

            match requireFrontmatterValue "name" file values with
            | Some name ->
                if name <> expectedName then
                    error "skill-name" (relativePath file) $"Skill name '{name}' must match folder '{expectedName}'"

                if not (Regex.IsMatch(name, @"^[a-z0-9]+(?:-[a-z0-9]+)*$")) then
                    error "skill-name" (relativePath file) "Skill name must be kebab-case"
            | None -> ()

            match requireFrontmatterValue "description" file values with
            | Some description ->
                if not (Regex.IsMatch(description, @"^(Run|Use|Do not use)\b", RegexOptions.IgnoreCase)) then
                    error "skill-routing" (relativePath file) "Description must start with a concrete Run/Use/Do not use trigger"

                if not (Regex.IsMatch(description, @"\bdo not (use|run)\b", RegexOptions.IgnoreCase)) then
                    error "skill-routing" (relativePath file) "Description must include a negative routing hint"
            | None -> ()

            let requiredSections = [ "## Purpose"; "## Guardrails"; "## Workflow"; "## Output" ]
            let sectionIndexes =
                requiredSections
                |> List.map (fun section -> section, lines |> Array.tryFindIndex (fun line -> line.Trim() = section))

            for section, index in sectionIndexes do
                if index.IsNone then
                    error "skill-section" (relativePath file) $"Missing required section '{section}'"

            let presentIndexes = sectionIndexes |> List.choose snd
            if presentIndexes <> (presentIndexes |> List.sort) then
                error "skill-section" (relativePath file) "Core sections must follow Purpose, Guardrails, Workflow, Output order"

let agentsRoot = Path.Combine(root, "agents")

if Directory.Exists agentsRoot then
    for file in recursiveFiles agentsRoot "*.md" do
        parseFrontmatter file
        |> Option.iter (fun (_, values) -> requireFrontmatterValue "description" file values |> ignore)

if Directory.Exists agentsRoot then
    for file in recursiveFiles agentsRoot "*.md" do
        let agentName = Path.GetFileNameWithoutExtension file
        if isProhibitedAgentName agentName then
            error "prohibited-agent" (relativePath file) $"Agent definition '{agentName}' is prohibited by the migration contract"

        parseFrontmatter file
        |> Option.iter (fun (_, values) ->
            let model = match values.TryGetValue "model" with | true, value -> value | _ -> ""
            let variant = match values.TryGetValue "variant" with | true, value -> value | _ -> ""

            if String.IsNullOrWhiteSpace model || not (productionModels.Contains model) then
                error "agent-model" (relativePath file) $"Agent model '{model}' is not a verified production ID"

            if String.IsNullOrWhiteSpace variant || not (productionVariants.Contains variant) then
                error "agent-variant" (relativePath file) $"Agent variant '{variant}' is not a required production variant"

            if not (roleUsesAssignedChannel agentName model) then
                error "agent-routing" (relativePath file) $"Role '{agentName}' is not assigned to its required production channel")

let modelProfilesRoot = Path.Combine(root, "model-profiles")
if Directory.Exists modelProfilesRoot then
    error "removed-surface" "model-profiles" "Obsolete model-profile artifacts must not exist"

for path, expectedCapability, expectedVariant in expectedAgentAssignments do
    let fullPath = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))
    if not (File.Exists fullPath) then
        error "agent-topology" path "Required semantic agent definition is missing"
    else
        match parseFrontmatter fullPath with
        | None -> ()
        | Some(_, values) ->
            match values.TryGetValue "model" with
            | true, model when capabilityOfModel model = Some expectedCapability -> ()
            | true, model -> error "agent-model" path $"Expected model capability '{capabilityName expectedCapability}' but found '{model}'"
            | _ -> error "agent-model" path $"Missing model; expected '{capabilityName expectedCapability}'"

            match values.TryGetValue "variant" with
            | true, variant when variant = expectedVariant -> ()
            | true, variant -> error "agent-variant" path $"Expected variant '{expectedVariant}' but found '{variant}'"
            | _ -> error "agent-variant" path $"Missing variant; expected '{expectedVariant}'"

for role in languageRoles do
    let assignments =
        languageTeams
        |> List.choose (fun team ->
            let path = $"agents/software/{team}/{role}.md"
            let fullPath = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))
            if not (File.Exists fullPath) then
                None
            else
                match parseFrontmatter fullPath with
                | Some(_, values) ->
                    let model = match values.TryGetValue "model" with | true, value -> value | _ -> ""
                    let variant = match values.TryGetValue "variant" with | true, value -> value | _ -> ""
                    Some(path, capabilityOfModel model, variant)
                | None -> None)

    let distinctAssignments =
        assignments
        |> List.map (fun (_, capability, variant) -> capability, variant)
        |> List.distinct

    if distinctAssignments.Length > 1 then
        let paths = assignments |> List.map (fun (path, _, _) -> path) |> String.concat ", "
        error "team-consistency" paths $"Language teams disagree on '{role}' capability or variant"

let obsoleteTeamRule = Path.Combine(root, "rules", "software", "team.md")
if File.Exists obsoleteTeamRule then
    error "removed-surface" "rules/software/team.md" "Removed team rule must not exist"

if Directory.Exists skillsRoot then
    for file in recursiveFiles skillsRoot "SKILL.md" do
        let content = File.ReadAllText file
        if Directory.GetParent(file).Name.Equals("audit-session", StringComparison.OrdinalIgnoreCase)
           || Regex.IsMatch(content, "(?im)^name:\\s*['\"]?audit-session['\"]?\\s*$") then
            error "removed-surface" (relativePath file) "Removed audit-session skill must not exist"

let commandsRoot = Path.Combine(root, "commands")

if Directory.Exists commandsRoot then
    for file in recursiveFiles commandsRoot "*.md" do
        parseFrontmatter file
        |> Option.iter (fun (_, values) ->
            requireFrontmatterValue "description" file values |> ignore

            match values.TryGetValue "agent" with
            | true, agent when not (String.IsNullOrWhiteSpace agent) ->
                let agentFile = Path.Combine(agentsRoot, agent.Replace('/', Path.DirectorySeparatorChar) + ".md")
                if not (File.Exists agentFile) then
                    error "command-agent" (relativePath file) $"Agent route '{agent}' does not resolve to agents/{agent}.md"
            | _ -> ())

let pascalCase = Regex(@"^[A-Z][A-Za-z0-9]*(?:\.[A-Z][A-Za-z0-9]*)*$")

for directory in [ "commands"; "scripts"; "skills" ] do
    let fullDirectory = Path.Combine(root, directory)
    if Directory.Exists fullDirectory then
        for file in recursiveFiles fullDirectory "*.fsx" do
            let name = Path.GetFileNameWithoutExtension file
            if not (pascalCase.IsMatch name) then
                error "fsharp-name" (relativePath file) "F# script filename must be PascalCase"

let scriptsReadme = Path.Combine(root, "scripts", "README.md")

if File.Exists scriptsReadme then
    let indexedHelperRows =
        File.ReadAllLines scriptsReadme
        |> Array.choose (fun line ->
            let matched = Regex.Match(line, @"^scripts/(?<name>[^ |]+\.fsx)\s+\|")
            if matched.Success then Some matched.Groups.["name"].Value else None)

    let indexedHelpers = indexedHelperRows |> Set.ofArray

    indexedHelperRows
    |> Array.countBy id
    |> Array.filter (fun (_, count) -> count > 1)
    |> Array.iter (fun (name, _) -> error "helper-index" "scripts/README.md" $"Duplicate helper index row for scripts/{name}")

    let actualHelpers =
        Directory.EnumerateFiles(Path.Combine(root, "scripts"), "*.fsx", SearchOption.TopDirectoryOnly)
        |> Seq.map Path.GetFileName
        |> Set.ofSeq

    for missing in Set.difference actualHelpers indexedHelpers do
        error "helper-index" "scripts/README.md" $"Missing helper index row for scripts/{missing}"

    for stale in Set.difference indexedHelpers actualHelpers do
        error "helper-index" "scripts/README.md" $"Stale helper index row for scripts/{stale}"

    let indexedRows =
        File.ReadAllLines scriptsReadme
        |> Array.choose (fun line ->
            let m = Regex.Match(line, @"^scripts/(?<file>[^ |]+\.fsx)\s+\|\s*(?<modules>[^|]+)\s*\|\s*(?<exports>[^|]+)\s*\|")
            if m.Success then
                let file = m.Groups.["file"].Value
                let modules =
                    m.Groups.["modules"].Value.Trim().Split(',', StringSplitOptions.RemoveEmptyEntries)
                    |> Array.map (fun s -> s.Trim())
                    |> Array.toList
                let exports =
                    m.Groups.["exports"].Value.Trim().Split(';', StringSplitOptions.RemoveEmptyEntries)
                    |> Array.collect (fun group ->
                        let parts = group.Trim().Split(',', StringSplitOptions.RemoveEmptyEntries) |> Array.map (fun s -> s.Trim())
                        if parts.Length > 0 then
                            let moduleName =
                                parts
                                |> Array.tryFind (fun export -> export.Contains '.')
                                |> Option.map (fun export -> export.Substring(0, export.IndexOf '.'))
                                |> Option.orElseWith (fun () ->
                                    match modules with
                                    | [ moduleName ] -> Some moduleName
                                    | _ -> None)

                            parts
                            |> Array.map (fun export ->
                                if export.Contains '.' then export
                                else moduleName |> Option.map (fun name -> name + "." + export) |> Option.defaultValue export)
                        else
                            parts)
                    |> Set.ofArray
                Some(file, modules, exports)
            else
                None)

    for file, declaredModules, declaredExports in indexedRows do
        let fsxPath = Path.Combine(root, "scripts", file)
        if File.Exists fsxPath then
            let content = File.ReadAllText fsxPath
            let actualModules =
                Regex.Matches(content, @"^module\s+(\w+)\s*=", RegexOptions.Multiline)
                |> Seq.cast<Match>
                |> Seq.map (fun m -> m.Groups.[1].Value)
                |> Set.ofSeq

            for m in declaredModules do
                if not (actualModules.Contains m) then
                    error "helper-index" "scripts/README.md" $"Module '{m}' declared in index for scripts/{file} but not found in file"

            for m in actualModules do
                if not (List.contains m declaredModules) then
                    error "helper-index" "scripts/README.md" $"Module '{m}' found in scripts/{file} but not listed in index"

            let actualExports =
                let publicBinding = Regex(@"^    let\s+(?!private\b)(?:\(\|(?<active>\w+)\||(?<name>\w+))")
                let moduleDeclaration = Regex(@"^module\s+(?<name>\w+)\s*=")
                let mutable currentModule = None

                File.ReadAllLines fsxPath
                |> Seq.choose (fun line ->
                    let moduleMatch = moduleDeclaration.Match line
                    if moduleMatch.Success then
                        currentModule <- Some moduleMatch.Groups.["name"].Value
                        None
                    else
                        let bindingMatch = publicBinding.Match line
                        match currentModule, bindingMatch.Success with
                        | Some moduleName, true ->
                            let exportName =
                                if bindingMatch.Groups.["active"].Success then bindingMatch.Groups.["active"].Value
                                else bindingMatch.Groups.["name"].Value
                            Some(moduleName + "." + exportName)
                        | _ -> None)
                |> Set.ofSeq

            for export in Set.difference declaredExports actualExports do
                error "helper-index" "scripts/README.md" $"Export '{export}' declared in index for scripts/{file} but not found in file"

            for export in Set.difference actualExports declaredExports do
                error "helper-index" "scripts/README.md" $"Public export '{export}' in scripts/{file} is missing from the helper index"

let markdownFiles =
    seq {
        for name in [ "AGENTS.md"; "README.md" ] do
            let file = Path.Combine(root, name)
            if File.Exists file then yield file

        for directory in [ "agents"; "commands"; "rules"; "scripts"; "skills" ] do
            let fullDirectory = Path.Combine(root, directory)
            if Directory.Exists fullDirectory then
                yield! recursiveFiles fullDirectory "*.md"
    }
    |> Seq.distinct
    |> Seq.toArray

let containsMarker (marker: string) (content: string) =
    content.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0

let missingMarkers (markers: string list) (content: string) =
    markers |> List.filter (fun marker -> not (containsMarker marker content))

let requireMarkers (code: string) (path: string) (markers: string list) =
    let fullPath = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))
    if not (File.Exists fullPath) then
        error code path "Required canonical file does not exist"
    else
        let content = File.ReadAllText fullPath
        for marker in missingMarkers markers content do
            error code path $"Missing canonical marker '{marker}'"

let canonicalReviewMarkers =
    [ "NEW -> DISCOVERY -> REMEDIATION -> VERIFICATION -> FROZEN"
      "Discovery occurs once, after the evidence precondition, per frozen solution and implementation baseline"
      "return `BLOCKED: <missing inputs>` and stop"
      "accepted finding set is finite and frozen"
      "Verification is not a fresh review"
      "generic request to review a frozen artifact means Verification"
      "Automatic remediation is limited to two passes"
      "Do not run builds or tests" ]

requireMarkers "review-convergence" "rules/software/review.md" canonicalReviewMarkers

requireMarkers
    "task-convergence"
    "skills/task/SKILL.md"
    [ "owns architecture selection"
      "independent architecture proposals only when that gate requires them"
      "one Discovery review"
      "bounded remediation, and targeted Verification"
      "Do not restart Discovery after the accepted finding set freezes" ]

requireMarkers
    "audit-convergence"
    "skills/audit/SKILL.md"
    [ "accepts a finite scope"
      "freezes the smallest sufficient solution"
      "Already explicitly authorized frozen batches may execute"
      "targeted semantic Verification rather than another broad audit"
      "at most two remediation passes"
      "generic re-review of a frozen result as Verification" ]

requireMarkers
    "discovery-routing"
    "skills/task/references/agent-gates.md"
    [ "only the language `reviewer` is mandatory"
      "`reviewer` and `validator`"
      "`reviewer` and `guardian`"
      "`reviewer`, `guardian`, and `validator`"
      "selected independently"
      "language-matching `architect` and `challenger`"
      "`database/reviewer`"
      "`database/engineer`"
      "`routine`"
      "`contract`"
      "`architecture`"
      "`combined`"
      "`executor`"
      "`general`" ]

requireMarkers
    "task-batching"
    "skills/task/SKILL.md"
    [ "Batch TASK.md state updates coherently"
      "recompute progress and validate before each batch is durable"
      "Durability boundary: validated TASK.md facts are durable"
      "in-session drafts, proposals, and working notes are transient"
      "Parallelize only already-required independent waves"
      "keep dependent chains (implementation → build → test → review → remediation) ordered" ]

requireMarkers
    "parallel-waves"
    "skills/task/references/agent-gates.md"
    [ "## Parallel waves and state batching"
      "Dispatch only already-required independent waves in parallel"
      "`architect` and `challenger` (isolated, read-only, equivalent frozen evidence)"
      "task-specific `Implement:` subtasks that are genuinely independent slices"
      "the independently selected reviewers in the chosen profile"
      "Dependent chains stay strictly ordered"
      "Never parallelize dependent work"
      "Each batch recomputes progress then validates before it is durable"
      "Durability boundary: validated TASK.md facts are durable" ]

requireMarkers
    "handoff-batching"
    "rules/software/agent-handoff.md"
    [ "Batch TASK.md state coherently"
      "recompute then validate each batch"
      "the durability boundary explicit"
      "validated TASK.md facts are durable"
      "transient scratch state is not"
      "Dispatch only already-required independent waves in parallel"
      "keep dependent chains ordered"
      "Do not add ceremony for trivial one-shot work" ]

for scenario in requiredRoutingScenarios do
    let assignment = routingFor scenario
    if assignment = emptyRouting then
        error "routing-scenario" "skills/audit/scripts/ValidateInfrastructure.fsx" $"Required routing scenario '{scenario}' is not encoded"
    else
        for role in List.distinct (assignment.Design @ assignment.Implementation @ assignment.Tests @ assignment.Discovery) do
            for path in routedAgentFiles role do
                let fullPath = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))
                if not (File.Exists fullPath) then
                    error "routing-agent" path $"Routing scenario '{scenario}' requires missing agent '{role}'"

requireMarkers
    "engineer-ownership"
    "rules/software/agent-handoff.md"
    [ "own production implementation and"
      "the single build point"
      "implementation-native plan"
      "static"
      "configuration validation"
      "When no applicable language tester exists"
      "Do not automatically substitute another"
      "paid provider" ]

requireMarkers
    "engineer-frozen-solution"
    "rules/software/agent-handoff.md"
    [ "must not independently redesign a frozen solution"
      "return `BLOCKED` to the"
      "do not redesign" ]

requireMarkers
    "tester-ownership"
    "rules/software/testing.md"
    [ "normal owner of applicable test design"
      "implementation, and execution"
      "When no applicable language tester exists"
      "the implementation owner owns the required tests"
      "tester owns test execution for the whole task surface" ]

requireMarkers
    "specialist-surface"
    "skills/task/references/agent-gates.md"
    [ "Use database, DevOps, security, or performance specialists only when concrete task or diff evidence"
      "materially affects their owned surface"
      "Before Discovery, build and test evidence must be recorded"
      "returns `BLOCKED` and does not review" ]

requireMarkers
    "devops-validation"
    "agents/software/devops/engineer.md"
    [ "own project-native plan, static, configuration, and build validation"
      "distinct from tests" ]

requireMarkers
    "spec-scenarios"
    "skills/audit/references/behavioral-evaluation.md"
    [ "S11. Routine Discovery selects only reviewer"
      "S12. Combined risk selects independent review trio"
      "S13. Provider exhaustion has no paid fallback" ]

for engineerPath in
    [ for team in languageTeams do
          yield $"agents/software/{team}/engineer.md"
      yield "agents/software/database/engineer.md"
      yield "agents/software/devops/engineer.md" ] do
    requireMarkers "engineer-handoff" engineerPath [ "agent-handoff.md" ]

let reviewerMandates =
    [ "dotnet/csharp", "C#"
      "dotnet/fsharp", "F#"
      "rust", "Rust" ]

let mandateRequirements =
    [ "reviewer", [ "Primary mandate:"; "behavioral correctness"; "regressions"; "error handling" ]
      "guardian", [ "Primary mandate:"; "frozen architecture"; "dependency direction"; "accidental complexity"; "Do not replace a valid frozen design" ]
      "validator", [ "Primary mandate:"; "contracts"; "acceptance criteria"; "test adequacy" ] ]

let duplicateMandatePaths mandates =
    mandates
    |> Seq.filter (fun (_, _, mandate) -> not (String.IsNullOrWhiteSpace mandate))
    |> Seq.groupBy (fun (_, _, mandate) -> mandate)
    |> Seq.choose (fun (_, reviewers) ->
        let paths = reviewers |> Seq.map (fun (_, path, _) -> path) |> Seq.sort |> Seq.toList
        if paths.Length > 1 then Some paths else None)
    |> Seq.toList

for languagePath, languageName in reviewerMandates do
    let mandates = ResizeArray<string * string * string>()

    for reviewerRole, markers in mandateRequirements do
        let path = $"agents/software/{languagePath}/{reviewerRole}.md"
        let fullPath = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))

        if not (File.Exists fullPath) then
            error "reviewer-mandate" path $"Supported {languageName} {reviewerRole} definition is missing"
        else
            let content = File.ReadAllText fullPath
            for marker in markers do
                if not (containsMarker marker content) then
                    error "reviewer-mandate" path $"{reviewerRole} is missing required mandate marker '{marker}'"

            let mandate =
                Regex.Match(content, @"(?im)^Primary mandate:\s*(?<mandate>.+)$")
                |> fun matched -> if matched.Success then matched.Groups.["mandate"].Value else ""
                |> fun value -> Regex.Replace(value.ToLowerInvariant(), @"\s+", " ").Trim()

            mandates.Add(reviewerRole, path, mandate)

    for reviewerPaths in duplicateMandatePaths mandates do
        error "reviewer-mandate" (String.concat ", " reviewerPaths) $"Supported {languageName} reviewer mandates must be distinct"

let taskTemplateMarkers =
    [ "## Solution Contract"
      "- State: DRAFT"
      "- Requirements: TBD"
      "- Acceptance criteria: TBD"
      "- Accepted assumptions: None recorded."
      "- Non-goals: TBD"
      "- Chosen solution: TBD"
      "- Important boundaries/contracts: TBD"
      "- Implementation constraints: TBD"
      "- Review profile: TBD"
      "## Review"
      "- State: NEW"
      "- Implementation baseline: TBD"
      "- Remediation pass: 0"
      "- Build evidence: Not run."
      "- Test evidence: Not run."
      "### Accepted findings"
      "### Verification receipts"
      "Architecture routed per `references/agent-gates.md`; independent proposals used only when that gate requires them, then coordinator solution frozen"
      "Conditional specialists run per `references/agent-gates.md` or explicitly N/A"
      "Engineer-owned implementation completed"
      "Tester inspected existing coverage"
      "Run the one Discovery selected by `references/agent-gates.md`"
      "build and test evidence is recorded"
      "returns `BLOCKED`"
      "Targeted Verification receipts recorded"
      "- Behavioral specification: (optional) `.tasks/{TASK-ID}/SPEC.md`"
      "TaskScratch.fsx"
      "promote durable evidence"
      "manifest-registered disposable entries" ]

requireMarkers "task-template-drift" "skills/task/references/template.md" taskTemplateMarkers

requireMarkers
    "task-template-drift"
    "skills/task/scripts/TaskMd.fsx"
    [ "requiredCodeGateLabels"
      "requiredDesignGateLabels"
      "solutionContractHeading"
      "reviewHeading"
      "validReviewStates"
      "discoveryReviewProfiles"
      "reviewProfileConstraint"
      "routine"
      "contract"
      "architecture"
      "combined"
      "behavioralSpecReferencePrefix"
      "Architecture routed per `references/agent-gates.md`; independent proposals used only when that gate requires them, then coordinator solution frozen"
      "Engineer-owned implementation completed"
      "Tester inspected existing coverage"
      "## Solution Contract"
      "## Review"
      "- Implementation baseline: "
      "- Remediation pass: "
      "- Build evidence: "
      "- Test evidence: "
      "### Accepted findings"
      "### Verification receipts" ]

requireMarkers
    "task-template-drift"
    "skills/task/scripts/CreateTask.fsx"
    [ "references"
      "template.md"
      "canonical task template has no markdown code block" ]

requireMarkers
    "task-template-drift"
    "skills/task/scripts/ValidateTask.fsx"
    [ "#load \"TaskMd.fsx\""
      "requiredCodeGateLabels"
      "requiredDesignGateLabels"
      "validReviewStates"
      "reviewProfileConstraint"
      "Not applicable:"
      "Waived:"
      "hasRecordedWaiver" ]

requireMarkers
    "scratch-lifecycle"
    "skills/task/scripts/TaskScratch.fsx"
    [ "create <TASK-ID>"
      "register <ROOT> <PATH>"
      "report <ROOT>"
      "promote <ROOT> <ENTRY-PATH>"
      "seal <ROOT>"
      "clean <ROOT>"
      "manifest.json"
      "opencode/tasks"
      "handle-relative"
      "NtCreateFile"
      "ReparsePoint"
      "fails closed"
      "byte-verified"
      "rootId"
      "fileId"
      "digest"
      "safe filesystem"
      "promotedTo"
      "manifestVersion"
      "GetTempPath"
      "duplicate entry paths"
      "registered target is missing"
      "unregistered directory"
      "cannot register the manifest file itself"
      "registration is rejected"
      "promotion is rejected"
      "invalid run ID"
      "non-Windows"
      "no compatibility fallback" ]

requireMarkers
    "helper-index"
    "scripts/README.md"
    [ "skills/task/scripts/TaskScratch.fsx"
      "skills/task/scripts/TaskScratchTests.fsx" ]

requireMarkers
    "scratch-closeout"
    "skills/task/references/closing-steps.md"
    [ "TaskScratch.fsx"
      "promote durable evidence"
      "records that no active dependency remains"
      "manifest-registered, non-promoted file entries"
      "without per-file confirmation"
      "fails closed and deletes nothing"
      "reports everything retained" ]

let validatorPath = Path.Combine(__SOURCE_DIRECTORY__, Path.GetFileName __SOURCE_FILE__) |> relativePath
let textExtensions = Set.ofList [ ".md"; ".fsx"; ".js"; ".mjs"; ".json" ]

let liveInfrastructureFiles =
    seq {
        for name in [ "AGENTS.md"; "README.md"; "opencode.json" ] do
            let file = Path.Combine(root, name)
            if File.Exists file then yield file

        for directory in [ "agents"; "commands"; "lib"; "plugins"; "rules"; "scripts"; "skills" ] do
            let fullDirectory = Path.Combine(root, directory)
            if Directory.Exists fullDirectory then
                for file in recursiveFiles fullDirectory "*" do
                    if textExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()) then yield file
    }
    |> Seq.filter (fun file -> not ((relativePath file).Equals(validatorPath, StringComparison.OrdinalIgnoreCase)))
    |> Seq.distinct

let removedRoutePatterns =
    [ "audit-session", Regex(@"(?i)(?:agents/)?audit-session(?:\.md)?|skills/audit-session/?")
      "rules/software/team.md", Regex(@"(?i)rules/software/team\.md") ]

let removedRouteMatches (content: string) =
    removedRoutePatterns
    |> List.choose (fun (route, pattern) -> if pattern.IsMatch content then Some route else None)

for file in liveInfrastructureFiles do
    let content = File.ReadAllText file
    for route in removedRouteMatches content do
        error "removed-route-reference" (relativePath file) $"Live infrastructure references removed route '{route}'"

    for obsolete in obsoleteLiveMatches content do
        error "obsolete-reference" (relativePath file) $"Live infrastructure references obsolete identifier '{obsolete}'"

let assignedCapabilities =
    seq {
        for path, capability, _ in expectedAgentAssignments do
            let fullPath = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))
            if File.Exists fullPath then yield capability

        if File.Exists configPath then
            try
                use document = JsonDocument.Parse(File.ReadAllText configPath)
                match tryProperty "model" document.RootElement with
                | Some model when model.ValueKind = JsonValueKind.String ->
                    yield! capabilityOfModel (model.GetString()) |> Option.toList
                | _ -> ()

                match tryProperty "small_model" document.RootElement with
                | Some model when model.ValueKind = JsonValueKind.String ->
                    yield! capabilityOfModel (model.GetString()) |> Option.toList
                | _ -> ()
            with _ ->
                ()
    }
    |> Set.ofSeq

for missing in Set.difference allCapabilities assignedCapabilities do
    error "production-model" "agents" $"Required model capability '{capabilityName missing}' is not assigned"

let normalizedRoot = root.Replace("\\", "/").TrimEnd('/')
let knownRouteRoots = [ "agents/"; "commands/"; "lib/"; "plugins/"; "references/"; "rules/"; "scripts/"; "skills/" ]

let normalizeRoute (token: string) =
    let mutable value = token.Trim().TrimStart('@').Replace("\\", "/")
    let globalPrefix = "~/.config/opencode/"

    if value.StartsWith(normalizedRoot + "/", StringComparison.OrdinalIgnoreCase) then
        value <- value.Substring(normalizedRoot.Length + 1)
    elif value.StartsWith(globalPrefix, StringComparison.OrdinalIgnoreCase) then
        value <- value.Substring(globalPrefix.Length)

    value.TrimStart('/')

let isConcreteRoute (route: string) =
    let hasKnownRoot = knownRouteRoots |> List.exists (fun prefix -> route.StartsWith(prefix, StringComparison.Ordinal))
    let isRootFile = route = "AGENTS.md" || route = "README.md" || route = "opencode.json"
    let hasFileExtension =
        [ ".md"; ".fsx"; ".js"; ".mjs"; ".json" ]
        |> List.exists (fun extension -> route.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
    (hasKnownRoot || isRootFile)
    && hasFileExtension
    && not (Regex.IsMatch(route, @"[\s*{}<>$]"))

for file in markdownFiles do
    for line in File.ReadAllLines file do
        for matched in Regex.Matches(line, @"`(?<route>[^`]+)`") |> Seq.cast<Match> do
            let route = normalizeRoute matched.Groups.["route"].Value
            if isConcreteRoute route then
                let candidateBases =
                    [ Path.Combine(root, route)
                      Path.Combine(Directory.GetParent(file).FullName, route)
                      Path.Combine(Directory.GetParent(Directory.GetParent(file).FullName).FullName, route) ]

                let candidates =
                    candidateBases
                    |> List.choose (fun candidate ->
                        try
                            let fullPath = Path.GetFullPath candidate
                            let relative = Path.GetRelativePath(root, fullPath).Replace("\\", "/")
                            if not (Path.IsPathRooted relative)
                               && relative <> ".."
                               && not (relative.StartsWith("../", StringComparison.Ordinal)) then
                                Some fullPath
                            else
                                None
                        with _ ->
                            None)

                if candidates |> List.exists File.Exists |> not then
                    error "stale-route" (relativePath file) $"Referenced path '{route}' does not exist"

let normalizeParagraph (lines: string list) =
    lines
    |> String.concat " "
    |> fun value -> Regex.Replace(value.ToLowerInvariant(), @"\s+", " ").Trim()

let concreteRouteMatches (line: string) =
    Regex.Matches(line, @"`(?<route>[^`]+)`")
    |> Seq.cast<Match>
    |> Seq.filter (fun matched ->
        matched.Groups.["route"].Value
        |> normalizeRoute
        |> isConcreteRoute)
    |> Seq.toList

let maxRoutingResidualLength = 180

let isRoutingLine (line: string) =
    let routeMatches = concreteRouteMatches line
    let residual =
        routeMatches
        |> List.fold (fun (text: string) (matched: Match) -> text.Replace(matched.Value, "")) line
        |> fun text -> Regex.Replace(text, @"\s+", " ").Trim()

    not routeMatches.IsEmpty
    && residual.Length <= maxRoutingResidualLength
    && Regex.IsMatch(
        line.TrimStart().TrimStart('-').TrimStart(),
        @"^(Load|Use|Follow|Cross-check|If\b|For\b)",
        RegexOptions.IgnoreCase)

let isRouteDominant lines =
    match lines with
    | [ line ] -> isRoutingLine line
    | _ when lines |> List.forall (fun line -> line.TrimStart().StartsWith("-")) ->
        lines |> List.forall isRoutingLine
    | _ -> false

let paragraphs file =
    let results = ResizeArray<string>()
    let current = ResizeArray<string>()
    let mutable fenced = false

    let flush () =
        let normalized = current |> Seq.toList |> normalizeParagraph
        let lines = current |> Seq.toList
        if normalized.Length >= 180 && not (isRouteDominant lines) then results.Add normalized
        current.Clear()

    for line in File.ReadAllLines file do
        let trimmed = line.Trim()
        if trimmed.StartsWith("```") then
            flush ()
            fenced <- not fenced
        elif fenced || String.IsNullOrWhiteSpace trimmed || trimmed.StartsWith("#") || trimmed.StartsWith("|") then
            flush ()
        elif trimmed <> "---" && not (Regex.IsMatch(trimmed, @"^[A-Za-z_][A-Za-z0-9_-]*:\s")) then
            current.Add trimmed

    flush ()
    results |> Seq.toList

let fencedBlocks file =
    let results = ResizeArray<string>()
    let current = ResizeArray<string>()
    let fenceRegex = Regex(@"^\s*(?<fence>`{3,}|~{3,})")
    let mutable openingFence: string option = None

    let flush () =
        let normalized = current |> Seq.toList |> normalizeParagraph
        if normalized.Length >= 180 then results.Add normalized
        current.Clear()

    for line in File.ReadAllLines file do
        let matched = fenceRegex.Match line
        match openingFence with
        | None when matched.Success ->
            openingFence <- Some matched.Groups.["fence"].Value
            current.Clear()
        | Some opening when matched.Success ->
            let candidate = matched.Groups.["fence"].Value
            if candidate.[0] = opening.[0] && candidate.Length >= opening.Length then
                flush ()
                openingFence <- None
            else
                current.Add line
        | Some _ -> current.Add line
        | None -> ()

    if openingFence.IsSome then
        error "markdown-fence" (relativePath file) "Unclosed fenced block"

    results |> Seq.toList

markdownFiles
|> Seq.collect (fun file -> paragraphs file |> Seq.map (fun paragraph -> paragraph, relativePath file))
|> Seq.groupBy fst
|> Seq.iter (fun (_, occurrences) ->
    let paths = occurrences |> Seq.map snd |> Seq.distinct |> Seq.sort |> Seq.toList
    if paths.Length > 1 then
        warning "duplicate-prose" (String.concat ", " paths) "Exact normalized prose appears in multiple documents; verify one canonical owner")

markdownFiles
|> Seq.collect (fun file -> fencedBlocks file |> Seq.map (fun block -> block, relativePath file))
|> Seq.groupBy fst
|> Seq.iter (fun (_, occurrences) ->
    let paths = occurrences |> Seq.map snd |> Seq.distinct |> Seq.sort |> Seq.toList
    if paths.Length > 1 then
        warning "duplicate-template" (String.concat ", " paths) "Exact normalized fenced template appears in multiple documents; keep one canonical owner")

let orderedFindings =
    findings
    |> Seq.sortBy (fun finding ->
        match finding.Severity with
        | Error -> 0, finding.Path, finding.Code
        | Warning -> 1, finding.Path, finding.Code)
    |> Seq.toList

for finding in orderedFindings do
    let label = if finding.Severity = Error then "ERROR" else "WARN"
    printfn "[%s] %s %s: %s" label finding.Code finding.Path finding.Message

let errorCount = orderedFindings |> List.filter (fun finding -> finding.Severity = Error) |> List.length
let warningCount = orderedFindings.Length - errorCount

if errorCount = 0 then
    printfn "OK infrastructure validation (%d warning(s))" warningCount
    exit 0
else
    eprintfn "FAILED infrastructure validation (%d error(s), %d warning(s))" errorCount warningCount
    exit 1
