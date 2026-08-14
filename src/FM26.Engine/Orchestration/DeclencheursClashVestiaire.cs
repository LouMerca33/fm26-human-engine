using FM26.Engine.Declenchement;

namespace FM26.Engine.Orchestration;

/// <summary>
/// Déclencheurs de l'archétype clash_vestiaire (spec §3) : conditions de contexte minimales
/// ("série_défaites >= 2", "affinité_paire <= -30" dans l'exemple JSON de la spec) traduites
/// en seuils typés plutôt qu'en expressions à parser, plus la probabilité de base consommée
/// par le moteur de déclenchement pondéré de la Couche 1 (<see cref="DeclenchementCalculator"/>).
///
/// Les conditions de contexte agissent comme un portillon booléen : tant qu'elles ne sont pas
/// toutes remplies, l'archétype ne peut pas se déclencher, quel que soit le résultat du tirage
/// pondéré. Une fois le portillon ouvert, c'est la formule de la Couche 1 qui prend le relais.
/// </summary>
public sealed record DeclencheursClashVestiaire
{
    /// <summary>Nombre minimal de défaites consécutives pour que le portillon s'ouvre.</summary>
    public int SerieDefaitesMinimale { get; }

    /// <summary>Score d'affinité maximal (inclus) entre la paire pour que le portillon s'ouvre : plus la tension est forte (score bas), plus la condition est facile à remplir.</summary>
    public double AffiniteMaximale { get; }

    /// <summary>Probabilité mensuelle de base avant modificateurs, transmise à <see cref="DeclenchementCalculator"/>.</summary>
    public double ProbabiliteBase { get; }

    public DeclencheursClashVestiaire(int serieDefaitesMinimale, double affiniteMaximale, double probabiliteBase)
    {
        if (serieDefaitesMinimale < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(serieDefaitesMinimale), serieDefaitesMinimale,
                "Le seuil de série de défaites ne peut pas être négatif.");
        }

        if (affiniteMaximale is < -100.0 or > 100.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(affiniteMaximale), affiniteMaximale,
                "Le seuil d'affinité doit être compris entre -100 et 100.");
        }

        if (probabiliteBase is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(probabiliteBase), probabiliteBase,
                "La probabilité de base doit être comprise entre 0 et 1.");
        }

        SerieDefaitesMinimale = serieDefaitesMinimale;
        AffiniteMaximale = affiniteMaximale;
        ProbabiliteBase = probabiliteBase;
    }

    /// <summary>Portillon booléen : les deux conditions de contexte minimales doivent être remplies.</summary>
    public bool ConditionsContexteRemplies(ContexteJeu contexte, double scoreAffinite)
    {
        ArgumentNullException.ThrowIfNull(contexte);

        if (!double.IsFinite(scoreAffinite))
        {
            throw new ArgumentOutOfRangeException(
                nameof(scoreAffinite), scoreAffinite, "Le score d'affinité doit être un nombre fini.");
        }

        return contexte.SerieDefaitesConsecutives >= SerieDefaitesMinimale
            && scoreAffinite <= AffiniteMaximale;
    }
}
