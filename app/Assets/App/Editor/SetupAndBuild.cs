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
using UnityEngine.InputSystem.UI;

public static class SetupAndBuild
{
    private static readonly Color ColorCleanOffWhite = new Color(0.98f, 0.976f, 0.965f, 1f); // #FAF9F6
    private static readonly Color ColorDeepForest = new Color(0.07f, 0.216f, 0.165f, 1f);   // #12372A
    private static readonly Color ColorForestGreen = new Color(0.12f, 0.365f, 0.259f, 1f);  // #1F5D42
    private static readonly Color ColorSoftSage = new Color(0.66f, 0.745f, 0.635f, 1f);     // #A9BEA2
    private static readonly Color ColorCharcoal = new Color(0.125f, 0.149f, 0.125f, 1f);    // #202620
    private static readonly Color ColorMossGreen = new Color(0.376f, 0.490f, 0.310f, 1f);   // #607D4F

    [MenuItem("GerakAR/Setup Scenes and Build APK")]
    public static void ExecuteSetupAndBuild()
    {
        Debug.Log("[GerakAR] Memulai setup scene...");
        
        // Generate dynamic UI prefabs for Bottom Sheet populating
        var interFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/MobileARTemplateAssets/UI/Fonts/Inter-Regular_SDF.asset");
        var btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MobileARTemplateAssets/UI/Sprites/ActivationButtonOpaque.png");
        GeneratePrefabs(interFont, btnSprite);

        CreateBootstrapScene();
        CreateMainARScene();
        UpdateBuildSettings();
        BuildAPK();
    }

    private static void GeneratePrefabs(TMP_FontAsset interFont, Sprite btnSprite)
    {
        System.IO.Directory.CreateDirectory("Assets/App/Prefabs");

        // 1. StepItem prefab
        var stepGo = new GameObject("StepItem", typeof(RectTransform));
        var stepText = stepGo.AddComponent<TextMeshProUGUI>();
        stepText.fontSize = 24;
        stepText.color = ColorCharcoal;
        if (interFont != null) stepText.font = interFont;
        stepGo.GetComponent<RectTransform>().sizeDelta = new Vector2(880f, 60f);
        PrefabUtility.SaveAsPrefabAsset(stepGo, "Assets/App/Prefabs/StepItem.prefab");
        Object.DestroyImmediate(stepGo);

        // 2. BulletItem prefab
        var bulletGo = new GameObject("BulletItem", typeof(RectTransform));
        var bulletText = bulletGo.AddComponent<TextMeshProUGUI>();
        bulletText.fontSize = 22;
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
        thumbImg.color = ColorSoftSage;
        SetCenterPosition(thumbGo.GetComponent<RectTransform>(), 0f, 25f, 200f, 100f);

        var titleGo = CreateUIObject("Title", cardGo);
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.text = "Related";
        titleText.fontSize = 18;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = ColorCharcoal;
        titleText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) titleText.font = interFont;
        SetCenterPosition(titleGo.GetComponent<RectTransform>(), 0f, -50f, 200f, 40f);

        PrefabUtility.SaveAsPrefabAsset(cardGo, "Assets/App/Prefabs/RelatedCard.prefab");
        Object.DestroyImmediate(cardGo);
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
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
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
        introMetaText.text = "<b>Media Pembelajaran</b>\n<color=#A9BEA2>Skripsi Pendidikan SD</color>";
        introMetaText.fontSize = 28;
        introMetaText.color = Color.white;
        introMetaText.alignment = TextAlignmentOptions.Left;
        if (interFont != null) introMetaText.font = interFont;
        SetCenterPosition(introMetaGo.GetComponent<RectTransform>(), -260f, 780f, 480f, 100f);

        // G01 Header Kanan - Stylized UNP Logo Badge (Gold Circle with UNP text)
        var unpBadgeGo = CreateUIObject("UNPBadge", introGo);
        var unpBadgeImg = unpBadgeGo.AddComponent<Image>();
        unpBadgeImg.sprite = btnSprite;
        unpBadgeImg.type = Image.Type.Sliced;
        unpBadgeImg.color = new Color(0.957f, 0.729f, 0.094f, 1f); // Gold #F4BA18
        SetCenterPosition(unpBadgeGo.GetComponent<RectTransform>(), 400f, 780f, 100f, 100f);

