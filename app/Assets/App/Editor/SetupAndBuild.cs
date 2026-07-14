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
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();
        var canvasGroup = canvasGo.AddComponent<CanvasGroup>();

        // 1. Intro Panel
        var introGo = new GameObject("IntroPanel");
        introGo.transform.SetParent(canvasGo.transform, false);
        var introImg = introGo.AddComponent<Image>();
        introImg.color = ColorCleanOffWhite;
        StretchRect(introGo.GetComponent<RectTransform>());
        var introController = managersGo.AddComponent<IntroController>();

        // Intro Logo / Title Text
        var titleGo = new GameObject("TitleText");
        titleGo.transform.SetParent(introGo.transform, false);
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.text = "GerakAR";
        titleText.fontSize = 48;
        titleText.color = ColorDeepForest;
        titleText.alignment = TextAlignmentOptions.Center;
        SetCenterPosition(titleGo.GetComponent<RectTransform>(), 0f, 0f, 300f, 100f);

        // 2. Onboarding Panel
        var onboardGo = new GameObject("OnboardingPanel");
        onboardGo.transform.SetParent(canvasGo.transform, false);
        var onboardImg = onboardGo.AddComponent<Image>();
        onboardImg.color = ColorCleanOffWhite;
        StretchRect(onboardGo.GetComponent<RectTransform>());
        onboardGo.SetActive(false);

        // Onboarding Title
        var obTitleGo = new GameObject("OnboardingTitle");
        obTitleGo.transform.SetParent(onboardGo.transform, false);
        var obTitle = obTitleGo.AddComponent<TextMeshProUGUI>();
        obTitle.text = "Sebelum Mulai";
        obTitle.fontSize = 32;
        obTitle.color = ColorDeepForest;
        obTitle.alignment = TextAlignmentOptions.Center;
        SetCenterPosition(obTitleGo.GetComponent<RectTransform>(), 0f, 150f, 400f, 60f);

        // Onboarding Instructions Text
        var obTextGo = new GameObject("OnboardingText");
        obTextGo.transform.SetParent(onboardGo.transform, false);
        var obText = obTextGo.AddComponent<TextMeshProUGUI>();
        obText.text = "• Gunakan di tempat yang cukup luas.\n• Minta guru atau orang tua mendampingi.\n• Izinkan kamera untuk melihat gerakan.";
        obText.fontSize = 18;
        obText.color = ColorCharcoal;
        obText.alignment = TextAlignmentOptions.Left;
        SetCenterPosition(obTextGo.GetComponent<RectTransform>(), 0f, 0f, 500f, 180f);

        // Onboarding Start Button
        var startBtnGo = new GameObject("MulaiButton");
        startBtnGo.transform.SetParent(onboardGo.transform, false);
        var startBtnImg = startBtnGo.AddComponent<Image>();
        startBtnImg.color = ColorDeepForest;
        var startBtn = startBtnGo.AddComponent<Button>();
        SetCenterPosition(startBtnGo.GetComponent<RectTransform>(), 0f, -150f, 200f, 60f);

        var btnTextGo = new GameObject("Text");
        btnTextGo.transform.SetParent(startBtnGo.transform, false);
        var btnText = btnTextGo.AddComponent<TextMeshProUGUI>();
        btnText.text = "MULAI";
        btnText.fontSize = 20;
        btnText.color = Color.white;
        btnText.alignment = TextAlignmentOptions.Center;
        StretchRect(btnTextGo.GetComponent<RectTransform>());

        // 3. Unsupported Panel
        var unsupGo = new GameObject("UnsupportedPanel");
        unsupGo.transform.SetParent(canvasGo.transform, false);
        var unsupImg = unsupGo.AddComponent<Image>();
        unsupImg.color = ColorCleanOffWhite;
        StretchRect(unsupGo.GetComponent<RectTransform>());
        unsupGo.SetActive(false);

        var unsupTextGo = new GameObject("UnsupportedText");
        unsupTextGo.transform.SetParent(unsupGo.transform, false);
        var unsupText = unsupTextGo.AddComponent<TextMeshProUGUI>();
        unsupText.fontSize = 20;
        unsupText.color = ColorCharcoal;
        unsupText.alignment = TextAlignmentOptions.Center;
        SetCenterPosition(unsupTextGo.GetComponent<RectTransform>(), 0f, 0f, 500f, 200f);

        // Link script fields
        var serialIntro = new SerializedObject(introController);
        serialIntro.FindProperty("introCanvasGroup").objectReferenceValue = canvasGroup;
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
        var canvasGo = new GameObject("UI Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        var arUI = canvasGo.AddComponent<ARUIController>();

        // 1. Scan Overlay Panel
        var scanGo = new GameObject("ScanOverlay");
        scanGo.transform.SetParent(canvasGo.transform, false);
        StretchRect(scanGo.GetComponent<RectTransform>());

        // Corner Frame Image (Corner Only focuses)
        var scanFrameGo = new GameObject("ScanFrame");
        scanFrameGo.transform.SetParent(scanGo.transform, false);
        var frameImg = scanFrameGo.AddComponent<Image>();
        frameImg.color = Color.white; // Assign outline or transparent sprite here
        SetCenterPosition(scanFrameGo.GetComponent<RectTransform>(), 0f, 0f, 220f, 220f);

        // Hint Text
        var hintGo = new GameObject("HintText");
        hintGo.transform.SetParent(scanGo.transform, false);
        var hintText = hintGo.AddComponent<TextMeshProUGUI>();
        hintText.text = "Arahkan kamera ke gambar gerakan";
        hintText.fontSize = 16;
        hintText.color = Color.white;
        hintText.alignment = TextAlignmentOptions.Center;
        SetCenterPosition(hintGo.GetComponent<RectTransform>(), 0f, -160f, 400f, 50f);

        // 2. Detection Toast Panel
        var toastGo = new GameObject("DetectionToast");
        toastGo.transform.SetParent(canvasGo.transform, false);
        var toastImg = toastGo.AddComponent<Image>();
        toastImg.color = ColorCleanOffWhite;
        SetCenterPosition(toastGo.GetComponent<RectTransform>(), 0f, 50f, 280f, 80f);

        var toastTextGo = new GameObject("Text");
        toastTextGo.transform.SetParent(toastGo.transform, false);
        var toastText = toastTextGo.AddComponent<TextMeshProUGUI>();
        toastText.text = "✔ Gambar Terdeteksi";
        toastText.fontSize = 18;
        toastText.color = ColorForestGreen;
        toastText.alignment = TextAlignmentOptions.Center;
        StretchRect(toastTextGo.GetComponent<RectTransform>());
        toastGo.SetActive(false);

        // 3. AR Controls Panel (Group label + timeline + FABs)
        var arControlsGo = new GameObject("ARControls");
        arControlsGo.transform.SetParent(canvasGo.transform, false);
        StretchRect(arControlsGo.GetComponent<RectTransform>());
        arControlsGo.SetActive(false);

        // Name Label
        var nameLabelGo = new GameObject("MovementNameLabel");
        nameLabelGo.transform.SetParent(arControlsGo.transform, false);
        var nameLabel = nameLabelGo.AddComponent<TextMeshProUGUI>();
        nameLabel.text = "SQUAT";
        nameLabel.fontSize = 24;
        nameLabel.color = ColorDeepForest;
        nameLabel.alignment = TextAlignmentOptions.Center;
        SetCenterPosition(nameLabelGo.GetComponent<RectTransform>(), 0f, 300f, 300f, 60f);

        // Close Button FAB
        var closeGo = new GameObject("CloseButton");
        closeGo.transform.SetParent(arControlsGo.transform, false);
        var closeImg = closeGo.AddComponent<Image>();
        closeImg.color = Color.white;
        var closeBtn = closeGo.AddComponent<Button>();
        SetAnchorRight(closeGo.GetComponent<RectTransform>(), -30f, 150f, 54f, 54f);

        // Material Button FAB
        var matBtnGo = new GameObject("MaterialButton");
        matBtnGo.transform.SetParent(arControlsGo.transform, false);
        var matBtnImg = matBtnGo.AddComponent<Image>();
        matBtnImg.color = ColorDeepForest;
        var matBtn = matBtnGo.AddComponent<Button>();
        SetAnchorRight(matBtnGo.GetComponent<RectTransform>(), -30f, 80f, 54f, 54f);

        // Timeline
        var timelineRootGo = new GameObject("Timeline");
        timelineRootGo.transform.SetParent(arControlsGo.transform, false);
        SetCenterPosition(timelineRootGo.GetComponent<RectTransform>(), 0f, -300f, 500f, 80f);

        // Slider component
        var sliderGo = new GameObject("Slider");
        sliderGo.transform.SetParent(timelineRootGo.transform, false);
        var slider = sliderGo.AddComponent<Slider>();
        StretchRect(sliderGo.GetComponent<RectTransform>());

        // Play/Pause Button
        var playPauseGo = new GameObject("PlayPauseButton");
        playPauseGo.transform.SetParent(timelineRootGo.transform, false);
        var playPauseImg = playPauseGo.AddComponent<Image>();
        playPauseImg.color = Color.white;
        var playPauseBtn = playPauseGo.AddComponent<Button>();
        SetAnchorLeft(playPauseGo.GetComponent<RectTransform>(), -40f, 0f, 36f, 36f);

        // Timeline Controller setup
        var timelineCtrl = timelineRootGo.AddComponent<PoseTimelineController>();
        var serialTimeline = new SerializedObject(timelineCtrl);
        serialTimeline.FindProperty("timelineSlider").objectReferenceValue = slider;
        serialTimeline.FindProperty("movementController").objectReferenceValue = movementController;
        serialTimeline.ApplyModifiedProperties();

        // 4. Bottom Sheet Panel
        var sheetGo = new GameObject("BottomSheet");
        sheetGo.transform.SetParent(canvasGo.transform, false);
        var sheetImg = sheetGo.AddComponent<Image>();
        sheetImg.color = ColorCleanOffWhite;
        var sheetRT = sheetGo.GetComponent<RectTransform>();
        sheetRT.anchorMin = new Vector2(0f, 0f);
        sheetRT.anchorMax = new Vector2(1f, 0f);
        sheetRT.pivot = new Vector2(0.5f, 0f);
        sheetRT.anchoredPosition = new Vector2(0f, -Screen.height);
        sheetRT.sizeDelta = new Vector2(0f, Screen.height * 0.92f);

        // Grab Handle
        var handleGo = new GameObject("GrabHandle");
        handleGo.transform.SetParent(sheetGo.transform, false);
        var handleImg = handleGo.AddComponent<Image>();
        handleImg.color = ColorSoftSage;
        SetCenterPosition(handleGo.GetComponent<RectTransform>(), 0f, sheetRT.sizeDelta.y - 15f, 80f, 60f);

        // Sheet Header Type (Utama / Tambahan)
        var categoryGo = new GameObject("CategoryTypeLabel");
        categoryGo.transform.SetParent(sheetGo.transform, false);
        var categoryTxt = categoryGo.AddComponent<TextMeshProUGUI>();
        categoryTxt.text = "Gerakan Utama";
        categoryTxt.fontSize = 14;
        categoryTxt.color = ColorSoftSage;
        SetCenterPosition(categoryGo.GetComponent<RectTransform>(), -150f, sheetRT.sizeDelta.y - 60f, 200f, 30f);

        // Back to primary button
        var backBtnGo = new GameObject("BackToPrimaryButton");
        backBtnGo.transform.SetParent(sheetGo.transform, false);
        var backBtnImg = backBtnGo.AddComponent<Image>();
        backBtnImg.color = ColorDeepForest;
        var backBtn = backBtnGo.AddComponent<Button>();
        SetCenterPosition(backBtnGo.GetComponent<RectTransform>(), 150f, sheetRT.sizeDelta.y - 60f, 120f, 35f);

        // Sheet Controller & Scrim
        var scrimGo = new GameObject("Scrim");
        scrimGo.transform.SetParent(canvasGo.transform, false);
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
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
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
}
