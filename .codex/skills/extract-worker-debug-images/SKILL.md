---
name: extract-worker-debug-images
description: Extract debug screenshots from a remote Docker container to the local machine. Use when asked to list, copy, or bulk export image files from `embassy-access-worker` (usually `/app/debug`) through a Docker SSH context such as `vps-root`.
---

# Extract Worker Debug Images

Use the bundled PowerShell script to copy images from the remote container to a local folder.

## Defaults
- Context: `vps-root`
- Container: `embassy-access-worker`
- Remote directory: `/app/debug`
- Pattern: `*.png`
- Local destination: `.vps/embassy-access/debug`

## Workflow
1. Validate remote access:
   - `docker --context vps-root ps`
2. Run the script:
   - `powershell -File .codex/skills/extract-worker-debug-images/scripts/pull-images.ps1`
3. For most recent image only:
   - `powershell -File .codex/skills/extract-worker-debug-images/scripts/pull-images.ps1 -MaxCount 1`
4. For another pattern/folder:
   - `powershell -File .codex/skills/extract-worker-debug-images/scripts/pull-images.ps1 -Pattern *.jpg -RemoteDir /app/other-debug -Destination .\tmp\images`

## Guardrails
- Never delete or mutate files in the remote container.
- Always report local output file paths after copying.
- If no files match, return a clear message and stop.
