
# Scripts Collection

A collection of useful automation scripts for system maintenance and productivity.

## 📁 Directory Structure

```
scripts/
├── README.md
└── powershell/
    └── windows_cleanup.ps1
```

## 🚀 Scripts Overview

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
