namespace FM26.Engine.Orchestration;

/// <summary>
/// Base commune d'une phase d'archétype (spec §3 : liste ordonnée "phases", chaque objet
/// portant un nom, un délai en jours depuis la phase précédente et un flag choix_utilisateur).
/// </summary>
public abstract record PhaseArchetypeBase
{
    /// <summary>Nom de la phase tel qu'il apparaît dans le schéma JSON de la spec (ex. "incident").</summary>
    public abstract string Nom { get; }

    /// <summary>Délai en jours depuis le début de la phase précédente avant que celle-ci ne puisse débuter.</summary>
    public int DelaiJours { get; }

    /// <summary>Si vrai, cette phase attend un choix utilisateur avant de pouvoir se résoudre.</summary>
    public bool ChoixUtilisateur { get; }

    protected PhaseArchetypeBase(int delaiJours, bool choixUtilisateur)
    {
        if (delaiJours < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(delaiJours), delaiJours, "Le délai d'une phase ne peut pas être négatif.");
        }

        DelaiJours = delaiJours;
        ChoixUtilisateur = choixUtilisateur;
    }
}
