# Dostępność 1.06 / Accessibility 1.06

Data: 2026-11-14. Windows 10.0.26300, NVDA 2026.3.1.
**PASS: 900 punktów głównego UI, 180 natywnych dialogów i 72 traya.**

## Polski

Przechwycono i przejrzano tekst rzeczywistych wypowiedzi działającego NVDA
z Podglądu mowy. Nie nagrywano ani nie odsłuchiwano audio syntezatora.
Każdy z sześciu języków ma 150 punktów: trzy motywy, tekst 200%, główne pola,
wartości/jednostki/zakresy, pomoc, RGB i zmiana dostępnych zakładek integry.
Suma: 900. Ctrl+Tab i Ctrl+Shift+Tab były rzeczywistymi klawiszami SendInput
w oknie testowym. Obsługiwane funkcje integry wracały do interfejsu; puste
zakładki były pomijane. Test nie stosował parametrów fizycznej karty.

| Sprawdzenie | Wynik |
| --- | --- |
| Sześć języków × trzy motywy × tekst 200%, NVDA | 900 punktów PASS |
| Nazwy i zakresy edycji liczbowych | 306 punktów z pojedynczą nazwą pola, rolą, wartością i zakresem |
| Dostępne zakładki integry, w obie strony | PASS, rzeczywiste Ctrl+Tab/Ctrl+Shift+Tab |
| Automatyczny layout 100/200%, sześć języków i trzy motywy | 180 kombinacji zakładek PASS |
| Typ i możliwości GPU | 48 kombinacji PASS, bez zapisów sprzętowych |
| Nowy wybór języka opisów aktualizacji, pełne LNG, instrukcje i preferencje | Dwa lokalne testy ukierunkowane PASS |
| Natywne About/Wsparcie/aktualizacja i dialogi plików, sześć języków | 180 punktów PASS |
| Prawdziwy Shell tray, sześć języków, integra i dedykowana GPU | 12 prób, 72 punkty PASS; Enter i menu |

Główne okno i jego layout nie zmieniały się po audycie 900 punktów. Końcowa
poprawka dotyczy wyłącznie wyboru przetłumaczonego opisu aktualizacji; jej okna
oraz tray sprawdzono na końcowym EXE. W przypadku starszego opisu bez języka
aplikacji wybierany jest dostępny tekst angielski lub polski.

