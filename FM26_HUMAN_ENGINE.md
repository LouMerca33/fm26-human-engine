# FM26 Human Engine — Spécifications du projet

Système d'événements narratifs réalistes pour Football Manager 26, piloté par l'API Claude, intégré via un plugin BepInEx. Objectif : injecter de la vraie psychologie de vestiaire, de la pression institutionnelle et des scandales crédibles dans une save FM26 basée sur l'Increase Realism Megapack (monde actuel).

---

## 1. Vue d'ensemble de l'architecture

```
┌─────────────────┐      lit la save       ┌──────────────────┐
│  FM26 (jeu)      │◄──────────────────────►│  Plugin BepInEx   │
│  (Realism Pack)  │   écrit moral/CA/etc.   │  (C#, il2cpp)     │
└─────────────────┘                         └────────┬──────────┘
                                                       │ contexte JSON
                                                       ▼
                                             ┌──────────────────┐
                                             │  API Claude       │
                                             │  (Haiku, cache)   │
                                             └────────┬──────────┘
                                                       │ événement généré
                                                       ▼
                                             ┌──────────────────┐
                                             │  Panneau in-game   │
                                             │  (UI custom)       │
                                             └──────────────────┘
```

Trois couches indépendantes, développables séparément :
1. **Le moteur de données** (traits, affinités, historique) — pur C#/JSON, aucune dépendance API
2. **Le moteur narratif** (appels Claude, génération de texte + décisions) — dépend de la couche 1
3. **L'interface** (panneau in-game, "téléphone" façon Agent Manager) — dépend des couches 1 et 2

Ordre de développement recommandé : **1 → 2 → 3**, chaque couche étant testable seule avant de brancher la suivante.

---

## 2. Système de traits & affinités (Couche 1)

### 2.1 Traits de personnalité

Chaque joueur/staff reçoit, à la création de la save (ou à son arrivée au club), un profil généré une seule fois et persistant :

| Trait | Échelle | Ce qu'il influence |
|---|---|---|
| `ego` | 1-20 | Réaction à la comparaison, au banc, à la critique publique |
| `leadership` | 1-20 | Capacité à apaiser ou au contraire cristalliser un groupe |
| `loyaute_groupe` | 1-20 | Probabilité de couvrir/dénoncer un coéquipier |
| `tolerance_pression` | 1-20 | Résistance aux mauvais résultats, médias, direction |
| `discretion` | 1-20 | Probabilité d'être la source d'une fuite media |
| `ambition` | 1-20 | Tendance à pousser pour un transfert, un rôle, du temps de jeu |

Génération : appel Claude unique par joueur à sa création, avec cohérence contextuelle (âge, nationalité, historique de carrière simulé si dispo dans la base), pas de tirage purement random pour éviter l'incohérence.

### 2.2 Score d'affinité (paire ou groupe)

```
affinite(A, B) = f(
  écart_ego(A,B),
  complémentarité_leadership(A,B),
  concurrence_poste(A,B),      // même position = friction potentielle accrue
  historique_evenements(A,B)   // les tensions passées pèsent plus lourd
)
```

Résultat : score -100 (tension forte) à +100 (bonne entente), recalculé après chaque événement impliquant la paire.

### 2.3 Déclenchement probabiliste

```
probabilité_evenement(mois) = base_par_archétype
                             × modificateur_contexte (résultats, classement, série)
                             × modificateur_affinité (score bas = probabilité accrue)
                             × facteur_aléatoire (roll)
```

Le hasard reste présent (le "roll") mais **pondéré**, jamais pur — c'est ce qui rend le système crédible sans être un simple générateur random.

---

## 3. Schéma d'archétype d'événement (Couche 2)

### Exemple complet : Clash de vestiaire

```json
{
  "id": "clash_vestiaire",
  "declencheurs": {
    "contexte_min": ["série_défaites >= 2", "affinité_paire <= -30"],
    "probabilité_base": 0.04
  },
  "phases": [
    {
      "phase": "incident",
      "génération": "Claude écrit la scène (2-3 phrases) à partir du contexte des 2 joueurs impliqués",
      "conséquences_immédiates": {
        "moral_equipe": -8,
        "cohesion": -12
      }
    },
    {
      "phase": "reaction_presse",
      "delai_jours": 1,
      "génération": "Claude génère 3 options de communication (apaiser / couvrir / sanctionner publiquement)",
      "choix_utilisateur": true
    },
    {
      "phase": "consequence_moyen_terme",
      "delai_jours": 14,
      "branches": {
        "apaiser": "tension latente, probabilité de réplique réduite",
        "couvrir": "risque de fuite média (probabilité liée au trait discretion des témoins)",
        "sanctionner": "moral du sanctionné -20, mais respect de l'autorité +10 côté groupe"
      }
    }
  ],
  "écriture_save": {
    "champs_modifiés": ["morale", "reputation_club", "relation_joueur_staff"]
  }
}
```

### Autres archétypes à modéliser sur ce patron (phase 2+)
- Fuite média / taupe du vestiaire
- Boycott d'entraînement
- Tension président / entraîneur
- Négociation d'agent difficile
- Scandale extra-sportif
- Rivalité interne pour un poste

---

## 4. Interface — le "téléphone" (Couche 3)

Inspiré du concept Agent Manager mais connecté en direct à la save (pas d'éditeur manuel requis, le plugin écrit directement) :
- Fil d'actualité du vestiaire (rumeurs, tensions visibles/invisibles)
- Messages de pression de la direction, contextualisés
- Choix de communication (conférence de presse, message interne)
- Historique des relations par joueur

---

## 5. Contraintes techniques & coûts

- **BepInEx** : injection il2cpp, lecture/écriture save FM26
- **API Claude (Haiku)** : cache local par contexte pour éviter les appels redondants (cf. modèle déjà validé sur CoachLab, ~0,60€ pour 200-300 générations)
- **Compatible avec** : Increase Realism Megapack, pack de difficulté Hardcore/Berserk, mods cosmétiques
- **Incompatible avec** : bases rétro (2000/01) si un jour tu changes de save — les deux "mondes" ne se combinent pas

---

## 6. Roadmap de développement

| Phase | Contenu | Environnement |
|---|---|---|
| 0 | Setup projet, environnement BepInEx sur macOS, hello-world plugin | Claude Code |
| 1 | Système de traits/affinités (données pures, testable hors-jeu) | Claude Code |
| 2 | Premier archétype complet (clash vestiaire) bout en bout | Claude Code |
| 3 | Écriture dans la save via l'éditeur (moral, réputation) | Claude Code |
| 4 | Interface "téléphone" basique | Claude Code |
| 5 | Extension à 5-6 archétypes supplémentaires | Claude Code |

---

*Document de travail — à faire évoluer au fil du développement, comme le CLAUDE.md de CoachLab.*
