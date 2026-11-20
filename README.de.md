# Radeoner

**1.07** aktualisiert AMD-Treiber über Windows Update und verbessert die Erkennung nach einem erneuten OpenRGB-Scan. [Änderungen](docs/releases/v1.07.md#deutsch)

Ein leichtes, portables AMD-GPU-Panel für Windows mit nativen Windows-Forms-
Steuerelementen, Tastaturbedienung und Unterstützung für NVDA.

[Polski](README.pl.md) · [English](README.md) · **Deutsch** ·
[Русский](README.ru.md) · [Español](README.es.md) · [Français](README.fr.md)

## Herunterladen und starten

1. Das portable ZIP aus den [Veröffentlichungen](https://github.com/proszczok2002/Radeoner/releases/latest) herunterladen.
2. Alle Dateien in einen beschreibbaren Ordner entpacken.
3. **Radeoner.exe** starten. Ein Installationsprogramm ist nicht erforderlich.

Benötigt werden Windows 10/11 x64, Microsoft .NET Framework 4.8 und ein AMD-
Treiber mit ADLX. Eine Driver-Only-Installation genügt, wenn sie ADLX enthält.
Das Adrenalin-Bedienfeld wird nicht benötigt. Die Treiberbibliothek wird nur
aus System32 geladen. Radeoner installiert keinen eigenen GPU-Kerneltreiber.

Für Radeoner empfehlen wir **Driver Only** unter den zusätzlichen Optionen des AMD-Installers. Wer das Adrenalin-Bedienfeld behält, sollte die GPU nur mit einer Anwendung einstellen. Ein normaler Treiberwechsel benötigt keinen Factory Reset. Bei einer sauberen Neuinstallation entfernt Factory Reset alte AMD-Pakete und verhindert die Rückkehr zum vorherigen Treiber. Bei Notebooks auch den Hersteller-Treiber prüfen.
[AMD-Treiber und ältere GPUs](docs/AMD_DRIVERS.md) · [AMD](https://www.amd.com/en/resources/support-articles/faqs/RSX2-INSTALL.html)

Wählen Sie Ihre GPU und öffnen Sie **Einstellungen → Aktualisierungen → AMD-Treiber aktualisieren**. Suchen Sie in Windows Update, wählen Sie einen angebotenen Treiber, lesen Sie bei Bedarf die Lizenzbedingungen und wählen Sie **Treiber installieren**. Windows fordert Administratorrechte an und installiert das Paket aus dem Systemkatalog. Radeoner meldet das Ergebnis und einen erforderlichen Neustart. Der angebotene Treiber kann von der neuesten Version auf der AMD-Website abweichen.

## Funktionen und Version 1.06

- Messwerte werden standardmäßig jede Sekunde aktualisiert, auch im Infobereich.
- Taktraten, Spannung, Leistungsgrenze, Lüfterkurven und Zero RPM stehen nur
  bereit, wenn der Treiber sie unterstützt. Unterstützte Funktionen einer
  integrierten GPU bleiben erhalten; leere Registerkarten werden ausgeblendet.
- Bei integrierten GPUs nennt das Infobereichssymbol keinen Lüfter. Bei anderen
  GPUs erscheint die Drehzahl nur mit gültigem Messwert; 0 RPM bleibt gültig.
- Profile lassen sich als JSON sichern und wiederherstellen. Import füllt nur
  die Felder aus; erst **Anwenden** mit Bestätigung schreibt zur GPU.
- Sechs Sprachen, System-/Hell-/Dunkel-Design und Textgröße 100–200%.
- **F1** öffnet das vollständige übersetzte Handbuch ohne Internetzugang.
- Optionale Start- und Intervallprüfungen für Updates; Änderungen werden vor
  der Installation angezeigt. Profile und Einstellungen bleiben erhalten.

RGB verwendet weiterhin einen optionalen lokalen **OpenRGB-SDK-Server**.
Radeoner kann dessen geprüftes offizielles ZIP herunterladen; Entpacken,
Starten und Aktivieren des SDK-Servers erfolgen separat. Eine unabhängige
Hardwareimplementierung ist in 1.06 nicht enthalten.

## Tastatur und NVDA

| Taste | Funktion |
| --- | --- |
| Tab / Umschalt+Tab | Nächstes / vorheriges Feld |
| Strg+Tab / Strg+Umschalt+Tab | Nächste / vorherige verfügbare Registerkarte |
| Strg+1 bis Strg+5 | Messwerte, Regelung, Lüfter, Bericht, Einstellungen |
| F5 / F6 / F1 | Neu lesen / Status auswählen / Hilfe |
| Strg+E / Strg+I | Profil exportieren / importieren |
| Strg+Eingabe | Änderungen anwenden |
| Strg+Umschalt+S | Bericht speichern |
| Alt+F4 | In den Infobereich ausblenden |

Ein nicht verfügbares Strg+1…5-Ziel öffnet Messwerte. Zum vollständigen Beenden
**Beenden** verwenden. Das Symbol ist über Windows+B und Eingabe erreichbar.
In der Messwertliste wählen Pfeile Zeilen; Strg+A und Strg+C kopieren Werte.

## Grenzen, Dateien und Mitarbeit

Eine prozentuale Leistungsgrenze ist keine TDP in Watt und kein gemeinsames
CPU/iGPU-Limit. Manche Navi4-Werte sind Versätze; die Feldbeschreibung beachten.
Radeoner entsperrt keine Firmware und steuert nicht universell Laptoplüfter.
Physische Schreibzugriffe und RGB müssen auf den jeweiligen Karten geprüft
werden. Automatische Tests ersetzen keine tatsächliche NVDA-Sprachausgabe.
Der aktuelle [Prüfbericht](docs/AUDIT_1.06.md) unterscheidet diese Nachweise.

Alle sechs Übersetzungen und Handbücher sind in der EXE enthalten. Eigene
UTF-8-Dateien `lang/<Sprachcode>.lng` können Texte ersetzen. Die portable
Acht-Dateien-Struktur bleibt mit dem Updater 1.05 kompatibel. Einstellungen
stehen neben der EXE in `Radeoner.settings.json`; GPU-Profile werden separat
exportiert und beim Start nicht automatisch angewendet.

Das öffentliche Update 1.05 → 1.06 wurde mit unveränderten Profil-/Einstellungs-
Prüfsummen bestätigt: [Prüfbericht](docs/UPDATE_TEST_1.06.md).

[Vollständiges Handbuch](Readme.de.txt) · [Änderungen 1.06](docs/releases/v1.06.md#deutsch)

Der Quellcode steht unter der **MIT-Lizenz**. Für einen lokalen Build:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Test
```

[Fehler melden](https://github.com/proszczok2002/Radeoner/issues) oder Pull Request
einreichen; dabei GPU-Modell sowie Windows-, Treiber-, Programm- und NVDA-Version
nennen. NVDA nicht allein anhand von MSAA-Namen als geprüft erklären.

Autor: [proszczok.pl](https://proszczok.pl/), Kontakt: [Telegram](https://t.me/proszczok).
Freiwillige Unterstützung: [PayPal](https://paypal.me/proszczok),
[Revolut](https://revolut.me/r_rosiejka).
