using UnityEditor;
using UnityEngine;
using TMPro;
using System.IO;

public static class FontSetupHelper
{
    private static readonly string[] Weights = { "Regular", "Medium", "SemiBold", "Bold" };

    [MenuItem("Build/Setup Poppins Font Assets")]
    public static void SetupPoppinsFonts()
    {
        var fontDir = "Assets/App/UI/Fonts/Poppins";
        if (!Directory.Exists(fontDir))
        {
            Debug.LogError($"[FontSetupHelper] Font directory not found: {fontDir}");
            return;
        }

        foreach (var weight in Weights)
        {
            var ttfPath = $"{fontDir}/Poppins-{weight}.ttf";
            var sdfPath = $"{fontDir}/Poppins-{weight}_SDF.asset";

            if (!File.Exists(ttfPath))
            {
                Debug.LogWarning($"[FontSetupHelper] TTF not found: {ttfPath}");
                continue;
            }

            if (File.Exists(sdfPath))
            {
                var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(sdfPath);
                if (existing != null && existing.atlasTexture != null && existing.material != null)
                {
                    Debug.Log($"[FontSetupHelper] SDF already exists and is valid: {sdfPath}");
                    continue;
                }
                else
                {
                    Debug.LogWarning($"[FontSetupHelper] Existing SDF at {sdfPath} is invalid/missing atlas. Recreating...");
                    AssetDatabase.DeleteAsset(sdfPath);
                }
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (font == null)
            {
                AssetDatabase.ImportAsset(ttfPath);
                font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            }

            if (font == null)
            {
                Debug.LogError($"[FontSetupHelper] Failed to load font: {ttfPath}");
                continue;
            }

            var sdfFont = TMP_FontAsset.CreateFontAsset(font);
            if (sdfFont == null)
            {
                Debug.LogError($"[FontSetupHelper] Failed to create SDF asset for {weight}");
                continue;
            }

            sdfFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;

            AssetDatabase.CreateAsset(sdfFont, sdfPath);

            if (sdfFont.atlasTextures != null)
            {
                for (int i = 0; i < sdfFont.atlasTextures.Length; i++)
                {
                    if (sdfFont.atlasTextures[i] != null)
                    {
                        sdfFont.atlasTextures[i].name = $"{sdfFont.name} Atlas";
                        AssetDatabase.AddObjectToAsset(sdfFont.atlasTextures[i], sdfFont);
                    }
                }
            }

            if (sdfFont.material != null)
            {
                sdfFont.material.name = $"{sdfFont.name} Material";
                AssetDatabase.AddObjectToAsset(sdfFont.material, sdfFont);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[FontSetupHelper] Created Poppins-{weight} SDF asset at {sdfPath}");
        }

        AssetDatabase.Refresh();
        Debug.Log("[FontSetupHelper] Poppins font setup complete");
    }

    public static TMP_FontAsset LoadPoppinsFont(string weight)
    {
        var path = $"Assets/App/UI/Fonts/Poppins/Poppins-{weight}_SDF.asset";
        var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        return asset;
    }
}
