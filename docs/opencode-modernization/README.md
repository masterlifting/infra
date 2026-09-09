# OpenCode Modernization — Static Preservation Package

This directory preserves the accepted architecture, migration decisions, implementation order, and backlog-alignment evidence for the OpenCode modernization program.

It exists so the modernization work does **not** depend on a ChatGPT session, local temporary files, or human memory.

## Files

- [`universal-task-framework.snapshot/manifest.md`](./universal-task-framework.snapshot/manifest.md)  
  Lossless SHA-256-verifiable snapshot of the accepted Universal Task Framework proposal, split into seven sequential parts solely because the GitHub connector could not upload the existing 92 KB local file directly as one blob.

- [`backlog-migration-ledger.md`](./backlog-migration-ledger.md)  
  Ownership matrix, retained/superseded requirements, issue migration plan, and record of applied backlog changes.

- [`implementation-roadmap.md`](./implementation-roadmap.md)  
  Canonical implementation order and issue ownership boundaries.

- [`backlog-alignment-evidence.md`](./backlog-alignment-evidence.md)  
  Static evidence snapshot: artifact hashes, issue replacements, addendum comment IDs, and verified invariants.

## Current canonicality

Before #9 cutover:

- GitHub issues remain the actionable backlog;
- this directory is the static evidence/architecture preservation package;
- the proposal snapshot owns detailed #12 architecture semantics and is protected by the manifest SHA-256.

During #9 cutover:

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
