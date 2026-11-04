## v0.8.28 Beta — 출력 장치 등록 후 공통 연결 완료 절차

- 출력 장치 선택기 종료 후 종료 코드만으로 등록 실패를 단정하지 않고 실제 Windows 등록 정보를 재확인합니다.
- 출력 장치 등록, XT 설정 경로 검색, Infinity Audio Equalizer Pro 관리 설정 연결, 저장된 DSP 설정 적용 및 기록 검증을 하나의 공통 후속 절차로 연결했습니다.
- 전체 효과 ON 시 음향 설정 미연결 상태에서도 동일한 후속 절차를 재사용합니다.
- 설정 경로 인식 지연은 짧게 재확인하고, 사용자의 재등록을 자동으로 요청하거나 장치 선택기를 반복 실행하지 않습니다.
- 원래 저장한 전체 효과 ON/OFF 상태를 변경하지 않습니다.
- 실제 Windows 장치 등록/오디오 출력 통합 테스트는 미실행입니다.

## v0.8.27 Beta — 출력 장치 등록 후 연결 동기화

- DeviceSelector 종료 직후 Windows APO 등록 정보 및 XT 설정 경로가 늦게 반영되는 상황에 대비해 짧은 재확인 과정을 추가했습니다.
- 같은 등록 작업에서 설정 경로 확인 후 연결 및 저장된 음향 설정 반영을 수행합니다. 장치 선택기를 자동으로 다시 실행하지 않습니다.
- 설정 경로 확인 실패 시 즉시 중복 등록이나 폴더 선택 팝업을 띄우지 않고 설정 경로 버튼으로 직접 지정할 수 있게 안내합니다.
- 장치 등록 여부 확인 불가와 미등록을 구별해 성공 상태를 과장하지 않습니다.
- Windows 환경의 실제 실행·장치 등록 통합 테스트는 미수행했습니다.

## v0.8.25 Beta — 최초 실행 기본 전체 효과 OFF

- `AppSettings.GlobalEnabled`의 신규 사용자 기본값을 `false`로 변경했습니다. `BypassAllEffects=true` 기본값은 유지합니다.
- 기존 `settings.json`이 있으면 저장된 ON/OFF를 그대로 불러오며, Windows 로그인 자동 실행도 상태를 강제로 바꾸지 않습니다.
- 신규 사용자 OFF 및 직전 ON/OFF 복원에 대한 smoke test를 추가했습니다.
- 새 설치 환경 재현을 위한 파일·이전 개발판 이관·XT 설정 주의사항을 문서화했습니다.
- 프로그램 및 MSI 빌드 절차는 변경하지 않았습니다.

## v0.8.23 Beta — EqualizerAPO-XT reinstall confirmation

- Before showing the existing XT installation notice, check for a valid installed XT folder with both Editor.exe and DeviceSelector.exe via XtSetupService.FindDeviceSelector.
- If already installed, ask whether to reinstall/update; default to No. No download or installer launch occurs if declined.
- If confirmed, retain the existing bold installation notice and explicit consent step before downloading.
- Change only the configuration-editor name in the installation warning to EqualizerAPO-XT(EQ APO XT AVX2).
- Unchanged: download verification, device registration, DSP, MSI build flow, and XT uninstallation behavior.

## v0.8.22 Beta — balanced window margins and product name

- Set main client width to 1140px (from 1160px), matching the existing 1140px design canvas and 12px content margins on both sides without changing the canvas, vertical layout, or scroll extent.
- Replaced shortened standalone product name in UI headings, tooltip text, dialogs, and human-readable documentation with the full `Infinity Audio Equalizer Pro` name.
- Kept stable settings paths, APO markers, VST3 filenames, registry value names, code namespaces, and compatibility identifiers unchanged.
- No DSP or MSI build-flow changes.

## v0.8.21 Beta

Reduced the shared extra checkbox-to-help icon spacing from 7px to 1px for all seven inline icons. Preserved measured text width and 30px glyph/text safety space; no layout or engine changes.

## v0.8.20 Beta — Windows login: tray-only autostart

