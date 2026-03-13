using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public static class ShaderLoader
    {
        private const string MyPackageId = "TOT.CeleTech.MKIII";

        public static Shader CustomShader { get; private set; }
        public static Shader MaskedShader { get; private set; }
        public static Shader PulseEffectShader { get; private set; }
        static ShaderLoader()
        {
            var mod = LoadedModManager.RunningModsListForReading.FirstOrDefault(
                m => m.PackageId.Equals(MyPackageId, StringComparison.InvariantCultureIgnoreCase));

            if (mod == null)
            {
                Log.Error($"[CELETECH] Cannot find mod packageId: {MyPackageId}");
                return;
            }
            CustomShader = LoadFromThisMod(mod, "Map/CMC_UniversalCamoShader_RGB");
            MaskedShader = LoadFromThisMod(mod, "Map/UniversalCamoShader_ApparelMasked");
            PulseEffectShader = LoadFromThisMod(mod, "Map/ShaderAnimated_A");
        }
        private static Shader LoadFromThisMod(ModContentPack mod, string shaderPath)
        {
            string byFolderName = $"Assets/Data/{mod.FolderName}/Materials/{shaderPath}.shader";
            string byPackageId = $"Assets/Data/{mod.PackageIdPlayerFacing}/Materials/{shaderPath}.shader";
            foreach (var bundle in mod.assetBundles.loadedAssetBundles)
            {
                Shader s = bundle.LoadAsset<Shader>(byFolderName)
                        ?? bundle.LoadAsset<Shader>(byFolderName.ToLowerInvariant())
                        ?? bundle.LoadAsset<Shader>(byPackageId)
                        ?? bundle.LoadAsset<Shader>(byPackageId.ToLowerInvariant());

                if (s != null)
                {
                    Log.Message($"[CELETECH] Loaded shader '{shaderPath}' => '{s.name}', supported={s.isSupported}");
                    return s;
                }
            }
            Shader fallback = ShaderDatabase.LoadShader(shaderPath);
            if (fallback == null || fallback == ShaderDatabase.DefaultShader)
            {
                Log.Error($"[CELETECH] Shader not found: {shaderPath}");
            }
            else
            {
                Log.Warning($"[CELETECH] Fallback loaded shader '{shaderPath}' => '{fallback.name}'");
            }
            return fallback;
        }
        public static void DumpShaderAssetNames()
        {
            var mod = LoadedModManager.RunningModsListForReading.FirstOrDefault(
                m => m.PackageId.Equals(MyPackageId, StringComparison.InvariantCultureIgnoreCase));
            if (mod == null) return;

            for (int i = 0; i < mod.assetBundles.loadedAssetBundles.Count; i++)
            {
                var bundle = mod.assetBundles.loadedAssetBundles[i];
                Log.Message($"[CELETECH] Bundle[{i}] = {bundle.name}");
                foreach (var n in bundle.GetAllAssetNames().Where(n => n.EndsWith(".shader")))
                    Log.Message("  " + n);
            }
        }
    }
}