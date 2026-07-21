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
