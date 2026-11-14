# Aktualizacja 1.05 → 1.06 / Update test

**Lokalny kandydat: PASS**, 2026-11-14T15:24:08.4908069Z.
Publiczny kanał GitHub nie został jeszcze przetestowany dla 1.06; stabilne
publikacja 1.06 nastąpi po zakończonym [audycie NVDA](AUDIT_1.06.md).

Opublikowany EXE 1.05 przyjął przygotowany ośmioplikowy manifest i ZIP 1.06,
podmienił pliki, uruchomił rzeczywiste nowe okno i obsługę ADLX oraz potwierdził
inicjalizację. Stary EXE i pomocnik zostały usunięte; preferencje i profil
zachowały sumy. Użyto izolowanego folderu ze spacjami. Nie stosowano ustawień
GPU ani podświetlenia i nie nadpisano użytkowej kopii programu.

**Local candidate: PASS**. The published 1.05 EXE accepted the eight-file
1.06 manifest/ZIP and completed real replacement, new WinForms/ADLX startup,
acknowledgement and old EXE/helper cleanup. Preference/profile hashes remained
unchanged in an isolated path containing spaces. Public GitHub-latest E2E is
pending publication; actual NVDA validation has passed.

| File | SHA-256 |
| --- | --- |
| 1.05 EXE | `475a90c9c51216a98762209d53cb86c5b38c0ae9d0ff4542aea626da4b399710` |
| 1.06 candidate ZIP | `f88edf54ab391a2199fbc164f9732bff245394f505eb9969fb1b5bc228226e6c` |
| 1.06 candidate EXE | `b72f5623bc93f3f828a0e164cc56069caa31ca4b00f8767bb6f07ff1290b3ceb` |

Evidence before housekeeping:
`artifacts/update-e2e/local-20261114-162403/result.json`.
After housekeeping use `niepotrzebne/*/INDEKS.csv` to locate the same path.
