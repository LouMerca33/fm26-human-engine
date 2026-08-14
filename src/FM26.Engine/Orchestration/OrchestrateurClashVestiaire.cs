using FM26.Engine.Affinite;
using FM26.Engine.Declenchement;

namespace FM26.Engine.Orchestration;

/// <summary>
/// Moteur d'orchestration de l'archétype clash_vestiaire (spec §3) : évalue le déclenchement
/// (portillon de contexte + formule pondérée de la Couche 1), démarre une instance et la fait
/// avancer d'une phase à l'autre selon le temps écoulé et, pour reaction_presse, selon le choix
/// utilisateur fourni.
///
/// <see cref="Avancer"/> ne fait progresser une instance que d'au plus une phase par appel, même
/// si <c>jourActuel</c> dépasse plusieurs seuils de délai d'un coup : un appelant qui vérifie
/// l'instance à chaque tick de jour convergera naturellement vers la bonne phase sans jamais
/// sauter la génération narrative d'une phase intermédiaire.
/// </summary>
public static class OrchestrateurClashVestiaire
{
    /// <summary>Évalue si l'archétype se déclenche : portillon de contexte (spec "contexte_min") puis formule pondérée de la Couche 1.</summary>
    public static bool EstDeclenche(ArchetypeClashVestiaire archetype, ContexteJeu contexte, double scoreAffinite, IGenerateurAleatoire generateurRoll)
    {
        ArgumentNullException.ThrowIfNull(archetype);

        if (!archetype.Declencheurs.ConditionsContexteRemplies(contexte, scoreAffinite))
        {
            return false;
        }

        var archetypeEvenement = new ArchetypeEvenement(ArchetypeClashVestiaire.Id, archetype.Declencheurs.ProbabiliteBase);
        double probabilitePonderee = DeclenchementCalculator.CalculerProbabilitePonderee(archetypeEvenement, contexte, scoreAffinite);

        return DeclenchementCalculator.EstDeclenche(probabilitePonderee, generateurRoll);
    }

    /// <summary>Démarre une instance : applique immédiatement les conséquences de la phase incident.</summary>
    public static InstanceClashVestiaire Demarrer(ArchetypeClashVestiaire archetype, Personne joueurA, Personne joueurB, int jourActuel)
    {
        ArgumentNullException.ThrowIfNull(archetype);

        return new InstanceClashVestiaire(joueurA, joueurB, jourActuel, archetype.Incident.ConsequencesImmediates);
    }

    /// <summary>
    /// Fait avancer l'instance d'au plus une phase. <paramref name="choix"/> n'est pris en compte
    /// que durant reaction_presse (seule phase à choix_utilisateur=true) ; une fois enregistré, il
    /// est conservé même si un appel ultérieur le repasse à null. <paramref name="generateurFuite"/>
    /// n'est consommé que si le choix enregistré est "couvrir" (risque de fuite média).
    /// </summary>
    public static void Avancer(
        ArchetypeClashVestiaire archetype,
        InstanceClashVestiaire instance,
        int jourActuel,
        IGenerateurAleatoire generateurFuite,
        ChoixCommunication? choix = null)
    {
        ArgumentNullException.ThrowIfNull(archetype);
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(generateurFuite);

        switch (instance.PhaseActuelle)
        {
            case PhaseInstance.Incident:
                if (jourActuel >= instance.JourEntreeDansPhase + archetype.ReactionPresse.DelaiJours)
                {
                    instance.PasserAReactionPresse(jourActuel);
                }
                break;

            case PhaseInstance.ReactionPresse:
                AvancerReactionPresse(archetype, instance, jourActuel, generateurFuite, choix);
                break;

            case PhaseInstance.ConsequenceMoyenTerme:
                instance.Terminer();
                break;

            case PhaseInstance.Terminee:
                break;
        }
    }

    private static void AvancerReactionPresse(
        ArchetypeClashVestiaire archetype,
        InstanceClashVestiaire instance,
        int jourActuel,
        IGenerateurAleatoire generateurFuite,
        ChoixCommunication? choix)
    {
        if (choix is { } choixFourni)
        {
            instance.EnregistrerChoix(choixFourni);
        }

        if (instance.Choix is not { } choixEnregistre)
        {
            // choix_utilisateur = true : sans choix, la phase ne peut pas se résoudre.
            return;
        }

        if (jourActuel < instance.JourEntreeDansPhase + archetype.ConsequenceMoyenTerme.DelaiJours)
        {
            return;
        }

        var effets = ResoudreEffetsBranche(archetype, instance, choixEnregistre, generateurFuite);
        instance.PasserAConsequence(jourActuel, effets);
    }

    private static EffetsArchetype ResoudreEffetsBranche(
        ArchetypeClashVestiaire archetype, InstanceClashVestiaire instance, ChoixCommunication choix, IGenerateurAleatoire generateurFuite)
    {
        var effetsBase = archetype.ConsequenceMoyenTerme.ObtenirEffets(choix);

        if (choix != ChoixCommunication.Couvrir)
        {
            return effetsBase;
        }

        double probabiliteFuite = RisqueFuiteMediaCalculator.CalculerProbabiliteFuite(instance.JoueurA, instance.JoueurB);
        bool fuite = generateurFuite.TirerRoll() < probabiliteFuite;

        return fuite
            ? effetsBase with { ReputationClub = effetsBase.ReputationClub - 15.0, Cohesion = effetsBase.Cohesion - 5.0 }
            : effetsBase;
    }
}
