namespace FM26.Engine.Affinite;

/// <summary>
/// Calcule le score d'affinité entre deux personnes (spec §2.2) :
/// affinite(A,B) = f(écart_ego, complémentarité_leadership, concurrence_poste, historique_evenements)
/// Résultat borné à [-100, 100] : -100 = tension forte, +100 = bonne entente.
///
/// Chaque composante ci-dessous est un premier jet de calibrage (poids choisis pour rester
/// dans des ordres de grandeur raisonnables). Couche 2 pourra recalibrer ces poids une fois
/// le système confronté à du contenu narratif réel — voir STATUS.md.
/// </summary>
public static class AffiniteCalculator
{
    private const double EcartMaxTrait = 19.0; // amplitude max d'un écart entre deux traits sur l'échelle 1-20
    private const double ScoreMin = -100.0;
    private const double ScoreMax = 100.0;

    public static double CalculerScore(Personne a, Personne b, IReadOnlyCollection<EvenementHistorique>? historique = null)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        double ego = ComposanteEgo(a.Traits.Ego, b.Traits.Ego);
        double leadership = ComposanteLeadership(a.Traits.Leadership, b.Traits.Leadership);
        double poste = ComposanteConcurrencePoste(a, b);
        double hist = ComposanteHistorique(historique);

        double brut = ego + leadership + poste + hist;
        return Math.Clamp(brut, ScoreMin, ScoreMax);
    }

    /// <summary>
    /// Deux ego élevés et proches se disputent la vedette (tension) ; un écart marqué
    /// (hiérarchie acceptée) ou des ego bas atténuent l'effet. Contribution dans [-30, 0].
    /// </summary>
    private static double ComposanteEgo(int egoA, int egoB)
    {
        double ecart = Math.Abs(egoA - egoB);
        double moyenne = (egoA + egoB) / 2.0;
        double facteurClash = (moyenne / 20.0) * (1.0 - ecart / EcartMaxTrait);
        return -30.0 * facteurClash;
    }

    /// <summary>
    /// Un écart de leadership marqué est complémentaire (l'un dirige, l'autre suit :
    /// contribution positive). Deux leaders forts et proches en niveau se disputent
    /// l'ascendant sur le groupe (contribution négative). Résultat dans [-15, 15].
    /// </summary>
    private static double ComposanteLeadership(int leadershipA, int leadershipB)
    {
        double ecart = Math.Abs(leadershipA - leadershipB);
        double moyenne = (leadershipA + leadershipB) / 2.0;
        double complementarite = ecart / EcartMaxTrait;
        double luttePouvoir = (moyenne / 20.0) * (1.0 - ecart / EcartMaxTrait);
        return 15.0 * complementarite - 15.0 * luttePouvoir;
    }

    /// <summary>
    /// Même poste = concurrence directe pour une place dans l'équipe, amplifiée par
    /// l'ambition combinée des deux joueurs. Contribution dans [-20, 0], 0 si postes différents.
    /// </summary>
    private static double ComposanteConcurrencePoste(Personne a, Personne b)
    {
        if (!string.Equals(a.Poste, b.Poste, StringComparison.OrdinalIgnoreCase))
        {
            return 0.0;
        }

        double ambitionMoyenne = (a.Traits.Ambition + b.Traits.Ambition) / 2.0;
        return -10.0 - (ambitionMoyenne / 20.0) * 10.0;
    }

    /// <summary>
    /// Somme des impacts passés, pondérés par ancienneté (décroissance ~ moitié de poids
    /// tous les 90 jours) : un événement récent pèse plus lourd qu'un ancien de même magnitude.
    /// </summary>
    private static double ComposanteHistorique(IReadOnlyCollection<EvenementHistorique>? historique)
    {
        if (historique is null || historique.Count == 0)
        {
            return 0.0;
        }

        double somme = 0.0;
        foreach (var evenement in historique)
        {
            double decroissance = 1.0 / (1.0 + evenement.JoursDepuis / 90.0);
            somme += evenement.ImpactAffinite * decroissance;
        }

        return somme;
    }
}
