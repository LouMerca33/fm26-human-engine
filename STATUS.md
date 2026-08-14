# STATUS — FM26 Human Engine

Dernière mise à jour : 2026-08-14 (run autonome Claude Code, environnement cloud sans accès FM26/BepInEx/IL2CPP).

## Où en est le projet

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

`dotnet test` : **65/65 tests passent**, aucun test cassé commité.

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

- Poursuivre le calibrage / renforcement des tests de la Couche 1 si des lacunes apparaissent.
- Couche 2 (moteur narratif, appels Claude, archétypes d'événements type "clash_vestiaire")
  nécessite l'environnement local (accès réel à l'API Claude en conditions de coût/cache) —
  hors périmètre de cet environnement cloud.
- Couche 3 (interface "téléphone") : hors périmètre, dépend des couches 1 et 2.

## Notes d'exécution

- `dotnet` SDK non préinstallé dans cet environnement cloud ; installé via `apt-get install
  dotnet-sdk-8.0` en début de run (à refaire si l'environnement est recréé from scratch).
- Pas de NOTES_FOR_AGENT.md au moment de ce run.
