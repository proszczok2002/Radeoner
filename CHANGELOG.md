# Zmiany / Changelog

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
