using UnityEngine;
using UnityEditor;
using System.IO;

public static class CreateUIShapeSprites
{
    [MenuItem("Build/Generate UI Shape Sprites")]
    public static void Execute()
    {
        string dir = "Assets/App/UI/Sprites/Shapes";
        Directory.CreateDirectory(dir);

        // 1. UISolidRectangle (8x8 solid white)
        GenerateSolidRectangle(Path.Combine(dir, "UISolidRectangle.png"));

        // 2. Rounded Rects
        GenerateRoundedRect(Path.Combine(dir, "RoundedRect-08.png"), 32, 8);
        GenerateRoundedRect(Path.Combine(dir, "RoundedRect-12.png"), 48, 12);
        GenerateRoundedRect(Path.Combine(dir, "RoundedRect-16.png"), 64, 16);
        GenerateRoundedRect(Path.Combine(dir, "RoundedRect-24.png"), 96, 24);

        // 3. Perfect Circle
        GenerateRoundedRect(Path.Combine(dir, "Circle-24.png"), 48, 24);

        // 4. Round Top only
        GenerateRoundTopRect(Path.Combine(dir, "RoundTop-24.png"), 96, 24);

        AssetDatabase.Refresh();
        Debug.Log("[GerakAR] Shape sprite generation complete.");
    }

    private static void GenerateSolidRectangle(string path)
    {
        int size = 8;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color white = Color.white;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                tex.SetPixel(x, y, white);
        tex.Apply();

        byte[] pngData = tex.EncodeToPNG();
        File.WriteAllBytes(path, pngData);
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.alphaIsTransparency = false;
            importer.SaveAndReimport();
        }
    }

    private static void GenerateRoundedRect(string path, int size, int radius)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float alpha = 1f;
                if (x < radius && y < radius)
                {
                    float dx = x - radius + 0.5f;
                    float dy = y - radius + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    alpha = Mathf.Clamp01(radius - dist + 0.5f);
                }
                else if (x < radius && y >= size - radius)
                {
                    float dx = x - radius + 0.5f;
                    float dy = y - (size - radius) + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    alpha = Mathf.Clamp01(radius - dist + 0.5f);
                }
                else if (x >= size - radius && y < radius)
                {
                    float dx = x - (size - radius) + 0.5f;
                    float dy = y - radius + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    alpha = Mathf.Clamp01(radius - dist + 0.5f);
                }
                else if (x >= size - radius && y >= size - radius)
                {
                    float dx = x - (size - radius) + 0.5f;
                    float dy = y - (size - radius) + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    alpha = Mathf.Clamp01(radius - dist + 0.5f);
                }

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        byte[] pngData = tex.EncodeToPNG();
        File.WriteAllBytes(path, pngData);
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.alphaIsTransparency = true;
            importer.spriteBorder = new Vector4(radius, radius, radius, radius);
            importer.SaveAndReimport();
        }
    }

    private static void GenerateRoundTopRect(string path, int size, int radius)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float alpha = 1f;
                // Only round top-left and top-right corners
                if (x < radius && y >= size - radius) // top-left
                {
                    float dx = x - radius + 0.5f;
                    float dy = y - (size - radius) + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    alpha = Mathf.Clamp01(radius - dist + 0.5f);
                }
                else if (x >= size - radius && y >= size - radius) // top-right
                {
                    float dx = x - (size - radius) + 0.5f;
                    float dy = y - (size - radius) + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    alpha = Mathf.Clamp01(radius - dist + 0.5f);
                }

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        byte[] pngData = tex.EncodeToPNG();
        File.WriteAllBytes(path, pngData);
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.alphaIsTransparency = true;
            importer.spriteBorder = new Vector4(radius, 0, radius, radius); // no border padding for bottom edge
            importer.SaveAndReimport();
        }
    }
}
