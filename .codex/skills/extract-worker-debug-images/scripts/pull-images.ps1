[CmdletBinding()]
param(
  [string]$Context = "vps-root",
  [string]$Container = "embassy-access-worker",
  [string]$RemoteDir = "/app/debug",
  [string]$Pattern = "*.png",
  [string]$Destination = ".vps/embassy-access/debug",
  [int]$MaxCount = 0
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
  throw "docker CLI not found in PATH."
}

$destPath = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $Destination))
New-Item -ItemType Directory -Path $destPath -Force | Out-Null

$remoteListCmd = "ls -1t $RemoteDir/$Pattern 2>/dev/null || true"
$rawList = docker --context $Context exec $Container sh -lc $remoteListCmd

$files = @($rawList | Where-Object { $_ -and $_.Trim() -ne "" })
if ($files.Count -eq 0) {
  Write-Output "No files matched '$Pattern' in '$RemoteDir' for container '$Container' (context '$Context')."
  exit 0
}

if ($MaxCount -gt 0) {
  $files = @($files | Select-Object -First $MaxCount)
}

$copied = @()
foreach ($remoteFile in $files) {
  $name = Split-Path -Path $remoteFile -Leaf
  $localFile = Join-Path $destPath $name
  docker --context $Context cp "${Container}:${remoteFile}" $localFile | Out-Null
  $copied += $localFile
}

Write-Output ("Copied {0} file(s):" -f $copied.Count)
$copied | ForEach-Object { Write-Output $_ }
