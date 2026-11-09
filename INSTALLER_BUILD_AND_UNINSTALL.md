# Inno Setup 설치형 빌드 및 제거 정책

## 설치 프로그램 생성

1. Windows에서 **.NET 10 SDK**와 **Inno Setup 6.5 이상(6.x)**을 설치합니다. Inno Setup 공식 배포: https://jrsoftware.org/isinfo.php
2. `build_windows.bat`를 실행하면 테스트 → Windows x64 self-contained publish → 저작권 고지 확인 → Inno Setup 컴파일을 연속 수행합니다.
3. 생성물: `release\installer\Infinity-Audio-Equalizer-Pro-Windows-0.8.1-Beta-Setup.exe`
4. 이미 `release\win-x64`에 정상 publish 결과물이 있으면 `build_setup.bat`로 설치 파일만 다시 생성할 수 있습니다.
5. Inno Setup이 비표준 경로에 있다면 `ISCC_EXE` 환경 변수에 `ISCC.exe` 전체 경로를 지정합니다.

## 설치 동작

- `Program Files\Infinity Audio Equalizer Pro` 아래에 프로그램, 자체 컴프레서 VST3 및 저작권 고지 설치
- 시작 메뉴 항목 생성, 바탕화면 바로가기는 선택 가능
- .NET 10 런타임 자체 포함 (별도 데스크톱 런타임 설치 불필요)
- EqualizerAPO-XT 설치 및 장치 등록은 앱이 안내하며, 설치 파일 안에 XT 배포물을 동봉하지 않음
- 기존 사용자 설정·프리셋은 설치 과정에서 덮어쓰지 않음

## 프로그램 제거

- 제거 시작 시 우리 프로그램이 연결한 APO 필터가 존재하면 그 필터를 먼저 안전하게 분리하고 기존 설정을 시각 기록 백업으로 보존합니다. **XT 설치 및 출력 장치 등록은 그대로 유지합니다.**
- 연결 분리 실패 시 사용자에게 원인을 안내하고 제거 계속 여부를 선택하게 합니다.
- 별도 선택 창에서 **임시 설치 파일과 오류 로그 삭제** 여부를 묻습니다. 기본 선택은 **삭제하지 않음**입니다.
- 선택적으로 지우는 폴더는 `%TEMP%\InfinityAudioEqualizerPro\XTInstall` 및 `%LOCALAPPDATA%\InfinityAudioEqualizerPro\Logs`입니다.
- **절대 자동 삭제하지 않는 항목**: `%LOCALAPPDATA%\InfinityAudioEqualizerPro\settings.json`, `.bak`, 장치별 프리셋, XT 자체·장치 등록·기존 XT 설정 백업.
- 관리자 계정이 다른 경우 임시/로그 정리는 해당 계정의 경로로 해석될 수 있으므로, 다른 사용자의 데이터를 찾아 지우지 않습니다.
- Inno Setup 제거는 설치된 파일만 추적해 삭제합니다. 사용자 설정 전체 폴더를 재귀 삭제하지 않습니다.

## 테스트 시나리오

- 깨끗한 PC, 미설치 XT 상태에서 Setup 실행 → 앱 실행 → XT 별도 설치/등록 → EQ 적용
- 같은 버전 재설치 → 설정 보존 여부
- 프로그램 실행 중 업그레이드 → 설치가 실행 중 앱을 감지하고 종료를 요구하는지
- XT 연결된 상태에서 제거 → 연결 분리, XT 잔존, 백업 확인
- 제거 선택 `아니요` → 로그/임시 다운로드 남음, 설정 남음
- 제거 선택 `예` → 로그/임시 다운로드 삭제, 설정 남음
- Inno Setup 미설치 상태에서 빌드 → 실행 파일 빌드 후 설치 프로그램 생성 오류가 명확히 표시되는지

이 문서는 빌드/제거 동작의 의도와 수동 검증 항목입니다. 별도 Windows에서 실제 Inno Setup 컴파일 및 제거 시험이 필요합니다.


## Compiler lookup

`build_setup.bat` now checks user/system installations of Inno Setup 6 and 7,
`ISCC_EXE`, and the system PATH. Set `ISCC_EXE` explicitly for a nonstandard
installation. Confirm that your Inno Setup edition is appropriately licensed.
The installer filename is generated from `<Version>` in the C# project file.

## Build without installer

Run `build_no_setup.bat` to build/test/publish only. This is equivalent to `set SETUP_TARGET=none` followed by `build_windows.bat`; Inno Setup is not invoked.


## MSI packaging option (WiX)

The original Inno Setup EXE and MSIX test builds remain available. An optional **unsigned MSI** build is included via `build_windows_msi.bat` (full build) or `build_msi.bat` (package existing publish output). See the MSI build scripts and WiX project for installer details.
