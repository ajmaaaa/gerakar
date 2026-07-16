using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GerakAR.Core;
using GerakAR.AR;
using GerakAR.Animation;
using GerakAR.UI;
using GerakAR.Audio;
using GerakAR.Content;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;

public static class SetupAndBuild
{
    private static readonly Color ColorCleanOffWhite = new Color(0.98f, 0.976f, 0.965f, 1f); // #FAF9F6
    private static readonly Color ColorDeepForest = new Color(0.07f, 0.216f, 0.165f, 1f);   // #12372A
    private static readonly Color ColorForestGreen = new Color(0.12f, 0.365f, 0.259f, 1f);  // #1F5D42
    private static readonly Color ColorSoftSage = new Color(0.66f, 0.745f, 0.635f, 1f);     // #A9BEA2
    private static readonly Color ColorCharcoal = new Color(0.125f, 0.149f, 0.125f, 1f);    // #202620
    private static readonly Color ColorMossGreen = new Color(0.376f, 0.490f, 0.310f, 1f);   // #607D4F

    [MenuItem("Build/Setup and Build APK")]
    public static void ExecuteSetupAndBuild()
    {
        ExecuteSetupOnly();
        BuildAPK();
    }

    [MenuItem("Build/Setup Scenes Only")]
    public static void ExecuteSetupOnly()
    {
        ConfigurePlayerSettings();
        Debug.Log("[GerakAR] Memulai setup scene...");
        
        // Generate dynamic UI prefabs for Bottom Sheet populating
        var interFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/MobileARTemplateAssets/UI/Fonts/Inter-Regular_SDF.asset");
        var btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MobileARTemplateAssets/UI/Sprites/ActivationButtonOpaque.png");
        GeneratePrefabs(interFont, btnSprite);

        CreateBootstrapScene();
        CreateMainARScene();
        UpdateBuildSettings();
        ValidateSetup();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void ConfigurePlayerSettings()
    {
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "id.ac.unp.gerakar");
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        PlayerSettings.Android.forceInternetPermission = false;

        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.allowDebugging = false;
    }

    private static void GeneratePrefabs(TMP_FontAsset interFont, Sprite btnSprite)
    {
        System.IO.Directory.CreateDirectory("Assets/App/Prefabs");

        // 1. StepItem prefab
        var stepGo = new GameObject("StepItem", typeof(RectTransform));
        var stepText = stepGo.AddComponent<TextMeshProUGUI>();
        stepText.enableWordWrapping = true;
        stepText.overflowMode = TMPro.TextOverflowModes.Overflow;
        stepText.fontSize = 8.0f;
        stepText.color = ColorCharcoal;
        if (interFont != null) stepText.font = interFont;
        stepGo.GetComponent<RectTransform>().sizeDelta = new Vector2(880f, 60f);
        PrefabUtility.SaveAsPrefabAsset(stepGo, "Assets/App/Prefabs/StepItem.prefab");
        Object.DestroyImmediate(stepGo);

        // 2. BulletItem prefab
        var bulletGo = new GameObject("BulletItem", typeof(RectTransform));
        var bulletText = bulletGo.AddComponent<TextMeshProUGUI>();
        bulletText.enableWordWrapping = true;
        bulletText.overflowMode = TMPro.TextOverflowModes.Overflow;
        bulletText.fontSize = 7.3f;
        bulletText.color = ColorCharcoal;
        if (interFont != null) bulletText.font = interFont;
        bulletGo.GetComponent<RectTransform>().sizeDelta = new Vector2(880f, 50f);
        PrefabUtility.SaveAsPrefabAsset(bulletGo, "Assets/App/Prefabs/BulletItem.prefab");
        Object.DestroyImmediate(bulletGo);

        // 3. RelatedCard prefab
        var cardGo = new GameObject("RelatedCard", typeof(RectTransform));
        var cardImg = cardGo.AddComponent<Image>();
        
        cardImg.sprite = btnSprite;
        cardImg.type = Image.Type.Sliced;
        cardImg.color = Color.white;
        cardGo.AddComponent<Button>();
        var cardRT = cardGo.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(240f, 180f);

        var thumbGo = CreateUIObject("Thumbnail", cardGo);
        var thumbImg = thumbGo.AddComponent<Image>();
        thumbImg.preserveAspect = true;
        thumbImg.color = ColorSoftSage;
        SetCenterPosition(thumbGo.GetComponent<RectTransform>(), 0f, 8.3f, 66.7f, 33.3f);

        var titleGo = CreateUIObject("Title", cardGo);
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.enableWordWrapping = true;
        titleText.overflowMode = TMPro.TextOverflowModes.Overflow;
        titleText.text = "Related";
        titleText.fontSize = 6.0f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = ColorCharcoal;
        titleText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) titleText.font = interFont;
        SetCenterPosition(titleGo.GetComponent<RectTransform>(), 0f, -16.7f, 66.7f, 13.3f);

