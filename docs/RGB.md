# GPU lighting / Podświetlenie GPU

## Polski

Otwórz **Regulacja → Podświetlenie karty (RGB)**. Funkcja korzysta z
opcjonalnego, zewnętrznego OpenRGB. Radeoner nie instaluje go, nie uruchamia
automatycznie i nie dodaje sterowników do obsługi podświetlenia.

1. Jeśli nie masz OpenRGB, wybierz **Pobierz OpenRGB 1.1 (Windows 64-bit)**.
   W systemowym oknie zapisu wybierz miejsce na ZIP. Pobieranie pokazuje postęp;
   **Anuluj pobieranie** lub zamknięcie okna przerywa transfer. Po zakończeniu
   rozpakuj ZIP, uruchom OpenRGB.exe i włącz serwer SDK na tym komputerze.
   Możesz też skorzystać z [oficjalnych wydań](https://openrgb.org/releases.html).
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
możliwości i ponownie sprawdza tożsamość karty przed zapisem. Zmiana listy
podczas wykrywania może uruchomić ponowne połączenie i odczyt świeżej listy.
Przy zmianie listy lub tożsamości podczas stosowania koloru operacja kończy się
komunikatem. Opóźnione potwierdzenie powoduje powtórny odczyt stanu.
Odczyt zwrotny sprawdza stan zgłoszony przez
OpenRGB, a nie fizyczny pomiar światła. Inne typy urządzeń nie otrzymują zmian.

Testy obejmują prawdziwy TCP na loopback z kontrolowanym serwerem SDK:
format pakietów, filtrowanie GPU, kolor per LED i specyficzny dla trybu,
jasność sprzętową, przyciemnianie koloru, Off/czarny oraz odrzucenie zmienionej
karty i błędnych pakietów. Sprawdzono też pobranie oficjalnego OpenRGB 1.1
oraz połączenie z jego serwerem SDK przy wyłączonym wykrywaniu sprzętu.
Fizyczny test podświetlenia wymaga karty z RGB.
Profile JSON Radeonera dotyczą ADLX; nie przechowują obecnie RGB.

## English

Open **Tuning → GPU lighting (RGB)**. This optional feature requires external
[OpenRGB](https://openrgb.org/) with its SDK server enabled on the same computer.
Radeoner does not install/start OpenRGB or bundle lighting drivers.

If needed, choose **Download OpenRGB 1.1 (Windows 64-bit)** and select a ZIP
destination in the native Save dialog. Status shows progress; **Cancel download**
or closing the window cancels it. Extract the downloaded ZIP, run OpenRGB.exe
and enable its SDK server. The app downloads but does not install/run OpenRGB.

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
**127.0.0.1**. Only GPU devices are listed. If OpenRGB changes its list during
discovery, Radeoner reconnects up to twice and reads a fresh list. Changes
reported during an Apply operation stop it. If confirmation lags, Radeoner
repeats the read up to twice. Each lighting command is sent once per Apply.
Readback validates the state reported by OpenRGB.
Loopback TCP tests cover framing, capabilities, per-LED/mode color, hardware
and software brightness, off/black and rejection of changed devices/bad packets.
The official OpenRGB 1.1 download and SDK connection also passed with hardware
detection disabled. Lighting writes were tested on a simulated GPU.
ADLX JSON profiles do not currently include RGB; nothing is applied at startup.

## Troubleshooting / Rozwiązywanie problemów

| Result | Next check |
| --- | --- |
| Server unavailable / Brak serwera | Start the OpenRGB SDK server and use the port it shows. If OpenRGB is restarting, wait for it to finish. Uruchom serwer SDK w OpenRGB i sprawdź port. |
| Connected, no RGB GPUs / Połączono, brak kart | Check whether the card appears in OpenRGB itself, then reconnect after its scan. Sprawdź, czy karta jest na liście w samym OpenRGB. |
| Device list changed / Lista się zmieniła | Reconnect and select your card again. Połącz się ponownie i wybierz kartę. |
| Change unconfirmed / Brak potwierdzenia | Check the card's current color in OpenRGB before applying again. Sprawdź aktualny kolor w OpenRGB przed kolejną próbą. |

For an RGB report, include the exact card model, OpenRGB version, SDK port,
whether the GPU appears in OpenRGB, the Radeoner message and the action that
triggered it. These details distinguish a connection failure from missing
hardware support. [OpenRGB releases and supported devices](https://openrgb.org/releases.html)

## Paczka pobierania / Download provenance

- Official release: [OpenRGB 1.1](https://codeberg.org/OpenRGB/OpenRGB/releases/tag/release_1.1),
  linked by [openrgb.org/releases.html](https://openrgb.org/releases.html).
- ZIP: `OpenRGB_1.1_Windows_64_8e16de2.zip`, 22,019,605 bytes.
- SHA-256: `63291726c6fc84eb479ef2ee752e97e1187f6ce9c49fd27eee40ded588a8172a`.
- Hash independently calculated from the official HTTPS download on 2026-11-14;
  it is a pinned integrity check, not a publisher signature. The official API
  did not expose a checksum for this asset. Updating the pin needs source review.
- HTTPS to the fixed official Codeberg URL, no redirects, five-minute transfer
  limit. No OpenRGB binary is bundled or mirrored in Radeoner releases.
- Failed size/hash checks, cancellation and HTTP errors preserve an existing
  destination. Temporary `.part` files are removed. The user confirms overwrite
  using the native Save dialog before a verified file replaces the destination.
- Tests cover valid replacement/new files, invalid hash, truncated/oversized
  content without a length header, HTTP errors, redirects and cancellation.
