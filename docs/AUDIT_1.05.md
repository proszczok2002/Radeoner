# Audyt dostępności 1.05 / Accessibility audit (work in progress)

## Stan / Status

**Poprawki są wersją testową. Weryfikacja wypowiedzi NVDA pozostaje otwarta.**
Nie publikować stabilnego wydania 1.05 ani nie zastępować używanej kopii
portable przed sprawdzeniem każdej zmiany graficznej z NVDA, zgodnie
z wymaganiem użytkownika. Stabilne publiczne wydanie pozostaje 1.04.

**This is an audit candidate. Actual NVDA speech verification is pending.**
The user's requirement is to check every visual change with NVDA. Automated
MSAA checks and an NVDA process running are not sufficient release evidence.

Środowisko: Windows build 26300.9974, 26H2; NVDA 2026.3.1 według lokalnego
logu. Nie zmieniano ustawień NVDA, nie instalowano dodatków i nie zatrzymywano
czytnika ani działającej kopii użytkownika.

## Znalezione problemy i poprawki / Findings and changes

| Obszar / Area | Poprawka / Change | Sprawdzenie automatyczne / Automated evidence | NVDA |
| --- | --- | --- | --- |
| Główne okno / Main window | Przewijany układ zachowuje miejsce na treść zakładek przy 200%; nie znika ona pod paskami przycisków | Minimum wysokości treści, widoczność całego pola po fokusie | Pending |
| Regulacja / Tuning | Równe kolumny, zawijanie opisów, zakres i uwaga także w opisie dostępnym pola | Nazwy, jednostki, zakresy i widoczność pól w PL/EN | Pending |
| Wentylator / Fan | Osobne nazwane wiersze temperatury i prędkości każdego punktu, miejsce na fokus Zero RPM | Kolejność, nazwy i zakresy pól; brak zapisów GPU | Pending |
| Listy wyboru / Combos | Uwzględnienie rzeczywistej wysokości kontrolki Windows; kolory pola zgodne z motywem | Pole nie wychodzi poza wiersz, nazwa/rola pozostają natywne | Pending |
| Motywy / Themes | Spójne tła pól/list, stany przycisków i zaznaczenie zakładki z widocznym fokusem | Kontrast zadanych kolorów; systemowe kolory/renderowanie w gałęzi wysokiego kontrastu | Pending |
| Pomoc i pełny opis / Details | Motyw i tekst z aplikacji; przycisk zamknięcia rośnie z tekstem; F6 wybiera treść | Nazwa i rola pola, kopiowalny tekst, Tab/Escape, widoczność przy 200% | Pending |
| RGB | Przewijana treść; krótsze etykiety przycisków z pełnymi nazwami dostępnymi; połączone pary R/G/B | Połączenie z kontrolowanym SDK, widoczność wszystkich aktywnych pól przy 200% | Pending |
| Brak obsługi / Unsupported controls | Wyjaśnienia braku regulacji/wentylatora jako dostępny klawiszem Tab tekst | Tylko do odczytu, nazwa, treść i TabStop | Pending |
| Błędna liczba / Invalid input | Fokus i zaznaczenie wracają do pola wymagającego poprawy | Niepoprawny tekst odrzucony, brak zapisu GPU | Pending |

Motywy zachowują standardowe kontrolki Microsoft Windows Forms i ich
dostawców dostępności. Okna wyboru plików i TaskDialog zachowują wygląd
systemowy Windows. Wysoki kontrast ma pierwszeństwo przed wyborem motywu.
Dialogi RGB i pełnego opisu reagują również na zmianę preferencji systemu.

W jednej wcześniejszej próbie wystąpił wyjątek `Dispose while CreateHandle`
przy zamykaniu okna testowego. Dodano osłony przed aktualizacją zamykanych
kontrolek i test zamknięcia RGB podczas oczekiwania na listę urządzeń.
Późniejsze próby przeszły; nie jest to dowód wykluczenia każdego możliwego
wyścigu ani zastępstwo dłuższej próby interaktywnej z NVDA.

## Wykonane sprawdzenia / Completed automated checks

- 57 testów: umowy ADLX, profile, aktualizator, izolacja sterownika,
  obsługa klawiatury przez PreProcessMessage, nazwy/role MSAA i układ.
- 60 kombinacji głównego okna: 5 zakładek × PL/EN × jasny/ciemny/systemowy
  × tekst 100%/200%, okno 760 × 650. Każde aktywne pole jest fokusowane
  i sprawdzane względem wszystkich nadrzędnych obszarów przycinania.
- Dialogi RGB i pełnego opisu przy 200%, w obu językach i trzech motywach;
  RGB korzysta z kontrolowanego serwera TCP, bez fizycznej karty RGB.
- Testy uruchamiają te same style wizualne Windows co aplikacja. Zachowano
  zrzuty DrawToBitmap do przeglądu układu; nie zastępują zdjęcia rzeczywistego
  pulpitu ani weryfikacji odczytu czytnika.
