using System.IO;
using System.Linq;
using MoveMotion.Content;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AirSquatSetup
{
    private const string ModelPath = "Assets/App/Models/Squat/Air Squat.fbx";
    private const string TextureFolder = "Assets/App/Models/Squat/Textures";
    private const string MaterialPath = "Assets/App/Models/Squat/Materials/AirSquat.mat";
    private const string ControllerPath = "Assets/App/Animations/Controllers/AirSquat.controller";
    private const string PrefabPath = "Assets/App/Prefabs/Models/AirSquat.prefab";
    private const string MovementDataPath = "Assets/App/Content/MovementData/MovementData_Squat.asset";
    private const string RuntimeTargetPath = "Assets/StreamingAssets/C5.png";

    [MenuItem("Build/Setup Air Squat Test Model")]
    public static void Setup()
    {
        string sourcePath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../models/Air Squat.fbx"));
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Air Squat source FBX was not found.", sourcePath);

        PrepareScanTarget();

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ModelPath)) ?? string.Empty);
        File.Copy(sourcePath, Path.GetFullPath(ModelPath), true);
        AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

        ConfigureModelImporter();
        ExtractAndConfigureTextures();

        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(candidate => !candidate.name.StartsWith("__preview__"));
        if (modelAsset == null || clip == null)
            throw new System.InvalidOperationException("Air Squat FBX did not import with a model and animation clip.");

        Material material = CreateOrUpdateMaterial();
        AnimatorController controller = CreateOrUpdateController(clip);
        GameObject prefab = CreateOrUpdatePrefab(modelAsset, material, controller);

        MovementData movement = AssetDatabase.LoadAssetAtPath<MovementData>(MovementDataPath);
        if (movement == null)
            throw new System.InvalidOperationException("MovementData_Squat.asset was not found.");

        movement.modelPrefab = prefab;
        movement.animationClip = clip;
        EditorUtility.SetDirty(movement);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[AirSquatSetup] Ready: model='{modelAsset.name}', clip='{clip.name}', " +
                  $"duration={clip.length:F2}s, prefab='{PrefabPath}'.");
    }

    [MenuItem("Build/Capture Air Squat Test Preview")]
    public static void CapturePreview()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(candidate => !candidate.name.StartsWith("__preview__"));
        if (prefab == null || clip == null)
            throw new System.InvalidOperationException("Run Air Squat setup before capturing its preview.");

        Scene previewScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        var cameraObject = new GameObject("Preview Camera", typeof(Camera));
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(244, 240, 230, 255);
        camera.fieldOfView = 32f;

        var lightObject = new GameObject("Preview Light", typeof(Light));
        Light light = lightObject.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.4f;
        lightObject.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
        RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);

        string outputDirectory = System.Environment.GetEnvironmentVariable("AIR_SQUAT_PREVIEW_DIR");
        if (string.IsNullOrEmpty(outputDirectory))
            outputDirectory = "/tmp/opencode/gerakar-air-squat-preview";
        Directory.CreateDirectory(outputDirectory);

        SavePreview(instance, clip, 0f, camera, Path.Combine(outputDirectory, "AirSquat_Start.png"));
        SavePreview(instance, clip, clip.length * 0.25f, camera, Path.Combine(outputDirectory, "AirSquat_Quarter.png"));
        SavePreview(instance, clip, clip.length * 0.5f, camera, Path.Combine(outputDirectory, "AirSquat_Mid.png"));
        SavePreview(instance, clip, clip.length * 0.75f, camera, Path.Combine(outputDirectory, "AirSquat_ThreeQuarter.png"));

        Debug.Log($"[AirSquatSetup] Preview images saved to '{outputDirectory}'.");
    }

    [MenuItem("Build/Validate Air Squat Scan Setup")]
    public static void ValidateScanSetup()
    {
        EditorSceneManager.OpenScene("Assets/App/Scenes/MainAR.unity", OpenSceneMode.Single);
        SetupAndBuild.ValidateSetup();

        MovementData movement = AssetDatabase.LoadAssetAtPath<MovementData>(MovementDataPath);
        if (movement == null || movement.modelPrefab == null || movement.animationClip == null)
            throw new System.InvalidOperationException("Squat movement model or animation is not assigned.");

        Animator animator = movement.modelPrefab.GetComponentInChildren<Animator>(true);
        if (animator == null || animator.runtimeAnimatorController == null)
            throw new System.InvalidOperationException("Air Squat prefab Animator is not configured.");

        string sourceTarget = Path.GetFullPath(Path.Combine(Application.dataPath, "../../objects/C5.png"));
        string runtimeTarget = Path.GetFullPath(RuntimeTargetPath);
        if (!File.Exists(sourceTarget) ||
            !CreateWhiteBackgroundTarget(File.ReadAllBytes(sourceTarget)).SequenceEqual(File.ReadAllBytes(runtimeTarget)))
            throw new System.InvalidOperationException("Source C5.png differs from the runtime scan target.");

        Debug.Log($"[AirSquatSetup] Scan setup passed: target='C5.png', " +
                  $"model='{movement.modelPrefab.name}', clip='{movement.animationClip.name}'.");
    }

    private static void PrepareScanTarget()
    {
        string sourcePath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../objects/C5.png"));
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("C5 scan source image was not found.", sourcePath);

        File.WriteAllBytes(Path.GetFullPath(RuntimeTargetPath),
            CreateWhiteBackgroundTarget(File.ReadAllBytes(sourcePath)));
        AssetDatabase.ImportAsset(RuntimeTargetPath,
            ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
    }

    private static byte[] CreateWhiteBackgroundTarget(byte[] sourceBytes)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(sourceBytes, false))
            throw new System.InvalidOperationException("C5.png could not be decoded.");

        Color32[] pixels = texture.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 pixel = pixels[i];
            float alpha = pixel.a / 255f;
            pixel.r = (byte)Mathf.RoundToInt(pixel.r * alpha + 255f * (1f - alpha));
            pixel.g = (byte)Mathf.RoundToInt(pixel.g * alpha + 255f * (1f - alpha));
            pixel.b = (byte)Mathf.RoundToInt(pixel.b * alpha + 255f * (1f - alpha));
            pixel.a = 255;
            pixels[i] = pixel;
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        byte[] flattened = texture.EncodeToPNG();
        Object.DestroyImmediate(texture);
        return flattened;
    }

    private static void ConfigureModelImporter()
    {
        var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null)
            throw new System.InvalidOperationException("Air Squat ModelImporter is unavailable.");

        importer.importAnimation = true;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.isReadable = false;
        importer.meshCompression = ModelImporterMeshCompression.Medium;

        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        foreach (ModelImporterClipAnimation clip in clips)
        {
            clip.loopTime = true;
            clip.loopPose = true;
        }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    private static Material CreateOrUpdateMaterial()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(MaterialPath)) ?? string.Empty);

        Material importedMaterial = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Material>().FirstOrDefault();
        Texture baseTexture = importedMaterial != null && importedMaterial.HasProperty("_BaseMap")
            ? importedMaterial.GetTexture("_BaseMap")
            : importedMaterial?.mainTexture;
        Texture normalTexture = importedMaterial != null && importedMaterial.HasProperty("_BumpMap")
            ? importedMaterial.GetTexture("_BumpMap")
            : null;

        Texture2D[] extractedTextures = AssetDatabase.FindAssets("t:Texture2D", new[] { TextureFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<Texture2D>)
            .Where(texture => texture != null)
            .ToArray();
        baseTexture ??= extractedTextures.FirstOrDefault(texture =>
            !texture.name.ToLowerInvariant().Contains("normal"));
        normalTexture ??= extractedTextures.FirstOrDefault(texture =>
            texture.name.ToLowerInvariant().Contains("normal"));

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            throw new System.InvalidOperationException("URP Lit shader was not found.");

        if (material == null)
        {
            material = new Material(shader) { name = "AirSquat" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        if (baseTexture != null)
            material.SetTexture("_BaseMap", baseTexture);
        if (normalTexture != null)
        {
            material.SetTexture("_BumpMap", normalTexture);
            material.EnableKeyword("_NORMALMAP");
        }
        material.SetColor("_BaseColor", Color.white);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ExtractAndConfigureTextures()
    {
        Directory.CreateDirectory(Path.GetFullPath(TextureFolder));
        var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        importer?.ExtractTextures(TextureFolder);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TextureFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            if (textureImporter == null)
                continue;

            textureImporter.maxTextureSize = 2048;
            textureImporter.textureCompression = TextureImporterCompression.CompressedHQ;
            if (Path.GetFileNameWithoutExtension(path).ToLowerInvariant().Contains("normal"))
                textureImporter.textureType = TextureImporterType.NormalMap;
            textureImporter.SaveAndReimport();
        }
    }

    private static AnimatorController CreateOrUpdateController(AnimationClip clip)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState state = stateMachine.states
            .Select(child => child.state)
            .FirstOrDefault(candidate => candidate.name == "MovementClip");
        if (state == null)
            state = stateMachine.AddState("MovementClip");

        state.motion = clip;
        stateMachine.defaultState = state;
        EditorUtility.SetDirty(state);
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static GameObject CreateOrUpdatePrefab(
        GameObject modelAsset,
        Material material,
        RuntimeAnimatorController controller)
    {
        var wrapper = new GameObject("AirSquat");
        GameObject model = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
        if (model == null)
            model = Object.Instantiate(modelAsset);

        model.name = "Model";
        model.transform.SetParent(wrapper.transform, false);
        model.transform.localPosition = new Vector3(0f, -0.04f, 0f);
        model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        model.transform.localScale = Vector3.one * 0.045f;

        Animator animator = model.GetComponentInChildren<Animator>();
        if (animator == null)
            animator = model.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
                materials[i] = material;
            renderer.sharedMaterials = materials;
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(wrapper, PrefabPath);
        Object.DestroyImmediate(wrapper);
        return prefab;
    }

    private static void SavePreview(
        GameObject instance,
        AnimationClip clip,
        float time,
        Camera camera,
        string outputPath)
    {
        Animator animator = instance.GetComponentInChildren<Animator>();
        if (animator == null)
            throw new System.InvalidOperationException("Air Squat prefab has no Animator.");
        clip.SampleAnimation(animator.gameObject, time);

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new System.InvalidOperationException("Air Squat prefab has no renderer.");

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1))
            bounds.Encapsulate(renderer.bounds);

        float distance = Mathf.Max(bounds.size.y * 2.2f, 0.75f);
        camera.transform.position = bounds.center + Vector3.back * distance;
        camera.transform.LookAt(bounds.center);
        Debug.Log($"[AirSquatSetup] Preview t={time:F2}s bounds center={bounds.center}, " +
                  $"size={bounds.size}, camera={camera.transform.position}.");

        var renderTexture = new RenderTexture(360, 800, 24);
        camera.targetTexture = renderTexture;
        camera.Render();
        camera.Render();

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;
        var texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();
        File.WriteAllBytes(outputPath, texture.EncodeToPNG());

        Object.DestroyImmediate(texture);
        RenderTexture.active = previous;
        camera.targetTexture = null;
        renderTexture.Release();
        Object.DestroyImmediate(renderTexture);
    }
}
