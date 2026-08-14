namespace FM26.Engine.Narratif;

/// <summary>Contexte minimal nécessaire pour générer le texte de la scène d'un clash_vestiaire et ses options de communication.</summary>
public sealed record SceneIncidentContexte
{
    public string NomJoueurA { get; }
    public string NomJoueurB { get; }
    public string NomClub { get; }

    public SceneIncidentContexte(string nomJoueurA, string nomJoueurB, string nomClub)
    {
        NomJoueurA = ValiderNonVide(nomJoueurA, nameof(nomJoueurA));
        NomJoueurB = ValiderNonVide(nomJoueurB, nameof(nomJoueurB));
        NomClub = ValiderNonVide(nomClub, nameof(nomClub));
    }

    private static string ValiderNonVide(string valeur, string nom)
    {
        if (string.IsNullOrWhiteSpace(valeur))
        {
            throw new ArgumentException($"{nom} ne peut pas être vide.", nom);
        }

        return valeur;
    }
}
