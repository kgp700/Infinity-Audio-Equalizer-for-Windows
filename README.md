# Infinity Audio Equalizer Pro for Windows

Windows용 오디오 이퀄라이저입니다. EqualizerAPO-XT에 연결하여 출력 장치별로 31밴드 EQ, AutoEQ 및 음향 효과를 설정합니다.

## 빌드

`build_windows.bat`를 실행하세요. 자동 테스트 후 Windows용 실행 파일이 `release\win-x64`에 생성됩니다. Build date는 빌드 스크립트가 실제 빌드를 시작한 날짜와 시각으로 기록합니다.

## 처음 설치하는 경우

1. 프로그램에서 **EqualizerAPO-XT 설치**를 선택하고 **설치 파일 다운로드 및 실행**을 선택합니다. 공식 설치파일을 검증해 실행합니다. 자동 다운로드가 어려우면 공식 배포 웹페이지로 이동할 수 있습니다.
2. 설치가 완료되면 Device Selector가 자동으로 열립니다. 사용할 재생 장치를 등록하고 등록 도구를 종료하세요. Windows 재시작 안내가 표시되면 재시작합니다.
3. 장치 등록 도구가 정상 종료되면 프로그램이 현재 장치별 설정을 자동으로 연결·적용합니다. 기존 XT 설정은 자동으로 백업되며 완료창에서 백업 위치를 확인할 수 있습니다.
4. 이미 XT가 설치된 상태라면 **Equalizer APO 출력 장치 등록** 또는 **Equalizer APO 연결 / 다시 적용**을 사용할 수 있습니다. 소리가 달라지지 않으면 장치 등록과 Windows의 오디오 향상 기능을 확인합니다. 설정 파일 저장만으로 실제 APO 처리 여부까지 판단할 수는 없습니다.

XT를 이미 별도 폴더에 설치한 경우 설치 경로를 찾아 연결할 수 있습니다. 설치 여부는 `DeviceSelector.exe`와 `Editor.exe`의 존재로 확인합니다. `config.txt`만 남아 있는 폴더는 설치 완료 상태로 간주하지 않습니다.

## 설정과 해제

- 장치별 프로필, AutoEQ, 사용자 31밴드 EQ, 프리앰프 및 공간 음향을 설정할 수 있습니다.
- AutoEQ 곡선 창에는 **AutoEQ / 사용자 EQ / 합산 응답** 세 곡선이 있으며 개별적으로 표시하거나 숨길 수 있습니다. 곡선은 저장된 EQ 보정값을 비교하고 정적 프리앰프·공간감·컴프레서는 제외합니다.
- 출력 장치의 EQ를 처음부터 적용하려면 **Equalizer APO 연결 / 다시 적용**을 사용합니다.
- 개발·복구용 기능 중 APO **연결 해제**와 **연결 재구성**은 해당 연결 버튼을 마우스 오른쪽 버튼으로 클릭해 사용할 수 있습니다.
- **전체 효과 OFF**는 앱이 생성하는 모든 음향 필터를 우회합니다. EqualizerAPO-XT 자체를 제거하거나 Windows 장치 APO 등록을 해제하는 작업은 별개입니다.

설정 내보내기는 별도 선택창에서 **타인 공유용**(장치 ID·사용자 경로 제외)과 **내 컴퓨터용** 전체 백업 버튼으로 구분합니다.

## 프로그램 정보

버전 정보에서 실제 빌드 날짜·시간, 개인정보처리방침, 이용약관, 오픈소스 라이선스를 확인할 수 있습니다. 지속적으로 참조되는 README와 정책 문서에는 프로그램 버전 번호를 기재하지 않습니다.

## 개발 및 음향 검증

테스트 소스는 `tests`에 포함되어 있습니다. Windows 장치에서의 실제 소리 변화와 디바이스별 APO 등록 상태는 해당 컴퓨터에서 별도로 확인해야 합니다. 개발 및 오류 정보는 로그에 기록하고, 일반 사용자 화면에는 간결한 상태만 표시합니다.

## 설치·연결 흐름 및 실행 방식

- 프로그램을 다시 실행하면 기존 창이 전면으로 나타납니다. 한 번에 하나의 창만 실행됩니다.
- XT 설치 파일은 공식 릴리스에서 내려받고 해시를 확인한 후 실행합니다. 설치가 완료되면 출력 장치 등록 도구가 이어서 열립니다.
- 등록 도구를 정상 종료하면 현재 프리셋을 APO 설정에 즉시 연결하고 적용합니다. 드라이버 수준의 적용 여부는 실제 출력 장치 설정에 따라 달라질 수 있습니다.
- 처음 연결 시 기존 `config.txt`는 별도 파일에 자동 보관됩니다. 완료 안내에서 백업된 파일의 위치를 확인할 수 있습니다.
- 설치 폴더를 직접 찾는 창은 짧은 제목으로 표시하며 `Editor.exe`와 `DeviceSelector.exe`가 모두 있는 폴더만 허용합니다.
- EqualizerAPO-XT 자체의 장치 등록은 애플리케이션의 설정 연결과 별개입니다.

## 컴프레서와 공간 음향 조작

- 전방 비교/후방 비교: 전후 공간감을 켜고 버튼을 눌러 가상 음원 위치를 비교합니다. 다시 누르면 기존 음향 효과로 돌아갑니다.
- Studio Ambience 샘플레이트: 기본값은 현재 출력 장치의 자동 감지입니다. 감지할 수 없으면 수동으로 선택할 수 있습니다.
- 5밴드 컴프레서: 상세 조절에서 대역별 탭과 슬라이더를 사용합니다. [적용] 후 현재 설정을 저장합니다.

