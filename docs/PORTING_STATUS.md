# Porting and verification matrix

| Domain | Source implementation | Validation status |
|---|---|---|
| Native Windows output device listing | MMDevice COM API | Requires Windows runtime test |
| XT installer discovery/download | Official GitHub release asset + SHA256SUMS hash | Requires Windows/network test |
| XT device registration | Elevation of official DeviceSelector | Requires user confirmation; no automatic registration status |
| Per-user APO bridge | Elevated include, app-only folder ACL, backup | Code added; requires Windows integration test |
| Existing global APO settings | Session-only pause and text restoration; unknown commands abort | Deterministic smoke tests |
| EQ audio renderer | 31-band GraphicEQ, AutoEQ PEQ/GraphicEQ, manual/source preamp | Renderer smoke tests; hearing/frequency measurement needed |
| C# bypass | Emits no C# DSP commands | Renderer smoke tests |
| Live apply | Debounced atomic managed.txt write | Requires device test |
| VST3 compressor themes, strength and APO directive | Ported via C++ plugin and C# normalized parameter model | Configuration smoke tests only; real plugin loading unverified |
| Binaural surround and ambience | Not ported | Not claimed to work |
| Online AutoEQ, startup/tray, one-click app installer | Not ported | Not claimed to work |

Python baseline remains untouched. No audio or antivirus test result is claimed.
