# Dostępność / Accessibility

Aktualne prace: [audyt 1.05 — weryfikacja NVDA otwarta](AUDIT_1.05.md).
Current work: [1.05 audit candidate — NVDA verification pending](AUDIT_1.05.md).
Poniższe wyniki opisują wcześniejsze wydania / Older release results follow.

## Polski

Audyt obejmuje strukturę Microsoft WinForms/MSAA, nazwy, role, opisy zakresów,
przejście klawiaturą, fokus, motywy oraz rzeczywiste okna systemowe Windows.
Automatyczna inspekcja nie zastępuje odsłuchu NVDA. Odsłuch z udziałem
użytkownika pozostaje do wykonania; nie deklarujemy pełnej zgodności.

### Dodatkowe próby 1.03

50 testów automatycznych przeszło. Sprawdzono wejście klawiszy przez
`PreProcessMessage` z listy, pól, raportu i przycisków. Widoczna próba użyła
rzeczywistego SendInput dla pełnego cyklu Ctrl+Tab i Ctrl+Shift+Tab przy
aktywnym **NVDA 2026.3.1**; zakładki i fokus zachowały się poprawnie.
Widoczne okna O programie/wsparcia utworzyły się maksymalnie w **52 ms**
w tej próbie. Osobne pomiary bez opóźnienia testowego: około 34–50 ms.
To czas utworzenia okna, **nie czas odczytu treści przez syntezator**.

Nie przechwycono mowy i nie wykonano odsłuchu. Próba automatyzacji konsoli
NVDA napotkała blokadę Windows (odmowa dostępu, kod 5); nie instalowano
dodatków i nie zmieniano konfiguracji czytnika. Samo istnienie procesu NVDA
nie stanowi potwierdzenia zrozumiałego odczytu każdej kontrolki.

Ustawienia mają trzy nazwane grupy, krótsze opisy i automatyczne przewijanie
do fokusu. Odczyty to standardowy ListView Parametr/Wartość. Status jest
jednowierszowy, dostępny przez F6. Niepotrzebne na danej zakładce przyciski
regulacji są ukryte; F5 i pozostałe skróty globalne nadal działają.
Sprawdzono brak konfliktów Alt na każdej zakładce w obu językach oraz
dostęp do przycisków przy tekście 100% i 200% w małym oknie.

### Poprawione problemy

| Problem | Zmiana |
| --- | --- |
| Nazwa pola tylko na NumericUpDown | Wewnętrzne pole edycji ma tę samą nazwę, jednostkę i zakres |
| Fokus znikał przy przebudowie pól | Przywracanie po identyfikatorze ustawienia; zachowanie zaznaczenia tekstu |
| Domyślna kolejność Tab | Jawna kolejność; nagłówki i pojemniki nie są dodatkowym przystankiem |
| Osobne Personalizacja/Aktualizacje | Jedna przewijalna zakładka Ustawienia; łącznie pięć zakładek |
| Białe tło przy ciemnym motywie Windows | Odczyt AppsUseLightTheme dla motywu Systemowy; reakcja na zmianę preferencji |
| Powiadomienie bez zmian wydania | Systemowy TaskDialog pokazuje zmiany i Tak/Nie; pełny opis jako tekst |
| Trudny dostęp do informacji i kontaktu | O programie oraz Wsparcie i kontakt w Ustawieniach, standardowe przyciski |

Zakładki mają tekstowe nazwy Odczyty, Regulacja, Wentylator, Raport, Ustawienia,
bez `&` w tytułach. Ctrl+1…Ctrl+5 wybiera odpowiednią zakładkę. Ctrl+Tab oraz
Ctrl+Shift+Tab pozostają dostępne. Tab/Shift+Tab przechodzą po kontrolkach.
F6 kieruje fokus do ostatniego komunikatu, F1 otwiera pomoc z tekstem do
odczytu i kopiowania. Lista odczytów ma strzałki góra/dół i Ctrl+A/Ctrl+C;
wartości zmieniają się co sekundę bez przenoszenia zaznaczenia. Raport pozostaje
tekstem tylko do odczytu z Ctrl+A/Ctrl+C.

Potwierdzenia są oknami Windows; wybór pliku używa OpenFileDialog/SaveFileDialog.
Nowe okna aktualizacji, informacji i wsparcia używają natywnego TaskDialog.
Długi opis ma osobne okno ze standardowym, tylko do odczytu TextBox i Zamknij;
Tab oraz Escape pozwalają je opuścić. Wysoki kontrast Windows ma pierwszeństwo.
Natywne okna systemowe korzystają z ustawień tekstu i języka Windows.

Test `-LiveDialogs` otwiera rzeczywiste okna, odczytuje nazwy i treść MSAA,
sprawdza Tak/Nie, przyciski informacji/wsparcia i zamyka je bez otwierania
linków. `-LiveUi` sprawdza rzeczywistą pętlę komunikatów, nazwy MSAA,
fokus/zaznaczenie raportu i pięć skrótów zakładek. Żaden test nie stosuje
ustawień do fizycznego GPU. Zapisy regulacji są testowane przez symulację ADLX.

### Przejście z NVDA

