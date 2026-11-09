# Project-only cleanup: preserve the Git metadata and both cleaning scripts.
# Never invoke this file from a remote URL. Run CLEAN_PROJECT.bat in the target directory.
$ErrorActionPreference = 'Stop'
$root = [System.IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\')
$rootItem = Get-Item -LiteralPath $root -Force
if (($rootItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
    Write-Host '[BLOCKED] The project directory is a junction or symbolic link.' -ForegroundColor Red
    exit 2
}
$driveRoot = [System.IO.Path]::GetPathRoot($root).TrimEnd('\')
$protectedRoots = @($driveRoot, $env:SystemRoot, $env:USERPROFILE, $env:PUBLIC, $env:ProgramFiles, ${env:ProgramFiles(x86)}, $env:ProgramData, $env:APPDATA, $env:LOCALAPPDATA) | Where-Object { $_ }
foreach ($badPath in $protectedRoots) {
    if ($root -ieq $badPath.TrimEnd('\')) {
        Write-Host ('[BLOCKED] A protected top-level directory was selected: ' + $root) -ForegroundColor Red
        exit 2
    }
}
$gitPath = Join-Path $root '.git'
$hasGit = Test-Path -LiteralPath $gitPath
$keep = @('.git', 'CLEAN_PROJECT.bat', 'CLEAN_PROJECT.ps1')
$children = @(Get-ChildItem -LiteralPath $root -Force | Where-Object { $keep -notcontains $_.Name })
Write-Host ''
Write-Host '[TARGET DIRECTORY]' -ForegroundColor Cyan
Write-Host $root
Write-Host ''
Write-Host ('Git metadata (.git): ' + $(if ($hasGit) { 'FOUND - WILL BE PRESERVED' } else { 'NOT FOUND' })) -ForegroundColor $(if ($hasGit) { 'Green' } else { 'Yellow' })
Write-Host 'Always preserved: CLEAN_PROJECT.bat, CLEAN_PROJECT.ps1'
if (-not $hasGit) {
    Write-Host '[WARNING] No .git was found. This cleanup will still work, but there is no Git history to preserve.' -ForegroundColor Yellow
}
# Protect nested repositories and linked directories; do not walk through reparse points.
$unsafe = @()
foreach ($item in $children) {
    if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        $unsafe += $item.FullName
        continue
    }
    if ($item.PSIsContainer) {
        $nestedGit = @(Get-ChildItem -LiteralPath $item.FullName -Force -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -eq '.git' -and ($_.PSIsContainer -or -not $_.PSIsContainer) } |
            Select-Object -First 1)
        if ($nestedGit.Count -gt 0) { $unsafe += $item.FullName }
    }
}
if ($unsafe.Count -gt 0) {
    Write-Host '[BLOCKED] Found nested Git metadata or directory links. No changes were made:' -ForegroundColor Red
    $unsafe | ForEach-Object { Write-Host ('  ' + $_) }
    exit 2
}
Write-Host ''
Write-Host ('[DELETE PREVIEW] ' + $children.Count + ' top-level items') -ForegroundColor Yellow
foreach ($item in $children) {
    Write-Host ('  ' + $(if ($item.PSIsContainer) { '[DIR]  ' } else { '[FILE] ' }) + $item.Name)
}
if ($children.Count -eq 0) {
    Write-Host 'Nothing to delete.' -ForegroundColor Green
    exit 0
}
Write-Host ''
Write-Host 'IMPORTANT: Files are permanently removed (not moved to Recycle Bin).' -ForegroundColor Yellow
$first = Read-Host 'Proceed? Type Y to continue'
if ($first -cne 'Y') { Write-Host '[CANCELLED] No changes made.'; exit 1 }
$second = Read-Host 'FINAL CONFIRMATION: type DELETE in uppercase'
if ($second -cne 'DELETE') { Write-Host '[CANCELLED] No changes made.'; exit 1 }
$errors = 0
foreach ($item in $children) {
    try {
        Remove-Item -LiteralPath $item.FullName -Recurse -Force -ErrorAction Stop
        Write-Host ('[DELETED] ' + $item.Name)
    } catch {
        $errors++
        Write-Host ('[ERROR] Could not delete: ' + $item.FullName + ' - ' + $_.Exception.Message) -ForegroundColor Red
    }
}
if ($errors -gt 0) { Write-Host ('[INCOMPLETE] ' + $errors + ' item(s) failed.') -ForegroundColor Yellow; exit 3 }
if ($hasGit -and -not (Test-Path -LiteralPath $gitPath)) {
    Write-Host '[ERROR] Git metadata is missing after cleanup. Restore from backup!' -ForegroundColor Red
    exit 4
}
Write-Host '[SUCCESS] Cleanup finished; protected items are intact.' -ForegroundColor Green
exit 0
