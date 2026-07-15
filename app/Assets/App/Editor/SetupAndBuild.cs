using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.ARFoundation;
using GerakAR.Core;
using GerakAR.AR;
using GerakAR.Animation;
using GerakAR.UI;
using GerakAR.Audio;
using GerakAR.Content;
using System.Collections.Generic;
using UnityEditor.Build;

public static class SetupAndBuild
{
    private static readonly Color ColorCleanOffWhite = new Color(0.98f, 0.976f, 0.965f, 1f); // #FAF9F6
    private static readonly Color ColorDeepForest = new Color(0.07f, 0.216f, 0.165f, 1f);   // #12372A
    private static readonly Color ColorForestGreen = new Color(0.12f, 0.365f, 0.259f, 1f);  // #1F5D42
    private static readonly Color ColorSoftSage = new Color(0.66f, 0.745f, 0.635f, 1f);     // #A9BEA2
    private static readonly Color ColorCharcoal = new Color(0.125f, 0.149f, 0.125f, 1f);    // #202620

    [MenuItem("GerakAR/Setup Scenes and Build APK")]
    public static void ExecuteSetupAndBuild()
    {
        Debug.Log("[GerakAR] Memulai setup scene...");
        CreateBootstrapScene();
        CreateMainARScene();
        UpdateBuildSettings();
        BuildAPK();
    }

    private static void CreateBootstrapScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Bootstrap";

        // Create Managers GameObject
        var managersGo = new GameObject("Managers");
        var stateMgr = managersGo.AddComponent<AppStateManager>();
        var permissionController = managersGo.AddComponent<PermissionController>();
        var arChecker = managersGo.AddComponent<ARAvailabilityChecker>();
        var onboardingController = managersGo.AddComponent<OnboardingController>();

        // Create Camera
        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Color;
        cam.backgroundColor = ColorCleanOffWhite;
        camGo.tag = "MainCamera";

        // UI Canvas
        var canvasGo = new GameObject("Canvas", typeof(RectTransform));
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        // Event System
        var eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // 1. Intro Panel
        var introGo = CreateUIObject("IntroPanel", canvasGo);
        var introImg = introGo.AddComponent<Image>();
        introImg.color = ColorCleanOffWhite;
        StretchRect(introGo.GetComponent<RectTransform>());
        var introCanvasGroup = introGo.AddComponent<CanvasGroup>();
        var introController = managersGo.AddComponent<IntroController>();

        // Load premium UI assets
        var interFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/MobileARTemplateAssets/UI/Fonts/Inter-Regular_SDF.asset");
        var btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MobileARTemplateAssets/UI/Sprites/ActivationButtonOpaque.png");

