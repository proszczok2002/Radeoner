param([string]$AppDirectory = (Join-Path (Split-Path $PSScriptRoot -Parent) 'portable'), [switch]$LiveRead, [switch]$LiveUi, [switch]$LiveTray, [switch]$LiveDialogs, [switch]$LiveKeyboard)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$output = Join-Path $root 'artifacts/tests'
[IO.Directory]::CreateDirectory($output) | Out-Null
$app = Join-Path $AppDirectory 'Radeoner.exe'
if (-not (Test-Path -LiteralPath $app)) { throw 'Build Radeoner first.' }
Copy-Item -LiteralPath $app -Destination $output -Force
Copy-Item -LiteralPath "$app.config" -Destination $output -Force
$testConfig = Join-Path $output 'Radeoner.Tests.exe.config'
Copy-Item -LiteralPath "$app.config" -Destination $testConfig -Force
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$testExe = Join-Path $output 'Radeoner.Tests.exe'
$fixture = Join-Path $output 'Radeoner.UpdateFixture.exe'
& $compiler /nologo /target:winexe /platform:x64 /optimize+ "/out:$fixture" (Join-Path $PSScriptRoot 'fixtures/UpdateFixture.cs')
if ($LASTEXITCODE -ne 0) { throw 'Update fixture build failed.' }
$sources = @(Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' | ForEach-Object { $_.FullName })
& $compiler /nologo /utf8output /target:exe /platform:x64 /optimize+ /warn:4 "/win32manifest:$(Join-Path $root 'app.manifest')" "/out:$testExe" "/reference:$app" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:Accessibility.dll /reference:System.Runtime.Serialization.dll /reference:System.Net.Http.dll /reference:System.IO.Compression.dll $sources
if ($LASTEXITCODE -ne 0) { throw 'Test build failed.' }
& $testExe
if ($LASTEXITCODE -ne 0) {
    $driverLog = Join-Path $output 'Radeoner.log'
    if (Test-Path -LiteralPath $driverLog) { Get-Content -LiteralPath $driverLog -Encoding UTF8 -Tail 60 }
    throw 'Tests failed.'
}
if ($LiveRead) {
    $report = Join-Path $output 'live-read.txt'
    $process = Start-Process -FilePath $app -ArgumentList @('--report', ('"' + $report + '"')) -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit(30000)) { throw "Read-only diagnostic is still running (PID $($process.Id))." }
    if ($process.ExitCode -ne 0) { throw "Read-only diagnostic failed: $report" }
    $text = Get-Content -LiteralPath $report -Encoding UTF8 -Raw
    if ($text -notmatch 'Radeoner|ADLX') { throw 'Invalid diagnostic report.' }
    Write-Output "PASS live read-only diagnostic: $report"
}
if ($LiveUi -or $LiveTray -or $LiveKeyboard) {
    $report = Join-Path $output $(if($LiveKeyboard){'live-keyboard.txt'}elseif($LiveTray){'live-tray.txt'}else{'live-ui.txt'})
    $mode = if($LiveKeyboard){'--live-keyboard'}elseif($LiveTray){'--live-tray'}else{'--live-ui'}
    $windowStyle = if($LiveKeyboard){'Normal'}else{'Hidden'}
    $process = Start-Process -FilePath $testExe -ArgumentList @($mode, ('"' + $report + '"')) -WindowStyle $windowStyle -PassThru
    $uiTimeout = if($LiveKeyboard){90000}else{40000}
    if (-not $process.WaitForExit($uiTimeout)) { throw "UI test is still running (PID $($process.Id))." }
    if ($process.ExitCode -ne 0) { throw "UI startup failed: $report" }
    Write-Output (Get-Content -LiteralPath $report -Encoding UTF8 -TotalCount 1)
}
if ($LiveDialogs) {
    $report = Join-Path $output 'live-dialogs.txt'
    $process = Start-Process -FilePath $testExe -ArgumentList @('--live-dialogs', ('"' + $report + '"')) -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit(20000)) { throw "Native dialog test is still running (PID $($process.Id))." }
    if ($process.ExitCode -ne 0) { throw "Native dialog test failed: $report" }
    Write-Output (Get-Content -LiteralPath $report -Encoding UTF8 -TotalCount 1)
}
