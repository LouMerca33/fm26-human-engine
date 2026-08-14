using FM26.Engine.Affinite;
using FM26.Engine.Declenchement;
using FM26.Engine.Narratif;
using FM26.Engine.Orchestration;
using FM26.Engine.Tests.Declenchement;
using FM26.Engine.Traits;

namespace FM26.Engine.Tests;

/// <summary>
/// Vérifie que la Couche 2 (orchestration clash_vestiaire + narration) s'enchaîne correctement
/// à partir de la Couche 1 (traits -> affinité -> déclenchement), pas seulement en isolation.
/// </summary>
public class OrchestrationPipelineIntegrationTests
{
    [Fact]
    public void Pipeline_TraitsAffiniteDeclenchementOrchestrationEtNarration_SEnchainentDeBoutEnBout()
    {
        var generateurTraits = new DeterministicPersonalityTraitGenerator();

        // Deux ego élevés et proches, même poste : le cas de tension le plus favorable au
        // déclenchement d'un clash_vestiaire (cf. PipelineIntegrationTests de la Couche 1).
        var traitsRivalA = new PersonalityTraits(ego: 19, leadership: 18, loyauteGroupe: 5, tolerancePression: 8, discretion: 5, ambition: 19);
        var traitsRivalB = new PersonalityTraits(ego: 18, leadership: 17, loyauteGroupe: 5, tolerancePression: 8, discretion: 5, ambition: 19);
        var joueurA = new Personne("rival-a", "AT", traitsRivalA);
        var joueurB = new Personne("rival-b", "AT", traitsRivalB);

        double scoreAffinite = AffiniteCalculator.CalculerScore(joueurA, joueurB);

        var archetype = ArchetypeClashVestiaire.CreerReference();
        var contexte = new ContexteJeu(serieDefaitesConsecutives: 4, positionClassementNormalisee: 0.8);
        var generateurRoll = new GenerateurAleatoireFixe(0.0); // garantit le déclenchement pour ce test

        bool declenche = OrchestrateurClashVestiaire.EstDeclenche(archetype, contexte, scoreAffinite, generateurRoll);
        Assert.True(declenche, $"Attendu un déclenchement avec une tension forte (score {scoreAffinite}) et un contexte défavorable.");

        var instance = OrchestrateurClashVestiaire.Demarrer(archetype, joueurA, joueurB, jourActuel: 0);
        Assert.Equal(PhaseInstance.Incident, instance.PhaseActuelle);

        var generateurNarratif = new GenerateurNarratifTemplate();
        var contexteScene = new SceneIncidentContexte("Rival A", "Rival B", "Club Test FC");
        string scene = generateurNarratif.GenererSceneIncident(contexteScene);
        var options = generateurNarratif.GenererOptionsCommunication(contexteScene);
        Assert.False(string.IsNullOrWhiteSpace(scene));
        Assert.Equal(3, options.Count);

        OrchestrateurClashVestiaire.Avancer(archetype, instance, jourActuel: 1, generateurRoll);
        Assert.Equal(PhaseInstance.ReactionPresse, instance.PhaseActuelle);

        OrchestrateurClashVestiaire.Avancer(archetype, instance, jourActuel: 15, generateurRoll, choix: ChoixCommunication.Sanctionner);
        Assert.Equal(PhaseInstance.ConsequenceMoyenTerme, instance.PhaseActuelle);
        Assert.Equal(-20.0, instance.EffetsAppliques[^1].MoralJoueurCible);

        OrchestrateurClashVestiaire.Avancer(archetype, instance, jourActuel: 16, generateurRoll);
        Assert.Equal(PhaseInstance.Terminee, instance.PhaseActuelle);
    }
}