        // Intro Logo / Title Text
        var titleGo = CreateUIObject("TitleText", introGo);
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.text = "GerakAR";
        titleText.fontSize = 54;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = ColorDeepForest;
        titleText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) titleText.font = interFont;
        SetCenterPosition(titleGo.GetComponent<RectTransform>(), 0f, 0f, 300f, 100f);

        // 2. Onboarding Panel
        var onboardGo = CreateUIObject("OnboardingPanel", canvasGo);
        var onboardImg = onboardGo.AddComponent<Image>();
        onboardImg.color = ColorCleanOffWhite;
        StretchRect(onboardGo.GetComponent<RectTransform>());
        onboardGo.SetActive(false);

        // Instruction Card container (white box with rounded corners)
        var cardGo = CreateUIObject("InstructionCard", onboardGo);
        var cardImg = cardGo.AddComponent<Image>();
        cardImg.sprite = btnSprite;
        cardImg.type = Image.Type.Sliced;
        cardImg.color = Color.white;
        SetCenterPosition(cardGo.GetComponent<RectTransform>(), 0f, 40f, 460f, 320f);

        // Onboarding Title (inside the card)
        var obTitleGo = CreateUIObject("OnboardingTitle", cardGo);
        var obTitle = obTitleGo.AddComponent<TextMeshProUGUI>();
        obTitle.text = "Sebelum Mulai";
        obTitle.fontSize = 30;
        obTitle.fontStyle = FontStyles.Bold;
        obTitle.color = ColorDeepForest;
        obTitle.alignment = TextAlignmentOptions.Center;
        if (interFont != null) obTitle.font = interFont;
        SetCenterPosition(obTitleGo.GetComponent<RectTransform>(), 0f, 100f, 400f, 50f);

        // Onboarding Instructions Text (inside the card)
        var obTextGo = CreateUIObject("OnboardingText", cardGo);
        var obText = obTextGo.AddComponent<TextMeshProUGUI>();
        obText.text = "• Gunakan di tempat yang cukup luas.\n\n• Minta guru atau orang tua mendampingi.\n\n• Izinkan kamera untuk melihat gerakan.";
        obText.fontSize = 16;
        obText.color = ColorCharcoal;
        obText.alignment = TextAlignmentOptions.Left;
        if (interFont != null) obText.font = interFont;
        SetCenterPosition(obTextGo.GetComponent<RectTransform>(), 0f, -30f, 400f, 180f);

        // Onboarding Start Button (below the card)
        var startBtnGo = CreateUIObject("MulaiButton", onboardGo);
        var startBtnImg = startBtnGo.AddComponent<Image>();
        startBtnImg.sprite = btnSprite;
        startBtnImg.type = Image.Type.Sliced;
        startBtnImg.color = ColorDeepForest;
        var startBtn = startBtnGo.AddComponent<Button>();
        SetCenterPosition(startBtnGo.GetComponent<RectTransform>(), 0f, -180f, 220f, 55f);

        var btnTextGo = CreateUIObject("Text", startBtnGo);
        var btnText = btnTextGo.AddComponent<TextMeshProUGUI>();
        btnText.text = "MULAI";
        btnText.fontSize = 18;
        btnText.fontStyle = FontStyles.Bold;
        btnText.color = Color.white;
        btnText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) btnText.font = interFont;
        StretchRect(btnTextGo.GetComponent<RectTransform>());

        // 3. Unsupported Panel
        var unsupGo = CreateUIObject("UnsupportedPanel", canvasGo);
        var unsupImg = unsupGo.AddComponent<Image>();
        unsupImg.color = ColorCleanOffWhite;
        StretchRect(unsupGo.GetComponent<RectTransform>());
        unsupGo.SetActive(false);

        var unsupTextGo = CreateUIObject("UnsupportedText", unsupGo);
        var unsupText = unsupTextGo.AddComponent<TextMeshProUGUI>();
        unsupText.text = "HP Anda tidak mendukung AR.\nAplikasi akan berjalan dalam Mode 3D.";
        unsupText.fontSize = 18;
        unsupText.color = ColorCharcoal;
        unsupText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) unsupText.font = interFont;
        SetCenterPosition(unsupTextGo.GetComponent<RectTransform>(), 0f, 0f, 500f, 200f);

        // Link script fields
        var serialIntro = new SerializedObject(introController);
        serialIntro.FindProperty("introCanvasGroup").objectReferenceValue = introCanvasGroup;
        serialIntro.ApplyModifiedProperties();

        var serialOnboard = new SerializedObject(onboardingController);
        serialOnboard.FindProperty("onboardingPanel").objectReferenceValue = onboardGo;
        serialOnboard.ApplyModifiedProperties();

        var serialChecker = new SerializedObject(arChecker);
        serialChecker.FindProperty("unsupportedPanel").objectReferenceValue = unsupGo;
        serialChecker.FindProperty("unsupportedMessageText").objectReferenceValue = unsupText;
        serialChecker.ApplyModifiedProperties();

        // Wire button click
        UnityEditor.Events.UnityEventTools.AddPersistentListener(startBtn.onClick, onboardingController.OnMulaiPressed);

        EditorSceneManager.SaveScene(scene, "Assets/App/Scenes/Bootstrap.unity");
        Debug.Log("[GerakAR] Scene Bootstrap selesai dibuat.");
    }

    private static void CreateMainARScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MainAR";

        // XR Origin & Camera setup
        var originGo = new GameObject("XR Origin");
        var origin = originGo.AddComponent<Unity.XR.CoreUtils.XROrigin>();
        var camGo = new GameObject("AR Camera");
        camGo.transform.SetParent(originGo.transform, false);
        var cam = camGo.AddComponent<Camera>();
        camGo.AddComponent<ARCameraManager>();
        camGo.AddComponent<ARCameraBackground>();
        camGo.tag = "MainCamera";
        origin.Camera = cam;

        // AR Session
        var sessionGo = new GameObject("AR Session");
        sessionGo.AddComponent<ARSession>();

        // ModelRoot GameObject
        var modelRootGo = new GameObject("ModelRoot");

        // Load Database Asset
        var database = AssetDatabase.LoadAssetAtPath<MovementDatabase>("Assets/App/Content/MovementData/MovementDatabase.asset");

        // Managers
        var managersGo = new GameObject("Managers");
        var stateMgr = managersGo.AddComponent<AppStateManager>();
        var modelPool = managersGo.AddComponent<ModelPool>();
        var trackingController = managersGo.AddComponent<ARImageTrackingController>();
        var movementController = managersGo.AddComponent<MovementController>();
        var audioGuideController = managersGo.AddComponent<AudioGuideController>();

        // AR Tracked Image Manager (on XR Origin)
        var trackedImgMgr = originGo.AddComponent<ARTrackedImageManager>();

        // Configure ModelPool
        var serialPool = new SerializedObject(modelPool);
        serialPool.FindProperty("modelRoot").objectReferenceValue = modelRootGo.transform;
        serialPool.ApplyModifiedProperties();

        // Configure tracking controller
        var serialTracking = new SerializedObject(trackingController);
        serialTracking.FindProperty("movementDatabase").objectReferenceValue = database;
        serialTracking.FindProperty("modelPool").objectReferenceValue = modelPool;
        serialTracking.ApplyModifiedProperties();

        // Configure Audio Controller
        var serialAudio = new SerializedObject(audioGuideController);
        serialAudio.FindProperty("movementDatabase").objectReferenceValue = database;
        serialAudio.ApplyModifiedProperties();

        // Canvas
        var canvasGo = new GameObject("UI Canvas", typeof(RectTransform));
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        // Event System
        var eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // Load premium UI assets
        var interFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/MobileARTemplateAssets/UI/Fonts/Inter-Regular_SDF.asset");
        var btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MobileARTemplateAssets/UI/Sprites/ActivationButtonOpaque.png");
        var frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MobileARTemplateAssets/UI/Sprites/MixedCorners.png");
        var roundTopSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MobileARTemplateAssets/UI/Sprites/RoundRadius_10_Top.png");

        var arUI = canvasGo.AddComponent<ARUIController>();

        // 1. Scan Overlay Panel
        var scanGo = CreateUIObject("ScanOverlay", canvasGo);
        StretchRect(scanGo.GetComponent<RectTransform>());

        // Corner Frame Image (Corner Only focuses)
        var scanFrameGo = CreateUIObject("ScanFrame", scanGo);
        var frameImg = scanFrameGo.AddComponent<Image>();
        frameImg.sprite = frameSprite;
        frameImg.type = Image.Type.Simple;
        frameImg.color = Color.white;
        SetCenterPosition(scanFrameGo.GetComponent<RectTransform>(), 0f, 0f, 220f, 220f);

        // Hint Text
        var hintGo = CreateUIObject("HintText", scanGo);
        var hintText = hintGo.AddComponent<TextMeshProUGUI>();
        hintText.text = "Arahkan kamera ke gambar gerakan";
        hintText.fontSize = 18;
        hintText.fontStyle = FontStyles.Bold;
        hintText.color = Color.white;
        hintText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) hintText.font = interFont;
        SetCenterPosition(hintGo.GetComponent<RectTransform>(), 0f, -160f, 450f, 50f);

        // 2. Detection Toast Panel
        var toastGo = CreateUIObject("DetectionToast", canvasGo);
        var toastImg = toastGo.AddComponent<Image>();
        toastImg.sprite = btnSprite;
        toastImg.type = Image.Type.Sliced;
        toastImg.color = ColorCleanOffWhite;
        SetCenterPosition(toastGo.GetComponent<RectTransform>(), 0f, 50f, 280f, 80f);

        var toastTextGo = CreateUIObject("Text", toastGo);
        var toastText = toastTextGo.AddComponent<TextMeshProUGUI>();
        toastText.text = "✔ Gambar Terdeteksi";
        toastText.fontSize = 18;
        toastText.fontStyle = FontStyles.Bold;
        toastText.color = ColorForestGreen;
        toastText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) toastText.font = interFont;
        StretchRect(toastTextGo.GetComponent<RectTransform>());
        toastGo.SetActive(false);

        // 3. AR Controls Panel (Group label + timeline + FABs)
        var arControlsGo = CreateUIObject("ARControls", canvasGo);
        StretchRect(arControlsGo.GetComponent<RectTransform>());
        arControlsGo.SetActive(false);

        // Name Label
        var nameLabelGo = CreateUIObject("MovementNameLabel", arControlsGo);
        var nameLabel = nameLabelGo.AddComponent<TextMeshProUGUI>();
        nameLabel.text = "SQUAT";
        nameLabel.fontSize = 26;
        nameLabel.fontStyle = FontStyles.Bold;
        nameLabel.color = ColorDeepForest;
        nameLabel.alignment = TextAlignmentOptions.Center;
        if (interFont != null) nameLabel.font = interFont;
        SetCenterPosition(nameLabelGo.GetComponent<RectTransform>(), 0f, 300f, 300f, 60f);

        // Close Button FAB
        var closeGo = CreateUIObject("CloseButton", arControlsGo);
        var closeImg = closeGo.AddComponent<Image>();
        closeImg.sprite = btnSprite;
        closeImg.type = Image.Type.Sliced;
        closeImg.color = Color.white;
        var closeBtn = closeGo.AddComponent<Button>();
        SetAnchorRight(closeGo.GetComponent<RectTransform>(), -30f, 150f, 54f, 54f);

        // Close Icon Text inside FAB
        var closeTextGo = CreateUIObject("Text", closeGo);
        var closeText = closeTextGo.AddComponent<TextMeshProUGUI>();
        closeText.text = "✕";
        closeText.fontSize = 20;
        closeText.color = ColorDeepForest;
        closeText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) closeText.font = interFont;
        StretchRect(closeTextGo.GetComponent<RectTransform>());

        // Material Button FAB
        var matBtnGo = CreateUIObject("MaterialButton", arControlsGo);
        var matBtnImg = matBtnGo.AddComponent<Image>();
        matBtnImg.sprite = btnSprite;
        matBtnImg.type = Image.Type.Sliced;
        matBtnImg.color = ColorDeepForest;
        var matBtn = matBtnGo.AddComponent<Button>();
        SetAnchorRight(matBtnGo.GetComponent<RectTransform>(), -30f, 80f, 54f, 54f);

        // Material Button Icon / Text inside FAB
        var matTextGo = CreateUIObject("Text", matBtnGo);
        var matText = matTextGo.AddComponent<TextMeshProUGUI>();
        matText.text = "Materi";
        matText.fontSize = 11;
        matText.fontStyle = FontStyles.Bold;
        matText.color = Color.white;
        matText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) matText.font = interFont;
        StretchRect(matTextGo.GetComponent<RectTransform>());

        // Timeline
        var timelineRootGo = CreateUIObject("Timeline", arControlsGo);
        SetCenterPosition(timelineRootGo.GetComponent<RectTransform>(), 0f, -300f, 500f, 80f);

        // Slider component
        var sliderGo = CreateUIObject("Slider", timelineRootGo);
        var slider = sliderGo.AddComponent<Slider>();
        StretchRect(sliderGo.GetComponent<RectTransform>());

        // Play/Pause Button
        var playPauseGo = CreateUIObject("PlayPauseButton", timelineRootGo);
        var playPauseImg = playPauseGo.AddComponent<Image>();
        playPauseImg.sprite = btnSprite;
        playPauseImg.type = Image.Type.Sliced;
        playPauseImg.color = Color.white;
        var playPauseBtn = playPauseGo.AddComponent<Button>();
        SetAnchorLeft(playPauseGo.GetComponent<RectTransform>(), -40f, 0f, 36f, 36f);

        // Play/Pause Icon Text inside button
        var playPauseTextGo = CreateUIObject("Text", playPauseGo);
        var playPauseText = playPauseTextGo.AddComponent<TextMeshProUGUI>();
        playPauseText.text = "⏸";
        playPauseText.fontSize = 16;
        playPauseText.color = ColorDeepForest;
        playPauseText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) playPauseText.font = interFont;
        StretchRect(playPauseTextGo.GetComponent<RectTransform>());

        // Timeline Controller setup
        var timelineCtrl = timelineRootGo.AddComponent<PoseTimelineController>();
        var serialTimeline = new SerializedObject(timelineCtrl);
        serialTimeline.FindProperty("timelineSlider").objectReferenceValue = slider;
        serialTimeline.FindProperty("movementController").objectReferenceValue = movementController;
        serialTimeline.ApplyModifiedProperties();

        // 4. Bottom Sheet Panel
        var sheetGo = CreateUIObject("BottomSheet", canvasGo);
        var sheetImg = sheetGo.AddComponent<Image>();
        sheetImg.sprite = roundTopSprite;
        sheetImg.type = Image.Type.Sliced;
        sheetImg.color = ColorCleanOffWhite;
        var sheetRT = sheetGo.GetComponent<RectTransform>();
        sheetRT.anchorMin = new Vector2(0f, 0f);
        sheetRT.anchorMax = new Vector2(1f, 0f);
        sheetRT.pivot = new Vector2(0.5f, 0f);
        sheetRT.anchoredPosition = new Vector2(0f, -Screen.height);
        sheetRT.sizeDelta = new Vector2(0f, Screen.height * 0.92f);

        // Grab Handle
        var handleGo = CreateUIObject("GrabHandle", sheetGo);
        var handleImg = handleGo.AddComponent<Image>();
        handleImg.sprite = btnSprite;
        handleImg.type = Image.Type.Sliced;
        handleImg.color = ColorSoftSage;
        SetCenterPosition(handleGo.GetComponent<RectTransform>(), 0f, sheetRT.sizeDelta.y - 15f, 80f, 8f);

        // Sheet Header Type (Utama / Tambahan)
        var categoryGo = CreateUIObject("CategoryTypeLabel", sheetGo);
        var categoryTxt = categoryGo.AddComponent<TextMeshProUGUI>();
        categoryTxt.text = "Gerakan Utama";
        categoryTxt.fontSize = 16;
        categoryTxt.fontStyle = FontStyles.Bold;
        categoryTxt.color = ColorForestGreen;
        if (interFont != null) categoryTxt.font = interFont;
        SetCenterPosition(categoryGo.GetComponent<RectTransform>(), -120f, sheetRT.sizeDelta.y - 60f, 200f, 30f);

        // Back to primary button
        var backBtnGo = CreateUIObject("BackToPrimaryButton", sheetGo);
        var backBtnImg = backBtnGo.AddComponent<Image>();
        backBtnImg.sprite = btnSprite;
        backBtnImg.type = Image.Type.Sliced;
        backBtnImg.color = ColorDeepForest;
        var backBtn = backBtnGo.AddComponent<Button>();
        SetCenterPosition(backBtnGo.GetComponent<RectTransform>(), 150f, sheetRT.sizeDelta.y - 60f, 120f, 35f);

        // Back Button Text
        var backBtnTextGo = CreateUIObject("Text", backBtnGo);
        var backBtnText = backBtnTextGo.AddComponent<TextMeshProUGUI>();
        backBtnText.text = "Kembali";
        backBtnText.fontSize = 14;
        backBtnText.fontStyle = FontStyles.Bold;
        backBtnText.color = Color.white;
        backBtnText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) backBtnText.font = interFont;
        StretchRect(backBtnTextGo.GetComponent<RectTransform>());

        // Sheet Controller & Scrim
        var scrimGo = CreateUIObject("Scrim", canvasGo);
        var scrimImg = scrimGo.AddComponent<Image>();
        scrimImg.color = new Color(0, 0, 0, 0f);
        StretchRect(scrimGo.GetComponent<RectTransform>());
        scrimGo.AddComponent<Button>(); // Tap triggers close
        scrimGo.SetActive(false);

        // Move bottom sheet to front
        sheetGo.transform.SetAsLastSibling();

        var sheetCtrl = sheetGo.AddComponent<BottomSheetController>();
        var serialSheet = new SerializedObject(sheetCtrl);
        serialSheet.FindProperty("sheetRect").objectReferenceValue = sheetRT;
        serialSheet.FindProperty("scrim").objectReferenceValue = scrimGo;
        serialSheet.FindProperty("movementController").objectReferenceValue = movementController;
        serialSheet.ApplyModifiedProperties();

        // Material content controller
        var matCtrl = sheetGo.AddComponent<MaterialContentController>();
        var serialMat = new SerializedObject(matCtrl);
        serialMat.FindProperty("categoryTypeLabel").objectReferenceValue = categoryTxt;
        serialMat.FindProperty("movementNameText").objectReferenceValue = nameLabel;
        serialMat.FindProperty("backToPrimaryButton").objectReferenceValue = backBtn;
        serialMat.ApplyModifiedProperties();

        // Link ARUIController
        var serialUI = new SerializedObject(arUI);
        serialUI.FindProperty("scanOverlay").objectReferenceValue = scanGo;
        serialUI.FindProperty("detectionToast").objectReferenceValue = toastGo;
        serialUI.FindProperty("arControls").objectReferenceValue = arControlsGo;
        serialUI.FindProperty("movementNameLabel").objectReferenceValue = nameLabel;
        serialUI.FindProperty("closeButton").objectReferenceValue = closeBtn;
        serialUI.FindProperty("materialButton").objectReferenceValue = matBtn;
        serialUI.FindProperty("timelineRoot").objectReferenceValue = timelineRootGo;
        serialUI.FindProperty("playPauseButton").objectReferenceValue = playPauseBtn;
        serialUI.FindProperty("playPauseIcon").objectReferenceValue = playPauseImg;
        serialUI.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, "Assets/App/Scenes/MainAR.unity");
        Debug.Log("[GerakAR] Scene MainAR selesai dibuat.");
    }

    private static void UpdateBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene("Assets/App/Scenes/Bootstrap.unity", true),
            new EditorBuildSettingsScene("Assets/App/Scenes/MainAR.unity", true)
        };
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[GerakAR] Build Settings scenes updated.");
    }

    private static void BuildAPK()
    {
        Debug.Log("[GerakAR] Menjalankan build Android APK...");
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        
        string targetPath = "Builds/GerakAR.apk";
        System.IO.Directory.CreateDirectory("Builds");

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = new[] {
                "Assets/App/Scenes/Bootstrap.unity",
                "Assets/App/Scenes/MainAR.unity"
            },
            locationPathName = targetPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[GerakAR] Build berhasil! File APK: {targetPath} ({summary.totalSize} bytes)");
        }
        else
        {
            Debug.LogError($"[GerakAR] Build gagal dengan status: {summary.result}");
        }
    }

    // ── UI layout helper functions ────────────────────────────────────

    private static void StretchRect(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    private static void SetCenterPosition(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    private static void SetAnchorRight(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(1f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    private static void SetAnchorLeft(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    private static GameObject CreateUIObject(string name, GameObject parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        if (parent != null)
            go.transform.SetParent(parent.transform, false);
        return go;
    }
}
