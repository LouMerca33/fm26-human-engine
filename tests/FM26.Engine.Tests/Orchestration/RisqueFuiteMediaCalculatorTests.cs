using FM26.Engine.Affinite;
using FM26.Engine.Orchestration;
using FM26.Engine.Traits;

namespace FM26.Engine.Tests.Orchestration;

public class RisqueFuiteMediaCalculatorTests
{
    private static Personne CreerPersonne(string id, int discretion) =>
        new(id, "MC", new PersonalityTraits(ego: 10, leadership: 10, loyauteGroupe: 10, tolerancePression: 10, discretion: discretion, ambition: 10));

    [Fact]
    public void CalculerProbabiliteFuite_AvecTemoinsTresDiscrets_DonneUnRisqueFaible()
    {
        var temoinA = CreerPersonne("a", discretion: 20);
        var temoinB = CreerPersonne("b", discretion: 20);

        double probabilite = RisqueFuiteMediaCalculator.CalculerProbabiliteFuite(temoinA, temoinB);

        Assert.Equal(0.0, probabilite, precision: 10);
    }

    [Fact]
    public void CalculerProbabiliteFuite_AvecTemoinsPeuDiscrets_DonneUnRisqueEleve()
    {
        var temoinA = CreerPersonne("a", discretion: 1);
        var temoinB = CreerPersonne("b", discretion: 1);

        double probabilite = RisqueFuiteMediaCalculator.CalculerProbabiliteFuite(temoinA, temoinB);

        Assert.Equal(0.95, probabilite, precision: 10);
    }

    [Fact]
    public void CalculerProbabiliteFuite_AugmenteQuandLaDiscretionMoyenneDiminue()
    {
        var temoinsDiscrets = (CreerPersonne("a", discretion: 18), CreerPersonne("b", discretion: 18));
        var temoinsIndiscrets = (CreerPersonne("c", discretion: 3), CreerPersonne("d", discretion: 3));

        double probaDiscrets = RisqueFuiteMediaCalculator.CalculerProbabiliteFuite(temoinsDiscrets.Item1, temoinsDiscrets.Item2);
        double probaIndiscrets = RisqueFuiteMediaCalculator.CalculerProbabiliteFuite(temoinsIndiscrets.Item1, temoinsIndiscrets.Item2);

        Assert.True(probaIndiscrets > probaDiscrets);
        Assert.InRange(probaDiscrets, 0.0, 1.0);
        Assert.InRange(probaIndiscrets, 0.0, 1.0);
    }
}
