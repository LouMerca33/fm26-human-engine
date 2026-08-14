using FM26.Engine.Affinite;
using FM26.Engine.Declenchement;
using FM26.Engine.Orchestration;
using FM26.Engine.Tests.Declenchement;
using FM26.Engine.Traits;

namespace FM26.Engine.Tests.Orchestration;

public class OrchestrateurClashVestiaireTests
{
    private static readonly ArchetypeClashVestiaire Archetype = ArchetypeClashVestiaire.CreerReference();

    private static Personne CreerJoueur(string id, int discretion = 10) =>
        new(id, "MC", new PersonalityTraits(ego: 10, leadership: 10, loyauteGroupe: 10, tolerancePression: 10, discretion: discretion, ambition: 10));

    private static readonly Personne JoueurA = CreerJoueur("joueur-a");
    private static readonly Personne JoueurB = CreerJoueur("joueur-b");

    // --- EstDeclenche : portillon de contexte + formule pondérée de la Couche 1 ---

    [Fact]
    public void EstDeclenche_QuandLePortillonDeContexteEstFerme_RenvoieFauxSansMemeConsulterLeRoll()
    {
        var contexte = new ContexteJeu(serieDefaitesConsecutives: 0, positionClassementNormalisee: 0.5);
        var generateur = new GenerateurAleatoireFixe(0.0); // se déclencherait si le portillon était ouvert

        bool declenche = OrchestrateurClashVestiaire.EstDeclenche(Archetype, contexte, scoreAffinite: -90.0, generateur);

        Assert.False(declenche);
    }

    [Fact]
    public void EstDeclenche_QuandLePortillonEstOuvertEtLeRollBat_LaProbabilite_RenvoieVrai()
    {
        var contexte = new ContexteJeu(serieDefaitesConsecutives: 5, positionClassementNormalisee: 0.9);
        var generateur = new GenerateurAleatoireFixe(0.0);

        bool declenche = OrchestrateurClashVestiaire.EstDeclenche(Archetype, contexte, scoreAffinite: -90.0, generateur);

        Assert.True(declenche);
    }

    [Fact]
    public void EstDeclenche_QuandLePortillonEstOuvertMaisLeRollNeBatPasLaProbabilite_RenvoieFaux()
    {
        var contexte = new ContexteJeu(serieDefaitesConsecutives: 2, positionClassementNormalisee: 0.0);
        var generateur = new GenerateurAleatoireFixe(0.999);

        bool declenche = OrchestrateurClashVestiaire.EstDeclenche(Archetype, contexte, scoreAffinite: -30.0, generateur);

        Assert.False(declenche);
    }

    // --- Démarrage ---

    [Fact]
    public void Demarrer_AppliqueImmediatementLesConsequencesDeLIncident()
    {
        var instance = OrchestrateurClashVestiaire.Demarrer(Archetype, JoueurA, JoueurB, jourActuel: 100);

        Assert.Equal(PhaseInstance.Incident, instance.PhaseActuelle);
        Assert.Equal(100, instance.JourEntreeDansPhase);
        Assert.Single(instance.EffetsAppliques);
        Assert.Equal(-8.0, instance.EffetsAppliques[0].MoralEquipe);
        Assert.Equal(-12.0, instance.EffetsAppliques[0].Cohesion);
    }

    [Fact]
    public void Demarrer_AvecLesMemesJoueurs_LeveException()
    {
        Assert.Throws<ArgumentException>(() => OrchestrateurClashVestiaire.Demarrer(Archetype, JoueurA, JoueurA, jourActuel: 0));
    }

    // --- Incident -> reaction_presse ---

    [Fact]
    public void Avancer_DepuisIncident_AvantLeDelaiDUnJour_RestesEnIncident()
    {
        var instance = OrchestrateurClashVestiaire.Demarrer(Archetype, JoueurA, JoueurB, jourActuel: 0);

        OrchestrateurClashVestiaire.Avancer(Archetype, instance, jourActuel: 0, new GenerateurAleatoireFixe(0.5));

        Assert.Equal(PhaseInstance.Incident, instance.PhaseActuelle);
    }

    [Fact]
    public void Avancer_DepuisIncident_ApresLeDelaiDUnJour_PasseAReactionPresse()
    {
        var instance = OrchestrateurClashVestiaire.Demarrer(Archetype, JoueurA, JoueurB, jourActuel: 0);

        OrchestrateurClashVestiaire.Avancer(Archetype, instance, jourActuel: 1, new GenerateurAleatoireFixe(0.5));

        Assert.Equal(PhaseInstance.ReactionPresse, instance.PhaseActuelle);
        Assert.Equal(1, instance.JourEntreeDansPhase);
    }

