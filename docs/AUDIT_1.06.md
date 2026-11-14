# Dostępność 1.06 / Accessibility 1.06

Data: 2026-11-14. Windows 10.0.26300, NVDA 2026.3.1.
**900 punktów rzeczywistych wypowiedzi NVDA: PASS. Natywne dialogi i tray: w trakcie.**

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
| Nazwy i zakresy edycji liczbowych | Odczyt pojedynczej nazwy, roli, wartości i zakresu |
| Dostępne zakładki integry, w obie strony | PASS, rzeczywiste Ctrl+Tab/Ctrl+Shift+Tab |
| Automatyczny layout 100/200%, sześć języków i trzy motywy | 180 kombinacji zakładek PASS |
| Typ i możliwości GPU | 48 kombinacji PASS, bez zapisów sprzętowych |
| Nowy wybór języka opisów aktualizacji, pełne LNG, instrukcje i preferencje | Dwa lokalne testy ukierunkowane PASS |
| Natywne About/Wsparcie/aktualizacja i dialogi plików, sześć języków | W TRAKCIE |
| Prawdziwy Shell tray, sześć języków, integra i dedykowana GPU | W TRAKCIE |

Główne okno i jego layout nie zmieniały się po audycie 900 punktów. Końcowa
poprawka dotyczy wyłącznie wyboru przetłumaczonego opisu aktualizacji; jej okna
oraz tray są sprawdzane na końcowym EXE. W przypadku starszego opisu bez języka
aplikacji wybierany jest dostępny tekst angielski lub polski.

Wcześniejszy pełny lokalny przebieg miał 61 testów PASS. Nowy test wyboru
języka dodaje jeden przypadek regresji. Rzeczywiste skróty, odczyty w tle oraz
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
are being checked against the final EXE. Two focused local language tests
passed, in addition to the previous 61-check full regression run. Automated
layout coverage has 180 combinations; capability coverage has 48. Native About/
Support creation on Radeon 880M took at most 39 ms, not synthesizer reading time.

## Deutsch

Die tatsächlichen NVDA-Ausgaben wurden im Sprachbetrachter erfasst und geprüft:
150 Prüfpunkte pro Sprache, insgesamt 900, mit drei Designs und 200% Text.
Namen, Rollen, Werte, Bereiche und die verfügbaren Registerkarten wurden geprüft.
Es wird keine Audioaufnahme oder vollständige Hardwareprüfung behauptet.
Die abschließenden nativen Dialog- und Infobereichsprüfungen laufen noch.

## Русский

Реальные сообщения NVDA записаны из просмотрщика речи и проверены: по 150
точек для каждого языка, всего 900, три темы и текст 200%. Проверены имена,
роли, значения, диапазоны и доступные вкладки. Это проверка текста речи,
а не запись и прослушивание аудио или проверка всех моделей GPU.
Проверки окончательных системных диалогов и значка трея ещё выполняются.

## Español

Se capturó y revisó el texto real del visualizador de voz de NVDA: 150 puntos
por idioma, 900 en total, tres temas y texto al 200%. Se comprobaron nombres,
roles, valores, intervalos y pestañas disponibles. No se afirma haber grabado
ni escuchado el audio ni haber probado todos los modelos físicos de GPU.
Las comprobaciones finales de diálogos nativos y bandeja siguen en curso.

## Français

Le texte réel du visualiseur de parole NVDA a été capturé et vérifié : 150
points par langue, 900 au total, trois thèmes et texte à 200 %. Les noms,
rôles, valeurs, plages et onglets disponibles ont été contrôlés. Il ne s'agit
pas d'une déclaration d'écoute audio ni d'un test de toutes les GPU physiques.
Les vérifications finales des dialogues natifs et de l'icône sont en cours.

## Ograniczenia / Limits

- Windows i sterownik dostarczają własne nazwy urządzeń, trybów i kontrolek
  systemowych; ich język może różnić się od języka aplikacji.
- Natywna lista może powtórzyć pierwszy wiersz przy wejściu. Zachowano standardowy
  dostawca dostępności, role, wybór i kopiowanie listy.
- Nie zmieniano konfiguracji NVDA ani użytkowej kopii programu w Documents.
- Nie przeprowadzono fizycznej regulacji dedykowanej GPU, testu obciążenia RX 9060
  ani sterowania rzeczywistym RGB. Natywny backend RGB nie został wdrożony.
- System high contrast was exercised through the test hook, without changing
  the user's Windows setting. Native list entry can repeat its first row.
- No physical GPU tuning, RX 9060 load test or real RGB was performed. RGB
  still requires optional OpenRGB. This is not universal accessibility certification.

## Dowody / Evidence

Główne UI: `niepotrzebne/20261114-162402-nvda-ui-1.06/artifacts/tests/nvda-1.06-ui.txt`.
Bieżące dialogi i tray: `artifacts/tests/`. Po porządkach użyj indeksu
`niepotrzebne/*/INDEKS.csv`. Surowe transkrypcje pozostają lokalne.
Powtórzenie: `--live-nvda`, `--nvda-dialogs` oraz
`tools/Test-Nvda-Tray.ps1 -Language <pl|en|de|ru|es|fr> [-Integrated]`.