# Package script for Rotatonator release builds
# This script safely packages the release build, preventing duplicate runtime folders

param(
    [string]$Version = "1.6.0"
)

$ErrorActionPreference = "Stop"

Write-Host "Packaging Rotatonator v$Version..." -ForegroundColor Cyan
Write-Host ""

# Define paths
$projectPath = ".\Rotatonator"
$buildOutputPath = "$projectPath\bin\Release\net8.0-windows"
$publishPath = ".\publish\Rotatonator-v$Version"
$zipPath = ".\publish\Rotatonator-v$Version-win-x64.zip"

# Check if build output exists
if (-not (Test-Path $buildOutputPath)) {
    Write-Host "ERROR: Build output not found at $buildOutputPath" -ForegroundColor Red
    Write-Host "Please run build.ps1 first" -ForegroundColor Yellow
    exit 1
}

Write-Host "Cleaning old publish directory..." -ForegroundColor Cyan
if (Test-Path $publishPath) {
    Remove-Item $publishPath -Recurse -Force
}
New-Item -ItemType Directory -Path $publishPath -Force | Out-Null

Write-Host "Copying release files..." -ForegroundColor Cyan
Copy-Item -Path "$buildOutputPath\*" -Destination $publishPath -Recurse -Force

# PACKAGING GUARD: Remove nested runtime/platform folders to prevent duplication
Write-Host "Applying packaging guards (removing duplicate runtime folders)..." -ForegroundColor Cyan
$nestedRuntimeFolders = @("win-x64", "win-x86", "win-arm64", "linux-x64", "osx-x64")
foreach ($folder in $nestedRuntimeFolders) {
    $nestedPath = Join-Path $publishPath $folder
    if (Test-Path $nestedPath) {
        Write-Host "  - Removing duplicate: $folder/" -ForegroundColor Yellow
        Remove-Item $nestedPath -Recurse -Force
    }
}

# Stop any running Rotatonator processes to avoid file locks
Write-Host "Checking for running Rotatonator processes..." -ForegroundColor Cyan
$runningProcess = Get-Process -Name "Rotatonator" -ErrorAction SilentlyContinue
if ($runningProcess) {
    Write-Host "  - Stopping running Rotatonator process..." -ForegroundColor Yellow
    $runningProcess | Stop-Process -Force
    Start-Sleep -Milliseconds 500
}

# Create zip package
Write-Host "Creating release package..." -ForegroundColor Cyan
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}
Compress-Archive -Path "$publishPath\*" -DestinationPath $zipPath -CompressionLevel Optimal

# Display results
Write-Host ""
Write-Host "Package created successfully!" -ForegroundColor Green
$zipInfo = Get-Item $zipPath
$sizeMB = [math]::Round($zipInfo.Length / 1MB, 2)
Write-Host ""
Write-Host "Package: $($zipInfo.FullName)" -ForegroundColor Yellow
Write-Host "Size: $sizeMB MB" -ForegroundColor Yellow
Write-Host "Created: $($zipInfo.LastWriteTime)" -ForegroundColor Yellow
Write-Host ""

# Verify no duplicate runtime folders in zip
Write-Host "Verifying package integrity..." -ForegroundColor Cyan
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
$duplicateEntries = $archive.Entries | Where-Object { 
    $_.FullName -match '^(win-x64|win-x86|win-arm64|linux-x64|osx-x64)/' 
}
$archive.Dispose()

if ($duplicateEntries) {
    Write-Host "WARNING: Found unexpected runtime folder entries in package!" -ForegroundColor Red
    $duplicateEntries | ForEach-Object { Write-Host "  - $($_.FullName)" -ForegroundColor Yellow }
} else {
    Write-Host "  [OK] No duplicate runtime folders detected" -ForegroundColor Green
}

Write-Host ""
Write-Host "Done!" -ForegroundColor Green
