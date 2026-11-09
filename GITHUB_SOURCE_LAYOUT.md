# GitHub 소스 구조

- `src/`: Windows C# GUI, DSP 설정 생성, XT 설치·연동 코드
- `vst3/src/` 및 `vst3/CMakeLists.txt`: InfinityCompressor 자체 C++ 소스
- `tests/`: 설정 생성과 회귀 테스트
- `assets/`: 제품 이미지/아이콘
- `legal/`, `THIRD_PARTY_LICENSES/`: 현재 Windows C# 배포용 법적 문서 및 타사 라이선스
- `HRTF_DATA_CREDITS.txt`, `release-assets/hrtf_provenance.json`: MIT KEMAR 측정 데이터 출처
- `InfinityCompressor.vst3/`: 현재 로컬 FULL 패키지에만 있는 사전 빌드 플러그인. Git에는 올리지 않음
- `build_windows.bat`: 개발용 일괄 테스트·컴파일 (공인 SDK/컴파일러 확보 필요)

과거 Python 버전은 기존 Git 커밋/태그 또는 별도 보존 자료로 관리합니다. 현행 C# 소스 ZIP에 구 Python 전체 소스와 Qt 라이선스 약속을 중복 포함하지 않습니다.
