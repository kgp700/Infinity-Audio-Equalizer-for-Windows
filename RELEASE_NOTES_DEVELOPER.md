## v0.8.45 Beta — 복원 대상 자동 선택 및 불필요한 보관 옵션 제거

- 전체 설정 복원 장치 연결 창에서 `현재 장치에 적용하지 않음 (기존 설정은 보관)` 항목을 제거했습니다. 사용자가 `취소`를 누르면 백업 가져오기 자체가 중단됩니다.
- Windows 기본 멀티미디어 재생 장치가 복원 대상에 있다면 해당 장치를 기본 선택합니다. 동일한 이름의 이전 백업 프로필을 우선하여 연결하고, 다수의 백업 프로필이 있으면 다른 장치는 명확히 이름이 일치하는 경우에만 자동 선택합니다.
- 기본 재생 장치가 이미 같은 ID로 백업에 포함되어 별도 연결 대상이 아닐 경우, 다른 연결 가능한 출력 장치를 제안하고 이를 설명합니다.
- 선택하지 않은 추가 백업 프로필은 원래 ID로 보존됩니다. 하나의 현재 장치에 복수 백업 프로필을 연결하는 것은 계속 차단합니다.
- `FullSettingsRestore.SuggestMappingsWithDefault`를 추가하고 기본 출력 장치 우선 지정, 이름별 매핑, 대상 없음 회귀 테스트를 추가했습니다. APO 등록·음향효과·백업 파일의 보존 정책은 변경하지 않았습니다.
- Windows .NET 실제 빌드와 DPI별 복원 창 UI 테스트는 별도 확인이 필요합니다.

## v0.8.44 Beta — 전체 설정 복원 장치 연결 창 사용성 개선

- `FullSettingsRestoreDialog`의 하단 안내문과 버튼을 별도 2행 레이아웃으로 배치하고 충분한 높이와 최소 창 크기를 확보하여 버튼이 잘리는 문제를 수정했습니다.
- 장치 연결 안 함 선택지를 `현재 장치에 적용하지 않음 (기존 설정은 보관)`으로 변경했습니다. 백업 프로필을 현재 Windows 장치 ID에 연결하지 않으면 원래 ID에 보존되며 현재 장치에는 적용되지 않는다는 점을 상단/하단에 설명합니다.
- 실제 장치 매핑을 하나 이상 지정한 경우 `선택한 장치로 복원`, 지정하지 않은 경우 `설정 복원 계속`을 표시합니다. 기존 복원 데이터 구조, 매핑 처리 및 APO 등록/연결 로직은 변경하지 않았습니다.
- Windows .NET 10 WinForms 빌드·DPI별 레이아웃 시각 검증은 별도 확인이 필요합니다.

## v0.8.43 Beta — Surround Stage 기본 처리 방식 HRTF 공간감

- 신규 출력 장치와 전체/선택 장치 초기화의 Surround Stage 처리 방식을 `HRTF 공간감`으로 변경했다. 기존 `Cinema` 모드와 모든 수치, 효과 사용 OFF 상태는 유지한다.
- 기존 사용자 저장 프로필은 자동으로 변경하지 않는다. 기본값 회귀 테스트를 변경했다.
- Windows .NET 빌드 및 실제 음향 테스트는 별도 확인이 필요하다.

## v0.8.42 Beta — 신규 장치·초기화 기본 설정 통일

- 사용자 지정 스크린샷(빨간 박스)에 맞춰 신규 출력 장치 프로필의 체크 상태와 수치를 고정했다. 장치 음향효과/AutoEQ/추가 EQ는 체크, 프리앰프 수동 −5dB, 정적 EQ 모드 체크, 저지연 체크 해제, 31밴드 모두 0dB.
- Stereo Width는 사용 OFF/강도 16%, Surround Stage는 사용 OFF/기본 공간 확장/Cinema/45·80·45, 전후 공간감 체크 및 하위 수치 80·75·75·80, 균형형 65·80·55를 보존했다.
- Studio Ambience 사용 OFF/강도 3/잔향 1000ms/샘플레이트 자동 감지, 컴프레서 사용 OFF/자연스러운 개방감/강도 12를 기본값으로 지정했다.
- 신설 장치 생성, 선택 장치 효과 초기화, 전체 장치 효과 초기화가 같은 DeviceProfileDefaults 팩토리를 사용한다. 이름/Windows 장치 ID는 유지, 전체 초기화는 글로벌 효과 OFF로 되돌린다.
- 신규 설치에서 Windows 로그인 시 자동 실행을 켜고, 전체 초기화 시에도 켜도록 했다. 기존 설정을 가진 사용자의 개별 자동 실행 선택은 재실행 시 변경하지 않는다.
- 기존 저장된 장치 프로필을 업데이트만으로 덮어쓰지 않는다. 리그레션 테스트에서 지정한 초기 수치를 검사한다.
- 앱이 OFF일 때의 APO 경고 표시 정책, XT 다운로드, 복구 백업 로직은 변경하지 않았다.

