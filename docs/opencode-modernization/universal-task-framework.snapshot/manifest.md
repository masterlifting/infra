# Universal Task Framework Snapshot Manifest

**Snapshot date:** 2026-09-09  
**Source:** `universal-task-framework-proposal-final.md`  
**Full SHA-256:** `d14ab3779fe51728728a9925145d34e791b0d5e23f870f8fbcc8ee02239c085c`  
**Full byte length (UTF-8):** 92055  
**Full line count:** 2677

The accepted proposal is preserved losslessly in seven sequential parts because the GitHub connector used for this migration could not directly upload the existing local 92 KB file as one blob.

Reconstruction order:

1. `part-01.md` — lines 1–383 — SHA-256 `7db333ecb7ebfce5b190e2d46091e98a6b88d51c03d46e6d0c2f51b93999761b`
2. `part-02.md` — lines 384–766 — SHA-256 `a7ec2d50f4644abe46b9d6551eb9a31a8bd148414182866bed1095620922a826`
3. `part-03.md` — lines 767–1149 — SHA-256 `58a55045f4791c99727f05070422ee21ea5c0087097e2083c1e80be175a12c21`
4. `part-04.md` — lines 1150–1532 — SHA-256 `7f0a93655d2e0e74b981f1e9fffda279eaf6bdc5d434265510d3c21489f78813`
5. `part-05.md` — lines 1533–1915 — SHA-256 `a1925e21adb170376403e505aa6249053e3795dbc7a74d129c443888f364a539`
6. `part-06.md` — lines 1916–2298 — SHA-256 `eee759d9bd6181d66b7ac62cc093f140ac3d6cf501294078ecb5ea9f5b5fd2a2`
7. `part-07.md` — lines 2299–2677 — SHA-256 `85343fa57900519768402a8dbbb84d81de86c62827cd83737a191eb18d5e66f0`

Reconstruction:

```bash
cat part-01.md part-02.md part-03.md part-04.md part-05.md part-06.md part-07.md > universal-task-framework.md
sha256sum universal-task-framework.md
```

The resulting hash must equal:

`d14ab3779fe51728728a9925145d34e791b0d5e23f870f8fbcc8ee02239c085c`

During #9 cutover, reconstruct/commit the single canonical destination file (recommended conceptual path `docs/architecture/universal-task-framework.md`) and verify this hash before declaring the migration complete.

After a verified destination copy exists, these parts remain historical preservation evidence and must not become a parallel editable architecture source.
