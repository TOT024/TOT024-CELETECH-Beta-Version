#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class RimWorldShaderBundleBuilder
{
    // 这里改成你的 PackageId（和 About.xml 一致）
    private const string PackageId = "TOT.CeleTech.MKIII";

    private class ShaderEntry
    {
        public string bundleBaseName; // 不带平台后缀
        public string shaderAssetPath; // Unity工程内路径，必须 Assets/... .shader
        public ShaderEntry(string bundleBaseName, string shaderAssetPath)
        {
            this.bundleBaseName = bundleBaseName;
            this.shaderAssetPath = shaderAssetPath.Replace("\\", "/");
        }
    }

    // 你当前的3个shader
    private static readonly ShaderEntry[] Entries = new[]
    {
        new ShaderEntry("cmc_shader",       $"Assets/Data/{PackageId}/Materials/Map/CMC_UniversalCamoShader_RGB.shader"),
        new ShaderEntry("cmc_shadermasked", $"Assets/Data/{PackageId}/Materials/Map/UniversalCamoShader_ApparelMasked.shader"),
        new ShaderEntry("cmc_shaderpulse",  $"Assets/Data/{PackageId}/Materials/Map/ShaderAnimated_A.shader"),
    };

    [MenuItem("Tools/RimWorld/Build Shader Bundles (Win/Linux/Mac)")]
    public static void BuildAllPlatforms()
    {
        string outputDir = EditorUtility.OpenFolderPanel(
            "Select your MOD's AssetBundles folder (e.g. .../Mods/YourMod/AssetBundles)",
            Application.dataPath,
            ""
        );

        if (string.IsNullOrEmpty(outputDir))
            return;

        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        if (!ValidateEntries())
            return;

        BuildForTarget(BuildTarget.StandaloneWindows64, "_win", outputDir);
        BuildForTarget(BuildTarget.StandaloneLinux64, "_linux", outputDir);
        BuildForTarget(BuildTarget.StandaloneOSX, "_mac", outputDir);

        AssetDatabase.Refresh();
        Debug.Log($"[RimWorldShaderBundleBuilder] Done. Output: {outputDir}");
    }

    private static bool ValidateEntries()
    {
        bool ok = true;
        foreach (var e in Entries)
        {
            if (!e.shaderAssetPath.StartsWith("Assets/"))
            {
                Debug.LogError($"[RimWorldShaderBundleBuilder] Invalid path (must start with Assets/): {e.shaderAssetPath}");
                ok = false;
                continue;
            }

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(e.shaderAssetPath);
            if (shader == null)
            {
                Debug.LogError($"[RimWorldShaderBundleBuilder] Shader not found at path: {e.shaderAssetPath}");
                ok = false;
            }
        }
        return ok;
    }

    private static void BuildForTarget(BuildTarget target, string suffix, string outputDir)
    {
        var builds = new List<AssetBundleBuild>();

        foreach (var e in Entries)
        {
            builds.Add(new AssetBundleBuild
            {
                assetBundleName = e.bundleBaseName + suffix, // 例如 cmc_shader_win
                assetNames = new[] { e.shaderAssetPath }      // 直接打这个shader
            });
        }

        var manifest = BuildPipeline.BuildAssetBundles(
            outputDir,
            builds.ToArray(),
            BuildAssetBundleOptions.StrictMode | BuildAssetBundleOptions.ChunkBasedCompression,
            target
        );

        if (manifest == null)
            Debug.LogError($"[RimWorldShaderBundleBuilder] Build failed for {target}");
        else
            Debug.Log($"[RimWorldShaderBundleBuilder] Built {target} -> suffix {suffix}");
    }
}
#endif