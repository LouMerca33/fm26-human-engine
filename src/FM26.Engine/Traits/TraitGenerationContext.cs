namespace FM26.Engine.Traits;

/// <summary>
/// Contexte utilisé pour générer un profil de personnalité cohérent (spec §2.1) :
/// pas de tirage purement random, la génération tient compte de l'âge et du poste.
/// </summary>
public sealed record TraitGenerationContext
{
    public const int AgeMin = 15;
    public const int AgeMax = 50;

    public string IdentifiantJoueur { get; }
    public int Age { get; }
    public string? Nationalite { get; }
    public string? Poste { get; }

    public TraitGenerationContext(string identifiantJoueur, int age, string? nationalite = null, string? poste = null)
    {
        if (string.IsNullOrWhiteSpace(identifiantJoueur))
        {
            throw new ArgumentException(
                "L'identifiant du joueur ne peut pas être vide.", nameof(identifiantJoueur));
        }

        if (age < AgeMin || age > AgeMax)
        {
            throw new ArgumentOutOfRangeException(
                nameof(age), age, $"L'âge doit être compris entre {AgeMin} et {AgeMax}.");
        }

        IdentifiantJoueur = identifiantJoueur;
        Age = age;
        Nationalite = nationalite;
        Poste = poste;
    }
}
