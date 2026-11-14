# Audyt dostępności 1.05 / Accessibility audit

Data / Date: **2026-11-14**. Windows build 26300.9974 (26H2), NVDA 2026.3.1.

## Wynik / Result

Sprawdzono **rzeczywisty tekst Podglądu mowy działającego NVDA**, niezależnie
od testów MSAA. Przejrzano nazwy, role, wartości, zakresy i powtórzenia.
Nie wykonywano nagrania ani odsłuchu sygnału audio syntezatora.

Actual running NVDA Speech Viewer output was captured and reviewed, separately
from MSAA checks. This validates the recorded speech text, not an audio recording
or a claim of universal accessibility certification.

| Próba / Check | Wynik / Result |
| --- | --- |
| 5 zakładek, PL/EN, jasny/ciemny/systemowy, tekst 100/200% | 540 punktów rzeczywistych odczytów NVDA: PASS |
| Pola GPU, wentylator, ustawienia, RGB i pełny opis | Nazwy, role, wartości i zakresy odczytane |
| Pola liczbowe: poprawność roli i strzałki | Pojedyncza nazwa/zakres, rola pokrętła; klawisz Up przez SendInput zmienia 42 na 43, NVDA odczytuje 43 |
| O programie, wsparcie, Tak/Nie aktualizacji, pliki importu/eksportu/raportu/OpenRGB, PL/EN | 60 punktów odczytu NVDA: PASS; pliki anulowano, aktualizację odrzucono, linków nie otwierano |
| Ctrl+Tab i Ctrl+Shift+Tab, także z rozwiniętej listy języka | Rzeczywisty SendInput w oknie testowym: PASS |
| Tray: nazwa i wersja, GPU, temperatura, wentylator i moc | Wypowiedź NVDA z rzeczywistej ikony Shell: PASS |
| Ukrycie, fokus ikony, Enter i przywrócenie okna, menu traya | Rzeczywista ikona, wejście klawiatury i odczyt menu: PASS |
| Układ 760 x 650, 5 zakładek x PL/EN x 3 motywy x 100/200% | 60 kombinacji, kontrola całych pól we wszystkich przodkach przycinania: PASS |
| Domyślne testy regresji | 57: PASS; ADLX i RGB z danymi kontrolnymi |

Native About/support dialogs created in at most **60 ms** in the visible keyboard
trial; this is window creation time, not synthesizer reading duration.

## Poprawki / Changes

- Główne okno zachowuje użyteczne miejsce na treść zakładek przy dużym tekście.
  Całość i formularze przewijają się do pełnego pola z fokusem.
- Parametry i punkty wentylatora mają wyrównane wiersze, zawijane etykiety,
  zakresy i jednostki. Natywne listy wyboru uwzględniają rzeczywistą wysokość.
- Motywy mają spójne pola/listy/przyciski i oznaczenie wybranej zakładki.
  Zaznaczenie i fokus nie zależą wyłącznie od koloru.
- Pomoc i pełny opis przejmują motyw i rozmiar tekstu. Przycisk zamknięcia
  rośnie z tekstem. F6 wybiera treść; Escape zamyka.
- RGB ma przewijaną treść i krótsze widoczne etykiety przycisków z pełnymi
  nazwami dostępnymi. R/G/B pozostają razem ze swoimi polami.
- Brak obsługi regulacji/wentylatora jest wyjaśniony w kopiowalnym polu
  dostępnym klawiszem Tab. Niepoprawna liczba kieruje fokus do poprawy.
- NVDA ujawnił powtarzanie nazwy i zakresu liczby przez zewnętrzne pokrętło
  i wewnętrzną edycję. Kontener ma teraz rolę klienta, a natywne pole z fokusem
  rolę pokrętła oraz nazwę/jednostkę/zakres/wartość. Nie wymieniono dostawcy
  dostępności ani klasy kontrolki Windows. Przykład rzeczywistego odczytu:
  `Minimalny zegar GPU, MHz — pokrętło — 1000 — Zakres 500 do 3000, krok 10`.
- Nazwa grupy zakładek to teraz Zakładki Radeonera / Radeoner tabs.
- Aktualizacje motywu i odpowiedzi RGB pomijają zamykane kontrolki. Test
  zamknięcia podczas oczekiwania na listę urządzeń przeszedł również z NVDA.

English: responsive aligned rows, unclipped native combos, complete focus
scrolling, consistent palettes, themed scalable details, scrollable RGB,
focusable unsupported-feature explanations and invalid-field recovery were
checked. Actual NVDA testing found and removed duplicated numeric labels and
ranges while preserving native spin semantics, values and arrow-key input.

## Zakres i ograniczenia / Scope and limits

- Dane GPU i lokalny serwer RGB w przebiegu mowy były kontrolnymi danymi testowymi.
  Nie zmieniano nastaw fizycznej karty. Osobny test widocznego okna uruchomił
  rzeczywisty odczyt ADLX. Nie wykonano fizycznego testu RGB TUF ani obciążenia RX 9060.
- Wysoki kontrast ma pierwszeństwo w kodzie i testach wyboru palety. Nie
  przełączano globalnego trybu wysokiego kontrastu Windows użytkownika.
- Test palety sprawdza zadane kolory tekst/tło >= 4,5:1 według
  [wzoru W3C](https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html).
  Nie jest to certyfikacja każdego piksela rysowanego przez Windows.
- NVDA może powtórzyć pierwszy wiersz natywnej listy przy wejściu. W zapisanych
  próbach niektóre wejścia miały dwa jednakowe komunikaty tego wiersza.
  Powtórzenia nazw i długich zakresów pól liczbowych usunięto.
- Inne czytniki, monitory/DPI i modele GPU wymagają osobnych prób.

No NVDA add-on was installed and no reader configuration was changed. Speech
Viewer was enabled by the user. Optional interactive tests abort when focus
leaves the owned window; Shell icon tests additionally verify the exact owned
icon name through UI Automation before sending Enter. Raw transcripts remain
local and are not published wholesale.

## Powtórzenie testów / Reproduction

Enable NVDA Speech Viewer and run `tools/Test-Nvda.ps1`. It builds an isolated
copy and captures actual speech at `artifacts/tests/live-nvda.txt`.
Additional modes of `Radeoner.Tests.exe` are `--nvda-dialogs <report>` and
`--nvda-numeric <report>`; `tools/Test-Nvda-Tray.ps1` checks the actual owned
Shell icon. Keep the keyboard idle during the tour and activate the clearly
labelled audit window if Windows requests manual activation.

Evidence before housekeeping: `artifacts/tests/live-nvda.txt` (540 checkpoints),
`nvda-native.txt` (60), `nvda-extended.txt` and `.values` (numeric keys),
`nvda-tray-keyboard-*.txt` and `keyboard-release.txt`. Locate archived paths
using `niepotrzebne/*/INDEKS.csv`. These are text captures, not audio recordings.
