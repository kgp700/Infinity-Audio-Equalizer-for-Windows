# Explicit opt-in: test certificate creation; never for public GitHub distribution.
$ErrorActionPreference = 'Stop'
$subject = 'CN=KGP Software Lab MSIX Test'
$cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject $subject -CertStoreLocation 'Cert:\CurrentUser\My' -KeyExportPolicy Exportable -KeyUsage DigitalSignature -NotAfter (Get-Date).AddYears(2)
if (-not $cert) { throw 'Test certificate creation failed.' }
$out = Join-Path (Resolve-Path (Join-Path $PSScriptRoot '..')).Path 'release\msix'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$cer = Join-Path $out 'MSIX-TEST-ONLY.cer'
Export-Certificate -Cert $cert -FilePath $cer -Force | Out-Null
Write-Host "TEST THUMBPRINT: $($cert.Thumbprint)"
Write-Host "TEST CERTIFICATE (PUBLIC ONLY): $cer"
Write-Host 'Do NOT publish or distribute private key or trust an unknown certificate.'
Write-Host ('Build signed package: .\build_msix.bat -Sign -CertificateThumbprint ' + $cert.Thumbprint)
Write-Host 'Before installation, explicitly trust the exported certificate on the TEST computer.'
