using FM26.Engine.Traits;

namespace FM26.Engine.Tests.Traits;

public class PersonalityTraitsTests
{
    [Fact]
    public void Construction_AvecValeursValides_ReussitEtConserveLesChamps()
    {
        var traits = new PersonalityTraits(
            ego: 12,
            leadership: 15,
            loyauteGroupe: 8,
            tolerancePression: 20,
            discretion: 1,
            ambition: 10);

        Assert.Equal(12, traits.Ego);
        Assert.Equal(15, traits.Leadership);
        Assert.Equal(8, traits.LoyauteGroupe);
        Assert.Equal(20, traits.TolerancePression);
        Assert.Equal(1, traits.Discretion);
        Assert.Equal(10, traits.Ambition);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    [InlineData(-5)]
    [InlineData(100)]
    public void Construction_AvecEgoHorsPlage_LeveException(int egoInvalide)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PersonalityTraits(egoInvalide, 10, 10, 10, 10, 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void Construction_AvecLeadershipHorsPlage_LeveException(int leadershipInvalide)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PersonalityTraits(10, leadershipInvalide, 10, 10, 10, 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void Construction_AvecLoyauteGroupeHorsPlage_LeveException(int loyauteInvalide)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PersonalityTraits(10, 10, loyauteInvalide, 10, 10, 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void Construction_AvecTolerancePressionHorsPlage_LeveException(int toleranceInvalide)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PersonalityTraits(10, 10, 10, toleranceInvalide, 10, 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void Construction_AvecDiscretionHorsPlage_LeveException(int discretionInvalide)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PersonalityTraits(10, 10, 10, 10, discretionInvalide, 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void Construction_AvecAmbitionHorsPlage_LeveException(int ambitionInvalide)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PersonalityTraits(10, 10, 10, 10, 10, ambitionInvalide));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    public void Construction_AuxBornesDeLEchelle_Reussit(int valeurLimite)
    {
        var traits = new PersonalityTraits(valeurLimite, valeurLimite, valeurLimite, valeurLimite, valeurLimite, valeurLimite);

        Assert.Equal(valeurLimite, traits.Ego);
    }
}
