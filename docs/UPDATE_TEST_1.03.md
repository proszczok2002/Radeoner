# Test aktualizacji 1.02 → 1.03 / Update test

## Polski

Lokalny test pełnego mechanizmu: **PASS**, 2026-11-13, 23:08 UTC.
Opublikowany EXE 1.02 zaktualizował się do paczki 1.03. Potwierdzono:

- zmianę wersji pliku 1.0.2.0 → 1.0.3.0 i zgodność końcowej sumy;
- start rzeczywistego nowego okna oraz jego procesu obsługi ADLX;
- potwierdzenie inicjalizacji, usunięcie starego EXE i pomocnika aktualizacji;
- identyczne sumy kontrolnego profilu i preferencji;
- brak zmian parametrów fizycznego GPU i RGB.

Test działał w osobnej kopii portable ze spacjami w ścieżce. Działająca kopia
użytkownika i NVDA pozostały uruchomione.

Publiczny kanał GitHub: **PASS**, 2026-11-13, 23:11 UTC. Opublikowany EXE 1.02
bez konta/tokena pobrał manifest i ZIP 1.03 z latest. Przeszły podmiana,
inicjalizacja okna i ADLX, usunięcie starego EXE/pomocnika oraz kontrola sum
profilu i preferencji. Końcowy EXE ma sumę podaną w tabeli.
Kompilacja i wszystkie 50 testów przeszły też na czystym runnerze GitHub Actions,
łącznie z testem komunikacji z terminala UTF-8.

## English

Local full-update test: **PASS**, 2026-11-13, 23:08 UTC. The published 1.02 EXE
updated to 1.03 (1.0.2.0 → 1.0.3.0). The final EXE matched its checksum;
the actual new window and ADLX child initialized; startup was acknowledged,
old EXE/update helper were removed, and preference/profile hashes stayed equal.
No physical GPU tuning or RGB changes were made. The test used an isolated
portable copy with spaces; the user's running app and NVDA were left running.
Public GitHub-feed test: **PASS**, 2026-11-13, 23:11 UTC. The published 1.02
EXE downloaded 1.03 from the anonymous latest-release feed. Replacement, real
window/ADLX startup, old EXE/helper cleanup and unchanged preference/profile
checksums passed. The final EXE matches the table. Build and all 50 tests also
passed on a clean GitHub Actions runner, including the UTF-8 console regression.

## SHA-256

| Plik / File | SHA-256 |
| --- | --- |
| 1.02 EXE | `bc67d81630a187736d1479608685a1fcae0f6ecd071917c35afaa16a59c29cfe` |
| 1.03 ZIP | `721410567b146944e442506dd309055d3ce8f5ff250a3140d5d6bb8f42fbbd53` |
| 1.03 EXE | `7aee2607ce981c7911e030b4222684e97e5a6d25ef6feed9ec36903049e3efee` |

Raw local evidence before housekeeping:
`artifacts/update-e2e/local-20261114-000850/result.json`.
Public-feed evidence:
`artifacts/update-e2e/github-20261114-001114/result.json`.
After housekeeping use the corresponding `niepotrzebne/*/INDEKS.csv`.
