# Migration Progress

Status: Complete
Frozen specification: `migration/solution.md`
Review state: FROZEN
Automatic remediation pass: 1 of 2 for final batch

## Acceptance Matrix

| Concern | Frozen contract |
| --- | --- |
| Capability inventory | `/task` owns resumable product/software work; `/audit-infra` owns session, harness, and project harness work. |
| Target identity | Global harness at `C:/Users/andre/.config/opencode`; preserve pre-existing worktree edits. |
| State boundaries | `NEW -> DISCOVERY -> REMEDIATION -> VERIFICATION -> FROZEN`; Discovery runs once per frozen solution and implementation baseline. Pagination is not applicable. |
| Concurrency and idempotency | Independent agents may run in parallel; coordinator decisions are singular; repeated generic review maps to Verification and must not create another finding set. |
| Effect boundaries | The primary coordinator owns decisions and edits; `audit-infra` remains read-only; external writes, commits, pushes, destructive actions, and each infrastructure batch require explicit confirmation. |
| Verification and receipts | Record baseline identity, accepted finding IDs and contracts, remediation diff, applicable requirements, build/test evidence, pass count, and `FIXED`, `NOT FIXED`, or `REGRESSION INTRODUCED` results here or in `TASK.md`. |
| Resource limits | Batches contain at most 10 files; Discovery occurs once; automatic remediation is limited to two passes; heuristic validation emits warnings. |
| Failure paths | Pass 2 may address an unresolved blocking accepted finding or a blocking regression introduced by pass 1. Stop and report any blocker remaining after pass 2. |
| Acceptance scenarios | Obsolete routes are removed; orchestration has one owner; tests precede Discovery; finding sets freeze; deterministic validation and targeted Verification complete. |

## Decisions

- Treat `TASK.md` as the durable receipt for `/task` and this progress/session record as the receipt for this `/audit-infra` migration.
- Keep the `audit-infra` subagent read-only; the primary coordinator delegates implementation, build, and test work to applicable owners.
- Apply the global validator to global harness structure. For project scope, run deterministic checks supported by the project's actual schema and record any unavailable check as residual risk.
- The user approved batch 1 on 2026-08-11.

## Batches

1. [x] Canonical workflow ownership: global constraints, `/task`, `/audit-infra`, review convergence, and obsolete team-rule removal.
2. [x] Durable task-state contract and F# task scripts.
3. [x] Reviewer role templates.
4. [x] Reviewer routing identities.
5. [x] Deterministic migration enforcement.

## Evidence

- Architecture gate: Architect 2 approved. Architect 1 raised four ambiguities; the coordinator resolved them in the matrix and decisions above without reopening the frozen architecture.
- Verification pass 1: B1-F1, B1-F2, B1-F3, B1-F4, B1-F5, and B1-F6 fixed; operational verification found that `/audit-infra` did not explicitly require per-batch confirmation.
- Remediation pass 1: added the missing explicit per-batch confirmation requirement to `skills/audit-infra/SKILL.md`.
- Targeted re-verification: the per-batch confirmation finding is `FIXED`; no blocking regression was introduced.
- Validation: `npm run validate:infra` passed with 0 warnings; `npm run test:task` passed; `npm run test:safety` passed (38 cases, 18 patterns); `git diff --check` passed.
- Batch 2 implementation: added concise frozen solution, review state/profile, baseline, finding, remediation-pass, evidence, and verification receipt state to generated tasks and deterministic validation.
- Batch 2 Discovery: accepted B2-F1 through B2-F4 plus directly related negative-test gaps; architecture-conformity review approved.
- Batch 2 remediation pass 1: all production findings fixed and `npm run test:task` passed; targeted Verification found one missing out-of-domain result test.
- Batch 2 remediation pass 2: added the missing `BOGUS` result case; `npm run test:task` passed and final targeted Verification returned `FIXED`.
- Batch 2 structural validation: `npm run validate:infra` passed with 0 warnings and `git diff --check` passed.
- Batch 3: shared C#, F#, and Rust reviewer templates now require the assigned independent mandate while delegating convergence semantics to canonical `rules/software/review.md`.
- Batch 3 remediation: removed duplicated convergence prose after deterministic validation warned about three copies; targeted re-verification kept B3-F1 through B3-F5 `FIXED`.
- Batch 3 validation: `npm run validate:infra` passed with 0 warnings and `git diff --check` passed.
- Batch 4: all nine language reviewer identities now have distinct correctness, frozen-architecture conformity, or contracts/testing mandates; model and permission settings were preserved.
- Batch 4 verification: B4-F1 through B4-F5 are `FIXED`; `npm run validate:infra` passed with 0 warnings and `git diff --check` passed.
- Batch 5: the infrastructure validator now rejects removed surfaces/routes, prohibited orchestration agents, missing canonical convergence markers, indistinct reviewer mandates, and reliable task template/reference drift.
- Batch 5 remediation pass 1: accepted B5-F1 for missing validator negative coverage; added built-in `--self-test` cases that share production helpers. Targeted Verification returned `FIXED`.
- Final validation: validator `--self-test`, `npm run validate:infra`, `npm run test:task`, `npm run test:safety`, and `git diff --check` all passed; infrastructure validation reported 0 warnings and safety tests passed 38 cases across 18 patterns.

## Next

Migration complete. Restart OpenCode to load the updated harness definitions.