- Windows Run registry command now includes `--startup` when the user enables login autostart.
- `Program.Main` distinguishes `--startup` from explicit/manual EXE or shortcut launches; repeated autostart invocation will not foreground an existing instance.
- MainForm retains its normal Load/device/APO initialization and tray icon but starts invisible without a taskbar entry only when passed `--startup`; normal launches continue showing the main window.
- Skip interactive initial APO installation/configuration guidance on Windows login only. Preserve guidance on manual launch.
- Existing same-path, argument-free Run entries are migrated to `--startup`; no autostart option is switched on or off automatically.
- No changes to DSP, installer logic, main window dimensions, or scrolling.
- Linux-based source/assertion/ZIP checks only; Windows runtime and MSI build not executed here.

## v0.8.19 Beta

WinForms: unify seven checkbox help-icon offsets using final label text metrics; remove per-row correction/clamping, recalculate checkbox widths to prevent cut-off. Shift Stereo Width, Surround Stage and Studio Ambience adjacent controls within existing section bounds. Retain EXE+MSI builder.

## v0.8.18 Beta

- ⓘ 대표 기능 도움말 아이콘을 18px / 10.5pt로 확장하고 간격을 정적 EQ 모드 기준으로 보정.
- 스테레오 확장 / Surround Stage / 전후 공간감 아이콘 위치를 미세 조정.
- Studio Ambience 강도·잔향 길이·샘플레이트·감지 결과 및 테스트 버튼의 가로 좌표를 오른쪽으로 이동하여 도움말과 겹침 방지.
- 기본 창 크기 및 스크롤 크기 변화 없음.

## v0.8.12 Beta — APO endpoint registration and switch states
- Read-only Windows render endpoint FxProperties probe, distinguishing APO config-link from installed endpoint registration.
- Detect cancelled device registration and guide registration on the next ON request.
- Show ON/OFF after user switch interactions, not settings-saved diagnostics.

## v0.8.11 Beta — 사용자 상태 메시지 정리
- 출력 적용 미확인, 장치 적용 미확인 등 내부 진단성 문구를 앱 상태표시줄/팝업/툴팁에서 제거.
- 저장/연결/설치/ON 전환 등 실제 수행한 작업을 기준으로 메시지 변경.
- 내부 교차 진단 불일치 및 출력 장치 등록 판단 유보는 진단 로그에만 기록하며 사용자 화면에 표시하지 않음.
- 기존 APO 복구/전체 효과 ON·OFF 동작은 유지.

## v0.8.10 Beta — ON activation cross-check
- Added an independent file/installation diagnostic for XT installation, config directory, and Infinity Audio Equalizer Pro bridge link.
- Captures status bar observations before asynchronous ON application can overwrite them.
- Modal recovery is offered only when independent diagnosis corroborates the existing status messages.
- If inconsistent, reports a nonmodal warning and records a diagnostic.
- Endpoint registration cannot be proven by selector return; no automatic registration prompt on a heuristic alone.
- OFF and exit remain unconditional.

## MSI ICE69 shortcut fix — no application version change

- Replaced cross-feature `[#MainExeFile]` shortcut targets with `[INSTALLFOLDER]InfinityAudioEqualizerProWindows.exe` for both optional shortcut features.
- Preserved blue MSI wizard, licensing, path selection, optional shortcuts, and post-install launch.
- Only MSI WiX authoring and packaging documents changed; real Windows MSI build remains unverified.

## MSI packaging correction — application version unchanged (v0.8.6 Beta)

- Separated the Start Menu shortcut from the per-machine executable WiX component; added HKCU registry KeyPath and uninstall-time shortcut directory cleanup to address ICE43, ICE57, and ICE64.
- MSI packaging source/docs only; no application code, UI, DSP, or application-version changes. Windows WiX build and install/uninstall validation are still pending.

# v0.8.6 Beta

- Added experimental full-trust MSIX test packaging and documentation; Inno Setup remains intact.
- Added explicit test signing workflow. No changes to audio processing.

