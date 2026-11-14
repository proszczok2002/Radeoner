# Zmiany / Changelog

## 1.05 — wersja testowa / audit candidate, not released

### Polski

- Przewijany układ głównego okna zachowuje miejsce na zakładkę przy dużym
  tekście; pola regulacji i wentylatora mają wyrównane wiersze i zawijane opisy.
- Poprawiono wysokości natywnych list wyboru, przewijanie do całego pola
  z fokusem, motyw pól/list/przycisków i wyróżnienie aktywnej zakładki.
- Pomoc i pełny opis przejmują motyw i wielkość tekstu. RGB ma przewijaną
  treść, krótsze przyciski oraz pełne nazwy dostępne dla czytnika.
- Wyjaśnienia niedostępnych funkcji są osiągalne klawiszem Tab; błędne dane
  kierują fokus na pole do poprawy. Osłony przy zamykaniu RGB i zmianie motywu.
- Rozszerzono testy układu PL/EN i motywów oraz dodano osobny test rzeczywistych
  wypowiedzi NVDA. **Nowe zmiany graficzne czekają na weryfikację NVDA**;
  [szczegóły i ograniczenia audytu](docs/AUDIT_1.05.md).

### English

- Scrollable main layout preserves usable tab space at large text sizes;
  aligned tuning/fan rows, wrapped descriptions, correctly sized native combos
  and scrolling to the complete focused field.
- Consistent field/list/button palette and tab indication; themed, scalable
  details/help dialogs and a scrollable RGB dialog with full accessible names.
- Focusable unsupported-feature explanations, invalid-input focus recovery
  and closing/theme-update guards. Expanded PL/EN/theme/layout tests and an
  opt-in actual NVDA Speech Viewer capture.
- **Visual changes await actual NVDA verification; no stable 1.05 release yet.**

## 1.04

### Polski

- W oknie RGB dodano **Pobierz OpenRGB 1.1 (Windows 64-bit)**. Systemowy
  dialog zapisu pozwala wybrać folder i potwierdzić zastąpienie istniejącego ZIP.
- Pobieranie działa w tle, pokazuje postęp i ma przycisk anulowania. Zamknięcie
  okna przerywa transfer. Rozmiar i SHA-256 są sprawdzane przed zapisem docelowym;
  błąd lub anulowanie zachowują istniejący plik i usuwają plik częściowy.
- Po pobraniu systemowy dialog wyjaśnia rozpakowanie ZIP, uruchomienie OpenRGB
  i włączenie serwera SDK. Radeoner nie uruchamia pobranego EXE ani instalatorów.
- Tłumaczenia i instrukcje po polsku i angielsku. Pozostała obsługa RGB wymaga
  uruchomionego OpenRGB, tak jak w 1.03.

### English

- Added **Download OpenRGB 1.1 (Windows 64-bit)** to the RGB dialog, using a
  native Save dialog with overwrite confirmation, background transfer,
  percentage status and cancellation. Closing the window cancels the transfer.
- The official portable ZIP is checked against a pinned size and SHA-256
  before replacing the destination. Failure/cancellation preserves existing
  files and removes partial downloads.
- A native completion dialog explains extracting the ZIP and enabling the
  OpenRGB SDK server. Downloaded executables/installers are not started.
  Polish/English strings and instructions are included.

## 1.03

### Polski

- Jawna obsługa Ctrl+Tab i Ctrl+Shift+Tab z każdej kontrolki głównego okna;
  zachowano Ctrl+1…5. Poprawiono konflikty skrótów Alt po polsku i angielsku.
- Odczyty są natywną listą parametrów i wartości. Domyślnie odświeżają się
  co sekundę, również z fokusem i w trayu. Błąd próbki nie zatrzymuje timera.
- ADLX przeniesiono do własnego procesu pomocniczego: izolacja awarii,
  timeout i ponowne łączenie. Zapisy po awarii wymagają pełnego odczytu.
  Dodano ograniczony lokalny Radeoner.log. Konkretnej awarii RX 9060 pod
  obciążeniem nie odtworzono na fizycznej RX 9060; sprawdzono kontrolowane błędy.
- Tytuł i tray podają wersję, np. Radeoner v1.03.
- Ustawienia mają grupy, krótsze opisy i przewijanie do fokusu także przy
  tekście 200%. Przyciski regulacji są na właściwych zakładkach; skróty globalne działają dalej.
- Skrócono treść okien informacji i wsparcia, ograniczono powtarzane adresy
  i wielokrotne odczyty ustawień motywu podczas malowania.
- Regulacja → Podświetlenie karty (RGB): opcjonalny lokalny OpenRGB SDK,
  wybór GPU/trybu, gotowe kolory lub R/G/B, jasność i wyłączenie. Jasność
  sprzętowa, gdy zgłoszona; w pozostałych przypadkach przyciemnianie koloru.
- 50 testów automatycznych. Widoczna próba rzeczywistych klawiszy oraz okien
  informacji/wsparcia przeszła przy aktywnym NVDA; maksymalny czas utworzenia
  dialogu w tej próbie: 52 ms. Nie przechwycono ani nie odsłuchano syntezy.
- Dokumentacja PL/EN opisuje zakres audytu, stabilność i konfigurację RGB.

