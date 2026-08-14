namespace FM26.Engine.Traits;

/// <summary>
/// Génère le profil de personnalité d'un joueur/staff à sa création (spec §2.1).
///
/// La spec décrit à terme un appel Claude unique par joueur pour garantir la cohérence
/// narrative. Cette interface découple cette décision : la Couche 1 (ce projet) fournit
/// une implémentation déterministe et pure C#, testable hors-jeu sans dépendance réseau ;
/// la Couche 2 pourra brancher une implémentation appuyée sur l'API Claude derrière la
/// même interface sans rien changer au moteur d'affinités/déclenchement qui consomme
/// <see cref="PersonalityTraits"/>.
/// </summary>
public interface IPersonalityTraitGenerator
{
    PersonalityTraits Generate(TraitGenerationContext contexte);
}
