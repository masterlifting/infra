// opencode plugin: block destructive shell commands before they execute.
//
// Auto-loaded from ~/.config/opencode/plugins/ (no opencode.json entry needed).
// The pattern list lives in ../lib/destructive-patterns.mjs and is shared with
// lib/destructive-patterns.test.mjs, so the deployed guard and its test can
// never drift apart.
//
// This is defense-in-depth on top of opencode.json's permission denies: it is
// order-independent (globs are not) and covers cases the config misses
// (SQL DROP/TRUNCATE, force-push to main/master).

import { findDestructive } from "../lib/destructive-patterns.mjs"

export const BlockDestructive = async () => {
  return {
    "tool.execute.before": async (input, output) => {
      if (input.tool !== "bash") return
      const command = output?.args?.command
      const reason = findDestructive(command)
      if (reason) {
        throw new Error(`Blocked destructive command (${reason})`)
      }
    },
  }
}
