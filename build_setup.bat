@echo on
setlocal EnableExtensions
cd /d "%~dp0"

if not exist "release\win-x64\InfinityAudioEqualizerProWindows.exe" (
  echo ERROR: Windows publish output is missing. Run build_windows.bat first.
  exit /b 1
)

rem The .csproj is the single source of truth for the application version.
set "APP_VERSION="
for /f "usebackq delims=" %%V in (`powershell -NoProfile -Command "[xml]$p = Get-Content -LiteralPath 'InfinityAudioEqualizerPro.csproj' -Raw; $v = $p.Project.PropertyGroup.Version; if (-not $v -or $v -notmatch '^\d+\.\d+\.\d+\.0$') { exit 3 }; $v -replace '\.0$',''"`) do set "APP_VERSION=%%V"
if not defined APP_VERSION (
  echo ERROR: Could not read the 4-part release version from the project file.
  exit /b 1
)

echo Packaging Infinity Audio Equalizer Pro for Windows v%APP_VERSION% Beta...
set "INNO_COMPILER="
if defined ISCC_EXE if exist "%ISCC_EXE%" set "INNO_COMPILER=%ISCC_EXE%"
if not defined INNO_COMPILER if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" set "INNO_COMPILER=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not defined INNO_COMPILER if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" set "INNO_COMPILER=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if not defined INNO_COMPILER if exist "%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" set "INNO_COMPILER=%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"
if not defined INNO_COMPILER if exist "%ProgramFiles%\Inno Setup 7\ISCC.exe" set "INNO_COMPILER=%ProgramFiles%\Inno Setup 7\ISCC.exe"
if not defined INNO_COMPILER if exist "%ProgramFiles(x86)%\Inno Setup 7\ISCC.exe" set "INNO_COMPILER=%ProgramFiles(x86)%\Inno Setup 7\ISCC.exe"
if not defined INNO_COMPILER if exist "%LOCALAPPDATA%\Programs\Inno Setup 7\ISCC.exe" set "INNO_COMPILER=%LOCALAPPDATA%\Programs\Inno Setup 7\ISCC.exe"
if not defined INNO_COMPILER (
  where ISCC.exe >nul 2>&1
  if not errorlevel 1 set "INNO_COMPILER=ISCC.exe"
)
if not defined INNO_COMPILER (
  echo ERROR: Inno Setup compiler ISCC.exe was not found.
  echo Install a licensed/eligible Inno Setup version from https://jrsoftware.org/isinfo.php
  echo Or set ISCC_EXE to the full path of ISCC.exe.
  exit /b 1
)
echo Using Inno Setup compiler: "%INNO_COMPILER%"
"%INNO_COMPILER%" /DAppVersion=%APP_VERSION% "installer\InfinityAudioEqualizerPro.iss"
if errorlevel 1 exit /b 1
if not exist "release\installer\Infinity-Audio-Equalizer-Pro-Windows-%APP_VERSION%-Beta-Setup.exe" (
  echo ERROR: Inno Setup finished but the expected installer file was not found.
  exit /b 1
)
echo SUCCESS: release\installer\Infinity-Audio-Equalizer-Pro-Windows-%APP_VERSION%-Beta-Setup.exe
exit /b 0
