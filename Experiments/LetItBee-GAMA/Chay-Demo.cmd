@echo off
powershell.exe -NoProfile -File "%~dp0Start-Demo.ps1"
if errorlevel 1 pause
