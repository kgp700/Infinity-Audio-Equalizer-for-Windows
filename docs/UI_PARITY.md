# C# UI equivalence against Python 0.7.20 Beta

The C# interface follows the order and Korean labels of `reference/main.py`:
- Product title / description / device / global power
- Device preset / AutoEQ online search and import / preamp / low latency
- Stereo Width
- Infinity Audio Equalizer Pro Surround Stage (basic processing mode)
- Front/rear direction panel (visible but disabled)
- Infinity Audio Equalizer Pro Studio Ambience
- Five-band compressor themes / strength / custom editable bands / theme export and import
- 31-band graphic EQ (-12 to +12 dB)
- APO installation, registration, connection, config, Windows startup
- Reset, settings export/import, tray window and exit controls

**Not pixel-identical:** The original uses Qt/PySide6; this port uses WinForms. Native text rendering, margins, sliders and responsive wrapping differ. No side-by-side Windows screenshot comparison has been performed.

**Implemented with actual APO commands:** basic Surround Stage matrix + reproducible stereo IR; Studio Ambience stereo IR; stereo width; 31-band EQ; AutoEQ; VST3 theme parameters.

**Not implemented:** Stable/Balanced/HRTF Surround processing, front/rear HRTF Stage3, comparison A/B buttons, PCM response measurement, precise automatic sample-rate detection and Python-grade endpoint notification registration. The corresponding unimplemented UI controls are disabled, not connected to fake actions.

**Important:** Surround Stage and Studio Ambience are algorithm ports, not bit-identical PCM to Python: .NET `Random` differs from Python `random.Random` in the ambience diffuse tail. Basic Stage taps use the same formulas, and filename versions are segregated inside the C# managed folder. All functions are OFF by default on fresh profiles. Sample rate is selected explicitly, default 48 kHz; use the actual device sample rate for convolution.

AUDIO SAFETY: APO config file write/read check does not prove Windows has loaded the filter or that the response is correct. Test at modest volume. Existing Python settings are not edited. Disconnect C# from APO before returning to the Python version.