        var unpTextGo = CreateUIObject("Text", unpBadgeGo);
        var unpText = unpTextGo.AddComponent<TextMeshProUGUI>();
        unpText.text = "<b>UNP</b>";
        unpText.fontSize = 24;
        unpText.color = new Color(0.05f, 0.29f, 0.54f, 1f); // Blue #0D4B8A
        unpText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) unpText.font = interFont;
        StretchRect(unpTextGo.GetComponent<RectTransform>());

        // G01 Middle Graphic - Soft Sage Cube/Silhouette
        var introCenterGo = CreateUIObject("CenterGraphic", introGo);
        var centerImg = introCenterGo.AddComponent<Image>();
        var cubeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MobileARTemplateAssets/UI/Sprites/Icon-Cube.png");
        if (cubeSprite != null) centerImg.sprite = cubeSprite;
        centerImg.color = ColorSoftSage;
        SetCenterPosition(introCenterGo.GetComponent<RectTransform>(), 0f, 150f, 220f, 220f);

        // G01 Bottom Brand - Title & Progress Bar
        var brandGroupGo = CreateUIObject("BrandGroup", introGo);
        SetCenterPosition(brandGroupGo.GetComponent<RectTransform>(), 0f, -500f, 900f, 320f);

        var titleGo = CreateUIObject("TitleText", brandGroupGo);
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.text = "GerakAR";
        titleText.fontSize = 72;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) titleText.font = interFont;
        SetCenterPosition(titleGo.GetComponent<RectTransform>(), 0f, 120f, 800f, 100f);

        var subtitleGo = CreateUIObject("SubtitleText", brandGroupGo);
        var subtitleText = subtitleGo.AddComponent<TextMeshProUGUI>();
        subtitleText.text = "Belajar Gerak Jadi Seru";
        subtitleText.fontSize = 28;
        subtitleText.color = ColorSoftSage;
        subtitleText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) subtitleText.font = interFont;
        SetCenterPosition(subtitleGo.GetComponent<RectTransform>(), 0f, 40f, 800f, 50f);

        // G01 Loading progress bar
        var progressTrackGo = CreateUIObject("ProgressTrack", brandGroupGo);
        var trackImg = progressTrackGo.AddComponent<Image>();
        trackImg.sprite = btnSprite;
        trackImg.type = Image.Type.Sliced;
        trackImg.color = new Color(0.12f, 0.365f, 0.259f, 0.3f); // #1F5D42 with 30% opacity
        SetCenterPosition(progressTrackGo.GetComponent<RectTransform>(), 0f, -80f, 400f, 12f);

        var progressFillGo = CreateUIObject("ProgressFill", progressTrackGo);
        var fillImg = progressFillGo.AddComponent<Image>();
        fillImg.sprite = btnSprite;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
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
        SetCenterPosition(safetyHeaderGo.GetComponent<RectTransform>(), 0f, 600f, 900f, 320f);

        var checkIconCircleGo = CreateUIObject("CheckCircle", safetyHeaderGo);
        var circleImg = checkIconCircleGo.AddComponent<Image>();
        circleImg.sprite = btnSprite;
        circleImg.type = Image.Type.Sliced;
        circleImg.color = new Color(0.66f, 0.745f, 0.635f, 0.3f); // Soft Sage 30%
        SetCenterPosition(checkIconCircleGo.GetComponent<RectTransform>(), 0f, 100f, 120f, 120f);

        var checkIconTextGo = CreateUIObject("Text", checkIconCircleGo);
        var checkIconText = checkIconTextGo.AddComponent<TextMeshProUGUI>();
        checkIconText.text = "✔";
        checkIconText.fontSize = 44;
        checkIconText.color = ColorDeepForest;
        checkIconText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) checkIconText.font = interFont;
        StretchRect(checkIconTextGo.GetComponent<RectTransform>());

        var obTitleGo = CreateUIObject("OnboardingTitle", safetyHeaderGo);
        var obTitle = obTitleGo.AddComponent<TextMeshProUGUI>();
        obTitle.text = "Sebelum Mulai";
        obTitle.fontSize = 48;
        obTitle.fontStyle = FontStyles.Bold;
        obTitle.color = ColorDeepForest;
        obTitle.alignment = TextAlignmentOptions.Center;
        if (interFont != null) obTitle.font = interFont;
        SetCenterPosition(obTitleGo.GetComponent<RectTransform>(), 0f, -10f, 800f, 60f);

        var obSubtitleGo = CreateUIObject("OnboardingSubtitle", safetyHeaderGo);
        var obSubtitle = obSubtitleGo.AddComponent<TextMeshProUGUI>();
        obSubtitle.text = "Ayo bergerak dengan aman dan nyaman.";
        obSubtitle.fontSize = 24;
        obSubtitle.color = ColorCharcoal;
        obSubtitle.alignment = TextAlignmentOptions.Center;
        if (interFont != null) obSubtitle.font = interFont;
        SetCenterPosition(obSubtitleGo.GetComponent<RectTransform>(), 0f, -70f, 800f, 50f);

        // G02 List Cards (1, 2, 3)
        var obListGo = CreateUIObject("InstructionList", onboardGo);
        SetCenterPosition(obListGo.GetComponent<RectTransform>(), 0f, 20f, 960f, 520f);

        // Card 1
        var card1Go = CreateUIObject("Card1", obListGo);
        var card1Img = card1Go.AddComponent<Image>();
        card1Img.sprite = btnSprite;
        card1Img.type = Image.Type.Sliced;
        card1Img.color = Color.white;
        SetCenterPosition(card1Go.GetComponent<RectTransform>(), 0f, 160f, 920f, 140f);

        var num1CircleGo = CreateUIObject("NumCircle", card1Go);
        var num1CircleImg = num1CircleGo.AddComponent<Image>();
        num1CircleImg.sprite = btnSprite;
        num1CircleImg.type = Image.Type.Sliced;
        num1CircleImg.color = ColorForestGreen;
        SetCenterPosition(num1CircleGo.GetComponent<RectTransform>(), -380f, 0f, 64f, 64f);

        var num1TextGo = CreateUIObject("Text", num1CircleGo);
        var num1Text = num1TextGo.AddComponent<TextMeshProUGUI>();
        num1Text.text = "1";
        num1Text.fontSize = 28;
        num1Text.fontStyle = FontStyles.Bold;
        num1Text.color = Color.white;
        num1Text.alignment = TextAlignmentOptions.Center;
        if (interFont != null) num1Text.font = interFont;
        StretchRect(num1TextGo.GetComponent<RectTransform>());

        var desc1Go = CreateUIObject("Desc", card1Go);
        var desc1 = desc1Go.AddComponent<TextMeshProUGUI>();
        desc1.text = "Gunakan di tempat yang cukup luas.";
        desc1.fontSize = 28;
        desc1.color = ColorCharcoal;
        desc1.alignment = TextAlignmentOptions.Left;
        if (interFont != null) desc1.font = interFont;
        SetCenterPosition(desc1Go.GetComponent<RectTransform>(), 90f, 0f, 720f, 80f);

        // Card 2
        var card2Go = CreateUIObject("Card2", obListGo);
        var card2Img = card2Go.AddComponent<Image>();
        card2Img.sprite = btnSprite;
        card2Img.type = Image.Type.Sliced;
        card2Img.color = Color.white;
        SetCenterPosition(card2Go.GetComponent<RectTransform>(), 0f, 0f, 920f, 140f);

        var num2CircleGo = CreateUIObject("NumCircle", card2Go);
        var num2CircleImg = num2CircleGo.AddComponent<Image>();
        num2CircleImg.sprite = btnSprite;
        num2CircleImg.type = Image.Type.Sliced;
        num2CircleImg.color = ColorForestGreen;
        SetCenterPosition(num2CircleGo.GetComponent<RectTransform>(), -380f, 0f, 64f, 64f);

        var num2TextGo = CreateUIObject("Text", num2CircleGo);
        var num2Text = num2TextGo.AddComponent<TextMeshProUGUI>();
        num2Text.text = "2";
        num2Text.fontSize = 28;
        num2Text.fontStyle = FontStyles.Bold;
        num2Text.color = Color.white;
        num2Text.alignment = TextAlignmentOptions.Center;
        if (interFont != null) num2Text.font = interFont;
        StretchRect(num2TextGo.GetComponent<RectTransform>());

        var desc2Go = CreateUIObject("Desc", card2Go);
        var desc2 = desc2Go.AddComponent<TextMeshProUGUI>();
        desc2.text = "Minta guru atau orang tua mendampingi.";
        desc2.fontSize = 28;
        desc2.color = ColorCharcoal;
        desc2.alignment = TextAlignmentOptions.Left;
        if (interFont != null) desc2.font = interFont;
        SetCenterPosition(desc2Go.GetComponent<RectTransform>(), 90f, 0f, 720f, 80f);

        // Card 3
        var card3Go = CreateUIObject("Card3", obListGo);
        var card3Img = card3Go.AddComponent<Image>();
        card3Img.sprite = btnSprite;
        card3Img.type = Image.Type.Sliced;
        card3Img.color = Color.white;
        SetCenterPosition(card3Go.GetComponent<RectTransform>(), 0f, -160f, 920f, 140f);

        var num3CircleGo = CreateUIObject("NumCircle", card3Go);
        var num3CircleImg = num3CircleGo.AddComponent<Image>();
        num3CircleImg.sprite = btnSprite;
        num3CircleImg.type = Image.Type.Sliced;
        num3CircleImg.color = ColorForestGreen;
        SetCenterPosition(num3CircleGo.GetComponent<RectTransform>(), -380f, 0f, 64f, 64f);

        var num3TextGo = CreateUIObject("Text", num3CircleGo);
        var num3Text = num3TextGo.AddComponent<TextMeshProUGUI>();
        num3Text.text = "3";
        num3Text.fontSize = 28;
        num3Text.fontStyle = FontStyles.Bold;
        num3Text.color = Color.white;
        num3Text.alignment = TextAlignmentOptions.Center;
        if (interFont != null) num3Text.font = interFont;
        StretchRect(num3TextGo.GetComponent<RectTransform>());

        var desc3Go = CreateUIObject("Desc", card3Go);
        var desc3 = desc3Go.AddComponent<TextMeshProUGUI>();
        desc3.text = "Izinkan kamera untuk melihat gerakan.";
        desc3.fontSize = 28;
        desc3.color = ColorCharcoal;
        desc3.alignment = TextAlignmentOptions.Left;
        if (interFont != null) desc3.font = interFont;
        SetCenterPosition(desc3Go.GetComponent<RectTransform>(), 90f, 0f, 720f, 80f);

        // G02 Bottom Buttons Group
        var btnGroupGo = CreateUIObject("ButtonGroup", onboardGo);
        SetCenterPosition(btnGroupGo.GetComponent<RectTransform>(), 0f, -500f, 960f, 320f);

        // Tombol MULAI
        var startBtnGo = CreateUIObject("MulaiButton", btnGroupGo);
        var startBtnImg = startBtnGo.AddComponent<Image>();
        startBtnImg.sprite = btnSprite;
        startBtnImg.type = Image.Type.Sliced;
        startBtnImg.color = ColorForestGreen;
        var startBtn = startBtnGo.AddComponent<Button>();
        startBtnGo.AddComponent<GerakAR.UI.OnboardingButtonWirer>();
        SetCenterPosition(startBtnGo.GetComponent<RectTransform>(), 0f, 60f, 920f, 100f);

        var btnTextGo = CreateUIObject("Text", startBtnGo);
        var btnText = btnTextGo.AddComponent<TextMeshProUGUI>();
        btnText.text = "MULAI";
        btnText.fontSize = 36;
        btnText.fontStyle = FontStyles.Bold;
        btnText.color = Color.white;
        btnText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) btnText.font = interFont;
        StretchRect(btnTextGo.GetComponent<RectTransform>());

        // Fallback Simulasi Links
        var fallbackLinksGo = CreateUIObject("FallbackLinks", btnGroupGo);
        SetCenterPosition(fallbackLinksGo.GetComponent<RectTransform>(), 0f, -60f, 920f, 50f);

        var nonARLinkGo = CreateUIObject("NonARLink", fallbackLinksGo);
        var nonARLinkText = nonARLinkGo.AddComponent<TextMeshProUGUI>();
        nonARLinkText.text = "<u>Simulasi Non-AR</u>";
        nonARLinkText.fontSize = 22;
        nonARLinkText.color = ColorForestGreen;
        nonARLinkText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) nonARLinkText.font = interFont;
        var nonARBtn = nonARLinkGo.AddComponent<Button>();
        SetCenterPosition(nonARLinkGo.GetComponent<RectTransform>(), -220f, 0f, 340f, 50f);

        var camErrorLinkGo = CreateUIObject("CameraErrorLink", fallbackLinksGo);
        var camErrorLinkText = camErrorLinkGo.AddComponent<TextMeshProUGUI>();
        camErrorLinkText.text = "<u>Simulasi Kendala Kamera</u>";
        camErrorLinkText.fontSize = 22;
        camErrorLinkText.color = ColorDeepForest;
        camErrorLinkText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) camErrorLinkText.font = interFont;
        var camErrorBtn = camErrorLinkGo.AddComponent<Button>();
        SetCenterPosition(camErrorLinkGo.GetComponent<RectTransform>(), 220f, 0f, 340f, 50f);

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
        SetCenterPosition(nonARHeaderGo.GetComponent<RectTransform>(), 0f, 750f, 960f, 160f);

        var nonARTitleGo = CreateUIObject("Title", nonARHeaderGo);
        var nonARTitle = nonARTitleGo.AddComponent<TextMeshProUGUI>();
        nonARTitle.text = "GerakAR";
        nonARTitle.fontSize = 44;
        nonARTitle.fontStyle = FontStyles.Bold;
        nonARTitle.color = ColorDeepForest;
        nonARTitle.alignment = TextAlignmentOptions.Left;
        if (interFont != null) nonARTitle.font = interFont;
        SetCenterPosition(nonARTitleGo.GetComponent<RectTransform>(), -220f, 10f, 440f, 60f);

        var nonARSubGo = CreateUIObject("Sub", nonARHeaderGo);
        var nonARSub = nonARSubGo.AddComponent<TextMeshProUGUI>();
        nonARSub.text = "MODE PEMBELAJARAN MANDIRI";
        nonARSub.fontSize = 18;
        nonARSub.fontStyle = FontStyles.Bold;
        nonARSub.color = ColorForestGreen;
        nonARSub.alignment = TextAlignmentOptions.Left;
        if (interFont != null) nonARSub.font = interFont;
        SetCenterPosition(nonARSubGo.GetComponent<RectTransform>(), -220f, -40f, 440f, 30f);

        var nonARBadgeGo = CreateUIObject("Badge", nonARHeaderGo);
        var badgeImg = nonARBadgeGo.AddComponent<Image>();
        badgeImg.sprite = btnSprite;
        badgeImg.type = Image.Type.Sliced;
        badgeImg.color = new Color(0.72f, 0.4f, 0.29f, 0.1f); // Light Terracotta/Amber
        SetCenterPosition(nonARBadgeGo.GetComponent<RectTransform>(), 320f, 0f, 240f, 70f);

        var badgeTextGo = CreateUIObject("Text", nonARBadgeGo);
        var badgeText = badgeTextGo.AddComponent<TextMeshProUGUI>();
        badgeText.text = "NON-AR MODE";
        badgeText.fontSize = 20;
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
        SetCenterPosition(warnBannerGo.GetComponent<RectTransform>(), 0f, 540f, 920f, 180f);

        var warnIconGo = CreateUIObject("Icon", warnBannerGo);
        var warnIcon = warnIconGo.AddComponent<TextMeshProUGUI>();
        warnIcon.text = "⚠️";
        warnIcon.fontSize = 48;
        warnIcon.alignment = TextAlignmentOptions.Center;
        SetCenterPosition(warnIconGo.GetComponent<RectTransform>(), -380f, 0f, 100f, 100f);

        var warnTextGo = CreateUIObject("Text", warnBannerGo);
        var warnText = warnTextGo.AddComponent<TextMeshProUGUI>();
        warnText.text = "<b>Perangkat Belum Mendukung AR</b>\n<size=22>Kamu diarahkan langsung untuk membaca modul materi edukasi secara lengkap tanpa fitur kamera.</size>";
        warnText.fontSize = 26;
        warnText.color = new Color(0.72f, 0.4f, 0.29f, 1f); // Terracotta
        warnText.alignment = TextAlignmentOptions.Left;
        if (interFont != null) warnText.font = interFont;
        SetCenterPosition(warnTextGo.GetComponent<RectTransform>(), 70f, 0f, 720f, 120f);

        // G08 Catalog Content
        var catalogCatalogGo = CreateUIObject("CatalogCatalog", nonARModePanelGo);
        SetCenterPosition(catalogCatalogGo.GetComponent<RectTransform>(), 0f, 40f, 920f, 620f);

        var catTitleGo = CreateUIObject("CatTitleText", catalogCatalogGo);
        var catTitle = catTitleGo.AddComponent<TextMeshProUGUI>();
        catTitle.text = "KATALOG GERAKAN SISWA";
        catTitle.fontSize = 20;
        catTitle.fontStyle = FontStyles.Bold;
        catTitle.color = ColorDeepForest;
        catTitle.alignment = TextAlignmentOptions.Left;
        if (interFont != null) catTitle.font = interFont;
        SetCenterPosition(catTitleGo.GetComponent<RectTransform>(), 0f, 260f, 900f, 40f);

        // Catalogue Card Squat
        var cardSquatGo = CreateUIObject("CardSquat", catalogCatalogGo);
        var squatImg = cardSquatGo.AddComponent<Image>();
        squatImg.sprite = btnSprite;
        squatImg.type = Image.Type.Sliced;
        squatImg.color = Color.white;
        SetCenterPosition(cardSquatGo.GetComponent<RectTransform>(), 0f, 120f, 900f, 160f);

        var squatIconGo = CreateUIObject("Icon", cardSquatGo);
        var squatIcon = squatIconGo.AddComponent<TextMeshProUGUI>();
        squatIcon.text = "🦵";
        squatIcon.fontSize = 44;
        squatIcon.alignment = TextAlignmentOptions.Center;
        SetCenterPosition(squatIconGo.GetComponent<RectTransform>(), -380f, 0f, 80f, 80f);

        var squatTitleGo = CreateUIObject("TitleText", cardSquatGo);
        var squatTitle = squatTitleGo.AddComponent<TextMeshProUGUI>();
        squatTitle.text = "<b>Gerakan Squat</b>\n<size=20><color=#607D4F>Melatih otot paha dan sendi lutut</color></size>";
        squatTitle.fontSize = 28;
        squatTitle.color = ColorDeepForest;
        squatTitle.alignment = TextAlignmentOptions.Left;
        if (interFont != null) squatTitle.font = interFont;
        SetCenterPosition(squatTitleGo.GetComponent<RectTransform>(), 80f, 0f, 540f, 100f);

        var squatBukaGo = CreateUIObject("BukaButton", cardSquatGo);
        var squatBukaImg = squatBukaGo.AddComponent<Image>();
        squatBukaImg.sprite = btnSprite;
        squatBukaImg.type = Image.Type.Sliced;
        squatBukaImg.color = ColorForestGreen;
        var squatBukaBtn = squatBukaGo.AddComponent<Button>();
        SetCenterPosition(squatBukaGo.GetComponent<RectTransform>(), 360f, 0f, 140f, 70f);

        var squatBukaTextGo = CreateUIObject("Text", squatBukaGo);
        var squatBukaText = squatBukaTextGo.AddComponent<TextMeshProUGUI>();
        squatBukaText.text = "Buka";
        squatBukaText.fontSize = 22;
        squatBukaText.fontStyle = FontStyles.Bold;
        squatBukaText.color = Color.white;
        squatBukaText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) squatBukaText.font = interFont;
        StretchRect(squatBukaTextGo.GetComponent<RectTransform>());

        // Card Jumping Jack (Locked)
        var cardJackGo = CreateUIObject("CardJacks", catalogCatalogGo);
        var jackImg = cardJackGo.AddComponent<Image>();
        jackImg.sprite = btnSprite;
        jackImg.type = Image.Type.Sliced;
        jackImg.color = new Color(1f, 1f, 1f, 0.6f);
        SetCenterPosition(cardJackGo.GetComponent<RectTransform>(), 0f, -50f, 900f, 160f);

        var jackIconGo = CreateUIObject("Icon", cardJackGo);
        var jackIcon = jackIconGo.AddComponent<TextMeshProUGUI>();
        jackIcon.text = "🤸";
        jackIcon.fontSize = 44;
        jackIcon.alignment = TextAlignmentOptions.Center;
        SetCenterPosition(jackIconGo.GetComponent<RectTransform>(), -380f, 0f, 80f, 80f);

        var jackTitleGo = CreateUIObject("TitleText", cardJackGo);
        var jackTitle = jackTitleGo.AddComponent<TextMeshProUGUI>();
        jackTitle.text = "<b>Jumping Jacks</b>\n<size=20><color=#607D4F>Melatih kardio & koordinasi tubuh</color></size>";
        jackTitle.fontSize = 28;
        jackTitle.color = new Color(0.07f, 0.216f, 0.165f, 0.8f);
        jackTitle.alignment = TextAlignmentOptions.Left;
        if (interFont != null) jackTitle.font = interFont;
        SetCenterPosition(jackTitleGo.GetComponent<RectTransform>(), 80f, 0f, 540f, 100f);

        var jackLockGo = CreateUIObject("BukaButton", cardJackGo);
        var jackLockImg = jackLockGo.AddComponent<Image>();
        jackLockImg.sprite = btnSprite;
        jackLockImg.type = Image.Type.Sliced;
        jackLockImg.color = new Color(0.8f, 0.8f, 0.8f, 0.7f);
        SetCenterPosition(jackLockGo.GetComponent<RectTransform>(), 360f, 0f, 140f, 70f);

        var jackLockTextGo = CreateUIObject("Text", jackLockGo);
        var jackLockText = jackLockTextGo.AddComponent<TextMeshProUGUI>();
        jackLockText.text = "Kunci";
        jackLockText.fontSize = 22;
        jackLockText.fontStyle = FontStyles.Bold;
        jackLockText.color = Color.white;
        jackLockText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) jackLockText.font = interFont;
        StretchRect(jackLockTextGo.GetComponent<RectTransform>());

        // G08 Back Button
        var catalogBackGo = CreateUIObject("CatalogBackButton", nonARModePanelGo);
        var catalogBackText = catalogBackGo.AddComponent<TextMeshProUGUI>();
        catalogBackText.text = "← Petunjuk";
        catalogBackText.fontSize = 24;
        catalogBackText.fontStyle = FontStyles.Bold;
        catalogBackText.color = ColorForestGreen;
        catalogBackText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) catalogBackText.font = interFont;
        var catalogBackBtn = catalogBackGo.AddComponent<Button>();
        SetCenterPosition(catalogBackGo.GetComponent<RectTransform>(), -320f, -800f, 220f, 60f);

        // G09 view (Kamera Belum Aktif)
        var cameraErrorPanelGo = CreateUIObject("CameraErrorPanel", unsupGo);
        StretchRect(cameraErrorPanelGo.GetComponent<RectTransform>());
        cameraErrorPanelGo.SetActive(false);

        var camOffIconGo = CreateUIObject("CamOffIcon", cameraErrorPanelGo);
        var camOffIcon = camOffIconGo.AddComponent<TextMeshProUGUI>();
        camOffIcon.text = "📷🚫";
        camOffIcon.fontSize = 90;
        camOffIcon.alignment = TextAlignmentOptions.Center;
        SetCenterPosition(camOffIconGo.GetComponent<RectTransform>(), 0f, 220f, 200f, 200f);

        var camErrorTitleGo = CreateUIObject("Title", cameraErrorPanelGo);
        var camErrorTitle = camErrorTitleGo.AddComponent<TextMeshProUGUI>();
        camErrorTitle.text = "Kamera Belum Aktif";
        camErrorTitle.fontSize = 44;
        camErrorTitle.fontStyle = FontStyles.Bold;
        camErrorTitle.color = ColorDeepForest;
        camErrorTitle.alignment = TextAlignmentOptions.Center;
        if (interFont != null) camErrorTitle.font = interFont;
        SetCenterPosition(camErrorTitleGo.GetComponent<RectTransform>(), 0f, 80f, 800f, 60f);

        var camErrorDescGo = CreateUIObject("Desc", cameraErrorPanelGo);
        var camErrorDesc = camErrorDescGo.AddComponent<TextMeshProUGUI>();
        camErrorDesc.text = "Izinkan akses kamera agar GerakAR dapat melihat gambar gerakan.";
        camErrorDesc.fontSize = 26;
        camErrorDesc.color = ColorCharcoal;
        camErrorDesc.alignment = TextAlignmentOptions.Center;
        if (interFont != null) camErrorDesc.font = interFont;
        SetCenterPosition(camErrorDescGo.GetComponent<RectTransform>(), 0f, -20f, 760f, 120f);

        // G09 BUKA PENGATURAN Button
        var settingsGo = CreateUIObject("SettingsButton", cameraErrorPanelGo);
        var settingsImg = settingsGo.AddComponent<Image>();
        settingsImg.sprite = btnSprite;
        settingsImg.type = Image.Type.Sliced;
        settingsImg.color = ColorForestGreen;
        var settingsBtn = settingsGo.AddComponent<Button>();
        SetCenterPosition(settingsGo.GetComponent<RectTransform>(), 0f, -200f, 800f, 100f);

        var settingsTextGo = CreateUIObject("Text", settingsGo);
        var settingsText = settingsTextGo.AddComponent<TextMeshProUGUI>();
        settingsText.text = "BUKA PENGATURAN";
        settingsText.fontSize = 30;
        settingsText.fontStyle = FontStyles.Bold;
        settingsText.color = Color.white;
        settingsText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) settingsText.font = interFont;
        StretchRect(settingsTextGo.GetComponent<RectTransform>());

        // G09 Coba Lagi Button
        var retryGo = CreateUIObject("RetryButton", cameraErrorPanelGo);
        var retryBtn = retryGo.AddComponent<Button>();
        SetCenterPosition(retryGo.GetComponent<RectTransform>(), 0f, -320f, 800f, 80f);

        var retryTextGo = CreateUIObject("Text", retryGo);
        var retryText = retryTextGo.AddComponent<TextMeshProUGUI>();
        retryText.text = "Coba Lagi";
        retryText.fontSize = 28;
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
        serialBtn.FindProperty("catalogBackBtn").objectReferenceValue = catalogBackBtn;
        serialBtn.FindProperty("settingsBtn").objectReferenceValue = settingsBtn;
        serialBtn.FindProperty("retryBtn").objectReferenceValue = retryBtn;
        serialBtn.ApplyModifiedProperties();

        var serialChecker = new SerializedObject(arChecker);
        serialChecker.FindProperty("unsupportedPanel").objectReferenceValue = unsupGo;
        serialChecker.FindProperty("unsupportedMessageText").objectReferenceValue = null; // Controlled by BootstrapUIController
        serialChecker.ApplyModifiedProperties();

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

        // CameraOffset GameObject (required by XROrigin for floor offset)
        var cameraOffsetGo = new GameObject("Camera Offset");
        cameraOffsetGo.transform.SetParent(originGo.transform, false);

        // AR Camera sebagai child dari CameraOffset
        var camGo = new GameObject("AR Camera");
        camGo.transform.SetParent(cameraOffsetGo.transform, false);
        var cam = camGo.AddComponent<Camera>();
        camGo.AddComponent<ARCameraManager>();
        camGo.AddComponent<ARCameraBackground>();
        camGo.tag = "MainCamera";

        // TrackedPoseDriver (Input System) - WAJIB agar kamera bisa tracking AR
        // AR Foundation akan mengisi binding secara otomatis saat runtime
        camGo.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();

        // Link XROrigin ke camera dan offset
        var serialOrigin = new SerializedObject(origin);
        serialOrigin.FindProperty("m_Camera").objectReferenceValue = cam;
        serialOrigin.FindProperty("m_CameraFloorOffsetObject").objectReferenceValue = cameraOffsetGo;
        serialOrigin.ApplyModifiedProperties();

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
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
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
        scanTitle.text = "GerakAR";
        scanTitle.fontSize = 44;
        scanTitle.fontStyle = FontStyles.Bold;
        scanTitle.color = Color.white;
        scanTitle.alignment = TextAlignmentOptions.Center;
        if (interFont != null) scanTitle.font = interFont;
        SetCenterPosition(scanTitleGo.GetComponent<RectTransform>(), 0f, 750f, 600f, 60f);

        var scanSubGo = CreateUIObject("HeaderSub", scanGo);
        var scanSub = scanSubGo.AddComponent<TextMeshProUGUI>();
        scanSub.text = "Belajar Gerak Jadi Seru";
        scanSub.fontSize = 22;
        scanSub.color = ColorSoftSage;
        scanSub.alignment = TextAlignmentOptions.Center;
        if (interFont != null) scanSub.font = interFont;
        SetCenterPosition(scanSubGo.GetComponent<RectTransform>(), 0f, 700f, 600f, 40f);

        // G03 Central Scan Guide Frame
        var scanFrameGo = CreateUIObject("ScanFrame", scanGo);
        var scanFrameImg = scanFrameGo.AddComponent<Image>();
        var frameSpriteAsset = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/DemoSceneAssets/Sprites/Round Radius 4 Outline.png");
        if (frameSpriteAsset != null) scanFrameImg.sprite = frameSpriteAsset;
        scanFrameImg.type = Image.Type.Sliced;
        scanFrameImg.color = ColorCleanOffWhite;
        SetCenterPosition(scanFrameGo.GetComponent<RectTransform>(), 0f, 80f, 480f, 480f);

        // G03 Scan Laser Line
        var scanLineGo = CreateUIObject("ScanLaserLine", scanFrameGo);
        var scanLineImg = scanLineGo.AddComponent<Image>();
        scanLineImg.sprite = btnSprite;
        scanLineImg.type = Image.Type.Sliced;
        scanLineImg.color = new Color(0.66f, 0.745f, 0.635f, 0.7f); // Glowing Soft Sage
        SetCenterPosition(scanLineGo.GetComponent<RectTransform>(), 0f, 0f, 440f, 8f);

        // G03 Scan Target Pill Below Frame
        var scanPillGo = CreateUIObject("ScanTargetPill", scanGo);
        var scanPillImg = scanPillGo.AddComponent<Image>();
        scanPillImg.sprite = btnSprite;
        scanPillImg.type = Image.Type.Sliced;
        scanPillImg.color = new Color(0f, 0f, 0f, 0.45f); // Black 45%
        SetCenterPosition(scanPillGo.GetComponent<RectTransform>(), 0f, -220f, 420f, 70f);

        var scanPillTextGo = CreateUIObject("Text", scanPillGo);
        var scanPillText = scanPillTextGo.AddComponent<TextMeshProUGUI>();
        scanPillText.text = "Pindai Target Gambar";
        scanPillText.fontSize = 22;
        scanPillText.fontStyle = FontStyles.Bold;
        scanPillText.color = ColorCleanOffWhite;
        scanPillText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) scanPillText.font = interFont;
        StretchRect(scanPillTextGo.GetComponent<RectTransform>());

        // G03 Bottom Instruction Card
        var instructionCardGo = CreateUIObject("InstructionCard", scanGo);
        var instCardImg = instructionCardGo.AddComponent<Image>();
        instCardImg.sprite = btnSprite;
        instCardImg.type = Image.Type.Sliced;
        instCardImg.color = ColorCleanOffWhite;
        SetCenterPosition(instructionCardGo.GetComponent<RectTransform>(), 0f, -540f, 720f, 130f);

        var instIconCircleGo = CreateUIObject("IconCircle", instructionCardGo);
        var instIconCircleImg = instIconCircleGo.AddComponent<Image>();
        instIconCircleImg.sprite = btnSprite;
        instIconCircleImg.type = Image.Type.Sliced;
        instIconCircleImg.color = new Color(0.66f, 0.745f, 0.635f, 0.2f); // Soft Sage 20%
        SetCenterPosition(instIconCircleGo.GetComponent<RectTransform>(), -280f, 0f, 70f, 70f);

        var instIconTextGo = CreateUIObject("Text", instIconCircleGo);
        var instIconText = instIconTextGo.AddComponent<TextMeshProUGUI>();
        instIconText.text = "📷";
        instIconText.fontSize = 28;
        instIconText.alignment = TextAlignmentOptions.Center;
        StretchRect(instIconTextGo.GetComponent<RectTransform>());

        var hintGo = CreateUIObject("HintText", instructionCardGo);
        var hintText = hintGo.AddComponent<TextMeshProUGUI>();
        hintText.text = "Arahkan kamera ke gambar gerakan";
        hintText.fontSize = 24;
        hintText.fontStyle = FontStyles.Bold;
        hintText.color = ColorCharcoal;
        hintText.alignment = TextAlignmentOptions.Left;
        if (interFont != null) hintText.font = interFont;
        SetCenterPosition(hintGo.GetComponent<RectTransform>(), 70f, 0f, 540f, 60f);

        var instSubtitleGo = CreateUIObject("InstructionSubtitle", scanGo);
        var instSubtitle = instSubtitleGo.AddComponent<TextMeshProUGUI>();
        instSubtitle.text = "Pastikan seluruh gambar terlihat";
        instSubtitle.fontSize = 22;
        instSubtitle.color = ColorCleanOffWhite;
        instSubtitle.alignment = TextAlignmentOptions.Center;
        if (interFont != null) instSubtitle.font = interFont;
        SetCenterPosition(instSubtitleGo.GetComponent<RectTransform>(), 0f, -620f, 700f, 40f);

        // 2. Detection Toast Panel (G04) - Centered success card
        var toastGo = CreateUIObject("DetectionToast", canvasGo);
        var toastImg = toastGo.AddComponent<Image>();
        toastImg.sprite = btnSprite;
        toastImg.type = Image.Type.Sliced;
        toastImg.color = ColorCleanOffWhite;
        SetCenterPosition(toastGo.GetComponent<RectTransform>(), 0f, 0f, 540f, 360f);

        var toastCircleGo = CreateUIObject("SuccessCircle", toastGo);
        var toastCircleImg = toastCircleGo.AddComponent<Image>();
        toastCircleImg.sprite = btnSprite;
        toastCircleImg.type = Image.Type.Sliced;
        toastCircleImg.color = ColorForestGreen;
        SetCenterPosition(toastCircleGo.GetComponent<RectTransform>(), 0f, 80f, 130f, 130f);

        var toastCheckGo = CreateUIObject("Text", toastCircleGo);
        var toastCheck = toastCheckGo.AddComponent<TextMeshProUGUI>();
        toastCheck.text = "✔";
        toastCheck.fontSize = 58;
        toastCheck.color = Color.white;
        toastCheck.alignment = TextAlignmentOptions.Center;
        if (interFont != null) toastCheck.font = interFont;
        StretchRect(toastCheckGo.GetComponent<RectTransform>());

        var toastTextGo = CreateUIObject("TitleText", toastGo);
        var toastText = toastTextGo.AddComponent<TextMeshProUGUI>();
        toastText.text = "Gerakan Ditemukan!";
        toastText.fontSize = 32;
        toastText.fontStyle = FontStyles.Bold;
        toastText.color = ColorDeepForest;
        toastText.alignment = TextAlignmentOptions.Center;
        if (interFont != null) toastText.font = interFont;
        SetCenterPosition(toastTextGo.GetComponent<RectTransform>(), 0f, -50f, 480f, 50f);

        var toastPillGo = CreateUIObject("MovementPill", toastGo);
        var toastPillImg = toastPillGo.AddComponent<Image>();
        toastPillImg.sprite = btnSprite;
        toastPillImg.type = Image.Type.Sliced;
        toastPillImg.color = new Color(0.72f, 0.4f, 0.29f, 0.1f); // Terracotta 10%
        SetCenterPosition(toastPillGo.GetComponent<RectTransform>(), 0f, -110f, 220f, 50f);

        var toastPillTextGo = CreateUIObject("Text", toastPillGo);
        var toastPillText = toastPillTextGo.AddComponent<TextMeshProUGUI>();
        toastPillText.text = "SQUAT";
        toastPillText.fontSize = 20;
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
        arHeaderTitle.text = "GerakAR";
        arHeaderTitle.fontSize = 44;
        arHeaderTitle.fontStyle = FontStyles.Bold;
        arHeaderTitle.color = Color.white;
        arHeaderTitle.alignment = TextAlignmentOptions.Center;
        if (interFont != null) arHeaderTitle.font = interFont;
        SetCenterPosition(arHeaderTitleGo.GetComponent<RectTransform>(), 0f, 750f, 600f, 60f);

        var arHeaderSubGo = CreateUIObject("HeaderSub", arControlsGo);
        var arHeaderSub = arHeaderSubGo.AddComponent<TextMeshProUGUI>();
        arHeaderSub.text = "Belajar Gerak Jadi Seru";
        arHeaderSub.fontSize = 22;
        arHeaderSub.color = ColorSoftSage;
        arHeaderSub.alignment = TextAlignmentOptions.Center;
        if (interFont != null) arHeaderSub.font = interFont;
        SetCenterPosition(arHeaderSubGo.GetComponent<RectTransform>(), 0f, 700f, 600f, 40f);

        // G05 Right Column of Floating Circular FABs
        var fabColumnGo = CreateUIObject("FABColumn", arControlsGo);
        SetAnchorRight(fabColumnGo.GetComponent<RectTransform>(), -40f, 150f, 120f, 380f);

        // FAB 1: Audio Play/Pause Button
        var playPauseGo = CreateUIObject("PlayPauseButton", fabColumnGo);
        var playPauseImg = playPauseGo.AddComponent<Image>();
        playPauseImg.sprite = btnSprite;
        playPauseImg.type = Image.Type.Sliced;
        playPauseImg.color = ColorDeepForest;
        var playPauseBtn = playPauseGo.AddComponent<Button>();
        SetCenterPosition(playPauseGo.GetComponent<RectTransform>(), 0f, 120f, 100f, 100f);

        var playPauseImgGo = CreateUIObject("Icon", playPauseGo);
        var playPauseImgComp = playPauseImgGo.AddComponent<Image>();
        var pauseSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/DemoSceneAssets/Sprites/Circle_60x60_Horizontal.png");
        var playSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/DemoSceneAssets/Sprites/Forward.png");
        if (pauseSprite != null) playPauseImgComp.sprite = pauseSprite;
        playPauseImgComp.color = ColorSoftSage;
        SetCenterPosition(playPauseImgGo.GetComponent<RectTransform>(), 0f, 12f, 44f, 44f);

        var playPauseLabelGo = CreateUIObject("Text", playPauseGo);
        var playPauseLabel = playPauseLabelGo.AddComponent<TextMeshProUGUI>();
        playPauseLabel.text = "PLAY";
        playPauseLabel.fontSize = 14;
        playPauseLabel.fontStyle = FontStyles.Bold;
        playPauseLabel.color = Color.white;
        playPauseLabel.alignment = TextAlignmentOptions.Center;
        if (interFont != null) playPauseLabel.font = interFont;
        SetCenterPosition(playPauseLabelGo.GetComponent<RectTransform>(), 0f, -28f, 90f, 25f);

        // FAB 2: Open Material Detail Button
        var matBtnGo = CreateUIObject("MaterialButton", fabColumnGo);
        var matBtnImg = matBtnGo.AddComponent<Image>();
        matBtnImg.sprite = btnSprite;
        matBtnImg.type = Image.Type.Sliced;
        matBtnImg.color = ColorForestGreen;
        var matBtn = matBtnGo.AddComponent<Button>();
        SetCenterPosition(matBtnGo.GetComponent<RectTransform>(), 0f, 0f, 100f, 100f);

        var matIconGo = CreateUIObject("IconText", matBtnGo);
        var matIcon = matIconGo.AddComponent<TextMeshProUGUI>();
        matIcon.text = "📖";
        matIcon.fontSize = 36;
        matIcon.alignment = TextAlignmentOptions.Center;
        StretchRect(matIconGo.GetComponent<RectTransform>());

        // FAB 3: Close / Reset Scan Button
        var closeGo = CreateUIObject("CloseButton", fabColumnGo);
        var closeImg = closeGo.AddComponent<Image>();
        closeImg.sprite = btnSprite;
        closeImg.type = Image.Type.Sliced;
        closeImg.color = ColorDeepForest;
        var closeBtn = closeGo.AddComponent<Button>();
        SetCenterPosition(closeGo.GetComponent<RectTransform>(), 0f, -120f, 100f, 100f);

        var closeIconGo = CreateUIObject("IconText", closeGo);
        var closeIcon = closeIconGo.AddComponent<TextMeshProUGUI>();
        closeIcon.text = "✕";
        closeIcon.fontSize = 36;
        closeIcon.color = Color.white;
        closeIcon.alignment = TextAlignmentOptions.Center;
        if (interFont != null) closeIcon.font = interFont;
        StretchRect(closeIconGo.GetComponent<RectTransform>());

        // G05 Bottom Info & Timeline Slider Card
        var timelineRootGo = CreateUIObject("TimelineCard", arControlsGo);
        var tlCardImg = timelineRootGo.AddComponent<Image>();
        tlCardImg.sprite = roundTopSprite;
        tlCardImg.type = Image.Type.Sliced;
        tlCardImg.color = new Color(0.957f, 0.941f, 0.902f, 0.96f); // Warm Cream 96%
        SetCenterPosition(timelineRootGo.GetComponent<RectTransform>(), 0f, -620f, 960f, 240f);

        // G05 Info tags inside bottom card
        var tlInfoRowGo = CreateUIObject("InfoRow", timelineRootGo);
        SetCenterPosition(tlInfoRowGo.GetComponent<RectTransform>(), 0f, 75f, 900f, 60f);

        var squatTagGo = CreateUIObject("SquatTag", tlInfoRowGo);
        var squatTagImg = squatTagGo.AddComponent<Image>();
        squatTagImg.sprite = btnSprite;
        squatTagImg.type = Image.Type.Sliced;
        squatTagImg.color = ColorCleanOffWhite;
        SetCenterPosition(squatTagGo.GetComponent<RectTransform>(), -330f, 0f, 180f, 50f);

        var squatDotGo = CreateUIObject("Dot", squatTagGo);
        var squatDotImg = squatDotGo.AddComponent<Image>();
        squatDotImg.sprite = btnSprite;
        squatDotImg.type = Image.Type.Sliced;
        squatDotImg.color = new Color(0.72f, 0.4f, 0.29f, 1f); // Terracotta dot
        SetCenterPosition(squatDotGo.GetComponent<RectTransform>(), -60f, 0f, 20f, 20f);

        var nameLabelGo = CreateUIObject("Name", squatTagGo);
        var nameLabel = nameLabelGo.AddComponent<TextMeshProUGUI>();
        nameLabel.text = "Squat";
        nameLabel.fontSize = 24;
        nameLabel.fontStyle = FontStyles.Bold;
        nameLabel.color = ColorDeepForest;
        nameLabel.alignment = TextAlignmentOptions.Left;
        if (interFont != null) nameLabel.font = interFont;
        SetCenterPosition(nameLabelGo.GetComponent<RectTransform>(), 20f, 0f, 110f, 40f);

        var statusTagGo = CreateUIObject("StatusTag", tlInfoRowGo);
        var statusTag = statusTagGo.AddComponent<TextMeshProUGUI>();
        statusTag.text = "Status: Loop";
        statusTag.fontSize = 20;
        statusTag.fontStyle = FontStyles.Bold;
        statusTag.color = ColorForestGreen;
        statusTag.alignment = TextAlignmentOptions.Right;
        if (interFont != null) statusTag.font = interFont;
        SetCenterPosition(statusTagGo.GetComponent<RectTransform>(), 310f, 0f, 220f, 40f);

        // Timeline Slider Control
        var sliderGo = CreateUIObject("Slider", timelineRootGo);
        var slider = sliderGo.AddComponent<Slider>();
        SetCenterPosition(sliderGo.GetComponent<RectTransform>(), 0f, 5f, 880f, 40f);

        // Customized Slider Visuals
        var sliderBgGo = CreateUIObject("Background", sliderGo);
        var slBgImg = sliderBgGo.AddComponent<Image>();
        slBgImg.sprite = btnSprite;
        slBgImg.type = Image.Type.Sliced;
        slBgImg.color = ColorSoftSage;
        SetCenterPosition(sliderBgGo.GetComponent<RectTransform>(), 0f, 0f, 880f, 10f);

        // Slider Handle Area
        var handleAreaGo = CreateUIObject("Handle Area", sliderGo);
        StretchRect(handleAreaGo.GetComponent<RectTransform>());

        var handleVisGo = CreateUIObject("Handle", handleAreaGo);
        var handleVisImg = handleVisGo.AddComponent<Image>();
        handleVisImg.sprite = btnSprite;
        handleVisImg.type = Image.Type.Sliced;
        handleVisImg.color = ColorForestGreen;
        SetCenterPosition(handleVisGo.GetComponent<RectTransform>(), 0f, 0f, 44f, 44f);
        slider.handleRect = handleVisGo.GetComponent<RectTransform>();

        // Under-slider info labels
        var tlBottomLabelsGo = CreateUIObject("BottomLabels", timelineRootGo);
        SetCenterPosition(tlBottomLabelsGo.GetComponent<RectTransform>(), 0f, -55f, 880f, 40f);

        var startLabelGo = CreateUIObject("Mulai", tlBottomLabelsGo);
        var startLabel = startLabelGo.AddComponent<TextMeshProUGUI>();
        startLabel.text = "Mulai";
        startLabel.fontSize = 20;
        startLabel.color = ColorMossGreen;
        startLabel.alignment = TextAlignmentOptions.Left;
        if (interFont != null) startLabel.font = interFont;
        SetCenterPosition(startLabelGo.GetComponent<RectTransform>(), -380f, 0f, 120f, 35f);

        var midLabelGo = CreateUIObject("Petunjuk", tlBottomLabelsGo);
        var midLabel = midLabelGo.AddComponent<TextMeshProUGUI>();
        midLabel.text = "Geser untuk memeriksa pose";
        midLabel.fontSize = 20;
        midLabel.color = ColorMossGreen;
        midLabel.alignment = TextAlignmentOptions.Center;
        if (interFont != null) midLabel.font = interFont;
        SetCenterPosition(midLabelGo.GetComponent<RectTransform>(), 0f, 0f, 400f, 35f);

        var endLabelGo = CreateUIObject("Selesai", tlBottomLabelsGo);
        var endLabel = endLabelGo.AddComponent<TextMeshProUGUI>();
        endLabel.text = "Selesai";
        endLabel.fontSize = 20;
        endLabel.color = ColorMossGreen;
        endLabel.alignment = TextAlignmentOptions.Right;
        if (interFont != null) endLabel.font = interFont;
        SetCenterPosition(endLabelGo.GetComponent<RectTransform>(), 380f, 0f, 120f, 35f);

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
        sheetRT.pivot = new Vector2(0.5f, 0f);
        sheetRT.anchoredPosition = new Vector2(0f, -Screen.height);
        sheetRT.sizeDelta = new Vector2(0f, Screen.height * 0.94f);

        // Grab Handle
        var handleGo = CreateUIObject("GrabHandle", sheetGo);
        var handleImg = handleGo.AddComponent<Image>();
        handleImg.sprite = btnSprite;
        handleImg.type = Image.Type.Sliced;
        handleImg.color = ColorSoftSage;
        SetCenterPosition(handleGo.GetComponent<RectTransform>(), 0f, sheetRT.sizeDelta.y - 20f, 100f, 10f);

        // Sheet Header Area (Utama / Tambahan)
        var categoryGo = CreateUIObject("CategoryTypeLabel", sheetGo);
        var categoryTxt = categoryGo.AddComponent<TextMeshProUGUI>();
        categoryTxt.text = "GERAKAN UTAMA";
        categoryTxt.fontSize = 22;
        categoryTxt.fontStyle = FontStyles.Bold;
        categoryTxt.color = new Color(0.72f, 0.4f, 0.29f, 1f); // Terracotta
        if (interFont != null) categoryTxt.font = interFont;
        SetCenterPosition(categoryGo.GetComponent<RectTransform>(), -260f, sheetRT.sizeDelta.y - 70f, 400f, 40f);

        // Dedicated Sheet Movement Name Text
        var sheetTitleGo = CreateUIObject("MovementTitle", sheetGo);
        var sheetTitleText = sheetTitleGo.AddComponent<TextMeshProUGUI>();
        sheetTitleText.text = "SQUAT";
        sheetTitleText.fontSize = 40;
        sheetTitleText.fontStyle = FontStyles.Bold;
        sheetTitleText.color = ColorDeepForest;
        sheetTitleText.alignment = TextAlignmentOptions.Left;
        if (interFont != null) sheetTitleText.font = interFont;
        SetCenterPosition(sheetTitleGo.GetComponent<RectTransform>(), -220f, sheetRT.sizeDelta.y - 120f, 480f, 60f);

        // Back to primary button (G07 -> G06)
        var backBtnGo = CreateUIObject("BackToPrimaryButton", sheetGo);
        var backBtnImg = backBtnGo.AddComponent<Image>();
        backBtnImg.sprite = btnSprite;
        backBtnImg.type = Image.Type.Sliced;
        backBtnImg.color = ColorDeepForest;
        var backBtn = backBtnGo.AddComponent<Button>();
        SetCenterPosition(backBtnGo.GetComponent<RectTransform>(), 360f, sheetRT.sizeDelta.y - 90f, 160f, 70f);

        // Back Button Text
        var backBtnTextGo = CreateUIObject("Text", backBtnGo);
        var backBtnText = backBtnTextGo.AddComponent<TextMeshProUGUI>();
        backBtnText.text = "Kembali";
        backBtnText.fontSize = 22;
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
        SetCenterPosition(sheetCloseGo.GetComponent<RectTransform>(), 410f, sheetRT.sizeDelta.y - 90f, 70f, 70f);

        var sheetCloseTxtGo = CreateUIObject("Text", sheetCloseGo);
        var sheetCloseTxt = sheetCloseTxtGo.AddComponent<TextMeshProUGUI>();
        sheetCloseTxt.text = "✕";
        sheetCloseTxt.fontSize = 28;
        sheetCloseTxt.color = ColorCharcoal;
        sheetCloseTxt.alignment = TextAlignmentOptions.Center;
        if (interFont != null) sheetCloseTxt.font = interFont;
        StretchRect(sheetCloseTxtGo.GetComponent<RectTransform>());

        // Scroll View under sheetGo
        var scrollViewGo = CreateUIObject("ScrollView", sheetGo);
        var scrollRect = scrollViewGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        SetCenterPosition(scrollViewGo.GetComponent<RectTransform>(), 0f, -110f, 960f, sheetRT.sizeDelta.y - 220f);

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
        aboutTitle.text = "TENTANG GERAKAN";
        aboutTitle.fontSize = 20;
        aboutTitle.fontStyle = FontStyles.Bold;
        aboutTitle.color = new Color(0.72f, 0.4f, 0.29f, 1f); // Terracotta
        if (interFont != null) aboutTitle.font = interFont;

        var descGo = CreateUIObject("Description", aboutGroupGo);
        var descText = descGo.AddComponent<TextMeshProUGUI>();
        descText.fontSize = 26;
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
        stepsTitle.text = "CARA MELAKUKAN";
        stepsTitle.fontSize = 20;
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
        safetyText.fontSize = 24;
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
        mistakesTitle.text = "HINDARI INI";
        mistakesTitle.fontSize = 20;
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
        trainedTitle.text = "OTOT YANG TERLATIH";
        trainedTitle.fontSize = 20;
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
        relatedTitle.text = "GERAKAN SERUPA";
        relatedTitle.fontSize = 20;
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
        serialSheet.FindProperty("movementController").objectReferenceValue = movementController;
        serialSheet.ApplyModifiedProperties();

        // Wire sheet close button to close sheet
        sheetCloseBtn.onClick.AddListener(sheetCtrl.CloseSheet);

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