    // --- reaction_presse : attend un choix ---

    [Fact]
    public void Avancer_DepuisReactionPresse_SansChoixMemeApresLeDelai_RestesEnReactionPresse()
    {
        var instance = DemarrerEtPasserAReactionPresse();

        OrchestrateurClashVestiaire.Avancer(Archetype, instance, jourActuel: 100, new GenerateurAleatoireFixe(0.5), choix: null);

        Assert.Equal(PhaseInstance.ReactionPresse, instance.PhaseActuelle);
        Assert.Null(instance.Choix);
    }

    [Fact]
    public void Avancer_DepuisReactionPresse_AvecChoixMaisAvantLeDelaiDe14Jours_MemoriseLeChoixMaisResteEnReactionPresse()
    {
        var instance = DemarrerEtPasserAReactionPresse(); // entre en reaction_presse au jour 1

        OrchestrateurClashVestiaire.Avancer(Archetype, instance, jourActuel: 5, new GenerateurAleatoireFixe(0.5), choix: ChoixCommunication.Apaiser);

        Assert.Equal(PhaseInstance.ReactionPresse, instance.PhaseActuelle);
        Assert.Equal(ChoixCommunication.Apaiser, instance.Choix);
    }

    // --- Les 3 branches de consequence_moyen_terme ---

    [Fact]
    public void Avancer_BrancheApaiser_ProduitLesEffetsDApaisement()
    {
        var instance = DemarrerEtPasserAReactionPresse();

        OrchestrateurClashVestiaire.Avancer(Archetype, instance, jourActuel: 15, new GenerateurAleatoireFixe(0.5), choix: ChoixCommunication.Apaiser);

        Assert.Equal(PhaseInstance.ConsequenceMoyenTerme, instance.PhaseActuelle);
        var effets = instance.EffetsAppliques[^1];
        Assert.Equal(3.0, effets.MoralEquipe);
        Assert.Equal(5.0, effets.Cohesion);
        Assert.Equal(0.0, effets.MoralJoueurCible);
        Assert.Equal(0.0, effets.RespectAutorite);
        Assert.Equal(0.0, effets.ReputationClub);
    }

    [Fact]
    public void Avancer_BrancheSanctionner_DonneMoralJoueurCibleMoins20EtRespectAutoritePlus10()
    {
        var instance = DemarrerEtPasserAReactionPresse();

        OrchestrateurClashVestiaire.Avancer(Archetype, instance, jourActuel: 15, new GenerateurAleatoireFixe(0.5), choix: ChoixCommunication.Sanctionner);

        var effets = instance.EffetsAppliques[^1];
        Assert.Equal(-20.0, effets.MoralJoueurCible);
        Assert.Equal(10.0, effets.RespectAutorite);
        Assert.Equal(0.0, effets.MoralEquipe);
        Assert.Equal(0.0, effets.Cohesion);
        Assert.Equal(0.0, effets.ReputationClub);
    }

    [Fact]
    public void Avancer_BrancheCouvrir_SansFuiteMedia_DonneUniquementLEffetDeBase()
    {
        // discretion moyenne des 2 joueurs = 10/20 -> probabilité de fuite = 0.5 ; un roll de
        // 0.6 (>= 0.5) ne déclenche pas la fuite.
        var instance = DemarrerEtPasserAReactionPresse();

        OrchestrateurClashVestiaire.Avancer(Archetype, instance, jourActuel: 15, new GenerateurAleatoireFixe(0.6), choix: ChoixCommunication.Couvrir);

        var effets = instance.EffetsAppliques[^1];
        Assert.Equal(-2.0, effets.Cohesion);
        Assert.Equal(0.0, effets.ReputationClub);
    }

    [Fact]
    public void Avancer_BrancheCouvrir_AvecFuiteMedia_AjouteLaPenaliteDeReputationEtDeCohesion()
    {
        // discretion moyenne des 2 joueurs = 10/20 -> probabilité de fuite = 0.5 ; un roll de
        // 0.1 (< 0.5) déclenche la fuite.
        var instance = DemarrerEtPasserAReactionPresse();

        OrchestrateurClashVestiaire.Avancer(Archetype, instance, jourActuel: 15, new GenerateurAleatoireFixe(0.1), choix: ChoixCommunication.Couvrir);

        var effets = instance.EffetsAppliques[^1];
        Assert.Equal(-2.0 - 5.0, effets.Cohesion);
        Assert.Equal(-15.0, effets.ReputationClub);
    }

