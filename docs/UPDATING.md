# Aktualizacje / Updates

## Polski

W Ustawieniach można włączyć lub wyłączyć sprawdzanie, wybrać sprawdzanie
przy starcie i odstęp 5–10080 minut. Ustawienia są pamiętane w
`Radeoner.settings.json`. `Sprawdź teraz` działa także przy wyłączonym harmonogramie.
Nowa wersja pokazuje systemowe okno z opisem zmian z treści wydania GitHub.
Tak pobiera i instaluje, Nie odkłada; Wszystkie zmiany otwiera pełny tekst.
Odmowa nie jest ponawiana automatycznie dla tej samej wersji w bieżącej sesji.
Sprawdź teraz lub Pobierz i zainstaluj pozwala wrócić do propozycji. Przed restartem
wyeksportuj niezastosowane pola, jeżeli chcesz je zachować.

### Źródło i przebieg

1. Aplikacja odpytuje `https://api.github.com/repos/proszczok2002/Radeoner/releases/latest`.
2. Przyjmuje stabilne wydanie z `update.json` i wskazanym ZIP-em w tym samym repozytorium/tagu.
3. Sprawdza wersję, rozmiar, sumę SHA-256 ZIP-u, listę plików i sumę każdego pliku.
4. Rozpakowuje wyłącznie osiem znanych plików aplikacji do własnego ukrytego katalogu roboczego.
5. Uruchamia tymczasową kopię bieżącego EXE jako pomocnika i kończy starą instancję.
6. Pomocnik czeka na koniec procesu, zachowuje stare pliki i podmienia pliki aplikacji.
7. Nowy EXE inicjalizuje rzeczywiste okno WinForms i obsługę karty, po czym potwierdza start.
8. Dopiero wtedy usuwana jest kopia starego EXE. Nowa instancja sprząta pomocnika po jego zakończeniu.

Jeżeli nowy proces nie potwierdzi startu w ciągu 60 sekund, pomocnik próbuje
przywrócić stare pliki. Instalacja nie rusza preferencji, profili, autostartu,
sterowników ani ustawień GPU. Kontrole sum chronią przed uszkodzeniem plików;
zaufanie do publikacji opiera się na HTTPS i uprawnieniach do repozytorium GitHub.
Binaria nie mają obecnie podpisu Authenticode.

### Wersje i tworzenie wydania

Źródłem wersji jest `version.json`. Widoczne **1.0** ma numer wewnętrzny
`1.0.0.0`, **1.01** — `1.0.1.0`, **1.02** — `1.0.2.0`, a **1.03** — `1.0.3.0`.
Porównywana jest wersja wewnętrzna, nie liczba dziesiętna. Każde następne
wydanie musi mieć większy numer wewnętrzny.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Test
powershell -NoProfile -ExecutionPolicy Bypass -File .\package.ps1
```

W `artifacts/releases/<wersja>/` powstają ZIP, `update.json` i `SHA256SUMS.txt`.
Opublikuj wszystkie trzy jako zasoby wydania z tagiem `v<wersja>`.
Repozytorium źródłowe w tagu powinno odpowiadać zbudowanej wersji. Oznacz
wydanie jako najnowsze stabilne, jeżeli ma być proponowane przez updater.

Manifest ma pola `Format=Radeoner.Update`, `Schema=1`, `Version`, `DisplayVersion`,
`Asset`, `Sha256`, `Size` oraz `Files` z `Path`, `Sha256` i `Size`. Preferencje
i profile nigdy nie należą do listy plików aktualizacji. Niedozwolone ścieżki,
zduplikowane pliki, dowiązania i rozbieżne sumy są odrzucane.

### Pełny test aktualizacji

Nie uruchamiaj go na jedynej kopii swojego programu. Skrypt sam tworzy osobny
folder ze spacją w nazwie i kontrolne preferencje/profile. Potrzebuje paczki
starej wersji i manifestu nowej. Test uruchamia rzeczywisty nowy interfejs,
sprawdza potwierdzenie i sprzątanie; nie zapisuje ustawień GPU.

Zmień `OldArchive` na rzeczywistą ścieżkę ZIP-u 1.01. Stare wydania po
porządkach są w `niepotrzebne/` (indeks wskazuje ścieżkę); paczkę można też
pobrać z [wydania 1.01](https://github.com/proszczok2002/Radeoner/releases/tag/v1.01).

```powershell
# Pobranie przez prawdziwy publiczny kanał GitHub:
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\Update-E2E.ps1 `
  -OldArchive .\artifacts\releases\1.01\Radeoner-1.01-portable.zip `
  -ExpectedManifest .\artifacts\releases\1.02\update.json -FromGitHub

