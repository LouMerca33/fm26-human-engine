namespace FM26.Engine.Orchestration;

/// <summary>
/// Phase "reaction_presse" de clash_vestiaire (spec §3) : intervient <see cref="PhaseArchetypeBase.DelaiJours"/>
/// jours après le début de l'incident, propose 3 options de communication (apaiser / couvrir /
/// sanctionner, générées via <c>IGenerateurNarratif</c>) et attend un choix utilisateur — c'est
/// la seule phase de clash_vestiaire où <see cref="PhaseArchetypeBase.ChoixUtilisateur"/> vaut vrai.
/// </summary>
public sealed record PhaseReactionPresse : PhaseArchetypeBase
{
    public override string Nom => "reaction_presse";

    public PhaseReactionPresse(int delaiJours)
        : base(delaiJours, choixUtilisateur: true)
    {
    }
}
