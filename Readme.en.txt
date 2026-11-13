Radeoner 1.0 - lightweight, portable AMD GPU control for Windows

Polish instructions: Czytaj.txt. Full documentation:
https://github.com/proszczok2002/Radeoner

Run Radeoner.exe on Windows 10/11 x64 with .NET Framework 4.8 and an AMD driver
that supplies ADLX. The Adrenalin control panel is not required. Supported tuning
features depend on the GPU, driver and computer firmware; laptop and integrated
GPUs can expose monitoring without any tuning controls.

Features: GPU selection; text sensors; driver-exposed power limits, clocks,
voltage and fans; JSON profiles; tray readings; optional Windows startup;
Polish/English LNG files; light/dark/system theme; text size 100-200%; updates.

Profiles save all available editor values, including unapplied edits. Import
checks GPU/subsystem identity, ranges, units and fan points, then fills the
editor. Only Apply writes settings to the driver. Starting Radeoner or enabling
Windows startup never applies a GPU tuning profile.

Alt+F4 and the close button hide to the tray. Use Exit in the window or tray
menu to stop the app. Sensor updates every three seconds are optional and can
keep a laptop GPU awake. Missing readings are marked rather than estimated.

Updates tab: optional checks at startup and every 5-10080 minutes (default 60).
Check now also works when automatic checks are off. Download and install uses
the latest stable GitHub release, verifies SHA-256 and restarts Radeoner.
The old executable remains in a temporary backup until the new window has
initialized successfully, then the backup and temporary helper are removed.
If startup fails, the updater attempts to restore the previous files.
Preferences and user profiles are preserved. Updates never change AMD drivers
or GPU tuning settings. No account, token or administrator access is needed.

Keyboard: Tab/Shift+Tab - controls; Ctrl+Tab - tabs; F5 - readings;
Ctrl+Enter - Apply; Ctrl+E - export profile; Ctrl+I - import profile;
Ctrl+Shift+S - save report; F6 - latest status text; F1 - help.
Text readings can be selected and copied using Ctrl+A, Ctrl+C.

Preferences are saved next to the EXE in Radeoner.settings.json. Translations
are in lang/pl.lng and lang/en.lng (UTF-8, key=value, \n for a new line).
High contrast overrides the selected theme. Moving the portable folder requires
turning Windows startup off and on again to register the new EXE location.

Validation limitations: automated WinForms/MSAA checks do not replace an NVDA
listening session. Physical tuning writes have not been tested on every GPU.
An AMD Radeon 880M with the tested driver exposes sensors but no manual tuning.
The power limit is a percentage, not shared CPU/iGPU TDP in watts. On Navi4+
the maximum clock and voltage can be offsets from the base values.

Source and collaboration: https://github.com/proszczok2002/Radeoner
License: MIT for the application source. See THIRD_PARTY_NOTICES.md.
