#r "nuget: ClosedXML, 0.105.0"
#r "nuget: DocumentFormat.OpenXml, 3.3.0"
#r "nuget: CsvHelper, 33.1.0"

open System
open System.Globalization
open System.IO
open System.Text
open System.Text.Json
open System.Text.RegularExpressions
open ClosedXML.Excel
open CsvHelper
open CsvHelper.Configuration
open DocumentFormat.OpenXml
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml.Wordprocessing

type Operation =
    | MarkdownToDocx
    | DocxToMarkdown
    | CsvToXlsx
    | JsonToXlsx
    | XlsxToCsv
    | XlsxToJson

let usage () =
    eprintfn "usage: OfficeDocuments.fsx <md-to-docx|docx-to-md|csv-to-xlsx|json-to-xlsx|xlsx-to-csv|xlsx-to-json> --input <path> --output <path> [--sheet <name>]"

let parseOperation = function
    | "md-to-docx" -> Some MarkdownToDocx
    | "docx-to-md" -> Some DocxToMarkdown
    | "csv-to-xlsx" -> Some CsvToXlsx
    | "json-to-xlsx" -> Some JsonToXlsx
    | "xlsx-to-csv" -> Some XlsxToCsv
    | "xlsx-to-json" -> Some XlsxToJson
    | _ -> None

let tryArgument key (args: string array) =
    args
    |> Array.tryFindIndex ((=) key)
    |> Option.bind (fun index -> args |> Array.tryItem (index + 1))

let requirePath kind path =
    if String.IsNullOrWhiteSpace path then invalidArg kind $"{kind} path is required"
    Path.GetFullPath path

let ensureInput path =
    if not (File.Exists path) then raise (FileNotFoundException("Input file not found", path))

let ensureOutputDirectory (path: string) =
    match Path.GetDirectoryName path with
    | directory when not (String.IsNullOrWhiteSpace directory) -> Directory.CreateDirectory directory |> ignore
    | _ -> ()

let temporaryOutputPath (output: string) =
    let directory = Path.GetDirectoryName output
    let name = Path.GetFileNameWithoutExtension output
    let extension = Path.GetExtension output
    Path.Combine(directory, $".{name}.{Guid.NewGuid():N}.tmp{extension}")

let inlineRuns (text: string) =
    let paragraph = Paragraph()
    let pattern = Regex(@"(\*\*[^*]+\*\*|`[^`]+`)")
    let mutable position = 0

    let addRun value bold code =
        let properties = RunProperties()
        if bold then properties.AppendChild(Bold()) |> ignore
        if code then properties.AppendChild(RunFonts(Ascii = "Consolas", HighAnsi = "Consolas")) |> ignore
        let run = Run()
        run.AppendChild properties |> ignore
        run.AppendChild(Text(value, Space = SpaceProcessingModeValues.Preserve)) |> ignore
        paragraph.AppendChild run

    for matched: Match in pattern.Matches text do
        if matched.Index > position then addRun text.[position .. matched.Index - 1] false false |> ignore
        let token = matched.Value
        if token.StartsWith '`' then
            addRun token.[1 .. token.Length - 2] false true |> ignore
        else
            addRun token.[2 .. token.Length - 3] true false |> ignore
        position <- matched.Index + matched.Length

    if position < text.Length then addRun text.[position..] false false |> ignore
    paragraph

let markdownToDocx (input: string) (output: string) =
    use document = WordprocessingDocument.Create(output, WordprocessingDocumentType.Document)
    let main = document.AddMainDocumentPart()
    main.Document <- Document()
    main.Document.AppendChild(Body()) |> ignore
    let body = main.Document.Body

    for line in File.ReadLines input do
        let heading = Regex.Match(line, @"^(#{1,6})\s+(.+)$")
        let bullet = Regex.Match(line, @"^\s*[-*+]\s+(.+)$")
        let numbered = Regex.Match(line, @"^\s*(\d+[.)])\s+(.+)$")

        let paragraph =
            if heading.Success then
                let p = inlineRuns heading.Groups.[2].Value
                let properties = ParagraphProperties()
                properties.AppendChild(ParagraphStyleId(Val = $"Heading{heading.Groups.[1].Value.Length}")) |> ignore
                p.ParagraphProperties <- properties
                p
            elif bullet.Success || numbered.Success then
                let marker = if bullet.Success then "• " else numbered.Groups.[1].Value + " "
                inlineRuns (marker + (if bullet.Success then bullet.Groups.[1].Value else numbered.Groups.[2].Value))
            else inlineRuns line

        body.AppendChild paragraph |> ignore

    main.Document.Save()