## 사용자 데이터 및 음향 설정 경로

앱 자체 설정과 로그는 `%LOCALAPPDATA%\InfinityAudioEqualizerPro`에 저장합니다.
프로필은 `settings.json`, 설정 백업은 `settings.json.bak`, 오류 로그는 `Logs\apo-apply.log`입니다.
EqualizerAPO-XT의 활성 설정 폴더는 Windows에 등록된 `HKLM\SOFTWARE\EqualizerAPO`의 `ConfigPath`를 우선 사용합니다.
음향 필터는 해당 폴더의 `InfinityAudioEqualizerPro\managed.txt`에 저장됩니다.
XT 설치 파일의 임시 다운로드 경로는 `%TEMP%\InfinityAudioEqualizerPro\XTInstall`입니다.

이전 개발 버전의 `%LOCALAPPDATA%\InfinityAudioEqualizerProCSharpPreview\settings.json`이 있고 신규 경로에 설정이 없다면 **최초 한 번만** 새 폴더로 복사합니다. 이전 파일은 삭제하지 않으며 복사 완료 표시를 남겨, 사용자가 새 설정을 초기화했을 때 이전 값이 다시 자동 복원되는 일을 막습니다.

기존 음향 설정에 `InfinityAudioEqualizerProCS` 연결이 남아 있다면 **Equalizer APO 연결 / 다시 적용**으로 새 관리 폴더에 다시 연결할 수 있습니다. 과거 관리 폴더는 자동 삭제하지 않으며 새 경로 적용 확인 후 정리할 수 있습니다.


장치 감지 및 AutoEQ 프로필은 Windows 출력 장치 ID별로 관리됩니다.

### 음향 조절 동작

5밴드 컴프레서 상세 창에서는 설정을 움직이는 동안 짧은 간격으로 반영됩니다. [취소] 또는 닫기를 누르면 이전 설정으로 돌아갑니다. 컴프레서 사용이 꺼져 있거나 APO가 연결되지 않았을 때는 출력에 즉시 반영되지 않습니다. Studio Ambience의 [잔향 적용 테스트]는 임시 확인 기능으로, 다시 누르면 이전 음향 효과로 복귀합니다.

### 컴프레서 상세 설정
Sub, Bass, Mid, High의 주파수 경계는 이웃 대역과 겹치지 않는 범위에서 조절됩니다. Air 대역은 20kHz로 고정됩니다. 컴프레서 상세창은 메인 화면과 동일한 어두운 색상을 사용합니다.

## 라이선스와 GitHub 공개

자체 C# 애플리케이션과 컴프레서 C++ 소스는 MIT 라이선스입니다.
상세 제3자 고지: `THIRD_PARTY_NOTICES.md`, `THIRD_PARTY_LICENSES/`,
`HRTF_DATA_CREDITS.txt`. 공개 전에는 별도로 빌드 및 배포 검증을 진행합니다.

GitHub 소스 저장소에는 바이너리를 올리지 않습니다.
컴프레서 VST3 바이너리는 검증된 빌드 결과를 GitHub Releases에 별도 배포하며,
현재 FULL 개발 ZIP에만 로컬 테스트용 바이너리가 포함되어 있습니다.
GitHub에서 소스를 새로 받은 경우 VST3 SDK 3.8+를 별도 체크아웃해
`vst3/README_BUILD.txt`에 따라 플러그인을 빌드해야 합니다.

## Windows 설치형 패키지 빌드

`build_windows.bat`는 테스트와 .NET 자체 포함 배포 파일을 생성한 후 Inno Setup 6.5 이상으로 설치 프로그램까지 제작합니다. 설치 스크립트는 `installer/InfinityAudioEqualizerPro.iss`, 생성된 Setup.exe는 `release/installer/`에 위치합니다. 상세 절차와 제거 시 임시 파일만 선택적으로 정리하는 정책은 [설치 및 제거 가이드](INSTALLER_BUILD_AND_UNINSTALL.md)를 참고하세요.

## 신규 장치 프로필 기본값

- Studio Ambience 잔향 길이: **1000ms** (Studio Ambience 사용은 기존 기본 OFF 유지)
- Surround Stage: **사용 OFF**, 모드 **Cinema**, 처리 방식 **HRTF 공간감**, 전후 공간감 **ON**
- 5밴드 컴프레서: **사용 OFF**, 음향 테마 **자연스러운 개방감**, 테마 강도 **12**

위 값들은 **새로 생성되는 장치 프로필**에만 적용됩니다. 기존에 저장된 프로필을 덮어쓰지 않습니다. `전체 설정 초기화`와 같은 중립화 동작은 기존 안전 규칙을 유지합니다.



### 설치 프로그램 없이 빌드

`build_no_setup.bat`를 실행하면 .NET 테스트 및 실행 파일 게시까지만 수행하며 Inno Setup 패키징을 건너뜁니다. 기존 `build_windows.bat`의 기본 Inno Setup 동작은 유지합니다.


## MSI packaging option (WiX)

The original Inno Setup EXE and MSIX test builds remain available. An optional **unsigned MSI** build is included via `build_windows_msi.bat` (full build) or `build_msi.bat` (package existing publish output). See `INSTALLER_BUILD_AND_UNINSTALL.md` for installation and uninstallation guidance.
