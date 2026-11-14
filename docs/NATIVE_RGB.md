# Niezależne RGB / Independent RGB

## Polski

Wersja 1.06 nadal steruje podświetleniem przez opcjonalny lokalny OpenRGB.
Nie zawiera natywnego sterownika kontrolerów RGB. Pobranie OpenRGB z programu
nie jest równoznaczne z niezależną obsługą sprzętu.

Bezpośrednia obsługa wymaga identyfikacji producenta karty, podsystemu PCI,
kontrolera podświetlenia i jego protokołu. Sama nazwa „Radeon RX 9070 XT”
nie wystarcza do wyboru zgodnej implementacji. Lista detektorów OpenRGB zawiera
ASUS TUF Radeon RX 9070 XT w rodzinie ENE SMBus. Publiczne AMD ADL udostępnia
operacje I2C, lecz samo istnienie API nie potwierdza dostępu do danego kontrolera
na konkretnym modelu i sterowniku Windows.

Kolejny etap wymaga pełnego modelu i testowej karty: identyfikacja bez zmiany
świateł, sprawdzenie transportu, ograniczona lista zgodnych kontrolerów,
testy pakietów i błędów oraz dopiero potem weryfikacja koloru, jasności i
wyłączenia na sprzęcie. Nie wdrożono zgadywanych zapisów do magistrali.
Lokalna integra Radeon 880M nie pozwala potwierdzić działania RGB na TUF.

Kod Radeonera pozostaje MIT. Nie skopiowano kodu GPL z OpenRGB do aplikacji.
Dialog RGB pozostaje dostępny z klawiatury i podlega audytowi NVDA razem z
pozostałymi oknami.

## English

Version 1.06 still uses optional local OpenRGB for GPU lighting. It does not
include a native RGB controller backend. Downloading OpenRGB from Radeoner
does not make hardware access independent of it.

Direct support needs the board manufacturer, PCI subsystem identity, lighting
controller and protocol. OpenRGB lists ASUS TUF Radeon RX 9070 XT in its ENE
SMBus detectors. AMD ADL exposes I2C operations; API availability alone does
not prove access to a particular controller with a particular Windows driver.

The next step needs an exact board model and test hardware: identify without
changing lights, verify transport, allow only known controllers, test packets
and failures, then verify color, brightness and off on real hardware. No guessed
bus writes were shipped. The local Radeon 880M cannot validate TUF lighting.
Radeoner remains MIT; no OpenRGB GPL code was copied into the application.

## Źródła / Primary sources

- [AMD ADL I2C, DDC and EDID APIs](https://gpuopen-librariesandsdks.github.io/adl/group__I2CDDCEDIDAPI.html)
- [OpenRGB ENE SMBus detectors](https://github.com/CalcProgrammer1/OpenRGB/blob/master/Controllers/ENESMBusController/ENESMBusControllerDetect.cpp)
- [Current OpenRGB integration](RGB.md)
