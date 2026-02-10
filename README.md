# Infrastructure (infra)

Infrastructure configuration repo: VPS Docker Compose stacks and a small set of maintenance scripts.

## Layout

```
.
|-- README.md
|-- scripts/
|   `-- powershell/
|       `-- windows_cleanup.ps1
`-- vps/
    `-- embassy-access/
        |-- Caddyfile
        |-- docker-compose.yml
        |-- backups/                # host-mounted SQL dumps (gitignored)
        `-- postgres/
            |-- Dockerfile
            |-- backup.sh
            `-- entrypoint.sh
```

## embassy-access stack (`vps/embassy-access/`)

This stack is intended to run on a VPS and is managed with Docker Compose.

### Services

- `browser-webapi`: `ponkorn/browser-webapi:latest` (internal service; no host ports published by default)
- `postgres`: Postgres 18 (custom image with cron-driven backups)
- `pgadmin`: `dpage/pgadmin4:latest` (served via Caddy; no direct host ports by default)
- `caddy`: reverse proxy + TLS termination (binds host ports `80` and `443`)

### One-time setup (external network)

The Compose file uses an external network named `embassy-access-network`. Create it once on the host:

```bash
docker network create --driver bridge embassy-access-network
```

### Configuration (`.env`)

Create a `.env` file (gitignored) in `vps/embassy-access/` with at least:

```bash
POSTGRES_PASSWORD=change-me
PGADMIN_EMAIL=admin@example.com
PGADMIN_PASSWORD=change-me
BACKUP_INTERVAL_DAYS=7
```

### Run locally (for validation)

```bash
cd vps/embassy-access
docker compose up -d --build
docker compose ps
```

Logs:

```bash
docker compose logs -f postgres
```

### Access (Caddy)

`caddy` serves routes defined in `vps/embassy-access/Caddyfile`.

Default config proxies:
- `pgadmin.masterlifting.fit` -> `pgadmin:80`

Notes:
- If the host already uses ports `80/443`, you must change the Compose ports or disable the `caddy` service.
- For local testing, you may need to adjust `Caddyfile` (for example to `localhost` / `:80`) or add a hosts entry.

### Backups

Backups are written by `vps/embassy-access/postgres/backup.sh` to `/backups` inside the container, which is mounted to
`vps/embassy-access/backups/` on the host (gitignored).

Automation:
- A cron job runs daily at 02:00 inside the Postgres container.
- The script only performs a dump if `BACKUP_INTERVAL_DAYS` have elapsed since the last backup (tracked in `last_backup.txt`).

Manual backup:

```bash
docker exec embassy-access-postgres /usr/local/bin/backup.sh
```

Restore a dump:

```bash
docker exec -i embassy-access-postgres psql -U eauser -d eadb < backups/backup_YYYYMMDD_HHMMSS.sql
```

## Deploy embassy-access infra to the VPS (this repo)

Deployment is automated via GitHub Actions: `.github/workflows/deploy-embassy-access.yml`.

### Prerequisites (VPS)

- Docker + Docker Compose installed.
- The SSH user used for deploy can run `docker` (typically by being in the `docker` group).
- SSH uses the default port `22` (the workflow does not set a custom port).
- Files are synced to `/usr/src/infra/vps/embassy-access` on the VPS.
- The stack uses an external Docker network named `embassy-access-network` (the workflow creates it if missing).

### Prerequisites (GitHub Secrets)

Configure these secrets in the GitHub repository that hosts this workflow:

- `VPS_HOST`
- `VPS_USERNAME`
- `VPS_SSH` (private key)
- `VPS_PASSPHRASE` (optional; only if the key is encrypted)

### How it deploys

- On pushes to `main`: the workflow runs, but deploy steps only run if `vps/embassy-access/**` changed.
- On tag push `embassy-access`: deploy steps always run (explicit release signal).
- Manual: run from the Actions UI (`workflow_dispatch`).

What the workflow does:
- SCP syncs `vps/embassy-access` to `/usr/src/infra` on the VPS.
- SSH runs `docker compose pull` then `docker compose up -d --build` in `/usr/src/infra/vps/embassy-access`.
- Prunes unused images/build cache (best-effort).
- Uses GitHub Actions `concurrency` to avoid overlapping deploy runs.

### Trigger a deploy from git

Normal path (push to `main` with changes under `vps/embassy-access/**`):

```bash
git add vps/embassy-access
git commit -m "Update embassy-access stack"
git push origin main
```

Force deploy (move tag `embassy-access` to the current commit and push it):

```bash
git tag -f embassy-access
git push -f origin embassy-access
```

## Windows cleanup script

Path: `scripts/powershell/windows_cleanup.ps1`

This script is destructive and requires elevated PowerShell.

```powershell
# Run from repo root in an elevated PowerShell
.\scripts\powershell\windows_cleanup.ps1
```

## Security notes

- Do not commit secrets. `.env` files are gitignored.
- Treat DB dumps as sensitive data. `backups/` is gitignored.

