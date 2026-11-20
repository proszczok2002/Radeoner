# AMD drivers and GPU support

## English

**Driver Only is the recommended AMD installation type for Radeoner.** Download
the correct package for your GPU and Windows version. In the AMD installer,
open **Additional Options** and choose **Driver Only**. The installer can still
be called AMD Software: Adrenalin Edition. Driver Only installs the display
driver without the Adrenalin control panel. If you keep the panel, use one
application for tuning and turn off automatic profiles or tuning in the other.
[AMD installer guide](https://www.amd.com/en/resources/support-articles/faqs/RSX2-INSTALL.html)

For a normal update, leave **Factory Reset** off. For a clean reinstall or
troubleshooting, consider Factory Reset if the installer offers it. AMD says it
removes previous AMD Software packages, temporarily pauses Windows Update,
restarts the PC and removes the ability to roll back to the previous driver.
Laptop and prebuilt PC owners should also check the manufacturer's driver and
release notes. [AMD Auto-Detect and installation options](https://www.amd.com/en/resources/support-articles/faqs/gpu-131.html)

| Layer | Role |
| --- | --- |
| Windows display device and Driver Store | Windows selects the applicable signed package using hardware IDs, INF matching and driver rank. |
| AMD display driver | Provides the actual Windows graphics support and publishes the installed driver version. |
| ADLX, `amdadlx64.dll` | Radeoner loads it from System32 in a helper process and queries each GPU for monitoring and tuning. |
| Legacy ADL, `atiadlxx.dll` | A separate AMD interface with different Overdrive generations. Its presence alone does not confirm features on a GPU. |
| Adrenalin control panel | Optional UI in AMD's Default/Minimal install types. |
| OpenRGB | Separate lighting backend with its own device support and SDK connection. |

The local read-only inventory found a signed Radeon 880M display driver
32.0.32015.12012, ADLX 1.5.0.124 and ADL 7.26.20.1616. This describes the
tested machine and driver. On other cards, the driver, board firmware and cooling
controller determine what Radeoner can show. Current ADLX controls include clocks,
voltage or offsets, percentage power limit, and supported fan settings. Some
older ADLX cards already expose adjustable GPU and VRAM performance states.
If ADLX initialization fails, Windows inventory can still list AMD adapters and
their installed driver versions.

**A driver updater is feasible as a guided AMD workflow.** Start by showing the
installed driver and linking to AMD Auto-Detect for supported GPUs or AMD's
product selector for legacy GPUs. Leave final package selection and Driver Only
installation to AMD or the PC manufacturer. Windows Update is a separate route
with its own device targeting. A future integrated update check would need
reliable GPU, subsystem, OS and OEM matching plus signature verification. The
current Radeoner updater services the portable application.
[AMD Auto-Detect and legacy selector](https://www.amd.com/en/resources/support-articles/faqs/gpu-131.html) ·
[Microsoft driver package selection](https://learn.microsoft.com/en-us/windows-hardware/drivers/install/overview-of-the-driver-selection-process)

**Older cards and more controls.** AMD documents reduced ADLX functionality on
some legacy drivers and a separate initialization path that does not cover all
devices. On cards outside ADLX, evaluate a read-only ADL fallback first by
querying `ADL2_Overdrive_Caps` per adapter and supporting only verified
Overdrive generations. A real legacy card and its last supported driver are
needed to confirm sensor units, valid ranges and safe readback. ADLX also
documents preset tuning and a percentage-based TDC limit. AMD calls for
application validation of TDC, so both are candidates for testing on hardware.
[ADLX compatibility](https://gpuopen.com/manuals/adlx/programming-with-adlx/adlx-programming-guide/specifications/compatibility/) ·
[ADL Overdrive capabilities](https://gpuopen-librariesandsdks.github.io/adl/group__OVERDRIVE5API.html) ·
[ADLX GPU tuning interfaces](https://gpuopen.com/manuals/adlx/adlx-sdk-references/adlx-interfaces/gpu-tuning/) ·
[AMD TDC validation note](https://gpuopen.com/manuals/adlx/adlx-sdk-references/adlx-interfaces/gpu-tuning/iadlxmanualpowertuning/settdclimit/)

## Polski

**Do Radeonera zalecamy instalację Driver Only.** Pobierz paczkę właściwą
dla karty i wersji Windows, a w instalatorze AMD wybierz **Opcje dodatkowe →
Driver Only (Tylko sterownik)**. Paczka może mieć nazwę Adrenalin Edition.
W tym trybie AMD instaluje sterownik grafiki bez panelu Adrenalin. Jeśli
zostawiasz panel, strojenie wykonuj w jednym programie. W drugim wyłącz
automatyczne profile lub strojenie.
[Instrukcja instalatora AMD](https://www.amd.com/en/resources/support-articles/faqs/RSX2-INSTALL.html)

Przy zwykłej aktualizacji zostaw **Factory Reset** wyłączony. To opcja przy
czystej reinstalacji lub rozwiązywaniu problemów. Usuwa poprzednie paczki AMD,
tymczasowo wstrzymuje Windows Update, uruchamia komputer ponownie i wyłącza
możliwość powrotu do poprzedniej wersji sterownika. W laptopach i gotowych
komputerach sprawdź też pakiet i zalecenia producenta.
[AMD Auto-Detect i opcje instalacji](https://www.amd.com/en/resources/support-articles/faqs/gpu-131.html)

Windows dopasowuje podpisany pakiet sterownika do identyfikatorów sprzętu i
plików INF. Radeoner korzysta z ADLX z System32 we własnym procesie pomocniczym.
Starsze ADL jest osobnym API z kilkoma generacjami Overdrive. OpenRGB to osobna
warstwa podświetlenia. Lokalny odczyt wykazał podpisany sterownik Radeon 880M
32.0.32015.12012, ADLX 1.5.0.124 i ADL 7.26.20.1616.

**Aktualizator sterownika można dodać jako pomoc w uruchomieniu oficjalnego
instalatora.** Dla wspieranych kart AMD oferuje Auto-Detect, a dla starszych
ręczny wybór produktu. Na laptopie źródłem może być producent. Zintegrowane
sprawdzanie wymagałoby wiarygodnego dopasowania karty, podsystemu, wersji
Windows i pakietu OEM, a także weryfikacji podpisu. Aktualizator w Radeonerze
obsługuje obecnie aplikację.
[Sterowniki AMD](https://www.amd.com/en/support/download/drivers.html) ·
[Jak Windows wybiera pakiet](https://learn.microsoft.com/en-us/windows-hardware/drivers/install/overview-of-the-driver-selection-process)

**Starsze karty i nowe ustawienia.** Dla starszych kart z ADLX Radeoner już
obsługuje stany wydajności GPU i VRAM, jeśli sterownik je udostępnia. Gdy ADLX
nie startuje, odczyt Windows identyfikuje kartę i wersję sterownika. Kolejnym
etapem może być odczyt czujników przez ADL. Weryfikacja obejmie wersję Overdrive,
jednostki i zakresy na fizycznej karcie z jej ostatnim zgodnym sterownikiem.
Układy z nowszym ADLX mogą udostępniać dodatkowe tryby strojenia i limit prądu
TDC w procentach. AMD zaleca zweryfikować działanie TDC w aplikacji na sprzęcie.
[Zgodność ADLX](https://gpuopen.com/manuals/adlx/programming-with-adlx/adlx-programming-guide/specifications/compatibility/) ·
[Starsze API Overdrive](https://gpuopen-librariesandsdks.github.io/adl/group__OVERDRIVE5API.html) ·
[Limit TDC](https://gpuopen.com/manuals/adlx/adlx-sdk-references/adlx-interfaces/gpu-tuning/iadlxmanualpowertuning/settdclimit/)
