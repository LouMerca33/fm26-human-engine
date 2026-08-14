namespace FM26.Engine.Orchestration;

/// <summary>
/// Phase "consequence_moyen_terme" de clash_vestiaire (spec §3) : intervient <see cref="PhaseArchetypeBase.DelaiJours"/>
/// jours après le début de reaction_presse, résout les conséquences chiffrées de la branche
/// correspondant au choix fait durant reaction_presse (apaiser / couvrir / sanctionner). Le
/// choix a déjà été fait à la phase précédente : cette phase n'attend pas de nouveau choix
/// utilisateur, elle applique la branche déjà sélectionnée.
/// </summary>
public sealed record PhaseConsequenceMoyenTerme : PhaseArchetypeBase
{
    public override string Nom => "consequence_moyen_terme";

    private readonly IReadOnlyDictionary<ChoixCommunication, EffetsArchetype> _effetsParBranche;

    public PhaseConsequenceMoyenTerme(int delaiJours, IReadOnlyDictionary<ChoixCommunication, EffetsArchetype> effetsParBranche)
        : base(delaiJours, choixUtilisateur: false)
    {
        ArgumentNullException.ThrowIfNull(effetsParBranche);

        foreach (ChoixCommunication choix in Enum.GetValues<ChoixCommunication>())
        {
            if (!effetsParBranche.ContainsKey(choix))
            {
                throw new ArgumentException(
                    $"Les effets de la branche '{choix}' sont manquants.", nameof(effetsParBranche));
            }
        }

        // Copie défensive : sans elle, un appelant qui réutilise et mute ensuite le dictionnaire
        // d'origine (ex. un futur chargement depuis JSON/config, spec §6 phase 5) empoisonnerait
        // silencieusement cette instance déjà construite.
        _effetsParBranche = new Dictionary<ChoixCommunication, EffetsArchetype>(effetsParBranche);
    }

    public EffetsArchetype ObtenirEffets(ChoixCommunication choix) => _effetsParBranche[choix];
}
