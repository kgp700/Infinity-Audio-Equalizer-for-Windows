InfinityCompressor VST3 — source build notes

Infinity Audio Equalizer Pro-authored plugin source: vst3/src/ (MIT License, root LICENSE)
Upstream dependency: Steinberg VST3 SDK >= 3.8, MIT License

Install Git, CMake (>=3.21) and Windows Visual Studio C++ build tools.
Clone the SDK and its submodules (not bundled in this repository):
  git clone --recursive https://github.com/steinbergmedia/vst3sdk.git vst3/vst3sdk
Verify the checked-out SDK is version 3.8 or later and preserve its LICENSE and dependency notices.

From project root:
  cmake -S vst3 -B vst3/build -A x64
  cmake --build vst3/build --config Release

Then validate the generated VST3 with XT and a VST3 validator. Record SDK commit,
compiler version and binary SHA-256 for each end-user release.

For ordinary Windows builds the compatible precompiled InfinityCompressor.vst3 directory
can be placed in the project root; it is intentionally excluded from Git history.
The FULL development ZIP contains an existing binary for local testing, but its
bit-for-bit correspondence to this recovered source has not been verified.
Do not claim binary provenance or release-license completion without this check.