## v0.8.5 Beta — InstallForge integration removed

- Removed `build_installforge.bat`, `tools/Build-InstallForge.ps1`, `installer/InfinityAudioEqualizerPro.ifp`, `INSTALLFORGE_BUILD_AND_LIMITATIONS.md`.
- `build_windows.bat` accepts `SETUP_TARGET=inno` (default) or `none` only; invalid target values fail explicitly.
- Preserved `build_no_setup.bat`, `build_setup.bat`, Inno Setup installer/uninstaller implementation, all DSP paths and tests.
- Synchronized csproj, Inno template, user documentation and release notes with v0.8.5 Beta.
- Historical release notes remain as development history, but InstallForge is no longer supported.

## v0.8.4 Beta

InstallForge CLI 탐색 개선(일반 경로/레지스트리/PATH, 구버전 GUI 안내), 패키징 생략용 build_no_setup.bat 추가, 제품 버전 동기화.

## v0.8.3 Beta — InstallForge XML build fix

- Fixed `Build-InstallForge.ps1` assigning installer paths to XML elements: `SetupFilePath`, `SetupIconPath`, and `UninstallerIconPath` now use `XmlNode.InnerText` instead of PowerShell XML adapter assignment that caused `XmlNodeSetShouldBeAString`.
- Validate required `BuildConfiguration` elements explicitly before writing the generated InstallForge project.
- Updated .NET assembly version and installer template versions to v0.8.3 Beta. The generated installer filename continues to be read from the .csproj version.
- Audio DSP and the Inno Setup uninstall flow are unchanged.
- Validation performed: XML parsing, script content checks, and ZIP integrity. Real InstallForge compilation requires a Windows machine with InstallForge installed.

## v0.8.2 Beta — optional InstallForge packaging

- Added XML InstallForge project template, PowerShell project generator and independent CLI build script.
- `SETUP_TARGET=inno|installforge|both|none` selects packaging; default stays Inno.
- Generated .ifp enumerates the publish root, making source path relocatable; output is a separate InstallForge EXE.
- InstallForge uninstall limitations explicitly documented; no equivalent pre-uninstall APO cleanup claim.
- No DSP changes.

## v0.8.1 Beta — packaging and version alignment

- Updated product, assembly, installer version metadata to 0.8.1 Beta.
- Inno Setup compiler detection includes 6 and 7 in per-user and machine locations.
- `build_setup.bat` derives its package name from the project version, avoiding stale hardcoded filenames.
- No audio engine or user-profile changes.

# 개발자용 릴리즈 노트

## GitHub 소스·제3자 라이선스 정비

- Windows .NET 배포에 적용되지 않는 Python/PySide6/PyInstaller 관련 릴리스 고지를 현행 패키지에서 제거했습니다. 과거 Python 릴리스의 라이선스 의무는 그대로 유지됩니다.
- C# 자체 소스는 MIT, 외부 XT는 별도 설치, AutoEq는 사용자 요청 시 다운로드, KEMAR 데이터는 저자 표시로 분리해 표기했습니다.
- `vst3/src/` C++ 플러그인 소스와 CMake 빌드 안내, Steinberg/AutoEq MIT 원문, HRIR 출처·해시 정보를 포함했습니다.
- GitHub용 `.gitignore`를 보강해 VST3 바이너리, .NET 빌드 결과, 키와 사용자 설정이 커밋되지 않도록 했습니다.
- 출력 폴더에 라이선스 문서를 자동 복사하고 빌드 전후에 필수 고지 존재 여부를 검사합니다.
- 구 Python 참조 폴더(런타임에 필요하지 않음)와 현행 배포에 적용되지 않는 LGPL 교체 안내·제3자 소스 제공 약속 문서를 제거했습니다.
- **미완료:** 로컬 동봉 VST3 바이너리와 C++ 소스의 일치 여부, SDK 정확한 버전/서브모듈 구성, 실기기 빌드·실행 검증. 완료로 간주하지 않습니다.
# 개발자용 릴리즈 노트

## 설정 내보내기 선택 UI 개선

