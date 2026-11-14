# Aktualizacja 1.05 → 1.06 / Update test

**Lokalny kandydat: PASS**, 2026-11-14T14:52:36.2002508Z.
Publiczny kanał GitHub nie został jeszcze przetestowany dla 1.06; stabilne
latest pozostaje 1.05 do ukończenia [audytu NVDA](AUDIT_1.06.md).

Opublikowany EXE 1.05 przyjął przygotowany ośmioplikowy manifest i ZIP 1.06,
podmienił pliki, uruchomił rzeczywiste nowe okno i obsługę ADLX oraz potwierdził
inicjalizację. Stary EXE i pomocnik zostały usunięte; preferencje i profil
zachowały sumy. Użyto izolowanego folderu ze spacjami. Nie stosowano ustawień
GPU ani podświetlenia i nie nadpisano użytkowej kopii programu.

**Local candidate: PASS**. The published 1.05 EXE accepted the eight-file
1.06 manifest/ZIP and completed real replacement, new WinForms/ADLX startup,
acknowledgement and old EXE/helper cleanup. Preference/profile hashes remained
unchanged in an isolated path containing spaces. Public GitHub-latest E2E is
pending release after actual NVDA validation; stable latest remains 1.05.

| File | SHA-256 |
| --- | --- |
| 1.05 EXE | `475a90c9c51216a98762209d53cb86c5b38c0ae9d0ff4542aea626da4b399710` |
| 1.06 candidate ZIP | `69765fb2af890a86d490fccd0514668e3b1eadea873eddb4aa0bb218c3ab9030` |
| 1.06 candidate EXE | `6454a789300d1a2b35de6f091bee35a33648871c2ed89f509d70b943a4d7c50b` |

Evidence before housekeeping:
`artifacts/update-e2e/local-20261114-155232/result.json`.
After housekeeping use `niepotrzebne/*/INDEKS.csv` to locate the same path.
