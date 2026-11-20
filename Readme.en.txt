Radeoner 1.06 - lightweight, portable AMD GPU control for Windows

1.06 adds German, Russian, Spanish and French with translated offline guides.
Unavailable tuning and fan pages are omitted according to driver capabilities.
Ctrl+Tab skips these pages. Integrated GPUs never announce fans in the tray;
other GPUs announce RPM only when a valid reading exists, including 0 RPM.

Polish instructions: Czytaj.txt. Full documentation:
https://github.com/proszczok2002/Radeoner

Run Radeoner.exe on Windows 10/11 x64 with .NET Framework 4.8 and an AMD driver
that supplies ADLX. The Adrenalin control panel is not required. Supported tuning
features depend on the GPU, driver and computer firmware; laptop and integrated
GPUs can expose monitoring without any tuning controls.

Features: GPU selection; text sensors; driver-exposed power limits, clocks,
voltage and fans; JSON profiles; tray readings; optional Windows startup;
Polish/English/German/Russian/Spanish/French LNG files; light/dark/system theme;
text size 100-200%; updates.

Profiles save all available editor values, including unapplied edits. Import
checks GPU/subsystem identity, ranges, units and fan points, then fills the
editor. Only Apply writes settings to the driver. Starting Radeoner or enabling
Windows startup never applies a GPU tuning profile.

Alt+F4 and the close button hide to the tray. Use Exit in the window or tray
menu to stop the app. Sensor updates every second are enabled by default and can
keep a laptop GPU awake. Missing readings are marked rather than estimated.

Settings tab: optional checks at startup and every 5-10080 minutes (default 60).
Check now also works when automatic checks are off. Download and install uses
the latest stable GitHub release, verifies SHA-256 and restarts Radeoner.
A new version opens a native Windows prompt containing release notes.
Yes installs; No postpones. All changes opens the full, copyable text.
The old executable remains in a temporary backup until the new window has
initialized successfully, then the backup and temporary helper are removed.
If startup fails, the updater attempts to restore the previous files.
Preferences and user profiles are preserved. Updates never change AMD drivers
or GPU tuning settings. No account, token or administrator access is needed.

Keyboard: Tab/Shift+Tab - controls; Ctrl+Tab - tabs; F5 - readings;
Ctrl+Enter - Apply; Ctrl+E - export profile; Ctrl+I - import profile;
Ctrl+Shift+S - save report; F6 - latest status text; F1 - help.
Text readings can be selected and copied using Ctrl+A, Ctrl+C.

Preferences are saved next to the EXE in Radeoner.settings.json. Six translations
are embedded in the EXE and their UTF-8 sources are in lang/*.lng on GitHub
(key=value, \n for a new line). A local lang/<code>.lng can override a language.
The portable ZIP retains its existing eight files for compatibility with older
updaters. The four new languages need no separate files. Full translated guides
are embedded in the EXE and available offline through F1.
System theme follows the Windows light/dark app setting. High contrast overrides the selected theme. Moving the portable folder requires
turning Windows startup off and on again to register the new EXE location.

Validation limitations: automated WinForms/MSAA checks do not replace an NVDA
listening session. Physical tuning writes have not been tested on every GPU.
An AMD Radeon 880M with the tested driver exposes sensors but no manual tuning.
The power limit is a percentage, not shared CPU/iGPU TDP in watts. On Navi4+
the maximum clock and voltage can be offsets from the base values.

Source and collaboration: https://github.com/proszczok2002/Radeoner
License: MIT for the application source. See THIRD_PARTY_NOTICES.md.

Ctrl+1 to Ctrl+5: Readings, Tuning, Fan, Report, Settings.
Ctrl+Tab/Ctrl+Shift+Tab cycle through available pages. If the selected GPU does
not expose tuning or fan controls, the corresponding empty pages are omitted.
Ctrl+1...5 keep their logical meaning; an unavailable page returns to Readings.
Integrated GPUs retain every tuning feature the driver actually exposes.
Dedicated GPUs also retain access to the optional OpenRGB dialog. RGB still
requires OpenRGB in this version; direct controller support is not implemented.
About and Support and contact are in Settings. Support is optional.
Author: https://proszczok.pl/
PayPal: https://paypal.me/proszczok
Revolut: https://revolut.me/r_rosiejka
Telegram: https://t.me/proszczok

1.03: LIVE READINGS, STABILITY, KEYBOARD AND RGB
Readings use a native parameter/value list: Up/Down, Ctrl+A, Ctrl+C.
Values update every second while focused and in the tray. Title and icon
include the version. Ctrl+Tab/Ctrl+Shift+Tab work from every main-window control.
Settings use groups, short descriptions and scrolling to the focused control.

ADLX runs in an owned child of the same EXE, without installing a service.
Helper crashes/timeouts leave the UI open; failed reads show stale values and
retry. Radeoner.log stays local. After a write failure check current settings
with F5; failed writes are never replayed automatically. The reported RX 9060
load crash has not been reproduced on physical RX 9060 hardware locally.

Tuning -> GPU lighting (RGB): start OpenRGB and enable its SDK server on
127.0.0.1, usually port 6742; Connect, select a GPU, mode and color, Apply RGB.
Turn off or 0% disables lighting. Brightness uses hardware capability when
reported, otherwise selected RGB channels are dimmed. Only GPU devices exposed
by OpenRGB appear. Support depends on its version. Physical TUF RX 9070 XT RGB
was not tested locally. OpenRGB is optional and not bundled/installed.
Since 1.04, Download OpenRGB saves the official OpenRGB 1.1 Windows 64-bit
portable ZIP to your chosen location, with SHA-256 checks, progress and cancel.
Extract the ZIP, run OpenRGB.exe, enable its SDK server, then Connect in Radeoner.
A failed/cancelled download preserves any existing destination file.
Radeoner JSON profiles currently store ADLX tuning, not RGB settings.

AMD DRIVER UPDATES
Open Settings → Updates → AMD driver updates for the selected GPU.
Check Windows Update, choose an offered driver and read the license if needed.
Choose Install driver and confirm. Windows asks for administrator permission.
Radeoner reports the result and whether a restart is needed. Windows Update
can offer a different version than the AMD website, or no driver at all.
