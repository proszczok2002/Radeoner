Radeoner 1.06 — Benutzerhandbuch

Radeoner ist ein leichtes, portables AMD-GPU-Panel für Windows 10/11 x64 mit
Microsoft .NET Framework 4.8. Der AMD-Treiber mit ADLX genügt; das Adrenalin-
Bedienfeld wird nicht benötigt. Radeoner.exe aus einem beschreibbaren Ordner
starten. Es werden keine zusätzlichen Dienste für die GPU-Regelung installiert.

SPRACHE UND DARSTELLUNG
Unter Einstellungen > Sprache: Polski, English, Deutsch, Русский, Español oder
Français. Design: System, Hell oder Dunkel; Textgröße: 100 bis 200 Prozent.
Änderungen wirken sofort. Dieses Handbuch ist über F1 auch offline erreichbar.
Die Übersetzungsquellen liegen als UTF-8-Dateien unter lang/*.lng auf GitHub.
Die neuen Sprachpakete sind in der EXE enthalten; keine getrennte Installation.

FUNKTIONEN JE NACH GPU
Die GPU oben auswählen. Messwerte, Bericht und Einstellungen bleiben verfügbar.
Regelung und Lüfter werden nur angezeigt, wenn passende Funktionen verfügbar
sind; dedizierte Karten haben zusätzlich den Zugang zum optionalen RGB.
Nicht unterstützte Spannungen, Taktraten und Kurven erscheinen nicht als
bearbeitbare Felder. Eine integrierte GPU kann einzelne Regelfunktionen haben;
diese bleiben zugänglich. Laptoplüfter können von der Firmware geregelt werden.
Der Infobereich nennt bei einer integrierten GPU keinen GPU-Lüfter. Bei anderen
GPUs erscheint die Drehzahl nur mit gültigem Messwert; 0 RPM ist gültig.

BEDIENUNG MIT TASTATUR UND NVDA
Tab / Umschalt+Tab: vorwärts/rückwärts durch Felder. Strg+Tab / Strg+Umschalt+Tab:
nächste/vorherige verfügbare Registerkarte. Strg+1 bis Strg+5: Messwerte,
Regelung, Lüfter, Bericht, Einstellungen. Nicht verfügbare Funktionen werden
übersprungen. F5: Einstellungen neu lesen. F6: Status auswählen und lesen.
F1: Hilfe. Zahlen können eingegeben oder mit Pfeil hoch/runter verändert werden.
Jedes Zahlenfeld nennt Funktion, Einheit, Wert und zulässigen Bereich.
Messwertliste: Pfeile wählen Zeilen, Strg+A wählt alle, Strg+C kopiert.
Alt+F4 verbirgt im Infobereich. Zum vollständigen Beenden Beenden wählen.
Mit Windows+B kann die Benachrichtigungszone erreicht werden; Radeoner wählen
und Eingabe zum Öffnen drücken. Das Kontextmenü enthält auch Beenden.

GPU-WERTE ÄNDERN
Werte prüfen, bearbeiten und Anwenden oder Strg+Eingabe wählen. Erst die
Bestätigung schreibt zur GPU; Start und Messwertabfragen verändern nichts.
Bereich und Schritt stammen vom Treiber. Navi4 und spätere GPUs können statt
absolutem Takt/Spannung einen Versatz anbieten: die Feldbeschreibung beachten.
Eine prozentuale Leistungsgrenze ist keine TDP in Watt und verändert nicht
automatisch gemeinsame CPU/iGPU-Grenzen. Unsichere Werte können Instabilität
verursachen. Lüftertemperaturen müssen steigen und Drehzahlen dürfen nicht sinken.
Werkseinstellungen setzt nach Bestätigung auch extern geänderte GPU-Werte zurück.
Letzte Änderung rückgängig ist nur verfügbar, wenn eine bestätigte Änderung vorliegt.

PROFILE UND BERICHT
Strg+E exportiert die Editorwerte, einschließlich nicht angewendeter Änderungen,
als JSON. Strg+I importiert ein Profil nur in den Editor. GPU-Identität, Bereiche,
Einheiten und Kurven werden geprüft. Zum Schreiben erneut Anwenden bestätigen.
Profile sichern und nur auf kompatiblen Karten verwenden. RGB ist nicht Teil
der ADLX-Profile. Strg+Umschalt+S speichert den Textbericht. Berichte vor dem
öffentlichen Teilen prüfen, da sie Hardwarekennungen enthalten können.

RGB
Regelung > Kartenbeleuchtung. Derzeit wird ein lokaler OpenRGB-SDK-Server
benötigt; eine unabhängige Hardwareimplementierung ist nicht Teil dieser Version.
OpenRGB herunterladen speichert ein geprüftes offizielles ZIP. Entpacken,
OpenRGB.exe starten und den SDK-Server (üblicherweise Port 6742) aktivieren.
Verbinden, GPU und Modus wählen, Farbe/Helligkeit festlegen, RGB anwenden.
Ausschalten oder 0 Prozent schaltet ab. Modellunterstützung kommt von OpenRGB.
Es werden weder OpenRGB noch dessen Hardwaretreiber automatisch gestartet/installiert.

START, UPDATES UND FEHLER
Autostart öffnet im Infobereich bei Anmeldung dieses Benutzers. Automatische
Updates können mit Intervall und Prüfung beim Start eingestellt werden. Gefundene
Versionen zeigen zuerst Änderungen; Ja lädt, prüft und installiert, Nein bricht ab.
Der Updater bewahrt Profile/Einstellungen und entfernt die alte EXE erst nach
erfolgreichem Start; bei fehlgeschlagenem Start wird die alte Version wiederhergestellt.
Bei Treiberfehlern bleibt das Fenster offen und versucht erneut zu lesen.
Schreibzugriffe werden nicht automatisch wiederholt. Nach Fehler F5 verwenden
und tatsächliche Werte prüfen. Lokale Fehler stehen in Radeoner.log.

QUELLCODE, KONTAKT UND FREIWILLIGE UNTERSTÜTZUNG
MIT-Lizenz: https://github.com/proszczok2002/Radeoner
Versionen: https://github.com/proszczok2002/Radeoner/releases
Autor: https://proszczok.pl/ — Telegram: https://t.me/proszczok
PayPal: https://paypal.me/proszczok — Revolut: https://revolut.me/r_rosiejka
Einstellungen > Über Radeoner / Unterstützung und Kontakt öffnet diese Seiten.
Fehler mit Programm-/Windows-/NVDA-Version, GPU-Modell und Schritten melden.
