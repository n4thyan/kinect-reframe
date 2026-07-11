[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$buildScript = Join-Path $PSScriptRoot 'build.ps1'
$executable = Join-Path $repositoryRoot "src\KinectReframe.App\bin\$Configuration\KinectReframe.App.exe"

$runningProcesses = Get-CimInstance Win32_Process -Filter "Name = 'KinectReframe.App.exe'" -ErrorAction SilentlyContinue
if ($runningProcesses) {
    Write-Host 'Stopping the currently running Kinect Reframe instance...' -ForegroundColor Yellow

    foreach ($process in $runningProcesses) {
        try {
            Stop-Process -Id $process.ProcessId -Force -ErrorAction Stop
        }
        catch {
            Write-Warning "Could not stop Kinect Reframe process $($process.ProcessId): $($_.Exception.Message)"
        }
    }

    Start-Sleep -Milliseconds 500
}

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
