@echo on
setlocal
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "tools\Build-MSIX.ps1" -Root "%cd%" %*
exit /b %errorlevel%
