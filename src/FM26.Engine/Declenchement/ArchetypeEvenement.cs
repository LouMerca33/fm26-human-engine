namespace FM26.Engine.Declenchement;

/// <summary>Config minimale d'un archétype d'événement pour le calcul de déclenchement (spec §2.3 / §3).</summary>
public sealed record ArchetypeEvenement
{
    /// <summary>Identifiant de l'archétype, ex. "clash_vestiaire".</summary>
    public string Id { get; }

    /// <summary>Probabilité mensuelle de base avant modificateurs, dans [0, 1].</summary>
    public double ProbabiliteBase { get; }

    public ArchetypeEvenement(string id, double probabiliteBase)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("L'identifiant de l'archétype ne peut pas être vide.", nameof(id));
        }

        if (probabiliteBase is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(probabiliteBase), probabiliteBase, "La probabilité de base doit être comprise entre 0 et 1.");
        }

        Id = id;
        ProbabiliteBase = probabiliteBase;
    }
}
