# Behavioral Specification

## Requirement: Owned task scratch

Temporary task artifacts MUST be created below a machine-local, canonical task
scratch root. The root MUST be isolated by valid task and run identifiers,
outside the repository, and recorded in a versioned ownership manifest.

### Scenario: Create an owned scratch root

Given a valid task and run identifier
When scratch is created
Then the helper creates a deterministic OS-temp-based root and a valid manifest
that identifies that task, run, and root.

## Requirement: Provenance-based cleanup

Only registered, non-promoted artifacts in a valid owned scratch root MAY be
deleted automatically at task close. Unknown, malformed, escaped, reparse, or
otherwise ambiguous material MUST be retained and reported.

### Scenario: Clean eligible owned scratch

Given a completed task with a valid manifest and a registered disposable file
When closing cleanup runs
Then it deletes that file without per-file confirmation and reports the result.

### Scenario: Preserve unsafe or unresolved scratch

Given scratch that is unregistered, malformed, escaped, reparse-linked, or
needed by an incomplete task or verification
When cleanup runs
Then it retains the material and reports why.

## Requirement: Explicit durable promotion

An owned scratch artifact becomes durable only through explicit promotion to the
current task's `docs/` or `scripts/` namespace. The durable copy MUST survive
scratch cleanup.

### Scenario: Promote evidence

Given a registered owned scratch artifact and an allowed task docs destination
When it is explicitly promoted
Then the helper verifies ownership and destination, copies the artifact, and
marks the source as promoted in the manifest.

## Requirement: No inferred or broad cleanup

The mechanism MUST NOT infer ownership from names, age, timestamps, git state,
or apparent usefulness. It MUST NOT clean repository source, durable task
records, project-native build outputs, arbitrary system-temp files, or broad
Git worktrees.
