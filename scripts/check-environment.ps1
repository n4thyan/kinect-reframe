[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

function Write-Check {
    param(
        [string]$Name,
        [bool]$Passed,
        [string]$Detail
    )

    $marker = if ($Passed) { '[OK]' } else { '[!!]' }
    $colour = if ($Passed) { 'Green' } else { 'Yellow' }
    Write-Host "$marker $Name" -ForegroundColor $colour
    if ($Detail) {
        Write-Host "     $Detail"
    }
}

Write-Host 'Kinect Reframe environment check' -ForegroundColor Cyan
Write-Host '---------------------------------'

$isWindows = $env:OS -eq 'Windows_NT'
Write-Check 'Windows' $isWindows $env:OS

$sdkCandidates = @(
    $env:KINECTSDK10_DIR,
    "$env:ProgramFiles\Microsoft SDKs\Kinect\v1.8",
    "${env:ProgramFiles(x86)}\Microsoft SDKs\Kinect\v1.8"
) | Where-Object { $_ } | Select-Object -Unique

$sdkRoot = $sdkCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
$assemblyPath = if ($sdkRoot) { Join-Path $sdkRoot 'Assemblies\Microsoft.Kinect.dll' } else { $null }

Write-Check 'Kinect SDK 1.8' ([bool]$sdkRoot) $sdkRoot
Write-Check 'Microsoft.Kinect.dll' ([bool]($assemblyPath -and (Test-Path $assemblyPath))) $assemblyPath

$targetingPack = Test-Path "$env:ProgramFiles(x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"
Write-Check '.NET Framework 4.8 targeting pack' $targetingPack "$env:ProgramFiles(x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"

$kinectDevices = @()
if (Get-Command Get-PnpDevice -ErrorAction SilentlyContinue) {
    $kinectDevices = Get-PnpDevice -ErrorAction SilentlyContinue |
        Where-Object { $_.FriendlyName -match 'Kinect|Xbox NUI' }
}

Write-Check 'Kinect USB devices detected' ($kinectDevices.Count -gt 0) "$($kinectDevices.Count) matching device(s)"
if ($kinectDevices.Count -gt 0) {
    $kinectDevices | Format-Table Status, Class, FriendlyName -AutoSize
}

if (-not $sdkRoot -or -not $assemblyPath -or -not (Test-Path $assemblyPath)) {
    Write-Host ''
    Write-Host 'Install Kinect for Windows SDK 1.8, then reopen PowerShell.' -ForegroundColor Yellow
    exit 1
}

Write-Host ''
Write-Host 'Core Kinect development dependencies were found.' -ForegroundColor Green