let paragraphToMarkdown (paragraph: Paragraph) =
    let text = paragraph.InnerText
    let style =
        Option.ofObj paragraph.ParagraphProperties
        |> Option.bind (fun properties -> Option.ofObj properties.ParagraphStyleId)
        |> Option.bind (fun value -> Option.ofObj value.Val)
        |> Option.map string

    match style with
    | Some value when value.StartsWith("Heading", StringComparison.OrdinalIgnoreCase) ->
        match Int32.TryParse(value.Substring("Heading".Length)) with
        | true, level -> String.replicate (min 6 level) "#" + " " + text
        | _ -> text
    | _ when text.StartsWith("• ", StringComparison.Ordinal) -> "- " + text.Substring 2
    | _ -> text

let docxToMarkdown (input: string) (output: string) =
    use document = WordprocessingDocument.Open(input, false)
    let body = document.MainDocumentPart.Document.Body
    body.Elements<Paragraph>()
    |> Seq.map paragraphToMarkdown
    |> fun lines -> File.WriteAllLines(output, lines, UTF8Encoding(false))

let csvRows (path: string) =
    use reader = new StreamReader(path, Encoding.UTF8, true)
    let config = CsvConfiguration(CultureInfo.InvariantCulture, HasHeaderRecord = true)
    use csv = new CsvReader(reader, config)
    if not (csv.Read()) then [||], [||]
    else
        csv.ReadHeader() |> ignore
        let headers = csv.HeaderRecord |> Option.ofObj |> Option.defaultValue [||]
        let rows = ResizeArray<string array>()
        while csv.Read() do
            if csv.Parser.Count <> headers.Length then invalidArg "input" $"CSV row {csv.Parser.Row} has {csv.Parser.Count} fields; expected {headers.Length}"
            rows.Add(headers |> Array.mapi (fun index _ -> csv.GetField index))
        headers, rows.ToArray()

let writeWorkbook (output: string) (sheetName: string) (headers: string array) (rows: string array array) =
    use workbook = new XLWorkbook()
    let sheet = workbook.Worksheets.Add sheetName
    headers |> Array.iteri (fun column value -> sheet.Cell(1, column + 1).Value <- value)
    rows |> Array.iteri (fun row values -> values |> Array.iteri (fun column value -> sheet.Cell(row + 2, column + 1).Value <- value))
    if headers.Length > 0 then
        sheet.Range(1, 1, 1, headers.Length).Style.Font.Bold <- true
        sheet.SheetView.FreezeRows 1
        sheet.Columns().AdjustToContents() |> ignore
    workbook.SaveAs output
    rows.Length

let csvToXlsx (input: string) (output: string) (sheetName: string) =
    let headers, rows = csvRows input
    writeWorkbook output sheetName headers rows

let jsonValue (value: JsonElement) =
    match value.ValueKind with
    | JsonValueKind.Null -> ""
    | JsonValueKind.String -> value.GetString() |> Option.ofObj |> Option.defaultValue ""
    | _ -> value.GetRawText()

let jsonToXlsx (input: string) (output: string) (sheetName: string) =
    use json = JsonDocument.Parse(File.ReadAllText input)
    if json.RootElement.ValueKind <> JsonValueKind.Array then invalidArg "input" "JSON input must be an array of objects"
    let objects = json.RootElement.EnumerateArray() |> Seq.toArray
    if objects |> Array.exists (fun item -> item.ValueKind <> JsonValueKind.Object) then invalidArg "input" "Every JSON array item must be an object"
    let headers = objects |> Seq.collect (fun item -> item.EnumerateObject() |> Seq.map _.Name) |> Seq.distinct |> Seq.toArray
    let rows = objects |> Array.map (fun item -> headers |> Array.map (fun header -> match item.TryGetProperty header with | true, value -> jsonValue value | _ -> ""))
    writeWorkbook output sheetName headers rows

