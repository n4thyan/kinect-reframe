[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$buildScript = Join-Path $PSScriptRoot 'build.ps1'
$executable = Join-Path $repositoryRoot "src\KinectReframe.App\bin\$Configuration\KinectReframe.App.exe"

& $buildScript -Configuration $Configuration
if ($LASTEXITCODE -ne 0) {
    Write-Host ''
    Write-Host 'Build failed. Kinect Reframe was not launched.' -ForegroundColor Red
    exit $LASTEXITCODE
}

if (-not (Test-Path $executable)) {
    throw "Build reported success, but the executable was not found at: $executable"
}

Write-Host ''
Write-Host "Launching $executable" -ForegroundColor Green
& $executable
