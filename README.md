# Radeoner

**1.08** shows the full release notes in the update window and checks drivers
directly on AMD’s product pages. [Release notes](docs/releases/v1.08.md)

**A lightweight, portable AMD GPU panel for Windows, designed with NVDA in mind.**

[Polski](README.pl.md) · [Download](https://github.com/proszczok2002/Radeoner/releases/latest) · [Report an issue](https://github.com/proszczok2002/Radeoner/issues) · [Contribute](CONTRIBUTING.md)

[Deutsch](README.de.md) · [Русский](README.ru.md) · [Español](README.es.md) · [Français](README.fr.md)

Radeoner communicates with the installed AMD driver through the public ADLX API.
Use the monitoring and tuning capabilities your GPU exposes without installing
the Adrenalin control panel. Its Microsoft Windows Forms/.NET Framework 4.8 UI
uses standard controls, keyboard navigation, explicit screen-reader names and
text results. No browser runtime, Radeoner service or extra kernel driver.

The application source is public under the **MIT license**. Bug reports,
translations, accessibility improvements and pull requests are welcome.

## Download and run

1. Download `Radeoner-<version>-portable.zip` from [Releases](https://github.com/proszczok2002/Radeoner/releases).
2. Extract the complete archive into a writable folder.
3. Run **Radeoner.exe**. No installer or administrator privileges are required.

Requirements: **Windows 10/11 x64, .NET Framework 4.8**, and an AMD driver that
supplies ADLX. Radeoner loads the installed ADLX library from System32.

**Recommended setup for Radeoner: Driver Only.** Download the correct AMD
package and choose **Additional Options → Driver Only** in the installer.
The full Adrenalin control panel is optional. If you keep it, use a single
app for GPU tuning and turn off automatic Adrenalin tuning before writing
with Radeoner. Leave **Factory Reset** off for a routine update. For a clean
reinstall, AMD's optional Factory Reset removes previous packages and disables
rollback to the previous driver. On laptops, also check the manufacturer's
driver. [Official AMD installation instructions](https://www.amd.com/en/resources/support-articles/faqs/RSX2-INSTALL.html)

To update a driver, select your GPU and open **Settings → Updates → AMD driver
updates**. Check the AMD product page, read AMD’s license, and choose
**Download and check package**. Radeoner verifies AMD’s signature and asks
Windows which display INF matches the selected GPU. If the matched driver is
newer, choose **Install AMD graphics driver**. Windows asks for administrator
permission and uses the verified display files. AMD’s setup program is never
started. Radeoner reports whether a restart is required.
[AMD downloads](https://www.amd.com/en/support/download/drivers.html) ·
[Driver architecture, update options and legacy support](docs/AMD_DRIVERS.md)

## Features

| Area | Capabilities |
| --- | --- |
| GPUs | Select an AMD GPU; integrated/discrete identification from the driver |
| Monitoring | Native parameter/value list, live every second by default, including the tray |
| Tuning | Percentage power limit, GPU/VRAM clocks, voltage and legacy performance states |
| RGB | Color, brightness and off through an optional local OpenRGB SDK server |
| Cooling | Fan curve, Zero RPM, minimum/target RPM when exposed |
| Profiles | JSON export/import with GPU/subsystem, unit and range validation |
| Tray | Alt+F4 hides the window; the icon shows the GPU and actual readings |
| Settings | Polish, English, German, Russian, Spanish, French LNG sources, themes, text 100–200% |
| Windows startup | Optional startup to the tray for the current user |
| Updates | Manual and optional scheduled checks, download and executable replacement |

Starting the app never applies a GPU profile. Import only fills the editor.
**Apply** shows the changes, writes them and verifies driver readback. A failed
write triggers an attempt to restore previous values. Export includes unapplied
editor values; press F5 first when backing up the current driver settings.

## Hardware support and validation limits

**Not every GPU, integrated GPU or laptop exposes tuning.** Radeoner queries
capabilities and hides unavailable fields and empty tuning/fan pages. Supported
integrated-GPU features remain available; diagnostic reasons stay in Report.
It does not unlock firmware or
control a laptop's embedded fan controller. The tested Radeon 880M exposed
sensors but no manual tuning through its installed driver.

The power limit is a percentage relative to the factory setting, **not TDP in
watts** or a shared CPU/iGPU power limit. On Navi4+, maximum clock and voltage
may be offsets from base values. Tuning can affect stability; Radeoner does not
run a stability test.

Accessibility checks cover WinForms/MSAA and actual NVDA speech text captured
from Speech Viewer. This does not claim an audio listening session or coverage
of every screen-reader configuration. Writes have been tested against
in-memory ADLX interfaces; physical tuning on supported GPUs needs further
community testing. See [validation status](docs/VALIDATION.md).

## Updates

The **Settings** tab offers checks at startup, an interval of **5–10080 minutes**
(default 60), **Check now**, and **Download and install**. Automatic checks are
initially enabled and can be switched off. Checking never installs an update
automatically. A new version opens a native Windows dialog containing release
notes. **Yes** downloads and installs; **No** postpones the update.
The full release notes appear in a focused, scrollable, read-only field.

Updates come from this repository's latest stable [GitHub release](https://github.com/proszczok2002/Radeoner/releases).
Radeoner verifies the ZIP and each file using SHA-256, exits, replaces the
files and starts the new version. The old EXE stays in a backup until the new
window confirms successful initialization. It then removes the previous EXE
and temporary helper. If the candidate fails to start, the updater attempts
to restore the previous files. User profiles and `Radeoner.settings.json` are
preserved. Driver packages and GPU tuning settings are not updated or applied.

See [update format, recovery and tests](docs/UPDATING.md) for details.
The complete **1.0 → 1.01 update from public GitHub has been verified**:
[test result and file checksums](docs/UPDATE_TEST.md).
The **1.01 → 1.02** public-feed integration test also passed:
[1.02 test result](docs/UPDATE_TEST_1.02.md).
The **1.02 → 1.03** public GitHub update also passed:
[1.03 test result](docs/UPDATE_TEST_1.03.md).
The **1.03 → 1.04** and **1.04 → 1.05** public updates also passed:
[1.04](docs/UPDATE_TEST_1.04.md), [1.05](docs/UPDATE_TEST_1.05.md).
The **1.07 → 1.08** update passed with the local package and the public GitHub
channel, preserving profiles and preferences. [Test result](docs/UPDATE_TEST_1.08.md)
[NVDA 1.06 audit](docs/AUDIT_1.06.md): 900 UI, 180 dialog and 72 tray checkpoints.

## Keyboard, tray and preferences

| Shortcut | Action |
| --- | --- |
| Tab / Shift+Tab | Fields and buttons |
| Up / Down | Sensor rows |
| Ctrl+Tab / Ctrl+Shift+Tab | Tabs |
| Ctrl+1 … Ctrl+5 | Readings, Tuning, Fan, Report, Settings |
| F5 | Read settings and sensors |
| Ctrl+Enter | Apply edits |
| Ctrl+E / Ctrl+I | Export / import a profile |
| Ctrl+Shift+S | Save a report |
| F6 / F1 | Latest status text / help |
| Alt+F4 | Hide to the tray |

**Exit** in the window or tray menu stops the app completely. Launching the
EXE again shows the existing window. Sensor updates every second are enabled by default and continue in the tray and can keep a laptop GPU awake.

Preferences are stored next to the EXE in `Radeoner.settings.json`. Languages:
`lang/*.lng` sources (UTF-8, `key=value`, `\n` for a new line). All six languages
and guides are embedded in the EXE. A local `lang/<code>.lng` overrides embedded
text. New languages require no extra portable files, preserving compatibility
with older updaters. F1 opens the full translated offline guide.
Ctrl+Tab skips unavailable pages. Ctrl+1…5 keep their logical meaning; an
unavailable page returns to Readings. Integrated GPUs never announce fans in
the tray; other GPUs announce only valid RPM readings, including 0 RPM.
RGB still requires OpenRGB: [native RGB status](docs/NATIVE_RGB.md).
The System theme follows the Windows app light/dark setting. Windows high contrast overrides the chosen theme. After moving the folder,
turn Windows startup off and on again to register the new executable location.

## Build and contribute

```powershell
git clone https://github.com/proszczok2002/Radeoner.git
cd Radeoner
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Test
powershell -NoProfile -ExecutionPolicy Bypass -File .\package.ps1
```

The EXE is written to `portable/`. The ZIP, `update.json` and checksums are
written to `artifacts/releases/<version>/`. The build uses the compiler supplied
with .NET Framework; Visual Studio, `dotnet SDK`, NuGet and the AMD SDK are not
required. Default tests do not need an AMD GPU and never change GPU settings.

[CONTRIBUTING.md](CONTRIBUTING.md) covers source layout, tests, translations and
pull requests. See [CHANGELOG.md](CHANGELOG.md) for releases.

## License and attribution

Application source: [MIT](LICENSE). The AMD SDK and driver binaries are not
redistributed; see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
Radeoner is an independent community project, not an AMD or Microsoft product.

## Author, support and contact

**Settings** contains **About** and **Support and contact**.
Support is optional; link buttons open your browser.

- [Author website](https://proszczok.pl/)
- [PayPal](https://paypal.me/proszczok)
- [Revolut](https://revolut.me/r_rosiejka)
- [Contact on Telegram](https://t.me/proszczok)

Audit scope and screen-reader checklist: [accessibility](docs/ACCESSIBILITY.md).

## Live readings and RGB in 1.03

Readings and tray update every second. The native list preserves selection
and updates values while focused. Ctrl+A selects all rows; Ctrl+C copies them.
Window title and tray include the version, such as **Radeoner v1.03**.
Ctrl+Tab/Ctrl+Shift+Tab work from controls inside and outside tab pages.

ADLX runs in an owned child process using the same EXE. A native helper crash
or timeout leaves the UI open. A single failed read no longer disables polling:
last values are marked stale and reads are retried. `Radeoner.log` stays local.
The helper exits with the app; no service is installed.
[Stability and RX 9060 verification limits](docs/STABILITY.md).

**Tuning → GPU lighting (RGB)** opens standard controls for device, mode,
preset or R/G/B 0–255, and brightness 0–100%. This optional feature requires
**OpenRGB with its local SDK server enabled** (default port 6742). Radeoner
does not install OpenRGB or connect until Connect is selected.
[RGB setup and limitations](docs/RGB.md).
