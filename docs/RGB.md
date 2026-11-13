# Podświetlenie GPU / GPU lighting — 1.03

## Polski

Otwórz **Regulacja → Podświetlenie karty (RGB)**. Funkcja korzysta z
opcjonalnego, zewnętrznego OpenRGB. Radeoner nie instaluje go, nie uruchamia
automatycznie i nie dodaje sterowników do obsługi podświetlenia.

1. Uruchom [OpenRGB](https://openrgb.org/) i włącz jego serwer SDK na tym samym komputerze.
2. W Radeonerze podaj port (domyślnie **6742**) i wybierz **Połącz z OpenRGB**.
3. Wybierz kartę z listy **Karta RGB**. Lista zawiera tylko urządzenia typu GPU.
4. Wybierz tryb obsługujący kolor, gotowy kolor albo R/G/B od 0 do 255.
5. Ustaw jasność 0–100% i wybierz **Zastosuj RGB**. **Wyłącz podświetlenie** lub 0% gasi światło.

Tab/Shift+Tab przechodzą po kontrolkach. Ctrl+Enter stosuje RGB, F6 odczytuje
stan, Escape zamyka okno. Otworzenie okna i samo Połącz nie zmieniają świateł.
Zamknięcie przerywa trwającą komunikację. Nie ma zapisu RGB przy starcie.

Jasność używa zakresu zgłoszonego przez OpenRGB, gdy wybrany tryb ją obsługuje.
W przeciwnym razie program skaluje kanały wybranego koloru. Wyłączenie wybiera
tryb Off lub ustawia czarny kolor, gdy karta udostępnia ręczny kolor.
Nie wszystkie karty i tryby mają te same funkcje. Nazwy trybów pochodzą z OpenRGB.

Obsługa TUF RX 9070 XT oraz innych modeli zależy od używanej wersji OpenRGB.
Bieżący kod wykrywania upstream zawiera TUF RX 9070 XT:
[detektor ENE](https://github.com/CalcProgrammer1/OpenRGB/blob/master/Controllers/ENESMBusController/ENESMBusControllerDetect.cpp).
To informacja o kodzie OpenRGB, **nie potwierdzenie fizycznego testu tej karty w Radeonerze**.
Sprawdź też [listę urządzeń OpenRGB](https://openrgb.org/devices.html).

Radeoner implementuje niezależnego klienta
[protokołu SDK](https://github.com/CalcProgrammer1/OpenRGB/blob/master/Documentation/OpenRGBSDK.md),
negocjując wersję 3. Łączy się wyłącznie z **127.0.0.1**, odczytuje
możliwości i ponownie sprawdza tożsamość karty przed zapisem. Zmiana listy lub
tożsamości przerywa operację. Odczyt zwrotny sprawdza stan zgłoszony przez
OpenRGB, a nie fizyczny pomiar światła. Inne typy urządzeń nie otrzymują zmian.

Testy obejmują prawdziwy TCP na loopback z kontrolowanym serwerem SDK:
format pakietów, filtrowanie GPU, kolor per LED i specyficzny dla trybu,
jasność sprzętową, przyciemnianie koloru, Off/czarny oraz odrzucenie zmienionej
karty i błędnych pakietów. **Lokalnie nie wykonano fizycznego testu RGB GPU.**
Profile JSON Radeonera dotyczą ADLX; nie przechowują obecnie RGB.

## English

Open **Tuning → GPU lighting (RGB)**. This optional feature requires external
[OpenRGB](https://openrgb.org/) with its SDK server enabled on the same computer.
Radeoner does not install/start OpenRGB or bundle lighting drivers.

Connect on port **6742** (or your configured port), select an RGB GPU and
color-capable mode, choose a preset or R/G/B 0–255, set brightness 0–100%,
then **Apply RGB**. **Turn lighting off** or 0% disables lighting. Opening
the dialog or connecting makes no lighting changes. Tab/Shift+Tab navigates,
Ctrl+Enter applies, F6 focuses status, Escape closes and cancels communication.

Reported hardware brightness is used when available; otherwise RGB channels
are scaled. Off selects the Off mode or sends black through a color-capable
mode. Capabilities and mode names come from OpenRGB. Model support, including
TUF RX 9070 XT, depends on its version; the upstream detector linked above
contains this model, but no physical RGB GPU was tested locally with Radeoner.

The independent SDK client negotiates protocol 3 and connects only to
**127.0.0.1**. Only GPU devices are listed. Identity and device-list changes
abort writes. Readback validates OpenRGB's reported state, not physical light.
Loopback TCP tests cover framing, capabilities, per-LED/mode color, hardware
and software brightness, off/black and rejection of changed devices/bad packets.
ADLX JSON profiles do not currently include RGB; nothing is applied at startup.
