using FM26.Engine.Orchestration;

namespace FM26.Engine.Tests.Orchestration;

public class ArchetypeClashVestiaireTests
{
    [Fact]
    public void CreerReference_CorrespondAuxValeursDeLExempleJsonDeLaSpec()
    {
        var archetype = ArchetypeClashVestiaire.CreerReference();

        Assert.Equal("clash_vestiaire", ArchetypeClashVestiaire.Id);

        Assert.Equal(2, archetype.Declencheurs.SerieDefaitesMinimale);
        Assert.Equal(-30.0, archetype.Declencheurs.AffiniteMaximale);
        Assert.Equal(0.04, archetype.Declencheurs.ProbabiliteBase);

        Assert.Equal("incident", archetype.Incident.Nom);
        Assert.Equal(0, archetype.Incident.DelaiJours);
        Assert.False(archetype.Incident.ChoixUtilisateur);
        Assert.Equal(-8.0, archetype.Incident.ConsequencesImmediates.MoralEquipe);
        Assert.Equal(-12.0, archetype.Incident.ConsequencesImmediates.Cohesion);

        Assert.Equal("reaction_presse", archetype.ReactionPresse.Nom);
        Assert.Equal(1, archetype.ReactionPresse.DelaiJours);
        Assert.True(archetype.ReactionPresse.ChoixUtilisateur);

        Assert.Equal("consequence_moyen_terme", archetype.ConsequenceMoyenTerme.Nom);
        Assert.Equal(14, archetype.ConsequenceMoyenTerme.DelaiJours);
        Assert.False(archetype.ConsequenceMoyenTerme.ChoixUtilisateur);

        var effetsSanctionner = archetype.ConsequenceMoyenTerme.ObtenirEffets(ChoixCommunication.Sanctionner);
        Assert.Equal(-20.0, effetsSanctionner.MoralJoueurCible);
        Assert.Equal(10.0, effetsSanctionner.RespectAutorite);
    }

    [Fact]
    public void Phases_RenvoieLaSequenceOrdonneeIncidentPuisReactionPresseePuisConsequence()
    {
        var archetype = ArchetypeClashVestiaire.CreerReference();

        Assert.Equal(
            new[] { "incident", "reaction_presse", "consequence_moyen_terme" },
            archetype.Phases.Select(p => p.Nom));
    }

    [Fact]
    public void EcritureSave_CorrespondAuxChampsDeLExempleJsonDeLaSpec()
    {
        var archetype = ArchetypeClashVestiaire.CreerReference();

        Assert.Equal(
            new[] { "morale", "reputation_club", "relation_joueur_staff" },
            archetype.EcritureSave.ChampsModifies);
    }

    [Fact]
    public void PhaseConsequenceMoyenTerme_SansEffetsPourTousLesChoix_LeveException()
    {
        var effetsIncomplets = new Dictionary<ChoixCommunication, EffetsArchetype>
        {
            [ChoixCommunication.Apaiser] = new EffetsArchetype(),
            [ChoixCommunication.Sanctionner] = new EffetsArchetype(),
            // Couvrir manquant.
        };

        Assert.Throws<ArgumentException>(() => new PhaseConsequenceMoyenTerme(delaiJours: 14, effetsIncomplets));
    }

    [Theory]
    [InlineData(-1)]
    public void PhaseArchetype_AvecDelaiNegatif_LeveException(int delaiInvalide)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PhaseReactionPresse(delaiInvalide));
    }

    [Fact]
    public void EcritureSaveArchetype_AvecListeVide_LeveException()
    {
        Assert.Throws<ArgumentException>(() => new EcritureSaveArchetype(Array.Empty<string>()));
    }
}
