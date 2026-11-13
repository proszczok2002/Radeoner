[CmdletBinding(SupportsShouldProcess=$true, ConfirmImpact='Low')]
param()
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent)).TrimEnd('\')
$archive = Join-Path $root 'niepotrzebne'
$release = Get-Content -LiteralPath (Join-Path $root 'version.json') -Raw | ConvertFrom-Json
if ($release.display -notmatch '^\d+(\.\d+){1,3}$') { throw 'Invalid release version.' }
$currentAssets = @(('Radeoner-' + $release.display + '-portable.zip'), 'update.json', 'SHA256SUMS.txt')
$moves = [Collections.Generic.List[string]]::new()
Get-ChildItem -LiteralPath $root -File -Filter 'Radeoner-*-portable.zip' | ForEach-Object { $moves.Add($_.FullName) }
$vendor = Join-Path $root 'vendor'
if (Test-Path -LiteralPath $vendor) { $moves.Add($vendor) }
$artifacts = Join-Path $root 'artifacts'
if (Test-Path -LiteralPath $artifacts) {
    Get-ChildItem -LiteralPath $artifacts -Force | ForEach-Object {
        if ($_.Name -ne 'releases' -or -not $_.PSIsContainer) { $moves.Add($_.FullName) }
        else {
            Get-ChildItem -LiteralPath $_.FullName -Force | ForEach-Object {
                if ($_.Name -ne $release.display -or -not $_.PSIsContainer) { $moves.Add($_.FullName) }
                else {
                    Get-ChildItem -LiteralPath $_.FullName -Force | Where-Object { $_.PSIsContainer -or $_.Name -notin $currentAssets } | ForEach-Object { $moves.Add($_.FullName) }
                }
            }
        }
    }
}
if ($moves.Count -eq 0) { Write-Output 'Project is already tidy.'; return }
$batch = Join-Path $archive ((Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0,6))
$index = [Collections.Generic.List[object]]::new()
$running = @(Get-Process -Name Radeoner,Radeoner.Tests -ErrorAction SilentlyContinue)
foreach ($source in $moves) {
    $absoluteSource = [IO.Path]::GetFullPath($source)
    if (-not $absoluteSource.StartsWith($root + '\', [StringComparison]::OrdinalIgnoreCase) -or
        $absoluteSource.StartsWith($archive + '\', [StringComparison]::OrdinalIgnoreCase) -or
        $absoluteSource -eq (Join-Path $root '.git')) { throw "Unsafe source: $absoluteSource" }
    $relative = $absoluteSource.Substring($root.Length + 1)
    $target = [IO.Path]::GetFullPath((Join-Path $batch $relative))
    if (-not $target.StartsWith($archive + '\', [StringComparison]::OrdinalIgnoreCase)) { throw "Unsafe destination: $target" }
    foreach ($process in $running) {
        if ($process.Path -and ($process.Path -eq $absoluteSource -or $process.Path.StartsWith($absoluteSource + '\', [StringComparison]::OrdinalIgnoreCase))) {
            throw "A running process is using $absoluteSource (PID $($process.Id))."
        }
    }
    if (Test-Path -LiteralPath $target) { throw "Archive destination exists: $target" }
}
foreach ($source in $moves) {
    $relative = [IO.Path]::GetFullPath($source).Substring($root.Length + 1)
    $target = [IO.Path]::GetFullPath((Join-Path $batch $relative))
    if ($PSCmdlet.ShouldProcess($relative, 'Move into niepotrzebne')) {
        [IO.Directory]::CreateDirectory((Split-Path $target -Parent)) | Out-Null
        Move-Item -LiteralPath $source -Destination $target
        $index.Add([pscustomobject]@{Original=$relative;Archived=$target.Substring($root.Length+1)})
        # Save after each move so an interrupted run retains its recovery index.
        $index | Export-Csv -LiteralPath (Join-Path $batch 'INDEKS.csv') -NoTypeInformation -Encoding UTF8
    }
}
if ($index.Count) {
    $text = @'
ARCHIWUM PROJEKTU RADEONER

Stare pliki pozostaja na dysku w podfolderach oznaczonych data.
INDEKS.csv w kazdym podfolderze zawiera oryginalne i archiwalne sciezki.
Do przywrocenia pojedynczego pliku uzyj indeksu; nie nadpisuj biezacych zrodel.
Zawartosc archiwum nie jest potrzebna do zwyklej kompilacji ani uruchomienia.
SDK AMD, jesli zarchiwizowane, pozostaje materialem opcjonalnym.
Publiczne wyniki testu aktualizacji: docs/UPDATE_TEST.md.
Folder jest pomijany przez Git. Niczego tutaj automatycznie nie usuwamy.
'@
    [IO.File]::WriteAllText((Join-Path $archive 'Czytaj.txt'), $text, [Text.UTF8Encoding]::new($true))
    Write-Output "Moved $($index.Count) entries. Index: $(Join-Path $batch 'INDEKS.csv')"
}
