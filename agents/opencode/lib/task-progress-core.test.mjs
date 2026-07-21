import assert from "node:assert/strict"
import { setTimeout as delay } from "node:timers/promises"
import { createTaskQueue, extractTaskFiles, isTaskFile } from "./task-progress-core.mjs"
import { TaskProgress } from "../plugins/task-progress.js"

assert.equal(isTaskFile("C:/repo/.tasks/BACK-1/TASK.md"), true)
assert.equal(isTaskFile("C:/repo/.tasks/BACK-1/notes.md"), false)

const extracted = extractTaskFiles("apply_patch", {
  patchText: [
    "*** Add File: .tasks/BACK-1/TASK.md",
    "*** Update File: .tasks/BACK-2/TASK.md",
    "*** Move to: .tasks/BACK-3/TASK.md",
    "*** Delete File: .tasks/BACK-4/TASK.md",
  ].join("\n"),
}, "C:/repo", () => true)

assert.deepEqual(extracted.map((path) => path.replace(/\\/g, "/")), [
  "C:/repo/.tasks/BACK-1/TASK.md",
  "C:/repo/.tasks/BACK-2/TASK.md",
  "C:/repo/.tasks/BACK-3/TASK.md",
])

let active = 0
let maxActive = 0
const calls = []
const synchronizeTask = async (taskFile) => {
  active += 1
  maxActive = Math.max(maxActive, active)
  calls.push(taskFile)
  await delay(10)
  active -= 1
  return []
}

const samePathQueue = createTaskQueue({
  synchronizeTask,
  canonicalize: (path) => path,
  platform: "win32",
})

await Promise.all([
  samePathQueue.enqueueTask("C:/repo/.tasks/BACK-1/TASK.md", "C:/repo"),
  samePathQueue.enqueueTask("c:/repo/.tasks/back-1/task.md", "C:/repo"),
])
assert.equal(maxActive, 1, "same task path must synchronize serially")
assert.equal(calls.length, 2)
assert.equal(samePathQueue.size(), 0)

active = 0
maxActive = 0
const differentPathQueue = createTaskQueue({
  synchronizeTask,
  canonicalize: (path) => path,
  platform: "win32",
})
await Promise.all([
  differentPathQueue.enqueueTask("C:/repo/.tasks/BACK-1/TASK.md", "C:/repo"),
  differentPathQueue.enqueueTask("C:/repo/.tasks/BACK-2/TASK.md", "C:/repo"),
])
assert.equal(maxActive, 2, "different task paths should synchronize independently")

let attempts = 0
const recoveryQueue = createTaskQueue({
  synchronizeTask: async () => {
    attempts += 1
    if (attempts === 1) throw new Error("expected failure")
    return []
  },
  canonicalize: (path) => path,
})
await assert.rejects(recoveryQueue.enqueueTask("/repo/.tasks/TASK-1/TASK.md", "/repo"))
await recoveryQueue.enqueueTask("/repo/.tasks/TASK-1/TASK.md", "/repo")
assert.equal(attempts, 2, "queue must recover after a rejected synchronization")
assert.equal(recoveryQueue.size(), 0)

const plugin = await TaskProgress({ directory: "C:/repo", worktree: "C:/repo" })
assert.equal(typeof plugin["tool.execute.after"], "function", "plugin hook must load")

console.log("OK task progress extraction, queue serialization, recovery, and plugin loading")
