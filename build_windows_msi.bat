@echo on
setlocal
cd /d "%~dp0"
if errorlevel 1 goto :fail
rem Always build the EXE and MSI in one pass, regardless of inherited SETUP_TARGET.
set "SETUP_TARGET=msi"
rem The combined launcher owns the final pause; child scripts must not pause.
set "BUILD_NO_PAUSE=1"
call "build_windows.bat"
if errorlevel 1 goto :fail
if not exist "release\win-x64\InfinityAudioEqualizerProWindows.exe" (
  echo ERROR: Windows EXE was not found after publishing.
  goto :fail
)
echo.
echo ==========================================================
echo SUCCESS: Windows EXE and MSI build completed.
echo EXE: release\win-x64\InfinityAudioEqualizerProWindows.exe
echo MSI: release\installer\Infinity-Audio-Equalizer-Pro-Windows-*-Beta-Setup.msi
echo ==========================================================
echo.
pause
exit /b 0
:fail
echo.
echo ERROR: Windows EXE / MSI combined build failed.
echo Check the error messages above. A previous EXE/MSI may remain on disk.
echo.
pause
exit /b 1