## v0.8.41 Beta — XT 설치 다운로드 신뢰성 및 진행 표시, Surround 기본 모드
- GitHub 릴리스 API 자산의 `digest`(sha256:)로 설치 파일을 검증합니다. `SHA256SUMS.txt`를 요청하지 않습니다. 유효한 digest 누락·파일 불일치 시 자동 실행을 차단합니다.
- 최초 요청에는 앱 User-Agent, 일시 네트워크 오류의 재시도에는 Edge 호환 User-Agent를 사용합니다. 최대 3회 재시도와 HTTPS/공식 릴리스 URL 검사는 유지합니다.
- XT 설치 진행 중 진행 창을 표시하고 메타데이터 조회, 다운로드 바이트/전체 크기, 재시도 및 검증 단계를 알립니다. 외부 설치 프로그램 실행 전 진행 창을 닫습니다.
- 선택 장치 효과 초기화와 모든 장치 효과 초기화 후 Surround Stage 모드를 `Cinema`로 설정합니다. 다른 초기화 값은 유지합니다.
- Windows .NET 10 빌드 및 실기기 설치 시험은 별도 확인이 필요합니다.

## v0.8.40 Beta — EqualizerAPO-XT 설치 다운로드 일시 오류 재시도
- 공식 GitHub 최신 릴리스 정보, SHA256SUMS.txt 및 설치 파일 다운로드에서 429/408/500/502/503/504 또는 요청 타임아웃 시 최대 3회 재시도.
- 체크섬 검증, HTTPS 공식 릴리스 URL 검증, 설치 파일 제한 및 실행 흐름은 유지. 일시 오류가 지속되면 기존 공식 배포 페이지 안내로 이동 가능.
- 장치별 복구/백업, APO 등록 판정, UI 및 프리셋 구조 변경 없음.
- Windows 실제 다운로드 및 설치 시험은 별도 확인 필요.

## v0.8.39 Beta — 새 장치 인식 중복 백업 및 복구 목록 정리
- `RecoveryBackupStore.SaveBeforeNewEndpointIfChanged`: 블루투스 재페어링 중 새 Windows 출력 장치 ID가 여러 번 발견되어도, 기존에 조정한 음향 프로필 집합이 동일하면 같은 보호 백업을 반복 저장하지 않도록 개선.
- 새 장치에 자동 생성된 변경 없는 기본 프로필과 복구 안내 표시 기록은 새 장치 보호 백업의 중복 판정에서 제외. 기존 EQ/AutoEQ/컴프레서/공간 효과를 실제로 변경한 경우에는 추가 백업을 허용.
- `RecoveryBackupStore.FindRecoveryCandidates`: 이전 버전이 만든 안전 백업 파일에 포함된 변경 없는 새 장치 프로필은 복구 목록에서도 제외. EQ가 0 dB라도 다른 효과가 있으면 보존.
- 기존 영구 복구 백업은 수정하거나 삭제하지 않음. 정기 백업 없음, 수동 복구 및 APO 등록 판정 변경 없음.
- `tests/Program.cs`: 연속된 새 Bluetooth 출력 장치 ID 두 건에 대해 백업 1개만 생성되는지, 기존 EQ/AutoEQ 값 보존, 실제 설정 변경 시 두 번째 백업 생성, 기존 다중 항목 백업에서 기본 프로필 필터링을 검증하는 회귀 테스트 추가.
- 문서 구성과 `.gitignore`는 사용자 제공 Git 프로젝트를 기준으로 유지; 추가 안내 문서의 불필요한 동봉을 제거. 기존 문서의 깨진 참조를 보정.
- Windows .NET 10 빌드 및 실제 Bluetooth 장치 테스트는 별도 검증 필요.

