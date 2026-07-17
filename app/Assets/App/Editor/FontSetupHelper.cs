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
                Debug.Log($"[FontSetupHelper] SDF already exists: {sdfPath}");
                continue;
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

            var sdfFont = TMP_FontAsset.CreateFontAsset(font, 90, 9, 5, true);
            if (sdfFont == null)
            {
                Debug.LogError($"[FontSetupHelper] Failed to create SDF asset for {weight}");
                continue;
            }

            sdfFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            sdfFont.material.mainTexture.filterMode = FilterMode.Bilinear;

            AssetDatabase.CreateAsset(sdfFont, sdfPath);
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
