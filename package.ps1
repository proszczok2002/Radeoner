param([switch]$Rebuild, [string]$AppDirectory = (Join-Path $PSScriptRoot 'portable'), [string]$OutputDirectory = (Join-Path $PSScriptRoot 'artifacts/releases'), [string]$VersionFile = (Join-Path $PSScriptRoot 'version.json'))
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
if ($Rebuild) {
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'build.ps1') -Test -OutputDirectory $AppDirectory -VersionFile $VersionFile
    if ($LASTEXITCODE -ne 0) { throw 'Build/tests failed.' }
}
$expected = @('Radeoner.exe', 'Radeoner.exe.config', 'Czytaj.txt', 'Readme.en.txt', 'LICENSE', 'THIRD_PARTY_NOTICES.md', 'lang/pl.lng', 'lang/en.lng')
$files = @($expected | ForEach-Object { Join-Path $AppDirectory $_ })
foreach ($file in $files) { if (-not (Test-Path -LiteralPath $file)) { throw "Missing file: $file" } }
$release = Get-Content -LiteralPath $VersionFile -Raw | ConvertFrom-Json
$outputFolder = Join-Path $OutputDirectory $release.display
[IO.Directory]::CreateDirectory($outputFolder) | Out-Null
$asset = 'Radeoner-' + $release.display + '-portable.zip'
$output = Join-Path $outputFolder $asset
$binaryVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $AppDirectory 'Radeoner.exe')).FileVersion
if ($binaryVersion -ne $release.assembly) { throw 'Binary version does not match version.json. Rebuild first.' }
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$stream = [IO.File]::Open($output, [IO.FileMode]::Create, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
try {
    $zip = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create, $true)
    try { for ($i=0; $i -lt $expected.Count; $i++) { [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $files[$i], $expected[$i]) | Out-Null } }
    finally { $zip.Dispose() }
} finally { $stream.Dispose() }
$archive = [IO.Compression.ZipFile]::OpenRead($output)
try {
    $actual = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\','/') })
    if (Compare-Object ($expected | Sort-Object) ($actual | Sort-Object)) { throw 'Unexpected ZIP contents.' }
} finally { $archive.Dispose() }
$manifestFiles = @()
for ($i=0; $i -lt $files.Count; $i++) {
    $manifestFiles += [ordered]@{Path=$expected[$i];Sha256=(Get-FileHash -LiteralPath $files[$i] -Algorithm SHA256).Hash.ToLowerInvariant();Size=(Get-Item -LiteralPath $files[$i]).Length}
}
$zipHash = (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash.ToLowerInvariant()
$metadata = [ordered]@{Format='Radeoner.Update';Schema=1;Version=$release.assembly;DisplayVersion=$release.display;Asset=$asset;Sha256=$zipHash;Size=(Get-Item -LiteralPath $output).Length;Files=$manifestFiles}
[IO.File]::WriteAllText((Join-Path $outputFolder 'update.json'), ($metadata | ConvertTo-Json -Depth 6), [Text.UTF8Encoding]::new($false))
$checksums = @(($zipHash + '  ' + $asset), ((Get-FileHash -LiteralPath (Join-Path $outputFolder 'update.json')).Hash.ToLowerInvariant() + '  update.json'))
[IO.File]::WriteAllLines((Join-Path $outputFolder 'SHA256SUMS.txt'), $checksums, [Text.UTF8Encoding]::new($false))
Write-Output "Release package: $output"
Write-Output "SHA-256: $zipHash"
