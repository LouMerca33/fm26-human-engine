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

## Prochaines étapes

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
