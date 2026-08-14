using BepInEx;
using BepInEx.Unity.IL2CPP;

namespace FM26.BepInExPlugin;

/// <summary>
/// Plugin BepInEx minimal ("hello world") pour FM26 Human Engine.
///
/// Rôle actuel : valider que le pipeline d'injection complet fonctionne (BepInEx IL2CPP
/// chargé dans le process FM26 via Doorstop + dylib injection, plugin découvert et exécuté par
/// le chainloader) en loggant un message au chargement, via le logger standard BepInEx. Aucune
/// logique de jeu pour l'instant — cf. FM26_HUMAN_ENGINE.md, Phase 0 de la roadmap.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public class Plugin : BasePlugin
{
    public const string PluginGuid = "dev.loumerca.fm26humanengine";
    public const string PluginName = "FM26 Human Engine";
    public const string PluginVersion = "0.1.0";

    public override void Load()
    {
        Log.LogInfo($"{PluginName} v{PluginVersion} chargé avec succès (hello world).");
    }
}
