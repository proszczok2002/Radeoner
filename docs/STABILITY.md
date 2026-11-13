# Stabilność i odczyty / Stability and monitoring — 1.03

## Polski

Zgłoszono zamknięcie 1.02 podczas obciążania RX 9060 oraz zatrzymujące się
odczyty traya. Na lokalnym komputerze jest Radeon 880M; nie było stosu awarii
z RX 9060 i nie odtworzono tego konkretnego przypadku na tej karcie.
Zmiany poniżej ograniczają skutki awarii i poprawiają wykryty problem odświeżania.

- W 1.02 pojedynczy wyjątek zatrzymywał timer czujników. W 1.03 timer działa
  dalej, ostatnie dane są oznaczane jako stare, a połączenie jest ponawiane.
- Domyślny interwał wynosi sekundę. Widoczna lista i tray korzystają z tych
  samych aktualnych próbek; fokus na liście nie zamraża wartości.
- Odczyt w tle pobiera same czujniki. Nie odczytuje co sekundę tabel regulacji,
  napięć, krzywych i zakresów. Niezastosowane wartości pól pozostają zachowane.
- UI nie ładuje ADLX. Wszystkie operacje sterownika są szeregowane w własnym
  procesie `Radeoner.exe --driver-worker`, komunikującym się przez lokalne
  przekierowane strumienie. Nie ma nowej usługi, portu sieciowego ani sterownika.
- Awaria natywnego pomocnika lub brak odpowiedzi kończy żądanie z błędem,
  pozostawiając UI. Limit zwykłego żądania to 6 sekund, inicjalizacji 20 sekund.
  Kolejne połączenie jest możliwe po krótkiej przerwie. Kończone są wyłącznie
  własne procesy pomocnicze. Pomocnik obserwuje zakończenie rodzica.
- Po utracie procesu zapisy są blokowane do ponownego pełnego odczytu ustawień.
  Nie powtarzamy automatycznie niepotwierdzonego zapisu. Po błędzie użyj F5
  i sprawdź wartości; cofnięcie nie zawsze da się potwierdzić po awarii sterownika.

`Radeoner.log` obok EXE zapisuje wersję, czas UTC, operację i szczegóły błędu.
Rozmiar jest ograniczony, powtarzane identyczne błędy są ograniczane czasowo.
Plik nie jest wysyłany do internetu. Nie zawiera przechwytywanej mowy NVDA.

Testy uruchamiają rzeczywisty pomocnik testowy, kończą go w trakcie odczytu
i symulują zawieszenie. Sprawdzają ponowne połączenie, blokadę niepotwierdzonych
zapisów, timeout, pełne przekazanie krzywych/zakresów i zachowanie szkicu pól.
Osobny test UI sprawdza błąd próbki i automatyczne wznowienie bez zatrzymania timera.
Rzeczywiste odczyty i cykl życia pomocnika sprawdzono na 880M, bez zmiany parametrów.
Nie jest to test stabilności pod obciążeniem fizycznej RX 9060 ani gwarancja
odporności na awarię całego systemowego sterownika grafiki.

## English

The user reported a 1.02 crash under RX 9060 load and stalled tray readings.
Only a Radeon 880M was available locally; no RX 9060 crash stack was available
and that exact hardware/load failure was not reproduced.

1.03 keeps the timer running after a read exception, marks last values stale
and retries. Default polling is one second, including while the sensor list
is focused. Polling reads metrics only; tuning tables and draft edits are not
reloaded every tick. UI and tray use the same samples.

ADLX is isolated in an owned child of the same EXE, with serialized requests
over redirected local streams. No service, network port or extra driver is
installed. Child crashes/timeouts leave the UI open. Normal requests allow
6 seconds, startup 20 seconds; reconnects have a short backoff. Only owned
helpers are terminated, and they monitor their parent. Writes remain blocked
after connection loss until a full settings read; unconfirmed writes are not
replayed automatically. Use F5 and review current values after a failed write.

Bounded `Radeoner.log` stays local and throttles repeated errors. Controlled
real-process tests cover crash, hang, reconnect, write blocking and complete
snapshot/curve transfer. UI tests cover transient-error recovery and retained
drafts. Real read-only operation/helper lifetime were checked on 880M. Physical
RX 9060 load stability and system-wide driver failures remain outside this verification.