    [Fact]
    public void Avancer_BrancheCouvrir_AvecTemoinsPeuDiscrets_LeRisqueDeFuiteEstPlusEleve()
    {
        // Témoins peu discrets (discretion=1) -> probabilité de fuite = 1 - 1/20 = 0.95 : un
        // roll de 0.6, qui n'aurait pas déclenché de fuite avec des témoins moyennement
        // discrets (probabilité 0.5), en déclenche une ici.
        var joueurIndiscretA = CreerJoueur("indiscret-a", discretion: 1);
        var joueurIndiscretB = CreerJoueur("indiscret-b", discretion: 1);
        var instance = OrchestrateurClashVestiaire.Demarrer(Archetype, joueurIndiscretA, joueurIndiscretB, jourActuel: 0);
        OrchestrateurClashVestiaire.Avancer(Archetype, instance, jourActuel: 1, new GenerateurAleatoireFixe(0.5));

        OrchestrateurClashVestiaire.Avancer(Archetype, instance, jourActuel: 15, new GenerateurAleatoireFixe(0.6), choix: ChoixCommunication.Couvrir);

        var effets = instance.EffetsAppliques[^1];
        Assert.Equal(-15.0, effets.ReputationClub);
    }

    [Fact]
    public void Avancer_BrancheCouvrir_AvantLeDelaiDe14Jours_NeResoutPasEncoreLaBranche()
    {
        var instance = DemarrerEtPasserAReactionPresse();

        OrchestrateurClashVestiaire.Avancer(Archetype, instance, jourActuel: 10, new GenerateurAleatoireFixe(0.1), choix: ChoixCommunication.Couvrir);

        Assert.Equal(PhaseInstance.ReactionPresse, instance.PhaseActuelle);
        Assert.Single(instance.EffetsAppliques); // seul l'effet de l'incident a été appliqué
    }

    // --- Fin de vie de l'instance ---

    [Fact]
    public void Avancer_DepuisConsequenceMoyenTerme_PasseATerminee()
    {
        var instance = DemarrerEtPasserAReactionPresse();
        OrchestrateurClashVestiaire.Avancer(Archetype, instance, jourActuel: 15, new GenerateurAleatoireFixe(0.5), choix: ChoixCommunication.Apaiser);

        OrchestrateurClashVestiaire.Avancer(Archetype, instance, jourActuel: 16, new GenerateurAleatoireFixe(0.5));

        Assert.Equal(PhaseInstance.Terminee, instance.PhaseActuelle);
    }

    [Fact]
    public void Avancer_DepuisTerminee_RestesTerminee()
    {
        var instance = DemarrerEtPasserAReactionPresse();
        OrchestrateurClashVestiaire.Avancer(Archetype, instance, jourActuel: 15, new GenerateurAleatoireFixe(0.5), choix: ChoixCommunication.Apaiser);
        OrchestrateurClashVestiaire.Avancer(Archetype, instance, jourActuel: 16, new GenerateurAleatoireFixe(0.5));

        OrchestrateurClashVestiaire.Avancer(Archetype, instance, jourActuel: 17, new GenerateurAleatoireFixe(0.5));

        Assert.Equal(PhaseInstance.Terminee, instance.PhaseActuelle);
    }

    [Fact]
    public void Avancer_NeSauteJamaisPlusDUnePhaseParAppel_MemeSiLeJourDepasseTousLesSeuils()
    {
        var instance = OrchestrateurClashVestiaire.Demarrer(Archetype, JoueurA, JoueurB, jourActuel: 0);

        OrchestrateurClashVestiaire.Avancer(Archetype, instance, jourActuel: 1000, new GenerateurAleatoireFixe(0.5), choix: ChoixCommunication.Apaiser);

        // Un seul appel, même avec un jour très avancé : seule la transition incident -> reaction_presse a eu lieu.
        Assert.Equal(PhaseInstance.ReactionPresse, instance.PhaseActuelle);
    }

    private static InstanceClashVestiaire DemarrerEtPasserAReactionPresse()
    {
        var instance = OrchestrateurClashVestiaire.Demarrer(Archetype, JoueurA, JoueurB, jourActuel: 0);
        OrchestrateurClashVestiaire.Avancer(Archetype, instance, jourActuel: 1, new GenerateurAleatoireFixe(0.5));
        return instance;
    }
}
