# Radeoner

Panneau AMD GPU léger et portable pour Windows, avec contrôles Windows Forms
natifs, navigation au clavier et prise en charge de NVDA.

[Polski](README.md) · [English](README.en.md) · [Deutsch](README.de.md) ·
[Русский](README.ru.md) · [Español](README.es.md) · **Français**

## Télécharger et démarrer

1. Téléchargez le ZIP portable depuis les [versions](https://github.com/proszczok2002/Radeoner/releases/latest).
2. Extrayez tous les fichiers dans un dossier accessible en écriture.
3. Lancez **Radeoner.exe**. Aucun installateur n'est nécessaire.

Il faut Windows 10/11 x64, Microsoft .NET Framework 4.8 et un pilote AMD avec
ADLX. Une installation Driver Only convient si elle contient ADLX ; le panneau
Adrenalin n'est pas requis. La bibliothèque du pilote est chargée uniquement
depuis System32. Radeoner n'installe aucun pilote noyau propre pour la GPU.

## Fonctions et version 1.06

- Les relevés sont actualisés chaque seconde par défaut, même dans la zone
  de notification.
- Fréquences, tension, limite de puissance, courbes du ventilateur et Zero RPM
  dépendent du pilote. Les fonctions disponibles d'une GPU intégrée restent
  accessibles ; les onglets vides sont masqués.
- L'icône d'une GPU intégrée ne mentionne pas de ventilateur. Pour les autres
  GPU, elle indique uniquement une vitesse valide, y compris 0 RPM.
- Sauvegarde des profils JSON. Importer remplit seulement les champs ;
  **Appliquer** écrit dans la GPU après confirmation.
- Six langues, thèmes système/clair/sombre et taille du texte de 100 à 200 %.
- **F1** ouvre le guide complet traduit sans connexion à internet.
- Recherche facultative des mises à jour au démarrage et à intervalle choisi.
  Les changements sont présentés avant l'installation ; profils et préférences
  sont conservés.

Le RGB nécessite toujours un **serveur SDK OpenRGB local** facultatif.
Radeoner peut télécharger le ZIP officiel vérifié. L'extraction, le lancement
d'OpenRGB et l'activation de son serveur SDK sont des étapes séparées.
La version 1.06 n'inclut pas d'accès matériel natif indépendant à l'éclairage.

## Clavier et NVDA

| Touches | Action |
| --- | --- |
| Tab / Maj+Tab | Champ suivant / précédent |
| Ctrl+Tab / Ctrl+Maj+Tab | Onglet disponible suivant / précédent |
| Ctrl+1…Ctrl+5 | Relevés, Réglages GPU, Ventilateur, Rapport, Paramètres |
| F5 / F6 / F1 | Actualiser / sélectionner l'état / aide |
| Ctrl+E / Ctrl+I | Exporter / importer un profil |
| Ctrl+Entrée | Appliquer les changements |
| Ctrl+Maj+S | Enregistrer le rapport |
| Alt+F4 | Masquer dans la zone de notification |

Une cible Ctrl+1…5 indisponible ouvre Relevés. **Quitter** ferme complètement
le programme. Windows+B permet d'atteindre l'icône ; Entrée ouvre la fenêtre.
Dans les relevés, les flèches sélectionnent les lignes et Ctrl+A/Ctrl+C
sélectionnent et copient les valeurs.

## Limites, fichiers et contributions

La limite de puissance en pourcentage n'est pas une TDP en watts ni la limite
commune CPU/iGPU. Certaines valeurs Navi4 sont des décalages : lisez leur
description. Radeoner ne déverrouille pas le micrologiciel et ne contrôle pas
tous les ventilateurs d'un portable. Les écritures physiques et le RGB doivent
être vérifiés sur les cartes concernées. Les tests automatiques ne remplacent
pas les annonces réelles de NVDA. Le [rapport](docs/AUDIT_1.06.md) distingue
ces vérifications.

Les six langues et guides sont intégrés dans l'EXE. Un fichier UTF-8 local
`lang/<code>.lng` peut remplacer les textes. La structure portable de huit
fichiers reste compatible avec l'actualisateur 1.05. Les préférences se trouvent
dans `Radeoner.settings.json`, près de l'EXE. Les profils GPU sont exportés
séparément et ne sont pas appliqués automatiquement au démarrage.

[Guide complet](Readme.fr.txt) · [Changements 1.06](docs/releases/v1.06.md#français)

Le code est public sous licence **MIT**. Compilation locale :

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Test
```

[Signalez les défauts](https://github.com/proszczok2002/Radeoner/issues) ou
proposez une pull request avec le modèle GPU et les versions de Windows,
du pilote, de l'application et de NVDA. Des noms MSAA présents ne prouvent
pas, à eux seuls, la lecture correcte par NVDA.

Auteur : [proszczok.pl](https://proszczok.pl/) ; contact : [Telegram](https://t.me/proszczok).
Soutien facultatif : [PayPal](https://paypal.me/proszczok),
[Revolut](https://revolut.me/r_rosiejka).
