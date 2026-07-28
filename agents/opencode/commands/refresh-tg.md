---
description: Guide a secret-safe Telegram MCP session refresh and verify connectivity.
agent: executor
---

Refresh the Telegram MCP authentication session stored in User-scoped Windows environment variables.

Steps:

1. Check the current MCP status:

```powershell
opencode mcp list
```

2. Tell the user to generate a new Telegram session in a private terminal outside OpenCode. Never run the generator through an agent tool because its output contains the session secret:

```powershell
$env:TELEGRAM_API_ID=[Environment]::GetEnvironmentVariable('TELEGRAM_API_ID','User'); $env:TELEGRAM_API_HASH=[Environment]::GetEnvironmentVariable('TELEGRAM_API_HASH','User'); uv --directory "$env:USERPROFILE\.config\opencode\mcp\telegram" run session_string_generator.py
```

3. After the user explicitly confirms saving the new session into the User environment, tell the user to run this in the same private terminal. The secure prompt prevents the session from entering shell history or agent input:

```powershell
$secure = Read-Host 'Paste Telegram session' -AsSecureString; $s = [System.Net.NetworkCredential]::new('', $secure).Password.Trim(); [Environment]::SetEnvironmentVariable('TELEGRAM_SESSION_STRING',$s,'User'); "saved len=$($s.Length) mod4=$($s.Length % 4)"; $s = $null; $secure.Dispose()
```

4. Restart VS Code and OpenCode.

5. Validate that the Telegram MCP is connected:

```powershell
opencode mcp list
```

If it still fails, run this secret-safe shape check:

```powershell
$v=[Environment]::GetEnvironmentVariable('TELEGRAM_SESSION_STRING','Process'); if ([string]::IsNullOrEmpty($v)) { 'missing in process env' } else { "len=$($v.Length) mod4=$($v.Length%4) hasSpace=$($v.Contains(' ')) hasNewline=$($v.Contains("`n") -or $v.Contains("`r"))" }
```

