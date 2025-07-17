# ========================================
# SAFE SYSTEM CLEANUP SCRIPT (WINDOWS 11)
# Author: Andrei
# With error reporting and path checks
# ========================================

# Set execution policy for this session
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force

# Elevation check
if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
        [Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Warning "Rerun this script as Administrator."
    exit 1
}

function Remove-Safely {
    param (
        [string]$Path
    )

    if (Test-Path $Path) {
        try {
            Remove-Item $Path -Recurse -Force -ErrorAction Stop
            Write-Host "Removed: $Path" -ForegroundColor Green
        }
        catch {
            Write-Host "Failed to remove: $Path" -ForegroundColor Red
            Write-Host $_.Exception.Message -ForegroundColor DarkGray
        }
    }
    else {
        Write-Host "Skipped (not found): $Path" -ForegroundColor Yellow
    }
}

function Remove-WildcardSafely {
    param (
        [string]$PathPattern
    )
    $items = Get-ChildItem -Path $PathPattern -Force -ErrorAction SilentlyContinue
    if ($items) {
        foreach ($item in $items) {
            Remove-Safely $item.FullName
        }
    }
    else {
        Write-Host "Skipped (not found or empty): $PathPattern" -ForegroundColor Yellow
    }
}

function Show-LargestFiles {
    param(
        [string]$Path = 'C:\',
        [int]$Top = 20,
        [int64]$MinSizeMB = 100
    )
    Write-Host "==> Scanning for large files (>${MinSizeMB}MB) in $Path..." -ForegroundColor Cyan
    try {
        $files = Get-ChildItem -Path $Path -Recurse -ErrorAction SilentlyContinue |
        Where-Object { -not $_.PSIsContainer -and $_.Length -gt ($MinSizeMB * 1MB) } |
        Sort-Object Length -Descending |
        Select-Object -First $Top
        if ($files) {
            foreach ($file in $files) {
                $sizeMB = [math]::Round($file.Length / 1MB, 2)
                Write-Host ("{0,8} MB  {1}" -f $sizeMB, $file.FullName) -ForegroundColor Magenta
            }
        }
        else {
            Write-Host "No files larger than $MinSizeMB MB found in $Path." -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "Error scanning for large files: $_" -ForegroundColor Red
    }
}

# ================== CLEANUP ==================

Write-Host "==> VS Code cache cleanup" -ForegroundColor Cyan
Remove-Safely "$env:APPDATA\Code\Cache"
Remove-Safely "$env:APPDATA\Code\CachedData"
Remove-Safely "$env:APPDATA\Code\CachedExtensionVSIXs"
Remove-Safely "$env:APPDATA\Code\logs"
Remove-Safely "$env:APPDATA\Code\GPUCache"
Remove-Safely "$env:APPDATA\Code\Crashpad"
Remove-Safely "$env:APPDATA\Code\Service Worker"
Remove-Safely "$env:APPDATA\Code\WebStorage"
Remove-Safely "$env:APPDATA\Code\CachedProfilesData"

Write-Host "==> Microsoft Edge cache cleanup" -ForegroundColor Cyan
Remove-Safely "$env:LOCALAPPDATA\Microsoft\Edge\User Data\Default\Cache"
Remove-Safely "$env:LOCALAPPDATA\Microsoft\Edge\User Data\Default\Code Cache"
Remove-Safely "$env:LOCALAPPDATA\Microsoft\Edge\User Data\Default\Media Cache"
Remove-Safely "$env:LOCALAPPDATA\Microsoft\Edge\User Data\ShaderCache"

Write-Host "==> Dev junk removal" -ForegroundColor Cyan
Remove-Safely "$env:LOCALAPPDATA\ms-playwright"
Remove-Safely "$env:LOCALAPPDATA\Postman"
Remove-Safely "$env:LOCALAPPDATA\npm-cache"
Remove-Safely "$env:LOCALAPPDATA\outline-client-updater"
Remove-Safely "$env:LOCALAPPDATA\outline-manager-updater"
Remove-Safely "$env:LOCALAPPDATA\Gizmo.Client.UI.Host.WPF.WebView2"
Remove-Safely "$env:LOCALAPPDATA\Gizmo.Web.Manager.UI.Host.WPF.WebView2"
Remove-Safely "$env:LOCALAPPDATA\Docker Desktop Installer"

Write-Host "==> Google Update cache cleanup" -ForegroundColor Cyan
Remove-WildcardSafely "${env:ProgramFiles(x86)}\Google\GoogleUpdater\crx_cache\*"

Write-Host "==> .NET NuGet package cache cleanup (optional - may affect build times)" -ForegroundColor Cyan
# Uncomment the next line if you want to clear NuGet global packages cache
# Remove-WildcardSafely "$env:USERPROFILE\.nuget\packages\*"
Write-Host "Note: NuGet cache cleanup is commented out to preserve packages. Uncomment if needed." -ForegroundColor Yellow

Write-Host "==> Misc user app data cleanup" -ForegroundColor Cyan
Remove-Safely "$env:APPDATA\Thunderbird"
#Remove-Safely "$env:APPDATA\Mozilla"
Remove-Safely "$env:APPDATA\Zoom"
Remove-Safely "$env:LOCALAPPDATA\Ledger Live"
Remove-Safely "$env:LOCALAPPDATA\Garmin"

Write-Host "==> Lenovo System Update cleanup" -ForegroundColor Cyan
Remove-Safely "C:\ProgramData\Lenovo\SystemUpdate\sessionSE\Repository"

Write-Host "==> Visual Studio offline package cleanup" -ForegroundColor Cyan
Remove-Safely "C:\ProgramData\Microsoft\VisualStudio\Packages"

Write-Host "==> CrashDumps cleanup" -ForegroundColor Cyan
Remove-Safely "$env:LOCALAPPDATA\CrashDumps"

Write-Host "==> TEMP folders cleanup" -ForegroundColor Cyan
Remove-WildcardSafely "$env:TEMP\*"
Remove-WildcardSafely "$env:LOCALAPPDATA\Temp\*"

Write-Host "==> Windows Update cache cleanup" -ForegroundColor Cyan
Remove-WildcardSafely "$env:WINDIR\SoftwareDistribution\Download\*"

Write-Host "==> Recycle Bin cleanup" -ForegroundColor Cyan
try {
    $shell = New-Object -ComObject Shell.Application
    $recycleBin = $shell.Namespace(0xA)
    $items = $recycleBin.Items()
    if ($items.Count -gt 0) {
        $items | ForEach-Object { $_.InvokeVerb('delete') }
        Write-Host "Recycle Bin emptied" -ForegroundColor Green
    }
    else {
        Write-Host "Recycle Bin already empty" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "Failed to empty Recycle Bin: $_" -ForegroundColor Red
}

Write-Host "==> Windows Prefetch cleanup" -ForegroundColor Cyan
Remove-WildcardSafely "$env:SystemRoot\Prefetch\*"

Write-Host "==> Windows Error Reporting cleanup" -ForegroundColor Cyan
Remove-WildcardSafely "$env:ProgramData\Microsoft\Windows\WER\ReportQueue\*"
Remove-WildcardSafely "$env:ProgramData\Microsoft\Windows\WER\ReportArchive\*"

Write-Host "==> Windows Thumbnail cache cleanup" -ForegroundColor Cyan
#Remove-WildcardSafely "$env:LOCALAPPDATA\Microsoft\Windows\Explorer\thumbcache_*.db"

Write-Host "==> Recent Files cleanup" -ForegroundColor Cyan
Remove-WildcardSafely "$env:APPDATA\Microsoft\Windows\Recent\*"

Write-Host "==> User Downloads/Documents .tmp/.bak/.log cleanup" -ForegroundColor Cyan
Remove-WildcardSafely "$env:USERPROFILE\Downloads\*.tmp"
Remove-WildcardSafely "$env:USERPROFILE\Downloads\*.bak"
Remove-WildcardSafely "$env:USERPROFILE\Downloads\*.log"
Remove-WildcardSafely "$env:USERPROFILE\Documents\*.tmp"
Remove-WildcardSafely "$env:USERPROFILE\Documents\*.bak"
Remove-WildcardSafely "$env:USERPROFILE\Documents\*.log"

Write-Host "==> System Restore points cleanup" -ForegroundColor Cyan
try {
    $restorePoints = vssadmin list shadows 2>$null
    if ($restorePoints -match 'Shadow Copy ID') {
        vssadmin delete shadows /all /quiet
        Write-Host "All system restore points deleted" -ForegroundColor Green
    }
    else {
        Write-Host "No system restore points found" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "Failed to delete system restore points: $_" -ForegroundColor Red
}

Write-Host "==> Old Windows installations cleanup" -ForegroundColor Cyan
Remove-Safely "C:\Windows.old"
Remove-Safely "C:\$WINDOWS.~BT"

Write-Host "==> Windows Font cache cleanup" -ForegroundColor Cyan
Remove-WildcardSafely "$env:LOCALAPPDATA\FontCache\*"
Remove-WildcardSafely "$env:SystemRoot\ServiceProfiles\LocalService\AppData\Local\FontCache\*"

Write-Host "==> Windows Store cache cleanup" -ForegroundColor Cyan
Remove-Safely "$env:LOCALAPPDATA\Packages\Microsoft.WindowsStore_8wekyb3d8bbwe\LocalCache"

Write-Host "==> Docker cleanup (if Docker is installed)" -ForegroundColor Cyan
try {
    if (Get-Command docker -ErrorAction SilentlyContinue) {
        Write-Host "Found Docker installation, running cleanup..." -ForegroundColor Yellow
        & docker system prune -f 2>$null
        & docker image prune -a -f 2>$null
        & docker volume prune -f 2>$null
        Write-Host "Docker cleanup completed" -ForegroundColor Green
    } else {
        Write-Host "Docker not found or not in PATH" -ForegroundColor Yellow
    }
} catch {
    Write-Host "Docker cleanup failed: $_" -ForegroundColor Red
}

Write-Host "==> DirectX Shader cache cleanup" -ForegroundColor Cyan
Remove-WildcardSafely "$env:LOCALAPPDATA\D3DSCache\*"

Write-Host "==> Delivery Optimization files cleanup" -ForegroundColor Cyan
Remove-WildcardSafely "$env:SystemRoot\SoftwareDistribution\DeliveryOptimization\*"

Write-Host "==> Print Spooler cleanup" -ForegroundColor Cyan
Remove-WildcardSafely "$env:SystemRoot\System32\spool\PRINTERS\*"

Write-Host "==> Registry: Clean invalid startup entries (Run keys)" -ForegroundColor Cyan
function Remove-InvalidRunEntries {
    param([string]$RegPath)
    $keys = Get-ItemProperty -Path $RegPath -ErrorAction SilentlyContinue
    if ($keys) {
        foreach ($name in $keys.PSObject.Properties | Where-Object { $_.Name -ne 'PSPath' -and $_.Name -ne 'PSParentPath' -and $_.Name -ne 'PSChildName' -and $_.Name -ne 'PSDrive' -and $_.Name -ne 'PSProvider' }) {
            $value = $keys.$($name.Name)
            $exe = $value -replace '"', '' -replace ' .*', ''
            if ($exe -and -not (Test-Path $exe)) {
                Remove-ItemProperty -Path $RegPath -Name $name.Name -ErrorAction SilentlyContinue
                Write-Host "Removed invalid Run entry: $name ($exe)" -ForegroundColor Green
            }
        }
    }
}
Remove-InvalidRunEntries -RegPath 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
Remove-InvalidRunEntries -RegPath 'HKLM:\Software\Microsoft\Windows\CurrentVersion\Run'

Write-Host "==> Registry: Clear Explorer's recent/frequent items" -ForegroundColor Cyan
try {
    Remove-Item -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\LastVisitedPidlMRU" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\OpenSavePidlMRU" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "Cleared Explorer MRU lists" -ForegroundColor Green
}
catch {
    Write-Host "Failed to clear Explorer MRU lists: $_" -ForegroundColor Red
}

Write-Host "" -ForegroundColor Cyan
Write-Host "==> Top 20 largest files (>100MB) on C:\" -ForegroundColor Cyan
Show-LargestFiles -Path 'C:\' -Top 20 -MinSizeMB 100

Write-Host ""
Write-Host "==> Cleanup complete." -ForegroundColor Green
Write-Host "==> Recommend running Disk Cleanup (cleanmgr) for additional space recovery." -ForegroundColor Yellow