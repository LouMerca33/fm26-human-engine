namespace FM26.Engine.Declenchement;

/// <summary>Implémentation par défaut de <see cref="IGenerateurAleatoire"/>, adossée à <see cref="System.Random"/>.</summary>
public sealed class GenerateurAleatoireSysteme : IGenerateurAleatoire
{
    private readonly Random _random;

    public GenerateurAleatoireSysteme()
    {
        _random = new Random();
    }

    public GenerateurAleatoireSysteme(int seed)
    {
        _random = new Random(seed);
    }

    public double TirerRoll() => _random.NextDouble();
}
