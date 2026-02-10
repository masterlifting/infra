# Repository Guidelines

## Project Structure

- `vps/embassy-access/`: VPS stack (Docker Compose) for `browser-webapi`, `postgres`, and `pgadmin`.
- `vps/embassy-access/postgres/`: Custom Postgres image + backup automation (`backup.sh`, `entrypoint.sh`, `Dockerfile`).
- `vps/embassy-access/backups/`: Host-mounted database dumps (gitignored).
- `scripts/powershell/windows_cleanup.ps1`: Windows maintenance script (destructive; requires Admin).

## Build, Test, and Development Commands

From `vps/embassy-access/`:

```bash
# One-time: compose expects this external network to exist
docker network create --driver bridge embassy-access-network

# Start/stop stack
docker compose up -d
docker compose down

# Tail logs
docker compose logs -f postgres
```

Backups:

```bash
# Run a manual backup inside the Postgres container
docker exec embassy-access-postgres /usr/local/bin/backup.sh

# Restore a dump (example file name)
docker exec -i embassy-access-postgres psql -U eauser -d eadb < backups/backup_YYYYMMDD_HHMMSS.sql
```

Windows cleanup (run from repo root, elevated PowerShell):

```powershell
.\scripts\powershell\windows_cleanup.ps1
```

## Coding Style & Naming Conventions

- YAML (`docker-compose.yml`): 2-space indentation; prefer explicit ports/volumes; keep service names stable.
- Shell (`*.sh`): `bash` with `set -e` style safety when adding new scripts; keep paths configurable via env vars.
- PowerShell (`*.ps1`): use approved verbs (`Remove-*`, `Show-*`), and keep destructive actions behind path checks.

## Testing Guidelines

No automated tests in this repository. Validate changes by running the stack locally and confirming:
- containers start cleanly (`docker compose ps`)
- backups are written to `vps/embassy-access/backups/`
- reverse proxy config changes are reflected (if using `Caddyfile`).

## Commit & Pull Request Guidelines

- Commit messages in this repo are short, imperative, and component-scoped (examples: “Update docker-compose.yml…”, “Fix …”, “Add …”).
- PRs should include:
  - a brief summary of what changed and why
  - any required config/env changes (`.env` variables like `POSTGRES_PASSWORD`, `PGADMIN_EMAIL`)
  - risk notes for destructive operations (especially `windows_cleanup.ps1`).

## Security & Configuration Tips

- Do not commit secrets. Use `.env` (gitignored) for credentials and intervals (e.g., `BACKUP_INTERVAL_DAYS`).
- Backups are gitignored (`backups/`). Treat them as sensitive data.