1. Zanotuj wersję Windows, NVDA, Radeonera i wybrany język.
2. Przejdź Tab/Shift+Tab: GPU, odświeżanie, profile, zakładki, przyciski i status.
3. Sprawdź Ctrl+Tab, Ctrl+Shift+Tab, Ctrl+1…5 i odczyt nazw zakładek.
4. Przeczytaj odczyty i raport; zaznacz tekst, użyj F5 i sprawdź fokus; użyj F6.
5. Na GPU udostępniającym regulację sprawdź nazwę, jednostkę, bieżącą wartość
   i zakres każdego pola oraz punktów wentylatora. Bez stosowania zmian.
6. W Ustawieniach sprawdź język, motyw, tekst 100–200%, autostart i aktualizacje.
7. Otwórz O programie oraz Wsparcie i kontakt. Sprawdź nazwy przycisków i Escape.
8. Przy nowszym wydaniu przeczytaj opis, otwórz Wszystkie zmiany, odłóż przez Nie.
   Tak wykonuje rzeczywistą aktualizację; przetestuj na kopii portable.
9. Alt+F4 chowa do traya; Win+B i menu ikony pozwalają wrócić. Zakończ kończy program.

Zgłoszenie powinno zawierać oczekiwany odczyt, rzeczywisty odczyt i kroki.
Nie dołączaj prywatnych danych. [Issues](https://github.com/proszczok2002/Radeoner/issues)
lub [Telegram autora](https://t.me/proszczok).

## English

1.03 passed 50 automated tests, real WinForms `PreProcessMessage` key routing,
and a visible full Ctrl+Tab/Ctrl+Shift+Tab tour using actual SendInput with
**NVDA 2026.3.1 running**. Visible About/support creation took at most **52 ms**
in that tour; separate probes measured roughly 34–50 ms. These are window
creation times, not speech latency. Speech was not captured/listened to.
Windows denied access (code 5) to console automation; NVDA configuration and
add-ons were not modified. Running NVDA is not proof of complete spoken usability.

Settings now have three named groups and shorter text. The sensor ListView
updates values while focused, preserving selection. A compact status field
is reached with F6. Irrelevant tuning buttons are hidden by page; global
shortcuts remain available. Polish/English Alt keys and focused-control
visibility at 100%/200% text were checked. RGB uses standard labeled controls,
numeric edit children, presets and explicit Apply/Off actions.

This audit covers Microsoft WinForms/MSAA structure, names, roles, ranges,
keyboard navigation, focus, themes and real native Windows dialogs. Automated
inspection is not an NVDA listening session. User-assisted listening remains
outstanding; full compatibility is not claimed.

Numeric edit children now expose names/units/ranges. Refreshing fields restores
focus and text selection. Tab order is explicit. Five tabs use clean text names:
Readings, Tuning, Fan, Report, Settings. Ctrl+1…5 selects them directly;
Ctrl+Tab/Ctrl+Shift+Tab cycles. System theme follows AppsUseLightTheme and responds
to Windows preference changes; high contrast takes precedence.

Updates use a native TaskDialog with immediate release notes and Yes/No.
All changes opens a standard read-only, copyable TextBox with Close/Escape.
About and support/contact also use TaskDialog; file operations use native
OpenFileDialog/SaveFileDialog. Native dialogs use Windows text/language settings.
F6 focuses status; F1 opens copyable help. The sensor list updates focused
rows without moving selection; Up/Down navigates and Ctrl+A/C copies rows.
Alt+F4 hides to tray; explicit Exit stops the app.

`-LiveDialogs` inspects real native windows and their MSAA content/buttons,
then dismisses them without opening links. `-LiveUi` checks the real message
loop, MSAA names, report focus/selection and all five direct tab shortcuts.
These tests do not write physical GPU settings. Tuning writes use simulated ADLX.

NVDA checklist: record app/Windows/NVDA versions; traverse all controls with
Tab/Shift+Tab; check tab shortcuts, field names/values/units/ranges and fan
points where supported; copy reports, refresh with F5 and inspect focus; use F6;
try language, theme and 100–200% text; inspect About/support and Escape; read
update notes and All changes, dismiss with No; try Yes on a disposable portable
copy; hide/restore via Win+B and exit explicitly. Report expected versus actual
speech and reproducible steps through GitHub Issues or the author's Telegram.
# Pobieranie OpenRGB w 1.04 / OpenRGB download

Okno RGB ma nazwany przycisk pobierania dostępny bez połączenia z serwerem.
Wybór miejsca zapisu i pytanie o zastąpienie używają standardowego SaveFileDialog.
Podczas transferu fokus przechodzi na Anuluj pobieranie. F6 wybiera tekstowy
stan z postępem; Escape zamyka okno i anuluje transfer. Zakończenie pokazuje
natywny TaskDialog z instrukcją rozpakowania i włączenia SDK.

The RGB download uses native Save and completion dialogs, explicit accessible
button names, a cancel button and a copyable status field (F6). Downloading is
asynchronous; closing cancels it. Automated checks do not constitute an NVDA
speech/listening test. No new NVDA speech test was performed for 1.04.