- `src/SettingsExportDialog.cs` 신규: 별도 모달 창에서 `타인 공유용`과 `내 컴퓨터용`을 명확한 버튼으로 제공합니다. 각각의 저장 범위 설명은 다크 테마 카드형 레이아웃에 표시하며, 충분한 버튼·텍스트 공간을 확보합니다.
- `src/MainForm.cs`: 기존 Yes/No/Cancel 확인창을 위 대화상자로 대체했습니다. `DialogResult.Yes`는 공유용, `DialogResult.No`는 전체 백업으로 매핑하여 기존 JSON 내보내기 동작과 파일명은 유지합니다.
- 파일 저장을 취소하면 내보내기를 수행하지 않으며, 공유용에서는 출력 장치 식별자·개인 환경 경로를 내보내지 않는 기존 `EffectTransfer` 처리 방식을 유지합니다.
- .NET SDK/Windows 환경이 없어 컴파일 및 화면 렌더링 검증은 수행하지 못했습니다.

## 출력 장치 / AutoEQ / UI 안정화

### 수정 사항

- `CompressorDetailsDialog.cs`: 5밴드 상세 창의 위치 고정 슬라이더·숫자 입력을 3열 `TableLayoutPanel`로 재구성했습니다. 창 크기 변경 시 숫자 입력란이 화면 밖으로 나가지 않도록 하며, 탭 헤더도 다크 테마로 그립니다.
- `CompressorThemes.cs`: 원본 Python `compressor_themes.py`의 8개 사용자용 한글 테마명과 `사용자 설정`을 추가하고, 기존 영문 ID는 JSON 및 VST3 파라미터 계산에 그대로 유지합니다.
- `MainForm.cs`: 장치 선택/AutoEQ 가져오기 이후 동일한 `UpdatePreampSummary`로 수동·원본·자동 감쇄와 최종값을 표시합니다. 수동 -5dB 화면에서 원본 감쇄 -6.2dB 문구가 남는 문제를 제거합니다.
- `MainForm.cs`: 사용자용 프리앰프 클리핑 안내 문구를 추가하고 `이 장치에 음향효과 적용`으로 이름을 수정했습니다. 해당 스위치는 `ApoRenderer.Render`에서 장치별 EQ·프리앰프·컴프레서·공간 효과 전체를 우회시키는 기존 동작과 일치합니다.
- `DeviceSelectionPolicy.cs`(신규), `MainForm.cs`: Windows 기본 출력 장치 ID 및 활성 장치 목록을 약 1.6초 간격으로 점검합니다. 기본 장치가 변경되면 그 장치의 독립 프로필을 읽고, 오디오 장치가 재연결되면 저장한 EQ 상태를 APO에 재적용하도록 요청합니다. 수동 선택은 기본 장치 변경 전까지 유지합니다.
- `MainForm.cs`: 현재 장치의 자동 샘플레이트 감지 경로를 장치 전환/주기 감지에 유지하며, 변경된 공유 모드 샘플레이트를 저장한 뒤 DSP 설정 재작성을 예약합니다.
- `tests/Program.cs`: 기본 장치 전환, 장치별 AutoEQ 독립성, JSON round-trip, 한글 테마 이름↔내부 ID 대응 검증 코드를 추가했습니다.

### 검증 제한

현재 작업 환경에 .NET 10 SDK/Windows 오디오 장치가 없어 `dotnet run --project tests/RendererSmokeTests.csproj -c Release`를 직접 수행할 수 없었습니다. C# 빌드와 실제 오디오 출력 검증이 필요하며, 정적 소스 검사는 빌드를 대체하지 않습니다.

## UI·DSP 적용 동기화 보완

