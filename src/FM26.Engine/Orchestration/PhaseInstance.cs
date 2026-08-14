namespace FM26.Engine.Orchestration;

/// <summary>État d'avancement d'une <see cref="InstanceClashVestiaire"/> dans la séquence de phases.</summary>
public enum PhaseInstance
{
    Incident,
    ReactionPresse,
    ConsequenceMoyenTerme,
    Terminee,
}
