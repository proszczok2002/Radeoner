# Dostępność 1.06 / Accessibility 1.06

Data: 2026-11-14. **Kandydat; rzeczywiste odczyty NVDA 1.06 oczekują na weryfikację.**

## Polski

61 testów regresji przeszło lokalnie. Automatyczna kontrola obejmuje sześć
języków, trzy motywy, tekst 100/200%, nazwy i role MSAA, kontrast, przewijanie
do całego pola z fokusem oraz okna instrukcji i RGB przy dużym tekście.
Sprawdzono 180 kombinacji zakładek oraz 48 kombinacji typu i możliwości GPU.
Obsługiwane funkcje integry nie są ukrywane; nieobsługiwane zakładki są pomijane.

Rzeczywiste Ctrl+Tab/Ctrl+Shift+Tab na lokalnym Radeon 880M, skróty logicznych
zakładek, chowanie/odtwarzanie okna, odczyty w tle oraz natywne O programie
i Wsparcie przeszły. Maksymalny czas utworzenia tych dialogów wyniósł 39 ms;
nie jest to czas czytania przez syntezator. NVDA był uruchomiony.

**Nie przechwycono wypowiedzi NVDA dla 1.06.** Próba potwierdziła brak okna
Podglądu mowy. Użytkownika poproszono o jego otwarcie, bez zatrzymywania czytnika
ani zmiany konfiguracji. Test nazw MSAA i obecność procesu NVDA nie zastępują
sprawdzenia rzeczywistych wypowiedzi. Do czasu tego sprawdzenia kandydat nie
powinien zastępować stabilnego wydania 1.05.

Przygotowany scenariusz `--live-nvda` obejmuje sześć języków, trzy motywy,
tekst 200%, wszystkie pola, pełne instrukcje i RGB oraz ukrywanie i ponowne
udostępnianie funkcji integry. `--nvda-dialogs` obejmuje sześć języków.
`tools/Test-Nvda-Tray.ps1 -Language <kod> [-Integrated]` sprawdza prawdziwą
ikonę Shell, jej odczyt, Enter i menu; kontroluje obecność lub brak RPM.

Lokalne dowody przed porządkami: `artifacts/verified-tests.txt`,
`artifacts/tests/keyboard-1.06.txt`, `artifacts/tests/nvda-1.06.txt`.
Po porządkach ścieżki można odtworzyć z `niepotrzebne/*/INDEKS.csv`.
Poprzedni, ukończony audyt wypowiedzi: [1.05](AUDIT_1.05.md).

## English

This is a candidate, with actual NVDA speech verification pending. All 61
regression tests passed locally. Automated checks cover six languages, three
themes, 100/200% text, MSAA names/roles, contrast, full focus visibility,
180 tab/layout combinations and 48 GPU capability combinations.

Real Ctrl+Tab/Ctrl+Shift+Tab, logical tab shortcuts, tray hide/restore,
background readings and native About/Support dialogs passed on Radeon 880M.
The maximum native-dialog creation time was 39 ms, not synthesizer reading
time. NVDA was running, but Speech Viewer was unavailable. No 1.06 NVDA speech
result or audio listening result is claimed. The user was asked to open the
viewer; NVDA was neither stopped nor reconfigured. Stable 1.05 is retained.

Prepared opt-in tools cover all six languages, the changed integrated-GPU
navigation, native dialogs and the actual Shell tray icon/menu. Hardware tuning
and physical RGB remain unverified on a discrete GPU; no physical tuning was
performed during these checks.
