# STATUS — FM26 Human Engine

Dernière mise à jour : 2026-08-14 (run autonome Claude Code, environnement cloud sans accès FM26/BepInEx/IL2CPP).

## Où en est le projet

**Couche 2 (orchestration d'archétype d'événement, clash_vestiaire) : implémentée, testée, revue QA
faite et corrections appliquées dans ce même run.** Voir sections dédiées plus bas.

**Couche 1 (traits/affinités/déclenchement) : implémentée et testée, premier jet.**
Structure .NET créée à la racine :

```
FM26HumanEngine.sln
src/FM26.Engine/                  (class library, net8.0, pur C#, aucune dépendance Unity/IL2CPP/BepInEx)
  Traits/
    PersonalityTraits.cs          — struct-record des 6 traits (ego, leadership, loyaute_groupe,
                                     tolerance_pression, discretion, ambition), validés 1-20
    TraitGenerationContext.cs     — contexte de génération (id joueur, âge)
    IPersonalityTraitGenerator.cs — interface de génération
    DeterministicPersonalityTraitGenerator.cs — implémentation pure C# (voir décision ci-dessous)
  Affinite/
    Personne.cs, EvenementHistorique.cs
    AffiniteCalculator.cs         — score d'affinité pondéré [-100, 100]
  Declenchement/
    ArchetypeEvenement.cs, ContexteJeu.cs
    IGenerateurAleatoire.cs, GenerateurAleatoireSysteme.cs
    DeclenchementCalculator.cs    — probabilité pondérée + roll

tests/FM26.Engine.Tests/          (xUnit)
  Traits/, Affinite/, Declenchement/
```

`dotnet test` (Couche 1 seule) : 65/65 tests passent.

## Couche 2 : orchestration de l'archétype clash_vestiaire (spec §3)

Correction par rapport à la note laissée dans un run précédent : la Couche 2 n'est **pas** hors
périmètre de cet environnement cloud. Seule la génération de texte par l'API Claude l'est (pas
d'accès réseau ici) — exactement le même type de tension que pour les traits en Couche 1, et
résolue de la même façon : interface + implémentation de repli pure C#.

```
src/FM26.Engine/
  Orchestration/
    ChoixCommunication.cs           — enum apaiser/couvrir/sanctionner (spécifique à clash_vestiaire)
    EffetsArchetype.cs              — conséquences chiffrées (moral_equipe, cohesion, moral_joueur_cible,
                                       respect_autorite, reputation_club), validées `double.IsFinite`
    DeclencheursClashVestiaire.cs   — portillon de contexte (série_défaites, affinité) + probabilité_base,
                                       typé (pas d'expressions à parser)
    PhaseArchetypeBase.cs           — base commune (nom, délai en jours, choix_utilisateur)
    PhaseIncident.cs, PhaseReactionPresse.cs, PhaseConsequenceMoyenTerme.cs
                                     — les 3 phases de l'exemple JSON de la spec, traduites en types forts
    EcritureSaveArchetype.cs        — métadonnée écriture_save (liste des champs save concernés), déclarative
    ArchetypeClashVestiaire.cs      — composite + `CreerReference()` calibré sur l'exemple JSON de la spec
    PhaseInstance.cs                — état d'une instance (Incident/ReactionPresse/ConsequenceMoyenTerme/Terminee)
    RisqueFuiteMediaCalculator.cs   — risque de fuite média = f(discrétion moyenne des 2 protagonistes)
    InstanceClashVestiaire.cs       — instance vivante (joueurs, phase, jour d'entrée en phase, choix, effets appliqués)
    OrchestrateurClashVestiaire.cs  — EstDeclenche (portillon + Couche 1), Demarrer, Avancer
  Narratif/
    SceneIncidentContexte.cs        — contexte minimal pour générer le texte (noms des 2 joueurs, club)
    IGenerateurNarratif.cs          — interface, même patron que IPersonalityTraitGenerator
    GenerateurNarratifTemplate.cs   — implémentation de repli à base de templates fixes, aucun appel réseau

tests/FM26.Engine.Tests/
  Orchestration/  — déclencheurs, archétype de référence, effets, risque de fuite, orchestrateur
  Narratif/        — générateur narratif template
  OrchestrationPipelineIntegrationTests.cs — traits → affinité → déclenchement → orchestration → narration, bout en bout
```

`dotnet test` (suite complète, après corrections QA ci-dessous) : **125/125 tests passent**, aucun
test cassé commité.

### Décisions de conception — Couche 2

- **`OrchestrateurClashVestiaire.Avancer` ne fait avancer une instance que d'au plus une phase par
  appel**, même si le jour fourni dépasse plusieurs seuils de délai d'un coup. Un appelant qui
  vérifie l'instance à chaque tick de jour du jeu converge naturellement vers la bonne phase sans
  jamais sauter la génération narrative d'une phase intermédiaire (ex. sauter reaction_presse si le
  joueur consulte son téléphone en retard).