let worksheet (input: string) (sheetName: string) =
    let workbook = new XLWorkbook(input)
    let sheet =
        if String.IsNullOrWhiteSpace sheetName then workbook.Worksheet 1
        else workbook.Worksheet sheetName
    workbook, sheet

let usedValues (sheet: IXLWorksheet) =
    match Option.ofObj (sheet.RangeUsed()) with
    | None -> [||], [||]
    | Some range ->
        let values = range.Rows() |> Seq.map (fun row -> row.Cells() |> Seq.map _.GetFormattedString() |> Seq.toArray) |> Seq.toArray
        if values.Length = 0 then [||], [||] else values.[0], values.[1..]

let xlsxToCsv (input: string) (output: string) (sheetName: string) =
    let workbook, sheet = worksheet input sheetName
    use workbook = workbook
    let headers, rows = usedValues sheet
    use writer = new StreamWriter(output, false, UTF8Encoding(false))
    use csv = new CsvWriter(writer, CultureInfo.InvariantCulture)
    for header in headers do csv.WriteField header
    csv.NextRecord()
    for row in rows do
        for value in row do csv.WriteField value
        csv.NextRecord()
    rows.Length

let xlsxToJson (input: string) (output: string) (sheetName: string) =
    let workbook, sheet = worksheet input sheetName
    use workbook = workbook
    let headers, rows = usedValues sheet
    if headers |> Array.exists String.IsNullOrWhiteSpace then invalidArg "input" "XLSX headers must not be blank"
    if (headers |> Array.distinct).Length <> headers.Length then invalidArg "input" "XLSX headers must be unique"
    let records = rows |> Array.map (fun row -> headers |> Array.mapi (fun index header -> header, (row |> Array.tryItem index |> Option.defaultValue "")) |> Map.ofArray)
    File.WriteAllText(output, JsonSerializer.Serialize(records, JsonSerializerOptions(WriteIndented = true)), UTF8Encoding(false))
    rows.Length

let args = fsi.CommandLineArgs |> Array.skip 1
let mutable createdOutput: string option = None

try
    if args.Length = 0 then usage (); exit 2
    let operation = parseOperation args.[0] |> Option.defaultWith (fun () -> usage (); invalidArg "operation" $"Unsupported operation: {args.[0]}")
    let input = tryArgument "--input" args |> Option.defaultValue "" |> requirePath "input"
    let output = tryArgument "--output" args |> Option.defaultValue "" |> requirePath "output"
    let sheet = tryArgument "--sheet" args |> Option.defaultValue "Sheet1"
    ensureInput input
    if File.Exists output then invalidArg "output" $"Output file already exists: {output}"
    ensureOutputDirectory output
    let temporaryOutput = temporaryOutputPath output
    createdOutput <- Some temporaryOutput

    let rowCount =
        match operation with
        | MarkdownToDocx -> markdownToDocx input temporaryOutput; None
        | DocxToMarkdown -> docxToMarkdown input temporaryOutput; None
        | CsvToXlsx -> Some(csvToXlsx input temporaryOutput sheet)
        | JsonToXlsx -> Some(jsonToXlsx input temporaryOutput sheet)
        | XlsxToCsv -> Some(xlsxToCsv input temporaryOutput (tryArgument "--sheet" args |> Option.defaultValue ""))
        | XlsxToJson -> Some(xlsxToJson input temporaryOutput (tryArgument "--sheet" args |> Option.defaultValue ""))

    File.Move(temporaryOutput, output)
    createdOutput <- None

    match rowCount with
    | Some count -> printfn "created: %s (%d data rows)" output count
    | None -> printfn "created: %s" output
with error ->
    createdOutput |> Option.iter (fun path -> if File.Exists path then File.Delete path)
    eprintfn "error: %s" error.Message
    exit 1
