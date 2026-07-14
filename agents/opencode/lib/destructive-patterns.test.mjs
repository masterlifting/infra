// Test for the shared destructive-command patterns.
// Imports the SAME module the plugin uses, so it always tests what ships.
// Run: node lib/destructive-patterns.test.mjs   (exit 0 = pass, 1 = fail)

import { destructivePatterns, findDestructive } from "./destructive-patterns.mjs"
import { BlockDestructive } from "../plugins/block-destructive.js"

const shouldBlock = [
  // bash rm -rf catastrophic targets
  "rm -rf /",
  "rm -rf ~",
  "rm -rf ..",
  "rm -fr /",
  "rm -r -f /",
  "rm --recursive --force -- /",
  "rm *",
  "rm -f *",
  // PowerShell / cmd deletes
  "Remove-Item -Recurse -Force ./x",
  "Remove-Item -Force C:\\temp\\a",
  "Remove-Item ./x -Recurse -Force",
  "rm -Recurse -Force ./x",
  "ri -r -Force .",
  "rd /s /q build",
  "del /q C:\\tmp\\a.txt",
  "Format-Volume -DriveLetter D",
  // SQL
  "psql -c 'DROP TABLE users'",
  "drop database prod",
  "TRUNCATE TABLE orders",
  "truncate accounts;",
  // git force-push to protected branches (both orders, -f and --force)
  "git push --force origin main",
  "git push origin main --force",
  "git push -f origin master",
  "git push origin master -f",
  // git history / working-tree destruction
  "git reset --hard origin/main",
  "git clean -fdx",
  "git clean -dfx",
]

const shouldAllow = [
  "rm -rf ./node_modules",          // scoped subpath, not catastrophic
  "rm file.txt",
  "Remove-Item ./build -Recurse",   // recurse without -Force (prompts)
  "Remove-Item -Filter *.log ./logs", // -Filter must not read as -Force
  "Get-ChildItem -Recurse -Force",  // not a delete cmdlet
  "git status",
  "git push origin feature/my-branch",
  "git commit -m 'work'",
  "dotnet build",
  "SELECT * FROM users",
  "ls -la",
]

let failures = 0

for (const cmd of shouldBlock) {
  const reason = findDestructive(cmd)
  if (!reason) {
    console.error(`FAIL (should block, allowed): ${cmd}`)
    failures++
  }
}

for (const cmd of shouldAllow) {
  const reason = findDestructive(cmd)
  if (reason) {
    console.error(`FAIL (should allow, blocked as "${reason}"): ${cmd}`)
    failures++
  }
}

// Guard: no pattern may carry the global flag (stateful lastIndex breaks .test()).
for (const { regex, reason } of destructivePatterns) {
  if (regex.flags.includes("g")) {
    console.error(`FAIL (pattern has 'g' flag): ${reason}`)
    failures++
  }
}

const hooks = await BlockDestructive()
const beforeExecute = hooks["tool.execute.before"]

await beforeExecute({ tool: "bash" }, { args: { command: "git status" } })
await beforeExecute({ tool: "read" }, { args: { command: "rm -rf /" } })

try {
  await beforeExecute({ tool: "bash" }, { args: { command: "rm -rf / --token=secret" } })
  console.error("FAIL (plugin allowed destructive command)")
  failures++
} catch (error) {
  if (error.message.includes("secret")) {
    console.error("FAIL (plugin error exposed command contents)")
    failures++
  }
}

const total = shouldBlock.length + shouldAllow.length
if (failures === 0) {
  console.log(`OK — ${total} cases pass, ${destructivePatterns.length} patterns`)
  process.exit(0)
} else {
  console.error(`\n${failures} failure(s) of ${total} cases`)
  process.exit(1)
}
