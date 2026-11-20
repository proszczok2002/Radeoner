# Porządek w projekcie

Wymaganie użytkownika: aktywny projekt ma pozostać mały i gotowy do dalszej
pracy. Stare wersje, niepotrzebne kopie robocze i zbędne pliki przenoś do
folderu `niepotrzebne/`. Nie usuwaj ich domyślnie.

- Zachowuj źródła, testy, tłumaczenia, dokumentację, konfigurację Gita,
  skrypty oraz lokalny `STAN_PRAC.md`.
- `portable/` to aktualny gotowy program; zachowuj preferencje i profile użytkownika.
- W `artifacts/releases/` zostawiaj tylko bieżące wydanie z `version.json`:
  ZIP, `update.json`, `SHA256SUMS.txt`. Dodatkowe kopie programu są zbędne.
- Stare buildy, klony kontrolne, rozpakowane kopie paczek, wyniki testów,
  pliki generowane i opcjonalne materiały SDK archiwizuj po zakończeniu pracy.
- Służy do tego `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Cleanup.ps1`.
  Skrypt zapisuje indeks przeniesionych ścieżek i nie modyfikuje źródeł.
- Każde wydanie zakończ uruchomieniem `tools/Cleanup.ps1` po ostatnim buildzie
  i testach. Przenieś stare EXE i kopie paczek do `niepotrzebne/`, zachowaj
  aktualny portable z danymi użytkownika i zapisz indeks w `STAN_PRAC.md`.
  Archiwum pozostaw do samodzielnego sprzątania przez użytkownika.
- Po buildzie/testach ponownie uporządkuj pliki robocze. Unikaj tworzenia
  kolejnych pełnych klonów bez potrzeby; używaj istniejących źródeł i folderów wynikowych.
- Przed przenoszeniem katalogów sprawdzaj ich pełne ścieżki. Nie przenoś
  aktywnego `.git`, samego archiwum ani plików używanych przez działający program.
- Aktualizuj lokalny stan prac o ścieżki archiwum. Porządki lokalne nie oznaczają
  usuwania opublikowanych wydań ani historii GitHub.

# Project housekeeping

Keep sources, tests, docs, translations, Git metadata and the current portable
application. Move obsolete local builds, release duplicates, scratch clones,
test output and separately obtained SDK material into `niepotrzebne/` using
`tools/Cleanup.ps1`. Keep only the current release ZIP/manifest/checksums active.
Preserve user preferences/profiles. Archive instead of deleting by default;
verify absolute paths and retain the move index for recovery.
Finish each release with `tools/Cleanup.ps1` after the final build and tests.
Record the archive index in `STAN_PRAC.md`. Leave archive pruning to the user.
