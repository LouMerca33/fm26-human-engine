namespace FM26.Engine.Orchestration;

/// <summary>
/// Phase "incident" de clash_vestiaire (spec §3) : déclenchée immédiatement (délai 0, pas de
/// choix utilisateur), applique des conséquences chiffrées immédiates au moment de l'incident.
/// </summary>
public sealed record PhaseIncident : PhaseArchetypeBase
{
    public override string Nom => "incident";

    public EffetsArchetype ConsequencesImmediates { get; }

    public PhaseIncident(EffetsArchetype consequencesImmediates)
        : base(delaiJours: 0, choixUtilisateur: false)
    {
        ConsequencesImmediates = consequencesImmediates;
    }
}
