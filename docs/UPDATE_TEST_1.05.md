# Aktualizacja 1.04 → 1.05 / Update test

2026-11-14: **PASS** lokalnie o 10:40 UTC oraz przez publiczny kanał GitHub
o 10:46 UTC. Opublikowany EXE 1.04 bez tokena pobrał manifest i ZIP 1.05,
podmienił pliki, uruchomił rzeczywiste nowe okno i obsługę ADLX, odebrał
potwierdzenie inicjalizacji oraz usunął stare EXE i pomocnika. Sumy profilu
i preferencji pozostały identyczne. Użyto osobnej kopii portable ze spacjami
w ścieżce. Kopia użytkownika i NVDA pozostały uruchomione. Nie stosowano
parametrów fizycznego GPU ani RGB.

The published 1.04 EXE completed local and anonymous GitHub-latest updates to
1.05: **PASS**, 2026-11-14 at 10:40 and 10:46 UTC. Real new-window/ADLX startup,
acknowledgement, removal of the old EXE/helper and unchanged preference/profile
hashes were verified in an isolated portable copy. The user's app and NVDA
were left running; no physical GPU/RGB tuning was applied.

57 regression checks passed locally and on clean Windows runners:
[main](https://github.com/proszczok2002/Radeoner/actions/runs/43586693615),
[tag v1.05](https://github.com/proszczok2002/Radeoner/actions/runs/43586697392).
The [separate actual NVDA audit](AUDIT_1.05.md) captured 540 UI and 60 native-dialog
checkpoints, numeric keys and the actual Shell tray icon/menu.

| Plik / File | SHA-256 |
| --- | --- |
| 1.04 EXE | `3e1d6d618eb9bbc942fc68f6d2e4b78f2908a9f61deff6f5932663c8a4717da6` |
| 1.05 ZIP | `13cf3f6ebc8bcae127cad10cd9018d0465a5a28be672640665ab9648dd191264` |
| 1.05 EXE | `475a90c9c51216a98762209d53cb86c5b38c0ae9d0ff4542aea626da4b399710` |

Raw evidence before housekeeping:
`artifacts/update-e2e/local-20261114-114047/result.json`,
`artifacts/update-e2e/github-20261114-114637/result.json`.
After archiving, locate these paths through `niepotrzebne/*/INDEKS.csv`.
