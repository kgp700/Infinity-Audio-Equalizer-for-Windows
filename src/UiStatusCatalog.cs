// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
namespace InfinityAudioEqualizerPro;
internal static class UiStatusCatalog
{
    internal const string XtMissing = "EqualizerAPO-XT가 설치되어 있지 않습니다. 아래 설치 버튼을 눌러 주세요.";
    internal const string InstallOrder = "EqualizerAPO-XT를 설치한 다음 출력 장치를 등록해 주세요.";
    internal const string MissingConfig = "EqualizerAPO-XT 설정 경로를 찾지 못했습니다. 경로를 확인해 주세요.";
    internal const string BridgeRequired = "음향 효과 연결이 필요합니다. 아래 연결 버튼을 눌러 주세요.";
    internal const string SettingsSaved = "EQ 설정 파일 저장 완료";
    internal const string Bypassed = "전체 효과 OFF";
    internal const string DeviceTestHint = "EQ가 적용되지 않으면 출력 장치 등록 상태와 현재 재생 장치를 확인하세요.";
    internal const string ConnectionCanceled = "연결 설정을 취소했습니다. 연결 버튼을 눌러 다시 시도할 수 있습니다.";
    internal const string PermissionDenied = "설정을 변경할 권한이 없습니다. 관리자 권한 요청을 승인해 주세요.";
    internal const string DeviceNotRegistered = "출력 장치 등록이 필요합니다. 아래 장치 등록 버튼을 눌러 주세요.";
    internal const string Downloading = "EqualizerAPO-XT 다운로드 중...";
    internal const string LaunchingInstaller = "설치 프로그램을 실행했습니다. 설치 후 출력 장치를 등록해 주세요.";
    internal const string SelectorLaunched = "출력 장치 등록 창을 열었습니다. 사용할 장치를 선택해 주세요.";
    internal const string AppliedError = "음향 설정을 적용하지 못했습니다. 다시 시도해 주세요.";
    internal static string HelpText => "음향 효과 연결 및 설정 상태는 화면의 안내에 따라 확인해 주세요.";
    // Technical error details and write verification are recorded in DiagnosticLog,
    // not presented as success or failure claims about audible output.
}
