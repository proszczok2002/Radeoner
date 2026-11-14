# Aktualizacja 1.05 → 1.06 / Update test

**PASS: końcowy pakiet lokalnie i przez publiczny GitHub latest.**
Lokalnie: 2026-11-14T15:24:08.4908069Z. Publicznie: 2026-11-14T15:49:02.3790030Z.
Stabilne wydanie: [Radeoner 1.06](https://github.com/proszczok2002/Radeoner/releases/tag/v1.06).

## Polski

Opublikowany EXE 1.05 bez tokena pobrał manifest i ZIP 1.06 z GitHub latest,
podmienił pliki, uruchomił rzeczywiste nowe okno i obsługę ADLX oraz potwierdził
inicjalizację. Stary EXE i pomocnik zostały usunięte; preferencje i profil
zachowały sumy. Użyto izolowanego folderu ze spacjami. Nie stosowano ustawień
GPU ani podświetlenia i nie nadpisano użytkowej kopii programu.

62 testy na czystym Windows CI przeszły przed publikacją:
[PR](https://github.com/proszczok2002/Radeoner/actions/runs/43611808229),
[push](https://github.com/proszczok2002/Radeoner/actions/runs/43611803535).
[Audyt NVDA](AUDIT_1.06.md): 900 punktów UI, 180 dialogów i 72 traya.
Końcowe przebiegi wydania: [main](https://github.com/proszczok2002/Radeoner/actions/runs/43612172645),
[tag](https://github.com/proszczok2002/Radeoner/actions/runs/43612178913).

## English

**Local and public update: PASS**. The published 1.05 EXE anonymously downloaded
the eight-file 1.06 manifest/ZIP from GitHub latest and completed SHA-256
validation, real replacement, new WinForms startup,
acknowledgement and old EXE/helper cleanup. Preference/profile hashes remained
unchanged in an isolated path containing spaces. No GPU/RGB tuning or changes
to the user's Documents copy were performed. All 62 clean Windows CI cases
passed before publication. The separate NVDA report covers 900 main-UI,
180 native-dialog and 72 tray speech-text checkpoints.

## Deutsch

Lokaler und öffentlicher Updatetest 1.05 → 1.06 bestanden: anonymes Herunterladen
von GitHub, SHA-256, Austausch der acht Dateien und tatsächlicher Start des
neuen Fensters bestätigt. Alte EXE/Helfer wurden danach entfernt; Profile und
Einstellungen behielten identische Prüfsummen. Keine GPU-/RGB-Werte geändert.
62 CI-Tests sowie die NVDA-Prüfungen sind im verlinkten Bericht dokumentiert.

## Русский

Локальное и публичное обновление 1.05 → 1.06 проверены: загрузка с GitHub без
токена, SHA-256, замена восьми файлов и подтверждённый запуск нового окна.
После подтверждения старый EXE и файлы помощника удалены. Контрольные суммы
профиля и настроек не изменились. Параметры GPU и RGB не применялись.
62 теста CI и проверки NVDA описаны в отчётах по ссылкам выше.

## Español

Actualización local y pública 1.05 → 1.06 verificada: descarga anónima de GitHub,
SHA-256, sustitución de ocho archivos y arranque real confirmado. Después se
eliminaron el EXE antiguo y el ayudante; perfiles y preferencias conservaron
sus sumas. No se aplicaron parámetros GPU/RGB. Los informes enlazados documentan
62 pruebas CI y los puntos de lectura real de NVDA.

## Français

Mise à jour locale et publique 1.05 → 1.06 validée : téléchargement anonyme
depuis GitHub, SHA-256, remplacement des huit fichiers et démarrage réel confirmé.
L'ancien EXE et l'assistant ont ensuite été supprimés ; profils et préférences
ont conservé leurs sommes. Aucun réglage GPU/RGB n'a été appliqué. Les rapports
liés documentent les 62 tests CI et les points de parole réelle de NVDA.

## SHA-256

| File | SHA-256 |
| --- | --- |
| 1.05 EXE | `475a90c9c51216a98762209d53cb86c5b38c0ae9d0ff4542aea626da4b399710` |
| 1.06 ZIP | `f88edf54ab391a2199fbc164f9732bff245394f505eb9969fb1b5bc228226e6c` |
| 1.06 EXE | `b72f5623bc93f3f828a0e164cc56069caa31ca4b00f8767bb6f07ff1290b3ceb` |

Evidence before housekeeping:
`artifacts/update-e2e/local-20261114-162403/result.json`;
`artifacts/update-e2e/github-20261114-164855/result.json`.
After housekeeping use `niepotrzebne/*/INDEKS.csv` to locate the same path.
