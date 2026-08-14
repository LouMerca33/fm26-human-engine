using FM26.Engine.Affinite;

namespace FM26.Engine.Orchestration;

/// <summary>
/// Instance vivante d'un archétype clash_vestiaire pour une paire de joueurs donnée : garde la
/// phase courante, le jour d'entrée dans cette phase et le choix de communication une fois fait.
/// Mutable par construction (l'état avance dans le temps), mais toutes les mutations passent par
/// <see cref="OrchestrateurClashVestiaire"/> — pas de setters publics.
/// </summary>
public sealed class InstanceClashVestiaire
{
    public Personne JoueurA { get; }
    public Personne JoueurB { get; }

    public PhaseInstance PhaseActuelle { get; private set; }

    /// <summary>Jour (compteur entier arbitraire, cohérent avec l'appelant) d'entrée dans <see cref="PhaseActuelle"/>.</summary>
    public int JourEntreeDansPhase { get; private set; }

    /// <summary>Choix de communication fait durant reaction_presse ; null tant qu'aucun choix n'a été fourni.</summary>
    public ChoixCommunication? Choix { get; private set; }

    /// <summary>Effets déjà appliqués, dans l'ordre chronologique (incident, puis branche de consequence_moyen_terme une fois résolue).</summary>
    public IReadOnlyList<EffetsArchetype> EffetsAppliques => _effetsAppliques;

    private readonly List<EffetsArchetype> _effetsAppliques = new();

    /// <summary>Jour d'application de chaque entrée correspondante de <see cref="_effetsAppliques"/> (même index).</summary>
    private readonly List<int> _joursEffetsAppliques = new();

    internal InstanceClashVestiaire(Personne joueurA, Personne joueurB, int jourDemarrage, EffetsArchetype effetsIncident)
    {
        ArgumentNullException.ThrowIfNull(joueurA);
        ArgumentNullException.ThrowIfNull(joueurB);

        if (joueurA.Id == joueurB.Id)
        {
            throw new ArgumentException("Un clash de vestiaire nécessite deux joueurs distincts.", nameof(joueurB));
        }

        JoueurA = joueurA;
        JoueurB = joueurB;
        PhaseActuelle = PhaseInstance.Incident;
        JourEntreeDansPhase = jourDemarrage;
        AjouterEffetApplique(effetsIncident, jourDemarrage);
    }

    internal void PasserAReactionPresse(int jour)
    {
        PhaseActuelle = PhaseInstance.ReactionPresse;
        JourEntreeDansPhase = jour;
    }

    internal void EnregistrerChoix(ChoixCommunication choix)
    {
        Choix ??= choix;
    }

    internal void PasserAConsequence(int jour, EffetsArchetype effets)
    {
        PhaseActuelle = PhaseInstance.ConsequenceMoyenTerme;
        JourEntreeDansPhase = jour;
        AjouterEffetApplique(effets, jour);
    }

    internal void Terminer()
    {
        PhaseActuelle = PhaseInstance.Terminee;
    }

    private void AjouterEffetApplique(EffetsArchetype effets, int jour)
    {
        _effetsAppliques.Add(effets);
        _joursEffetsAppliques.Add(jour);
    }

    /// <summary>
    /// Matérialise les <see cref="EffetsArchetype.ImpactAffinitePaire"/> déjà appliqués en
    /// <see cref="EvenementHistorique"/> réinjectables dans <see cref="Affinite.AffiniteCalculator.CalculerScore"/>
    /// pour la paire <see cref="JoueurA"/>/<see cref="JoueurB"/> — c'est ce qui referme la boucle de
    /// rétroaction de la spec §2.2 ("recalculé après chaque événement impliquant la paire") : sans
    /// cet appel, un clash déjà résolu n'a aucun effet sur la probabilité d'un futur clash entre les
    /// deux mêmes joueurs. Impacts nuls omis. L'ancienneté de chaque événement est recalculée par
    /// rapport à <paramref name="jourActuel"/>, pas figée au moment de l'application de l'effet —
    /// cohérent avec la sémantique de <see cref="EvenementHistorique.JoursDepuis"/> ("au moment du calcul").
    /// </summary>
    public IReadOnlyCollection<EvenementHistorique> ConstruireHistoriquePourAffinite(int jourActuel)
    {
        var historique = new List<EvenementHistorique>();

        for (int i = 0; i < _effetsAppliques.Count; i++)
        {
            double impact = _effetsAppliques[i].ImpactAffinitePaire;
            if (impact == 0.0)
            {
                continue;
            }

            int joursDepuis = Math.Max(0, jourActuel - _joursEffetsAppliques[i]);
            historique.Add(new EvenementHistorique(impact, joursDepuis));
        }

        return historique;
    }
}
