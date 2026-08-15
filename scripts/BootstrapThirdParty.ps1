$ErrorActionPreference = "Stop"

$Version = "v0.11.0.3"
$Commit = "cb6e0966ac305202c47f1d1a81c105966e29da96"
$ArchiveUrl = "https://github.com/ramokz/phantom-camera/archive/refs/tags/$Version.zip"
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$Destination = Join-Path $RepoRoot "addons/phantom_camera"
$TempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("lime-phantom-camera-" + [System.Guid]::NewGuid().ToString("N"))
$ArchivePath = Join-Path $TempRoot "phantom-camera.zip"
$ExtractPath = Join-Path $TempRoot "extract"

try {
    New-Item -ItemType Directory -Path $TempRoot -Force | Out-Null
    New-Item -ItemType Directory -Path $ExtractPath -Force | Out-Null

    Write-Host "Downloading Phantom Camera $Version ($Commit)..."
    Invoke-WebRequest -Uri $ArchiveUrl -OutFile $ArchivePath
    Expand-Archive -Path $ArchivePath -DestinationPath $ExtractPath -Force

    $PluginConfig = Get-ChildItem -Path $ExtractPath -Recurse -File -Filter "plugin.cfg" |
        Where-Object { $_.FullName -match '[\\/]addons[\\/]phantom_camera[\\/]plugin\.cfg$' } |
        Select-Object -First 1

    if ($null -eq $PluginConfig) {
        throw "Could not locate addons/phantom_camera/plugin.cfg in the pinned archive."
    }

    $Source = Split-Path $PluginConfig.FullName -Parent
    New-Item -ItemType Directory -Path (Split-Path $Destination -Parent) -Force | Out-Null

    if (Test-Path $Destination) {
        Remove-Item $Destination -Recurse -Force
    }

    Copy-Item -Path $Source -Destination $Destination -Recurse -Force
    Set-Content -Path (Join-Path $Destination ".lime-version") -Value "$Version`n$Commit`n" -NoNewline

    Write-Host "Phantom Camera materialized at addons/phantom_camera."
}
finally {
    if (Test-Path $TempRoot) {
        Remove-Item $TempRoot -Recurse -Force
    }
}