- MainForm 기본 클라이언트 높이 상향(기존 설계 캔버스가 충분한 디스플레이에서 스크롤되지 않게 조정).
- CompressorDetailsDialog: 160ms debounce된 실시간 값 변경 콜백, MainForm APO 반영 타이머 200ms, 원본 커스텀 밴드와 테마/강도 스냅샷 복원. 모달 대화상자 중 출력 장치 자동 전환 보류.
- Preview 오류는 상세 창 안내 영역에 표시하며 사용자 설정을 잃지 않도록 함.
- WinForms TabControl 빈 탭 스트립의 밝은 배경을 오버레이로 덮어 다크 테마 유지.
- Studio Ambience Diagnostic: JsonIgnore 임시 플래그, Python 진단과 동일한 고정 지연 탭, 전용 Convolution만 생성. 테스트 종료·장치 전환·전체 효과 OFF 시 플래그 해제.
- 음향 효과 범주별 적용 중/설정 기록 완료/실패 상태 분리; 효과 이름을 상태 안내에 반영. 실제 드라이버의 청감 처리 여부는 별도 장치 검증 필요.
- 잔향 진단 생성/해제/비영속성 회귀 검사 추가.
- 적용 완료 메시지는 사용자 설정의 ON/OFF 및 저지연 우회 여부를 확인한 뒤, 해당 효과가 현재 비활성화된 경우 이를 명확히 표시하도록 수정.
- 진단 모드에서는 컴프레서가 우회되므로 불필요한 VST 파일 검사를 건너뛰도록 조정.

## 컴프레서 상세 조절 / 장치 체크박스 UI
- WinForms 기본 TabControl을 다크 테마 전용 5개 탭 버튼·패널로 대체하여 밝은 시스템 테두리를 제거했습니다.
- Sub 30~500Hz, Bass 60~2500Hz, Mid 300~8000Hz, High 1000~16000Hz, Air 20000Hz 고정 범위를 기준으로 인접 대역 경계를 자동 반영합니다. 실제 편집 가능 범위는 양옆 대역에 따라 더 좁아집니다.
- 기존 프리셋이나 가져온 JSON의 대역 끝 주파수가 역전되어 있으면 대화상자에서 자동 정규화합니다. 컴프레서 음향 동작은 기존 로직을 유지합니다.
- 장치 음향 효과 체크박스의 문구를 스타일 계산 전에 지정해, 과거 문구 폭이 남아 글자가 잘리는 문제를 해결했습니다.
- 주파수 순서·이웃 경계·20kHz 고정값 회귀 테스트를 추가했습니다.

## Inno Setup 설치형 및 제거 정책 추가

- Windows x64 자체 포함(.NET 10) publish와 Inno Setup 6.x `ISCC.exe` 연속 빌드
- 설치 아이콘/라이선스/고지 포함, 설치 파일 `release/installer` 출력
- 제거 시 우리 APO Include만 탐지·백업·중립화(외부 XT 등록 유지)
- 임시 XT 다운로드/오류 로그만 선택 삭제, 사용자 프리셋 보존
- `.gitignore` 설치 생성물 제외, `INSTALLER_BUILD_AND_UNINSTALL.md` 추가
- Inno Setup 실제 컴파일 및 Windows 설치 제거 시험은 미수행


## 공유 설정 v2 — AutoEQ 분리 및 공간 음향 보존

- 타인 공유용 파일 포맷을 `InfinityAudioEqualizerPro-Effects-v2`로 변경했습니다.
- 공유 파일에서 장치별 AutoEQ 보정 데이터(`auto_filters`, `graphic`, `auto_preamp`, `auto_enabled`, `source`)를 제거합니다.
- HRTF 선택, 전후 공간감, 균형형/안정성 설정, Studio Ambience 자동 샘플레이트 선택을 누락 없이 포함합니다.
- 공유 파일을 적용할 때 기존 장치의 이름과 AutoEQ를 보존하고, 나머지 음향 효과를 반영합니다.
- 이전 v1 공유 파일도 읽을 수 있지만, 이미 빠진 HRTF 설정을 복구할 수는 없습니다.
- 공유 대상에 AutoEQ 원본이 없는 경우에는 AutoEQ 기반 프리앰프 모드를 전달하지 않고 수동 모드로 처리합니다.

## 신규 프로필 초기값 변경

