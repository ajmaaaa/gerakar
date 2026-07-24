using UnityEngine;
using UnityEditor;
using System.IO;

public static class CreateUISolidRectangle
{
    [MenuItem("Build/Create UISolidRectangle Sprite")]
    public static void Execute()
    {
        int size = 4;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color white = Color.white;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                tex.SetPixel(x, y, white);
        tex.Apply();

        byte[] pngData = tex.EncodeToPNG();
        string dir = "Assets/App/UI/Sprites";
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "UISolidRectangle.png");
        File.WriteAllBytes(path, pngData);
        AssetDatabase.Refresh();

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        Debug.Log("[MotionLearn] UISolidRectangle sprite created at " + path);
    }
}
