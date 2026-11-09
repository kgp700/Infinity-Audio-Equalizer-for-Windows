# Third-party notices

Infinity Audio Equalizer Pro for Windows — KGP Software Lab

**The application and its own C#/C++ sources are licensed under MIT** (see `LICENSE`).
This license does not replace the rights and terms of independent components or measurement data.

## Distributable components in the Windows package

| Component | License / rights | Treatment |
| --- | --- | --- |
| Infinity Audio Equalizer Pro C# WinForms application | MIT; Copyright (c) 2026 KGP Software Lab | Source and notice provided in `LICENSE`. |
| InfinityCompressor VST3 plugin (Infinity Audio Equalizer Pro-authored code) | MIT; Copyright (c) 2026 KGP Software Lab | Source in `vst3/`; local precompiled binary is provided in the FULL development package and may be included in end-user Releases. |
| Steinberg VST3 SDK (used to build the plugin) | MIT for SDK 3.8 and later | Copyright and full MIT text: `THIRD_PARTY_LICENSES/Steinberg-VST3-SDK-LICENSE.txt`. The actual SDK version and submodule licenses used for each binary build must be recorded before release. |
| MIT KEMAR HRIR measurements | Copyright (c) 1994 MIT Media Laboratory; attribution required | Authors Bill Gardner and Keith Martin; see `HRTF_DATA_CREDITS.txt` and `release-assets/hrtf_provenance.json`. Not relicensed as MIT software. |
| Microsoft .NET / Windows Forms | Microsoft runtime / operating system terms | This project currently builds as a **framework-dependent** app; .NET runtime is not copied into the source package. If a self-contained runtime is shipped later, review and include relevant runtime notices. |

## Separate services / optional installations

- **EqualizerAPO-XT** is downloaded from the official upstream release and installed separately upon user request. Its own code is licensed primarily under GPL-2.0-or-later and some bundled components may have further terms (e.g. ASIO wrapper GPLv3). **No EqualizerAPO-XT executable, DLL or installer is included in this application's distribution.** See https://github.com/115dkk/EqualizerAPO-XT for authoritative license and bundled third-party notices.
- **AutoEq**: Project code by Jaakko Pasanen is MIT; `THIRD_PARTY_LICENSES/AutoEq-LICENSE.txt` preserves its license. The application retrieves user-selected equalization profiles from upstream; these measurements and presets may depend on independent measurement authors (e.g. Rtings, oratory1990 and others), whose rights are not overridden by the project MIT license. No mass measurement dataset is shipped in this project.

## Trademark and attribution

- "VST" is a trademark of Steinberg Media Technologies GmbH. The product does not claim endorsement or certification by Steinberg.
- EqualizerAPO-XT, AutoEq, Microsoft and their respective names remain the property of their owners. This application is not an official release of those projects.

## Source, checksums and release review

- Application C# and compressor C++ source: this project repository (`src/`, `vst3/src/`).
- Official VST3 SDK: https://github.com/steinbergmedia/vst3sdk (recursively checked out locally for a source build; **not** vendored).
- KEMAR upstream terms: https://sound.media.mit.edu/resources/KEMAR.html
- The included prebuilt VST3 binary is **not cryptographically proven** to match the recovered compressor sources. Confirm reproducibility, SDK version, and build provenance before publishing it as an official release.

The older Python/PySide6/PyInstaller runtime notices and LGPL library-replacement guide apply only to *historical Python distributions*, not to the present .NET build. They are intentionally not bundled as the current app's licenses.
