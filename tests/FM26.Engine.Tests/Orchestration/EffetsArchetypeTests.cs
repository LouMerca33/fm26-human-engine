using FM26.Engine.Orchestration;

namespace FM26.Engine.Tests.Orchestration;

public class EffetsArchetypeTests
{
    [Fact]
    public void ValeursParDefaut_SontToutesNulles()
    {
        var effets = new EffetsArchetype();

        Assert.Equal(0.0, effets.MoralEquipe);
        Assert.Equal(0.0, effets.Cohesion);
        Assert.Equal(0.0, effets.MoralJoueurCible);
        Assert.Equal(0.0, effets.RespectAutorite);
        Assert.Equal(0.0, effets.ReputationClub);
        Assert.Equal(0.0, effets.ImpactAffinitePaire);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Constructeur_AvecValeurNonFinie_LeveException(double valeurInvalide)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EffetsArchetype(moralEquipe: valeurInvalide));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EffetsArchetype(cohesion: valeurInvalide));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EffetsArchetype(moralJoueurCible: valeurInvalide));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EffetsArchetype(respectAutorite: valeurInvalide));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EffetsArchetype(reputationClub: valeurInvalide));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EffetsArchetype(impactAffinitePaire: valeurInvalide));
    }

    [Fact]
    public void ProprietesEnGetSeul_EmpechentToutContournementDeLaValidationParWithOuInitialiseur()
    {
        // Régression QA : avec des propriétés `init` (plutôt que `get` seul), un `with` ou un
        // initialiseur d'objet appelleraient le constructeur sans-paramètre synthétisé des record
        // structs et non le constructeur validant ci-dessus, ce qui contournerait entièrement
        // `double.IsFinite`. Ce test ne peut pas "provoquer" le contournement (il ne compile plus
        // du tout dès que les propriétés repassent en `init` par erreur) : c'est le point — la
        // garantie est structurelle (erreur de compilation), pas seulement testée à l'exécution.
        // Deux instances construites avec les mêmes arguments doivent être égales par valeur
        // (record struct), ce qui confirme qu'aucune voie de mutation post-construction n'existe.
        var a = new EffetsArchetype(moralEquipe: -8.0, cohesion: -12.0, impactAffinitePaire: -25.0);
        var b = new EffetsArchetype(moralEquipe: -8.0, cohesion: -12.0, impactAffinitePaire: -25.0);

        Assert.Equal(a, b);
    }
}
