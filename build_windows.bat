@echo on
setlocal
cd /d "%~dp0"
dotnet --version || goto :fail
powershell -NoProfile -ExecutionPolicy Bypass -File "tools\Verify-ReleaseNotices.ps1" -Root "." || goto :fail
dotnet restore "InfinityAudioEqualizerPro.csproj" || goto :fail
powershell -NoProfile -Command "$json = @{build_date=(Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz')} | ConvertTo-Json -Compress; Set-Content -LiteralPath 'build_metadata.json' -Value $json -Encoding UTF8" || goto :fail
dotnet run --project "tests\RendererSmokeTests.csproj" -c Release || goto :fail
dotnet publish "InfinityAudioEqualizerPro.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:DebugType=None -p:DebugSymbols=false -o "release\win-x64" || goto :fail
powershell -NoProfile -ExecutionPolicy Bypass -File "tools\Verify-ReleaseNotices.ps1" -Root "release\win-x64" -Published || goto :fail
echo.
rem SETUP_TARGET: msi (default), inno (legacy), none (EXE only).
if not defined SETUP_TARGET set "SETUP_TARGET=msi"
if /I "%SETUP_TARGET%"=="inno" (
  call "build_setup.bat" || goto :fail
) else if /I "%SETUP_TARGET%"=="msi" (
  call "build_msi.bat" || goto :fail
) else if /I "%SETUP_TARGET%"=="none" (
  echo Installer packaging skipped.
) else (
  echo ERROR: Invalid SETUP_TARGET=%SETUP_TARGET%. Use inno, msi, or none.
  goto :fail
)
echo SUCCESS: release\win-x64\InfinityAudioEqualizerProWindows.exe
echo SUCCESS: installer packaging target %SETUP_TARGET% completed.
if not defined BUILD_NO_PAUSE pause
exit /b 0
:fail
echo BUILD FAILED. Check compiler, test, or installer errors above.
if not defined BUILD_NO_PAUSE pause
exit /b 1
