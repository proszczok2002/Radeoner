# Współpraca / Contributing

## Polski

Zapraszam do współpracy nad Radeonerem: kodem, dostępnością, tłumaczeniami
i sprawdzaniem obsługi różnych kart AMD. Załóż [issue](https://github.com/proszczok2002/Radeoner/issues)
z opisem problemu lub propozycji. Do kodu użyj forka, osobnej gałęzi i pull requesta.

### Kompilacja

Windows 10/11 x64 i .NET Framework 4.8 wystarczą. Sklonuj repozytorium i uruchom:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Test
powershell -NoProfile -ExecutionPolicy Bypass -File .\package.ps1
```

Nie trzeba pobierać SDK AMD, instalować NuGet ani Visual Studio. `src/Slots.cs`
zawiera używane identyfikatory ABI. Opcjonalny skrypt `tools/Generate-AdlxSlots.ps1`
pozwala porównać je z lokalnie i oddzielnie pobranym SDK AMD. Tego SDK nie dodawaj
do repozytorium ani paczki.

### Testy

Domyślne testy używają symulowanych natywnych interfejsów ADLX. Obejmują
walidację, zapis i cofanie, profile, kontrolki, tłumaczenia, autostart w osobnym
kluczu testowym oraz updater z rzeczywistym procesem odrzucającym kandydata.
Nie zmieniają parametrów GPU. CI wykonuje ten zestaw na Windows.

Testy rzeczywistego sprzętu są opcjonalne i wyłącznie odczytowe:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\Smoke.ps1 -LiveRead -LiveTray -LiveDialogs
```

Przejście z NVDA: nazwa karty, Tab/Shift+Tab, Ctrl+Tab, nazwy/jednostki pól,
F5, F6, kopiowanie raportu, Alt+F4, menu traya, Ustawienia, O programie, Wsparcie i kontakt.
Zapisz wersję NVDA oraz konkretne zachowanie. Sam odczyt etykiet w MSAA nie
jest potwierdzeniem odsłuchu czytnika ekranu.

### Zmiany w kodzie

- Zachowuj standardowe kontrolki i pełną obsługę klawiatury.
- Dodawaj teksty do obu LNG i zachowuj zgodne klucze oraz argumenty `{0}`.
- Nie zgaduj możliwości po nazwie GPU. Korzystaj z zapytań sterownika.
- Nie stosuj profili ani zmian GPU przy starcie aplikacji lub imporcie.
- Nie zmieniaj jednostek ani interpretacji napięcia/przesunięć bez testów.
- Przy aktualizacjach zachowuj preferencje i profile oraz sprawdzanie sum plików.
- Opisz problem, zmianę i wykonane testy w pull requeście.

### Struktura

| Pliki | Odpowiedzialność |
| --- | --- |
| `Native.cs`, `Slots.cs`, `AmdBackend.cs` | ADLX, marshaling, referencje i możliwości |
| `Model.cs`, `Transaction.cs` | Walidacja, zapis, odczyt po zapisie i cofanie |
| `Profiles.cs`, `Persistence.cs` | JSON, preferencje i autostart |
| `MainForm.cs`, `Appearance.cs`, `TrayIcon.cs` | Interfejs, motywy i tray |
| `Localization.cs`, `lang/` | Polski/English, teksty i zapasowe zasoby |
| `UpdateFeed.cs`, `UpdateInstall.cs`, `UpdateUI.cs` | Pobieranie, podmiana i obsługa aktualizacji |
| `version.json`, `build.ps1`, `package.ps1` | Wersja, kompilacja i paczka z manifestem |
| `tests/` | Testy bez sprzętu i opcjonalne testy integracyjne |

## English

Contributions to code, accessibility, translations and hardware validation are
welcome. Start an [issue](https://github.com/proszczok2002/Radeoner/issues) for a
problem or proposal. Fork the repository, create a focused branch and submit a PR.

Build on Windows 10/11 x64 with .NET Framework 4.8 using the commands above.
The AMD SDK, NuGet and Visual Studio are not required. Do not commit separately
downloaded SDK contents or driver binaries.

Default tests use in-memory native ADLX interfaces. They cover tuning validation,
readback/rollback, JSON profiles, accessible controls, languages, an isolated
startup registry key, and updater file/process handling. They never tune a real
GPU. The optional `-LiveRead -LiveTray -LiveDialogs` checks only read hardware.

For NVDA testing, record its version and actual speech/navigation behavior for
GPU selection, fields/units, tabs, reports, tray restoration, personalization
and updates. A passing MSAA label test does not establish real screen-reader
listening results.

Preserve keyboard access and standard controls. Update both LNG files and
their format placeholders. Query driver capabilities rather than matching GPU
names. Never apply tuning on startup or profile import. Preserve update
checksums, rollback, preferences and profiles. PRs should explain the problem,
resulting behavior and validation performed.

By contributing, you agree to license your contribution under this project's
[MIT license](LICENSE).
