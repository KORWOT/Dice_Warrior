@echo off
setlocal
title Dice Warrior - Local Mock Server

echo Dice Warrior - Local Mock Server
echo Default address: http://127.0.0.1:5087
echo Dashboard:       http://127.0.0.1:5087/dev/dashboard
echo Keep this window open while playing. Press Ctrl+C to stop.
echo.

where.exe dotnet >nul 2>nul
if errorlevel 1 goto missing_sdk

"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Server\Start-MockServer.ps1" %*
set "server_exit=%errorlevel%"
if "%server_exit%"=="0" exit /b 0

echo.
echo Server stopped with exit code %server_exit%. See the error above.
pause
exit /b %server_exit%

:missing_sdk
echo .NET SDK 10.0.302 or a compatible 10.0.3xx patch is required.
echo Install the SDK, then run this file again.
pause
exit /b 1
