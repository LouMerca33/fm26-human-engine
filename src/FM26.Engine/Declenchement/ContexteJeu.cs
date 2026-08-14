namespace FM26.Engine.Declenchement;

/// <summary>
/// Contexte sportif du club au moment du calcul — alimente le "modificateur_contexte"
/// de la formule de déclenchement (spec §2.3 : résultats, classement, série).
/// </summary>
public sealed record ContexteJeu
{
    /// <summary>Nombre de défaites consécutives récentes (0 = pas de série en cours).</summary>
    public int SerieDefaitesConsecutives { get; }

    /// <summary>Position dans le classement normalisée : 0.0 = leader du championnat, 1.0 = dernier.</summary>
    public double PositionClassementNormalisee { get; }

    public ContexteJeu(int serieDefaitesConsecutives, double positionClassementNormalisee)
    {
        if (serieDefaitesConsecutives < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(serieDefaitesConsecutives), serieDefaitesConsecutives,
                "La série de défaites consécutives ne peut pas être négative.");
        }

        if (positionClassementNormalisee is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(positionClassementNormalisee), positionClassementNormalisee,
                "La position de classement normalisée doit être comprise entre 0 et 1.");
        }

        SerieDefaitesConsecutives = serieDefaitesConsecutives;
        PositionClassementNormalisee = positionClassementNormalisee;
    }
}
