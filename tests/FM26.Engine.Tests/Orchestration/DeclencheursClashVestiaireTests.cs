using FM26.Engine.Declenchement;
using FM26.Engine.Orchestration;

namespace FM26.Engine.Tests.Orchestration;

public class DeclencheursClashVestiaireTests
{
    private static readonly DeclencheursClashVestiaire Declencheurs =
        new(serieDefaitesMinimale: 2, affiniteMaximale: -30.0, probabiliteBase: 0.04);

    [Fact]
    public void ConditionsContexteRemplies_QuandLesDeuxConditionsSontRemplies_RenvoieVrai()
    {
        var contexte = new ContexteJeu(serieDefaitesConsecutives: 2, positionClassementNormalisee: 0.5);

        Assert.True(Declencheurs.ConditionsContexteRemplies(contexte, scoreAffinite: -30.0));
    }

    [Fact]
    public void ConditionsContexteRemplies_QuandLaSerieDeDefaitesEstInsuffisante_RenvoieFaux()
    {
        var contexte = new ContexteJeu(serieDefaitesConsecutives: 1, positionClassementNormalisee: 0.5);

        Assert.False(Declencheurs.ConditionsContexteRemplies(contexte, scoreAffinite: -50.0));
    }

    [Fact]
    public void ConditionsContexteRemplies_QuandLAffiniteEstTropHaute_RenvoieFaux()
    {
        var contexte = new ContexteJeu(serieDefaitesConsecutives: 5, positionClassementNormalisee: 0.5);

        Assert.False(Declencheurs.ConditionsContexteRemplies(contexte, scoreAffinite: -10.0));
    }

    [Fact]
    public void ConditionsContexteRemplies_AvecScoreAffiniteNonFini_LeveException()
    {
        var contexte = new ContexteJeu(serieDefaitesConsecutives: 5, positionClassementNormalisee: 0.5);

        Assert.Throws<ArgumentOutOfRangeException>(() => Declencheurs.ConditionsContexteRemplies(contexte, double.NaN));
    }

    [Fact]
    public void Constructeur_AvecSerieDefaitesNegative_LeveException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DeclencheursClashVestiaire(serieDefaitesMinimale: -1, affiniteMaximale: -30.0, probabiliteBase: 0.04));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Constructeur_AvecProbabiliteBaseHorsIntervalle_LeveException(double probabiliteInvalide)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DeclencheursClashVestiaire(serieDefaitesMinimale: 2, affiniteMaximale: -30.0, probabiliteBase: probabiliteInvalide));
    }

    [Theory]
    [InlineData(-101.0)]
    [InlineData(101.0)]
    public void Constructeur_AvecAffiniteMaximaleHorsIntervalle_LeveException(double affiniteInvalide)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DeclencheursClashVestiaire(serieDefaitesMinimale: 2, affiniteMaximale: affiniteInvalide, probabiliteBase: 0.04));
    }
}
