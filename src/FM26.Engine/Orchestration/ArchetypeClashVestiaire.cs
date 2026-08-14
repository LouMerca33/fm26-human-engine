namespace FM26.Engine.Orchestration;

/// <summary>
/// Archétype de référence "clash_vestiaire" (spec §3), traduit fidèlement depuis l'exemple JSON
/// de la spec en types C# forts : déclencheurs, séquence ordonnée de 3 phases (incident →
/// reaction_presse → consequence_moyen_terme) et métadonnée écriture_save.
/// </summary>
public sealed record ArchetypeClashVestiaire
{
    public const string Id = "clash_vestiaire";

    public DeclencheursClashVestiaire Declencheurs { get; }
    public PhaseIncident Incident { get; }
    public PhaseReactionPresse ReactionPresse { get; }
    public PhaseConsequenceMoyenTerme ConsequenceMoyenTerme { get; }
    public EcritureSaveArchetype EcritureSave { get; }

    /// <summary>Séquence ordonnée des phases, fidèle à l'ordre du tableau "phases" du schéma JSON.</summary>
    public IReadOnlyList<PhaseArchetypeBase> Phases { get; }

    public ArchetypeClashVestiaire(
        DeclencheursClashVestiaire declencheurs,
        PhaseIncident incident,
        PhaseReactionPresse reactionPresse,
        PhaseConsequenceMoyenTerme consequenceMoyenTerme,
        EcritureSaveArchetype ecritureSave)
    {
        Declencheurs = declencheurs ?? throw new ArgumentNullException(nameof(declencheurs));
        Incident = incident ?? throw new ArgumentNullException(nameof(incident));
        ReactionPresse = reactionPresse ?? throw new ArgumentNullException(nameof(reactionPresse));
        ConsequenceMoyenTerme = consequenceMoyenTerme ?? throw new ArgumentNullException(nameof(consequenceMoyenTerme));
        EcritureSave = ecritureSave ?? throw new ArgumentNullException(nameof(ecritureSave));

        Phases = new PhaseArchetypeBase[] { Incident, ReactionPresse, ConsequenceMoyenTerme };
    }

    /// <summary>
    /// Instance de référence, calibrée sur l'exemple JSON de la spec §3 : déclencheurs
    /// (série_défaites >= 2, affinité_paire <= -30, probabilité_base 0.04), incident
    /// (moral_equipe -8, cohesion -12), reaction_presse (délai 1 jour), consequence_moyen_terme
    /// (délai 14 jours). Les branches apaiser/couvrir n'ont pas de valeurs chiffrées dans la
    /// spec (seule sanctionner en a : moral -20 / respect autorité +10) — premier jet à calibrer,
    /// même réserve que les formules de la Couche 1 (voir STATUS.md).
    /// </summary>
    public static ArchetypeClashVestiaire CreerReference() => new(
        declencheurs: new DeclencheursClashVestiaire(serieDefaitesMinimale: 2, affiniteMaximale: -30.0, probabiliteBase: 0.04),
        incident: new PhaseIncident(new EffetsArchetype(moralEquipe: -8.0, cohesion: -12.0)),
        reactionPresse: new PhaseReactionPresse(delaiJours: 1),
        consequenceMoyenTerme: new PhaseConsequenceMoyenTerme(
            delaiJours: 14,
            effetsParBranche: new Dictionary<ChoixCommunication, EffetsArchetype>
            {
                // Apaiser : "tension latente, probabilité de réplique réduite" — récupération
                // partielle du choc de l'incident, pas totale (la tension reste latente).
                [ChoixCommunication.Apaiser] = new EffetsArchetype(moralEquipe: 3.0, cohesion: 5.0),

                // Couvrir : effet de base tant qu'aucune fuite ne se produit (l'incertitude
                // pèse un peu sur la cohésion). Le risque de fuite lié à la discrétion des
                // témoins est calculé dynamiquement par OrchestrateurClashVestiaire, pas ici.
                [ChoixCommunication.Couvrir] = new EffetsArchetype(cohesion: -2.0),

                // Sanctionner : seule branche chiffrée dans la spec telle quelle.
                [ChoixCommunication.Sanctionner] = new EffetsArchetype(moralJoueurCible: -20.0, respectAutorite: 10.0),
            }),
        ecritureSave: new EcritureSaveArchetype(new[] { "morale", "reputation_club", "relation_joueur_staff" }));
}
