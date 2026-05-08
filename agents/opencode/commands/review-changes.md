---
description: Review current git changes for bugs, regressions, and missing tests.
agent: plan
---

Review the current working tree changes with a code-review mindset.

Use git status and git diff as needed. Prioritize concrete findings over summaries. Do not edit files.

Report findings first, ordered by severity, using `file:line severity: problem. fix.` If there are no findings, say that explicitly and list residual risks or missing verification.

Arguments: $ARGUMENTS
