param([string]$Root = ".", [switch]$Published)
$ErrorActionPreference = 'Stop'
$base = (Resolve-Path $Root).Path
$files = @('LICENSE','THIRD_PARTY_NOTICES.md','HRTF_DATA_CREDITS.txt',
  'legal/OPEN_SOURCE_LICENSES.md','legal/TERMS_AND_LICENSE.md',
  'THIRD_PARTY_LICENSES/Steinberg-VST3-SDK-LICENSE.txt',
  'THIRD_PARTY_LICENSES/AutoEq-LICENSE.txt')
if ($Published) {
  $files = @('LICENSE','THIRD_PARTY_NOTICES.md','HRTF_DATA_CREDITS.txt',
    'legal/OPEN_SOURCE_LICENSES.md','legal/TERMS_AND_LICENSE.md',
    'THIRD_PARTY_LICENSES/Steinberg-VST3-SDK-LICENSE.txt',
    'THIRD_PARTY_LICENSES/AutoEq-LICENSE.txt')
}
$failed = $false
foreach($file in $files) {
  if (-not (Test-Path -LiteralPath (Join-Path $base $file) -PathType Leaf)) {
    Write-Host "MISSING: $file" -ForegroundColor Red; $failed = $true
  }
}
if (-not $Published) {
  foreach($file in @('vst3/src/plugin.cpp','vst3/src/compressor_core.h','vst3/CMakeLists.txt','release-assets/hrtf_provenance.json')) {
    if (-not (Test-Path -LiteralPath (Join-Path $base $file) -PathType Leaf)) {Write-Host "MISSING: $file" -ForegroundColor Red; $failed = $true}
  }
}
if ($failed) { exit 1 }
Write-Host 'PASS: required copyright and license files present.' -ForegroundColor Green
exit 0
