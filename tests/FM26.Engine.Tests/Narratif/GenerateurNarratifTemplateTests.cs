using FM26.Engine.Narratif;
using FM26.Engine.Orchestration;

namespace FM26.Engine.Tests.Narratif;

public class GenerateurNarratifTemplateTests
{
    private static readonly SceneIncidentContexte Contexte = new("Joueur A", "Joueur B", "Club Test FC");

    private readonly GenerateurNarratifTemplate _generateur = new();

    [Fact]
    public void GenererSceneIncident_MentionneLesDeuxJoueursEtLeClub()
    {
        string scene = _generateur.GenererSceneIncident(Contexte);

        Assert.Contains("Joueur A", scene);
        Assert.Contains("Joueur B", scene);
        Assert.Contains("Club Test FC", scene);
    }

    [Fact]
    public void GenererOptionsCommunication_RenvoieExactementLesTroisOptionsDeLaSpec()
    {
        var options = _generateur.GenererOptionsCommunication(Contexte);

        Assert.Equal(3, options.Count);
        Assert.All(options.Values, texte => Assert.False(string.IsNullOrWhiteSpace(texte)));
        Assert.True(options.ContainsKey(ChoixCommunication.Apaiser));
        Assert.True(options.ContainsKey(ChoixCommunication.Couvrir));
        Assert.True(options.ContainsKey(ChoixCommunication.Sanctionner));
    }

    [Fact]
    public void GenererSceneIncident_EstDeterministe_MemeContexteMemeTexte()
    {
        string premier = _generateur.GenererSceneIncident(Contexte);
        string second = _generateur.GenererSceneIncident(Contexte);

        Assert.Equal(premier, second);
    }

    [Fact]
    public void GenererSceneIncident_AvecContexteNull_LeveException()
    {
        Assert.Throws<ArgumentNullException>(() => _generateur.GenererSceneIncident(null!));
    }

    [Theory]
    [InlineData("", "Joueur B", "Club")]
    [InlineData("Joueur A", "   ", "Club")]
    [InlineData("Joueur A", "Joueur B", "")]
    public void SceneIncidentContexte_AvecChampVide_LeveException(string nomA, string nomB, string nomClub)
    {
        Assert.Throws<ArgumentException>(() => new SceneIncidentContexte(nomA, nomB, nomClub));
    }
}
