@echo off
setlocal
set "SCRIPT_DIR=%~dp0"
echo.
echo ================================================================
echo   CLEAN PROJECT - Keep Git metadata
echo ================================================================
echo.
where powershell.exe >nul 2>&1
if errorlevel 1 (
  echo [ERROR] Windows PowerShell was not found. Nothing was deleted.
  pause
  exit /b 1
)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%CLEAN_PROJECT.ps1"
set "EXIT_CODE=%ERRORLEVEL%"
echo.
if "%EXIT_CODE%"=="0" (echo [OK] Clean operation finished.) else (echo [INFO] Clean operation cancelled, blocked, or incomplete. Exit code: %EXIT_CODE%)
pause
exit /b %EXIT_CODE%
