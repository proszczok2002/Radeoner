# Test aktualizacji 1.02 → 1.03 / Update test

## Polski

Lokalny test pełnego mechanizmu: **PASS**, 2026-11-13, 22:58 UTC.
Opublikowany EXE 1.02 zaktualizował się do paczki 1.03. Potwierdzono:

- zmianę wersji pliku 1.0.2.0 → 1.0.3.0 i zgodność końcowej sumy;
- start rzeczywistego nowego okna oraz jego procesu obsługi ADLX;
- potwierdzenie inicjalizacji, usunięcie starego EXE i pomocnika aktualizacji;
- identyczne sumy kontrolnego profilu i preferencji;
- brak zmian parametrów fizycznego GPU i RGB.

Test działał w osobnej kopii portable ze spacjami w ścieżce. Działająca kopia
użytkownika i NVDA pozostały uruchomione. Test publiczny zostanie dopisany po publikacji.

## English

Local full-update test: **PASS**, 2026-11-13, 22:58 UTC. The published 1.02 EXE
updated to 1.03 (1.0.2.0 → 1.0.3.0). The final EXE matched its checksum;
the actual new window and ADLX child initialized; startup was acknowledged,
old EXE/update helper were removed, and preference/profile hashes stayed equal.
No physical GPU tuning or RGB changes were made. The test used an isolated
portable copy with spaces; the user's running app and NVDA were left running.
Public-feed evidence will be added after publication.

## SHA-256

| Plik / File | SHA-256 |
| --- | --- |
| 1.02 EXE | `bc67d81630a187736d1479608685a1fcae0f6ecd071917c35afaa16a59c29cfe` |
| 1.03 ZIP | `0c97c13d78600dbcf95a99c58ec86652687804836a9dbaee1de66d160cecafcb` |
| 1.03 EXE | `643c2caf6af035b42993d1ec9318f8df62d69525577850cd745e2ce27f91d5a0` |

Raw local evidence before housekeeping:
`artifacts/update-e2e/local-20261113-235842/result.json`.
After housekeeping use the corresponding `niepotrzebne/*/INDEKS.csv`.
