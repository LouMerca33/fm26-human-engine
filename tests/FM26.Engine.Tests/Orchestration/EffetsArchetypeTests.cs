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
    }

    [Fact]
    public void With_PermetDeDeriverUneVarianteSansModifierLOriginal()
    {
        var original = new EffetsArchetype(cohesion: -2.0);

        var derive = original with { ReputationClub = -15.0, Cohesion = original.Cohesion - 5.0 };

        Assert.Equal(-2.0, original.Cohesion);
        Assert.Equal(0.0, original.ReputationClub);
        Assert.Equal(-7.0, derive.Cohesion);
        Assert.Equal(-15.0, derive.ReputationClub);
    }
}
