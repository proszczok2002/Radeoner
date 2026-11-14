# Stan weryfikacji / Validation status

Kandydat 1.06: 61 testów lokalnych, sześć języków i lokalny updater PASS;
rzeczywiste wypowiedzi NVDA oczekują na otwarcie Podglądu mowy.
[Audyt 1.06](AUDIT_1.06.md) · [lokalny update 1.05 → 1.06](UPDATE_TEST_1.06.md).
Ukończone testy rzeczywistych wypowiedzi i publicznego updatera stabilnej 1.05:
[NVDA 1.05](AUDIT_1.05.md) · [update 1.05](UPDATE_TEST_1.05.md).

Candidate 1.06: 61 local tests and local update passed; actual NVDA speech
verification awaits Speech Viewer. The reports linked above distinguish these
pending checks from completed stable-1.05 validation. Older notes follow.

## Polski

- 1.03: jawne Ctrl+Tab/Ctrl+Shift+Tab, odczyty co sekundę i wersja w tytule/trayu.
  Widoczny test rzeczywistych klawiszy oraz okien przeszedł z aktywnym NVDA 2026.3.1;
  mowy nie przechwycono. [Zakres audytu](ACCESSIBILITY.md).
- Awaria i zawieszenie własnego procesu sterownika zostały sprawdzone przez
  kontrolowane rzeczywiste procesy. Test błędu próbki potwierdza ponowienie bez
  zatrzymania timera i zachowanie szkicu. [Stabilność, ograniczenia RX 9060](STABILITY.md).
- RGB sprawdzono na serwerze testowym TCP z kontrolą pakietów; nie na fizycznym
  podświetleniu GPU. [Obsługa RGB i wymaganie OpenRGB](RGB.md).
- Publiczny update **1.02 → 1.03** przeszedł pełną podmianę, start okna i
  pomocnika ADLX, sprzątanie oraz kontrolę sum profilu/preferencji.
  [Wynik i sumy](UPDATE_TEST_1.03.md). CI również przeszło 50 testów.
- Audyt 1.02: pięć zakładek, nazwy edycji liczbowych, odtworzenie fokusu
  po wymianie pól, zaznaczenie raportu, Ctrl+1…5 i motyw systemowy przeszły
  kontrolę. Rzeczywiste okna TaskDialog udostępniają treść zmian oraz przyciski
  Tak/Nie, informacji i wsparcia w MSAA. Pełny zakres i próby NVDA:
  [audyt dostępności](ACCESSIBILITY.md).
- Publiczna aktualizacja **1.01 → 1.02** przeszła pełną podmianę, start nowego
  okna i sprzątanie kopii, zachowując profil i preferencje. [Wynik](UPDATE_TEST_1.02.md).
- 50 testów automatycznych: interfejsy ADLX w pamięci, walidacja/zapis/cofanie,
  JSON profili, WinForms/MSAA, preferencje, LNG, autostart w kluczu testowym,
  kanał aktualizacji, sumy, archiwa, kopie i wycofanie po nieudanym starcie.
- Domyślne testy nie zapisują żadnych parametrów rzeczywistego GPU.
- Odczyty sprawdzono na Radeon 880M, sterownik 32.0.32015.12012, ADLX 1.5.0.124.
  Działały obciążenie, zegary, temperatura, moc i zajęta pamięć. Ten sterownik
  nie udostępniał ręcznych regulacji tej integry.
- Sprawdzono uruchomienie WinForms, nazwy MSAA, chowanie i przywracanie z traya
  oraz odczyty w tle, bez zmian GPU.
- Lokalny test 1.0 → 1.01 przeszedł przez rzeczywistą podmianę, start nowego
  okna, usunięcie starego EXE/pomocnika i kontrolę sum preferencji/profile.
- Pełny test publicznego kanału GitHub **1.0 → 1.01** przeszedł 2026-11-13:
  pobranie przez updater, SHA-256, rzeczywista podmiana EXE, start nowego
  okna i usunięcie starego EXE/pomocnika. Preferencje i profil zachowały sumy.
  Szczegóły: [wynik testu aktualizacji](UPDATE_TEST.md).

Pozostają: odsłuch w prawdziwej sesji NVDA, fizyczne zapisy na kartach z
odblokowaną regulacją i szersze testy wielu GPU/sterowników/laptopów. Zgłoszenia
społeczności powinny podawać model GPU, wersję sterownika, aplikacji i NVDA
oraz rzeczywiste zachowanie. Nie traktuj samego istnienia kontrolki jako
potwierdzenia działania funkcji na każdej karcie.

## English

1.03 passed explicit Ctrl+Tab/Ctrl+Shift+Tab routing, one-second live readings
and version labels. A visible real-keyboard/dialog test passed with NVDA 2026.3.1
running; speech was not captured. [Audit](ACCESSIBILITY.md).
Controlled real child-process crashes/hangs and transient sensor errors passed
recovery/draft-preservation checks. [RX 9060 verification limits](STABILITY.md).
RGB uses a strict loopback TCP fixture, not a physical RGB GPU. [RGB details](RGB.md).
The public **1.02 → 1.03** update passed replacement, window/ADLX-child startup,
cleanup and preserved-data checksums. [Evidence](UPDATE_TEST_1.03.md).
GitHub Actions also passed all 50 tests.

The 1.02 audit passed five-tab navigation, numeric edit names, focus after
field replacement, report selection, Ctrl+1…5 and system-theme checks. Actual
TaskDialog windows expose release notes, Yes/No and information/support
buttons through MSAA. See the [audit and NVDA checklist](ACCESSIBILITY.md).
The public **1.01 → 1.02** update passed replacement, new startup and cleanup
with preserved preferences/profile. [Evidence](UPDATE_TEST_1.02.md).

50 automated tests cover in-memory ADLX interfaces, validation/readback/rollback,
JSON profiles, WinForms/MSAA, preferences/LNG, an isolated startup registry key,
the update feed, checksums, archives, backups and failed-candidate rollback.
Default tests never tune a physical GPU.

Read-only hardware testing used a Radeon 880M, driver 32.0.32015.12012 and ADLX
1.5.0.124. Usage, clocks, temperature, power and memory reads worked. That
driver exposed no manual tuning for this integrated GPU. WinForms startup,
MSAA names, tray hide/restore and background reads were checked without tuning.

A local 1.0 → 1.01 integration test passed real executable replacement, new
window initialization, old EXE/helper cleanup and preserved preference/profile
hashes. The full public GitHub-feed **1.0 → 1.01** test also passed on
2026-11-13: download through the app's updater, SHA-256 validation, real EXE
replacement, new window initialization and old EXE/helper cleanup. Preference
and profile checksums were unchanged. See [update test evidence](UPDATE_TEST.md).

Outstanding: an actual NVDA listening session, physical writes on GPUs exposing
tuning, and wider GPU/driver/laptop coverage. Please report hardware, driver,
app/NVDA versions and observed behavior. The presence of a control does not
establish universal hardware support.
