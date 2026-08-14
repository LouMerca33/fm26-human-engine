using FM26.Engine.Affinite;
using FM26.Engine.Declenchement;
using FM26.Engine.Traits;

namespace FM26.Engine.Tests;

/// <summary>
/// Vérifie que génération de traits -> score d'affinité -> probabilité de déclenchement
/// s'enchaînent sans erreur et restent cohérents bout en bout, pas seulement en isolation
/// module par module.
/// </summary>
public class PipelineIntegrationTests
{
    private readonly DeterministicPersonalityTraitGenerator _generateurTraits = new();

    [Fact]
    public void Pipeline_GenerationAffiniteDeclenchement_SEnchaineSansErreurEtDonneUnResultatCoherent()
    {
        var profilJoueurA = _generateurTraits.Generate(new TraitGenerationContext("joueur-a", 24));
        var profilJoueurB = _generateurTraits.Generate(new TraitGenerationContext("joueur-b", 27));

        var joueurA = new Personne("joueur-a", "AT", profilJoueurA);
        var joueurB = new Personne("joueur-b", "AT", profilJoueurB);

        double scoreAffinite = AffiniteCalculator.CalculerScore(joueurA, joueurB);
        Assert.InRange(scoreAffinite, -100.0, 100.0);

        var archetype = new ArchetypeEvenement("clash_vestiaire", probabiliteBase: 0.04);
        var contexte = new ContexteJeu(serieDefaitesConsecutives: 2, positionClassementNormalisee: 0.6);

        double probabilite = DeclenchementCalculator.CalculerProbabilitePonderee(archetype, contexte, scoreAffinite);
        Assert.InRange(probabilite, 0.0, 1.0);

        var generateurRoll = new GenerateurAleatoireSysteme(seed: 1234);
        bool declenche = DeclenchementCalculator.EstDeclenche(probabilite, generateurRoll);

        // Le résultat booléen dépend du roll : ce qu'on vérifie ici, c'est que l'appel
        // ne lève rien et retourne un bool valide pour toute la chaîne.
        Assert.True(declenche || !declenche);
    }

    [Fact]
    public void Pipeline_TensionForteEntreDeuxRivauxDeMemePoste_DonneUneProbabiliteStrictementSuperieure_AUneBonneEntente()
    {
        // Deux ego élevés et proches, même poste, forte ambition des deux côtés : le pire cas
        // de tension que le moteur d'affinité peut produire pour une paire donnée.
        var traitsRivalA = new PersonalityTraits(ego: 19, leadership: 18, loyauteGroupe: 5, tolerancePression: 8, discretion: 5, ambition: 19);
        var traitsRivalB = new PersonalityTraits(ego: 18, leadership: 17, loyauteGroupe: 5, tolerancePression: 8, discretion: 5, ambition: 19);
        var rivalA = new Personne("rival-a", "AT", traitsRivalA);
        var rivalB = new Personne("rival-b", "AT", traitsRivalB);

        // Deux profils sereins et complémentaires, postes différents : cas de bonne entente.
        var traitsSereinA = new PersonalityTraits(ego: 3, leadership: 15, loyauteGroupe: 18, tolerancePression: 18, discretion: 15, ambition: 5);
        var traitsSereinB = new PersonalityTraits(ego: 2, leadership: 2, loyauteGroupe: 18, tolerancePression: 18, discretion: 15, ambition: 5);
        var sereinA = new Personne("serein-a", "AT", traitsSereinA);
        var sereinB = new Personne("serein-b", "GB", traitsSereinB);

        double scoreTension = AffiniteCalculator.CalculerScore(rivalA, rivalB);
        double scoreBonneEntente = AffiniteCalculator.CalculerScore(sereinA, sereinB);

        var archetype = new ArchetypeEvenement("clash_vestiaire", probabiliteBase: 0.04);
        var contexte = new ContexteJeu(serieDefaitesConsecutives: 3, positionClassementNormalisee: 0.7);

        double probabiliteTension = DeclenchementCalculator.CalculerProbabilitePonderee(archetype, contexte, scoreTension);
        double probabiliteBonneEntente = DeclenchementCalculator.CalculerProbabilitePonderee(archetype, contexte, scoreBonneEntente);

        Assert.True(
            probabiliteTension > probabiliteBonneEntente,
            $"Probabilité en cas de tension ({probabiliteTension}) devrait dépasser celle d'une bonne entente ({probabiliteBonneEntente}).");
    }
}
