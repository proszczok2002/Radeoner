param([string]$OutputDirectory = 'portable', [switch]$Test, [string]$VersionFile = (Join-Path $PSScriptRoot 'version.json'))
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Brak kompilatora .NET Framework x64.' }
$output = if ([IO.Path]::IsPathRooted($OutputDirectory)) { $OutputDirectory } else { Join-Path $root $OutputDirectory }
[IO.Directory]::CreateDirectory($output) | Out-Null
if (-not (Test-Path -LiteralPath (Join-Path $root 'src/Slots.cs'))) { & (Join-Path $root 'tools/Generate-AdlxSlots.ps1') }
$sources = @(Get-ChildItem (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object { $_.FullName })
$release = Get-Content -LiteralPath $VersionFile -Raw | ConvertFrom-Json
$assemblyVersion = [version]$release.assembly
if ($release.display -notmatch '^\d+(\.\d+){1,3}$' -or $assemblyVersion.Build -lt 0 -or $assemblyVersion.Revision -lt 0) { throw 'Invalid version.json' }
$generated = Join-Path $root ('artifacts/generated/' + $release.assembly)
[IO.Directory]::CreateDirectory($generated) | Out-Null
$versionSource = Join-Path $generated 'VersionInfo.cs'
$metadata = @"
using System.Reflection;
[assembly: AssemblyVersion("$($release.assembly)")]
[assembly: AssemblyFileVersion("$($release.assembly)")]
[assembly: AssemblyInformationalVersion("$($release.display)")]
namespace Radeoner { public static class BuildInfo { public const string DisplayVersion = "$($release.display)"; public const string AssemblyVersion = "$($release.assembly)"; } }
"@
[IO.File]::WriteAllText($versionSource, $metadata, [Text.UTF8Encoding]::new($true))
$sources += $versionSource
$manifest = [xml](Get-Content -LiteralPath (Join-Path $root 'app.manifest') -Raw)
$manifest.assembly.assemblyIdentity.version = $release.assembly
$generatedManifest = Join-Path $generated 'app.manifest'
$manifest.Save($generatedManifest)
$exe = Join-Path $output 'Radeoner.exe'
$resources = @('pl','en' | ForEach-Object { '/resource:' + (Join-Path $root "lang/$_.lng") + ",Radeoner.Languages.$_.lng" })
& $compiler /nologo /utf8output /target:winexe /platform:x64 /optimize+ /warn:4 "/out:$exe" "/win32manifest:$generatedManifest" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Management.dll /reference:Accessibility.dll /reference:System.Runtime.Serialization.dll /reference:System.Net.Http.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll $resources $sources
if ($LASTEXITCODE -ne 0) { throw 'Kompilacja Radeonera nie powiodła się.' }
Copy-Item -LiteralPath (Join-Path $root 'app.config') -Destination "$exe.config" -Force
Copy-Item -LiteralPath (Join-Path $root 'Czytaj.txt'),(Join-Path $root 'Readme.en.txt'),(Join-Path $root 'LICENSE'),(Join-Path $root 'THIRD_PARTY_NOTICES.md') -Destination $output -Force
[IO.Directory]::CreateDirectory((Join-Path $output 'lang')) | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'lang/pl.lng'),(Join-Path $root 'lang/en.lng') -Destination (Join-Path $output 'lang') -Force
Write-Output "Gotowe: $exe"
if ($Test) { & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'tests/Smoke.ps1') -AppDirectory $output; if ($LASTEXITCODE -ne 0) { throw 'Testy nie powiodły się.' } }
