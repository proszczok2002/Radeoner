# Aktualizacja 1.03 → 1.04 / Update test

2026-11-14: **PASS** lokalnie o 07:44 UTC oraz przez publiczny kanał GitHub
o 07:45 UTC. Opublikowany EXE 1.03 pobrał manifest i ZIP 1.04 bez tokena,
podmienił pliki, uruchomił nowe okno i obsługę ADLX, odebrał potwierdzenie
startu oraz usunął stare EXE i pomocnika. Sumy profilu i preferencji są
identyczne przed i po aktualizacji. Test działał w osobnej kopii ze spacjami
w ścieżce; działająca aplikacja użytkownika i NVDA pozostały uruchomione.
Nie stosowano ustawień fizycznego GPU ani RGB.

The published 1.03 executable completed local and anonymous GitHub-latest
updates to 1.04: **PASS**, 2026-11-14 at 07:44 and 07:45 UTC. Real new-window
and ADLX startup, acknowledgement, old EXE/helper removal and unchanged
preference/profile checksums were verified in an isolated portable copy.
The user's running application and NVDA were left running; no GPU/RGB tuning
was applied.

51 testów / tests passed locally and on clean Windows GitHub runners:
[main](https://github.com/proszczok2002/Radeoner/actions/runs/43572530689),
[tag v1.04](https://github.com/proszczok2002/Radeoner/actions/runs/43572530680).
The production OpenRGB downloader also fetched and verified the official
22,019,605-byte ZIP. No downloaded OpenRGB executable was started.

| Plik / File | SHA-256 |
| --- | --- |
| 1.03 EXE | `7aee2607ce981c7911e030b4222684e97e5a6d25ef6feed9ec36903049e3efee` |
| 1.04 ZIP | `48dbca9e284a7f11b7cd465fdfb7761a37b0302725a5380cc151907cfea77215` |
| 1.04 EXE | `3e1d6d618eb9bbc942fc68f6d2e4b78f2908a9f61deff6f5932663c8a4717da6` |

Raw evidence before housekeeping:
`artifacts/update-e2e/local-20261114-084434/result.json`,
`artifacts/update-e2e/github-20261114-084550/result.json`.
After archiving, use `niepotrzebne/*/INDEKS.csv` to locate these paths.