`DeviceProfile` 속성 이니셜라이저에서 `AmbienceDecay=1000`, `SurroundMode="cinema"`, `SurroundBinaural=true`, `Surround3Enabled=true`, `CompressorTheme="OPEN"`, `CompressorStrength=12`로 변경. `SurroundEnabled=false`, `CompressorEnabled=false`는 그대로 유지. 기존 JSON은 제공된 필드 값 우선이므로 변경하지 않음. 회귀 테스트에 직렬화 왕복 및 기본 OFF 검증 추가.

## 신규 HRTF 기본값에 맞춘 회귀 테스트 수정

- 기존 `Surround/Studio convolution entries missing` 테스트가 전역 기본값을 이전 기본 FIR 경로라고 가정하던 부분을 수정했습니다. 기본 FIR 검사는 `SurroundBinaural=false`, `Surround3Enabled=false`를 명시합니다.
- 컴프레서/공간 효과 동시 활성화 테스트 및 기본 공간 확장 테스트도 같은 이유로 처리 모드를 명시합니다.
- 새로운 장치의 Cinema/HRTF/전후 분리 ON, Studio Ambience 1000ms 및 컴프레서 자연스러운 개방감/강도 12를 따로 확인하고, HRTF + Stage3의 Convolution 4개 명령 생성 여부를 검사합니다.
- 오디오 DSP 구현과 프로필 기본값은 변경하지 않았습니다.


### Packaging addition (MSI build integration)
- Added optional WiX Toolset SDK MSI project, MSI build scripts and Windows build selector, retaining Inno Setup and MSIX workflows.
- Added uninstall-time managed APO detach action and MSI-specific installer upgrade identity; documented separate-directory/coexistence safeguards.
- MSI compilation, installation, repair and APO/VST3 playback require Windows validation; no built installer is bundled in source.


### MSI wizard UI (packaging-only, v0.8.6 Beta maintained)
- Added WixToolset.UI.wixext 6.0.2 and WixUI_InstallDir to expose welcome, MIT license acceptance, installation directory selection, confirmation, progress and finish dialogs.
- Added installer/LICENSE_MSI.rtf generated from the existing LICENSE. No app source changes and no version bump.
- Real Windows MSI build and UI flows still require local acceptance testing.


### MSI-only packaging follow-up (application version remains v0.8.6 Beta)
- Branded MSI wizard with blue header and welcome/completion art.
- Changed MSI wizard to WixUI_FeatureTree so users can choose the installation folder and turn Start Menu and Desktop shortcuts on/off independently.
- Added optional post-install launch checkbox on the completion dialog.
- No application source, DSP code, or app version changed. Windows MSI build and runtime behavior require verification.

## MSI packaging follow-up (no app version change)
- Replaced the WiX dialog bitmap with a fuller Infinity Audio Equalizer PRO WINDOWS branding treatment.
- Replaced the WiX top banner with a clean blue-accent banner to improve Custom Setup text readability.
- Changed the MSI default installation folder name from `Infinity Audio Equalizer Pro MSI` to `Infinity Audio Equalizer Pro`.

## MSI packaging visual tidy-up (no app version change)
- Removed the two arc lines from the left-side MSI welcome branding artwork.
- Added `pause` at the end of `build_msi.bat` for easier build result inspection.

## v0.8.7 Beta — APO output registration recovery guidance
- File-link success and actual Windows audio endpoint APO activation are no longer presented as equivalent.
- Added guided recovery and direct official device selector launch after a manual bridge connection/reconfiguration.
- Bridge status and tooltips now distinguish connected configuration files from unverified Windows endpoint processing.
- No device registry write is attempted by Infinity Audio Equalizer Pro itself; device registration remains owned by the EqualizerAPO-XT selector.

## 0.8.8 Beta — tray exit cannot be blocked by APO errors
- Removed failure branch that reverted `_reallyExit` and left the tray app running if `ApoBridge.Apply` raised during shutdown.
- Best-effort bypass remains, while failures go to DiagnosticLog (no modal popup).
- Stop both apply/device timers and never invoke ApplyLive from FormClosing to prevent applying saved pending effects after shutdown bypass.
- Restore and persist the user's actual audio preferences; hide tray icon and close unconditionally.
- Existing uninstall-detach behavior remains unchanged.


