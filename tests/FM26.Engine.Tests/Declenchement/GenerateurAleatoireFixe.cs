using FM26.Engine.Declenchement;

namespace FM26.Engine.Tests.Declenchement;

/// <summary>Double de test : renvoie toujours la même valeur de roll, pour des assertions déterministes.</summary>
internal sealed class GenerateurAleatoireFixe(double valeur) : IGenerateurAleatoire
{
    public double TirerRoll() => valeur;
}
