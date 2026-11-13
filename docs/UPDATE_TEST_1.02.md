# Test aktualizacji 1.01 → 1.02 / Update test

## Polski

Lokalny test pełnej podmiany: **PASS**, 2026-11-13, 21:22 UTC.
Punkt startowy: EXE z opublikowanego ZIP-u 1.01. Kandydat: paczka 1.02.
Rzeczywista wersja pliku zmieniła się z 1.0.1.0 na 1.0.2.0. Nowe okno
WinForms potwierdziło start, po czym updater usunął stary EXE i pomocnika.
Kontrolny profil i preferencje zachowały sumy. Nie zapisano parametrów GPU.

Test działał w osobnym folderze ze spacjami. Tryb diagnostyczny 1.02 używa
osobnego mutexu tylko dla zweryfikowanego planu SmokeExit, dzięki czemu
nie trzeba zamykać normalnie działającego Radeonera. Zwykłe uruchomienie
nadal używa jednej instancji.

Test publicznego kanału zostanie dopisany po publikacji wydania.

## English

Local full-update integration test: **PASS**, 2026-11-13, 21:22 UTC.
The published 1.01 EXE updated to the 1.02 package. The real file version
changed from 1.0.1.0 to 1.0.2.0. A new WinForms window confirmed startup,
then the old EXE and temporary helper were removed. Preference and profile
checksums remained unchanged; no GPU settings were written.

The test used an isolated folder containing spaces. A verified SmokeExit plan
gets an isolated mutex in 1.02, allowing diagnostics alongside the user's
running app. Ordinary startup retains single-instance behavior.

Public-feed evidence will be added after publication.

## Sumy / Checksums

| Plik / File | SHA-256 |
| --- | --- |
| 1.01 EXE | `74b44f64f870eff672112795f9be8b9f34426f1656bde68cbf75112960eae1d4` |
| 1.02 ZIP | `e01095ff0ed8e12ccdf136941dec19c454febc97cc35341f5d4ad3fcb57c525a` |
| 1.02 EXE | `bc67d81630a187736d1479608685a1fcae0f6ecd071917c35afaa16a59c29cfe` |

Local raw evidence before housekeeping:
`artifacts/update-e2e/local-20261113-222216/result.json`.
After housekeeping use the corresponding `niepotrzebne/*/INDEKS.csv`.
Public 1.0 → 1.01 evidence remains in [UPDATE_TEST.md](UPDATE_TEST.md).
