using FM26.Engine.Declenchement;

namespace FM26.Engine.Tests.Declenchement;

public class DeclenchementCalculatorTests
{
    [Fact]
    public void CalculerProbabilitePonderee_AugmenteAvecLaSerieDeDefaites()
    {
        var archetype = new ArchetypeEvenement("clash_vestiaire", probabiliteBase: 0.04);
        var sansSerie = new ContexteJeu(serieDefaitesConsecutives: 0, positionClassementNormalisee: 0.5);
        var avecSerie = new ContexteJeu(serieDefaitesConsecutives: 5, positionClassementNormalisee: 0.5);

        double probaSansSerie = DeclenchementCalculator.CalculerProbabilitePonderee(archetype, sansSerie, scoreAffinite: 0);
        double probaAvecSerie = DeclenchementCalculator.CalculerProbabilitePonderee(archetype, avecSerie, scoreAffinite: 0);

        Assert.True(probaAvecSerie > probaSansSerie);
    }

    [Fact]
    public void CalculerProbabilitePonderee_AugmenteQuandLeClubEstMalClasse()
    {
        var archetype = new ArchetypeEvenement("clash_vestiaire", probabiliteBase: 0.04);
        var leader = new ContexteJeu(serieDefaitesConsecutives: 0, positionClassementNormalisee: 0.0);
        var dernier = new ContexteJeu(serieDefaitesConsecutives: 0, positionClassementNormalisee: 1.0);

        double probaLeader = DeclenchementCalculator.CalculerProbabilitePonderee(archetype, leader, scoreAffinite: 0);
        double probaDernier = DeclenchementCalculator.CalculerProbabilitePonderee(archetype, dernier, scoreAffinite: 0);

        Assert.True(probaDernier > probaLeader);
    }

    [Fact]
    public void CalculerProbabilitePonderee_AugmenteQuandAffiniteEstBasse()
    {
        var archetype = new ArchetypeEvenement("clash_vestiaire", probabiliteBase: 0.04);
        var contexte = new ContexteJeu(serieDefaitesConsecutives: 0, positionClassementNormalisee: 0.5);

        double probaBonneEntente = DeclenchementCalculator.CalculerProbabilitePonderee(archetype, contexte, scoreAffinite: 90);
        double probaTensionForte = DeclenchementCalculator.CalculerProbabilitePonderee(archetype, contexte, scoreAffinite: -90);

        Assert.True(probaTensionForte > probaBonneEntente);
    }

    [Fact]
    public void CalculerProbabilitePonderee_ResteToujoursDansLIntervalle0Et1()
    {
        var archetype = new ArchetypeEvenement("clash_vestiaire", probabiliteBase: 1.0);
        var contexteExtreme = new ContexteJeu(serieDefaitesConsecutives: 50, positionClassementNormalisee: 1.0);

        double probabilite = DeclenchementCalculator.CalculerProbabilitePonderee(archetype, contexteExtreme, scoreAffinite: -100);

        Assert.InRange(probabilite, 0.0, 1.0);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void CalculerProbabilitePonderee_AvecScoreAffiniteNonFini_LeveException(double scoreInvalide)
    {
        var archetype = new ArchetypeEvenement("clash_vestiaire", probabiliteBase: 0.04);
        var contexte = new ContexteJeu(serieDefaitesConsecutives: 0, positionClassementNormalisee: 0.5);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DeclenchementCalculator.CalculerProbabilitePonderee(archetype, contexte, scoreInvalide));
    }

    [Theory]
    [InlineData(-100, 0.6)] // modificateur_affinité = 1.5 (borne haute) × probabiliteBase 0.4
    [InlineData(100, 0.2)]  // modificateur_affinité = 0.5 (borne basse) × probabiliteBase 0.4
    public void CalculerProbabilitePonderee_AuxBornesDuScoreAffinite_AtteintLesBornesAttenduesDuModificateur(int scoreAffinite, double probabiliteAttendue)
    {
        // probabiliteBase=0.4 (assez bas pour que le clamp final [0,1] n'écrase pas l'effet du
        // modificateur_affinité) et contexte neutre isolent ce modificateur pour l'observer directement.
        var archetype = new ArchetypeEvenement("clash_vestiaire", probabiliteBase: 0.4);
        var contexteNeutre = new ContexteJeu(serieDefaitesConsecutives: 0, positionClassementNormalisee: 0.0);

        double probabilite = DeclenchementCalculator.CalculerProbabilitePonderee(archetype, contexteNeutre, scoreAffinite);

        Assert.Equal(probabiliteAttendue, probabilite, precision: 10);
    }

    [Fact]
    public void EstDeclenche_QuandRollEstInferieurALaProbabilite_RenvoieVrai()
    {
        var generateur = new GenerateurAleatoireFixe(0.05);

        bool declenche = DeclenchementCalculator.EstDeclenche(probabilitePonderee: 0.10, generateur);

        Assert.True(declenche);
    }

    [Fact]
    public void EstDeclenche_QuandRollEstSuperieurOuEgalALaProbabilite_RenvoieFaux()
    {
        var generateur = new GenerateurAleatoireFixe(0.50);

        bool declenche = DeclenchementCalculator.EstDeclenche(probabilitePonderee: 0.10, generateur);

        Assert.False(declenche);
    }

    [Fact]
    public void EstDeclenche_AvecProbabiliteZero_NeSeDeclencheJamais()
    {
        var generateur = new GenerateurAleatoireFixe(0.0);

        bool declenche = DeclenchementCalculator.EstDeclenche(probabilitePonderee: 0.0, generateur);

        Assert.False(declenche);
    }

    [Fact]
    public void EstDeclenche_AvecProbabiliteNaN_LeveException()
    {
        // Régression : Math.Clamp et les comparaisons `< 0.0 or > 1.0` laissent passer NaN
        // silencieusement en .NET (toute comparaison avec NaN vaut false). Sans garde explicite,
        // un NaN en amont produirait un événement qui ne se déclenche jamais, sans erreur.
        var generateur = new GenerateurAleatoireFixe(0.5);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DeclenchementCalculator.EstDeclenche(double.NaN, generateur));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void EstDeclenche_AvecProbabiliteHorsIntervalle_LeveException(double probabiliteInvalide)
    {
        var generateur = new GenerateurAleatoireFixe(0.5);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DeclenchementCalculator.EstDeclenche(probabiliteInvalide, generateur));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ArchetypeEvenement_AvecIdInvalide_LeveException(string idInvalide)
    {
        Assert.Throws<ArgumentException>(() => new ArchetypeEvenement(idInvalide, probabiliteBase: 0.1));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void ArchetypeEvenement_AvecProbabiliteBaseHorsIntervalle_LeveException(double probabiliteInvalide)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ArchetypeEvenement("clash_vestiaire", probabiliteInvalide));
    }

    [Fact]
    public void ContexteJeu_AvecSerieDefaitesNegative_LeveException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContexteJeu(-1, 0.5));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void ContexteJeu_AvecPositionClassementHorsIntervalle_LeveException(double positionInvalide)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContexteJeu(0, positionInvalide));
    }
}
