param(
    [Parameter(Mandatory=$true)][string]$OldArchive,
    [Parameter(Mandatory=$true)][string]$ExpectedManifest,
    [string]$NewArchive,
    [switch]$FromGitHub
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$kind = if($FromGitHub){'github-'}else{'local-'}
$folder = Join-Path $root ('artifacts/update-e2e/' + $kind + (Get-Date -Format 'yyyyMMdd-HHmmss'))
[IO.Directory]::CreateDirectory($folder) | Out-Null
$app = Join-Path $folder 'portable with spaces'
Expand-Archive -LiteralPath $OldArchive -DestinationPath $app
$exe = Join-Path $app 'Radeoner.exe'
$beforeVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($exe).FileVersion
$beforeHash = (Get-FileHash -LiteralPath $exe).Hash.ToLowerInvariant()
$expected = Get-Content -LiteralPath $ExpectedManifest -Encoding UTF8 -Raw | ConvertFrom-Json
$expectedExe = $expected.Files | Where-Object { $_.Path -eq 'Radeoner.exe' }
if ([version]$expected.Version -le [version]$beforeVersion) { throw 'Expected version must be newer.' }
if (@(Get-Process -Name Radeoner -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $exe }).Count) { throw 'The isolated test folder is already in use.' }
$settingsFile = Join-Path $app 'Radeoner.settings.json'
$profileFile = Join-Path $app 'my-profile.json'
[IO.File]::WriteAllText($settingsFile,'{"Version":1,"Language":"en","Theme":"dark","TextScale":125,"RefreshSensors":false,"CheckUpdates":false,"CheckUpdatesOnStartup":false,"UpdateIntervalMinutes":120}')
[IO.File]::WriteAllText($profileFile,'{"test":"user profile must remain unchanged"}')
$settingsHash = (Get-FileHash -LiteralPath $settingsFile).Hash
$profileHash = (Get-FileHash -LiteralPath $profileFile).Hash
$arguments = '--test-update'
if (-not $FromGitHub) {
    if (-not $NewArchive) { throw 'Provide NewArchive for a local test.' }
    $arguments += ' "' + (Resolve-Path -LiteralPath $ExpectedManifest).Path + '" "' + (Resolve-Path -LiteralPath $NewArchive).Path + '"'
}
Write-Output "Starting $beforeVersion -> $($expected.Version), source: $kind"
$parent = Start-Process -FilePath $exe -ArgumentList $arguments -WorkingDirectory $app -WindowStyle Hidden -PassThru
if (-not $parent.WaitForExit(45000)) { throw "Update download is still running: PID $($parent.Id)." }
if ($parent.ExitCode -ne 0) {
    $log = Join-Path $app 'update-smoke-error.txt'
    if(Test-Path -LiteralPath $log){ Get-Content -LiteralPath $log }
    throw "Updater exited with code $($parent.ExitCode)."
}
$deadline = [DateTime]::UtcNow.AddSeconds(100)
do {
    $staging = @(Get-ChildItem -LiteralPath $app -Directory -Force | Where-Object { $_.Name -like '.radeoner-update-*' })
    $version = if(Test-Path -LiteralPath $exe){ [Diagnostics.FileVersionInfo]::GetVersionInfo($exe).FileVersion } else { '' }
    if ($version -eq $expected.Version -and $staging.Count -eq 0) { break }
    Start-Sleep -Milliseconds 250
} while ([DateTime]::UtcNow -lt $deadline)
if ($version -ne $expected.Version -or $staging.Count -ne 0) {
    foreach($stage in $staging) { Get-ChildItem -LiteralPath $stage.FullName -Recurse -File | Select-Object FullName; Get-Content -LiteralPath (Join-Path $stage.FullName 'error.txt') -ErrorAction SilentlyContinue }
    throw 'Update did not finish and remove its staging/backup directory.'
}
if ((Get-FileHash -LiteralPath $exe).Hash.ToLowerInvariant() -ne $expectedExe.Sha256) { throw 'Final EXE checksum mismatch.' }
if ((Get-FileHash -LiteralPath $settingsFile).Hash -ne $settingsHash -or (Get-FileHash -LiteralPath $profileFile).Hash -ne $profileHash) { throw 'Preferences or profile changed.' }
$result = [ordered]@{Source=$(if($FromGitHub){'GitHub public latest release'}else{'Local package'});BeforeVersion=$beforeVersion;AfterVersion=$version;BeforeExeSha256=$beforeHash;AfterExeSha256=$expectedExe.Sha256;SettingsPreserved=$true;ProfilePreserved=$true;OldExeAndHelperRemoved=$true;FinishedUtc=[DateTime]::UtcNow.ToString('o');Directory=$app}
[IO.File]::WriteAllText((Join-Path $folder 'result.json'),($result | ConvertTo-Json -Depth 5))
Write-Output 'PASS real EXE replacement, new WinForms startup confirmation, old EXE/helper cleanup and preserved preferences/profile.'
Write-Output "Evidence: $(Join-Path $folder 'result.json')"
