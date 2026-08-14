# STATUS — FM26 Human Engine

Dernière mise à jour : 2026-08-14 (run autonome Claude Code, environnement cloud sans accès FM26/BepInEx/IL2CPP).

## Où en est le projet

**Couche 2 (orchestration d'archétype d'événement, clash_vestiaire) : implémentée et testée, premier jet.**
Voir section dédiée plus bas. Une revue QA adversariale a été lancée sur ce premier jet ; si elle
remonte de vrais bugs, ils seront corrigés dans un prochain run et reportés ici.

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

`dotnet test` (suite complète) : **114/114 tests passent**, aucun test cassé commité.

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

### Ce qui bloque — Phase 0 non terminée

**Étape 2 (premier lancement → génération des stubs d'interop) échoue**, avant même d'atteindre
le chargement de plugins. Cause racine identifiée avec certitude (log complet dans
`/private/tmp/.../scratchpad/bepinex/first_launch.log` de cette session, non conservé dans le
repo) :

1. **La chaîne de version Unity embarquée par FM26 est non standard.** Lecture hexadécimale de
   `fm.app/Contents/Resources/Data/globalgamemanagers` à l'offset 0x30 :
   `"6000.0.52f1-fm26-05f1"` — Sports Interactive/SEGA a suffixé leur build Unity custom
   (`-fm26-05f1`) au lieu du format standard `6000.0.52f1`.
2. `BepInEx.Unity.Common.UnityInfo.DetermineVersion()` lit bien ce champ mais
   `UnityVersion.Parse()` (package `AssetRipper.Primitives`) rejette la chaîne à cause du
   suffixe et lève une exception (catchée en interne) ; comme les autres candidats
   (`data.unity3d`, `mainData`) n'existent pas pour ce jeu, la détection tombe en fallback total
   → `Version = default` (`"0.0.0a0"`) — confirmé par le log : `Running under Unity 0.0.0a0`.
3. Conséquence en cascade : (a) `Il2CppInteropManager` construit l'URL de téléchargement des
   librairies de base Unity avec cette fausse version → `https://unity.bepinex.dev/libraries/
   0.0.0a0.zip` → **404** ; (b) même en ignorant ça, `Il2CppInterop.Runtime.UnityVersionHandler`
   n'a pas de handler enregistré pour la version `0.0.0` → **exception fatale dans le
   Preloader** (`TypeInitializationException` → `ApplicationException: No handler`) avant tout
   chargement de plugin.
4. **Pas de mécanisme de contournement propre côté BepInEx** : pas de variable d'env, pas de
   clé dans `BepInEx.cfg`, pas d'argument CLI pour forcer la version Unity détectée
   (`UnityInfo.SetRuntimeUnityVersion` existe mais est `internal`, inatteignable depuis un
   plugin — et de toute façon les plugins ne sont même pas encore chargés à ce stade).

**Second blocage indépendant, découvert en creusant une piste de contournement** (déposer un
faux fichier `data.unity3d` avec une version propre dans `Contents/Resources/Data/`, un des
autres chemins que `UnityInfo` sonde, pour court-circuiter la lecture ratée de
`globalgamemanagers` sans toucher au jeu) : **toute écriture à l'intérieur du bundle
`fm.app/Contents/...` échoue avec `EPERM` ("Operation not permitted")**, aussi bien depuis le
shell que depuis le process FM26 lui-même une fois le dylib injecté (le crash du Preloader
essaie d'écrire un log `Contents/MacOS/preloader_*.log` et échoue pour la même raison).
Vérifié : propriétaire/permissions/ACL/flags du fichier sont tous normaux (`rwxr-xr-x`,
`louysmercadier:staff`, pas de `schg`/`uchg`, pas d'ACL), volume monté en lecture-écriture — ce
n'est pas un problème de permissions Unix classique. Signature typique de la protection macOS
**"App Management"** (Réglages Système → Confidentialité et sécurité → Gestion des
applications, introduite en 2023+) qui bloque la modification du contenu d'un bundle `.app` par
un autre process/outil tant qu'elle n'est pas explicitement accordée — ça se règle uniquement
via l'interface graphique, pas en headless/CLI. Ce contournement est donc lui-même bloqué,
indépendamment du point 1.

**Rien n'a été forcé pour contourner ces deux blocages** (pas de patch binaire de BepInEx, pas
de modification des fichiers du jeu, pas de tentative de désactiver SIP/App Management) —
conformément à la consigne de s'arrêter plutôt que de pousser une solution bancale.

### Pistes pour la suite (non tentées cette session)

- Compiler une version patchée de `BepInEx.Unity.Common.dll` (ou d'`AssetRipper.Primitives`)
  tolérant un suffixe après la version Unity standard — nécessite de builder BepInEx depuis les
  sources (faisable, dotnet 10 SDK dispo localement), mais c'est modifier un binaire tiers, pas
  juste une config.
- Accorder manuellement la permission "Gestion des applications" à l'app hôte du terminal dans
  Réglages Système, ce qui débloquerait la piste du faux `data.unity3d` — nécessite une action
  utilisateur en dehors de toute session Claude Code.
- Vérifier si une version plus récente de BepInEx (bleeding-edge sur builds.bepinex.dev, au-delà
  du tag `v6.0.0-pre.2`/`be.697` testé ici) a corrigé la tolérance de `UnityVersion.Parse` —
  pas vérifié cette session.
- Ouvrir un ticket upstream (BepInEx ou AssetRipper) : cas générique de studios qui suffixent
  leur version Unity custom, susceptible de concerner d'autres jeux.

### État du dossier jeu (aucune save touchée)

BepInEx reste installé dans le dossier du jeu (fichiers inertes tant que le jeu n'est pas
lancé via `run_bepinex.sh` sous Rosetta manuellement — un lancement normal via Steam/Finder
n'injecte rien, `DYLD_INSERT_LIBRARIES` n'est positionné que dans le process lancé
explicitement par `run_bepinex.sh`). Le process FM26 du test s'est arrêté de lui-même après
l'exception fatale du Preloader ; aucun kill manuel n'a été nécessaire, aucune save n'a été
créée ni modifiée (le jeu n'a jamais atteint le menu principal).

## Prochaines étapes

- **BepInEx/plugin macOS** : résoudre le blocage de détection de version Unity (cf. pistes
  ci-dessus) avant de pouvoir poursuivre les étapes 2-5 de la Phase 0 (génération interop,
  validation du chargement du plugin en jeu, inventaire des classes/namespaces utiles pour la
  Phase 3).
- Revue QA adversariale (`fm26-qa`) lancée sur ce premier jet de la Couche 2 en fin de run ; si elle
  remonte de vrais bugs, les corriger dans le prochain run et reporter ici (même processus qu'en
  Couche 1 — voir section QA plus haut, qui documente encore uniquement la revue de Couche 1).
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
