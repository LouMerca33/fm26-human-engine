using FM26.Engine.Orchestration;

namespace FM26.Engine.Narratif;

/// <summary>
/// Implémentation de repli de <see cref="IGenerateurNarratif"/> : templates fixes, aucun appel
/// réseau. Même rôle pour la Couche 2 que <see cref="Traits.DeterministicPersonalityTraitGenerator"/>
/// pour la Couche 1 — texte cohérent et déterministe en attendant une implémentation Claude.
/// </summary>
public sealed class GenerateurNarratifTemplate : IGenerateurNarratif
{
    public string GenererSceneIncident(SceneIncidentContexte contexte)
    {
        ArgumentNullException.ThrowIfNull(contexte);

        return $"À l'entraînement, {contexte.NomJoueurA} et {contexte.NomJoueurB} en viennent aux mots devant " +
               $"tout le groupe. La tension, latente depuis plusieurs semaines à {contexte.NomClub}, éclate au " +
               "grand jour. Le staff technique doit s'interposer pour calmer les esprits.";
    }

    public IReadOnlyDictionary<ChoixCommunication, string> GenererOptionsCommunication(SceneIncidentContexte contexte)
    {
        ArgumentNullException.ThrowIfNull(contexte);

        return new Dictionary<ChoixCommunication, string>
        {
            [ChoixCommunication.Apaiser] =
                $"Minimiser l'incident publiquement et organiser une médiation interne entre " +
                $"{contexte.NomJoueurA} et {contexte.NomJoueurB}.",

            [ChoixCommunication.Couvrir] =
                $"Ne rien communiquer à l'extérieur et traiter l'incident en interne, au risque " +
                $"qu'il finisse par fuiter dans la presse.",

            [ChoixCommunication.Sanctionner] =
                $"Sanctionner publiquement le fautif pour réaffirmer l'autorité du club, au risque " +
                $"d'entamer son moral.",
        };
    }
}
