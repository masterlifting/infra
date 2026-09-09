# OpenCode Modernization — Static Preservation Package

This directory preserves the accepted architecture, migration decisions, implementation order, and backlog-alignment evidence for the OpenCode modernization program.

It exists so the modernization work does **not** depend on a ChatGPT session, local temporary files, or human memory.

## Files

- [`universal-task-framework.snapshot/manifest.md`](./universal-task-framework.snapshot/manifest.md)  
  Lossless SHA-256-verifiable snapshot of the accepted Universal Task Framework proposal, split into seven sequential parts solely because the GitHub connector could not upload the existing 92 KB local file directly as one blob.

- [`backlog-migration-ledger.md`](./backlog-migration-ledger.md)  
  Ownership matrix, retained/superseded requirements, applied issue alignment, and #9 cutover obligations.

- [`implementation-roadmap.md`](./implementation-roadmap.md)  
  Canonical implementation order and issue ownership boundaries.

- [`backlog-alignment-evidence.md`](./backlog-alignment-evidence.md)  
  Static evidence snapshot: artifact hashes, issue replacements, addendum comment IDs, verified invariants, and post-write Git blob integrity checks.

## Integrity

The reconstructed accepted proposal must have SHA-256:

`d14ab3779fe51728728a9925145d34e791b0d5e23f870f8fbcc8ee02239c085c`

All seven GitHub snapshot part blobs were verified against Git blob SHA values computed from the local accepted proposal source. See `backlog-alignment-evidence.md` and the snapshot manifest.

An integrity snapshot of the complete package existed at commit:

`7ddcc2c8bf69e7de98aa4b340aa9526537f5b4c9`

Later documentation-only commits strengthened cross-references/evidence while leaving the proposal snapshot bytes unchanged. The proposal SHA-256 above remains the canonical architecture-integrity check.

## Current canonicality

Before #9 cutover:

- GitHub issues remain the actionable backlog;
- this directory is the static evidence/architecture preservation package;
- the proposal snapshot owns detailed #12 architecture semantics and is protected by the manifest SHA-256.

During #9 cutover:

- use this directory as mandatory migration input;
- reconstruct the proposal into one canonical destination file, recommended conceptually as `docs/architecture/universal-task-framework.md`;
- verify the reconstructed SHA-256 against the snapshot manifest;
- migrate the ledger/roadmap/evidence package as useful historical evidence;
- fold temporary infra issue addenda into destination canonical issue bodies.

After #9 cutover:

- destination repository architecture/issue copies become canonical;
- this `infra` directory remains historical migration evidence;
- do not maintain two independent editable architecture/backlog copies.

## Mandatory implementation order

```text
#9 -> #10 -> #11 -> #12 -> #13 -> #14 -> #15 -> #17
```

See `implementation-roadmap.md` for ownership and transition details.
