# OpenCode Modernization — Static Preservation Package

This directory preserves the accepted architecture, migration decisions, implementation order, and backlog-alignment evidence for the OpenCode modernization program.

It exists so the modernization work does **not** depend on a ChatGPT session, local temporary files, or human memory.

## Contents

- `universal-task-framework.snapshot/manifest.md` + `part-01.md` … `part-07.md` — lossless accepted Universal Task Framework proposal snapshot.
- `backlog-migration-ledger.md` — ownership, superseded/retained semantics, applied alignment, #9 cutover obligations.
- `implementation-roadmap.md` — mandatory implementation order and issue ownership.
- `backlog-alignment-evidence.md` — issue/comment and Git integrity evidence.

## Integrity

Reconstructed proposal SHA-256:

`d14ab3779fe51728728a9925145d34e791b0d5e23f870f8fbcc8ee02239c085c`

The seven snapshot part Git blobs were verified against the accepted local proposal and total exactly 92,055 bytes.

Integrity snapshot commit:

`7ddcc2c8bf69e7de98aa4b340aa9526537f5b4c9`

Later documentation-only commits strengthen references/evidence; proposal snapshot bytes are unchanged and remain protected by the SHA-256.

## Canonicality

Before #9 cutover:

- GitHub issues are the actionable backlog;
- this directory is the static architecture/evidence preservation package;
- the proposal snapshot owns detailed #12 architecture semantics.

During #9 cutover, this directory is mandatory migration input. #9 must reconstruct/verify the proposal, migrate the aligned backlog, and fold temporary alignment comments into destination canonical issue bodies.

After #9 cutover, the destination shared OpenCode repository becomes canonical and this `infra` package becomes historical evidence.

## Mandatory implementation order

```text
#9 -> #10 -> #11 -> #12 -> #13 -> #14 -> #15 -> #17
```
