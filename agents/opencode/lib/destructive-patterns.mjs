// Destructive-command patterns — single source of truth.
//
// Imported by BOTH:
//   - ../plugins/block-destructive.js  (runtime guard: opencode tool.execute.before)
//   - ./destructive-patterns.test.mjs  (test — same list, so prod and test never drift)
//
// Ported from the finom Claude Code hook (.claude/hooks/pretool-block-destructive.js)
// and extended for the PowerShell/pwsh shell opencode runs here.
//
// Design notes:
//   - No `g` flag on any regex — a stateful lastIndex would break repeated .test().
//   - bash `rm -rf` is scoped to catastrophic bare targets (/, ~, ..) so ordinary
//     `rm -rf ./node_modules` still works; the PowerShell delete family is matched
//     whenever -Force is present, mirroring the blanket deny already in opencode.json.
//   - Adds coverage opencode.json's glob denies lack: SQL DROP/TRUNCATE, and
//     force-push to main/master (the config only marks `git push*` as "ask").

export const destructivePatterns = [
  // --- bash: rm -rf catastrophic bare targets (both flag orders) ---
  { regex: /\brm\s+-[a-zA-Z]*r[a-zA-Z]*f[a-zA-Z]*\s+(\/|~\/?|\.\.\/?)(\s|$)/, reason: "rm -rf targeting / ~ or .." },
  { regex: /\brm\s+-[a-zA-Z]*f[a-zA-Z]*r[a-zA-Z]*\s+(\/|~\/?|\.\.\/?)(\s|$)/, reason: "rm -fr targeting / ~ or .." },
  { regex: /\brm\s+(?:-[a-zA-Z]*r[a-zA-Z]*|--recursive)\s+(?:-[a-zA-Z]*f[a-zA-Z]*|--force)\s+(?:--\s+)?(\/|~\/?|\.\.\/?)(\s|$)/, reason: "rm recursive forced delete targeting / ~ or .." },
  { regex: /\brm\s+(?:-[a-zA-Z]*f[a-zA-Z]*|--force)\s+(?:-[a-zA-Z]*r[a-zA-Z]*|--recursive)\s+(?:--\s+)?(\/|~\/?|\.\.\/?)(\s|$)/, reason: "rm recursive forced delete targeting / ~ or .." },
  { regex: /\brm\s+(-[a-zA-Z]*f[a-zA-Z]*\s+)?\*(\s|$)/, reason: "rm targeting wildcard *" },

  // --- PowerShell / cmd: recursive & forced deletes ---
  { regex: /\b(Remove-Item|rm|ri|rmdir|rd|del|erase)\b.*\s[-/]fo[a-zA-Z]*/i, reason: "forced delete (Remove-Item -Force family)" },
  { regex: /\b(rd|rmdir)\b\s+\/s\b/i, reason: "rd /s (recursive delete)" },
  { regex: /\bdel\b.*\s\/[sq]\b/i, reason: "del /s or /q (recursive / quiet delete)" },
  { regex: /\b(Format-Volume|Clear-Disk|Remove-Partition)\b/i, reason: "disk-destructive PowerShell cmdlet" },

  // --- SQL ---
  { regex: /\bdrop\s+(table|database|schema)\b/i, reason: "DROP TABLE/DATABASE/SCHEMA" },
  { regex: /\btruncate\s+table\b/i, reason: "TRUNCATE TABLE" },
  { regex: /\btruncate\s+(only\s+)?[a-z_][\w.$]*\s*;/i, reason: "TRUNCATE statement (bare, ;-terminated)" },

  // --- git: force-push to protected branches (config only "asks" on push) ---
  { regex: /\bgit\s+push\s+.*--force\b.*\b(main|master)\b/, reason: "force-push to main/master" },
  { regex: /\bgit\s+push\s+.*-f\b.*\b(main|master)\b/, reason: "force-push to main/master" },
  { regex: /\bgit\s+push\s+.*\b(main|master)\b.*--force\b/, reason: "force-push to main/master" },
  { regex: /\bgit\s+push\s+.*\b(main|master)\b.*-f\b/, reason: "force-push to main/master" },

  // --- git: history / working-tree destruction ---
  { regex: /\bgit\s+reset\s+--hard\s+origin\/(main|master)\b/, reason: "git reset --hard to origin/main|master" },
  { regex: /\bgit\s+clean\s+-(?=[a-zA-Z]*f)(?=[a-zA-Z]*d)(?=[a-zA-Z]*x)[a-zA-Z]*/, reason: "git clean -fdx (nuke untracked + ignored)" },
]

// Returns the reason string of the first matching pattern, or null if the
// command is not destructive. This is the exact check the plugin runs, so the
// test can exercise it directly.
export function findDestructive(command) {
  if (typeof command !== "string" || command.length === 0) return null
  for (const { regex, reason } of destructivePatterns) {
    if (regex.test(command)) return reason
  }
  return null
}