        PrefabUtility.SaveAsPrefabAsset(cardGo, "Assets/App/Prefabs/RelatedCard.prefab");
        Object.DestroyImmediate(cardGo);
    }

    private static void CreateBootstrapScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Bootstrap";

        // Create Managers GameObject
        var managersGo = new GameObject("Managers");
        var permissionController = managersGo.AddComponent<PermissionController>();
        var arChecker = managersGo.AddComponent<ARAvailabilityChecker>();
        var onboardingController = managersGo.AddComponent<OnboardingController>();

        // Standalone AppStateManager (so it does not destroy scene-specific Managers via DontDestroyOnLoad)
        var stateMgrGo = new GameObject("AppStateManager");
        var stateMgr = stateMgrGo.AddComponent<AppStateManager>();

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
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(360f, 800f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Event System - pakai InputSystemUIInputModule karena project menggunakan New Input System
        var eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemGo.AddComponent<InputSystemUIInputModule>();

        // Load premium UI assets
        var interFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/MobileARTemplateAssets/UI/Fonts/Inter-Regular_SDF.asset");
        var btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MobileARTemplateAssets/UI/Sprites/ActivationButtonOpaque.png");

        // 1. Intro Panel (G01) - Hijau Tua Hutan
        var introGo = CreateUIObject("IntroPanel", canvasGo);
        var introImg = introGo.AddComponent<Image>();
        
        introImg.color = ColorDeepForest;
        StretchRect(introGo.GetComponent<RectTransform>());
        var introCanvasGroup = introGo.AddComponent<CanvasGroup>();
        introCanvasGroup.interactable = false;
        introCanvasGroup.blocksRaycasts = false;
        var introController = managersGo.AddComponent<IntroController>();

        // G01 Header Kiri - Media Pembelajaran Text
        var introMetaGo = CreateUIObject("MetadataText", introGo);
        var introMetaText = introMetaGo.AddComponent<TextMeshProUGUI>();
        introMetaText.enableWordWrapping = true;
        introMetaText.overflowMode = TMPro.TextOverflowModes.Overflow;
        introMetaText.text = "<b>Media Pembelajaran</b>\n<color=#A9BEA2>Skripsi Pendidikan SD</color>";
        introMetaText.fontSize = 12.0f;
        introMetaText.color = Color.white;
        introMetaText.alignment = TextAlignmentOptions.Left;
        if (interFont != null) introMetaText.font = interFont;
        SetAnchorTopLeft(introMetaGo.GetComponent<RectTransform>(), 20.0f, -48.0f, 160.0f, 33.3f);

        // G01 Header Kanan - Stylized UNP Logo Badge (Gold Circle with UNP text)
        var unpBadgeGo = CreateUIObject("UNPBadge", introGo);
        var unpBadgeImg = unpBadgeGo.AddComponent<Image>();
        
        unpBadgeImg.sprite = btnSprite;
        unpBadgeImg.type = Image.Type.Sliced;
        unpBadgeImg.color = new Color(0.957f, 0.729f, 0.094f, 1f); // Gold #F4BA18
        SetAnchorTopRight(unpBadgeGo.GetComponent<RectTransform>(), -20.0f, -48.0f, 33.3f, 33.3f);

        var unpTextGo = CreateUIObject("Text", unpBadgeGo);
        var unpText = unpTextGo.AddComponent<TextMeshProUGUI>();
        unpText.enableWordWrapping = true;
        unpText.overflowMode = TMPro.TextOverflowModes.Overflow;
        unpText.text = "<b>UNP</b>";
        unpText.fontSize = 10.0f;
        unpText.color = new Color(0.05f, 0.29f, 0.54f, 1f); // Blue #0D4B8A
        unpText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) unpText.font = interFont;
        StretchRect(unpTextGo.GetComponent<RectTransform>());

        // G01 Middle Graphic - Soft Sage Cube/Silhouette
        var introCenterGo = CreateUIObject("CenterGraphic", introGo);
        var centerImg = introCenterGo.AddComponent<Image>();
        centerImg.preserveAspect = true;
        var cubeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MobileARTemplateAssets/UI/Sprites/Icon-Cube.png");
        if (cubeSprite != null) centerImg.sprite = cubeSprite;
        centerImg.color = ColorSoftSage;
        SetCenterPosition(introCenterGo.GetComponent<RectTransform>(), 0f, 50.0f, 73.3f, 73.3f);

        // G01 Bottom Brand - Title & Progress Bar
        var brandGroupGo = CreateUIObject("BrandGroup", introGo);
        SetAnchorBottom(brandGroupGo.GetComponent<RectTransform>(), 0f, 100.0f, 300.0f, 120.0f);

        var titleGo = CreateUIObject("TitleText", brandGroupGo);
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.enableWordWrapping = true;
        titleText.overflowMode = TMPro.TextOverflowModes.Overflow;
        titleText.text = "GerakAR";
        titleText.fontSize = 34.0f; // matches Brand G01 size 34px
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) titleText.font = interFont;
        SetCenterPosition(titleGo.GetComponent<RectTransform>(), 0f, 40.0f, 266.7f, 36.0f);

        var subtitleGo = CreateUIObject("SubtitleText", brandGroupGo);
        var subtitleText = subtitleGo.AddComponent<TextMeshProUGUI>();
        subtitleText.enableWordWrapping = true;
        subtitleText.overflowMode = TMPro.TextOverflowModes.Overflow;
        subtitleText.text = "Belajar Gerak Jadi Seru";
        subtitleText.fontSize = 12.0f;
        subtitleText.color = ColorSoftSage;
        subtitleText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) subtitleText.font = interFont;
        SetCenterPosition(subtitleGo.GetComponent<RectTransform>(), 0f, 10.0f, 266.7f, 16.7f);

        // G01 Loading progress bar
        var progressTrackGo = CreateUIObject("ProgressTrack", brandGroupGo);
        var trackImg = progressTrackGo.AddComponent<Image>();
        
        trackImg.sprite = btnSprite;
        trackImg.type = Image.Type.Sliced;
        trackImg.color = new Color(0.12f, 0.365f, 0.259f, 0.3f); // #1F5D42 with 30% opacity
        SetCenterPosition(progressTrackGo.GetComponent<RectTransform>(), 0f, -30.0f, 180.0f, 6.0f);

        var progressFillGo = CreateUIObject("ProgressFill", progressTrackGo);
        var fillImg = progressFillGo.AddComponent<Image>();
        
        fillImg.sprite = btnSprite;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = 0; // Explicitly fill from Left
        fillImg.color = ColorCleanOffWhite;
        StretchRect(progressFillGo.GetComponent<RectTransform>());

        // Link fill image to IntroController
        var serialIntro = new SerializedObject(introController);
        serialIntro.FindProperty("introCanvasGroup").objectReferenceValue = introCanvasGroup;
        serialIntro.FindProperty("loadingFillImage").objectReferenceValue = fillImg;
        serialIntro.ApplyModifiedProperties();

        // 2. Onboarding Panel (G02) - Warm Cream
        var onboardGo = CreateUIObject("OnboardingPanel", canvasGo);
        var onboardImg = onboardGo.AddComponent<Image>();
        
        onboardImg.color = new Color(0.957f, 0.941f, 0.902f, 1f); // Warm Cream #F4F0E6
        StretchRect(onboardGo.GetComponent<RectTransform>());
        onboardGo.SetActive(false);

        // G02 Header - Shield Check Circle & Teks
        var safetyHeaderGo = CreateUIObject("SafetyHeader", onboardGo);
        SetAnchorTop(safetyHeaderGo.GetComponent<RectTransform>(), 0f, -60.0f, 300.0f, 120.0f);

        var checkIconCircleGo = CreateUIObject("CheckCircle", safetyHeaderGo);
        var circleImg = checkIconCircleGo.AddComponent<Image>();
        
        circleImg.sprite = btnSprite;
        circleImg.type = Image.Type.Sliced;
        circleImg.color = new Color(0.66f, 0.745f, 0.635f, 0.3f); // Soft Sage 30%
        SetCenterPosition(checkIconCircleGo.GetComponent<RectTransform>(), 0f, 35.0f, 44.0f, 44.0f);

        var checkIconTextGo = CreateUIObject("Text", checkIconCircleGo);
        var checkIconText = checkIconTextGo.AddComponent<TextMeshProUGUI>();
        checkIconText.enableWordWrapping = true;
        checkIconText.overflowMode = TMPro.TextOverflowModes.Overflow;
        checkIconText.text = "OK";
        checkIconText.fontSize = 20.0f;
        checkIconText.color = ColorDeepForest;
        checkIconText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) checkIconText.font = interFont;
        StretchRect(checkIconTextGo.GetComponent<RectTransform>());

        var obTitleGo = CreateUIObject("OnboardingTitle", safetyHeaderGo);
        var obTitle = obTitleGo.AddComponent<TextMeshProUGUI>();
        obTitle.enableWordWrapping = true;
        obTitle.overflowMode = TMPro.TextOverflowModes.Overflow;
        obTitle.text = "Sebelum Mulai";
        obTitle.fontSize = 24.0f; // matches title 24px
        obTitle.fontStyle = FontStyles.Bold;
        obTitle.color = ColorDeepForest;
        obTitle.alignment = TextAlignmentOptions.Center;
        if (interFont != null) obTitle.font = interFont;
        SetCenterPosition(obTitleGo.GetComponent<RectTransform>(), 0f, -10.0f, 266.7f, 28.0f);

        var obSubtitleGo = CreateUIObject("OnboardingSubtitle", safetyHeaderGo);
        var obSubtitle = obSubtitleGo.AddComponent<TextMeshProUGUI>();
        obSubtitle.enableWordWrapping = true;
        obSubtitle.overflowMode = TMPro.TextOverflowModes.Overflow;
        obSubtitle.text = "Ayo bergerak dengan aman dan nyaman.";
        obSubtitle.fontSize = 12.0f;
        obSubtitle.color = ColorCharcoal;
        obSubtitle.alignment = TextAlignmentOptions.Center;
        if (interFont != null) obSubtitle.font = interFont;
        SetCenterPosition(obSubtitleGo.GetComponent<RectTransform>(), 0f, -32.0f, 266.7f, 16.7f);

        // G02 List Cards (1, 2, 3)
        var obListGo = CreateUIObject("InstructionList", onboardGo);
        SetCenterPosition(obListGo.GetComponent<RectTransform>(), 0f, -10.0f, 320.0f, 180.0f);

        // Card 1
        var card1Go = CreateUIObject("Card1", obListGo);
        var card1Img = card1Go.AddComponent<Image>();
        
        card1Img.sprite = btnSprite;
        card1Img.type = Image.Type.Sliced;
        card1Img.color = Color.white;
        SetCenterPosition(card1Go.GetComponent<RectTransform>(), 0f, 60.0f, 306.7f, 52.0f);

        var num1CircleGo = CreateUIObject("NumCircle", card1Go);
        var num1CircleImg = num1CircleGo.AddComponent<Image>();
        num1CircleImg.preserveAspect = true;
        num1CircleImg.sprite = btnSprite;
        num1CircleImg.type = Image.Type.Sliced;
        num1CircleImg.color = ColorForestGreen;
        SetCenterPosition(num1CircleGo.GetComponent<RectTransform>(), -126.7f, 0f, 24.0f, 24.0f);

        var num1TextGo = CreateUIObject("Text", num1CircleGo);
        var num1Text = num1TextGo.AddComponent<TextMeshProUGUI>();
        num1Text.enableWordWrapping = true;
        num1Text.overflowMode = TMPro.TextOverflowModes.Overflow;
        num1Text.text = "1";
        num1Text.fontSize = 12.0f;
        num1Text.fontStyle = FontStyles.Bold;
        num1Text.color = Color.white;
        num1Text.alignment = TextAlignmentOptions.Center;
        if (interFont != null) num1Text.font = interFont;
        StretchRect(num1TextGo.GetComponent<RectTransform>());

        var desc1Go = CreateUIObject("Desc", card1Go);
        var desc1 = desc1Go.AddComponent<TextMeshProUGUI>();
        desc1.enableWordWrapping = true;
        desc1.overflowMode = TMPro.TextOverflowModes.Overflow;
        desc1.text = "Gunakan di tempat yang cukup luas.";
        desc1.fontSize = 12.0f;
        desc1.color = ColorCharcoal;
        desc1.alignment = TextAlignmentOptions.Left;
        if (interFont != null) desc1.font = interFont;
        SetCenterPosition(desc1Go.GetComponent<RectTransform>(), 20.0f, 0f, 220.0f, 30.0f);

        // Card 2
        var card2Go = CreateUIObject("Card2", obListGo);
        var card2Img = card2Go.AddComponent<Image>();
        
        card2Img.sprite = btnSprite;
        card2Img.type = Image.Type.Sliced;
        card2Img.color = Color.white;
        SetCenterPosition(card2Go.GetComponent<RectTransform>(), 0f, 0f, 306.7f, 52.0f);

        var num2CircleGo = CreateUIObject("NumCircle", card2Go);
        var num2CircleImg = num2CircleGo.AddComponent<Image>();
        num2CircleImg.preserveAspect = true;
        num2CircleImg.sprite = btnSprite;
        num2CircleImg.type = Image.Type.Sliced;
        num2CircleImg.color = ColorForestGreen;
        SetCenterPosition(num2CircleGo.GetComponent<RectTransform>(), -126.7f, 0f, 24.0f, 24.0f);

        var num2TextGo = CreateUIObject("Text", num2CircleGo);
        var num2Text = num2TextGo.AddComponent<TextMeshProUGUI>();
        num2Text.enableWordWrapping = true;
        num2Text.overflowMode = TMPro.TextOverflowModes.Overflow;
        num2Text.text = "2";
        num2Text.fontSize = 12.0f;
        num2Text.fontStyle = FontStyles.Bold;
        num2Text.color = Color.white;
        num2Text.alignment = TextAlignmentOptions.Center;
        if (interFont != null) num2Text.font = interFont;
        StretchRect(num2TextGo.GetComponent<RectTransform>());

        var desc2Go = CreateUIObject("Desc", card2Go);
        var desc2 = desc2Go.AddComponent<TextMeshProUGUI>();
        desc2.enableWordWrapping = true;
        desc2.overflowMode = TMPro.TextOverflowModes.Overflow;
        desc2.text = "Minta guru atau orang tua mendampingi.";
        desc2.fontSize = 12.0f;
        desc2.color = ColorCharcoal;
        desc2.alignment = TextAlignmentOptions.Left;
        if (interFont != null) desc2.font = interFont;
        SetCenterPosition(desc2Go.GetComponent<RectTransform>(), 20.0f, 0f, 220.0f, 30.0f);

        // Card 3
        var card3Go = CreateUIObject("Card3", obListGo);
        var card3Img = card3Go.AddComponent<Image>();
        
        card3Img.sprite = btnSprite;
        card3Img.type = Image.Type.Sliced;
        card3Img.color = Color.white;
        SetCenterPosition(card3Go.GetComponent<RectTransform>(), 0f, -60.0f, 306.7f, 52.0f);

        var num3CircleGo = CreateUIObject("NumCircle", card3Go);
        var num3CircleImg = num3CircleGo.AddComponent<Image>();
        num3CircleImg.preserveAspect = true;
        num3CircleImg.sprite = btnSprite;
        num3CircleImg.type = Image.Type.Sliced;
        num3CircleImg.color = ColorForestGreen;
        SetCenterPosition(num3CircleGo.GetComponent<RectTransform>(), -126.7f, 0f, 24.0f, 24.0f);

        var num3TextGo = CreateUIObject("Text", num3CircleGo);
        var num3Text = num3TextGo.AddComponent<TextMeshProUGUI>();
        num3Text.enableWordWrapping = true;
        num3Text.overflowMode = TMPro.TextOverflowModes.Overflow;
        num3Text.text = "3";
        num3Text.fontSize = 12.0f;
        num3Text.fontStyle = FontStyles.Bold;
        num3Text.color = Color.white;
        num3Text.alignment = TextAlignmentOptions.Center;
        if (interFont != null) num3Text.font = interFont;
        StretchRect(num3TextGo.GetComponent<RectTransform>());

        var desc3Go = CreateUIObject("Desc", card3Go);
        var desc3 = desc3Go.AddComponent<TextMeshProUGUI>();
        desc3.enableWordWrapping = true;
        desc3.overflowMode = TMPro.TextOverflowModes.Overflow;
        desc3.text = "Izinkan kamera untuk melihat gerakan.";
        desc3.fontSize = 12.0f;
        desc3.color = ColorCharcoal;
        desc3.alignment = TextAlignmentOptions.Left;
        if (interFont != null) desc3.font = interFont;
        SetCenterPosition(desc3Go.GetComponent<RectTransform>(), 20.0f, 0f, 220.0f, 30.0f);

        // G02 Bottom Buttons Group
        var btnGroupGo = CreateUIObject("ButtonGroup", onboardGo);
        SetAnchorBottom(btnGroupGo.GetComponent<RectTransform>(), 0f, 80.0f, 320.0f, 100.0f);

        // Tombol MULAI
        var startBtnGo = CreateUIObject("MulaiButton", btnGroupGo);
        var startBtnImg = startBtnGo.AddComponent<Image>();
        
        startBtnImg.sprite = btnSprite;
        startBtnImg.type = Image.Type.Sliced;
        startBtnImg.color = ColorForestGreen;
        var startBtn = startBtnGo.AddComponent<Button>();
        startBtnGo.AddComponent<GerakAR.UI.OnboardingButtonWirer>();
        SetCenterPosition(startBtnGo.GetComponent<RectTransform>(), 0f, 20.0f, 306.7f, 48.0f); // premium height 48px

        var btnTextGo = CreateUIObject("Text", startBtnGo);
        var btnText = btnTextGo.AddComponent<TextMeshProUGUI>();
        btnText.enableWordWrapping = true;
        btnText.overflowMode = TMPro.TextOverflowModes.Overflow;
        btnText.text = "MULAI";
        btnText.fontSize = 14.0f; // premium text-sm font-bold
        btnText.fontStyle = FontStyles.Bold;
        btnText.color = Color.white;
        btnText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) btnText.font = interFont;
        StretchRect(btnTextGo.GetComponent<RectTransform>());

        // Fallback Simulasi Links (Disabled in production build)
        var fallbackLinksGo = CreateUIObject("FallbackLinks", btnGroupGo);
        SetCenterPosition(fallbackLinksGo.GetComponent<RectTransform>(), 0f, -25.0f, 306.7f, 16.7f);
        fallbackLinksGo.SetActive(false); // HIDE simulation buttons

        var nonARLinkGo = CreateUIObject("NonARLink", fallbackLinksGo);
        var nonARLinkText = nonARLinkGo.AddComponent<TextMeshProUGUI>();
        nonARLinkText.enableWordWrapping = true;
        nonARLinkText.overflowMode = TMPro.TextOverflowModes.Overflow;
        nonARLinkText.text = "<u>Simulasi Non-AR</u>";
        nonARLinkText.fontSize = 7.3f;
        nonARLinkText.color = ColorForestGreen;
        nonARLinkText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) nonARLinkText.font = interFont;
        var nonARBtn = nonARLinkGo.AddComponent<Button>();
        SetCenterPosition(nonARLinkGo.GetComponent<RectTransform>(), -73.3f, 0f, 113.3f, 16.7f);

        var camErrorLinkGo = CreateUIObject("CameraErrorLink", fallbackLinksGo);
        var camErrorLinkText = camErrorLinkGo.AddComponent<TextMeshProUGUI>();
        camErrorLinkText.enableWordWrapping = true;
        camErrorLinkText.overflowMode = TMPro.TextOverflowModes.Overflow;
        camErrorLinkText.text = "<u>Simulasi Kendala Kamera</u>";
        camErrorLinkText.fontSize = 7.3f;
        camErrorLinkText.color = ColorDeepForest;
        camErrorLinkText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) camErrorLinkText.font = interFont;
        var camErrorBtn = camErrorLinkGo.AddComponent<Button>();
        SetCenterPosition(camErrorLinkGo.GetComponent<RectTransform>(), 73.3f, 0f, 113.3f, 16.7f);

        var serialOnboard = new SerializedObject(onboardingController);
        serialOnboard.FindProperty("onboardingPanel").objectReferenceValue = onboardGo;
        serialOnboard.ApplyModifiedProperties();

        // 3. Unsupported Panel (Parent for G08 & G09 Fallbacks)
        var unsupGo = CreateUIObject("UnsupportedPanel", canvasGo);
        var unsupImg = unsupGo.AddComponent<Image>();
        
        unsupImg.color = new Color(0.957f, 0.941f, 0.902f, 1f); // Warm Cream #F4F0E6
        StretchRect(unsupGo.GetComponent<RectTransform>());
        unsupGo.SetActive(false);

        // G08 view (Mode Non-AR Catalog)
        var nonARModePanelGo = CreateUIObject("NonARModePanel", unsupGo);
        StretchRect(nonARModePanelGo.GetComponent<RectTransform>());

        // G08 Brand Header
        var nonARHeaderGo = CreateUIObject("Header", nonARModePanelGo);
        SetAnchorTop(nonARHeaderGo.GetComponent<RectTransform>(), 0f, -48.0f, 320.0f, 53.3f);

        var nonARTitleGo = CreateUIObject("Title", nonARHeaderGo);
        var nonARTitle = nonARTitleGo.AddComponent<TextMeshProUGUI>();
        nonARTitle.enableWordWrapping = true;
        nonARTitle.overflowMode = TMPro.TextOverflowModes.Overflow;
        nonARTitle.text = "GerakAR";
        nonARTitle.fontSize = 20.0f;
        nonARTitle.fontStyle = FontStyles.Bold;
        nonARTitle.color = ColorDeepForest;
        nonARTitle.alignment = TextAlignmentOptions.Left;
        if (interFont != null) nonARTitle.font = interFont;
        SetCenterPosition(nonARTitleGo.GetComponent<RectTransform>(), -73.3f, 3.3f, 146.7f, 24.0f);

        var nonARSubGo = CreateUIObject("Sub", nonARHeaderGo);
        var nonARSub = nonARSubGo.AddComponent<TextMeshProUGUI>();
        nonARSub.enableWordWrapping = true;
        nonARSub.overflowMode = TMPro.TextOverflowModes.Overflow;
        nonARSub.text = "MODE PEMBELAJARAN MANDIRI";
        nonARSub.fontSize = 10.0f;
        nonARSub.fontStyle = FontStyles.Bold;
        nonARSub.color = ColorForestGreen;
        nonARSub.alignment = TextAlignmentOptions.Left;
        if (interFont != null) nonARSub.font = interFont;
        SetCenterPosition(nonARSubGo.GetComponent<RectTransform>(), -73.3f, -13.3f, 146.7f, 14.0f);

        var nonARBadgeGo = CreateUIObject("Badge", nonARHeaderGo);
        var badgeImg = nonARBadgeGo.AddComponent<Image>();
        
        badgeImg.sprite = btnSprite;
        badgeImg.type = Image.Type.Sliced;
        badgeImg.color = new Color(0.72f, 0.4f, 0.29f, 0.1f); // Light Terracotta/Amber
        SetCenterPosition(nonARBadgeGo.GetComponent<RectTransform>(), 106.7f, 0f, 80.0f, 23.3f);

        var badgeTextGo = CreateUIObject("Text", nonARBadgeGo);
        var badgeText = badgeTextGo.AddComponent<TextMeshProUGUI>();
        badgeText.enableWordWrapping = true;
        badgeText.overflowMode = TMPro.TextOverflowModes.Overflow;
        badgeText.text = "NON-AR MODE";
        badgeText.fontSize = 10.0f;
        badgeText.fontStyle = FontStyles.Bold;
        badgeText.color = new Color(0.72f, 0.4f, 0.29f, 1f); // Terracotta
        badgeText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) badgeText.font = interFont;
        StretchRect(badgeTextGo.GetComponent<RectTransform>());

        // G08 Terracotta Warning Banner
        var warnBannerGo = CreateUIObject("WarningBanner", nonARModePanelGo);
        var warnImg = warnBannerGo.AddComponent<Image>();
        
        warnImg.sprite = btnSprite;
        warnImg.type = Image.Type.Sliced;
        warnImg.color = new Color(0.72f, 0.4f, 0.29f, 0.08f); // Terracotta 8%
        SetAnchorTop(warnBannerGo.GetComponent<RectTransform>(), 0f, -110.0f, 320.0f, 60.0f);

        var warnIconGo = CreateUIObject("Icon", warnBannerGo);
        var warnIcon = warnIconGo.AddComponent<TextMeshProUGUI>();
        warnIcon.enableWordWrapping = true;
        warnIcon.overflowMode = TMPro.TextOverflowModes.Overflow;
        warnIcon.text = "i";
        warnIcon.fontSize = 20.0f;
        warnIcon.alignment = TextAlignmentOptions.Center;
        SetCenterPosition(warnIconGo.GetComponent<RectTransform>(), -130.0f, 0f, 30.0f, 30.0f);

        var warnTextGo = CreateUIObject("Text", warnBannerGo);
        var warnText = warnTextGo.AddComponent<TextMeshProUGUI>();
        warnText.enableWordWrapping = true;
        warnText.overflowMode = TMPro.TextOverflowModes.Overflow;
        warnText.text = "<b>Perangkat Belum Mendukung AR</b>\n<size=10><color=#202620>Kamu diarahkan langsung untuk membaca modul materi edukasi secara lengkap tanpa fitur kamera.</color></size>";
        warnText.fontSize = 11.0f;
        warnText.color = new Color(0.72f, 0.4f, 0.29f, 1f); // Terracotta
        warnText.alignment = TextAlignmentOptions.Left;
        if (interFont != null) warnText.font = interFont;
        SetCenterPosition(warnTextGo.GetComponent<RectTransform>(), 20.0f, 0f, 250.0f, 44.0f);

        // G08 Catalog Content
        var catalogCatalogGo = CreateUIObject("CatalogCatalog", nonARModePanelGo);
        SetCenterPosition(catalogCatalogGo.GetComponent<RectTransform>(), 0f, -35.0f, 320.0f, 280.0f);

        var catTitleGo = CreateUIObject("CatTitleText", catalogCatalogGo);
        var catTitle = catTitleGo.AddComponent<TextMeshProUGUI>();
        catTitle.enableWordWrapping = true;
        catTitle.overflowMode = TMPro.TextOverflowModes.Overflow;
        catTitle.text = "KATALOG GERAKAN SISWA";
        catTitle.fontSize = 11.0f;
        catTitle.fontStyle = FontStyles.Bold;
        catTitle.color = ColorDeepForest;
        catTitle.alignment = TextAlignmentOptions.Left;
        if (interFont != null) catTitle.font = interFont;
        SetCenterPosition(catTitleGo.GetComponent<RectTransform>(), 0f, 130.0f, 300.0f, 14.0f);

        // Catalogue Card Squat
        var cardSquatGo = CreateUIObject("CardSquat", catalogCatalogGo);
        var squatImg = cardSquatGo.AddComponent<Image>();
        
        squatImg.sprite = btnSprite;
        squatImg.type = Image.Type.Sliced;
        squatImg.color = Color.white;
        SetCenterPosition(cardSquatGo.GetComponent<RectTransform>(), 0f, 88.0f, 300.0f, 56.0f);

        var squatIconGo = CreateUIObject("Icon", cardSquatGo);
        var squatIcon = squatIconGo.AddComponent<TextMeshProUGUI>();
        squatIcon.enableWordWrapping = true;
        squatIcon.overflowMode = TMPro.TextOverflowModes.Overflow;
        squatIcon.text = "SQ";
        squatIcon.fontSize = 14.0f;
        squatIcon.fontStyle = FontStyles.Bold;
        squatIcon.color = new Color(0.72f, 0.4f, 0.29f, 1f);
        squatIcon.alignment = TextAlignmentOptions.Center;
        SetCenterPosition(squatIconGo.GetComponent<RectTransform>(), -120.0f, 0f, 30.0f, 30.0f);

        var squatTitleGo = CreateUIObject("TitleText", cardSquatGo);
        var squatTitle = squatTitleGo.AddComponent<TextMeshProUGUI>();
        squatTitle.enableWordWrapping = true;
        squatTitle.overflowMode = TMPro.TextOverflowModes.Overflow;
        squatTitle.text = "<b>Gerakan Squat</b>\n<size=10><color=#607D4F>Melatih otot paha dan sendi lutut</color></size>";
        squatTitle.fontSize = 12.0f;
        squatTitle.color = ColorDeepForest;
        squatTitle.alignment = TextAlignmentOptions.Left;
        if (interFont != null) squatTitle.font = interFont;
        SetCenterPosition(squatTitleGo.GetComponent<RectTransform>(), 20.0f, 0f, 180.0f, 36.0f);

        var squatBukaGo = CreateUIObject("BukaButton", cardSquatGo);
        var squatBukaImg = squatBukaGo.AddComponent<Image>();
        
        squatBukaImg.sprite = btnSprite;
        squatBukaImg.type = Image.Type.Sliced;
        squatBukaImg.color = ColorForestGreen;
        var squatBukaBtn = squatBukaGo.AddComponent<Button>();
        SetCenterPosition(squatBukaGo.GetComponent<RectTransform>(), 110.0f, 0f, 52.0f, 28.0f);

        var squatBukaTextGo = CreateUIObject("Text", squatBukaGo);
        var squatBukaText = squatBukaTextGo.AddComponent<TextMeshProUGUI>();
        squatBukaText.enableWordWrapping = true;
        squatBukaText.overflowMode = TMPro.TextOverflowModes.Overflow;
        squatBukaText.text = "Buka";
        squatBukaText.fontSize = 11.0f;
        squatBukaText.fontStyle = FontStyles.Bold;
        squatBukaText.color = Color.white;
        squatBukaText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) squatBukaText.font = interFont;
        StretchRect(squatBukaTextGo.GetComponent<RectTransform>());

        // Dynamic Stretching card
        var cardJackGo = CreateUIObject("CardDynamicStretch", catalogCatalogGo);
        var jackImg = cardJackGo.AddComponent<Image>();
        
        jackImg.sprite = btnSprite;
        jackImg.type = Image.Type.Sliced;
        jackImg.color = Color.white;
        SetCenterPosition(cardJackGo.GetComponent<RectTransform>(), 0f, 24.0f, 300.0f, 56.0f);

        var jackIconGo = CreateUIObject("Icon", cardJackGo);
        var jackIcon = jackIconGo.AddComponent<TextMeshProUGUI>();
        jackIcon.enableWordWrapping = true;
        jackIcon.overflowMode = TMPro.TextOverflowModes.Overflow;
        jackIcon.text = "DS";
        jackIcon.fontSize = 14.0f;
        jackIcon.fontStyle = FontStyles.Bold;
        jackIcon.color = new Color(0.247f, 0.486f, 0.471f, 1f);
        jackIcon.alignment = TextAlignmentOptions.Center;
        SetCenterPosition(jackIconGo.GetComponent<RectTransform>(), -120.0f, 0f, 30.0f, 30.0f);

        var jackTitleGo = CreateUIObject("TitleText", cardJackGo);
        var jackTitle = jackTitleGo.AddComponent<TextMeshProUGUI>();
        jackTitle.enableWordWrapping = true;
        jackTitle.overflowMode = TMPro.TextOverflowModes.Overflow;
        jackTitle.text = "<b>Dynamic Stretching</b>\n<size=10><color=#607D4F>Peregangan aktif sebelum bergerak</color></size>";
        jackTitle.fontSize = 12.0f;
        jackTitle.color = ColorDeepForest;
        jackTitle.alignment = TextAlignmentOptions.Left;
        if (interFont != null) jackTitle.font = interFont;
        SetCenterPosition(jackTitleGo.GetComponent<RectTransform>(), 20.0f, 0f, 180.0f, 36.0f);

        var jackLockGo = CreateUIObject("BukaButton", cardJackGo);
        var jackLockImg = jackLockGo.AddComponent<Image>();
        
        jackLockImg.sprite = btnSprite;
        jackLockImg.type = Image.Type.Sliced;
        jackLockImg.color = ColorForestGreen;
        var dynamicStretchBukaBtn = jackLockGo.AddComponent<Button>();
        SetCenterPosition(jackLockGo.GetComponent<RectTransform>(), 110.0f, 0f, 52.0f, 28.0f);

        var jackLockTextGo = CreateUIObject("Text", jackLockGo);
        var jackLockText = jackLockTextGo.AddComponent<TextMeshProUGUI>();
        jackLockText.enableWordWrapping = true;
        jackLockText.overflowMode = TMPro.TextOverflowModes.Overflow;
        jackLockText.text = "Buka";
        jackLockText.fontSize = 11.0f;
        jackLockText.fontStyle = FontStyles.Bold;
        jackLockText.color = Color.white;
        jackLockText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) jackLockText.font = interFont;
        StretchRect(jackLockTextGo.GetComponent<RectTransform>());

        // Ladder Drill card
        var cardLadderGo = CreateUIObject("CardLadderDrill", catalogCatalogGo);
        var ladderImg = cardLadderGo.AddComponent<Image>();
        ladderImg.sprite = btnSprite;
        ladderImg.type = Image.Type.Sliced;
        ladderImg.color = Color.white;
        SetCenterPosition(cardLadderGo.GetComponent<RectTransform>(), 0f, -40.0f, 300.0f, 56.0f);

        var ladderIconGo = CreateUIObject("Icon", cardLadderGo);
        var ladderIcon = ladderIconGo.AddComponent<TextMeshProUGUI>();
        ladderIcon.text = "LD";
        ladderIcon.fontSize = 14.0f;
        ladderIcon.fontStyle = FontStyles.Bold;
        ladderIcon.color = new Color(0.765f, 0.635f, 0.294f, 1f);
        ladderIcon.alignment = TextAlignmentOptions.Center;
        if (interFont != null) ladderIcon.font = interFont;
        SetCenterPosition(ladderIconGo.GetComponent<RectTransform>(), -120.0f, 0f, 30.0f, 30.0f);

        var ladderTitleGo = CreateUIObject("TitleText", cardLadderGo);
        var ladderTitle = ladderTitleGo.AddComponent<TextMeshProUGUI>();
        ladderTitle.enableWordWrapping = true;
        ladderTitle.overflowMode = TMPro.TextOverflowModes.Overflow;
        ladderTitle.text = "<b>Ladder Drill</b>\n<size=10><color=#607D4F>Melatih kelincahan dan langkah kaki</color></size>";
        ladderTitle.fontSize = 12.0f;
        ladderTitle.color = ColorDeepForest;
        ladderTitle.alignment = TextAlignmentOptions.Left;
        if (interFont != null) ladderTitle.font = interFont;
        SetCenterPosition(ladderTitleGo.GetComponent<RectTransform>(), 20.0f, 0f, 180.0f, 36.0f);

        var ladderBukaGo = CreateUIObject("BukaButton", cardLadderGo);
        var ladderBukaImg = ladderBukaGo.AddComponent<Image>();
        ladderBukaImg.sprite = btnSprite;
        ladderBukaImg.type = Image.Type.Sliced;
        ladderBukaImg.color = ColorForestGreen;
        var ladderDrillBukaBtn = ladderBukaGo.AddComponent<Button>();
        SetCenterPosition(ladderBukaGo.GetComponent<RectTransform>(), 110.0f, 0f, 52.0f, 28.0f);

        var ladderBukaTextGo = CreateUIObject("Text", ladderBukaGo);
        var ladderBukaText = ladderBukaTextGo.AddComponent<TextMeshProUGUI>();
        ladderBukaText.text = "Buka";
        ladderBukaText.fontSize = 11.0f;
        ladderBukaText.fontStyle = FontStyles.Bold;
        ladderBukaText.color = Color.white;
        ladderBukaText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) ladderBukaText.font = interFont;
        StretchRect(ladderBukaTextGo.GetComponent<RectTransform>());

        // G08 Back Button
        var catalogBackGo = CreateUIObject("CatalogBackButton", nonARModePanelGo);
        var catalogBackText = catalogBackGo.AddComponent<TextMeshProUGUI>();
        catalogBackText.enableWordWrapping = true;
        catalogBackText.overflowMode = TMPro.TextOverflowModes.Overflow;
        catalogBackText.text = "< Petunjuk";
        catalogBackText.fontSize = 12.0f;
        catalogBackText.fontStyle = FontStyles.Bold;
        catalogBackText.color = ColorForestGreen;
        catalogBackText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) catalogBackText.font = interFont;
        var catalogBackBtn = catalogBackGo.AddComponent<Button>();
        SetAnchorBottom(catalogBackGo.GetComponent<RectTransform>(), -100.0f, 40.0f, 90.0f, 30.0f);

        // G09 view (Kamera Belum Aktif)
        var cameraErrorPanelGo = CreateUIObject("CameraErrorPanel", unsupGo);
        StretchRect(cameraErrorPanelGo.GetComponent<RectTransform>());
        cameraErrorPanelGo.SetActive(false);

        var camOffIconGo = CreateUIObject("CamOffIcon", cameraErrorPanelGo);
        var camOffIcon = camOffIconGo.AddComponent<TextMeshProUGUI>();
        camOffIcon.enableWordWrapping = true;
        camOffIcon.overflowMode = TMPro.TextOverflowModes.Overflow;
        camOffIcon.text = "CAM";
        camOffIcon.fontSize = 44.0f;
        camOffIcon.alignment = TextAlignmentOptions.Center;
        SetCenterPosition(camOffIconGo.GetComponent<RectTransform>(), 0f, 73.3f, 66.7f, 66.7f);

        var camErrorTitleGo = CreateUIObject("Title", cameraErrorPanelGo);
        var camErrorTitle = camErrorTitleGo.AddComponent<TextMeshProUGUI>();
        camErrorTitle.enableWordWrapping = true;
        camErrorTitle.overflowMode = TMPro.TextOverflowModes.Overflow;
        camErrorTitle.text = "Kamera Belum Aktif";
        camErrorTitle.fontSize = 24.0f;
        camErrorTitle.fontStyle = FontStyles.Bold;
        camErrorTitle.color = ColorDeepForest;
        camErrorTitle.alignment = TextAlignmentOptions.Center;
        if (interFont != null) camErrorTitle.font = interFont;
        SetCenterPosition(camErrorTitleGo.GetComponent<RectTransform>(), 0f, 20.0f, 266.7f, 28.0f);

        var camErrorDescGo = CreateUIObject("Desc", cameraErrorPanelGo);
        var camErrorDesc = camErrorDescGo.AddComponent<TextMeshProUGUI>();
        camErrorDesc.enableWordWrapping = true;
        camErrorDesc.overflowMode = TMPro.TextOverflowModes.Overflow;
        camErrorDesc.text = "Izinkan akses kamera agar GerakAR dapat melihat gambar gerakan.";
        camErrorDesc.fontSize = 14.0f;
        camErrorDesc.color = ColorCharcoal;
        camErrorDesc.alignment = TextAlignmentOptions.Center;
        if (interFont != null) camErrorDesc.font = interFont;
        SetCenterPosition(camErrorDescGo.GetComponent<RectTransform>(), 0f, -16.0f, 260.0f, 44.0f);

        // G09 BUKA PENGATURAN Button
        var settingsGo = CreateUIObject("SettingsButton", cameraErrorPanelGo);
        var settingsImg = settingsGo.AddComponent<Image>();
        
        settingsImg.sprite = btnSprite;
        settingsImg.type = Image.Type.Sliced;
        settingsImg.color = ColorForestGreen;
        var settingsBtn = settingsGo.AddComponent<Button>();
        SetAnchorBottom(settingsGo.GetComponent<RectTransform>(), 0f, 120.0f, 280.0f, 48.0f);

        var settingsTextGo = CreateUIObject("Text", settingsGo);
        var settingsText = settingsTextGo.AddComponent<TextMeshProUGUI>();
        settingsText.enableWordWrapping = true;
        settingsText.overflowMode = TMPro.TextOverflowModes.Overflow;
        settingsText.text = "BUKA PENGATURAN";
        settingsText.fontSize = 14.0f;
        settingsText.fontStyle = FontStyles.Bold;
        settingsText.color = Color.white;
        settingsText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) settingsText.font = interFont;
        StretchRect(settingsTextGo.GetComponent<RectTransform>());

        // G09 Coba Lagi Button
        var retryGo = CreateUIObject("RetryButton", cameraErrorPanelGo);
        var retryBtn = retryGo.AddComponent<Button>();
        SetAnchorBottom(retryGo.GetComponent<RectTransform>(), 0f, 70.0f, 280.0f, 32.0f);

        var retryTextGo = CreateUIObject("Text", retryGo);
        var retryText = retryTextGo.AddComponent<TextMeshProUGUI>();
        retryText.enableWordWrapping = true;
        retryText.overflowMode = TMPro.TextOverflowModes.Overflow;
        retryText.text = "Coba Lagi";
        retryText.fontSize = 14.0f;
        retryText.fontStyle = FontStyles.Bold;
        retryText.color = ColorDeepForest;
        retryText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) retryText.font = interFont;
        StretchRect(retryTextGo.GetComponent<RectTransform>());

        // Attach the unified BootstrapUIController
        var bootstrapUI = managersGo.AddComponent<BootstrapUIController>();
        var serialBUI = new SerializedObject(bootstrapUI);
        serialBUI.FindProperty("introPanel").objectReferenceValue = introGo;
        serialBUI.FindProperty("onboardingPanel").objectReferenceValue = onboardGo;
        serialBUI.FindProperty("unsupportedPanel").objectReferenceValue = unsupGo;
        serialBUI.FindProperty("nonARModePanel").objectReferenceValue = nonARModePanelGo;
        serialBUI.FindProperty("cameraErrorPanel").objectReferenceValue = cameraErrorPanelGo;
        serialBUI.ApplyModifiedProperties();

        // Attach BootstrapButtonController to wire all click actions at runtime
        var btnCtrl = managersGo.AddComponent<BootstrapButtonController>();
        var serialBtn = new SerializedObject(btnCtrl);
        serialBtn.FindProperty("nonARModeLink").objectReferenceValue = nonARBtn;
        serialBtn.FindProperty("cameraErrorLink").objectReferenceValue = camErrorBtn;
        serialBtn.FindProperty("squatBukaBtn").objectReferenceValue = squatBukaBtn;
        serialBtn.FindProperty("dynamicStretchBukaBtn").objectReferenceValue = dynamicStretchBukaBtn;
        serialBtn.FindProperty("ladderDrillBukaBtn").objectReferenceValue = ladderDrillBukaBtn;
        serialBtn.FindProperty("catalogBackBtn").objectReferenceValue = catalogBackBtn;
        serialBtn.FindProperty("settingsBtn").objectReferenceValue = settingsBtn;
        serialBtn.FindProperty("retryBtn").objectReferenceValue = retryBtn;
        serialBtn.ApplyModifiedProperties();



        EditorSceneManager.SaveScene(scene, "Assets/App/Scenes/Bootstrap.unity");
        Debug.Log("[GerakAR] Scene Bootstrap selesai dibuat.");
    }

    private static void CreateMainARScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MainAR";

        // Keep ExecuteInEditMode ARUnityX components inactive while they are added.
        // The package does not ship a Linux Editor native library.
        var arRootGo = new GameObject("ARUnityX Runtime");
        arRootGo.SetActive(false);
        var arController = arRootGo.AddComponent<ARXController>();
        arController.enabled = false;
        arController.AutoStartAR = true;
        arController.UseNativeGLTexturingIfAvailable = false;
        arController.AllowNonRGBVideo = true;
        arController.QuitOnEscOrBack = false;
        arController.videoCParamName0 = "camera_para";

        var videoConfig = arRootGo.GetComponent<ARXVideoConfig>();
        ConfigureRearCamera(videoConfig);

        var imageTarget = arRootGo.AddComponent<ARXTrackable>();
        imageTarget.enabled = false;
        imageTarget.Tag = "C5";
        var serializedTarget = new SerializedObject(imageTarget);
        serializedTarget.FindProperty("<Type>k__BackingField").intValue = (int)ARXTrackable.TrackableType.TwoD;
        serializedTarget.FindProperty("<TwoDImageFile>k__BackingField").stringValue = "C5.png";
        serializedTarget.FindProperty("<TwoDImageWidth>k__BackingField").floatValue = 0.18f;
        serializedTarget.FindProperty("currentFiltered").boolValue = true;
        serializedTarget.FindProperty("currentFilterSampleRate").floatValue = 30f;
        serializedTarget.FindProperty("currentFilterCutoffFreq").floatValue = 15f;
        serializedTarget.ApplyModifiedProperties();

        // ARUnityX foreground camera. Layer 8 is reserved for its video background.
        var camGo = new GameObject("AR Camera");
        camGo.SetActive(false);
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = ColorDeepForest;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 100f;
        cam.cullingMask &= ~(1 << 8);
        cam.allowHDR = false;
        camGo.tag = "MainCamera";
        camGo.AddComponent<AudioListener>();
        var arCamera = camGo.AddComponent<ARXCamera>();
        arCamera.CameraContentMode = ARXCamera.ContentMode.Fill;
        var videoBackground = camGo.AddComponent<ARXVideoBackground>();
        videoBackground.BackgroundLayer = 8;

        // ARUnityX drives this anchor from the C5 trackable pose.
        var trackedAnchorGo = new GameObject("C5 Tracked Anchor");
        var trackedObject = trackedAnchorGo.AddComponent<ARXTrackedObject>();
        trackedObject.TrackableTag = "C5";
        trackedObject.secondsToRemainVisible = 0.75f;

        var modelRootGo = new GameObject("ModelRoot");
        modelRootGo.transform.SetParent(trackedAnchorGo.transform, false);

        // Load Database Asset
        var database = AssetDatabase.LoadAssetAtPath<MovementDatabase>("Assets/App/Content/MovementData/MovementDatabase.asset");

        // Managers
        var managersGo = new GameObject("Managers");
        var modelPool = managersGo.AddComponent<ModelPool>();
        var movementController = managersGo.AddComponent<MovementController>();
        var audioGuideController = managersGo.AddComponent<AudioGuideController>();
        var trackingController = managersGo.AddComponent<ARImageTrackingController>();
        var sessionController = managersGo.AddComponent<ARUnityXSessionController>();

        // Standalone AppStateManager (so it does not destroy scene-specific Managers via DontDestroyOnLoad)
        var stateMgrGo = new GameObject("AppStateManager");
        var stateMgr = stateMgrGo.AddComponent<AppStateManager>();

        // Configure ModelPool
        var serialPool = new SerializedObject(modelPool);
        serialPool.FindProperty("modelRoot").objectReferenceValue = modelRootGo.transform;
        serialPool.ApplyModifiedProperties();

        // Configure tracking controller
        var serialTracking = new SerializedObject(trackingController);
        serialTracking.FindProperty("imageTarget").objectReferenceValue = imageTarget;
        serialTracking.FindProperty("trackedObject").objectReferenceValue = trackedObject;
        serialTracking.FindProperty("referenceImageName").stringValue = "squat_target";
        serialTracking.FindProperty("movementDatabase").objectReferenceValue = database;
        serialTracking.FindProperty("modelPool").objectReferenceValue = modelPool;
        serialTracking.FindProperty("movementController").objectReferenceValue = movementController;
        serialTracking.ApplyModifiedProperties();

        var serialSession = new SerializedObject(sessionController);
        serialSession.FindProperty("arController").objectReferenceValue = arController;
        serialSession.FindProperty("imageTarget").objectReferenceValue = imageTarget;
        serialSession.FindProperty("trackedObject").objectReferenceValue = trackedObject;
        serialSession.FindProperty("arCamera").objectReferenceValue = arCamera;
        serialSession.FindProperty("videoBackground").objectReferenceValue = videoBackground;
        serialSession.FindProperty("trackingController").objectReferenceValue = trackingController;
        serialSession.ApplyModifiedProperties();

        // Configure Audio Controller
        var serialAudio = new SerializedObject(audioGuideController);
        serialAudio.FindProperty("movementDatabase").objectReferenceValue = database;
        serialAudio.ApplyModifiedProperties();

        // Canvas
        var canvasGo = new GameObject("UI Canvas", typeof(RectTransform));
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(360f, 800f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Event System - pakai InputSystemUIInputModule karena project menggunakan New Input System
        var eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemGo.AddComponent<InputSystemUIInputModule>();

        // Load premium UI assets
        var interFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/MobileARTemplateAssets/UI/Fonts/Inter-Regular_SDF.asset");
        var btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MobileARTemplateAssets/UI/Sprites/ActivationButtonOpaque.png");
        var frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MobileARTemplateAssets/UI/Sprites/MixedCorners.png");
        var roundTopSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MobileARTemplateAssets/UI/Sprites/RoundRadius_10_Top.png");

        var arUI = canvasGo.AddComponent<ARUIController>();

        // 1. Scan Overlay Panel (G03)
        var scanGo = CreateUIObject("ScanOverlay", canvasGo);
        StretchRect(scanGo.GetComponent<RectTransform>());

        // G03 Header Title
        var scanTitleGo = CreateUIObject("HeaderTitle", scanGo);
        var scanTitle = scanTitleGo.AddComponent<TextMeshProUGUI>();
        scanTitle.enableWordWrapping = true;
        scanTitle.overflowMode = TMPro.TextOverflowModes.Overflow;
        scanTitle.text = "GerakAR";
        scanTitle.fontSize = 20.0f; // matches ui.html text-xl
        scanTitle.fontStyle = FontStyles.Bold;
        scanTitle.color = Color.white;
        scanTitle.alignment = TextAlignmentOptions.Center;
        if (interFont != null) scanTitle.font = interFont;
        SetAnchorTop(scanTitleGo.GetComponent<RectTransform>(), 0f, -48.0f, 200.0f, 28.0f);

        var scanSubGo = CreateUIObject("HeaderSub", scanGo);
        var scanSub = scanSubGo.AddComponent<TextMeshProUGUI>();
        scanSub.enableWordWrapping = true;
        scanSub.overflowMode = TMPro.TextOverflowModes.Overflow;
        scanSub.text = "Belajar Gerak Jadi Seru";
        scanSub.fontSize = 10.0f; // matches text-[10px]
        scanSub.color = ColorSoftSage;
        scanSub.alignment = TextAlignmentOptions.Center;
        if (interFont != null) scanSub.font = interFont;
        SetAnchorTop(scanSubGo.GetComponent<RectTransform>(), 0f, -76.0f, 200.0f, 16.0f);

        // G03 Central Scan Guide Frame
        var scanFrameGo = CreateUIObject("ScanFrame", scanGo);
        SetCenterPosition(scanFrameGo.GetComponent<RectTransform>(), 0f, 0f, 232.0f, 232.0f);

        // Helper to create L-brackets (30x30 outer boundaries)
        System.Action<string, float, float, float, float> CreateCorner = (name, x, y, ax, ay) => {
            var c = CreateUIObject(name, scanFrameGo);
            c.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
            c.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            c.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            c.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
            c.GetComponent<RectTransform>().sizeDelta = new Vector2(30f, 30f);
            
            // Horizontal line
            var h = CreateUIObject(name + "H", c);
            var hImg = h.AddComponent<Image>();
            hImg.color = ColorCleanOffWhite;
            var hRT = h.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(ax, ay);
            hRT.anchorMax = new Vector2(ax, ay);
            hRT.pivot = new Vector2(ax, ay);
            hRT.anchoredPosition = Vector2.zero;
            hRT.sizeDelta = new Vector2(26f, 4f);

            // Vertical line
            var v = CreateUIObject(name + "V", c);
            var vImg = v.AddComponent<Image>();
            vImg.color = ColorCleanOffWhite;
            var vRT = v.GetComponent<RectTransform>();
            vRT.anchorMin = new Vector2(ax, ay);
            vRT.anchorMax = new Vector2(ax, ay);
            vRT.pivot = new Vector2(ax, ay);
            vRT.anchoredPosition = Vector2.zero;
            vRT.sizeDelta = new Vector2(4f, 26f);
        };

        CreateCorner("TopLeft", -101f, 101f, 0f, 1f);      // Anchor top-left
        CreateCorner("TopRight", 101f, 101f, 1f, 1f);       // Anchor top-right
        CreateCorner("BottomLeft", -101f, -101f, 0f, 0f);   // Anchor bottom-left
        CreateCorner("BottomRight", 101f, -101f, 1f, 0f);   // Anchor bottom-right

        // G03 Scan Laser Line
        var scanLineGo = CreateUIObject("ScanLaserLine", scanFrameGo);
        var scanLineImg = scanLineGo.AddComponent<Image>();
        
        scanLineImg.sprite = btnSprite;
        scanLineImg.type = Image.Type.Sliced;
        scanLineImg.color = new Color(0.66f, 0.745f, 0.635f, 0.7f); // Glowing Soft Sage
        scanLineGo.AddComponent<LaserLineAnimator>();
        scanLineGo.SetActive(false); // Hide initially until target is detected
        SetCenterPosition(scanLineGo.GetComponent<RectTransform>(), 0f, 100.0f, 180.0f, 2.7f);

        // G03 Scan Target Pill Below Frame (corresponds to translate-y-[130px] from center)
        var scanPillGo = CreateUIObject("ScanTargetPill", scanGo);
        var scanPillImg = scanPillGo.AddComponent<Image>();
        
        scanPillImg.sprite = btnSprite;
        scanPillImg.type = Image.Type.Sliced;
        scanPillImg.color = new Color(0f, 0f, 0f, 0.45f); // Black 45%
        SetCenterPosition(scanPillGo.GetComponent<RectTransform>(), 0f, -145.0f, 160.0f, 30.0f);

        var scanPillTextGo = CreateUIObject("Text", scanPillGo);
        var scanPillText = scanPillTextGo.AddComponent<TextMeshProUGUI>();
        scanPillText.enableWordWrapping = true;
        scanPillText.overflowMode = TMPro.TextOverflowModes.Overflow;
        scanPillText.text = "Pindai Target Gambar";
        scanPillText.fontSize = 11.0f; // matches text-[11px] or text-xs
        scanPillText.fontStyle = FontStyles.Bold;
        scanPillText.color = ColorCleanOffWhite;
        scanPillText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) scanPillText.font = interFont;
        StretchRect(scanPillTextGo.GetComponent<RectTransform>());

        // G03 Bottom Instruction Card (corresponds to bottom-[100px])
        var instructionCardGo = CreateUIObject("InstructionCard", scanGo);
        var instCardImg = instructionCardGo.AddComponent<Image>();
        
        instCardImg.sprite = btnSprite;
        instCardImg.type = Image.Type.Sliced;
        instCardImg.color = ColorCleanOffWhite;
        SetAnchorBottom(instructionCardGo.GetComponent<RectTransform>(), 0f, 100.0f, 280.0f, 52.0f);

        var instIconCircleGo = CreateUIObject("IconCircle", instructionCardGo);
        var instIconCircleImg = instIconCircleGo.AddComponent<Image>();
        instIconCircleImg.preserveAspect = true;
        instIconCircleImg.sprite = btnSprite;
        instIconCircleImg.type = Image.Type.Sliced;
        instIconCircleImg.color = new Color(0.66f, 0.745f, 0.635f, 0.2f); // Soft Sage 20%
        // Anchored to the left of the card
        instIconCircleGo.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0.5f);
        instIconCircleGo.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 0.5f);
        instIconCircleGo.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
        instIconCircleGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(16.0f, 0f);
        instIconCircleGo.GetComponent<RectTransform>().sizeDelta = new Vector2(28.0f, 28.0f);

        var instIconTextGo = CreateUIObject("Text", instIconCircleGo);
        var instIconText = instIconTextGo.AddComponent<TextMeshProUGUI>();
        instIconText.enableWordWrapping = true;
        instIconText.overflowMode = TMPro.TextOverflowModes.Overflow;
        instIconText.text = "CAM";
        instIconText.fontSize = 14.0f;
        instIconText.alignment = TextAlignmentOptions.Center;
        StretchRect(instIconTextGo.GetComponent<RectTransform>());

        var hintGo = CreateUIObject("HintText", instructionCardGo);
        var hintText = hintGo.AddComponent<TextMeshProUGUI>();
        hintText.enableWordWrapping = true;
        hintText.overflowMode = TMPro.TextOverflowModes.Overflow;
        hintText.text = "Arahkan kamera ke gambar gerakan";
        hintText.fontSize = 12.0f; // matches text-[12px]
        hintText.fontStyle = FontStyles.Bold;
        hintText.color = ColorCharcoal;
        hintText.alignment = TextAlignmentOptions.Left;
        if (interFont != null) hintText.font = interFont;
        // Anchored relative to the parent card
        hintGo.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0.5f);
        hintGo.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 0.5f);
        hintGo.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
        hintGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(56.0f, 0f);
        hintGo.GetComponent<RectTransform>().sizeDelta = new Vector2(-72.0f, 24.0f);

        var instSubtitleGo = CreateUIObject("InstructionSubtitle", scanGo);
        var instSubtitle = instSubtitleGo.AddComponent<TextMeshProUGUI>();
        instSubtitle.enableWordWrapping = true;
        instSubtitle.overflowMode = TMPro.TextOverflowModes.Overflow;
        instSubtitle.text = "Pastikan seluruh gambar terlihat";
        instSubtitle.fontSize = 10.0f; // matches text-[10px]
        instSubtitle.color = ColorCleanOffWhite;
        instSubtitle.alignment = TextAlignmentOptions.Center;
        if (interFont != null) instSubtitle.font = interFont;
        SetAnchorBottom(instSubtitleGo.GetComponent<RectTransform>(), 0f, 70.0f, 280.0f, 16.0f);

        // 2. Detection Toast Panel (G04) - Centered success card
        var toastGo = CreateUIObject("DetectionToast", canvasGo);
        var toastImg = toastGo.AddComponent<Image>();
        
        toastImg.sprite = btnSprite;
        toastImg.type = Image.Type.Sliced;
        toastImg.color = ColorCleanOffWhite;
        SetCenterPosition(toastGo.GetComponent<RectTransform>(), 0f, 0f, 180.0f, 120.0f);

        var toastCircleGo = CreateUIObject("SuccessCircle", toastGo);
        var toastCircleImg = toastCircleGo.AddComponent<Image>();
        toastCircleImg.preserveAspect = true;
        toastCircleImg.sprite = btnSprite;
        toastCircleImg.type = Image.Type.Sliced;
        toastCircleImg.color = ColorForestGreen;
        SetCenterPosition(toastCircleGo.GetComponent<RectTransform>(), 0f, 26.7f, 43.3f, 43.3f);

        var toastCheckGo = CreateUIObject("Text", toastCircleGo);
        var toastCheck = toastCheckGo.AddComponent<TextMeshProUGUI>();
        toastCheck.enableWordWrapping = true;
        toastCheck.overflowMode = TMPro.TextOverflowModes.Overflow;
        toastCheck.text = "OK";
        toastCheck.fontSize = 19.3f;
        toastCheck.color = Color.white;
        toastCheck.alignment = TextAlignmentOptions.Center;
        if (interFont != null) toastCheck.font = interFont;
        StretchRect(toastCheckGo.GetComponent<RectTransform>());

        var toastTextGo = CreateUIObject("TitleText", toastGo);
        var toastText = toastTextGo.AddComponent<TextMeshProUGUI>();
        toastText.enableWordWrapping = true;
        toastText.overflowMode = TMPro.TextOverflowModes.Overflow;
        toastText.text = "Gerakan Ditemukan!";
        toastText.fontSize = 10.7f;
        toastText.fontStyle = FontStyles.Bold;
        toastText.color = ColorDeepForest;
        toastText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) toastText.font = interFont;
        SetCenterPosition(toastTextGo.GetComponent<RectTransform>(), 0f, -16.7f, 160.0f, 16.7f);

        var toastPillGo = CreateUIObject("MovementPill", toastGo);
        var toastPillImg = toastPillGo.AddComponent<Image>();
        
        toastPillImg.sprite = btnSprite;
        toastPillImg.type = Image.Type.Sliced;
        toastPillImg.color = new Color(0.72f, 0.4f, 0.29f, 0.1f); // Terracotta 10%
        SetCenterPosition(toastPillGo.GetComponent<RectTransform>(), 0f, -36.7f, 73.3f, 16.7f);

        var toastPillTextGo = CreateUIObject("Text", toastPillGo);
        var toastPillText = toastPillTextGo.AddComponent<TextMeshProUGUI>();
        toastPillText.enableWordWrapping = true;
        toastPillText.overflowMode = TMPro.TextOverflowModes.Overflow;
        toastPillText.text = "SQUAT";
        toastPillText.fontSize = 6.7f;
        toastPillText.fontStyle = FontStyles.Bold;
        toastPillText.color = new Color(0.72f, 0.4f, 0.29f, 1f); // Terracotta
        toastPillText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) toastPillText.font = interFont;
        StretchRect(toastPillTextGo.GetComponent<RectTransform>());
        toastGo.SetActive(false);

        // 3. AR Controls Panel (G05 Inspect HUD)
        var arControlsGo = CreateUIObject("ARControls", canvasGo);
        StretchRect(arControlsGo.GetComponent<RectTransform>());
        arControlsGo.SetActive(false);

        // AR View Header Title (transparent background)
        var arHeaderTitleGo = CreateUIObject("HeaderTitle", arControlsGo);
        var arHeaderTitle = arHeaderTitleGo.AddComponent<TextMeshProUGUI>();
        arHeaderTitle.enableWordWrapping = true;
        arHeaderTitle.overflowMode = TMPro.TextOverflowModes.Overflow;
        arHeaderTitle.text = "GerakAR";
        arHeaderTitle.fontSize = 20.0f; // matches G03
        arHeaderTitle.fontStyle = FontStyles.Bold;
        arHeaderTitle.color = Color.white;
        arHeaderTitle.alignment = TextAlignmentOptions.Center;
        if (interFont != null) arHeaderTitle.font = interFont;
        SetAnchorTop(arHeaderTitleGo.GetComponent<RectTransform>(), 0f, -48.0f, 200.0f, 28.0f);

        var arHeaderSubGo = CreateUIObject("HeaderSub", arControlsGo);
        var arHeaderSub = arHeaderSubGo.AddComponent<TextMeshProUGUI>();
        arHeaderSub.enableWordWrapping = true;
        arHeaderSub.overflowMode = TMPro.TextOverflowModes.Overflow;
        arHeaderSub.text = "Belajar Gerak Jadi Seru";
        arHeaderSub.fontSize = 10.0f; // matches G03
        arHeaderSub.color = ColorSoftSage;
        arHeaderSub.alignment = TextAlignmentOptions.Center;
        if (interFont != null) arHeaderSub.font = interFont;
        SetAnchorTop(arHeaderSubGo.GetComponent<RectTransform>(), 0f, -76.0f, 200.0f, 16.0f);

        // G05 Right Column of Floating Circular FABs
        var fabColumnGo = CreateUIObject("FABColumn", arControlsGo);
        SetAnchorBottomRight(fabColumnGo.GetComponent<RectTransform>(), -16.0f, 120.0f, 40.0f, 126.7f);

        // FAB 1: Audio Play/Pause Button
        var playPauseGo = CreateUIObject("PlayPauseButton", fabColumnGo);
        var playPauseImg = playPauseGo.AddComponent<Image>();
        
        playPauseImg.sprite = btnSprite;
        playPauseImg.type = Image.Type.Sliced;
        playPauseImg.color = ColorDeepForest;
        var playPauseBtn = playPauseGo.AddComponent<Button>();
        SetCenterPosition(playPauseGo.GetComponent<RectTransform>(), 0f, 40.0f, 33.3f, 33.3f);

        var playPauseImgGo = CreateUIObject("Icon", playPauseGo);
        var playPauseImgComp = playPauseImgGo.AddComponent<Image>();
        playPauseImgComp.preserveAspect = true;
        var pauseSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/DemoSceneAssets/Sprites/Circle_60x60_Horizontal.png");
        var playSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/DemoSceneAssets/Sprites/Forward.png");
        if (pauseSprite != null) playPauseImgComp.sprite = pauseSprite;
        playPauseImgComp.color = ColorSoftSage;
        SetCenterPosition(playPauseImgGo.GetComponent<RectTransform>(), 0f, 4.0f, 14.7f, 14.7f);

        var playPauseLabelGo = CreateUIObject("Text", playPauseGo);
        var playPauseLabel = playPauseLabelGo.AddComponent<TextMeshProUGUI>();
        playPauseLabel.enableWordWrapping = true;
        playPauseLabel.overflowMode = TMPro.TextOverflowModes.Overflow;
        playPauseLabel.text = "PLAY";
        playPauseLabel.fontSize = 4.7f;
        playPauseLabel.fontStyle = FontStyles.Bold;
        playPauseLabel.color = Color.white;
        playPauseLabel.alignment = TextAlignmentOptions.Center;
        if (interFont != null) playPauseLabel.font = interFont;
        SetCenterPosition(playPauseLabelGo.GetComponent<RectTransform>(), 0f, -9.3f, 30.0f, 8.3f);

        // FAB 2: Open Material Detail Button
        var matBtnGo = CreateUIObject("MaterialButton", fabColumnGo);
        var matBtnImg = matBtnGo.AddComponent<Image>();
        
        matBtnImg.sprite = btnSprite;
        matBtnImg.type = Image.Type.Sliced;
        matBtnImg.color = ColorForestGreen;
        var matBtn = matBtnGo.AddComponent<Button>();
        SetCenterPosition(matBtnGo.GetComponent<RectTransform>(), 0f, 0f, 33.3f, 33.3f);

        var matIconGo = CreateUIObject("IconText", matBtnGo);
        var matIcon = matIconGo.AddComponent<TextMeshProUGUI>();
        matIcon.enableWordWrapping = true;
        matIcon.overflowMode = TMPro.TextOverflowModes.Overflow;
        matIcon.text = "M";
        matIcon.fontSize = 12.0f;
        matIcon.alignment = TextAlignmentOptions.Center;
        StretchRect(matIconGo.GetComponent<RectTransform>());

        // FAB 3: Close / Reset Scan Button
        var closeGo = CreateUIObject("CloseButton", fabColumnGo);
        var closeImg = closeGo.AddComponent<Image>();
        
        closeImg.sprite = btnSprite;
        closeImg.type = Image.Type.Sliced;
        closeImg.color = ColorDeepForest;
        var closeBtn = closeGo.AddComponent<Button>();
        SetCenterPosition(closeGo.GetComponent<RectTransform>(), 0f, -40.0f, 33.3f, 33.3f);

        var closeIconGo = CreateUIObject("IconText", closeGo);
        var closeIcon = closeIconGo.AddComponent<TextMeshProUGUI>();
        closeIcon.enableWordWrapping = true;
        closeIcon.overflowMode = TMPro.TextOverflowModes.Overflow;
        closeIcon.text = "X";
        closeIcon.fontSize = 12.0f;
        closeIcon.color = Color.white;
        closeIcon.alignment = TextAlignmentOptions.Center;
        if (interFont != null) closeIcon.font = interFont;
        StretchRect(closeIconGo.GetComponent<RectTransform>());

        // G05 Bottom Info & Timeline Slider Card
        var timelineRootGo = CreateUIObject("TimelineCard", arControlsGo);
        var tlCardImg = timelineRootGo.AddComponent<Image>();
        
        tlCardImg.sprite = btnSprite;
        tlCardImg.type = Image.Type.Sliced;
        tlCardImg.color = new Color(0.957f, 0.941f, 0.902f, 0.96f); // Warm Cream 96%
        SetAnchorBottom(timelineRootGo.GetComponent<RectTransform>(), 0f, 24.0f, 328.0f, 80.0f);

        // G05 Info tags inside bottom card
        var tlInfoRowGo = CreateUIObject("InfoRow", timelineRootGo);
        SetCenterPosition(tlInfoRowGo.GetComponent<RectTransform>(), 0f, 22.0f, 300.0f, 20.0f);

        var squatTagGo = CreateUIObject("SquatTag", tlInfoRowGo);
        var squatTagImg = squatTagGo.AddComponent<Image>();
        
        squatTagImg.sprite = btnSprite;
        squatTagImg.type = Image.Type.Sliced;
        squatTagImg.color = ColorCleanOffWhite;
        SetCenterPosition(squatTagGo.GetComponent<RectTransform>(), -110.0f, 0f, 64.0f, 18.0f);

        var squatDotGo = CreateUIObject("Dot", squatTagGo);
        var squatDotImg = squatDotGo.AddComponent<Image>();
        squatDotImg.preserveAspect = true;
        squatDotImg.sprite = btnSprite;
        squatDotImg.type = Image.Type.Sliced;
        squatDotImg.color = new Color(0.72f, 0.4f, 0.29f, 1f); // Terracotta dot
        SetCenterPosition(squatDotGo.GetComponent<RectTransform>(), -20.0f, 0f, 6.7f, 6.7f);

        var nameLabelGo = CreateUIObject("Name", squatTagGo);
        var nameLabel = nameLabelGo.AddComponent<TextMeshProUGUI>();
        nameLabel.enableWordWrapping = true;
        nameLabel.overflowMode = TMPro.TextOverflowModes.Overflow;
        nameLabel.text = "Squat";
        nameLabel.fontSize = 11.0f; // matches text-xs / 11px
        nameLabel.fontStyle = FontStyles.Bold;
        nameLabel.color = ColorDeepForest;
        nameLabel.alignment = TextAlignmentOptions.Left;
        if (interFont != null) nameLabel.font = interFont;
        SetCenterPosition(nameLabelGo.GetComponent<RectTransform>(), 8f, 0f, 38.0f, 14.0f);

        var statusTagGo = CreateUIObject("StatusTag", tlInfoRowGo);
        var statusTag = statusTagGo.AddComponent<TextMeshProUGUI>();
        statusTag.enableWordWrapping = true;
        statusTag.overflowMode = TMPro.TextOverflowModes.Overflow;
        statusTag.text = "Status: Loop";
        statusTag.fontSize = 10.0f; // matches text-[10px]
        statusTag.fontStyle = FontStyles.Bold;
        statusTag.color = ColorForestGreen;
        statusTag.alignment = TextAlignmentOptions.Right;
        if (interFont != null) statusTag.font = interFont;
        SetCenterPosition(statusTagGo.GetComponent<RectTransform>(), 103.3f, 0f, 73.3f, 13.3f);

        // Timeline Slider Control
        var sliderGo = CreateUIObject("Slider", timelineRootGo);
        var slider = sliderGo.AddComponent<Slider>();
        SetCenterPosition(sliderGo.GetComponent<RectTransform>(), 0f, 1.7f, 293.3f, 13.3f);

        // Customized Slider Visuals
        var sliderBgGo = CreateUIObject("Background", sliderGo);
        var slBgImg = sliderBgGo.AddComponent<Image>();
        
        slBgImg.sprite = btnSprite;
        slBgImg.type = Image.Type.Sliced;
        slBgImg.color = ColorSoftSage;
        SetCenterPosition(sliderBgGo.GetComponent<RectTransform>(), 0f, 0f, 293.3f, 3.3f);

        // Slider Handle Area
        var handleAreaGo = CreateUIObject("Handle Area", sliderGo);
        StretchRect(handleAreaGo.GetComponent<RectTransform>());

        var handleVisGo = CreateUIObject("Handle", handleAreaGo);
        var handleVisImg = handleVisGo.AddComponent<Image>();
        
        handleVisImg.sprite = btnSprite;
        handleVisImg.type = Image.Type.Sliced;
        handleVisImg.color = ColorForestGreen;
        SetCenterPosition(handleVisGo.GetComponent<RectTransform>(), 0f, 0f, 14.7f, 14.7f);
        slider.handleRect = handleVisGo.GetComponent<RectTransform>();

        // Under-slider info labels
        var tlBottomLabelsGo = CreateUIObject("BottomLabels", timelineRootGo);
        SetCenterPosition(tlBottomLabelsGo.GetComponent<RectTransform>(), 0f, -18.3f, 293.3f, 13.3f);

        var startLabelGo = CreateUIObject("Mulai", tlBottomLabelsGo);
        var startLabel = startLabelGo.AddComponent<TextMeshProUGUI>();
        startLabel.enableWordWrapping = true;
        startLabel.overflowMode = TMPro.TextOverflowModes.Overflow;
        startLabel.text = "Mulai";
        startLabel.fontSize = 6.7f;
        startLabel.color = ColorMossGreen;
        startLabel.alignment = TextAlignmentOptions.Left;
        if (interFont != null) startLabel.font = interFont;
        SetCenterPosition(startLabelGo.GetComponent<RectTransform>(), -126.7f, 0f, 40.0f, 11.7f);

        var midLabelGo = CreateUIObject("Petunjuk", tlBottomLabelsGo);
        var midLabel = midLabelGo.AddComponent<TextMeshProUGUI>();
        midLabel.enableWordWrapping = true;
        midLabel.overflowMode = TMPro.TextOverflowModes.Overflow;
        midLabel.text = "Geser untuk memeriksa pose";
        midLabel.fontSize = 6.7f;
        midLabel.color = ColorMossGreen;
        midLabel.alignment = TextAlignmentOptions.Center;
        if (interFont != null) midLabel.font = interFont;
        SetCenterPosition(midLabelGo.GetComponent<RectTransform>(), 0f, 0f, 133.3f, 11.7f);

        var endLabelGo = CreateUIObject("Selesai", tlBottomLabelsGo);
        var endLabel = endLabelGo.AddComponent<TextMeshProUGUI>();
        endLabel.enableWordWrapping = true;
        endLabel.overflowMode = TMPro.TextOverflowModes.Overflow;
        endLabel.text = "Selesai";
        endLabel.fontSize = 6.7f;
        endLabel.color = ColorMossGreen;
        endLabel.alignment = TextAlignmentOptions.Right;
        if (interFont != null) endLabel.font = interFont;
        SetCenterPosition(endLabelGo.GetComponent<RectTransform>(), 126.7f, 0f, 40.0f, 11.7f);

        // Timeline Controller setup
        var timelineCtrl = timelineRootGo.AddComponent<PoseTimelineController>();
        var serialTimeline = new SerializedObject(timelineCtrl);
        serialTimeline.FindProperty("timelineSlider").objectReferenceValue = slider;
        serialTimeline.FindProperty("movementController").objectReferenceValue = movementController;
        serialTimeline.ApplyModifiedProperties();

        // 4. Bottom Sheet Panel (G06 & G07) - Pulls up to 94% height
        var sheetGo = CreateUIObject("BottomSheet", canvasGo);
        var sheetImg = sheetGo.AddComponent<Image>();
        
        sheetImg.sprite = roundTopSprite;
        sheetImg.type = Image.Type.Sliced;
        sheetImg.color = ColorCleanOffWhite;
        var sheetRT = sheetGo.GetComponent<RectTransform>();
        sheetRT.anchorMin = new Vector2(0f, 0f);
        sheetRT.anchorMax = new Vector2(1f, 0f);
        sheetRT.pivot = new Vector2(0.5f, 1f); // Pivot at top-center (mandatory for BottomSheetController)
        sheetRT.anchoredPosition = new Vector2(0f, 0f); // Closed state by default
        sheetRT.sizeDelta = new Vector2(0f, 752f); // 94% height in 800px space

        // Grab Handle
        var handleGo = CreateUIObject("GrabHandle", sheetGo);
        var handleImg = handleGo.AddComponent<Image>();
        
        handleImg.sprite = btnSprite;
        handleImg.type = Image.Type.Sliced;
        handleImg.color = ColorSoftSage;
        SetCenterPosition(handleGo.GetComponent<RectTransform>(), 0f, sheetRT.sizeDelta.y - 6.7f, 33.3f, 3.3f);

        // Sheet Header Area (Utama / Tambahan)
        var categoryGo = CreateUIObject("CategoryTypeLabel", sheetGo);
        var categoryTxt = categoryGo.AddComponent<TextMeshProUGUI>();
        categoryTxt.enableWordWrapping = true;
        categoryTxt.overflowMode = TMPro.TextOverflowModes.Overflow;
        categoryTxt.text = "GERAKAN UTAMA";
        categoryTxt.fontSize = 7.3f;
        categoryTxt.fontStyle = FontStyles.Bold;
        categoryTxt.color = new Color(0.72f, 0.4f, 0.29f, 1f); // Terracotta
        if (interFont != null) categoryTxt.font = interFont;
        SetCenterPosition(categoryGo.GetComponent<RectTransform>(), -86.7f, sheetRT.sizeDelta.y - 23.3f, 133.3f, 13.3f);

        // Dedicated Sheet Movement Name Text
        var sheetTitleGo = CreateUIObject("MovementTitle", sheetGo);
        var sheetTitleText = sheetTitleGo.AddComponent<TextMeshProUGUI>();
        sheetTitleText.enableWordWrapping = true;
        sheetTitleText.overflowMode = TMPro.TextOverflowModes.Overflow;
        sheetTitleText.text = "SQUAT";
        sheetTitleText.fontSize = 13.3f;
        sheetTitleText.fontStyle = FontStyles.Bold;
        sheetTitleText.color = ColorDeepForest;
        sheetTitleText.alignment = TextAlignmentOptions.Left;
        if (interFont != null) sheetTitleText.font = interFont;
        SetCenterPosition(sheetTitleGo.GetComponent<RectTransform>(), -73.3f, sheetRT.sizeDelta.y - 40.0f, 160.0f, 20.0f);

        // Back to primary button (G07 -> G06)
        var backBtnGo = CreateUIObject("BackToPrimaryButton", sheetGo);
        var backBtnImg = backBtnGo.AddComponent<Image>();
        
        backBtnImg.sprite = btnSprite;
        backBtnImg.type = Image.Type.Sliced;
        backBtnImg.color = ColorDeepForest;
        var backBtn = backBtnGo.AddComponent<Button>();
        SetCenterPosition(backBtnGo.GetComponent<RectTransform>(), 120.0f, sheetRT.sizeDelta.y - 30.0f, 53.3f, 23.3f);

        // Back Button Text
        var backBtnTextGo = CreateUIObject("Text", backBtnGo);
        var backBtnText = backBtnTextGo.AddComponent<TextMeshProUGUI>();
        backBtnText.enableWordWrapping = true;
        backBtnText.overflowMode = TMPro.TextOverflowModes.Overflow;
        backBtnText.text = "Kembali";
        backBtnText.fontSize = 7.3f;
        backBtnText.fontStyle = FontStyles.Bold;
        backBtnText.color = Color.white;
        backBtnText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) backBtnText.font = interFont;
        StretchRect(backBtnTextGo.GetComponent<RectTransform>());

        // Close Bottom Sheet Button
        var sheetCloseGo = CreateUIObject("SheetCloseX", sheetGo);
        var sheetCloseImg = sheetCloseGo.AddComponent<Image>();
        
        sheetCloseImg.sprite = btnSprite;
        sheetCloseImg.type = Image.Type.Sliced;
        sheetCloseImg.color = new Color(0f, 0f, 0f, 0.05f); // Light transparent gray
        var sheetCloseBtn = sheetCloseGo.AddComponent<Button>();
        SetCenterPosition(sheetCloseGo.GetComponent<RectTransform>(), 136.7f, sheetRT.sizeDelta.y - 30.0f, 23.3f, 23.3f);

        var sheetCloseTxtGo = CreateUIObject("Text", sheetCloseGo);
        var sheetCloseTxt = sheetCloseTxtGo.AddComponent<TextMeshProUGUI>();
        sheetCloseTxt.enableWordWrapping = true;
        sheetCloseTxt.overflowMode = TMPro.TextOverflowModes.Overflow;
        sheetCloseTxt.text = "X";
        sheetCloseTxt.fontSize = 9.3f;
        sheetCloseTxt.color = ColorCharcoal;
        sheetCloseTxt.alignment = TextAlignmentOptions.Center;
        if (interFont != null) sheetCloseTxt.font = interFont;
        StretchRect(sheetCloseTxtGo.GetComponent<RectTransform>());

        // Scroll View under sheetGo
        var scrollViewGo = CreateUIObject("ScrollView", sheetGo);
        var scrollRect = scrollViewGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        SetCenterPosition(scrollViewGo.GetComponent<RectTransform>(), 0f, -36.7f, 320.0f, sheetRT.sizeDelta.y - 73.3f);

        // Viewport
        var viewportGo = CreateUIObject("Viewport", scrollViewGo);
        viewportGo.AddComponent<RectMask2D>();
        StretchRect(viewportGo.GetComponent<RectTransform>());
        scrollRect.viewport = viewportGo.GetComponent<RectTransform>();

        // Content Scroll Parent
        var contentGo = CreateUIObject("Content", viewportGo);
        var contentRT = contentGo.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0f, 1000f); // dynamically calculated
        
        var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 30f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        
        var csf = contentGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.content = contentRT;

        // 1. Tentang Gerakan
        var aboutGroupGo = CreateUIObject("AboutSection", contentGo);
        var aboutVlg = aboutGroupGo.AddComponent<VerticalLayoutGroup>();
        aboutVlg.spacing = 10f;
        aboutVlg.childControlWidth = true;
        aboutVlg.childControlHeight = true;

        var aboutTitleGo = CreateUIObject("Title", aboutGroupGo);
        var aboutTitle = aboutTitleGo.AddComponent<TextMeshProUGUI>();
        aboutTitle.enableWordWrapping = true;
        aboutTitle.overflowMode = TMPro.TextOverflowModes.Overflow;
        aboutTitle.text = "TENTANG GERAKAN";
        aboutTitle.fontSize = 6.7f;
        aboutTitle.fontStyle = FontStyles.Bold;
        aboutTitle.color = new Color(0.72f, 0.4f, 0.29f, 1f); // Terracotta
        if (interFont != null) aboutTitle.font = interFont;

        var descGo = CreateUIObject("Description", aboutGroupGo);
        var descText = descGo.AddComponent<TextMeshProUGUI>();
        descText.enableWordWrapping = true;
        descText.overflowMode = TMPro.TextOverflowModes.Overflow;
        descText.fontSize = 8.7f;
        descText.color = ColorCharcoal;
        if (interFont != null) descText.font = interFont;

        // 2. Cara Melakukan
        var stepsGroupGo = CreateUIObject("StepsSection", contentGo);
        var stepsVlg = stepsGroupGo.AddComponent<VerticalLayoutGroup>();
        stepsVlg.spacing = 15f;
        stepsVlg.childControlWidth = true;
        stepsVlg.childControlHeight = true;

        var stepsTitleGo = CreateUIObject("Title", stepsGroupGo);
        var stepsTitle = stepsTitleGo.AddComponent<TextMeshProUGUI>();
        stepsTitle.enableWordWrapping = true;
        stepsTitle.overflowMode = TMPro.TextOverflowModes.Overflow;
        stepsTitle.text = "CARA MELAKUKAN";
        stepsTitle.fontSize = 6.7f;
        stepsTitle.fontStyle = FontStyles.Bold;
        stepsTitle.color = new Color(0.72f, 0.4f, 0.29f, 1f); // Terracotta
        if (interFont != null) stepsTitle.font = interFont;

        var stepsContainerGo = CreateUIObject("Container", stepsGroupGo);
        var stepsContVlg = stepsContainerGo.AddComponent<VerticalLayoutGroup>();
        stepsContVlg.spacing = 15f;
        stepsContVlg.childControlWidth = true;
        stepsContVlg.childControlHeight = true;

        // 3. Safety Tip Box
        var safetyTipCardGo = CreateUIObject("SafetyTipCard", contentGo);
        var safetyImg = safetyTipCardGo.AddComponent<Image>();
        
        safetyImg.sprite = btnSprite;
        safetyImg.type = Image.Type.Sliced;
        safetyImg.color = new Color(0.66f, 0.745f, 0.635f, 0.25f); // Soft Sage 25%
        var safetyVlg = safetyTipCardGo.AddComponent<VerticalLayoutGroup>();
        safetyVlg.padding = new RectOffset(20, 20, 20, 20);

        var safetyTextGo = CreateUIObject("Text", safetyTipCardGo);
        var safetyText = safetyTextGo.AddComponent<TextMeshProUGUI>();
        safetyText.enableWordWrapping = true;
        safetyText.overflowMode = TMPro.TextOverflowModes.Overflow;
        safetyText.fontSize = 8.0f;
        safetyText.color = ColorDeepForest;
        if (interFont != null) safetyText.font = interFont;

        // 4. Mistakes & Trained Areas (Full State Extras)
        var fullExtrasGo = CreateUIObject("FullStateExtras", contentGo);
        var extrasVlg = fullExtrasGo.AddComponent<VerticalLayoutGroup>();
        extrasVlg.spacing = 30f;
        extrasVlg.childControlWidth = true;
        extrasVlg.childControlHeight = true;

        // Common Mistakes
        var mistakesTitleGo = CreateUIObject("MistakesTitle", fullExtrasGo);
        var mistakesTitle = mistakesTitleGo.AddComponent<TextMeshProUGUI>();
        mistakesTitle.enableWordWrapping = true;
        mistakesTitle.overflowMode = TMPro.TextOverflowModes.Overflow;
        mistakesTitle.text = "HINDARI INI";
        mistakesTitle.fontSize = 6.7f;
        mistakesTitle.fontStyle = FontStyles.Bold;
        mistakesTitle.color = new Color(0.72f, 0.4f, 0.29f, 1f); // Terracotta
        if (interFont != null) mistakesTitle.font = interFont;

        var mistakesContainerGo = CreateUIObject("MistakesContainer", fullExtrasGo);
        var mistakesContVlg = mistakesContainerGo.AddComponent<VerticalLayoutGroup>();
        mistakesContVlg.spacing = 10f;
        mistakesContVlg.childControlWidth = true;
        mistakesContVlg.childControlHeight = true;

        // Trained Areas
        var trainedTitleGo = CreateUIObject("TrainedTitle", fullExtrasGo);
        var trainedTitle = trainedTitleGo.AddComponent<TextMeshProUGUI>();
        trainedTitle.enableWordWrapping = true;
        trainedTitle.overflowMode = TMPro.TextOverflowModes.Overflow;
        trainedTitle.text = "OTOT YANG TERLATIH";
        trainedTitle.fontSize = 6.7f;
        trainedTitle.fontStyle = FontStyles.Bold;
        trainedTitle.color = new Color(0.72f, 0.4f, 0.29f, 1f); // Terracotta
        if (interFont != null) trainedTitle.font = interFont;

        var trainedContainerGo = CreateUIObject("TrainedContainer", fullExtrasGo);
        var trainedContVlg = trainedContainerGo.AddComponent<VerticalLayoutGroup>();
        trainedContVlg.spacing = 10f;
        trainedContVlg.childControlWidth = true;
        trainedContVlg.childControlHeight = true;

        // 5. Related Movements Section (G06 Horizontal Cards)
        var relatedGroupGo = CreateUIObject("RelatedGroup", contentGo);
        var relVlg = relatedGroupGo.AddComponent<VerticalLayoutGroup>();
        relVlg.spacing = 15f;
        relVlg.childControlWidth = true;
        relVlg.childControlHeight = true;

        var relatedTitleGo = CreateUIObject("Title", relatedGroupGo);
        var relatedTitle = relatedTitleGo.AddComponent<TextMeshProUGUI>();
        relatedTitle.enableWordWrapping = true;
        relatedTitle.overflowMode = TMPro.TextOverflowModes.Overflow;
        relatedTitle.text = "GERAKAN SERUPA";
        relatedTitle.fontSize = 6.7f;
        relatedTitle.fontStyle = FontStyles.Bold;
        relatedTitle.color = new Color(0.72f, 0.4f, 0.29f, 1f); // Terracotta
        if (interFont != null) relatedTitle.font = interFont;

        // Horizontal scroll container for related cards
        var relatedScrollViewGo = CreateUIObject("RelatedScrollView", relatedGroupGo);
        var relScroll = relatedScrollViewGo.AddComponent<ScrollRect>();
        relScroll.horizontal = true;
        relScroll.vertical = false;
        relatedScrollViewGo.GetComponent<RectTransform>().sizeDelta = new Vector2(900f, 200f);

        var relViewportGo = CreateUIObject("Viewport", relatedScrollViewGo);
        relViewportGo.AddComponent<RectMask2D>();
        StretchRect(relViewportGo.GetComponent<RectTransform>());
        relScroll.viewport = relViewportGo.GetComponent<RectTransform>();

        var relContentGo = CreateUIObject("Content", relViewportGo);
        var relContentRT = relContentGo.GetComponent<RectTransform>();
        relContentRT.anchorMin = new Vector2(0f, 0.5f);
        relContentRT.anchorMax = new Vector2(0f, 0.5f);
        relContentRT.pivot = new Vector2(0f, 0.5f);
        relContentRT.anchoredPosition = Vector2.zero;
        relContentRT.sizeDelta = new Vector2(800f, 180f); // dynamic width
        relScroll.content = relContentRT;

        var hlg = relContentGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        
        var relCsf = relContentGo.AddComponent<ContentSizeFitter>();
        relCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        relCsf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Sheet Controller & Scrim
        var scrimGo = CreateUIObject("Scrim", canvasGo);
        var scrimImg = scrimGo.AddComponent<Image>();
        
        scrimImg.color = new Color(0, 0, 0, 0.32f); // Camera Scrim opacity G06
        StretchRect(scrimGo.GetComponent<RectTransform>());
        scrimGo.AddComponent<Button>(); // Tap triggers close
        scrimGo.SetActive(false);

        // Move bottom sheet to front
        sheetGo.transform.SetAsLastSibling();

        var sheetCtrl = sheetGo.AddComponent<BottomSheetController>();
        var serialSheet = new SerializedObject(sheetCtrl);
        serialSheet.FindProperty("sheetRect").objectReferenceValue = sheetRT;
        serialSheet.FindProperty("scrim").objectReferenceValue = scrimGo;
        serialSheet.FindProperty("closeButton").objectReferenceValue = sheetCloseBtn;
        serialSheet.FindProperty("movementController").objectReferenceValue = movementController;
        serialSheet.ApplyModifiedProperties();

        // Material content controller
        var matCtrl = sheetGo.AddComponent<MaterialContentController>();
        var serialMat = new SerializedObject(matCtrl);
        
        // Load dynamically generated prefabs
        var stepItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/App/Prefabs/StepItem.prefab");
        var bulletItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/App/Prefabs/BulletItem.prefab");
        var relatedCardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/App/Prefabs/RelatedCard.prefab");

        serialMat.FindProperty("categoryTypeLabel").objectReferenceValue = categoryTxt;
        serialMat.FindProperty("movementNameText").objectReferenceValue = sheetTitleText;
        serialMat.FindProperty("backToPrimaryButton").objectReferenceValue = backBtn;
        serialMat.FindProperty("shortDescriptionText").objectReferenceValue = descText;
        serialMat.FindProperty("stepsContainer").objectReferenceValue = stepsContainerGo.transform;
        serialMat.FindProperty("stepItemPrefab").objectReferenceValue = stepItemPrefab;
        serialMat.FindProperty("safetyTipText").objectReferenceValue = safetyText;
        serialMat.FindProperty("fullStateExtras").objectReferenceValue = fullExtrasGo;
        serialMat.FindProperty("trainedAreasContainer").objectReferenceValue = trainedContainerGo.transform;
        serialMat.FindProperty("commonMistakesContainer").objectReferenceValue = mistakesContainerGo.transform;
        serialMat.FindProperty("bulletItemPrefab").objectReferenceValue = bulletItemPrefab;
        serialMat.FindProperty("relatedCardsContainer").objectReferenceValue = relContentGo.transform;
        serialMat.FindProperty("relatedCardPrefab").objectReferenceValue = relatedCardPrefab;
        serialMat.ApplyModifiedProperties();

        // Link ARUIController
        var serialUI = new SerializedObject(arUI);
        serialUI.FindProperty("scanOverlay").objectReferenceValue = scanGo;
        serialUI.FindProperty("scanLine").objectReferenceValue = scanLineGo;
        serialUI.FindProperty("detectionToast").objectReferenceValue = toastGo;
        serialUI.FindProperty("arControls").objectReferenceValue = arControlsGo;
        serialUI.FindProperty("movementNameLabel").objectReferenceValue = nameLabel;
        serialUI.FindProperty("closeButton").objectReferenceValue = closeBtn;
        serialUI.FindProperty("materialButton").objectReferenceValue = matBtn;
        serialUI.FindProperty("timelineRoot").objectReferenceValue = timelineRootGo;
        serialUI.FindProperty("playPauseButton").objectReferenceValue = playPauseBtn;
        serialUI.FindProperty("playPauseIcon").objectReferenceValue = playPauseImg;
        serialUI.ApplyModifiedProperties();

        // Finish provider wiring after the UI controllers have been created.
        serialTracking.Update();
        serialTracking.FindProperty("timelineController").objectReferenceValue = timelineCtrl;
        serialTracking.FindProperty("materialController").objectReferenceValue = matCtrl;
        serialTracking.FindProperty("uiController").objectReferenceValue = arUI;
        serialTracking.ApplyModifiedProperties();

        arRootGo.SetActive(true);
        camGo.SetActive(true);

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

    [MenuItem("Build/Validate ARUnityX Setup")]
    public static void ValidateSetup()
    {
        var controllers = Object.FindObjectsByType<ARXController>(FindObjectsInactive.Include);
        if (controllers.Length != 1 || controllers[0].enabled)
            throw new BuildFailedException("MainAR must contain exactly one disabled ARXController.");

        var trackables = Object.FindObjectsByType<ARXTrackable>(FindObjectsInactive.Include);
        if (trackables.Length != 1 || trackables[0].enabled ||
            trackables[0].Type != ARXTrackable.TrackableType.TwoD ||
            trackables[0].TwoDImageFile != "C5.png")
        {
            throw new BuildFailedException("MainAR C5 TwoD trackable is not configured correctly.");
        }

        if (!System.IO.File.Exists("Assets/StreamingAssets/C5.png"))
            throw new BuildFailedException("Assets/StreamingAssets/C5.png is missing.");

        if (Resources.Load<TextAsset>("ardata/camera_para") == null)
            throw new BuildFailedException("ARUnityX camera_para resource is missing.");

        MovementDatabase database = AssetDatabase.LoadAssetAtPath<MovementDatabase>(
            "Assets/App/Content/MovementData/MovementDatabase.asset");
        if (database == null || database.FindByReferenceImageName("squat_target") == null)
            throw new BuildFailedException("Squat movement mapping is missing.");

        if (GameObject.Find("XR Origin") != null || GameObject.Find("AR Session") != null)
            throw new BuildFailedException("AR Foundation objects remain in MainAR.");

        Debug.Log("[GerakAR] ARUnityX scene validation passed.");
    }

    private static void ConfigureRearCamera(ARXVideoConfig videoConfig)
    {
        int index = videoConfig.configs.FindIndex(config => config.platform == RuntimePlatform.Android);
        if (index < 0)
            throw new System.InvalidOperationException("ARUnityX Android video configuration is missing.");

        ARXVideoConfig.ARVideoPlatformConfig config = videoConfig.configs[index];
        config.module = ARXVideoConfig.ARVideoModule.Android;
        config.inputSelectionMethod = ARXVideoConfig.ARVideoConfigInputSelectionMethod.CameraAtPosition;
        config.position = ARXVideoConfig.AR_VIDEO_POSITION.AR_VIDEO_POSITION_BACK;
        config.width = 1280;
        config.height = 720;
        config.sizePreference = ARXVideoConfig.ARVideoSizeSelectionStrategySizePreference.closestpixelcount;
        config.isUsingManualConfig = false;
        config.isUsingUnityVideoSource = false;
        config.unityVideoSource = ARXVideoConfig.ARVideoUnityVideoSource.None;
        videoConfig.configs[index] = config;
    }

    private static void BuildAPK()
    {
        Debug.Log("[GerakAR] Menjalankan build Android APK...");
        ConfigurePlayerSettings();
        
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

    private static void SetAnchorTopLeft(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    private static void SetAnchorTopRight(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    private static void SetAnchorTop(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    private static void SetAnchorBottom(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    private static void SetAnchorBottomRight(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    private static void SetBottomSheetPosition(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 1f); // Pivot at the top-center!
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
