using FM26.Engine.Traits;

namespace FM26.Engine.Affinite;

/// <summary>Représentation minimale d'un joueur/staff nécessaire au calcul d'affinité.</summary>
public sealed record Personne
{
    public string Id { get; }
    public string Poste { get; }
    public PersonalityTraits Traits { get; }

    public Personne(string id, string poste, PersonalityTraits traits)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("L'identifiant de la personne ne peut pas être vide.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(poste))
        {
            throw new ArgumentException("Le poste ne peut pas être vide.", nameof(poste));
        }

        Id = id;
        Poste = poste;
        Traits = traits;
    }
}
