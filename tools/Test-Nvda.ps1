param([string]$AppDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(-not $AppDirectory) {
 $AppDirectory=Join-Path $root 'artifacts/nvda-candidate'
 & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'build.ps1') -OutputDirectory $AppDirectory
 if($LASTEXITCODE -ne 0) { throw 'Isolated audit build failed.' }
}
# Compile and run the regular checks before the interactive reader tour.
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'tests/Smoke.ps1') -AppDirectory $AppDirectory
if($LASTEXITCODE -ne 0) { throw 'Automated checks failed; NVDA tour was not started.' }
$testExe=Join-Path $root 'artifacts/tests/Radeoner.Tests.exe'
$report=Join-Path $root 'artifacts/tests/live-nvda.txt'
# Normal is intentional: this is an explicitly requested interactive audit.
$process=Start-Process -FilePath $testExe -ArgumentList '--live-nvda',('"'+$report+'"') -WindowStyle Normal -PassThru
Write-Output "Interactive NVDA audit PID $($process.Id). Report: $report"
Write-Output 'Keep Speech Viewer open. Do not use the keyboard during the tour. It reads actual NVDA text and changes no real GPU settings.'
# Poll in short intervals; foreground/reader failures are recorded rather than bypassed.
while(-not $process.WaitForExit(1000)) { }
if($process.ExitCode -ne 0) {
 Get-Content -LiteralPath $report -Encoding UTF8
 throw 'NVDA verification incomplete. No accessibility approval was recorded.'
}
Write-Output 'Captured NVDA checkpoints. Review the transcript for duplicate/unclear speech and complete the manual dialog/tray checklist before release.'
