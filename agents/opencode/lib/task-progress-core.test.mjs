import assert from "node:assert/strict"
import { setTimeout as delay } from "node:timers/promises"
import { createTaskQueue, createTaskSynchronizer, extractTaskFiles, extractViolationFindings, isTaskFile, sanitizeFindingLine } from "./task-progress-core.mjs"
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

// The hook must stay non-fatal for edits that do not resolve to an existing task
// file: no findings, no output mutation, and no sync process spawned.
const hook = plugin["tool.execute.after"]
const untouchedOutput = { output: "original" }
await hook({ tool: "edit", args: { filePath: "C:/repo/README.md" } }, untouchedOutput)
assert.equal(untouchedOutput.output, "original", "non-task edits must not be touched by the hook")
await hook({ tool: "edit", args: { filePath: "C:/repo/.tasks/BACK-1/TASK.md" } }, untouchedOutput)
assert.equal(untouchedOutput.output, "original", "missing task files must not be touched by the hook")

// Single-invocation synchronization: one FSI process per task sync, then sanitized
// finding extraction with non-fatal failure isolation.
let syncInvocations = 0
const cleanSync = createTaskSynchronizer({
  runScript: async () => {
    syncInvocations += 1
    return { ok: true, output: "OK fixture" }
  },
})
assert.deepEqual(await cleanSync("C:/repo/.tasks/BACK-9/TASK.md", "C:/repo"), [])
assert.equal(syncInvocations, 1, "synchronizer must run exactly one FSI process per sync")

const violationFindings = await createTaskSynchronizer({
  runScript: async () => ({
    ok: false,
    output: "VIOLATIONS in fixture\n  - finding one\n  - `raw` finding two\u0000",
  }),
})("C:/repo/.tasks/BACK-9/TASK.md", "C:/repo")
assert.deepEqual(violationFindings, ["- finding one", "- ?raw? finding two?"])

const failureFindings = await createTaskSynchronizer({
  runScript: async () => ({ ok: false, output: "" }),
})("C:/repo/.tasks/BACK-9/TASK.md", "C:/repo")
assert.deepEqual(failureFindings, ["- Task synchronization helper failed or timed out"])

assert.equal(sanitizeFindingLine("a`b\u0000c"), "a?b?c")
assert.equal(sanitizeFindingLine("- " + "x".repeat(500)), "- " + "x".repeat(298))
assert.deepEqual(
  extractViolationFindings("OK ignored\n- first\nsecond line\n- third", 2),
  ["- first", "- third"],
)

console.log("OK task progress extraction, queue serialization, recovery, single-invocation sync, and plugin loading")
