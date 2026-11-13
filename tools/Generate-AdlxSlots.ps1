$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$names = @('IADLXGPU','IADLXGPUList','IADLXSystem','IADLXGPUTuningServices','IADLXManualGraphicsTuning1','IADLXManualGraphicsTuning2','IADLXManualVRAMTuning1','IADLXManualVRAMTuning2','IADLXManualPowerTuning','IADLXManualFanTuning','IADLXManualTuningState','IADLXManualTuningStateList','IADLXManualFanTuningState','IADLXManualFanTuningStateList','IADLXPerformanceMonitoringServices','IADLXGPUMetrics','IADLXGPUMetricsSupport')
$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('// Generated from AMD ADLX headers. Run tools/Generate-AdlxSlots.ps1 after SDK changes.')
$lines.Add('namespace Radeoner { internal static class Slots {')
$seen = @{}
Get-ChildItem (Join-Path $root 'vendor/ADLX/SDK/Include') -Filter '*.h' | ForEach-Object {
    $source = Get-Content -LiteralPath $_.FullName -Raw
    foreach ($match in [regex]::Matches($source, '(?s)typedef struct (\w+)Vtbl\s*\{(.*?)\}\s*\w+;')) {
        $name = $match.Groups[1].Value
        if ($names -notcontains $name) { continue }
        $seen[$name] = $true
        $lines.Add(('internal static class {0} {{' -f $name))
        $slot = 0
        foreach ($method in [regex]::Matches($match.Groups[2].Value, 'ADLX_STD_CALL\s*\*\s*(\w+)\s*\)')) {
            $lines.Add(('internal const int {0} = {1};' -f $method.Groups[1].Value, $slot))
            $slot++
        }
        if ($slot -eq 0) { throw "Empty vtable: $name" }
        $lines.Add('}')
    }
}
foreach ($name in $names) { if (-not $seen[$name]) { throw "Missing vtable: $name" } }
$lines.Add('} }')
[IO.File]::WriteAllLines((Join-Path $root 'src/Slots.cs'), $lines, [Text.UTF8Encoding]::new($true))
