# Radeoner

**1.06:** funkcje i zakładki dopasowane do możliwości GPU, tray bez wentylatorów
integry oraz sześć języków z instrukcjami offline w F1.

**Lekki, przenośny panel kart AMD dla Windows, projektowany z myślą o NVDA.**

[English documentation](README.en.md) · [Pobierz / Download](https://github.com/proszczok2002/Radeoner/releases/latest) · [Zgłoś problem / Issues](https://github.com/proszczok2002/Radeoner/issues) · [Współpraca / Contributing](CONTRIBUTING.md)

[Deutsch](README.de.md) · [Русский](README.ru.md) · [Español](README.es.md) · [Français](README.fr.md)

Radeoner komunikuje się ze sterownikiem AMD przez publiczne API ADLX. Pozwala
korzystać z odczytów i regulacji udostępnianych przez kartę bez instalowania
panelu Adrenalin. Interfejs to Microsoft Windows Forms i .NET Framework 4.8:
standardowe kontrolki, obsługa klawiatury, jawne nazwy dla czytników ekranu
i tekstowe wyniki. Bez przeglądarki, usługi działającej w tle i dodatkowego
sterownika Radeonera.

Kod aplikacji jest publiczny na licencji **MIT**. Możesz zgłaszać błędy,
proponować funkcje, ulepszać tłumaczenia i przesyłać pull requesty.

## Pobranie i uruchomienie

1. Pobierz `Radeoner-<wersja>-portable.zip` ze strony [wydań](https://github.com/proszczok2002/Radeoner/releases).
2. Rozpakuj cały ZIP do folderu z prawem zapisu.
3. Uruchom **Radeoner.exe**. Instalator i uprawnienia administratora nie są wymagane.

Wymagania: **Windows 10/11 x64, .NET Framework 4.8** oraz sterownik AMD
udostępniający ADLX. Tryb instalacji sterownika **Driver Only** jest obsługiwany,
jeśli ten sterownik zawiera ADLX. Biblioteka jest wczytywana z System32;
nie jest dołączana ani pobierana oddzielnie przez aplikację.

## Co potrafi

| Obszar | Funkcje |
| --- | --- |
| Karty | Wybór AMD GPU i rozpoznawanie integry/karty dedykowanej przez sterownik |
| Odczyty | Natywna lista parametrów i wartości, domyślnie aktualizowana co sekundę, także w trayu |
| Regulacja | Procentowy limit mocy, zegary GPU/VRAM, napięcie, stany starszych kart |
| RGB | Kolor, jasność i wyłączenie przez opcjonalny lokalny serwer OpenRGB |
| Chłodzenie | Krzywa wentylatora, Zero RPM, minimalne/docelowe RPM, jeśli dostępne |
| Profile | Eksport/import JSON z kontrolą modelu GPU, podsystemu, jednostek i zakresów |
| Tray | Alt+F4 chowa okno; ikona pokazuje nazwę GPU i rzeczywiste odczyty |
| Ustawienia | Polski, English, Deutsch, Русский, Español, Français z LNG, motyw i tekst 100–200% |
| Start systemu | Opcjonalny autostart do traya dla bieżącego użytkownika |
| Aktualizacje | Ręczne i opcjonalne okresowe sprawdzanie, pobieranie i podmiana plików |

Uruchomienie aplikacji nie stosuje profilu GPU. Import jedynie wypełnia pola;
**Zastosuj** wyświetla listę zmian, zapisuje je i sprawdza odczytem sterownika.
Po błędzie zapisu program próbuje odtworzyć poprzednie wartości. Eksport
obejmuje także niezastosowane pola; aby wykonać kopię bieżącego stanu
sterownika, najpierw odśwież odczyt klawiszem F5.

## Ograniczenia sprzętu i weryfikacji

**Nie każda karta, integra ani laptop udostępnia regulację.** Radeoner pyta
sterownik o możliwości i ukrywa nieobsługiwane pola oraz puste zakładki regulacji.
Obsługiwane funkcje integry pozostają dostępne. Szczegóły braków są w Raporcie.
Nie odblokowuje BIOS-u ani
kontrolera wentylatorów laptopa. Na lokalnie sprawdzonym Radeon 880M działały
odczyty, lecz sterownik nie udostępniał ręcznej regulacji.

Limit mocy jest procentem względem fabrycznego limitu, **nie TDP w watach**
ani wspólnym limitem CPU/iGPU. Na Navi4+ maksymalny zegar i napięcie mogą
oznaczać przesunięcie względem wartości bazowej. Zmiany parametrów mogą wpływać
na stabilność; aplikacja nie wykonuje testu stabilności.

Testy dostępności obejmują kontrolki WinForms/MSAA i przechwytywanie tekstu
rzeczywistych wypowiedzi NVDA z Podglądu mowy. Nie jest to deklaracja odsłuchu
audio ani weryfikacji każdej konfiguracji czytnika. Zapisy sprawdzono na
symulowanych interfejsach ADLX; fizyczna regulacja na kartach z odblokowanymi
funkcjami wymaga dalszych testów społeczności. Pełny zakres: [stan weryfikacji](docs/VALIDATION.md).

## Aktualizacje

Zakładka **Ustawienia** udostępnia sprawdzanie przy starcie, odstęp **5–10080
minut** (domyślnie 60), **Sprawdź teraz** oraz **Pobierz i zainstaluj**.
Sprawdzanie jest początkowo włączone i można je wyłączyć. Samo sprawdzanie
nie instaluje aktualizacji automatycznie. Nowa wersja otwiera systemowe okno
z opisem zmian. **Tak** pobiera i instaluje; **Nie** odkłada aktualizację.
Przycisk **Wszystkie zmiany** otwiera pełny tekst do odczytu i kopiowania.

Źródłem są najnowsze stabilne [wydania tego repozytorium](https://github.com/proszczok2002/Radeoner/releases).
Program sprawdza SHA-256 ZIP-u i wszystkich plików, zamyka się, podmienia
pliki i uruchamia nową wersję. Poprzedni EXE pozostaje w kopii do potwierdzenia
startu nowego okna. Następnie stary EXE i tymczasowy pomocnik są usuwane.
Jeżeli kandydat nie wystartuje, updater próbuje przywrócić poprzednie pliki.
Profile i `Radeoner.settings.json` są zachowywane. Sterowniki i ustawienia
GPU nie są aktualizowane ani stosowane.

Szczegóły formatu, odzyskiwania i testowania: [aktualizacje / updates](docs/UPDATING.md).
Pełna aktualizacja **1.0 → 1.01 z publicznego GitHuba została sprawdzona**:
[wynik testu i sumy plików](docs/UPDATE_TEST.md).
Także **1.01 → 1.02** przeszło pełny test publicznego kanału:
[wynik testu 1.02](docs/UPDATE_TEST_1.02.md).
Także **1.02 → 1.03** przeszło pełny test aktualizacji z GitHuba:
[wynik testu 1.03](docs/UPDATE_TEST_1.03.md).
Także **1.03 → 1.04** przeszło pełny test publicznego kanału:
[wynik testu 1.04 / update test](docs/UPDATE_TEST_1.04.md).
Także **1.04 → 1.05** przeszło pełny test aktualizacji z GitHuba:
[wynik testu 1.05 / update test](docs/UPDATE_TEST_1.05.md).
Najnowsze **1.05 → 1.06** przeszło lokalny i publiczny test GitHub latest,
z zachowaniem profili i preferencji: [wynik 1.06](docs/UPDATE_TEST_1.06.md).
[Audyt NVDA 1.06](docs/AUDIT_1.06.md): 900 punktów UI, 180 dialogów i 72 traya.

## Klawiatura, tray i pliki

| Skrót | Działanie |
| --- | --- |
| Tab / Shift+Tab | Pola i przyciski |
| Strzałki góra/dół | Wiersze listy odczytów |
| Ctrl+Tab / Ctrl+Shift+Tab | Dostępne zakładki, z pominięciem nieobsługiwanych |
| Ctrl+1 … Ctrl+5 | Odczyty, Regulacja, Wentylator, Raport, Ustawienia |
| F5 | Odczyt ustawień i czujników |
| Ctrl+Enter | Zastosowanie zmian |
| Ctrl+E / Ctrl+I | Eksport / import profilu |
| Ctrl+Shift+S | Zapis raportu |
| F6 / F1 | Tekst ostatniego komunikatu / pomoc |
| Alt+F4 | Ukrycie do traya |

**Zakończ** w oknie lub menu traya kończy cały program. Ponowne uruchomienie
EXE otwiera istniejące okno. Odświeżanie czujników co sekundę jest domyślnie włączone
i działa także w trayu; może podtrzymywać aktywność GPU na baterii.

Preferencje zapisują się obok EXE w `Radeoner.settings.json`. Sześć języków
i instrukcji jest wbudowanych w EXE. Źródła tłumaczeń: `lang/*.lng` (UTF-8,
`klucz=tekst`, `\n` oznacza nową linię). Własny `lang/<kod>.lng` zastępuje
wbudowane teksty. Nowe języki nie wymagają dodatkowych plików w paczce portable,
dzięki czemu aktualizacja ze starszych wersji zachowuje zgodność.
Instrukcja F1 działa offline. Ctrl+1…5 zachowują stałe znaczenie; skrót do
nieobsługiwanej zakładki wybiera Odczyty. Integra nie ma wzmianki o wentylatorze
w trayu; dla pozostałych GPU obroty pojawiają się tylko z poprawnego odczytu,
w tym 0 RPM. RGB nadal wymaga OpenRGB: [stan i dalsze prace](docs/NATIVE_RGB.md).
Motyw Systemowy odczytuje jasny/ciemny wygląd aplikacji Windows.
Wysoki kontrast Windows ma pierwszeństwo przed motywem. Po przeniesieniu
folderu wyłącz i włącz autostart ponownie, aby zaktualizować ścieżkę.

## Kompilacja i współpraca

```powershell
git clone https://github.com/proszczok2002/Radeoner.git
cd Radeoner
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Test
powershell -NoProfile -ExecutionPolicy Bypass -File .\package.ps1
```

Gotowy EXE trafia do `portable/`, a ZIP, `update.json` i sumy SHA-256 do
`artifacts/releases/<wersja>/`. Wykorzystywany jest kompilator dostarczany
z .NET Framework; Visual Studio, `dotnet SDK`, NuGet i SDK AMD nie są potrzebne.
Testy domyślne nie wymagają fizycznej karty AMD i nie zmieniają ustawień GPU.

[CONTRIBUTING.md](CONTRIBUTING.md) opisuje strukturę kodu, testy, tłumaczenia
i zgłaszanie zmian. [CHANGELOG.md](CHANGELOG.md) zawiera historię wydań.

## Licencja i powiązania

Kod aplikacji: [MIT](LICENSE). SDK AMD i pliki sterownika nie są dołączane;
szczegóły: [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
Radeoner jest niezależnym projektem społecznościowym, nie produktem AMD ani Microsoft.

---

**English:** Radeoner is a lightweight, portable Windows AMD GPU panel designed
for keyboard and NVDA accessibility. It offers driver-exposed monitoring and
tuning, JSON profiles, tray readings, personalization and optional GitHub
updates. GPU/firmware support varies. The source is MIT licensed and
contributions are welcome. See the [full English README](README.en.md).

## Autor, wsparcie i kontakt

W **Ustawieniach** dostępne są **O programie** oraz **Wsparcie i kontakt**.
Wsparcie jest dobrowolne; przyciski otwierają przeglądarkę.

- [Strona autora](https://proszczok.pl/)
- [PayPal](https://paypal.me/proszczok)
- [Revolut](https://revolut.me/r_rosiejka)
- [Kontakt na Telegramie](https://t.me/proszczok)

Zakres audytu i lista prób z czytnikiem: [dostępność](docs/ACCESSIBILITY.md).

## Odczyty na żywo i RGB w 1.03

Odczyty i tray odświeżają się co sekundę. Lista zachowuje wybrany wiersz
i pokazuje nowe wartości również z fokusem. Ctrl+A zaznacza wszystkie wiersze,
Ctrl+C kopiuje zaznaczone. Tytuł i tray zawierają numer, np. **Radeoner v1.03**.
Ctrl+Tab i Ctrl+Shift+Tab działają z pól, listy i przycisków, także poza zakładkami.

ADLX działa w osobnym procesie uruchamianym z tego samego EXE. Awaria lub
przekroczenie czasu odpowiedzi pomocnika nie zamyka okna. Pojedynczy błąd
nie wyłącza odświeżania; stare dane są oznaczane, odczyty są ponawiane.
Log lokalny: `Radeoner.log`. Niczego nie wysyłamy automatycznie.
Pomocnik kończy się z aplikacją; nie jest instalowaną usługą.
[Szczegóły stabilności i ograniczenia testu RX 9060](docs/STABILITY.md).

**Regulacja → Podświetlenie karty (RGB)** otwiera standardowe okno z wyborem
karty, trybu, gotowego koloru lub kanałów R/G/B 0–255 oraz jasności 0–100%.
Wymaga uruchomionego **OpenRGB z włączonym lokalnym serwerem SDK** (domyślnie
port 6742). Składnik jest opcjonalny: Radeoner nie instaluje OpenRGB i nie
łączy się z nim bez użycia Połącz. Od **1.04** przycisk **Pobierz OpenRGB**
pobiera oficjalny ZIP 1.1 dla Windows 64-bit w wybrane miejsce, z kontrolą
SHA-256, postępem i anulowaniem. Rozpakuj go i włącz serwer SDK w OpenRGB.
[Instrukcja i ograniczenia RGB / English instructions](docs/RGB.md).

**1.04:** The RGB dialog can download the official OpenRGB 1.1 Windows 64-bit
portable ZIP to a chosen location, with hash verification and cancellation.
Extract it and start OpenRGB with its SDK server enabled before connecting.
