open System
open System.Collections.Generic
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
      "opencode.json"
      "agents"
      "commands"
      "lib"
      "plugins"
      "rules"
      "scripts"
      "skills" ]

for path in requiredPaths do
    let fullPath = Path.Combine(root, path)
    if not (File.Exists fullPath || Directory.Exists fullPath) then
        error "required-path" path "Required infrastructure path does not exist"

let tryProperty (name: string) (element: JsonElement) =
    let mutable value = Unchecked.defaultof<JsonElement>
    if element.TryGetProperty(name, &value) then Some value else None

let configPath = Path.Combine(root, "opencode.json")

if File.Exists configPath then
    try
        use document = JsonDocument.Parse(File.ReadAllText configPath)
        let config = document.RootElement

        match tryProperty "$schema" config with
        | Some schema when schema.GetString() = "https://opencode.ai/config.json" -> ()
        | _ -> error "config-schema" "opencode.json" "Missing or unexpected $schema value"

        if tryProperty "agent" config |> Option.isSome then
            error "inline-agents" "opencode.json" "Agent definitions must remain file-based under agents/"

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