## v0.8.38 Beta — 하단 설정 버튼 균등 정렬 / 복구 후보 필터 개선
- `MainForm.cs`: 하단 버튼의 4등분 폭과 순서를 일치시켰습니다.
- `RecoveryBackupStore.cs`: 현재 대상 장치의 라이브 프로필 제외, 변경 없는 기본 프로필 제외, 백업과 이전 장치 설정 우선 정렬을 공통 후보 선택 함수로 구현했습니다. 모든 효과의 저장값을 비교하며 31밴드가 평탄하다는 이유만으로 AutoEQ·공간 음향 설정을 제외하지 않습니다.
- `RecoverySettingsDialog.cs`: 수동/자동 복구 창에서 같은 후보 선택을 사용하고 빈 목록에서는 복원 버튼을 비활성화합니다.
- `tests/Program.cs`: 블루투스 새 ID 시 기존 프로필 보존과 빈 기본 프로필 필터링 회귀 검증을 추가했습니다.
- 기존 프리셋 구조, EqualizerAPO-XT 등록 판정, 자동 복구 알림, 영구 백업 정책은 변경하지 않았습니다.

## v0.8.37 Beta — 복구 목록 가시성 및 저장 시각 개선
- 기존 장치 ID와 일치하는 현재 프로필도 목록에 표시하되, 과거 복구 백업과 구분하고 자기 자신에게 재적용하는 동작은 막음.
- 기존 복구 백업은 동일 ID를 우선 표시하고, 장치 이름의 공백 표기가 달라도 복구 후보로 안내. 이름만으로 자동 복원하지 않음.
- 각 복구 항목에 원본 장치, 백업 생성 일시, 백업 사유를 표시하고, 현재 프로필은 설정 파일 최종 수정 시각으로 구분 표시(장치별 저장 시각은 아님).
- 백업이 존재하지 않는 경우 안내를 개선하고, 장치 이름 비교 회귀 테스트 추가.
- 정기 백업/백업 자동 삭제를 도입하지 않음. Windows 런타임 검증은 필요.

## v0.8.36 Beta — Windows 빌드 및 스모크 테스트 컴파일 수정
- tests/Program.cs: 저장 경로 변수 이름 충돌(CS0136)을 해결하고 기존 복구 테스트 유지.
- src/MainForm.cs: 초기 화면 준비 시 nullable 패널 호출 경고(CS8602) 2건 제거.
- src/ApoEndpointRegistration.cs: 사용하지 않는 변수 경고(CS0219) 제거. 판정 동작 변경 없음.
- 복구·프리셋·APO 동작은 v0.8.35 Beta와 동일. Windows .NET 실제 빌드는 미검증.


## v0.8.35 Beta
- Added atomic, non-expiring complete settings recovery snapshots on destructive actions (including AutoEQ replacement/clear and legacy import) and newly detected endpoint IDs.
- Added shared per-device recovery dialog, archived profile provenance, manual restore entrypoint, and one-time endpoint prompt tracking.
- Preserved original per-ID settings architecture. No automatic APO registration or plugin changes.
- Validation status: source-level checks only; Windows .NET build and real endpoint lifecycle tests pending.

## v0.8.34 Beta — 복원 완료 안내 및 APO 버튼 툴팁

- `MainForm.ImportSettings`에서 사용자 승인 매핑이 하나 이상 실제 적용된 전체 백업에 한해서 복원 완료 후 등록/재연결 가능성 안내를 표시. 미매핑 전체 백업 및 타인 공유용 경로에는 신규 팝업 없음.
- 메인 APO 장치 선택기 및 연결/다시 적용 두 버튼에 기존 `ToolTip` 인스턴스로 역할 설명과 드라이버/블루투스 기기 재등록 안내 추가. 별도 UI 요소 미추가.
- 장치 등록·컴프레서·APO 설정 처리·프로필 저장 방식은 변경하지 않음.
- Windows 실제 빌드·UI 테스트는 미실행.

## v0.8.33 Beta — 드라이버 재설치 후 전체 백업 장치 매핑 복원

