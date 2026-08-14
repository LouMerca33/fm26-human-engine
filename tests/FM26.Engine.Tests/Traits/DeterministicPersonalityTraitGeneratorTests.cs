using FM26.Engine.Traits;

namespace FM26.Engine.Tests.Traits;

public class DeterministicPersonalityTraitGeneratorTests
{
    private readonly DeterministicPersonalityTraitGenerator _generateur = new();

    [Fact]
    public void Generate_AvecMemeContexte_ProduitToujoursLeMemeProfil()
    {
        var contexte = new TraitGenerationContext("joueur-42", 24);

        var premierTirage = _generateur.Generate(contexte);
        var deuxiemeTirage = _generateur.Generate(contexte);

        Assert.Equal(premierTirage, deuxiemeTirage);
    }

    [Fact]
    public void Generate_AvecIdentifiantsDifferents_ProduitDesProfilsDifferents()
    {
        var contexteA = new TraitGenerationContext("joueur-1", 24);
        var contexteB = new TraitGenerationContext("joueur-2", 24);

        var profilA = _generateur.Generate(contexteA);
        var profilB = _generateur.Generate(contexteB);

        Assert.NotEqual(profilA, profilB);
    }

    [Theory]
    [InlineData("joueur-1", 17)]
    [InlineData("joueur-2", 24)]
    [InlineData("joueur-3", 31)]
    [InlineData("joueur-4", 38)]
    [InlineData("gardien-x", 29)]
    public void Generate_ProduitToujoursDesTraitsDansLEchelle1a20(string id, int age)
    {
        var contexte = new TraitGenerationContext(id, age);

        var profil = _generateur.Generate(contexte);

        Assert.InRange(profil.Ego, PersonalityTraits.ValeurMin, PersonalityTraits.ValeurMax);
        Assert.InRange(profil.Leadership, PersonalityTraits.ValeurMin, PersonalityTraits.ValeurMax);
        Assert.InRange(profil.LoyauteGroupe, PersonalityTraits.ValeurMin, PersonalityTraits.ValeurMax);
        Assert.InRange(profil.TolerancePression, PersonalityTraits.ValeurMin, PersonalityTraits.ValeurMax);
        Assert.InRange(profil.Discretion, PersonalityTraits.ValeurMin, PersonalityTraits.ValeurMax);
        Assert.InRange(profil.Ambition, PersonalityTraits.ValeurMin, PersonalityTraits.ValeurMax);
    }

    [Fact]
    public void Generate_JoueursJeunes_OntEnMoyenneUneAmbitionPlusElevee_QueJoueursAges()
    {
        double moyenneAmbitionJeunes = MoyenneAmbitionPourTrancheAge(ageMin: 16, ageMax: 19, prefixeId: "jeune");
        double moyenneAmbitionAges = MoyenneAmbitionPourTrancheAge(ageMin: 35, ageMax: 40, prefixeId: "veteran");

        Assert.True(
            moyenneAmbitionJeunes > moyenneAmbitionAges,
            $"Ambition moyenne jeunes ({moyenneAmbitionJeunes}) devrait dépasser celle des vétérans ({moyenneAmbitionAges}).");
    }

    [Fact]
    public void Generate_JoueursDansLaTrentaine_OntEnMoyenneUnLeadershipPlusEleve_QueJoueursTresJeunes()
    {
        double moyenneLeadershipTrentaine = MoyenneLeadershipPourTrancheAge(ageMin: 28, ageMax: 32, prefixeId: "trentaine");
        double moyenneLeadershipJeunes = MoyenneLeadershipPourTrancheAge(ageMin: 16, ageMax: 18, prefixeId: "ado");

        Assert.True(
            moyenneLeadershipTrentaine > moyenneLeadershipJeunes,
            $"Leadership moyen trentaine ({moyenneLeadershipTrentaine}) devrait dépasser celui des très jeunes ({moyenneLeadershipJeunes}).");
    }

    [Fact]
    public void Generate_AvecContexteNull_LeveException()
    {
        Assert.Throws<ArgumentNullException>(() => _generateur.Generate(null!));
    }

    private double MoyenneAmbitionPourTrancheAge(int ageMin, int ageMax, string prefixeId)
    {
        var ambitions = new List<int>();
        for (int age = ageMin; age <= ageMax; age++)
        {
            for (int i = 0; i < 20; i++)
            {
                var contexte = new TraitGenerationContext($"{prefixeId}-{age}-{i}", age);
                ambitions.Add(_generateur.Generate(contexte).Ambition);
            }
        }

        return ambitions.Average();
    }

    private double MoyenneLeadershipPourTrancheAge(int ageMin, int ageMax, string prefixeId)
    {
        var leaderships = new List<int>();
        for (int age = ageMin; age <= ageMax; age++)
        {
            for (int i = 0; i < 20; i++)
            {
                var contexte = new TraitGenerationContext($"{prefixeId}-{age}-{i}", age);
                leaderships.Add(_generateur.Generate(contexte).Leadership);
            }
        }

        return leaderships.Average();
    }
}
