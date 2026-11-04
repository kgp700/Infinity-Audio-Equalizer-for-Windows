@echo on
setlocal
cd /d "%~dp0"
set "SETUP_TARGET=none"
call "build_windows.bat"
exit /b %errorlevel%