- Gałąź wysokiego kontrastu testowana przez odizolowany punkt testowy;
  nie zmieniano konfiguracji Windows. Ręczna próba rzeczywistego trybu systemu
  pozostaje otwarta.
- Zadane kolory tekstu/tła mają sprawdzany współczynnik co najmniej 4,5:1.
  Punktem odniesienia jest [opis kontrastu W3C](https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html).
  To test palety aplikacji, a nie certyfikacja całego pulpitu lub pikseli
  malowanych przez natywne elementy Windows.
- Rzeczywiste TaskDialog: treść zmian, Tak/Nie, O programie, Wsparcie i kontakt;
  inspekcja MSAA przeszła, nie otwierano zewnętrznych linków.
- Lokalna pełna aktualizacja 1.04 → kandydat 1.05 przeszła: start nowego EXE
  i ADLX, potwierdzenie inicjalizacji, usunięcie starego EXE/pomocnika,
  identyczne sumy profilu i preferencji. Osobna kopia, 2026-11-14 09:49 UTC.
  Publiczny kanał nie jest testowany przed wymaganą weryfikacją NVDA.

English automated results: 57 checks passed; a 60-case main-window matrix
checks clipping/focus in PL/EN, three themes and 100/200% text. RGB/details
dialogs, palette contrast, simulated high-contrast selection and native
TaskDialog MSAA checks passed. A local full 1.04 → 1.05-candidate update also
passed while preserving settings/profiles. These results do not validate NVDA
speech or a physical RGB GPU.

## Próby niewykonane / Incomplete checks

Próba rzeczywistych klawiszy zatrzymała się, ponieważ Windows nie przyznał
oknu testowemu pierwszego planu. Test przerwał działanie bez wysyłania klawiszy.
Nie jest to zaliczona próba klawiatury z NVDA.

Podgląd mowy nie był dostępny podczas preflight. `live-nvda.txt` zapisuje
**NOT VERIFIED**; nie ma przechwyconej wypowiedzi potwierdzającej nowe kontrolki.
Nie deklarujemy odsłuchu ani pełnego audytu NVDA.

Foreground activation was refused during the actual keyboard-input tour.
No keys were sent to another window. Speech Viewer was unavailable, so the
NVDA capture reported **NOT VERIFIED** rather than substituting control names.

## Jak dokończyć z NVDA / Completing reader verification

1. Włącz **NVDA+N → Narzędzia → Podgląd mowy**. Nie zmieniaj opcji startu
   podglądu z systemem. Zamknij pozostałe menu NVDA klawiszem Escape.
2. Uruchom z katalogu projektu:
   `powershell -NoProfile -ExecutionPolicy Bypass -File tools/Test-Nvda.ps1`.
   Powstaje osobna kompilacja testowa; stabilna kopia portable pozostaje zachowana.
3. Aktywuj okno **Radeoner v1.05 — audyt / test data**, jeśli Windows sam go
   nie uaktywni. Test czeka do 30 sekund. Podczas przebiegu nie używaj klawiatury.
4. Test używa danych kontrolnych i lokalnego SDK testowego. Nie zapisuje
   parametrów fizycznego GPU. Dla obu języków, trzech motywów i dwóch rozmiarów
   tekstu fokusuje zakładki, pola, RGB i pełny opis. Zapisuje wyłącznie nowe
   wypowiedzi z tych punktów w `artifacts/tests/live-nvda.txt`.
5. Przeczytaj transkrypcję: nazwa, rola, wartość, jednostka, zakres, stan,
   brak niepotrzebnych powtórzeń. Samo znalezienie nazwy nie oznacza, że cała
   wypowiedź jest zrozumiała. Jeśli Windows blokuje także odczyt podglądu,
   potrzebny jest odsłuch użytkownika i zapis rzeczywistych wypowiedzi.
6. Osobno sprawdź rzeczywisty Tab/Shift+Tab, Ctrl+Tab/Ctrl+Shift+Tab z otwartą
   listą, F5/F6/F1, zachowanie błędu liczby, pliki importu/eksportu/raportu/ZIP,
   dialog Tak/Nie i pełny opis zmian, O programie, wsparcie, ukrycie/przywrócenie
   przez tray oraz jasny/ciemny i rzeczywisty wysoki kontrast Windows.
7. Dopiero po poprawieniu znalezionych problemów i zapisaniu wyników NVDA
   przygotuj stabilne 1.05, pełny lokalny/publiczny test aktualizacji i porządki.

English: enable NVDA Speech Viewer, run `tools/Test-Nvda.ps1`, activate the
clearly labelled audit window if needed, and keep the keyboard idle. The
optional tour reads actual newly appended Speech Viewer text, aborts if focus
leaves its own window, and never changes NVDA settings or physical GPU tuning.
Review name/role/value/state clarity manually; complete native file-dialog,
tray, actual keyboard and real high-contrast checks before release.
