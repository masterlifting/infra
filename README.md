
# Infrastructure Configuration

A collection of infrastructure configurations, Docker Compose setups, and automation scripts for system maintenance and productivity.

## 📁 Directory Structure

```
├── README.md
├── scripts/
│   └── powershell/
│       └── windows_cleanup.ps1
└── vps/
    └── embassy-access/
        ├── docker-compose.yml
        ├── backups/
        └── postgres/
            ├── Dockerfile
            ├── backup.sh
            └── entrypoint.sh
```

## 🚀 Components Overview

### VPS Services

#### Embassy Access Stack (`vps/embassy-access/`)

A Docker Compose stack for embassy access management with automated PostgreSQL backups.

**Services:**
- **PostgreSQL 18**: Custom build with automated backup functionality
  - Cron-based scheduled backups (daily at 2 AM)
  - Configurable backup interval (default: 7 days)
  - Backups stored on host filesystem (`./backups`)
  - Custom data directory configuration
- **pgAdmin 4**: Web-based PostgreSQL administration interface
  - Accessible on port 3002
  - Persistent configuration storage
- **Browser WebAPI**: Headless browser service for automation

**PostgreSQL Backup Features:**
- ✅ Automated pg_dump backups every X days (configurable)
- 📁 Backups saved to host filesystem (accessible outside Docker)
- 🕐 Scheduled via cron (daily check at 2 AM)
- 📊 Interval tracking to avoid unnecessary backups
- 🔄 Timestamped backup files (`backup_YYYYMMDD_HHMMSS.sql`)

**Environment Variables:**
```bash
# PostgreSQL
POSTGRES_PASSWORD=<your-password>
BACKUP_INTERVAL_DAYS=7  # Days between backups (default: 7)

# pgAdmin
PGADMIN_EMAIL=<your-email>
PGADMIN_PASSWORD=<your-password>
```

**Usage:**
```bash
# Start all services
cd vps/embassy-access
docker compose up -d

# View logs
docker compose logs -f postgres

# Manual backup
docker exec embassy-access-postgres /usr/local/bin/backup.sh

# Access pgAdmin
# Open http://localhost:3002
```

**Backup Management:**
- Backups are stored in `vps/embassy-access/backups/`
- To restore a backup: `docker exec -i embassy-access-postgres psql -U eauser -d eadb < backups/backup_YYYYMMDD_HHMMSS.sql`
- Change backup interval: Set `BACKUP_INTERVAL_DAYS` in your `.env` file or docker-compose
- Modify schedule: Edit cron expression in `postgres/Dockerfile`

### PowerShell Scripts

#### `windows_cleanup.ps1`
A comprehensive Windows 11 system cleanup script that safely removes temporary files, caches, and other system junk to free up disk space.

**Features:**
- ✅ Safe cleanup with error handling and path validation
- 🔍 Removes VS Code cache and temporary files
- 🌐 Cleans browser caches (Microsoft Edge)
- 🔧 Development tools cleanup (npm, Playwright, Postman, Docker)
- 🗑️ Google Update cache cleanup
- 🗃️ Optional .NET NuGet package cache cleanup (commented out by default)
- 🐳 Docker system/image/volume cleanup (if Docker is installed)
- 📱 Application data cleanup (Thunderbird, Zoom, Ledger Live, etc.)
- 🗑️ System temporary files and Windows Update cache
- ♻️ Recycle Bin and thumbnail cache cleanup
- 📊 Registry cleanup for invalid startup entries
- 📈 Large file detection (files >100MB)
- 🧹 Windows system maintenance (Prefetch, Error Reporting, etc.)

**Requirements:**
- Windows 11 (or Windows 10)
- Administrator privileges
- PowerShell execution policy bypass (handled automatically)

**Usage:**
```powershell
# Run as Administrator
.\powershell\windows_cleanup.ps1
```

**Safety Features:**
- Checks for Administrator privileges
- Validates paths before deletion
- Comprehensive error reporting
- Skips non-existent files/folders
- Color-coded output for easy monitoring

## 🗂️ Large Files Handling

The script reports the top 20 largest files (>100MB) on your C: drive. Some files can be safely deleted, while others are system or application files and should be kept.

**Automatically handled by the script:**
- Google Update cache (`crx_cache`)
- Docker images/volumes/containers (if Docker is installed)
- (Optional) NuGet package cache (uncomment in script if you want to clear)

**Manual cleanup recommendations:**
- Review large files reported by the script. Only delete files you recognize and do not need.
- Do **not** delete system files like `winre.wim`, `system.vhd`, or application binaries unless you are certain they are not needed.

## 🛡️ Safety Notes

- All scripts include safety checks and error handling
- Always run scripts as Administrator when required
- Review script contents before execution
- Scripts are designed to be safe but use at your own discretion

## 🤝 Contributing

Feel free to add new scripts or improve existing ones. Please ensure:
- Proper error handling
- Clear documentation
- Safety checks for destructive operations
- Cross-platform compatibility where applicable

## 📝 License

Personal use scripts - use responsibly.
