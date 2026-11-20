Radeoner 1.06 — guide utilisateur

Radeoner est un panneau AMD léger et portable pour Windows 10/11 x64 avec
Microsoft .NET Framework 4.8. Le pilote AMD avec ADLX suffit, sans panneau
Adrenalin. Lancez Radeoner.exe depuis un dossier accessible en écriture.
Aucun service supplémentaire de réglage GPU n'est installé.

LANGUE ET APPARENCE
Paramètres > Langue : Polski, English, Deutsch, Русский, Español ou Français.
Thèmes : système, clair et sombre. Texte : 100–200 %. Les changements sont
immédiats. F1 ouvre ce guide hors ligne. Les traductions du code public sont
des fichiers UTF-8 lang/*.lng. Les nouvelles langues sont intégrées à l'EXE ;
aucune installation séparée de pack linguistique n'est nécessaire.

FONCTIONS SELON LE GPU
Choisissez le GPU dans la liste supérieure. Relevés, Rapport et Paramètres
restent disponibles. Réglages GPU et Ventilateur apparaissent lorsqu'ils
proposent des fonctions ; les cartes dédiées donnent aussi accès au RGB
facultatif. Tensions, fréquences et courbes non prises en charge ne sont pas
présentées comme champs modifiables. Les fonctions disponibles d'un GPU
intégré restent accessibles. Le micrologiciel peut gérer les ventilateurs
d'un portable. L'icône d'un GPU intégré ne mentionne pas de ventilateur GPU.
Pour les autres GPU, les RPM apparaissent avec un relevé valide seulement ;
0 RPM est une valeur valide et indique l'arrêt.

CLAVIER ET NVDA
Tab / Maj+Tab : champs. Ctrl+Tab / Ctrl+Maj+Tab : onglet disponible suivant/
précédent. Ctrl+1–5 : Relevés, Réglages GPU, Ventilateur, Rapport, Paramètres.
Les fonctions indisponibles sont omises. F5 relit les paramètres. F6 sélectionne
et lit l'état. F1 ouvre l'aide. Saisissez les nombres ou utilisez Haut/Bas.
Chaque champ donne nom, unité, valeur et plage. Dans Relevés, les flèches
choisissent les lignes, Ctrl+A sélectionne tout et Ctrl+C copie.
Alt+F4 masque dans la zone de notification ; Quitter termine complètement.
Windows+B mène à la zone de notification : choisissez Radeoner puis Entrée
pour ouvrir. Le menu de l'icône permet également de quitter.

MODIFIER LES PARAMÈTRES
Vérifiez, modifiez et choisissez Appliquer ou Ctrl+Entrée. Seule la confirmation
écrit sur le GPU. Le démarrage et les lectures ne changent rien. Le pilote
définit plages et pas. Navi4 et les GPU ultérieurs peuvent proposer un décalage
de fréquence/tension plutôt qu'une valeur absolue : lisez la description.
Une limite de puissance en pourcentage n'est pas un TDP en watts et ne change
pas automatiquement la limite CPU/iGPU partagée. Des valeurs inadaptées peuvent
nuire à la stabilité. Les températures doivent croître et les vitesses ne pas
décroître. Paramètres d'usine réinitialise après confirmation aussi les réglages
modifiés ailleurs. Annuler est disponible après une écriture confirmée.

PROFILS ET RAPPORT
Ctrl+E exporte en JSON les valeurs de l'éditeur, même non appliquées. Ctrl+I
importe dans l'éditeur seulement. Identité GPU, plages, pas, unités et courbes
sont contrôlés. Confirmez ensuite Appliquer pour écrire. Sauvegardez les profils
et utilisez un matériel compatible. Les profils ADLX n'incluent pas le RGB.
Ctrl+Maj+S enregistre un rapport texte. Vérifiez-le avant publication : il peut
contenir des identifiants matériels.

RGB
Réglages GPU > Éclairage RGB. Un serveur SDK OpenRGB local est actuellement
nécessaire ; cette version n'intègre pas de commande matérielle indépendante.
Télécharger OpenRGB enregistre une archive officielle vérifiée. Extrayez le ZIP,
lancez OpenRGB.exe et activez le serveur SDK, généralement port 6742. Connectez,
choisissez GPU, mode, couleur et luminosité, puis Appliquer RGB. Éteindre ou
0 % éteint la lumière. La compatibilité du modèle dépend d'OpenRGB. Son logiciel
et ses pilotes matériels ne sont ni lancés ni installés automatiquement.

DÉMARRAGE, MISES À JOUR ET ERREURS
Le démarrage automatique ouvre dans la zone de notification à la connexion
de cet utilisateur. Réglez la recherche automatique, l'intervalle et la recherche
au démarrage. Les nouveautés sont présentées d'abord : Oui télécharge, vérifie
et installe ; Non annule. Profils et préférences sont conservés. L'ancien EXE
est supprimé après un démarrage réussi ; en cas d'échec, il est restauré.
Un échec du pilote laisse la fenêtre ouverte et réessaie les lectures, jamais
les écritures automatiquement. Utilisez F5 et vérifiez les valeurs réelles.
Les détails des erreurs locales se trouvent dans Radeoner.log.

CODE, CONTACT ET SOUTIEN FACULTATIF
Licence MIT : https://github.com/proszczok2002/Radeoner
Versions : https://github.com/proszczok2002/Radeoner/releases
Auteur : https://proszczok.pl/ — Telegram : https://t.me/proszczok
PayPal : https://paypal.me/proszczok — Revolut : https://revolut.me/r_rosiejka
Liens dans Paramètres > À propos / Soutien et contact. Pour signaler un défaut,
indiquez les versions du programme, Windows et NVDA, le modèle GPU et les étapes.

METTRE À JOUR LE PILOTE AMD
Ouvrez Paramètres → Mises à jour → Mettre à jour le pilote AMD.
Consultez la page produit et la licence AMD. Téléchargez le paquet complet :
Radeoner vérifie la signature et Windows identifie le fichier INF graphique.
Si le pilote est plus récent, confirmez et autorisez les droits d’administration.
Les mises à jour de Radeoner affichent tous les changements à l’ouverture.
