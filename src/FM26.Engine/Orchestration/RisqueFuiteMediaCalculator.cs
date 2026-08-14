using FM26.Engine.Affinite;
using FM26.Engine.Traits;

namespace FM26.Engine.Orchestration;

/// <summary>
/// Calcule le risque de fuite média de la branche "couvrir" (spec §3 : "risque de fuite média
/// (probabilité liée au trait discretion des témoins)").
///
/// Décision de conception : la spec associe le trait <c>discretion</c> à "la probabilité d'être
/// la source d'une fuite média" (§2.1), ce qui est ambigu sur le sens de la corrélation. On
/// retient ici le sens littéral du mot en français — une personne discrète garde un secret,
/// une personne peu discrète le répète — donc une discrétion élevée chez les témoins réduit le
/// risque de fuite. Les deux protagonistes du clash sont pris comme témoins directs de
/// l'incident (aucune liste de témoins tiers n'existe encore dans le moteur).
/// </summary>
public static class RisqueFuiteMediaCalculator
{
    public static double CalculerProbabiliteFuite(Personne temoinA, Personne temoinB)
    {
        ArgumentNullException.ThrowIfNull(temoinA);
        ArgumentNullException.ThrowIfNull(temoinB);

        double discretionMoyenne = (temoinA.Traits.Discretion + temoinB.Traits.Discretion) / 2.0;
        double probabilite = 1.0 - discretionMoyenne / PersonalityTraits.ValeurMax;

        return Math.Clamp(probabilite, 0.0, 1.0);
    }
}
