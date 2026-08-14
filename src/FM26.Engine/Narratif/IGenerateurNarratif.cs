using FM26.Engine.Orchestration;

namespace FM26.Engine.Narratif;

/// <summary>
/// Génère le texte narratif d'un archétype d'événement (spec §3 : la scène de l'incident, les
/// options de communication de reaction_presse).
///
/// Même patron que <see cref="Traits.IPersonalityTraitGenerator"/> : cette interface découple le
/// moteur d'orchestration de l'implémentation concrète de génération de texte. La Couche 2 fournit
/// ici une implémentation de repli à base de templates fixes, pure C#, sans appel réseau. Une
/// implémentation appuyée sur l'API Claude pourra être branchée derrière la même interface plus
/// tard sans toucher au reste du moteur.
/// </summary>
public interface IGenerateurNarratif
{
    /// <summary>Scène de l'incident (2-3 phrases), à partir du contexte des 2 joueurs impliqués.</summary>
    string GenererSceneIncident(SceneIncidentContexte contexte);

    /// <summary>Les 3 options de communication (apaiser / couvrir / sanctionner) proposées durant reaction_presse.</summary>
    IReadOnlyDictionary<ChoixCommunication, string> GenererOptionsCommunication(SceneIncidentContexte contexte);
}
