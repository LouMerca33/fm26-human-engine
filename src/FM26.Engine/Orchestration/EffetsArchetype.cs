namespace FM26.Engine.Orchestration;

/// <summary>
/// Conséquences chiffrées d'une phase ou d'une branche d'archétype (spec §3 : "conséquences
/// immédiates", branches de consequence_moyen_terme). Chaque champ correspond à un impact
/// concrètement utilisé par clash_vestiaire ; pas de champ générique non consommé.
/// </summary>
public readonly record struct EffetsArchetype
{
    /// <summary>Impact sur le moral de l'ensemble du groupe.</summary>
    public double MoralEquipe { get; init; }

    /// <summary>Impact sur la cohésion du vestiaire.</summary>
    public double Cohesion { get; init; }

    /// <summary>Impact sur le moral d'un joueur précis visé par la conséquence (ex. le joueur sanctionné).</summary>
    public double MoralJoueurCible { get; init; }

    /// <summary>Impact sur le respect de l'autorité du club au sein du groupe.</summary>
    public double RespectAutorite { get; init; }

    /// <summary>Impact sur la réputation du club (ex. fuite média rendue publique).</summary>
    public double ReputationClub { get; init; }

    public EffetsArchetype(
        double moralEquipe = 0.0,
        double cohesion = 0.0,
        double moralJoueurCible = 0.0,
        double respectAutorite = 0.0,
        double reputationClub = 0.0)
    {
        MoralEquipe = ValiderFini(moralEquipe, nameof(moralEquipe));
        Cohesion = ValiderFini(cohesion, nameof(cohesion));
        MoralJoueurCible = ValiderFini(moralJoueurCible, nameof(moralJoueurCible));
        RespectAutorite = ValiderFini(respectAutorite, nameof(respectAutorite));
        ReputationClub = ValiderFini(reputationClub, nameof(reputationClub));
    }

    private static double ValiderFini(double valeur, string nom)
    {
        if (!double.IsFinite(valeur))
        {
            throw new ArgumentOutOfRangeException(nom, valeur, $"{nom} doit être un nombre fini.");
        }

        return valeur;
    }
}
