using System.IO;
using UnityEditor;
using UnityEngine;

namespace MoveMotion.Editor
{
    public static class BrandingAssetSetup
    {
        private const string ComponentsFolder = "../components";
        private const string TargetFolder = "Assets/App/UI/Sprites/Branding";

        [MenuItem("Build/Setup Branding Assets")]
        public static void Setup()
        {
            Directory.CreateDirectory(TargetFolder);

            // 1. Copy Background
            string bgSrc = Path.Combine(ComponentsFolder, "background.png");
            string bgDst = Path.Combine(TargetFolder, "background.png");
            CopyAndImport(bgSrc, bgDst, true);

            // 2. Copy UNP Logo
            string unpSrc = Path.Combine(ComponentsFolder, "unp.jpg");
            string unpDst = Path.Combine(TargetFolder, "unp.png"); // save as png for clean import
            if (File.Exists(unpSrc))
            {
                byte[] bytes = File.ReadAllBytes(unpSrc);
                File.WriteAllBytes(unpDst, bytes);
                AssetDatabase.ImportAsset(unpDst, ImportAssetOptions.ForceSynchronousImport);
                ConfigureAsSprite(unpDst);
            }

            // 3. Copy Application Icon
            string iconSrc = Path.Combine(ComponentsFolder, "icon.png");
            string iconDst = Path.Combine(TargetFolder, "icon.png");
            CopyAndImport(iconSrc, iconDst, false); // Keep as Default Texture type for Icon settings

            // 4. Configure Application Icons in PlayerSettings
            Texture2D iconTex = AssetDatabase.LoadAssetAtPath<Texture2D>(iconDst);
            if (iconTex != null)
            {
                var groups = new[] { BuildTargetGroup.Android, BuildTargetGroup.Unknown };
                foreach (BuildTargetGroup group in groups)
                {
                    Texture2D[] icons = PlayerSettings.GetIconsForTargetGroup(group);
                    for (int i = 0; i < icons.Length; i++)
                        icons[i] = iconTex;
                    PlayerSettings.SetIconsForTargetGroup(group, icons);
                }
                Debug.Log("[BrandingAssetSetup] Configured application icons in PlayerSettings successfully.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CopyAndImport(string src, string dst, bool asSprite)
        {
            if (!File.Exists(src))
            {
                Debug.LogWarning($"[BrandingAssetSetup] Source file not found: {src}");
                return;
            }

            File.Copy(src, dst, true);
            AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceSynchronousImport);
            if (asSprite)
                ConfigureAsSprite(dst);
        }

        private static void ConfigureAsSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
    }
}
