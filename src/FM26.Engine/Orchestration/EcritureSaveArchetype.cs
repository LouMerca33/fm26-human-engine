namespace FM26.Engine.Orchestration;

/// <summary>
/// Métadonnée déclarative "écriture_save" du schéma JSON de la spec (§3) : liste les champs de
/// la save FM26 que cet archétype modifie in fine. Purement informatif à ce stade — l'écriture
/// réelle dans la save (Phase 3 de la roadmap, §6) est hors périmètre de cette couche, qui reste
/// pur C# testable hors-jeu.
/// </summary>
public sealed record EcritureSaveArchetype
{
    public IReadOnlyList<string> ChampsModifies { get; }

    public EcritureSaveArchetype(IReadOnlyList<string> champsModifies)
    {
        ArgumentNullException.ThrowIfNull(champsModifies);

        if (champsModifies.Count == 0)
        {
            throw new ArgumentException("La liste des champs modifiés ne peut pas être vide.", nameof(champsModifies));
        }

        ChampsModifies = champsModifies;
    }
}