- `FullSettingsRestore`: 전체 설정 백업의 프로필 ID와 현재 Render 엔드포인트 ID를 비교, 새로운 ID의 장치만 후보로 제안하고 복원 전 전부 검증.
- `FullSettingsRestoreDialog`: 구형 엔드포인트의 프로필을 새 장치로 명시적 매핑; 이름이 동일한 유일 후보는 제안만 하고 확정을 요구.
- 직렬화 기반 딥 카피로 수동 31밴드 EQ, AutoEQ 데이터, 컴프레서 커스텀 밴드, 공간 효과와 확장 필드를 보존하며 이전 ID 프로필은 삭제하지 않음.
- 장치 재등록, XT 설치/제거, 레지스트리 변경은 수행하지 않음. 연결 불가 시 기존 상태 진단/경고 사용.
- 스모크 테스트: 드라이버 교체 ID 변경, 31밴드 EQ 값 복원, AutoEQ/컴프레서 보존, 원본 유지, 잘못된 매핑 거부.

## v0.8.32 Beta — 첫 실행 창 일괄 표시

- `MainForm` 최초 실행에서 WinForms 컨트롤과 장치/APO 상태가 초기화되는 동안 메인 창을 투명하게 유지하고, 초기 레이아웃/그리기를 완료한 뒤 불투명도를 한 번에 100%로 설정합니다. 점진적으로 나타나는 애니메이션은 사용하지 않습니다.
- 첫 실행 안내는 창 표시 완료 후 실행합니다. 시작 중 빈 창틀이나 컨트롤의 순차적인 그리기가 보이지 않도록 시작 순서를 변경했습니다.
- Windows 로그인 시 트레이로만 시작하는 기존 동작과 수동 실행/트레이 복원, APO 효과/장치 관련 로직, 사용자 설정은 그대로 유지합니다.
- 실제 Windows WinForms 실행/화면 표시 검증은 이 Linux 환경에서 수행하지 못했습니다.

## v0.8.31 Beta — 활성화 실패 경고 가독성과 세션 미연결 표시 보완

- `MainForm`의 단일 경고 라벨을 고정 높이 Panel + 제목/조치 라벨로 전환, 대비 높은 붉은 배경과 17pt 제목/10.5pt 조치 안내를 분리하여 표시.
- `RefreshActivationWarning()`의 기존 4가지 구체적 원인 판단을 보존하면서, 검사상 문제 없음이더라도 현재 앱의 `_connected`가 false이면 연결 필요 안내를 추가해 사용자에게 혼란을 주던 상태 표시 불일치 경로를 보완.
- 미연결 상태의 사용자 변경 및 APO 설정 재적용 성공/실패 이후 경고 상태를 재평가.
- 경고 표시 시에만 메인 섹션을 82px 아래로 이동하고 해제하면 원복; 전체 효과 ON/OFF 체크 및 저장 설정, 오디오 렌더링, APO 등록 동작은 변경 없음.
- Windows .NET 10 SDK가 현 환경에 없어 실제 EXE/MSI 빌드 및 청음 통합 테스트는 수행하지 못함.

## v0.8.30 Beta — 음향효과 경고에 정확한 조치 버튼명 표시

- `MainForm.RefreshActivationWarning()`의 XT 미설치·출력 장치 미등록·APO 설정 경로 누락·브리지 미연결 경고에 실제 UI 버튼명을 포함한 조치 문구를 추가했습니다.
- 경고 디자인 및 표시 조건, 전체 효과 사용자 설정값 보존, 복구/적용 동작, 섹션 브랜딩은 v0.8.29 상태로 유지합니다.
- Windows 빌드 및 실제 장치 등록 통합 테스트는 별도 진행이 필요합니다.

## v0.8.29 Beta — 음향효과 적용 불가 경고 UI 및 효과 브랜딩 수정

- `MainForm.cs`: 사용자 전체 효과 ON 상태를 보존하면서 `IndependentlyDiagnoseActivation()` 결과에서 XT 미설치, 출력 장치 미등록, 설정 경로 없음, 브리지 미연결을 구분하여 별도 큰 경고 배너에 표시.
- 평상시 UI 간격 유지; 경고 발생 시 최상단 섹션을 아래로 배치하고 해제 시 원복. 일반 상태줄 메시지가 경고를 덮어쓰지 않도록 별도 Label로 구현.
- 전체 효과 버튼에서 오류 상황의 ON 표시를 억제하고 `⚠ 적용 불가`로 변경. 설정 체크 여부와 저장 상태는 변경하지 않음.
- 1.6초 장치 감시 및 기존 상태 갱신 루틴에서 경고 상태 재검사. 실제 DSP 청음 확인을 주장하지 않으며 Unknown 상태는 단정하지 않음.
- 두 효과 섹션의 제목만 브랜드명으로 변경. 체크박스 및 내부 DSP 키는 변경 없음.

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