- **Le délai de consequence_moyen_terme (14 jours) est compté depuis l'entrée en reaction_presse**,
  pas depuis l'incident — cohérent avec la structure séquentielle du tableau "phases" de la spec où
  chaque `delai_jours` est relatif à la phase précédente.
- **Le choix utilisateur peut être fourni avant l'échéance du délai** : il est mémorisé dès qu'il
  arrive, mais la branche ne se résout (et les effets ne s'appliquent) qu'une fois le délai de 14
  jours écoulé. Sans choix, la phase reaction_presse reste bloquée indéfiniment (`choix_utilisateur:
  true` de la spec traité comme une contrainte dure, pas de valeur par défaut inventée).
- **Risque de fuite média de la branche "couvrir"** : la spec (§2.1) associe le trait `discretion`
  à "la probabilité d'être la source d'une fuite média" sans préciser le sens de la corrélation.
  Interprétation retenue ici (documentée dans `RisqueFuiteMediaCalculator`) : sens littéral du mot
  français — discrétion élevée chez les témoins = risque de fuite réduit. Les 2 protagonistes du
  clash sont pris comme témoins directs (pas encore de notion de témoins tiers/coéquipiers dans le
  moteur). Si le tirage déclenche la fuite, une pénalité s'ajoute par-dessus l'effet de base de la
  branche (`ReputationClub -15`, `Cohesion -5`).
- **Valeurs chiffrées des branches apaiser/couvrir non spécifiées par la spec** (seule "sanctionner"
  a des chiffres dans l'exemple JSON : moral -20 / respect autorité +10) : premier jet raisonnable
  choisi et documenté comme tel dans `ArchetypeClashVestiaire.CreerReference()`, à calibrer plus
  tard sur du contenu narratif réel — même réserve que les formules de la Couche 1.
- **Réutilisation explicite de la Couche 1** plutôt que duplication : `EstDeclenche` applique
  d'abord le portillon booléen de contexte (`DeclencheursClashVestiaire`), puis délègue le calcul de
  probabilité pondérée et le tirage du roll à `DeclenchementCalculator` (Couche 1) via un
  `ArchetypeEvenement` construit à la volée ; `InstanceClashVestiaire` réutilise directement
  `Affinite.Personne` plutôt que de redéfinir un type joueur pour l'orchestration.

### QA — Couche 2

Revue adversariale (agent `fm26-qa`) passée sur le premier jet, corrections appliquées dans le même run :

- **Bug réel corrigé — `EffetsArchetype` contournait sa propre validation via `with`/initialiseur
  d'objet.** Les propriétés étaient déclarées en `{ get; init; }` ; le constructeur validait bien
  `double.IsFinite`, mais une expression `with` ou un initialiseur d'objet appellent le constructeur
  sans-paramètre synthétisé des record structs, pas le constructeur défini — la validation était
  donc totalement contournable (`effets with { MoralEquipe = double.NaN }` ne levait rien). Corrigé
  en repassant les propriétés en `get` seul, comme `PersonalityTraits` en Couche 1 (qui bloque déjà
  structurellement ce contournement — vérifié par la QA qu'un `with` dessus ne compile même pas).
  Conséquence : `with` n'est plus utilisable sur `EffetsArchetype` ; l'unique site d'appel
  (pénalité de fuite média dans `OrchestrateurClashVestiaire.ResoudreEffetsBranche`) reconstruit
  désormais l'instance explicitement via le constructeur validant. Sans conséquence pratique
  aujourd'hui (le seul site d'appel ne manipulait que des constantes finies), mais serait devenu un
  risque réel de corruption de save une fois la Phase 3 (écriture dans la save) branchée.
- **Lacune réelle corrigée — la résolution d'un clash ne refermait jamais la boucle de rétroaction
  sur l'affinité (spec §2.2 : "recalculé après chaque événement impliquant la paire").** Aucun
  `EvenementHistorique` n'était produit après un clash_vestiaire, quelle que soit la branche : deux
  joueurs qui viennent de vivre un clash avaient exactement la même affinité qu'avant aux yeux du
  moteur de déclenchement, ce qui rendait la description de la branche "apaiser" ("probabilité de
  réplique réduite") fausse en pratique. Corrigé par un nouveau champ
  `EffetsArchetype.ImpactAffinitePaire` (peuplé sur l'incident et les 3 branches, valeurs premier
  jet documentées comme telles) et une nouvelle méthode
  `InstanceClashVestiaire.ConstruireHistoriquePourAffinite(jourActuel)` qui matérialise les impacts
  déjà appliqués en `EvenementHistorique` réinjectables dans `AffiniteCalculator.CalculerScore` —
  l'ancienneté de chaque événement est recalculée par rapport au jour fourni par l'appelant, pas
  figée à la construction, cohérent avec la sémantique de `EvenementHistorique.JoursDepuis` déjà en
  place en Couche 1.
- **Durcissement — copies défensives ajoutées** dans `PhaseConsequenceMoyenTerme` (dictionnaire des
  effets par branche) et `EcritureSaveArchetype` (liste des champs modifiés) : sans elles, un
  appelant qui mute la collection d'origine après construction (scénario réaliste dès la Phase 5,
  chargement d'archétypes depuis config/JSON) empoisonnerait silencieusement une instance déjà
  construite. Testé explicitement (mutation post-construction sans effet sur l'instance).
- **Tests renforcés** : frontière exacte du délai de 14 jours testée au jour près (jour 14 ne résout
  pas, jour 15 résout — auparavant testé loin de la frontière côté "ne résout pas"), comportement
  "premier choix utilisateur gagne pour toujours" testé explicitement (deux choix différents fournis
  avant résolution, seul le premier compte), arguments `null` de `OrchestrateurClashVestiaire`
  testés, nouveaux tests sur `ConstruireHistoriquePourAffinite`.
- **Non retenu tel quel** : la QA a aussi noté que les valeurs premier-jet d'apaiser/couvrir restent
  non calibrées sur du contenu réel — attendu et déjà documenté comme tel, pas un bug.

## Décision de conception : génération des traits

La spec (§2.1) décrit un appel Claude unique par joueur pour générer les traits, mais range
ce sous-système sous "Couche 1 : données pures, testable hors-jeu". Pour lever cette tension
sans dépendance réseau dans une couche censée être pure C# testable en isolation :

- `IPersonalityTraitGenerator` est l'interface consommée par le reste du moteur.
- `DeterministicPersonalityTraitGenerator` est l'implémentation actuelle : génération
  déterministe (seed dérivée d'un hash FNV-1a stable de l'identifiant joueur — pas
  `string.GetHashCode()`, randomisé par processus en .NET), avec un biais contextuel simple
  sur l'âge (ambition plus haute chez les jeunes, leadership qui culmine vers 30 ans,
  tolérance à la pression qui augmente avec l'expérience). Même contexte en entrée → même
  profil en sortie, testable sans API.
- Quand la Couche 2 sera développée (en local, avec accès à l'API Claude), une implémentation
  `ClaudePersonalityTraitGenerator` pourra être branchée derrière la même interface sans
  toucher au moteur d'affinités/déclenchement. Ce n'est pas un blocage : c'est juste reporté
  au bon endroit dans l'ordre des couches.

## Calibrage des formules (premier jet, à ajuster)

- **Affinité** (`AffiniteCalculator`) : composantes ego (clash si deux ego hauts et proches),
  leadership (complémentarité si écart marqué, lutte de pouvoir si deux leaders forts et
  proches), concurrence de poste (pénalité si même poste, amplifiée par l'ambition combinée),
  historique (somme pondérée par ancienneté, décroissance ~moitié tous les 90 jours). Poids
  choisis pour rester dans des ordres de grandeur raisonnables, pas calibrés sur du contenu
  narratif réel — à revoir une fois la Couche 2 en place et testée avec de vrais archétypes.
- **Déclenchement** (`DeclenchementCalculator`) : probabilité pondérée = base_archétype ×
  modificateur_contexte (série de défaites + position au classement) × modificateur_affinité
  (score bas = probabilité accrue), bornée [0,1], puis comparée à un roll uniforme séparé
  (`IGenerateurAleatoire`, injectable pour les tests). Mêmes réserves de calibrage.

## QA

Revue adversariale (agent `fm26-qa`) passée sur ce premier jet, corrections appliquées dans le
même run :

- **Bug réel corrigé — NaN désamorçait silencieusement le garde-fou de déclenchement.**
  `Math.Clamp(NaN, ...)` et les comparaisons `< / >` renvoient toujours `false` avec NaN en
  .NET : un `scoreAffinite` NaN traversait tout le pipeline sans jamais lever d'exception, et
  `EstDeclenche` ne se déclenchait alors plus jamais silencieusement (aucun événement, aucune
  erreur — un faux négatif permanent et invisible). Corrigé par : validation explicite
  "fini" à la source (`EvenementHistorique.ImpactAffinite`, `CalculerProbabilitePonderee`) et
  garde de `EstDeclenche` réécrite en forme positive (`!(x >= 0 && x <= 1)`) pour que NaN soit
  effectivement rejeté. Tests de régression ajoutés.
- **API malhonnête corrigée — `Nationalite`/`Poste` retirés de `TraitGenerationContext`.**
  Ces paramètres étaient acceptés, validés, jamais utilisés par le générateur déterministe.
  Retirés plutôt que câblés avec une corrélation numérique non justifiée (nationalité →
  personnalité serait arbitraire) : Couche 2 pourra les réintroduire dans un contexte propre
  au générateur Claude, qui a un usage réel pour ce signal (cohérence narrative du texte).
- **Bornes du clamp `ModificateurAffinite` corrigées** (0.3/2.0 → 0.5/1.5, les seules
  réellement atteignables avec les poids actuels) et testées explicitement aux deux bornes.
- **Garde ajoutée** : `AffiniteCalculator.CalculerScore` lève désormais si les deux
  `Personne` passées ont le même `Id` (self-pairing silencieux détecté par la QA).
- **Test d'intégration ajouté** (`PipelineIntegrationTests`) : génération de traits → score
  d'affinité → probabilité de déclenchement enchaînés bout en bout, plus un cas comparatif
  tension forte vs. bonne entente sur toute la chaîne — la QA avait noté que chaque module
  n'était testé qu'en isolation.

## Chantier BepInEx / plugin macOS (session locale, 2026-08-14, accès réel à FM26)

Travail séparé de la Couche 1 ci-dessus, dans `src/FM26.BepInExPlugin/` — ne touche pas à
`src/FM26.Engine/`. Objectif de session : Phase 0 de la roadmap (setup BepInEx macOS,
hello-world plugin). **Bloqué avant la fin de la Phase 0**, voir détail ci-dessous.

### Ce qui marche

- **Installation BepInEx** : `BepInEx-Unity.IL2CPP-macos-x64-6.0.0-pre.2.zip` (release GitHub
  officielle `BepInEx/BepInEx`, tag `v6.0.0-pre.2`, build interne `6.0.0-be.697`) installé dans
  le dossier racine du jeu (`~/Library/Application Support/Steam/steamapps/common/Football
  Manager 26/`, **à côté de** `fm.app`, pas dedans — c'est le layout standard documenté par
  BepInEx pour les bundles `.app` macOS). Contient `BepInEx/`, `dotnet/` (runtime CoreCLR
  embarqué), `libdoorstop.dylib`, `run_bepinex.sh` (avec `executable_name="fm.app"` configuré).
  Piège rencontré : le zip officiel perd tous les bits de permission à l'extraction
  (`----------` sur tous les fichiers) — nécessite `chmod -R u+rwX` après `unzip`.
- **Injection confirmée fonctionnelle** : lancement via `arch -x86_64 ./run_bepinex.sh` depuis
  la racine du jeu. `vmmap` sur le process confirme `Code Type: X86-64 (translated)` (Rosetta
  actif comme prévu) et le Preloader BepInEx s'exécute bien dans le process (logs Preloader
  visibles, génération de `BepInEx/config/BepInEx.cfg`, `BepInEx/interop/`,
  `BepInEx/unity-libs/` sur le premier lancement). L'injection dylib + Doorstop fonctionnent
  donc bout en bout sur ce jeu.
- **Plugin BepInEx minimal créé et compile** : `src/FM26.BepInExPlugin/Plugin.cs`, classe
  `Plugin : BasePlugin` avec `[BepInPlugin]`, log `Log.LogInfo(...)` dans `Load()` — le vrai
  hello-world demandé (pas juste une dylib brute). `dotnet build
  src/FM26.BepInExPlugin/FM26.BepInExPlugin.csproj` réussit (net6.0, car
  `BepInEx.Unity.IL2CPP.dll` de cette release cible `.NETCoreApp,Version=v6.0`). Référence 5 DLL
  BepInEx vendues localement dans `src/FM26.BepInExPlugin/libs/` (non commitées, gitignored —
  voir `libs/README.md` pour la procédure de régénération). **Projet volontairement absent de
  `FM26HumanEngine.sln`** : un test a montré que `dotnet build FM26HumanEngine.sln` échoue si
  `libs/` est absent (cas de la routine cloud, qui n'a pas accès à FM26/BepInEx) — l'ajouter à
  la solution aurait cassé son `dotnet test` autonome. Se build uniquement en ciblant son
  `.csproj` directement.

### Blocage n°1 (détection de version Unity) — RÉSOLU, session du 2026-08-15

Le blocage de détection de version décrit ci-dessous a été corrigé, vérifié dans le vrai
`BepInEx/LogOutput.log` du jeu (`Running under Unity 6000.0.52f1`, plus de fallback
`0.0.0a0`, plus de 404 sur le téléchargement des librairies Unity). Root cause complète :

1. `BepInEx.Core.Paths.SetExecutablePath()` calcule `GameDataPath` comme
   `<GameRoot>/<ProcessName>_Data` (convention Windows/Linux, `MyGame_Data/` à côté d'un
   `.exe`). Ce chemin n'existe jamais sur macOS, où les données sont dans
   `<GameRoot>/<name>.app/Contents/Resources/Data/`. Résultat : chaque `File.Exists()` que
   `UnityInfo.DetermineVersion()` tente (`globalgamemanagers`, `data.unity3d`, `mainData`)
   échoue silencieusement, sans jamais lire la vraie version — fallback total sur
   `0.0.0a0`.
2. **Par-dessus ça**, la chaîne de version que FM26 embarque est de toute façon non standard :
   `"6000.0.52f1-fm26-05f1"` (suffixe custom Sports Interactive, offset 0x3B dans
   `globalgamemanagers`) — que `UnityVersion.Parse()` (`AssetRipper.Primitives`) rejette même
   si le fichier était trouvé au bon endroit.
3. Pas de mécanisme de contournement officiel côté BepInEx (`UnityInfo.SetRuntimeUnityVersion`
   existe mais est `internal`, inatteignable avant l'échec).
4. Écrire quoi que ce soit dans `fm.app/Contents/...` échoue avec `EPERM` — protection macOS
   "App Management" (Réglages Système → Confidentialité et sécurité), pas accordable en
   headless.

**Fix retenu** (dans `native/macos-gamedata-shadow/setup_shadow_data_dir.sh`, committé) :
crée le dossier `<ProcessName>_Data` que BepInEx attend, comme *sibling* de `fm.app` (donc
dans un emplacement normal et librement inscriptible du dossier Steam, jamais dans
`Contents/...`). Rempli de symlinks vers le vrai dossier `Data/`, sauf `globalgamemanagers`
qui reçoit une **copie** avec un seul octet patché (offset 59/0x3B, le `-` après
`6000.0.52f1` remplacé par un NUL) — tronque la chaîne à `"6000.0.52f1"`, que
`UnityVersion.Parse()` accepte. Le fichier réel dans `fm.app` n'est jamais touché. Script
idempotent, avec vérification de l'octet avant patch (échoue proprement si un futur patch du
jeu change ce layout plutôt que de corrompre un octet au hasard).

Une piste alternative plus radicale a aussi été explorée et fonctionne en principe mais n'a
pas été retenue comme fix principal : `native/globalgamemanagers-version-shim/version_shim.c`,
une dylib d'interposition (`DYLD_INTERPOSE` sur `open`/`read`/`pread`/`lseek`/`close`) qui
patche l'octet en mémoire au moment de la lecture, sans jamais rien écrire sur disque, pas
même une copie. Gardée dans le repo (voir son en-tête pour le détail technique, y compris un
piège rencontré avec `dlsym(RTLD_NEXT, ...)` qui bouclait à l'infini) comme alternative si le
shadow-dir posait un jour problème.

### Blocage n°2 — RÉSOLU avec BepInEx 6.0.0-be.785, session du 2026-08-15

Une fois le blocage n°1 corrigé, une première tentative (BepInEx 6.0.0-be.697) allait plus
loin (téléchargement des librairies Unity réussi) puis échouait sur une limitation différente
et plus profonde : `Cpp2IL.Core.Exceptions.LibCpp2ILInitializationException:
System.FormatException: Unsupported metadata version found! We support 23-29, got 31`.
Cpp2IL/LibCpp2IL embarqués dans be.697 ne savaient lire que les métadonnées IL2CPP jusqu'à la
version 29 ; FM26 (Unity 6000.0.52f1) utilise la version 31.

**Fix** : mise à jour vers un build bleeding-edge plus récent, **BepInEx 6.0.0-be.785**
(téléchargé depuis builds.bepinex.dev, extrait dans
`/private/tmp/.../scratchpad/bepinex-be785/`, installé dans le dossier du jeu à la place de
be.697). Lancé sous `caffeinate -i` + Rosetta + le fix de version déjà en place (shadow data
dir) + `BEPINEX_GAME_ASSEMBLY_PATH` pointé explicitement vers
`fm.app/Contents/Frameworks/GameAssembly.dylib`.

**Résultat confirmé** : `BepInEx/interop/` contient maintenant les 162 vraies assemblies
IL2CPP du jeu, y compris les assemblies spécifiques FM26/Sports Interactive —
`FM.GameConfig.dll`, `FM.GamePlugin.dll`, `FM.Graphics.dll`, `FM.Match.dll`, `FM.UI.dll`,
`FMGame.dll`, `SI.Core.dll`, `SI.Match.dll`, `SI.Services.dll`, `SI.UI.dll`,
`SI.Bindable.dll`, `SI.CityGen.dll`, etc. — en plus de tous les modules Unity/Il2Cpp
standards. **La génération d'interop réussit : le blocage v31 est résolu.**

**Ce qui n'est PAS encore confirmé** : le chargement effectif du plugin hello-world
(`src/FM26.BepInExPlugin/`). Le lancement de vérification a tourné ~7 minutes à 46-47% CPU
après "Chainloader initialized" — le process est allé au-delà de BepInEx, dans le vrai
bootstrap du jeu (chargement des traductions, tentatives d'init Steam API, création d'un
canal multijoueur — visible dans le stdout complet du process, distinct du
`BepInEx/LogOutput.log` propre à BepInEx), sans jamais logguer le message du plugin. Le
process a été arrêté par précaution à ce stade plutôt que de le laisser continuer vers un
écran ou menu quelconque. **Point d'honnêteté à noter** : le système de crash-reporting du
jeu (Backtrace) a enregistré un évènement "game crash" dans le stdout au moment précis de
l'arrêt (`Received game crash. Storing attributes...`) — impossible de dire avec certitude si
c'est un vrai crash du moteur coïncidant avec l'arrêt, ou le signal d'arrêt lui-même interprété
comme un crash par le SDK Backtrace. Aucun menu atteint, aucune save créée ni modifiée dans
les deux cas (le stdout s'arrête au chargement des traductions/multijoueur, bien avant tout
état lié à une save).

**Rien n'a été forcé pour aller plus loin** (pas de patch binaire, pas de modification des
fichiers du jeu au-delà du shadow-dir déjà décrit) — arrêt par précaution plutôt que de pousser
la vérification jusqu'à un état incertain.

### Pistes pour la suite

- **Prioritaire** : relancer avec be.785 déjà installé (rien à retélécharger), en surveillant
  précisément le moment après "Chainloader initialized" où les plugins IL2CPP se chargent
  réellement — arrêter le process dès le message de log du plugin hello-world capté (ou dès
  qu'on approche un écran/menu, selon ce qui vient en premier), pas après un délai fixe de
  plusieurs minutes qui laisse le jeu avancer trop loin dans son bootstrap.
- Une fois le chargement du plugin confirmé : faire l'inventaire exploratoire des
  classes/namespaces utiles pour la Phase 3 (lecture/écriture moral, réputation, relations
  joueur/staff) dans les assemblies `FM.*`/`SI.*` maintenant disponibles dans
  `BepInEx/interop/` — c'était l'étape 5 prévue à l'origine pour ce chantier, jamais atteinte.
- Ouvrir un ticket upstream (BepInEx et/ou Cpp2IL) : cas générique Unity 6000.x très récent,
  susceptible de concerner d'autres jeux, utile même si on trouve un contournement local.

### État du dossier jeu (aucune save touchée)

BepInEx reste installé dans le dossier du jeu (fichiers inertes tant que le jeu n'est pas
lancé via `run_bepinex.sh` sous Rosetta manuellement — un lancement normal via Steam/Finder
n'injecte rien, `DYLD_INSERT_LIBRARIES` n'est positionné que dans le process lancé
explicitement par `run_bepinex.sh`). Le process FM26 du test s'est arrêté de lui-même après
l'exception fatale du Preloader ; aucun kill manuel n'a été nécessaire, aucune save n'a été
créée ni modifiée (le jeu n'a jamais atteint le menu principal).

## Prochaines étapes

- **BepInEx/plugin macOS** : la détection de version Unity est réglée (voir "Blocage n°1 —
  RÉSOLU"). Reste à lever le blocage n°2 (Cpp2IL ne supporte pas les métadonnées IL2CPP v31,
  cf. pistes ci-dessus — build BepInEx bleeding-edge plus récent à retenter proprement) avant
  de pouvoir poursuivre les étapes 2-5 de la Phase 0 (génération interop, validation du
  chargement du plugin en jeu, inventaire des classes/namespaces utiles pour la Phase 3).
- Étendre l'orchestration aux autres archétypes listés en spec §3 ("Autres archétypes à modéliser
  sur ce patron") une fois clash_vestiaire éprouvé : fuite média, boycott d'entraînement, tension
  président/entraîneur, négociation d'agent difficile, scandale extra-sportif, rivalité de poste.
  Chacun aura probablement son propre enum de choix (pas de généralisation prématurée de
  `ChoixCommunication` avant d'avoir un deuxième cas réel à comparer).
- `ClaudeGenerateurNarratif` (implémentation réseau de `IGenerateurNarratif`) : nécessite
  l'environnement local (accès réel à l'API Claude en conditions de coût/cache) — hors périmètre de
  cet environnement cloud, comme `ClaudePersonalityTraitGenerator` en Couche 1.
- Couche 3 (interface "téléphone") : hors périmètre, dépend des couches 1 et 2.

## Notes d'exécution

- `dotnet` SDK non préinstallé dans cet environnement cloud ; installé via `apt-get install
  dotnet-sdk-8.0` en début de run (à refaire si l'environnement est recréé from scratch).
- Pas de NOTES_FOR_AGENT.md au moment de ce run.
