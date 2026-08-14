namespace FM26.Engine.Traits;

/// <summary>
/// Générateur déterministe de profils de personnalité : même contexte en entrée,
/// même profil en sortie (pas de dépendance au temps ni à un état global).
///
/// Cohérence contextuelle (spec §2.1, "pas de tirage purement random") : chaque trait est
/// tiré autour d'un centre biaisé par l'âge du joueur plutôt qu'autour du milieu de
/// l'échelle, puis bruité par un tirage pseudo-aléatoire dérivé d'une empreinte stable de
/// l'identifiant du joueur (voir <see cref="EmpreinteDeterministe"/>) — donc reproductible
/// d'une exécution à l'autre, contrairement à <c>string.GetHashCode()</c> qui est randomisé
/// par processus dans .NET.
/// </summary>
public sealed class DeterministicPersonalityTraitGenerator : IPersonalityTraitGenerator
{
    private const double AmplitudeBruit = 6.0;

    public PersonalityTraits Generate(TraitGenerationContext contexte)
    {
        ArgumentNullException.ThrowIfNull(contexte);

        double facteurAge = FacteurAgeCroissant(contexte.Age);
        double facteurLeadershipAge = FacteurAgeLeadership(contexte.Age);

        int ego = TirerTrait(contexte.IdentifiantJoueur, nameof(PersonalityTraits.Ego), centre: 10.5);
        int leadership = TirerTrait(contexte.IdentifiantJoueur, nameof(PersonalityTraits.Leadership), centre: 8.0 + 8.0 * facteurLeadershipAge);
        int loyauteGroupe = TirerTrait(contexte.IdentifiantJoueur, nameof(PersonalityTraits.LoyauteGroupe), centre: 10.5);
        int tolerancePression = TirerTrait(contexte.IdentifiantJoueur, nameof(PersonalityTraits.TolerancePression), centre: 9.0 + 6.0 * facteurAge);
        int discretion = TirerTrait(contexte.IdentifiantJoueur, nameof(PersonalityTraits.Discretion), centre: 10.5);
        int ambition = TirerTrait(contexte.IdentifiantJoueur, nameof(PersonalityTraits.Ambition), centre: 14.0 - 5.0 * facteurAge);

        return new PersonalityTraits(
            ego: ego,
            leadership: leadership,
            loyauteGroupe: loyauteGroupe,
            tolerancePression: tolerancePression,
            discretion: discretion,
            ambition: ambition);
    }

    /// <summary>0.0 à 15/16 ans, 1.0 à partir de 34 ans — progression linéaire entre les deux.</summary>
    private static double FacteurAgeCroissant(int age) =>
        Math.Clamp((age - TraitGenerationContext.AgeMin) / 19.0, 0.0, 1.0);

    /// <summary>Pic autour de 30 ans (capitaine dans sa force de l'âge), plus faible aux extrêmes.</summary>
    private static double FacteurAgeLeadership(int age) =>
        Math.Clamp(1.0 - Math.Abs(age - 30) / 20.0, 0.0, 1.0);

    private static int TirerTrait(string identifiantJoueur, string nomTrait, double centre)
    {
        int seed = EmpreinteDeterministe($"{identifiantJoueur}:{nomTrait}");
        var rng = new Random(seed);

        // Somme de deux tirages uniformes : distribution triangulaire, plus resserrée
        // autour du centre qu'un simple bruit uniforme.
        double bruit = (rng.NextDouble() + rng.NextDouble() - 1.0) * AmplitudeBruit;

        int valeur = (int)Math.Round(centre + bruit, MidpointRounding.AwayFromZero);
        return Math.Clamp(valeur, PersonalityTraits.ValeurMin, PersonalityTraits.ValeurMax);
    }

    /// <summary>
    /// Hachage FNV-1a 32 bits : stable entre exécutions et plateformes, contrairement à
    /// <c>string.GetHashCode()</c>. Utilisé uniquement pour dériver une graine de PRNG,
    /// pas à des fins cryptographiques.
    /// </summary>
    internal static int EmpreinteDeterministe(string cle)
    {
        unchecked
        {
            const uint offsetFnv = 2166136261;
            const uint primeFnv = 16777619;

            uint hash = offsetFnv;
            foreach (char c in cle)
            {
                hash ^= c;
                hash *= primeFnv;
            }

            return (int)hash;
        }
    }
}
