import { isAbsolute, resolve } from "node:path"

export const isTaskFile = (path) =>
  /(^|\/)\.tasks\/[^/]+\/TASK\.md$/i.test(path.replace(/\\/g, "/"))

export const extractTaskFiles = (tool, args, directory, exists) => {
  if (!args || !["edit", "write", "apply_patch"].includes(tool)) return []

  const paths = []
  const directPath = args.filePath ?? args.file_path
  if (typeof directPath === "string") paths.push(directPath)

  if (tool === "apply_patch" && typeof args.patchText === "string") {
    const header = /^\*\*\* (?:Add|Update) File: (.+)$/gm
    const move = /^\*\*\* Move to: (.+)$/gm
    for (const pattern of [header, move]) {
      for (const match of args.patchText.matchAll(pattern)) paths.push(match[1].trim())
    }
  }

  return [...new Set(paths
    .map((path) => isAbsolute(path) ? path : resolve(directory, path))
    .filter((path) => isTaskFile(path) && exists(path)))]
}

// Strip control characters and backticks from a finding line and bound its length,
// so plugin diagnostics stay deterministic and never leak raw process noise.
export const sanitizeFindingLine = (line) =>
  line.replace(/[\u0000-\u001f\u007f`]/g, "?").slice(0, 300)

// Extract the validator's `- <message>` finding bullets from its output, bounded to
// `limit` lines. Headers, timestamps, and stack traces are ignored.
export const extractViolationFindings = (output, limit = 10) =>
  output
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter((line) => line.startsWith("- "))
    .slice(0, limit)
    .map(sanitizeFindingLine)

// Build the single-invocation task synchronizer. `runScript` runs exactly one F#
// sync process and resolves to `{ ok, output }`. Non-fatal: a failed or timed out
// process with no validator output becomes one finding; validation findings are
// surfaced without aborting the enclosing edit.
export const createTaskSynchronizer = ({ runScript }) =>
  async (taskFile, projectRoot) => {
    const result = await runScript(taskFile, projectRoot)
    const hasViolations = result.output.includes("VIOLATIONS")
    const findings = []
    if (!result.ok && !hasViolations) findings.push("- Task synchronization helper failed or timed out")
    if (!hasViolations) return findings
    return findings.concat(extractViolationFindings(result.output))
  }

export const createTaskQueue = ({ synchronizeTask, canonicalize, platform = process.platform }) => {
  const queues = new Map()

  const enqueueTask = (taskFile, projectRoot) => {
    let queueKey
    try {
      queueKey = canonicalize(taskFile)
    } catch {
      queueKey = taskFile
    }
    if (platform === "win32") queueKey = queueKey.toLowerCase()

    const previous = queues.get(queueKey) ?? Promise.resolve([])
    const current = previous.catch(() => []).then(() => synchronizeTask(taskFile, projectRoot))
    queues.set(queueKey, current)
    const clear = () => {
      if (queues.get(queueKey) === current) queues.delete(queueKey)
    }
    current.then(clear, clear)
    return current
  }

  return { enqueueTask, size: () => queues.size }
}
