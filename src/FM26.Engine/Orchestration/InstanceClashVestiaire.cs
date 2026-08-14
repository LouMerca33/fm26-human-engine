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
        _effetsAppliques.Add(effetsIncident);
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
        _effetsAppliques.Add(effets);
    }

    internal void Terminer()
    {
        PhaseActuelle = PhaseInstance.Terminee;
    }
}
