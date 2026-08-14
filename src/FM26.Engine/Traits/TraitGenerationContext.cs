namespace FM26.Engine.Traits;

/// <summary>
/// Contexte utilisé pour générer un profil de personnalité cohérent (spec §2.1) :
/// pas de tirage purement random, la génération tient compte de l'âge.
///
/// La spec mentionne aussi la nationalité et l'historique de carrière simulé comme
/// facteurs de cohérence "si dispo dans la base". Ces signaux ont surtout de la valeur
/// pour un générateur appuyé sur du texte (Couche 2, appel Claude) ; l'implémentation
/// déterministe de Couche 1 n'a pas de base justifiable pour les traduire en ajustement
/// numérique de traits, donc ils ne figurent pas ici tant qu'ils ne sont pas réellement
/// consommés — pas de paramètre accepté puis ignoré.
/// </summary>
public sealed record TraitGenerationContext
{
    public const int AgeMin = 15;
    public const int AgeMax = 50;

    public string IdentifiantJoueur { get; }
    public int Age { get; }

    public TraitGenerationContext(string identifiantJoueur, int age)
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
    }
}
