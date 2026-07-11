[CmdletBinding()]
param(
    [string]$Version = '0.2.0',
    [switch]$IncludeSymbols
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$buildScript = Join-Path $PSScriptRoot 'build.ps1'
$outputDirectory = Join-Path $repositoryRoot 'src\KinectReframe.App\bin\Release'
$distDirectory = Join-Path $repositoryRoot 'dist'
$packageName = "Kinect-Reframe-$Version-win-x86"
$stageDirectory = Join-Path $distDirectory $packageName
$archivePath = Join-Path $distDirectory ($packageName + '.zip')

Write-Host 'Building release...' -ForegroundColor Cyan
& $buildScript -Configuration Release
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if (-not (Test-Path $outputDirectory)) {
    throw "Release output was not found: $outputDirectory"
}

if (Test-Path $stageDirectory) {
    Remove-Item $stageDirectory -Recurse -Force
}

New-Item $stageDirectory -ItemType Directory -Force | Out-Null

$files = @(
    'KinectReframe.App.exe',
    'KinectReframe.App.exe.config'
)

if ($IncludeSymbols) {
    $files += 'KinectReframe.App.pdb'
}

foreach ($file in $files) {
    $source = Join-Path $outputDirectory $file
    if (Test-Path $source) {
        Copy-Item $source $stageDirectory -Force
    }
}

Copy-Item (Join-Path $repositoryRoot 'README.md') $stageDirectory -Force
Copy-Item (Join-Path $repositoryRoot 'docs\VIDEO_RECORDING.md') $stageDirectory -Force
Copy-Item (Join-Path $repositoryRoot 'docs\UI_DESIGN.md') $stageDirectory -Force

$requirements = @'
Kinect Reframe requirements
===========================

- Windows 10 or Windows 11
- Xbox 360 Kinect with USB and external power adapter
- Kinect for Windows Runtime / SDK 1.8 installed
- x86 application support

Close Kinect Explorer, Kinect Studio and other sensor applications before starting Kinect Reframe.

User settings and crash logs are stored under:
%LOCALAPPDATA%\KinectReframe

Captures, videos, tracking recordings and point-cloud exports are written beside the executable.
'@

Set-Content -Path (Join-Path $stageDirectory 'REQUIREMENTS.txt') -Value $requirements -Encoding UTF8

if (Test-Path $archivePath) {
    Remove-Item $archivePath -Force
}

Compress-Archive -Path (Join-Path $stageDirectory '*') -DestinationPath $archivePath -CompressionLevel Optimal
Write-Host "Release package created: $archivePath" -ForegroundColor Green