# To samo podmienianie z lokalnych plików, bez pobierania:
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\Update-E2E.ps1 `
  -OldArchive .\artifacts\releases\1.01\Radeoner-1.01-portable.zip `
  -ExpectedManifest .\artifacts\releases\1.02\update.json `
  -NewArchive .\artifacts\releases\1.02\Radeoner-1.02-portable.zip
```

Opcja diagnostyczna `Radeoner.exe --test-update` wykonuje rzeczywisty update
z GitHuba i kończy nową instancję po potwierdzeniu i sprzątaniu. Wariant
`--test-update <manifest> <zip>` służy do lokalnych testów. To jawnie wywoływane
polecenia deweloperskie, nie zachowanie normalnego uruchomienia.

### Odzyskiwanie

Przy zwykłym błędzie działa przywracanie poprzedniej wersji. Po awarii zasilania,
blokadzie antywirusa lub braku uprawnień może pozostać ukryty katalog
`.radeoner-update-<identyfikator>`. `backup/` zawiera zachowane poprzednie pliki,
a `error.txt` opis błędu, jeśli został zapisany. Zamknij Radeoner i zachowaj
preferencje/profile. Możesz ponownie rozpakować oficjalną paczkę do tego folderu
bez usuwania swoich plików JSON. Nie kasuj jedynej kopii zapasowej przed
potwierdzeniem działania EXE. Dowiązania w ścieżce plików aplikacji nie są obsługiwane.

## English

The Settings tab controls automatic checks, startup checks and a 5–10080 minute
interval. Preferences persist next to the EXE. Check now works independently.
Checking never installs automatically. A new version opens a native Windows
prompt with GitHub release notes: Yes downloads and installs; No postpones.
All changes opens the full readable/copyable text. An automatic offer appears
once per version per session; manual checks can reopen it.
Export unapplied edits before restarting if you want to retain them.

The updater reads this repository's public latest-release API, accepts only a
stable release and its own `update.json`/ZIP assets, and verifies versions,
lengths and SHA-256 for the archive and every allowed file. HTTPS redirects
are restricted to GitHub's download hosts. Checksums detect corruption;
publication trust rests on HTTPS and repository access. Binaries are not
currently Authenticode signed.

Eight known application files are staged under a unique hidden directory.
A temporary copy of the old EXE waits for the old process to exit and replaces
the application files, keeping backups. The new executable initializes its
actual WinForms UI and GPU handling before acknowledging startup. Only then
is the old EXE deleted. The new process removes the helper after it exits.
If no confirmation arrives within 60 seconds, the helper attempts rollback.
Preferences, profiles, startup registration, drivers and GPU tuning are untouched.

`version.json` separates display versions from internal versions: 1.0 maps to
1.0.0.0, 1.01 maps to 1.0.1.0. Update comparisons use the latter. Build/package
with the commands above and attach ZIP, update.json and SHA256SUMS.txt to tag
`v<display version>`. Mark the intended stable release as latest.

The integration script above creates an isolated portable copy, tests the real
replacement/startup/cleanup path and checks preserved preference/profile hashes.
Set OldArchive to the actual 1.01 ZIP path: old local releases are archived
under niepotrzebne/ after housekeeping, or can be downloaded from Releases.
`-FromGitHub` downloads through the same public production feed as the app.
The explicit developer command `--test-update` is not used during ordinary startup.

After power loss or a persistent file lock, a hidden `.radeoner-update-*`
directory may remain with `backup/` and an error log. Close Radeoner, preserve
your JSON files and re-extract an official release if necessary. Keep the backup
until the restored executable works. Application-file links are unsupported.
