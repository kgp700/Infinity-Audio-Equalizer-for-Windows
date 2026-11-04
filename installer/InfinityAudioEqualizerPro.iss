#define AppName "Infinity Audio Equalizer Pro for Windows"
#ifndef AppVersion
  #define AppVersion "0.8.28"
#endif
#define AppExe "InfinityAudioEqualizerProWindows.exe"
#define AppPublisher "KGP Software Lab"

[Setup]
AppId={{CCFC7F86-47FA-47CF-B7F3-C9FA3CC6F36B}
AppName={#AppName}
AppVersion={#AppVersion} Beta
AppVerName={#AppName} {#AppVersion} Beta
AppPublisher={#AppPublisher}
AppPublisherURL=https://github.com/kgp700/Infinity-Audio-Equalizer-for-Windows
AppSupportURL=https://github.com/kgp700/Infinity-Audio-Equalizer-for-Windows/issues
AppUpdatesURL=https://github.com/kgp700/Infinity-Audio-Equalizer-for-Windows/releases
DefaultDirName={autopf}\Infinity Audio Equalizer Pro
DefaultGroupName=Infinity Audio Equalizer Pro for Windows
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
SetupIconFile=..\assets\Infinity_Audio_Equalizer.ico
UninstallDisplayIcon={app}\{#AppExe}
LicenseFile=..\LICENSE
OutputDir=..\release\installer
OutputBaseFilename=Infinity-Audio-Equalizer-Pro-Windows-{#AppVersion}-Beta-Setup
VersionInfoVersion={#AppVersion}.0
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} Setup
VersionInfoProductName={#AppName}
CloseApplications=yes
RestartApplications=no
AppMutex=Local\InfinityAudioEqualizerProWindows.Instance
UsePreviousAppDir=yes
ChangesEnvironment=no

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "바탕 화면 바로가기 만들기"; GroupDescription: "바로가기:"; Flags: unchecked

[Files]
; Install only Windows publish outputs, never the Git source tree or private settings.
Source: "..\release\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"

[Icons]
Name: "{autoprograms}\Infinity Audio Equalizer Pro for Windows"; Filename: "{app}\{#AppExe}"; WorkingDir: "{app}"
Name: "{autodesktop}\Infinity Audio Equalizer Pro for Windows"; Filename: "{app}\{#AppExe}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "Infinity Audio Equalizer Pro for Windows 실행"; Flags: nowait postinstall skipifsilent

[Code]
var
  CleanupTemporaryFiles: Boolean;

function InitializeUninstall(): Boolean;
var
  ResultCode: Integer;
  AppFile: String;
begin
  Result := True;
  // Disconnect our managed APO Include BEFORE the uninstaller removes its
  // local VST3 binaries. The helper touches only its own marker and makes a
  // timestamped backup. It does not unregister or uninstall XT.
  AppFile := ExpandConstant('{app}\{#AppExe}');
  if FileExists(AppFile) then
  begin
    ResultCode := 0;
    if not Exec(AppFile, '--uninstall-detach', ExpandConstant('{app}'),
      SW_HIDE, ewWaitUntilTerminated, ResultCode) or (ResultCode <> 0) then
    begin
      Result := MsgBox('음향 효과 연결을 자동으로 해제하지 못했습니다.' + #13#10 +
        '제거를 계속하면 EqualizerAPO-XT의 설정에 이전 연결이 남을 수 있습니다.' + #13#10 +
        '먼저 앱에서 APO 연결을 해제하는 것이 좋습니다.' + #13#10 + #13#10 +
        '그래도 프로그램을 제거하시겠습니까?',
        mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES;
      if not Result then Exit;
    end;
  end;
  // Default is NO. This never opts into removing presets or audio profiles.
  CleanupTemporaryFiles :=
    MsgBox('프로그램 제거 시 임시 설치 파일과 오류 로그도 삭제하시겠습니까?' + #13#10 + #13#10 +
      '삭제 대상:' + #13#10 +
      ' - Infinity Audio Equalizer Pro의 XT 설치용 임시 다운로드 파일' + #13#10 +
      ' - 이 Windows 계정에 기록된 프로그램 오류 로그' + #13#10 + #13#10 +
      '사용자 설정, 프리셋, 설정 백업, EqualizerAPO-XT 및 다른 프로그램 파일은 삭제하지 않습니다.',
      mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES;
  Result := True;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  TempDownloads, AppLogs: String;
begin
  if (CurUninstallStep = usPostUninstall) and CleanupTemporaryFiles then
  begin
    // Explicit, application-owned subfolders only. Never delete the parent
    // %LOCALAPPDATA%\InfinityAudioEqualizerPro because settings.json lives there.
    TempDownloads := AddBackslash(GetEnv('TEMP')) + 'InfinityAudioEqualizerPro\XTInstall';
    AppLogs := AddBackslash(ExpandConstant('{localappdata}')) + 'InfinityAudioEqualizerPro\Logs';
    if DirExists(TempDownloads) then
      if not DelTree(TempDownloads, True, True, True) then
        Log('Unable to remove optional temporary downloads: ' + TempDownloads);
    if DirExists(AppLogs) then
      if not DelTree(AppLogs, True, True, True) then
        Log('Unable to remove optional diagnostic logs: ' + AppLogs);
  end;
end;
