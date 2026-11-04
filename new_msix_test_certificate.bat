@echo on
setlocal
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "tools\New-MSIXTestCertificate.ps1"
exit /b %errorlevel%
