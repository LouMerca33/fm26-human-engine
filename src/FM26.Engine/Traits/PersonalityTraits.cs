namespace FM26.Engine.Traits;

/// <summary>
/// Profil de personnalité d'un joueur ou membre du staff (spec §2.1).
/// Généré une seule fois à la création/arrivée, puis persistant.
/// </summary>
public readonly record struct PersonalityTraits
{
    public const int ValeurMin = 1;
    public const int ValeurMax = 20;

    public int Ego { get; }
    public int Leadership { get; }
    public int LoyauteGroupe { get; }
    public int TolerancePression { get; }
    public int Discretion { get; }
    public int Ambition { get; }

    public PersonalityTraits(int ego, int leadership, int loyauteGroupe, int tolerancePression, int discretion, int ambition)
    {
        Ego = ValiderTrait(ego, nameof(Ego));
        Leadership = ValiderTrait(leadership, nameof(Leadership));
        LoyauteGroupe = ValiderTrait(loyauteGroupe, nameof(LoyauteGroupe));
        TolerancePression = ValiderTrait(tolerancePression, nameof(TolerancePression));
        Discretion = ValiderTrait(discretion, nameof(Discretion));
        Ambition = ValiderTrait(ambition, nameof(Ambition));
    }

    private static int ValiderTrait(int valeur, string nom)
    {
        if (valeur < ValeurMin || valeur > ValeurMax)
        {
            throw new ArgumentOutOfRangeException(
                nom, valeur, $"{nom} doit être compris entre {ValeurMin} et {ValeurMax}.");
        }

        return valeur;
    }
}