### English

- Explicit Ctrl+Tab/Ctrl+Shift+Tab handling from all main-window controls,
  retained Ctrl+1…5, unique Polish/English Alt mnemonics.
- Native parameter/value sensor list, live every second by default, including
  focused rows and tray. A failed sample no longer stops polling.
- ADLX runs in an owned child process with crash isolation, timeout and
  reconnect. Full settings read is required after failure before writes.
  Added bounded local Radeoner.log. The specific physical RX 9060 load crash
  was not reproduced; controlled crash/hang tests were verified.
- Window title and tray include the version, such as Radeoner v1.03.
- Grouped Settings, shorter descriptions and scrolling to focus at 200% text.
  Tuning buttons appear on relevant pages; global shortcuts remain available.
- Shorter About/support content and fewer repeated theme-registry queries.
- Optional GPU lighting through a local OpenRGB SDK server: GPU/mode, presets
  or RGB channels, brightness and off. Hardware brightness when exposed,
  otherwise color scaling. No OpenRGB installation or connection at startup.
- 50 automated tests. A visible real-keyboard/native-dialog tour passed with
  NVDA running; maximum dialog creation time in that tour was 52 ms. Speech
  was not captured/listened to. Bilingual audit, stability and RGB documentation.

## 1.02

### Polski

- Wspólna zakładka Ustawienia: wygląd, język, autostart i aktualizacje.
- Nazwy i zakresy także wewnętrznych pól liczbowych dla czytników ekranu,
  jawna kolejność Tab, przywracanie fokusu i zaznaczenia po odczycie.
- Pięć zakładek bez znaczników skrótów w nazwach; Ctrl+1 do Ctrl+5 wybiera
  zakładkę. Zachowano Ctrl+Tab oraz standardowe kontrolki Windows.
- Motyw Systemowy odczytuje jasny/ciemny wygląd aplikacji Windows;
  wysoki kontrast zachowuje pierwszeństwo.
- Nowa wersja od razu pokazuje systemowe okno z opisem zmian z GitHuba.
  Tak pobiera, sprawdza i instaluje; Nie odkłada. Wszystkie zmiany otwiera
  pełny tekst do odczytu i kopiowania. Jedno potwierdzenie wystarcza.
- O programie oraz Wsparcie i kontakt w Ustawieniach: PayPal, Revolut,
  Telegram, strona autora i kod źródłowy. Wsparcie jest dobrowolne.
- Audyt i instrukcja NVDA po polsku i angielsku. 37 testów automatycznych,
  test rzeczywistych okien TaskDialog/MSAA i sprawdzenie klawiatury/fokusu.
- Skrypt porządków archiwizuje zbędne wyniki do niepotrzebne/ z indeksem.

### English

- One Settings tab for appearance, language, Windows startup and updates.
- Screen-reader names and ranges on inner numeric edits, explicit Tab order,
  focus and selection restored after refreshing fields.
- Five tabs with clean names; Ctrl+1 to Ctrl+5 selects a tab directly.
  Ctrl+Tab and standard Windows controls are retained.
- System theme follows the Windows app light/dark preference; high contrast
  takes precedence.
- Available updates immediately show a native Windows prompt with GitHub
  release notes. Yes downloads, verifies and installs; No postpones.
  All changes opens the full readable/copyable text. One confirmation suffices.
- About and Support and contact in Settings: PayPal, Revolut, Telegram,
  author website and public source. Support is optional.
- Bilingual accessibility audit and NVDA checklist, 37 automated tests,
  real TaskDialog/MSAA checks and keyboard/focus verification.
- Housekeeping archives obsolete local output in niepotrzebne/ with an index.

## 1.01

### Polski

Wydanie testowe pełnej aktualizacji 1.0 → 1.01 przez publiczny kanał GitHub.
Numer wewnętrzny zwiększono z 1.0.0.0 do 1.0.1.0; interfejs i instrukcje
pokazują 1.01. Funkcje GPU pozostają zgodne z wydaniem 1.0. Celem jest
sprawdzenie pobrania, restartu, podmiany EXE i sprzątania kopii po poprawnym starcie.

### English

Release for testing the complete 1.0 → 1.01 update through the public GitHub
feed. Internal version advances from 1.0.0.0 to 1.0.1.0; the UI and instructions
show 1.01. GPU functionality is the same as 1.0. This release exercises
download, restart, executable replacement and confirmed-start backup cleanup.

## 1.0

### Polski

Pierwsze publiczne wydanie Radeonera: lekki panel Windows Forms dla AMD Driver
Only, tekstowe odczyty i regulacje dostępne przez ADLX, profile JSON, tray,
opcjonalny autostart, Personalizacja i języki Polski/English z LNG.
Dodano updater z opcjonalnym sprawdzaniem przy starcie i okresowo, kontrolą
SHA-256, podmianą EXE, potwierdzeniem nowego startu i próbą cofnięcia błędu.

### English

First public Radeoner release: lightweight Windows Forms panel for AMD Driver
Only installations, text monitoring and driver-exposed ADLX tuning, JSON
profiles, tray, optional startup, personalization and Polish/English LNG files.
Includes optional startup/scheduled update checks, SHA-256 validation,
executable replacement, new-start confirmation and attempted rollback.