## 0.8.9 Beta — APO activation and XT setup flow
- The global ON transition checks known XT installation, config path and Infinity Audio Equalizer Pro bridge disconnection; no dialog on OFF.
- XT install no longer automatically opens a second Device Selector over XT UI.
- Selector completion performs only a silent config synchronization; never claims DSP audibility.
- Avoids duplicate instructional message boxes during Device Selector launch.
- Endpoint registration remains unverified: no registry-based success inference.

## v0.8.13 Beta — EqualizerAPO-XT 사전 고지
- `InstallXt()`를 설치 전 고지 창으로 교체했습니다. XT는 별도 앱이며 Infinity Audio Equalizer Pro 제거 후에도 남는다는 내용을 굵게 안내합니다.
- 진행 동의 전에는 XT 설치 파일의 다운로드와 실행을 시작하지 않습니다. 취소 시 아무 작업도 하지 않습니다.
- 기존 설정/오디오 DSP/Windows 장치 등록 및 MSI 배포 로직은 유지합니다.

## APO configuration path picker (v0.8.14 Beta)
- The APO configuration folder picker now opens at the detected current EqualizerAPO-XT configuration directory, prioritizing the actual registered configuration root over stale saved paths.
- If automatic detection is unavailable, an existing saved path containing `config.txt` is used as the initial selection. If no valid path exists, the original folder picker opens normally.
- Opening or cancelling the picker does not change saved settings. Only an explicit selection is saved.
- No DSP or installer behavior changes.


## v0.8.15 Beta — in-place guided overlay
- `src/UserGuideOverlay.cs`: read-only bitmap-backed overlay, target highlighting and next/previous navigation.
- `src/MainForm.cs`: upper-right `사용자 가이드` action adjacent to About; existing scroll panel/canvas dimensions unchanged.
- Overlay is added directly to the Form rather than to the scrollable content.
- Low-latency help names its bypassed effects explicitly.
- Only MSI remains the preferred installer for distribution; Inno setup source kept for archival compatibility.
- Windows WinForms GUI build and runtime visuals have not been verified in this environment.

## v0.8.16 Beta
- 상단 사용자 가이드 버튼과 프로그램 제목의 표시 영역 중첩 제거.
- 가이드 항목별 ⓘ 도움말 표시, 최초 5회 실행에서 항목 직접 호버 지원.
- 가이드 오버레이 표시 중 툴팁 중복 표시 방지 및 CaptureViewport 메서드명 경고 제거.

## v0.8.17 Beta
- ⓘ 도움말 아이콘을 7개 대표 체크박스로 한정.
- 체크박스 전체 폭 대신 실제 텍스트 렌더링 폭을 기준으로 아이콘 좌표 계산.
- 기존 오버레이, 최초 5회 툴팁 동작, 고정 화면 크기 및 스크롤 유지.

### v0.8.24 Beta — XT 설치 존재 여부 검증 및 장치 등록 안내
- 사용자가 XT를 제거한 뒤 남은 current 디렉터리와 Editor.exe/DeviceSelector.exe가 설치된 것으로 오인되지 않도록 Windows 설치된 앱 등록을 함께 확인합니다.
- 현재 실행에서 사용자가 직접 선택한 설치 폴더는 연결 도구 실행을 위한 명시적 예외로 허용하되, 다음 실행에 자동으로 설치된 것으로 처리하지 않습니다.
- 설치 누락은 오래된 UI 상태 문자열과 상충하더라도 확정적으로 설치 안내를 표시하도록 합니다.
- 출력 장치 등록에서 설치 폴더를 찾지 못하면 설치 / 설치 폴더 선택 / 취소 선택지를 제공합니다.

- Corrected EqualizerAPO-XT post-install handling: do not open a folder picker or confirmation dialog immediately after the installer exits; briefly retry installation detection while XT finishes its own setup and device-selection UI. Existing manual device registration and installation-folder selection remain available.
