using FM26.Engine.Affinite;
using FM26.Engine.Traits;

namespace FM26.Engine.Tests.Affinite;

public class AffiniteCalculatorTests
{
    private static Personne CreerPersonne(string id, string poste, int ego = 10, int leadership = 10, int loyaute = 10, int tolerance = 10, int discretion = 10, int ambition = 10) =>
        new(id, poste, new PersonalityTraits(ego, leadership, loyaute, tolerance, discretion, ambition));

    [Fact]
    public void CalculerScore_DeuxEgoElevesEtProches_EstPlusTenduQueDeuxEgoElevesEtEcartes()
    {
        var a = CreerPersonne("a", "MC", ego: 19);
        var bProche = CreerPersonne("b-proche", "AT", ego: 18);
        var bEcarte = CreerPersonne("b-ecarte", "AT", ego: 2);

        double scoreProche = AffiniteCalculator.CalculerScore(a, bProche);
        double scoreEcarte = AffiniteCalculator.CalculerScore(a, bEcarte);

        Assert.True(
            scoreProche < scoreEcarte,
            $"Deux ego élevés et proches ({scoreProche}) devraient être plus tendus qu'un grand écart d'ego ({scoreEcarte}).");
    }

    [Fact]
    public void CalculerScore_MemePoste_EstPlusTenduQuePosteDifferent_ToutesChosesEgalesParAilleurs()
    {
        var a = CreerPersonne("a", "AT", ambition: 15);
        var bMemePoste = CreerPersonne("b-meme-poste", "AT", ambition: 15);
        var bAutrePoste = CreerPersonne("b-autre-poste", "GB", ambition: 15);

        double scoreMemePoste = AffiniteCalculator.CalculerScore(a, bMemePoste);
        double scoreAutrePoste = AffiniteCalculator.CalculerScore(a, bAutrePoste);

        Assert.True(
            scoreMemePoste < scoreAutrePoste,
            $"Même poste ({scoreMemePoste}) devrait générer plus de tension que des postes différents ({scoreAutrePoste}).");
    }

    [Fact]
    public void CalculerScore_ConcurrencePoste_EstPlusForteQuandAmbitionEstElevee()
    {
        var aFaibleAmbition = CreerPersonne("a-faible", "AT", ambition: 2);
        var bFaibleAmbition = CreerPersonne("b-faible", "AT", ambition: 2);
        var aForteAmbition = CreerPersonne("a-forte", "AT", ambition: 19);
        var bForteAmbition = CreerPersonne("b-forte", "AT", ambition: 19);

        double scoreFaibleAmbition = AffiniteCalculator.CalculerScore(aFaibleAmbition, bFaibleAmbition);
        double scoreForteAmbition = AffiniteCalculator.CalculerScore(aForteAmbition, bForteAmbition);

        Assert.True(
            scoreForteAmbition < scoreFaibleAmbition,
            $"Ambition élevée en concurrence directe ({scoreForteAmbition}) devrait accentuer la tension par rapport à une ambition faible ({scoreFaibleAmbition}).");
    }

    [Fact]
    public void CalculerScore_LeadershipComplementaire_EstMeilleurQueDeuxLeadersEnLutte()
    {
        var leaderFort = CreerPersonne("leader-fort", "MC", leadership: 19);
        var suiveur = CreerPersonne("suiveur", "DC", leadership: 2);
        var autreLeaderFort = CreerPersonne("autre-leader-fort", "DC", leadership: 18);

        double scoreComplementaire = AffiniteCalculator.CalculerScore(leaderFort, suiveur);
        double scoreLuttePouvoir = AffiniteCalculator.CalculerScore(leaderFort, autreLeaderFort);

        Assert.True(
            scoreComplementaire > scoreLuttePouvoir,
            $"Un duo complémentaire ({scoreComplementaire}) devrait avoir une meilleure affinité que deux leaders en lutte ({scoreLuttePouvoir}).");
    }

    [Fact]
    public void CalculerScore_EvenementRecentNegatif_PeseLourdQuUnEvenementAncienDeMemeMagnitude()
    {
        var a = CreerPersonne("a", "MC");
        var b = CreerPersonne("b", "DC");

        var historiqueRecent = new[] { new EvenementHistorique(impactAffinite: -40, joursDepuis: 5) };
        var historiqueAncien = new[] { new EvenementHistorique(impactAffinite: -40, joursDepuis: 720) };

        double scoreRecent = AffiniteCalculator.CalculerScore(a, b, historiqueRecent);
        double scoreAncien = AffiniteCalculator.CalculerScore(a, b, historiqueAncien);

        Assert.True(
            scoreRecent < scoreAncien,
            $"Un événement négatif récent ({scoreRecent}) devrait peser plus lourd qu'un ancien ({scoreAncien}).");
    }

    [Fact]
    public void CalculerScore_SansHistorique_NeLeveAucuneExceptionEtIgnoreLaComposante()
    {
        var a = CreerPersonne("a", "MC");
        var b = CreerPersonne("b", "DC");

        double scoreSansArgument = AffiniteCalculator.CalculerScore(a, b);
        double scoreListeVide = AffiniteCalculator.CalculerScore(a, b, Array.Empty<EvenementHistorique>());

        Assert.Equal(scoreSansArgument, scoreListeVide);
    }

    [Theory]
    [InlineData(20, 20, 20, 20, 20, 20)]
    [InlineData(1, 1, 1, 1, 1, 1)]
    public void CalculerScore_ResteToujoursDansLIntervalleMoins100Plus100(int ego, int leadership, int loyaute, int tolerance, int discretion, int ambition)
    {
        var a = CreerPersonne("a", "AT", ego, leadership, loyaute, tolerance, discretion, ambition);
        var b = CreerPersonne("b", "AT", ego, leadership, loyaute, tolerance, discretion, ambition);

        var historiqueExtreme = new[]
        {
            new EvenementHistorique(impactAffinite: -1000, joursDepuis: 0),
            new EvenementHistorique(impactAffinite: -1000, joursDepuis: 0),
        };

        double score = AffiniteCalculator.CalculerScore(a, b, historiqueExtreme);

        Assert.InRange(score, -100.0, 100.0);
    }

    [Fact]
    public void EvenementHistorique_AvecJoursDepuisNegatif_LeveException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EvenementHistorique(impactAffinite: -10, joursDepuis: -1));
    }

    [Fact]
    public void Personne_AvecIdVide_LeveException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Personne("", "MC", new PersonalityTraits(10, 10, 10, 10, 10, 10)));
    }
}