Wcześniejszy pełny lokalny przebieg miał 61 testów PASS. Nowy test wyboru
języka dodaje jeden przypadek regresji. Wszystkie 62 przypadki przeszły także
na czystym [Windows CI](https://github.com/proszczok2002/Radeoner/actions/runs/43610777104).
Rzeczywiste skróty, odczyty w tle oraz
natywne O programie/Wsparcie na Radeon 880M przeszły; maksymalne utworzenie
okna trwało 39 ms. To czas utworzenia, nie odczytu przez syntezator.

## English

Actual running NVDA Speech Viewer text was captured and reviewed. This is
speech-text validation, not an audio recording/listening claim. Each language
has 150 checkpoints across three themes and 200% text: main controls, numeric
names/roles/values/ranges, help, RGB and integrated-GPU capability changes.
Total: 900 PASS. Actual Ctrl+Tab/Ctrl+Shift+Tab input skipped unavailable pages
and retained supported integrated-GPU features.

The main-window/layout code was unchanged after this tour. The final change
only selects translated update notes; native dialogs and the actual Shell tray
passed against the final EXE: 180 dialog and 72 tray checkpoints. All six update
prompts spoke the selected language. Twelve tray cases covered each language
with both GPU types; integrated text omitted RPM, discrete text spoke 333 RPM,
and actual Enter restored the app. Two focused local language tests
passed, in addition to the previous 61-check full regression run. Automated
layout coverage has 180 combinations; capability coverage has 48. All 62
regression cases passed on clean Windows CI. Native About/
Support creation on Radeon 880M took at most 39 ms, not synthesizer reading time.

## Deutsch

Die tatsächlichen NVDA-Ausgaben wurden im Sprachbetrachter erfasst und geprüft:
150 Prüfpunkte pro Sprache, insgesamt 900, mit drei Designs und 200% Text.
Namen, Rollen, Werte, Bereiche und die verfügbaren Registerkarten wurden geprüft.
Es wird keine Audioaufnahme oder vollständige Hardwareprüfung behauptet.
Auch 180 native Dialog- und 72 Infobereichsprüfpunkte sowie 62 CI-Tests bestanden.
Updates nennen die Änderungen in der gewählten Sprache. Bei integrierten GPUs
nennt das Symbol keinen Lüfter; Eingabe öffnet die Anwendung.

## Русский

Реальные сообщения NVDA записаны из просмотрщика речи и проверены: по 150
точек для каждого языка, всего 900, три темы и текст 200%. Проверены имена,
роли, значения, диапазоны и доступные вкладки. Это проверка текста речи,
а не запись и прослушивание аудио или проверка всех моделей GPU.
Также пройдены 180 точек системных диалогов, 72 точки трея и 62 теста CI.
Обновления читают изменения на выбранном языке. Значок встроенной GPU не
упоминает вентилятор; Enter открывает окно.

## Español

Se capturó y revisó el texto real del visualizador de voz de NVDA: 150 puntos
por idioma, 900 en total, tres temas y texto al 200%. Se comprobaron nombres,
roles, valores, intervalos y pestañas disponibles. No se afirma haber grabado
ni escuchado el audio ni haber probado todos los modelos físicos de GPU.
También pasaron 180 puntos de diálogos nativos, 72 de bandeja y 62 pruebas CI.
Las actualizaciones leen los cambios en el idioma elegido; la bandeja integrada
omite ventiladores e Intro abre la ventana.

## Français

Le texte réel du visualiseur de parole NVDA a été capturé et vérifié : 150
points par langue, 900 au total, trois thèmes et texte à 200 %. Les noms,
rôles, valeurs, plages et onglets disponibles ont été contrôlés. Il ne s'agit
pas d'une déclaration d'écoute audio ni d'un test de toutes les GPU physiques.
Les 180 points de dialogues natifs, 72 de l'icône et 62 tests CI sont aussi validés.
Les mises à jour lisent les changements dans la langue choisie ; l'icône intégrée
omet le ventilateur et Entrée ouvre la fenêtre.

## Ograniczenia / Limits

- Windows i sterownik dostarczają własne nazwy urządzeń, trybów i kontrolek
  systemowych; ich język może różnić się od języka aplikacji.
- Natywna lista może powtórzyć pierwszy wiersz przy wejściu. Zachowano standardowy
  dostawcę dostępności, role, wybór i kopiowanie listy.
- Shell może poprzedzić tooltip nazwą okna ikony, powtarzając nazwę/wersję;
  prawidłowe dane wybranej ikony pozostają dostępne. Otwarcie przepełnienia
  zasobnika może najpierw ogłosić inną ikonę, zanim fokus trafi do testowanej.
- Nie zmieniano konfiguracji NVDA ani użytkowej kopii programu w Documents.
- Nie przeprowadzono fizycznej regulacji dedykowanej GPU, testu obciążenia RX 9060
  ani sterowania rzeczywistym RGB. Natywny backend RGB nie został wdrożony.
- System high contrast was exercised through the test hook, without changing
  the user's Windows setting. Native list entry can repeat its first row.
- No physical GPU tuning, RX 9060 load test or real RGB was performed. RGB
  still requires optional OpenRGB. This is not universal accessibility certification.

## Dowody / Evidence

Główne UI: `niepotrzebne/20261114-162402-nvda-ui-1.06/artifacts/tests/nvda-1.06-ui.txt`.
Dialogi: `artifacts/tests/native-<kod>.txt` (30 punktów na język), tray:
`artifacts/tests/nvda-tray-keyboard-*.txt`. Podsumowanie: `artifacts/audit-summary.json`.
Próby przerwane przez zmianę fokusu do Terminala lub limit synchronicznego
odczytu NVDA nie były zaliczone. Końcowe serie per język przeszły w całości;
odczyt narzędzia jest asynchroniczny, aby obsługiwać zapytania dostępności okna.
Po porządkach użyj indeksu
`niepotrzebne/*/INDEKS.csv`. Surowe transkrypcje pozostają lokalne.
Powtórzenie: `--live-nvda`, `--nvda-dialogs <raport> [kod języka]` oraz
`tools/Test-Nvda-Tray.ps1 -Language <pl|en|de|ru|es|fr> [-Integrated]`.
