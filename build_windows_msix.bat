@echo on
setlocal
cd /d "%~dp0"
call "build_no_setup.bat" || exit /b 1
call "build_msix.bat" %* || exit /b 1
exit /b 0
