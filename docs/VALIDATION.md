# Stan weryfikacji / Validation status

## Polski

- 34 testy automatyczne: interfejsy ADLX w pamięci, walidacja/zapis/cofanie,
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
- Wynik testu publicznego kanału zostanie dopisany po opublikowaniu obu wydań.

Pozostają: odsłuch w prawdziwej sesji NVDA, fizyczne zapisy na kartach z
odblokowaną regulacją i szersze testy wielu GPU/sterowników/laptopów. Zgłoszenia
społeczności powinny podawać model GPU, wersję sterownika, aplikacji i NVDA
oraz rzeczywiste zachowanie. Nie traktuj samego istnienia kontrolki jako
potwierdzenia działania funkcji na każdej karcie.

## English

34 automated tests cover in-memory ADLX interfaces, validation/readback/rollback,
JSON profiles, WinForms/MSAA, preferences/LNG, an isolated startup registry key,
the update feed, checksums, archives, backups and failed-candidate rollback.
Default tests never tune a physical GPU.

Read-only hardware testing used a Radeon 880M, driver 32.0.32015.12012 and ADLX
1.5.0.124. Usage, clocks, temperature, power and memory reads worked. That
driver exposed no manual tuning for this integrated GPU. WinForms startup,
MSAA names, tray hide/restore and background reads were checked without tuning.

A local 1.0 → 1.01 integration test passed real executable replacement, new
window initialization, old EXE/helper cleanup and preserved preference/profile
hashes. The public-feed result will be added after both releases are published.

Outstanding: an actual NVDA listening session, physical writes on GPUs exposing
tuning, and wider GPU/driver/laptop coverage. Please report hardware, driver,
app/NVDA versions and observed behavior. The presence of a control does not
establish universal hardware support.
