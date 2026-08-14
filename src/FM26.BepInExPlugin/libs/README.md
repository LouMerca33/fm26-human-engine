# Assemblies BepInEx (non versionnées)

Ce dossier attend, pour compiler `FM26.BepInExPlugin.csproj` :

- `BepInEx.Core.dll`
- `BepInEx.Preloader.Core.dll`
- `BepInEx.Unity.Common.dll`
- `BepInEx.Unity.IL2CPP.dll`
- `0Harmony.dll`

Elles ne sont **pas** commitées dans le repo (binaires tiers, redistribution évitée par hygiène de repo).
Pour les régénérer :

```bash
gh release download v6.0.0-pre.2 --repo BepInEx/BepInEx \
  --pattern "BepInEx-Unity.IL2CPP-macos-x64-6.0.0-pre.2.zip" -D /tmp/bepinex-dl
cd /tmp/bepinex-dl && unzip -q BepInEx-Unity.IL2CPP-macos-x64-6.0.0-pre.2.zip -d extracted
chmod -R u+rwX extracted   # le zip officiel perd les bits de permission
cp extracted/BepInEx/core/{BepInEx.Core.dll,BepInEx.Preloader.Core.dll,BepInEx.Unity.Common.dll,BepInEx.Unity.IL2CPP.dll,0Harmony.dll} \
  ~/Projects/fm26-human-engine/src/FM26.BepInExPlugin/libs/
```

Ce sont les mêmes DLL que celles installées dans le dossier du jeu FM26
(`~/Library/Application Support/Steam/steamapps/common/Football Manager 26/BepInEx/core/`)
une fois BepInEx installé — voir STATUS.md pour le détail de l'installation et son état actuel
(bloquée avant la génération des interop, cf. section dédiée).
