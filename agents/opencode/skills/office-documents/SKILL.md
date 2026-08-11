---
name: office-documents
description: Use when reading, creating, or converting simple local DOCX and XLSX files, including Markdown-to-DOCX and CSV/JSON-to-XLSX workflows for ONLYOFFICE. Do not use for pixel-perfect layouts, macros, recalculated formulas, complex embedded content, or desktop UI automation.
---

# Office Documents

## Purpose

Create and extract simple local Office documents that open in ONLYOFFICE without controlling the desktop application.

## Guardrails

- Keep all document content local unless the user explicitly approves an external transfer.
- The converter requires an explicit permission prompt before execution because its pinned NuGet packages may contact configured feeds and write to the global package cache during restoration.
- Treat conversion as content-oriented and text-based: preserve headings, paragraphs, inline bold/code when creating DOCX, and flat lists represented by visible markers, not exact layout or native Word list semantics.
- Treat spreadsheet values as text. CSV input requires a header row; JSON input requires an array of objects; XLSX export uses the first used row as headers.
- Do not claim support for macros, tracked changes, embedded media, charts, pivot tables, or formula recalculation.
- The converter never overwrites an output. Confirm and remove an existing file separately, or choose a new output path.

## Workflow

1. Determine the conversion and paths. Supported operations are `md-to-docx`, `docx-to-md`, `csv-to-xlsx`, `json-to-xlsx`, `xlsx-to-csv`, and `xlsx-to-json`.
2. For JSON input, require an array of objects. Object keys become spreadsheet headers.
3. Confirm the exact conversion command and that the output does not already exist, then request permission to run:

   ```powershell
   dotnet fsi "C:/Users/andre/.config/opencode/skills/office-documents/scripts/OfficeDocuments.fsx" <operation> --input "<input>" --output "<output>" [--sheet "<sheet>"]
   ```

4. Report the output path, operation, row count where applicable, and any unsupported content that may have been simplified.

## Output

- Return a concise success or error summary with the absolute output path.
- When reading a document, summarize or use the extracted Markdown, CSV, or JSON according to the user's request.
