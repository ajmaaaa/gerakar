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
        Debug.Log("[MotionLearn] Shape sprite generation complete.");
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

    private static void GenerateRoundedRect(string path, int unusedSize, int displayRadius)
    {
        int scale = 4;
        int radius = displayRadius * scale;
        int size = radius * 4;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = radius;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float alphaSum = 0f;
                int samples = 4;
                for (int sx = 0; sx < samples; sx++)
                {
                    for (int sy = 0; sy < samples; sy++)
                    {
                        float px = x + (sx + 0.5f) / samples;
                        float py = y + (sy + 0.5f) / samples;
                        float sampleAlpha = 1f;

                        if (px < r && py < r)
                        {
                            float dx = px - r; float dy = py - r;
                            if (dx * dx + dy * dy > r * r) sampleAlpha = 0f;
                        }
                        else if (px < r && py >= size - r)
                        {
                            float dx = px - r; float dy = py - (size - r);
                            if (dx * dx + dy * dy > r * r) sampleAlpha = 0f;
                        }
                        else if (px >= size - r && py < r)
                        {
                            float dx = px - (size - r); float dy = py - r;
                            if (dx * dx + dy * dy > r * r) sampleAlpha = 0f;
                        }
                        else if (px >= size - r && py >= size - r)
                        {
                            float dx = px - (size - r); float dy = py - (size - r);
                            if (dx * dx + dy * dy > r * r) sampleAlpha = 0f;
                        }
                        alphaSum += sampleAlpha;
                    }
                }

                float finalAlpha = alphaSum / (samples * samples);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, finalAlpha));
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
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.alphaIsTransparency = true;
            importer.spritePixelsPerUnit = 400;
            importer.spriteBorder = path.Contains("Circle") ? Vector4.zero : new Vector4(radius, radius, radius, radius);
            importer.SaveAndReimport();
        }
    }

    private static void GenerateRoundTopRect(string path, int unusedSize, int displayRadius)
    {
        int scale = 4;
        int radius = displayRadius * scale;
        int size = radius * 4;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = radius;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float alphaSum = 0f;
                int samples = 4;
                for (int sx = 0; sx < samples; sx++)
                {
                    for (int sy = 0; sy < samples; sy++)
                    {
                        float px = x + (sx + 0.5f) / samples;
                        float py = y + (sy + 0.5f) / samples;
                        float sampleAlpha = 1f;

                        if (px < r && py >= size - r) // top-left
                        {
                            float dx = px - r; float dy = py - (size - r);
                            if (dx * dx + dy * dy > r * r) sampleAlpha = 0f;
                        }
                        else if (px >= size - r && py >= size - r) // top-right
                        {
                            float dx = px - (size - r); float dy = py - (size - r);
                            if (dx * dx + dy * dy > r * r) sampleAlpha = 0f;
                        }
                        alphaSum += sampleAlpha;
                    }
                }

                float finalAlpha = alphaSum / (samples * samples);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, finalAlpha));
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
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.alphaIsTransparency = true;
            importer.spritePixelsPerUnit = 400;
            importer.spriteBorder = new Vector4(radius, 0, radius, radius);
            importer.SaveAndReimport();
        }
    }
}
