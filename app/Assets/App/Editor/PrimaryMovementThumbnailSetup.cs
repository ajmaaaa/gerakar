using System.Collections.Generic;
using System.IO;
using MotionLearn.Content;
using UnityEditor;
using UnityEngine;

public static class PrimaryMovementThumbnailSetup
{
    private const string DestinationFolder = "Assets/App/UI/Sprites/Primary";

    [MenuItem("Build/Setup Primary Movement Thumbnails")]
    public static void Setup()
    {
        string sourceFolder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../components"));
        Directory.CreateDirectory(Path.GetFullPath(DestinationFolder));

        var mappings = new Dictionary<string, (string source, string destination)>
        {
            { "squat", ("T1.png", "Squat.png") },
            { "dynamic_stretch", ("I1.png", "DynamicStretching.png") },
            { "ladder_drill", ("L1.png", "LadderDrill.png") }
        };

        foreach (var mapping in mappings)
        {
            string sourcePath = Path.Combine(sourceFolder, mapping.Value.source);
            string destinationPath = $"{DestinationFolder}/{mapping.Value.destination}";
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("Primary movement thumbnail was not found.", sourcePath);

            File.Copy(sourcePath, Path.GetFullPath(destinationPath), true);
            AssetDatabase.ImportAsset(destinationPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(destinationPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.maxTextureSize = 1024;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }

            MovementData movement = AssetDatabase.LoadAssetAtPath<MovementData>(
                $"Assets/App/Content/MovementData/MovementData_{MovementAssetSuffix(mapping.Key)}.asset");
            if (movement == null)
                throw new System.InvalidOperationException($"Movement data '{mapping.Key}' was not found.");

            movement.thumbnail = AssetDatabase.LoadAssetAtPath<Sprite>(destinationPath);
            EditorUtility.SetDirty(movement);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PrimaryMovementThumbnailSetup] Three primary thumbnails are ready.");
    }

    private static string MovementAssetSuffix(string movementId) => movementId switch
    {
        "squat" => "Squat",
        "dynamic_stretch" => "DynamicStretch",
        "ladder_drill" => "LadderDrill",
        _ => movementId
    };
}
