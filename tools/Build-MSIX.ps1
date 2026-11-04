# Build a full-trust MSIX from the existing self-contained win-x64 publish folder.
param([string]$Root = "", [switch]$Sign, [string]$CertificateThumbprint = "")
$ErrorActionPreference = 'Stop'
if (-not $Root) { $Root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path }
$Root = (Resolve-Path -LiteralPath $Root).Path
$publish = Join-Path $Root 'release\win-x64'
$exe = Join-Path $publish 'InfinityAudioEqualizerProWindows.exe'
if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) { throw 'Published EXE missing; first run build_no_setup.bat.' }
$project = [xml](Get-Content -Raw -LiteralPath (Join-Path $Root 'InfinityAudioEqualizerPro.csproj'))
$version = ([string]$project.Project.PropertyGroup.Version).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+\.\d+$') { throw "Invalid four-part MSIX version: $version" }
$versionText = $version -replace '\.0$', ''
$stage = Join-Path $Root 'release\msix\stage'
$output = Join-Path $Root ("release\msix\Infinity-Audio-Equalizer-Pro-Windows-$versionText-Beta-Test.msix")
$kitRoot = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'
$tools = @()
if (Test-Path $kitRoot) {
    $tools = @(Get-ChildItem -Path $kitRoot -Filter MakeAppx.exe -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match '\\x64\\MakeAppx\.exe$' } |
        Sort-Object FullName -Descending)
}
$makeappx = if ($env:MAKEAPPX_EXE -and (Test-Path $env:MAKEAPPX_EXE)) { $env:MAKEAPPX_EXE } elseif ($tools.Count -gt 0) { $tools[0].FullName } else { $null }
if (-not $makeappx) { throw 'MakeAppx.exe not found. Install Windows 10/11 SDK (MSIX tools), or set MAKEAPPX_EXE.' }
$signTool = Join-Path (Split-Path -Parent $makeappx) 'signtool.exe'
Write-Host "MakeAppx: $makeappx"
if (Test-Path $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
New-Item -ItemType Directory -Path $stage -Force | Out-Null
# Copy the actual publish payload, not source/build directories. Avoid stale prior MSIX files.
Copy-Item -Path (Join-Path $publish '*') -Destination $stage -Recurse -Force
$assetPath = Join-Path $stage 'Assets'
New-Item -ItemType Directory -Path $assetPath -Force | Out-Null
Copy-Item (Join-Path $Root 'msix\*.png') $assetPath -Force
$manifest = [xml](Get-Content -Raw -LiteralPath (Join-Path $Root 'msix\AppxManifest.template.xml'))
$ns = New-Object System.Xml.XmlNamespaceManager($manifest.NameTable)
$ns.AddNamespace('m','http://schemas.microsoft.com/appx/manifest/foundation/windows10')
$id = $manifest.SelectSingleNode('/m:Package/m:Identity', $ns)
if (-not $id) { throw 'Missing MSIX Identity node.' }
$id.SetAttribute('Version', $version)
$manifest.Save((Join-Path $stage 'AppxManifest.xml'))
foreach ($required in @('InfinityAudioEqualizerProWindows.exe','InfinityCompressor.vst3','surround3_hrir.bin','kemar_hrir_data.bin','LICENSE','AppxManifest.xml')) {
    if (-not (Test-Path (Join-Path $stage $required))) { throw "Required payload missing: $required" }
}
if (Test-Path $output) { Remove-Item -LiteralPath $output -Force }
& $makeappx pack /d $stage /p $output /o
if ($LASTEXITCODE -ne 0) { throw "MakeAppx failed with exit code $LASTEXITCODE" }
if ($Sign) {
    if (-not $CertificateThumbprint) { throw 'To sign, specify -CertificateThumbprint from your development certificate.' }
    if (-not (Test-Path $signTool)) { throw "SignTool missing: $signTool" }
    & $signTool sign /fd SHA256 /sha1 $CertificateThumbprint $output
    if ($LASTEXITCODE -ne 0) { throw "SignTool failed with exit code $LASTEXITCODE" }
    & $signTool verify /pa $output
    if ($LASTEXITCODE -ne 0) { Write-Warning 'SignTool /pa verification may not validate MSIX package signatures; use Get-AppxPackageSignature if available and Add-AppxPackage on test system.' }
    Write-Host "SIGNED TEST MSIX: $output"
} else {
    Write-Warning 'Unsigned MSIX created for packaging validation ONLY. It cannot be installed until signed and trusted.'
    Write-Host "UNSIGNED MSIX: $output"
}
