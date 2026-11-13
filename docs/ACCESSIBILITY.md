# Dostępność / Accessibility — 1.02

## Polski

Audyt obejmuje strukturę Microsoft WinForms/MSAA, nazwy, role, opisy zakresów,
przejście klawiaturą, fokus, motywy oraz rzeczywiste okna systemowe Windows.
Automatyczna inspekcja nie zastępuje odsłuchu NVDA. Odsłuch z udziałem
użytkownika pozostaje do wykonania; nie deklarujemy pełnej zgodności.

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
odczytu i kopiowania. Odczyty i raport mają Ctrl+A/Ctrl+C. Aktualizacja
czujników nie nadpisuje pola odczytów podczas jego czytania z fokusem.

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
F6 focuses status; F1 opens copyable help. Readings are not overwritten while
their text box has focus. Alt+F4 hides to tray; explicit Exit stops the app.

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
