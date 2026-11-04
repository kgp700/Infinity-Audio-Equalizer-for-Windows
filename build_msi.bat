@echo off
setlocal
cd /d "%~dp0"
if not exist "release\win-x64\InfinityAudioEqualizerProWindows.exe" (
  echo ERROR: Publish output missing. Run build_no_setup.bat first.
  goto :end_error
)
where dotnet >nul 2>&1 || (
  echo ERROR: .NET SDK is required.
  goto :end_error
)
set "APP_VERSION="
for /f "usebackq delims=" %%V in (`powershell -NoProfile -Command "[xml]$p = Get-Content -LiteralPath 'InfinityAudioEqualizerPro.csproj' -Raw; $v=$p.Project.PropertyGroup.Version; if($v -notmatch '^\d+\.\d+\.\d+\.0$'){exit 3}; $v -replace '\.0$',''"`) do set "APP_VERSION=%%V"
if not defined APP_VERSION (
  echo ERROR: Invalid app version.
  goto :end_error
)
rem The WiX project uses a .NET SDK-based WiX Toolset package (NuGet restore on first build).
set "PUBLISH_DIR=%CD%\release\win-x64"
dotnet build "installer\InfinityAudioEqualizerPro.wixproj" -c Release -p:AppVersion=%APP_VERSION% -p:PublishDir="%PUBLISH_DIR%"
if errorlevel 1 goto :end_error
set "MSI_FILE=release\installer\Infinity-Audio-Equalizer-Pro-Windows-%APP_VERSION%-Beta-Setup.msi"
if not exist "%MSI_FILE%" (
  echo ERROR: Expected MSI output not found: %MSI_FILE%
  goto :end_error
)
echo SUCCESS: %MSI_FILE%
echo WARNING: MSI is unsigned. SmartScreen and enterprise policies may block it.
echo WARNING: Do not install alongside existing Inno/MSIX installations.
goto :end_ok

:end_error
echo.
if not defined BUILD_NO_PAUSE pause
exit /b 1

:end_ok
echo.
if not defined BUILD_NO_PAUSE pause
exit /b 0
