namespace FM26.Engine.Affinite;

/// <summary>
/// Trace d'un événement passé impliquant une paire — "les tensions passées pèsent plus
/// lourd" (spec §2.2). Pondéré par ancienneté dans <see cref="AffiniteCalculator"/> : plus
/// <see cref="JoursDepuis"/> est grand, plus son poids dans le score actuel diminue.
/// </summary>
public sealed record EvenementHistorique
{
    /// <summary>Contribution brute au score d'affinité : négatif = tension, positif = rapprochement.</summary>
    public double ImpactAffinite { get; }

    /// <summary>Ancienneté de l'événement au moment du calcul, en jours (0 = aujourd'hui).</summary>
    public int JoursDepuis { get; }

    public EvenementHistorique(double impactAffinite, int joursDepuis)
    {
        if (!double.IsFinite(impactAffinite))
        {
            throw new ArgumentOutOfRangeException(
                nameof(impactAffinite), impactAffinite, "L'impact d'un événement doit être un nombre fini.");
        }

        if (joursDepuis < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(joursDepuis), joursDepuis, "L'ancienneté d'un événement ne peut pas être négative.");
        }

        ImpactAffinite = impactAffinite;
        JoursDepuis = joursDepuis;
    }
}
