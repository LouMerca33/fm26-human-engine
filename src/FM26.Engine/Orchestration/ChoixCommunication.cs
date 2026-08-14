namespace FM26.Engine.Orchestration;

/// <summary>
/// Options de communication proposées à la phase "reaction_presse" de l'archétype
/// clash_vestiaire (spec §3). Spécifique à cet archétype pour l'instant : les archétypes
/// futurs (fuite média, boycott, etc.) pourront définir leur propre jeu de choix plutôt que
/// de forcer une énumération commune non justifiée par le contenu actuel de la spec.
/// </summary>
public enum ChoixCommunication
{
    Apaiser,
    Couvrir,
    Sanctionner,
}
