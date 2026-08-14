namespace FM26.Engine.Orchestration;

/// <summary>
/// Conséquences chiffrées d'une phase ou d'une branche d'archétype (spec §3 : "conséquences
/// immédiates", branches de consequence_moyen_terme). Chaque champ correspond à un impact
/// concrètement utilisé par clash_vestiaire ; pas de champ générique non consommé.
///
/// Propriétés en <c>get</c> seul (pas <c>init</c>), comme <see cref="Traits.PersonalityTraits"/> en
/// Couche 1 : avec <c>init</c>, une expression <c>with</c> ou un initialiseur d'objet appellent le
/// constructeur sans-paramètre synthétisé des record structs, pas le constructeur ci-dessous, ce
/// qui contournerait entièrement la validation <c>double.IsFinite</c>. En <c>get</c> seul, le
/// compilateur refuse toute affectation hors constructeur — la validation ne peut pas être
/// court-circuitée. Pas de méthode <c>with</c> possible en conséquence : dériver une variante passe
/// par une nouvelle construction explicite (voir <see cref="Orchestration.OrchestrateurClashVestiaire"/>).
/// </summary>
public readonly record struct EffetsArchetype
{
    /// <summary>Impact sur le moral de l'ensemble du groupe.</summary>
    public double MoralEquipe { get; }

    /// <summary>Impact sur la cohésion du vestiaire.</summary>
    public double Cohesion { get; }

    /// <summary>Impact sur le moral d'un joueur précis visé par la conséquence (ex. le joueur sanctionné).</summary>
    public double MoralJoueurCible { get; }

    /// <summary>Impact sur le respect de l'autorité du club au sein du groupe.</summary>
    public double RespectAutorite { get; }

    /// <summary>Impact sur la réputation du club (ex. fuite média rendue publique).</summary>
    public double ReputationClub { get; }

    /// <summary>
    /// Impact sur l'affinité de la paire de joueurs impliquée (spec §2.2 : l'affinité est
    /// "recalculée après chaque événement impliquant la paire"). Consommé par
    /// <see cref="InstanceClashVestiaire.ConstruireHistoriquePourAffinite"/> pour produire des
    /// <see cref="Affinite.EvenementHistorique"/> réinjectables dans <see cref="Affinite.AffiniteCalculator"/> —
    /// c'est ce qui referme la boucle de rétroaction plutôt que de laisser l'affinité de la paire
    /// inchangée après un clash déjà résolu.
    /// </summary>
    public double ImpactAffinitePaire { get; }

    public EffetsArchetype(
        double moralEquipe = 0.0,
        double cohesion = 0.0,
        double moralJoueurCible = 0.0,
        double respectAutorite = 0.0,
        double reputationClub = 0.0,
        double impactAffinitePaire = 0.0)
    {
        MoralEquipe = ValiderFini(moralEquipe, nameof(moralEquipe));
        Cohesion = ValiderFini(cohesion, nameof(cohesion));
        MoralJoueurCible = ValiderFini(moralJoueurCible, nameof(moralJoueurCible));
        RespectAutorite = ValiderFini(respectAutorite, nameof(respectAutorite));
        ReputationClub = ValiderFini(reputationClub, nameof(reputationClub));
        ImpactAffinitePaire = ValiderFini(impactAffinitePaire, nameof(impactAffinitePaire));
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
