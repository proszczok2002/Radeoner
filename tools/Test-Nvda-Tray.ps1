$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$report=Join-Path $root ('artifacts/tests/nvda-tray-keyboard-'+(Get-Date -Format 'HHmmss')+'.txt')
Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes
Add-Type @'
using System; using System.Runtime.InteropServices;
public static class TrayEnter {
 [StructLayout(LayoutKind.Explicit,Size=40)] public struct Input { [FieldOffset(0)] public uint type; [FieldOffset(8)] public ushort key; [FieldOffset(12)] public uint flags; }
 [DllImport("user32.dll")] static extern uint SendInput(uint count,Input[] keys,int size);
 [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
 public static void Press() { foreach(int k in new[]{0x10,0x11,0x12,0x5B,0x5C,0x2D,0x14}) if((GetAsyncKeyState(k)&0x8000)!=0) throw new Exception("A user modifier is held"); if(SendInput(2,new[]{new Input{type=1,key=13},new Input{type=1,key=13,flags=2}},40)!=2)throw new Exception("Enter refused"); }
}
'@
$process=Start-Process -FilePath (Join-Path $root 'artifacts/tests/Radeoner.Tests.exe') -ArgumentList '--nvda-tray',('"'+$report+'"') -WindowStyle Normal -PassThru
$watch=[Diagnostics.Stopwatch]::StartNew()
while(-not (Test-Path -LiteralPath ($report+'.ready')) -and $watch.ElapsedMilliseconds -lt 15000){Start-Sleep -Milliseconds 100}
if(-not (Test-Path -LiteralPath ($report+'.ready'))){throw 'Own tray fixture not ready'}
$release=Get-Content -LiteralPath (Join-Path $root 'version.json') -Raw | ConvertFrom-Json
$prefix='Radeoner v'+$release.display
function Find-AuditIcon {
 $roots=[Windows.Automation.AutomationElement]::RootElement.FindAll([Windows.Automation.TreeScope]::Children,[Windows.Automation.Condition]::TrueCondition)
 foreach($window in $roots) {
  if($window.Current.ClassName -notmatch 'Shell_TrayWnd|Overflow'){continue}
  $children=$window.FindAll([Windows.Automation.TreeScope]::Descendants,[Windows.Automation.Condition]::TrueCondition)
  foreach($child in $children){if($child.Current.Name.StartsWith($prefix) -and $child.Current.ControlType -eq [Windows.Automation.ControlType]::Button){return $child}}
 }
 return $null
}
$icon=Find-AuditIcon
if(-not $icon){
 $bar=[Windows.Automation.AutomationElement]::RootElement.FindFirst([Windows.Automation.TreeScope]::Children,[Windows.Automation.PropertyCondition]::new([Windows.Automation.AutomationElement]::ClassNameProperty,'Shell_TrayWnd'))
 $buttons=$bar.FindAll([Windows.Automation.TreeScope]::Descendants,[Windows.Automation.PropertyCondition]::new([Windows.Automation.AutomationElement]::ControlTypeProperty,[Windows.Automation.ControlType]::Button))
 $more=@($buttons | Where-Object {$_.Current.Name -match 'Pokaż ukryte ikony|Show hidden icons|Ukryte ikony'})
 if($more.Count -eq 1){$more[0].GetCurrentPattern([Windows.Automation.InvokePattern]::Pattern).Invoke(); Start-Sleep -Milliseconds 400}
 $icon=Find-AuditIcon
}
if(-not $icon){throw 'Exact owned 1.05 notification icon not found'}
$icon.SetFocus(); Start-Sleep -Milliseconds 250
if(-not [Windows.Automation.AutomationElement]::FocusedElement.Current.Name.StartsWith($prefix)){throw 'Own icon did not receive focus'}
[IO.File]::WriteAllText($report+'.focus','ready')
while(-not (Test-Path -LiteralPath ($report+'.focused')) -and $watch.ElapsedMilliseconds -lt 30000){Start-Sleep -Milliseconds 100}
if(-not (Test-Path -LiteralPath ($report+'.focused'))){throw 'Speech checkpoint did not acknowledge own icon'}
if(-not [Windows.Automation.AutomationElement]::FocusedElement.Current.Name.StartsWith($prefix)){throw 'Own icon lost focus; no keys sent'}
[TrayEnter]::Press()
if(-not $process.WaitForExit(10000)){throw 'Tray fixture did not exit'}
Get-Content -LiteralPath $report -Encoding UTF8 -Tail 12
if($process.ExitCode -ne 0){throw 'Actual tray verification failed'}
