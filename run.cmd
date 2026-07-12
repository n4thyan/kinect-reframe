@echo off
setlocal
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\run.ps1" %*
set "exitcode=%errorlevel%"
if not "%exitcode%"=="0" (
  echo.
  echo Kinect Reframe did not launch successfully. Exit code: %exitcode%
  pause
)
exit /b %exitcode%
