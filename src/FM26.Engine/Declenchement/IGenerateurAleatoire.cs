namespace FM26.Engine.Declenchement;

/// <summary>
/// Source du "roll" de la formule de déclenchement (spec §2.3). Injectable pour rendre
/// <see cref="DeclenchementCalculator.EstDeclenche"/> testable de façon déterministe.
/// </summary>
public interface IGenerateurAleatoire
{
    /// <summary>Tire une valeur uniforme dans [0, 1).</summary>
    double TirerRoll();
}
