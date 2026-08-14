namespace FM26.Engine.Declenchement;

/// <summary>
/// Formule de déclenchement probabiliste pondéré (spec §2.3) :
///
///   probabilité_evenement(mois) = base_par_archétype
///                                × modificateur_contexte (résultats, classement, série)
///                                × modificateur_affinité (score bas = probabilité accrue)
///                                × facteur_aléatoire (roll)
///
/// Implémentation en deux temps : <see cref="CalculerProbabilitePonderee"/> calcule la
/// probabilité pondérée (base × modificateur_contexte × modificateur_affinité, bornée à
/// [0, 1]), puis <see cref="EstDeclenche"/> tire le "roll" et le compare à cette probabilité.
/// C'est ce roll qui reste la part de hasard pur — "pondéré, jamais pur" (spec) fait
/// référence à la probabilité qu'il doit battre, pas au roll lui-même.
/// </summary>
public static class DeclenchementCalculator
{
    private const int SerieDefaitesPlafond = 8;
    private const double PoidsSerieDefaites = 0.15;
    private const double PoidsClassement = 0.5;
    private const double PoidsAffinite = 0.5;

    // Bornes mathématiquement atteignables par ModificateurAffinite une fois scoreAffinite
    // clampé à [-100, 100] : 1 - (score/100)*PoidsAffinite vaut 1.5 à score=-100 et 0.5 à
    // score=100. Le Math.Clamp ci-dessous est un filet de sécurité, pas un plafond actif.
    private const double ModificateurAffiniteMin = 0.5;
    private const double ModificateurAffiniteMax = 1.5;

    public static double CalculerProbabilitePonderee(ArchetypeEvenement archetype, ContexteJeu contexte, double scoreAffinite)
    {
        ArgumentNullException.ThrowIfNull(archetype);
        ArgumentNullException.ThrowIfNull(contexte);

        if (!double.IsFinite(scoreAffinite))
        {
            throw new ArgumentOutOfRangeException(
                nameof(scoreAffinite), scoreAffinite, "Le score d'affinité doit être un nombre fini.");
        }

        double modContexte = ModificateurContexte(contexte);
        double modAffinite = ModificateurAffinite(scoreAffinite);
        double probabilite = archetype.ProbabiliteBase * modContexte * modAffinite;

        return Math.Clamp(probabilite, 0.0, 1.0);
    }

    public static bool EstDeclenche(double probabilitePonderee, IGenerateurAleatoire generateur)
    {
        ArgumentNullException.ThrowIfNull(generateur);

        // Écrit en négatif (plutôt que `< 0.0 or > 1.0`) car les comparaisons avec NaN sont
        // toujours fausses en .NET : la forme positive laisserait passer un NaN silencieusement
        // au lieu de lever, ce qui masquerait un état invalide en amont au lieu d'échouer fort.
        if (!(probabilitePonderee >= 0.0 && probabilitePonderee <= 1.0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(probabilitePonderee), probabilitePonderee, "La probabilité pondérée doit être comprise entre 0 et 1.");
        }

        return generateur.TirerRoll() < probabilitePonderee;
    }

    /// <summary>Série de défaites et mauvais classement amplifient la probabilité (plafonnés pour éviter une dérive).</summary>
    private static double ModificateurContexte(ContexteJeu contexte)
    {
        double effetSerie = Math.Min(contexte.SerieDefaitesConsecutives, SerieDefaitesPlafond) * PoidsSerieDefaites;
        double effetClassement = contexte.PositionClassementNormalisee * PoidsClassement;
        return 1.0 + effetSerie + effetClassement;
    }

    /// <summary>Score d'affinité bas (tension) = probabilité accrue ; score haut (bonne entente) = probabilité réduite.</summary>
    private static double ModificateurAffinite(double scoreAffinite)
    {
        double score = Math.Clamp(scoreAffinite, -100.0, 100.0);
        double modificateur = 1.0 - (score / 100.0) * PoidsAffinite;
        return Math.Clamp(modificateur, ModificateurAffiniteMin, ModificateurAffiniteMax);
    }
}
