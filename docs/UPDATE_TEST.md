# Test aktualizacji 1.0 → 1.01 / Update test

**Wynik: PASS / Result: PASS** — 2026-11-13, 20:48 UTC.

## Polski

Uruchomiono osobną kopię **opublikowanego EXE 1.0** z oficjalnego ZIP-u.
Program odpytał publiczny kanał `releases/latest` bez konta/tokena, pobrał
manifest i ZIP **1.01** z GitHub Releases i wykonał ten sam mechanizm podmiany,
którego używa przycisk Pobierz i zainstaluj.

Potwierdzono:

- wersja pliku zmieniła się z `1.0.0.0` na `1.0.1.0` (widoczne 1.0 → 1.01);
- końcowy EXE ma sumę SHA-256 z publicznego manifestu;
- nowy proces uruchomił rzeczywiste okno WinForms i potwierdził inicjalizację;
- poprzedni EXE, kopia zapasowa i tymczasowy pomocnik zostały usunięte;
- kontrolny profil i preferencje zachowały dokładnie te same sumy;
- test nie stosował profili ani nie zmieniał ustawień GPU.

Test odbył się w odizolowanym folderze ze spacjami w nazwie. Nowa aplikacja
zakończyła się po potwierdzeniu/sprzątaniu w jawnym trybie diagnostycznym.
Wcześniejszy lokalny test i test automatyczny wycofania po nieudanym starcie
również przeszły. Pierwsze zapytanie bezpośrednio po publikacji nadal zwróciło
poprzednie wydanie z pamięci podręcznej GitHuba; program niczego wtedy nie podmieniał.
Test publiczny powtórzono po pojawieniu się 1.01 w kanale anonimowym.

## English

An isolated copy of the **published 1.0 executable** queried the public
latest-release API without an account/token, downloaded the **1.01** manifest
and ZIP from GitHub Releases and used the same installer as Download and install.

The file version changed from 1.0.0.0 to 1.0.1.0; the final executable matched
the public manifest's SHA-256. A real new WinForms window initialized and
acknowledged readiness. The old executable, backup and temporary helper were
removed. Test preference/profile hashes remained unchanged. No GPU settings
or profiles were applied.

The test used a separate directory containing spaces. The new app exited
after readiness and cleanup in explicit diagnostic mode. Local handoff and
failed-start rollback tests had also passed. An initial request immediately
after publication still received GitHub's cached previous release and made no
changes; the public test passed once the anonymous feed exposed 1.01.

## Sumy / Checksums

| Plik / File | SHA-256 |
| --- | --- |
| 1.0 ZIP | `93ee5becdcebb271cf13b11640842270d5c0498c2c487825d96e70bf0c73b292` |
| 1.01 ZIP | `a46bad1f7a2d53c61210374aa0b7beea15e1af251ecdaa085fbe38a6ae38d992` |
| 1.0 EXE | `45900468ddce630b9ec9d38b6416cfc5dfa7cb244cc26a827aa915123e70fd0a` |
| 1.01 EXE | `74b44f64f870eff672112795f9be8b9f34426f1656bde68cbf75112960eae1d4` |

[1.0 release](https://github.com/proszczok2002/Radeoner/releases/tag/v1.0) ·
[1.01 release](https://github.com/proszczok2002/Radeoner/releases/tag/v1.01) ·
[Test script](../tests/Update-E2E.ps1)
