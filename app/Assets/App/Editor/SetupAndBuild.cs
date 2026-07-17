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
    // ── Green-dominant palette ────────────────────────────────────────
    private static readonly Color DeepForest     = new Color(0.07f, 0.216f, 0.165f, 1f);   // #12372A
    private static readonly Color ForestGreen    = new Color(0.12f, 0.365f, 0.259f, 1f);   // #1F5D42
    private static readonly Color WarmCream      = new Color(0.957f, 0.941f, 0.902f, 1f);  // #F4F0E6
    private static readonly Color WarmWhite      = new Color(1f, 1f, 1f, 1f);              // #FFFFFE
    private static readonly Color SoftSand       = new Color(0.918f, 0.867f, 0.812f, 1f);  // #EADDCF
    private static readonly Color SecondaryText  = new Color(0.443f, 0.376f, 0.251f, 1f);  // #716040
    private static readonly Color Error          = new Color(0.949f, 0.314f, 0.259f, 1f);  // #F25042

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

        var sdfFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/MobileARTemplateAssets/UI/Fonts/Inter-Regular_SDF.asset");
        var btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/MobileARTemplateAssets/UI/Sprites/ActivationButtonOpaque.png");
        var uiSolidRect = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/App/UI/Sprites/UISolidRectangle.png");
        var roundTopSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/MobileARTemplateAssets/UI/Sprites/RoundRadius_10_Top.png");

        GeneratePrefabs(sdfFont, btnSprite, uiSolidRect);
        CreateBootstrapScene(sdfFont, btnSprite, uiSolidRect);
        CreateMainARScene(sdfFont, btnSprite, uiSolidRect, roundTopSprite);
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

    private static void GeneratePrefabs(TMP_FontAsset font, Sprite btnSprite, Sprite uiSolidRect)
    {
        System.IO.Directory.CreateDirectory("Assets/App/Prefabs");

        // StepItem prefab
        var stepGo = new GameObject("StepItem", typeof(RectTransform));
        var stepText = stepGo.AddComponent<TextMeshProUGUI>();
        stepText.textWrappingMode = TextWrappingModes.Normal;
        stepText.overflowMode = TextOverflowModes.Overflow;
        stepText.fontSize = 13f;
        stepText.color = SecondaryText;
        stepText.alignment = TextAlignmentOptions.Left;
        if (font != null) stepText.font = font;
        stepGo.GetComponent<RectTransform>().sizeDelta = new Vector2(880f, 60f);
        PrefabUtility.SaveAsPrefabAsset(stepGo, "Assets/App/Prefabs/StepItem.prefab");
        Object.DestroyImmediate(stepGo);

        // BulletItem prefab
        var bulletGo = new GameObject("BulletItem", typeof(RectTransform));
        var bulletText = bulletGo.AddComponent<TextMeshProUGUI>();
        bulletText.textWrappingMode = TextWrappingModes.Normal;
        bulletText.overflowMode = TextOverflowModes.Overflow;
        bulletText.fontSize = 12f;
        bulletText.color = SecondaryText;
        bulletText.alignment = TextAlignmentOptions.Left;
        if (font != null) bulletText.font = font;
        bulletGo.GetComponent<RectTransform>().sizeDelta = new Vector2(880f, 50f);
        PrefabUtility.SaveAsPrefabAsset(bulletGo, "Assets/App/Prefabs/BulletItem.prefab");
        Object.DestroyImmediate(bulletGo);

        // RelatedCard prefab
        var cardGo = new GameObject("RelatedCard", typeof(RectTransform));
        var cardImg = cardGo.AddComponent<Image>();
        cardImg.sprite = btnSprite;
        cardImg.type = Image.Type.Sliced;
        cardImg.color = WarmWhite;
        cardGo.AddComponent<Button>();
        var cardRT = cardGo.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(240f, 180f);

        var thumbGo = CreateUIObject("Thumbnail", cardGo);
        var thumbImg = thumbGo.AddComponent<Image>();
        thumbImg.preserveAspect = true;
        thumbImg.color = SoftSand;
        SetCenterPosition(thumbGo.GetComponent<RectTransform>(), 0f, 8.3f, 66.7f, 33.3f);

        var titleGo = CreateUIObject("Title", cardGo);
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.textWrappingMode = TextWrappingModes.Normal;
        titleText.overflowMode = TextOverflowModes.Overflow;
        titleText.text = "Related";
        titleText.fontSize = 12f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = DeepForest;
        titleText.alignment = TextAlignmentOptions.Center;
        if (font != null) titleText.font = font;
        SetCenterPosition(titleGo.GetComponent<RectTransform>(), 0f, -16.7f, 66.7f, 13.3f);

        PrefabUtility.SaveAsPrefabAsset(cardGo, "Assets/App/Prefabs/RelatedCard.prefab");
        Object.DestroyImmediate(cardGo);

        // TimelineMarker prefab (small circle node)
        var markerGo = new GameObject("TimelineMarker", typeof(RectTransform));
        var markerImg = markerGo.AddComponent<Image>();
        markerImg.sprite = uiSolidRect != null
            ? uiSolidRect
            : btnSprite;
        markerImg.type = Image.Type.Simple;
        markerImg.preserveAspect = true;
        var markerRT = markerGo.GetComponent<RectTransform>();
        markerRT.sizeDelta = new Vector2(10f, 10f);
        PrefabUtility.SaveAsPrefabAsset(markerGo, "Assets/App/Prefabs/TimelineMarker.prefab");
        Object.DestroyImmediate(markerGo);
    }

    // ─────────────────────────────────────────────────────────────────
    // BOOTSTRAP SCENE
    // ─────────────────────────────────────────────────────────────────
    private static void CreateBootstrapScene(TMP_FontAsset font, Sprite btnSprite, Sprite uiSolidRect)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Bootstrap";

        var managersGo = new GameObject("Managers");
        managersGo.AddComponent<PermissionController>();
        managersGo.AddComponent<ARAvailabilityChecker>();
        managersGo.AddComponent<OnboardingController>();

        var stateMgrGo = new GameObject("AppStateManager");
        stateMgrGo.AddComponent<AppStateManager>();

        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Color;
        cam.backgroundColor = DeepForest;
        camGo.tag = "MainCamera";

        var canvasGo = CreateCanvas("Canvas", null);
        canvasGo.transform.SetParent(null);

        var eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemGo.AddComponent<InputSystemUIInputModule>();

        // ═══════════════════════════════════════════════════════════
        // G01 — OPENING FULL-SCREEN COVER
        // ═══════════════════════════════════════════════════════════
        var introGo = CreateUIObject("IntroPanel", canvasGo);
        var introImg = introGo.AddComponent<Image>();
        introImg.color = DeepForest;
        StretchRect(introGo.GetComponent<RectTransform>());
        var introCanvasGroup = introGo.AddComponent<CanvasGroup>();
        introCanvasGroup.interactable = false;
        introCanvasGroup.blocksRaycasts = false;
        var introController = managersGo.AddComponent<IntroController>();

        // Full-bleed cover image placeholder (stretch, no margins)
        var coverGo = CreateUIObject("FullBleedCoverImage", introGo);
        StretchRect(coverGo.GetComponent<RectTransform>());

        // Top identity
        var topIdGo = CreateUIObject("TopIdentity", introGo);
        SetAnchorTopLeft(topIdGo.GetComponent<RectTransform>(), 20f, -20f, 200f, 40f);

        var metaTextGo = CreateUIObject("MetadataText", topIdGo);
        var metaText = metaTextGo.AddComponent<TextMeshProUGUI>();
        metaText.textWrappingMode = TextWrappingModes.Normal;
        metaText.overflowMode = TextOverflowModes.Overflow;
        metaText.text = "<b>Media Pembelajaran</b>\n<color=#EADDCF>Skripsi Pendidikan SD</color>";
        metaText.fontSize = 12f;
        metaText.color = WarmWhite;
        metaText.alignment = TextAlignmentOptions.Left;
        if (font != null) metaText.font = font;
        StretchRect(metaTextGo.GetComponent<RectTransform>());

        // Center visual placeholder
        var centerGo = CreateUIObject("CenterVisual", introGo);
        SetCenterPosition(centerGo.GetComponent<RectTransform>(), 0f, 50f, 200f, 200f);

        // Bottom brand group
        var brandGroupGo = CreateUIObject("BrandGroup", introGo);
        SetAnchorBottom(brandGroupGo.GetComponent<RectTransform>(), 0f, 100f, 320f, 130f);

        var titleGo = CreateUIObject("TitleText", brandGroupGo);
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.textWrappingMode = TextWrappingModes.Normal;
        titleText.text = "GerakAR";
        titleText.fontSize = 34f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = WarmWhite;
        titleText.alignment = TextAlignmentOptions.Center;
        if (font != null) titleText.font = font;
        SetCenterPosition(titleGo.GetComponent<RectTransform>(), 0f, 45f, 266.7f, 36f);

        var subtitleGo = CreateUIObject("SubtitleText", brandGroupGo);
        var subtitleText = subtitleGo.AddComponent<TextMeshProUGUI>();
        subtitleText.textWrappingMode = TextWrappingModes.Normal;
        subtitleText.text = "Belajar Gerak Jadi Seru";
        subtitleText.fontSize = 12f;
        subtitleText.color = SoftSand;
        subtitleText.alignment = TextAlignmentOptions.Center;
        if (font != null) subtitleText.font = font;
        SetCenterPosition(subtitleGo.GetComponent<RectTransform>(), 0f, 15f, 266.7f, 16.7f);

        // StraightProgressBar — UISolidRectangle anti-taper
        var progressTrackGo = CreateUIObject("ProgressTrack", brandGroupGo);
        var trackImg = progressTrackGo.AddComponent<Image>();
        trackImg.sprite = uiSolidRect;
        trackImg.type = Image.Type.Simple;
        trackImg.preserveAspect = false;
        trackImg.color = SoftSand;
        trackImg.GetComponent<RectTransform>().localScale = Vector3.one;
        SetCenterPosition(progressTrackGo.GetComponent<RectTransform>(), 0f, -20f, 180f, 5f);

        var progressFillGo = CreateUIObject("ProgressFill", progressTrackGo);
        var fillImg = progressFillGo.AddComponent<Image>();
        fillImg.sprite = uiSolidRect;
        fillImg.type = Image.Type.Simple;
        fillImg.preserveAspect = false;
        fillImg.color = WarmWhite;
        fillImg.GetComponent<RectTransform>().localScale = Vector3.one;
        var fillRT = progressFillGo.GetComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(0f, 1f);
        fillRT.pivot = new Vector2(0f, 0.5f);
        fillRT.anchoredPosition = Vector2.zero;
        fillRT.sizeDelta = new Vector2(0f, 0f);

        // Loading helper label
        var loadingLabelGo = CreateUIObject("LoadingLabel", brandGroupGo);
        var loadingLabel = loadingLabelGo.AddComponent<TextMeshProUGUI>();
        loadingLabel.textWrappingMode = TextWrappingModes.Normal;
        loadingLabel.text = "Memuat pengalaman belajar";
        loadingLabel.fontSize = 10f;
        loadingLabel.color = SoftSand;
        loadingLabel.alignment = TextAlignmentOptions.Center;
        if (font != null) loadingLabel.font = font;
        SetCenterPosition(loadingLabelGo.GetComponent<RectTransform>(), 0f, -50f, 200f, 14f);

        var serialIntro = new SerializedObject(introController);
        serialIntro.FindProperty("introCanvasGroup").objectReferenceValue = introCanvasGroup;
        serialIntro.FindProperty("loadingFillImage").objectReferenceValue = fillImg;
        serialIntro.ApplyModifiedProperties();

        // ═══════════════════════════════════════════════════════════
        // G02 — ONBOARDING
        // ═══════════════════════════════════════════════════════════
        var onboardGo = CreateUIObject("OnboardingPanel", canvasGo);
        var onboardImg = onboardGo.AddComponent<Image>();
        onboardImg.color = WarmCream;
        StretchRect(onboardGo.GetComponent<RectTransform>());
        onboardGo.SetActive(false);

        // Decorative header bar
        var obHeaderBarGo = CreateUIObject("HeaderBar", onboardGo);
        var obHeaderBarImg = obHeaderBarGo.AddComponent<Image>();
        obHeaderBarImg.color = ForestGreen;
        SetAnchorTop(obHeaderBarGo.GetComponent<RectTransform>(), 0f, 0f, 360f, 6f);

        var safetyHeaderGo = CreateUIObject("SafetyHeader", onboardGo);
        SetAnchorTop(safetyHeaderGo.GetComponent<RectTransform>(), 0f, -60f, 320f, 120f);

        var obTitleGo = CreateUIObject("OnboardingTitle", safetyHeaderGo);
        var obTitle = obTitleGo.AddComponent<TextMeshProUGUI>();
        obTitle.textWrappingMode = TextWrappingModes.Normal;
        obTitle.text = "Sebelum Mulai";
        obTitle.fontSize = 24f;
        obTitle.fontStyle = FontStyles.Bold;
        obTitle.color = DeepForest;
        obTitle.alignment = TextAlignmentOptions.Center;
        if (font != null) obTitle.font = font;
        SetCenterPosition(obTitleGo.GetComponent<RectTransform>(), 0f, 30f, 266.7f, 30f);

        var obSubtitleGo = CreateUIObject("OnboardingSubtitle", safetyHeaderGo);
        var obSubtitle = obSubtitleGo.AddComponent<TextMeshProUGUI>();
        obSubtitle.textWrappingMode = TextWrappingModes.Normal;
        obSubtitle.text = "Ayo bergerak dengan aman dan nyaman.";
        obSubtitle.fontSize = 12f;
        obSubtitle.color = SecondaryText;
        obSubtitle.alignment = TextAlignmentOptions.Center;
        if (font != null) obSubtitle.font = font;
        SetCenterPosition(obSubtitleGo.GetComponent<RectTransform>(), 0f, -5f, 266.7f, 16.7f);

        var obListGo = CreateUIObject("InstructionList", onboardGo);
        SetCenterPosition(obListGo.GetComponent<RectTransform>(), 0f, -10f, 320f, 190f);

        // Card 1
        CreateOnboardingCard(obListGo, "Card1", "1", "Gunakan di tempat yang cukup luas.",
            0f, 65f, btnSprite, font);
        // Card 2
        CreateOnboardingCard(obListGo, "Card2", "2", "Minta guru atau orang tua mendampingi.",
            0f, 0f, btnSprite, font);
        // Card 3
        CreateOnboardingCard(obListGo, "Card3", "3", "Izinkan kamera untuk melihat gerakan.",
            0f, -65f, btnSprite, font);

        var btnGroupGo = CreateUIObject("ButtonGroup", onboardGo);
        SetAnchorBottom(btnGroupGo.GetComponent<RectTransform>(), 0f, 60f, 320f, 100f);

        // MULAI button — NO camera icon, text centered
        var startBtnGo = CreateUIObject("MulaiButton", btnGroupGo);
        var startBtnImg = startBtnGo.AddComponent<Image>();
        startBtnImg.sprite = btnSprite;
        startBtnImg.type = Image.Type.Sliced;
        startBtnImg.color = ForestGreen;
        startBtnGo.AddComponent<Button>();
        startBtnGo.AddComponent<OnboardingButtonWirer>();
        SetCenterPosition(startBtnGo.GetComponent<RectTransform>(), 0f, 20f, 306.7f, 50f);

        var btnTextGo = CreateUIObject("Text", startBtnGo);
        var btnText = btnTextGo.AddComponent<TextMeshProUGUI>();
        btnText.textWrappingMode = TextWrappingModes.Normal;
        btnText.text = "MULAI";
        btnText.fontSize = 16f;
        btnText.fontStyle = FontStyles.Bold;
        btnText.color = WarmWhite;
        btnText.alignment = TextAlignmentOptions.Center;
        if (font != null) btnText.font = font;
        StretchRect(btnTextGo.GetComponent<RectTransform>());

        // Fallback links (hidden in production)
        var fallbackLinksGo = CreateUIObject("FallbackLinks", btnGroupGo);
        SetCenterPosition(fallbackLinksGo.GetComponent<RectTransform>(), 0f, -25f, 306.7f, 16.7f);
        fallbackLinksGo.SetActive(false);

        var nonARLinkGo = CreateUIObject("NonARLink", fallbackLinksGo);
        var nonARLinkText = nonARLinkGo.AddComponent<TextMeshProUGUI>();
        nonARLinkText.textWrappingMode = TextWrappingModes.Normal;
        nonARLinkText.text = "<u>Simulasi Non-AR</u>";
        nonARLinkText.fontSize = 10f;
        nonARLinkText.color = ForestGreen;
        nonARLinkText.alignment = TextAlignmentOptions.Center;
        if (font != null) nonARLinkText.font = font;
        var nonARBtn = nonARLinkGo.AddComponent<Button>();
        SetCenterPosition(nonARLinkGo.GetComponent<RectTransform>(), -73.3f, 0f, 113.3f, 16.7f);

        var camErrorLinkGo = CreateUIObject("CameraErrorLink", fallbackLinksGo);
        var camErrorLinkText = camErrorLinkGo.AddComponent<TextMeshProUGUI>();
        camErrorLinkText.textWrappingMode = TextWrappingModes.Normal;
        camErrorLinkText.text = "<u>Simulasi Kendala Kamera</u>";
        camErrorLinkText.fontSize = 10f;
        camErrorLinkText.color = DeepForest;
        camErrorLinkText.alignment = TextAlignmentOptions.Center;
        if (font != null) camErrorLinkText.font = font;
        var camErrorBtn = camErrorLinkGo.AddComponent<Button>();
        SetCenterPosition(camErrorLinkGo.GetComponent<RectTransform>(), 73.3f, 0f, 113.3f, 16.7f);

        var serialOnboard = new SerializedObject(managersGo.GetComponent<OnboardingController>());
        serialOnboard.FindProperty("onboardingPanel").objectReferenceValue = onboardGo;
        serialOnboard.ApplyModifiedProperties();

        // ═══════════════════════════════════════════════════════════
        // UNSUPPORTED PANEL (Parent for G08 & G09)
        // ═══════════════════════════════════════════════════════════
        var unsupGo = CreateUIObject("UnsupportedPanel", canvasGo);
        var unsupImg = unsupGo.AddComponent<Image>();
        unsupImg.color = WarmCream;
        StretchRect(unsupGo.GetComponent<RectTransform>());
        unsupGo.SetActive(false);

        // ── G08 — NON-AR CATALOG ──
        var nonARModePanelGo = CreateUIObject("NonARModePanel", unsupGo);
        var nonARModePanelRT = nonARModePanelGo.GetComponent<RectTransform>();
        StretchRect(nonARModePanelRT);

        // G08 Header Deep Forest
        var g08HeaderBarGo = CreateUIObject("HeaderBar", nonARModePanelGo);
        var g08HeaderBarImg = g08HeaderBarGo.AddComponent<Image>();
        g08HeaderBarImg.color = DeepForest;
        SetAnchorTop(g08HeaderBarGo.GetComponent<RectTransform>(), 0f, 0f, 360f, 56f);

        var g08BrandGo = CreateUIObject("Brand", g08HeaderBarGo);
        var g08BrandText = g08BrandGo.AddComponent<TextMeshProUGUI>();
        g08BrandText.textWrappingMode = TextWrappingModes.Normal;
        g08BrandText.text = "GerakAR";
        g08BrandText.fontSize = 20f;
        g08BrandText.fontStyle = FontStyles.Bold;
        g08BrandText.color = WarmWhite;
        g08BrandText.alignment = TextAlignmentOptions.Left;
        if (font != null) g08BrandText.font = font;
        SetCenterPosition(g08BrandGo.GetComponent<RectTransform>(), -140f, 0f, 120f, 28f);

        var g08BadgeGo = CreateUIObject("ModeBadge", g08HeaderBarGo);
        var g08BadgeImg = g08BadgeGo.AddComponent<Image>();
        g08BadgeImg.sprite = btnSprite;
        g08BadgeImg.type = Image.Type.Sliced;
        g08BadgeImg.color = ForestGreen;
        SetCenterPosition(g08BadgeGo.GetComponent<RectTransform>(), 100f, 0f, 100f, 24f);

        var g08BadgeTextGo = CreateUIObject("Text", g08BadgeGo);
        var g08BadgeText = g08BadgeTextGo.AddComponent<TextMeshProUGUI>();
        g08BadgeText.textWrappingMode = TextWrappingModes.Normal;
        g08BadgeText.text = "NON-AR MODE";
        g08BadgeText.fontSize = 10f;
        g08BadgeText.fontStyle = FontStyles.Bold;
        g08BadgeText.color = WarmWhite;
        g08BadgeText.alignment = TextAlignmentOptions.Center;
        if (font != null) g08BadgeText.font = font;
        StretchRect(g08BadgeTextGo.GetComponent<RectTransform>());

        // G08 Collapsible Warning
        var collapsibleWarnGo = CreateCollapsibleWarning(
            nonARModePanelGo, btnSprite, font, "Assets/App/UI/Icons/Lucide/info.png");
        var collapsibleWarnRT = collapsibleWarnGo.GetComponent<RectTransform>();
        collapsibleWarnRT.anchorMin = new Vector2(0.5f, 1f);
        collapsibleWarnRT.anchorMax = new Vector2(0.5f, 1f);
        collapsibleWarnRT.pivot = new Vector2(0.5f, 1f);
        collapsibleWarnRT.anchoredPosition = new Vector2(0f, -70f);
        collapsibleWarnRT.sizeDelta = new Vector2(320f, 48f); // collapsed height

        // G08 Catalog Content
        var catalogCatalogGo = CreateUIObject("CatalogCatalog", nonARModePanelGo);
        SetCenterPosition(catalogCatalogGo.GetComponent<RectTransform>(), 0f, -20f, 320f, 320f);

        var catTitleGo = CreateUIObject("CatTitleText", catalogCatalogGo);
        var catTitle = catTitleGo.AddComponent<TextMeshProUGUI>();
        catTitle.textWrappingMode = TextWrappingModes.Normal;
        catTitle.text = "KATALOG GERAKAN";
        catTitle.fontSize = 14f;
        catTitle.fontStyle = FontStyles.Bold;
        catTitle.color = DeepForest;
        catTitle.alignment = TextAlignmentOptions.Left;
        if (font != null) catTitle.font = font;
        SetCenterPosition(catTitleGo.GetComponent<RectTransform>(), 0f, 145f, 300f, 16f);

        // Squat card
        var (squatBukaBtn, _, _) = CreateMovementCard(
            catalogCatalogGo, "CardSquat", "SQ", "Gerakan Squat",
            "Melatih otot paha dan sendi lutut",
            0f, 95f, btnSprite, font);

        // Dynamic Stretching card
        var (dynamicStretchBukaBtn, _, _) = CreateMovementCard(
            catalogCatalogGo, "CardDynamicStretch", "DS", "Dynamic Stretching",
            "Peregangan aktif sebelum bergerak",
            0f, 30f, btnSprite, font);

        // Ladder Drill card
        var (ladderDrillBukaBtn, _, _) = CreateMovementCard(
            catalogCatalogGo, "CardLadderDrill", "LD", "Ladder Drill",
            "Melatih kelincahan dan langkah kaki",
            0f, -35f, btnSprite, font);

        // Back button
        var catalogBackGo = CreateUIObject("CatalogBackButton", nonARModePanelGo);
        var catalogBackImg = catalogBackGo.AddComponent<Image>();
        catalogBackImg.sprite = btnSprite;
        catalogBackImg.type = Image.Type.Sliced;
        catalogBackImg.color = ForestGreen;
        var catalogBackBtn = catalogBackGo.AddComponent<Button>();
        SetAnchorBottom(catalogBackGo.GetComponent<RectTransform>(), 0f, 30f, 120f, 44f);

        var catalogBackTextGo = CreateUIObject("Text", catalogBackGo);
        var catalogBackText = catalogBackTextGo.AddComponent<TextMeshProUGUI>();
        catalogBackText.textWrappingMode = TextWrappingModes.Normal;
        catalogBackText.text = "< Petunjuk";
        catalogBackText.fontSize = 13f;
        catalogBackText.fontStyle = FontStyles.Bold;
        catalogBackText.color = WarmWhite;
        catalogBackText.alignment = TextAlignmentOptions.Center;
        if (font != null) catalogBackText.font = font;
        StretchRect(catalogBackTextGo.GetComponent<RectTransform>());

        // ── G09 — CAMERA DENIED ──
        var cameraErrorPanelGo = CreateUIObject("CameraErrorPanel", unsupGo);
        StretchRect(cameraErrorPanelGo.GetComponent<RectTransform>());
        cameraErrorPanelGo.SetActive(false);

        // Full Deep Forest background
        var camErrorBgGo = CreateUIObject("Background", cameraErrorPanelGo);
        var camErrorBgImg = camErrorBgGo.AddComponent<Image>();
        camErrorBgImg.color = DeepForest;
        StretchRect(camErrorBgGo.GetComponent<RectTransform>());

        // Central card Warm White
        var camCardGo = CreateUIObject("CentralCard", cameraErrorPanelGo);
        var camCardImg = camCardGo.AddComponent<Image>();
        camCardImg.sprite = btnSprite;
        camCardImg.type = Image.Type.Sliced;
        camCardImg.color = WarmWhite;
        SetCenterPosition(camCardGo.GetComponent<RectTransform>(), 0f, 20f, 280f, 280f);

        // Camera-off icon
        var camOffIconGo = CreateUIObject("CamOffIcon", camCardGo);
        var camOffIconImg = camOffIconGo.AddComponent<Image>();
        camOffIconImg.preserveAspect = true;
        camOffIconImg.sprite = btnSprite;
        camOffIconImg.type = Image.Type.Sliced;
        camOffIconImg.color = SoftSand;
        SetCenterPosition(camOffIconGo.GetComponent<RectTransform>(), 0f, 90f, 44f, 44f);

        var camOffIconTextGo = CreateUIObject("Text", camOffIconGo);
        var camOffIconText = camOffIconTextGo.AddComponent<TextMeshProUGUI>();
        camOffIconText.text = "!";
        camOffIconText.fontSize = 24f;
        camOffIconText.fontStyle = FontStyles.Bold;
        camOffIconText.color = DeepForest;
        camOffIconText.alignment = TextAlignmentOptions.Center;
        if (font != null) camOffIconText.font = font;
        StretchRect(camOffIconTextGo.GetComponent<RectTransform>());

        var camErrorTitleGo = CreateUIObject("Title", camCardGo);
        var camErrorTitle = camErrorTitleGo.AddComponent<TextMeshProUGUI>();
        camErrorTitle.textWrappingMode = TextWrappingModes.Normal;
        camErrorTitle.text = "Kamera Belum Aktif";
        camErrorTitle.fontSize = 20f;
        camErrorTitle.fontStyle = FontStyles.Bold;
        camErrorTitle.color = DeepForest;
        camErrorTitle.alignment = TextAlignmentOptions.Center;
        if (font != null) camErrorTitle.font = font;
        SetCenterPosition(camErrorTitleGo.GetComponent<RectTransform>(), 0f, 45f, 240f, 28f);

        var camErrorDescGo = CreateUIObject("Desc", camCardGo);
        var camErrorDesc = camErrorDescGo.AddComponent<TextMeshProUGUI>();
        camErrorDesc.textWrappingMode = TextWrappingModes.Normal;
        camErrorDesc.text = "Izinkan akses kamera agar GerakAR dapat melihat gambar gerakan.";
        camErrorDesc.fontSize = 13f;
        camErrorDesc.color = SecondaryText;
        camErrorDesc.alignment = TextAlignmentOptions.Center;
        if (font != null) camErrorDesc.font = font;
        SetCenterPosition(camErrorDescGo.GetComponent<RectTransform>(), 0f, 10f, 240f, 40f);

        // BUKA PENGATURAN button
        var settingsGo = CreateUIObject("SettingsButton", camCardGo);
        var settingsImg = settingsGo.AddComponent<Image>();
        settingsImg.sprite = btnSprite;
        settingsImg.type = Image.Type.Sliced;
        settingsImg.color = ForestGreen;
        var settingsBtn = settingsGo.AddComponent<Button>();
        SetCenterPosition(settingsGo.GetComponent<RectTransform>(), 0f, -35f, 240f, 44f);

        var settingsTextGo = CreateUIObject("Text", settingsGo);
        var settingsText = settingsTextGo.AddComponent<TextMeshProUGUI>();
        settingsText.textWrappingMode = TextWrappingModes.Normal;
        settingsText.text = "BUKA PENGATURAN";
        settingsText.fontSize = 14f;
        settingsText.fontStyle = FontStyles.Bold;
        settingsText.color = WarmWhite;
        settingsText.alignment = TextAlignmentOptions.Center;
        if (font != null) settingsText.font = font;
        StretchRect(settingsTextGo.GetComponent<RectTransform>());

        // Coba Lagi button
        var retryGo = CreateUIObject("RetryButton", camCardGo);
        var retryBtnImg = retryGo.AddComponent<Image>();
        retryBtnImg.sprite = btnSprite;
        retryBtnImg.type = Image.Type.Sliced;
        retryBtnImg.color = WarmCream;
        var retryBtn = retryGo.AddComponent<Button>();
        SetCenterPosition(retryGo.GetComponent<RectTransform>(), 0f, -80f, 240f, 36f);

        var retryTextGo = CreateUIObject("Text", retryGo);
        var retryText = retryTextGo.AddComponent<TextMeshProUGUI>();
        retryText.textWrappingMode = TextWrappingModes.Normal;
        retryText.text = "Coba Lagi";
        retryText.fontSize = 14f;
        retryText.fontStyle = FontStyles.Bold;
        retryText.color = DeepForest;
        retryText.alignment = TextAlignmentOptions.Center;
        if (font != null) retryText.font = font;
        StretchRect(retryTextGo.GetComponent<RectTransform>());

        // Helper text
        var helperGo = CreateUIObject("HelperText", cameraErrorPanelGo);
        var helperText = helperGo.AddComponent<TextMeshProUGUI>();
        helperText.textWrappingMode = TextWrappingModes.Normal;
        helperText.text = "Minta bantuan guru atau orang tua jika diperlukan.";
        helperText.fontSize = 11f;
        helperText.color = SoftSand;
        helperText.alignment = TextAlignmentOptions.Center;
        if (font != null) helperText.font = font;
        SetAnchorBottom(helperGo.GetComponent<RectTransform>(), 0f, 20f, 280f, 16f);

        // BootstrapUIController wiring
        var bootstrapUI = managersGo.AddComponent<BootstrapUIController>();
        var serialBUI = new SerializedObject(bootstrapUI);
        serialBUI.FindProperty("introPanel").objectReferenceValue = introGo;
        serialBUI.FindProperty("onboardingPanel").objectReferenceValue = onboardGo;
        serialBUI.FindProperty("unsupportedPanel").objectReferenceValue = unsupGo;
        serialBUI.FindProperty("nonARModePanel").objectReferenceValue = nonARModePanelGo;
        serialBUI.FindProperty("cameraErrorPanel").objectReferenceValue = cameraErrorPanelGo;
        serialBUI.ApplyModifiedProperties();

        // BootstrapButtonController wiring
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

    // ─────────────────────────────────────────────────────────────────
    // MAIN AR SCENE
    // ─────────────────────────────────────────────────────────────────
    private static void CreateMainARScene(TMP_FontAsset font, Sprite btnSprite, Sprite uiSolidRect, Sprite roundTopSprite)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MainAR";

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
        serializedTarget.FindProperty("<TwoDImageWidth>k__BackingField").floatValue = 0.12f;
        serializedTarget.FindProperty("currentFiltered").boolValue = true;
        serializedTarget.FindProperty("currentFilterSampleRate").floatValue = 30f;
        serializedTarget.FindProperty("currentFilterCutoffFreq").floatValue = 15f;
        serializedTarget.ApplyModifiedProperties();

        var camGo = new GameObject("AR Camera");
        camGo.SetActive(false);
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = DeepForest;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 100f;
        cam.cullingMask &= ~(1 << 8);
        cam.allowHDR = false;
        cam.allowMSAA = true;
        camGo.tag = "MainCamera";
        camGo.AddComponent<AudioListener>();
        var arCamera = camGo.AddComponent<ARXCamera>();
        arCamera.CameraContentMode = ARXCamera.ContentMode.Fill;
        var videoBackground = camGo.AddComponent<ARXVideoBackground>();
        videoBackground.BackgroundLayer = 8;

        var trackedAnchorGo = new GameObject("C5 Tracked Anchor");
        var trackedObject = trackedAnchorGo.AddComponent<ARXTrackedObject>();
        trackedObject.TrackableTag = "C5";
        trackedObject.secondsToRemainVisible = 0.75f;

        var modelRootGo = new GameObject("ModelRoot");
        modelRootGo.transform.SetParent(trackedAnchorGo.transform, false);

        var database = AssetDatabase.LoadAssetAtPath<MovementDatabase>(
            "Assets/App/Content/MovementData/MovementDatabase.asset");

        var managersGo = new GameObject("Managers");
        var modelPool = managersGo.AddComponent<ModelPool>();
        var movementController = managersGo.AddComponent<MovementController>();
        var audioGuideController = managersGo.AddComponent<AudioGuideController>();
        var trackingController = managersGo.AddComponent<ARImageTrackingController>();
        var backgroundPresenter = managersGo.AddComponent<ARUnityXURPBackgroundPresenter>();
        var sessionController = managersGo.AddComponent<ARUnityXSessionController>();

        var stateMgrGo = new GameObject("AppStateManager");
        stateMgrGo.AddComponent<AppStateManager>();

        var serialPool = new SerializedObject(modelPool);
        serialPool.FindProperty("modelRoot").objectReferenceValue = modelRootGo.transform;
        serialPool.ApplyModifiedProperties();

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
        serialSession.FindProperty("backgroundPresenter").objectReferenceValue = backgroundPresenter;
        serialSession.ApplyModifiedProperties();

        var serialBackgroundPresenter = new SerializedObject(backgroundPresenter);
        serialBackgroundPresenter.FindProperty("foregroundCamera").objectReferenceValue = cam;
        serialBackgroundPresenter.ApplyModifiedProperties();

        var serialAudio = new SerializedObject(audioGuideController);
        serialAudio.FindProperty("movementDatabase").objectReferenceValue = database;
        serialAudio.ApplyModifiedProperties();

        // ── UI CANVAS ──
        var canvasGo = CreateCanvas("UI Canvas", null);
        canvasGo.transform.SetParent(null);

        var eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemGo.AddComponent<InputSystemUIInputModule>();

        var arUI = canvasGo.AddComponent<ARUIController>();

        // ═══════════════════════════════════════════════════════════
        // G03 — SCANNER
        // ═══════════════════════════════════════════════════════════
        var scanGo = CreateUIObject("ScanOverlay", canvasGo);
        StretchRect(scanGo.GetComponent<RectTransform>());

        // G03 Header
        var scanTitleGo = CreateUIObject("HeaderTitle", scanGo);
        var scanTitle = scanTitleGo.AddComponent<TextMeshProUGUI>();
        scanTitle.textWrappingMode = TextWrappingModes.Normal;
        scanTitle.text = "GerakAR";
        scanTitle.fontSize = 20f;
        scanTitle.fontStyle = FontStyles.Bold;
        scanTitle.color = WarmWhite;
        scanTitle.alignment = TextAlignmentOptions.Center;
        if (font != null) scanTitle.font = font;
        SetAnchorTop(scanTitleGo.GetComponent<RectTransform>(), 0f, -48f, 200f, 28f);

        var scanSubGo = CreateUIObject("HeaderSub", scanGo);
        var scanSub = scanSubGo.AddComponent<TextMeshProUGUI>();
        scanSub.textWrappingMode = TextWrappingModes.Normal;
        scanSub.text = "Belajar Gerak Jadi Seru";
        scanSub.fontSize = 10f;
        scanSub.color = SoftSand;
        scanSub.alignment = TextAlignmentOptions.Center;
        if (font != null) scanSub.font = font;
        SetAnchorTop(scanSubGo.GetComponent<RectTransform>(), 0f, -76f, 200f, 16f);

        // G03 Central Scan Guide Frame — Solid Warm White corners
        var scanFrameGo = CreateUIObject("ScanFrame", scanGo);
        SetCenterPosition(scanFrameGo.GetComponent<RectTransform>(), 0f, 0f, 232f, 232f);

        // Helper for solid L-corner brackets
        System.Action<string, float, float, float, float> CreateSolidCorner =
            (name, x, y, ax, ay) =>
        {
            var c = CreateUIObject(name, scanFrameGo);
            var cRT = c.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0.5f, 0.5f);
            cRT.anchorMax = new Vector2(0.5f, 0.5f);
            cRT.pivot = new Vector2(0.5f, 0.5f);
            cRT.anchoredPosition = new Vector2(x, y);
            cRT.sizeDelta = new Vector2(26f, 26f);

            // Horizontal arm
            var h = CreateUIObject(name + "H", c);
            var hImg = h.AddComponent<Image>();
            hImg.sprite = uiSolidRect;
            hImg.type = Image.Type.Simple;
            hImg.preserveAspect = false;
            hImg.color = WarmWhite;
            var hRT = h.GetComponent<RectTransform>();
            hRT.localScale = Vector3.one;
            hRT.anchorMin = new Vector2(ax, ay);
            hRT.anchorMax = new Vector2(ax, ay);
            hRT.pivot = new Vector2(ax, ay);
            hRT.anchoredPosition = Vector2.zero;
            hRT.sizeDelta = new Vector2(24f, 3f);

            // Vertical arm
            var v = CreateUIObject(name + "V", c);
            var vImg = v.AddComponent<Image>();
            vImg.sprite = uiSolidRect;
            vImg.type = Image.Type.Simple;
            vImg.preserveAspect = false;
            vImg.color = WarmWhite;
            var vRT = v.GetComponent<RectTransform>();
            vRT.localScale = Vector3.one;
            vRT.anchorMin = new Vector2(ax, ay);
            vRT.anchorMax = new Vector2(ax, ay);
            vRT.pivot = new Vector2(ax, ay);
            vRT.anchoredPosition = Vector2.zero;
            vRT.sizeDelta = new Vector2(3f, 24f);
        };

        CreateSolidCorner("TopLeft", -103f, 103f, 0f, 1f);
        CreateSolidCorner("TopRight", 103f, 103f, 1f, 1f);
        CreateSolidCorner("BottomLeft", -103f, -103f, 0f, 0f);
        CreateSolidCorner("BottomRight", 103f, -103f, 1f, 0f);

        // DetectionSweep — UISolidRectangle, hidden initially
        var scanLineGo = CreateUIObject("DetectionSweep", scanFrameGo);
        var scanLineImg = scanLineGo.AddComponent<Image>();
        scanLineImg.sprite = uiSolidRect;
        scanLineImg.type = Image.Type.Simple;
        scanLineImg.preserveAspect = false;
        scanLineImg.color = WarmWhite;
        scanLineImg.GetComponent<RectTransform>().localScale = Vector3.one;
        scanLineGo.SetActive(false);
        SetCenterPosition(scanLineGo.GetComponent<RectTransform>(), 0f, 100f, 180f, 2f);

        // LaserLineAnimator for one-shot sweep
        var laserAnimator = scanLineGo.AddComponent<LaserLineAnimator>();
        var serialLaser = new SerializedObject(laserAnimator);
        serialLaser.FindProperty("limitY").floatValue = 100f;
        serialLaser.FindProperty("speed").floatValue = 250f;
        serialLaser.ApplyModifiedProperties();

        // Scan target pill
        var scanPillGo = CreateUIObject("ScanTargetPill", scanGo);
        var scanPillImg = scanPillGo.AddComponent<Image>();
        scanPillImg.sprite = btnSprite;
        scanPillImg.type = Image.Type.Sliced;
        scanPillImg.color = DeepForest;
        SetCenterPosition(scanPillGo.GetComponent<RectTransform>(), 0f, -145f, 160f, 30f);

        var scanPillTextGo = CreateUIObject("Text", scanPillGo);
        var scanPillText = scanPillTextGo.AddComponent<TextMeshProUGUI>();
        scanPillText.textWrappingMode = TextWrappingModes.Normal;
        scanPillText.text = "PINDAI TARGET GAMBAR";
        scanPillText.fontSize = 11f;
        scanPillText.fontStyle = FontStyles.Bold;
        scanPillText.color = WarmWhite;
        scanPillText.alignment = TextAlignmentOptions.Center;
        if (font != null) scanPillText.font = font;
        StretchRect(scanPillTextGo.GetComponent<RectTransform>());

        // Instruction card — Deep Forest
        var instructionCardGo = CreateUIObject("InstructionCard", scanGo);
        var instCardImg = instructionCardGo.AddComponent<Image>();
        instCardImg.sprite = btnSprite;
        instCardImg.type = Image.Type.Sliced;
        instCardImg.color = DeepForest;
        SetAnchorBottom(instructionCardGo.GetComponent<RectTransform>(), 0f, 100f, 280f, 52f);

        var hintGo = CreateUIObject("HintText", instructionCardGo);
        var hintText = hintGo.AddComponent<TextMeshProUGUI>();
        hintText.textWrappingMode = TextWrappingModes.Normal;
        hintText.text = "Arahkan kamera ke gambar gerakan";
        hintText.fontSize = 13f;
        hintText.fontStyle = FontStyles.Bold;
        hintText.color = WarmWhite;
        hintText.alignment = TextAlignmentOptions.Center;
        if (font != null) hintText.font = font;
        StretchRect(hintGo.GetComponent<RectTransform>());

        var instSubtitleGo = CreateUIObject("InstructionSubtitle", scanGo);
        var instSubtitle = instSubtitleGo.AddComponent<TextMeshProUGUI>();
        instSubtitle.textWrappingMode = TextWrappingModes.Normal;
        instSubtitle.text = "Pastikan seluruh gambar terlihat jelas";
        instSubtitle.fontSize = 10f;
        instSubtitle.color = SoftSand;
        instSubtitle.alignment = TextAlignmentOptions.Center;
        if (font != null) instSubtitle.font = font;
        SetAnchorBottom(instSubtitleGo.GetComponent<RectTransform>(), 0f, 70f, 280f, 16f);

        // ═══════════════════════════════════════════════════════════
        // G04 — DETECTION TOAST
        // ═══════════════════════════════════════════════════════════
        var toastGo = CreateUIObject("DetectionToast", canvasGo);
        var toastImg = toastGo.AddComponent<Image>();
        toastImg.sprite = btnSprite;
        toastImg.type = Image.Type.Sliced;
        toastImg.color = WarmWhite;
        SetCenterPosition(toastGo.GetComponent<RectTransform>(), 0f, 0f, 200f, 130f);

        var toastCircleGo = CreateUIObject("SuccessCircle", toastGo);
        var toastCircleImg = toastCircleGo.AddComponent<Image>();
        toastCircleImg.sprite = btnSprite;
        toastCircleImg.type = Image.Type.Sliced;
        toastCircleImg.color = ForestGreen;
        SetCenterPosition(toastCircleGo.GetComponent<RectTransform>(), 0f, 28f, 44f, 44f);

        var toastCheckGo = CreateUIObject("Text", toastCircleGo);
        var toastCheck = toastCheckGo.AddComponent<TextMeshProUGUI>();
        toastCheck.text = "✓";
        toastCheck.fontSize = 22f;
        toastCheck.fontStyle = FontStyles.Bold;
        toastCheck.color = WarmWhite;
        toastCheck.alignment = TextAlignmentOptions.Center;
        if (font != null) toastCheck.font = font;
        StretchRect(toastCheckGo.GetComponent<RectTransform>());

        var toastTextGo = CreateUIObject("TitleText", toastGo);
        var toastText = toastTextGo.AddComponent<TextMeshProUGUI>();
        toastText.textWrappingMode = TextWrappingModes.Normal;
        toastText.text = "Gerakan Ditemukan!";
        toastText.fontSize = 14f;
        toastText.fontStyle = FontStyles.Bold;
        toastText.color = DeepForest;
        toastText.alignment = TextAlignmentOptions.Center;
        if (font != null) toastText.font = font;
        SetCenterPosition(toastTextGo.GetComponent<RectTransform>(), 0f, -16f, 180f, 20f);

        // Movement name chip
        var toastPillGo = CreateUIObject("MovementPill", toastGo);
        var toastPillImg = toastPillGo.AddComponent<Image>();
        toastPillImg.sprite = btnSprite;
        toastPillImg.type = Image.Type.Sliced;
        toastPillImg.color = ForestGreen;
        SetCenterPosition(toastPillGo.GetComponent<RectTransform>(), 0f, -40f, 100f, 22f);

        var toastPillTextGo = CreateUIObject("Text", toastPillGo);
        var toastPillText = toastPillTextGo.AddComponent<TextMeshProUGUI>();
        toastPillText.textWrappingMode = TextWrappingModes.Normal;
        toastPillText.text = "SQUAT";
        toastPillText.fontSize = 10f;
        toastPillText.fontStyle = FontStyles.Bold;
        toastPillText.color = WarmWhite;
        toastPillText.alignment = TextAlignmentOptions.Center;
        if (font != null) toastPillText.font = font;
        StretchRect(toastPillTextGo.GetComponent<RectTransform>());
        toastGo.SetActive(false);

        // ═══════════════════════════════════════════════════════════
        // G05 — TRACKING HUD + SLIDER
        // ═══════════════════════════════════════════════════════════
        var arControlsGo = CreateUIObject("ARControls", canvasGo);
        StretchRect(arControlsGo.GetComponent<RectTransform>());
        arControlsGo.SetActive(false);

        // Header
        var arHeaderTitleGo = CreateUIObject("HeaderTitle", arControlsGo);
        var arHeaderTitle = arHeaderTitleGo.AddComponent<TextMeshProUGUI>();
        arHeaderTitle.textWrappingMode = TextWrappingModes.Normal;
        arHeaderTitle.text = "GerakAR";
        arHeaderTitle.fontSize = 20f;
        arHeaderTitle.fontStyle = FontStyles.Bold;
        arHeaderTitle.color = WarmWhite;
        arHeaderTitle.alignment = TextAlignmentOptions.Center;
        if (font != null) arHeaderTitle.font = font;
        SetAnchorTop(arHeaderTitleGo.GetComponent<RectTransform>(), 0f, -48f, 200f, 28f);

        var arHeaderSubGo = CreateUIObject("HeaderSub", arControlsGo);
        var arHeaderSub = arHeaderSubGo.AddComponent<TextMeshProUGUI>();
        arHeaderSub.textWrappingMode = TextWrappingModes.Normal;
        arHeaderSub.text = "Belajar Gerak Jadi Seru";
        arHeaderSub.fontSize = 10f;
        arHeaderSub.color = SoftSand;
        arHeaderSub.alignment = TextAlignmentOptions.Center;
        if (font != null) arHeaderSub.font = font;
        SetAnchorTop(arHeaderSubGo.GetComponent<RectTransform>(), 0f, -76f, 200f, 16f);

        // ── Floating Action Buttons Column ──
        var fabColumnGo = CreateUIObject("FABColumn", arControlsGo);
        var fabVlg = fabColumnGo.AddComponent<VerticalLayoutGroup>();
        fabVlg.spacing = 12f;
        fabVlg.childAlignment = TextAnchor.MiddleCenter;
        fabVlg.childControlWidth = true;
        fabVlg.childControlHeight = true;
        fabVlg.childForceExpandWidth = false;
        fabVlg.childForceExpandHeight = false;
        var fabCSF = fabColumnGo.AddComponent<ContentSizeFitter>();
        fabCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fabCSF.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        SetAnchorBottomRight(fabColumnGo.GetComponent<RectTransform>(), -16f, 120f, 48f, 160f);

        // FAB: Audio Play/Pause
        var playPauseGo = CreateFAB(fabColumnGo, "PlayPauseButton", DeepForest, "play", font);
        var playPauseBtn = playPauseGo.GetComponent<Button>();

        // FAB: Material
        var matBtnGo = CreateFAB(fabColumnGo, "MaterialButton", ForestGreen, "book", font);
        var matBtn = matBtnGo.GetComponent<Button>();

        // FAB: Close
        var closeGo = CreateFAB(fabColumnGo, "CloseButton", DeepForest, "x", font);
        var closeBtn = closeGo.GetComponent<Button>();

        // ── Timeline Card (G05 Bottom) ──
        var timelineRootGo = CreateUIObject("TimelineCard", arControlsGo);
        var tlCardImg = timelineRootGo.AddComponent<Image>();
        tlCardImg.sprite = btnSprite;
        tlCardImg.type = Image.Type.Sliced;
        tlCardImg.color = WarmWhite;
        SetAnchorBottom(timelineRootGo.GetComponent<RectTransform>(), 0f, 16f, 328f, 120f);

        // Info row: movement name tag + status
        var tlInfoRowGo = CreateUIObject("InfoRow", timelineRootGo);
        SetCenterPosition(tlInfoRowGo.GetComponent<RectTransform>(), 0f, 35f, 300f, 20f);

        var squatTagGo = CreateUIObject("SquatTag", tlInfoRowGo);
        var squatTagImg = squatTagGo.AddComponent<Image>();
        squatTagImg.sprite = btnSprite;
        squatTagImg.type = Image.Type.Sliced;
        squatTagImg.color = SoftSand;
        SetCenterPosition(squatTagGo.GetComponent<RectTransform>(), -110f, 0f, 80f, 22f);

        var nameLabelGo = CreateUIObject("Name", squatTagGo);
        var nameLabel = nameLabelGo.AddComponent<TextMeshProUGUI>();
        nameLabel.textWrappingMode = TextWrappingModes.Normal;
        nameLabel.text = "Squat";
        nameLabel.fontSize = 12f;
        nameLabel.fontStyle = FontStyles.Bold;
        nameLabel.color = DeepForest;
        nameLabel.alignment = TextAlignmentOptions.Center;
        if (font != null) nameLabel.font = font;
        StretchRect(nameLabelGo.GetComponent<RectTransform>());

        var statusTagGo = CreateUIObject("StatusTag", tlInfoRowGo);
        var statusTag = statusTagGo.AddComponent<TextMeshProUGUI>();
        statusTag.textWrappingMode = TextWrappingModes.Normal;
        statusTag.text = "Status: Loop";
        statusTag.fontSize = 10f;
        statusTag.fontStyle = FontStyles.Bold;
        statusTag.color = ForestGreen;
        statusTag.alignment = TextAlignmentOptions.Right;
        if (font != null) statusTag.font = font;
        SetCenterPosition(statusTagGo.GetComponent<RectTransform>(), 103.3f, 0f, 80f, 14f);

        // ── Reference Slider (G05) ──
        var sliderGo = CreateUIObject("PoseSlider", timelineRootGo);
        var slider = sliderGo.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        SetCenterPosition(sliderGo.GetComponent<RectTransform>(), 0f, 2f, 300f, 32f);

        // Track Container
        var trackContainerGo = CreateUIObject("TrackContainer", sliderGo);
        var tcRT = trackContainerGo.GetComponent<RectTransform>();
        tcRT.anchorMin = new Vector2(0f, 0f);
        tcRT.anchorMax = new Vector2(1f, 1f);
        tcRT.pivot = new Vector2(0.5f, 0.5f);
        tcRT.anchoredPosition = Vector2.zero;
        tcRT.sizeDelta = new Vector2(-24f, -24f); // padding 12 each side

        // Track Background (capsule)
        var trackBgGo = CreateUIObject("TrackBackground", trackContainerGo);
        var trackBgImg = trackBgGo.AddComponent<Image>();
        trackBgImg.sprite = btnSprite;
        trackBgImg.type = Image.Type.Sliced;
        trackBgImg.color = SoftSand;
        var tbgRT = trackBgGo.GetComponent<RectTransform>();
        tbgRT.anchorMin = new Vector2(0f, 0.5f);
        tbgRT.anchorMax = new Vector2(1f, 0.5f);
        tbgRT.pivot = new Vector2(0.5f, 0.5f);
        tbgRT.anchoredPosition = Vector2.zero;
        tbgRT.sizeDelta = new Vector2(0f, 30f);

        // Active Fill — LeftCap + FillBody + RightCap
        var activeFillGo = CreateUIObject("ActiveFill", trackContainerGo);
        var afRT = activeFillGo.GetComponent<RectTransform>();
        afRT.anchorMin = new Vector2(0f, 0f);
        afRT.anchorMax = new Vector2(0f, 1f);
        afRT.pivot = new Vector2(0f, 0.5f);
        afRT.anchoredPosition = Vector2.zero;
        afRT.sizeDelta = Vector2.zero;

        // Left Cap
        var leftCapGo = CreateUIObject("LeftCap", activeFillGo);
        var leftCapImg = leftCapGo.AddComponent<Image>();
        leftCapImg.sprite = btnSprite;
        leftCapImg.type = Image.Type.Sliced;
        leftCapImg.color = ForestGreen;
        leftCapImg.preserveAspect = true;
        var lcRT = leftCapGo.GetComponent<RectTransform>();
        lcRT.anchorMin = new Vector2(0f, 0.5f);
        lcRT.anchorMax = new Vector2(0f, 0.5f);
        lcRT.pivot = new Vector2(0.5f, 0.5f);
        lcRT.anchoredPosition = Vector2.zero;
        lcRT.sizeDelta = new Vector2(8f, 8f);

        // Fill Body
        var fillBodyGo = CreateUIObject("FillBody", activeFillGo);
        var fillBodyImg = fillBodyGo.AddComponent<Image>();
        fillBodyImg.sprite = uiSolidRect;
        fillBodyImg.type = Image.Type.Simple;
        fillBodyImg.preserveAspect = false;
        fillBodyImg.color = ForestGreen;
        fillBodyImg.GetComponent<RectTransform>().localScale = Vector3.one;
        var fbRT = fillBodyGo.GetComponent<RectTransform>();
        fbRT.anchorMin = new Vector2(0f, 0f);
        fbRT.anchorMax = new Vector2(1f, 1f);
        fbRT.pivot = new Vector2(0f, 0.5f);
        fbRT.anchoredPosition = Vector2.zero;
        fbRT.sizeDelta = new Vector2(0f, 0f);

        // Right Cap
        var rightCapGo = CreateUIObject("RightCap", activeFillGo);
        var rightCapImg = rightCapGo.AddComponent<Image>();
        rightCapImg.sprite = btnSprite;
        rightCapImg.type = Image.Type.Sliced;
        rightCapImg.color = ForestGreen;
        rightCapImg.preserveAspect = true;
        var rcRT = rightCapGo.GetComponent<RectTransform>();
        rcRT.anchorMin = new Vector2(1f, 0.5f);
        rcRT.anchorMax = new Vector2(1f, 0.5f);
        rcRT.pivot = new Vector2(0.5f, 0.5f);
        rcRT.anchoredPosition = Vector2.zero;
        rcRT.sizeDelta = new Vector2(8f, 8f);

        // Handle
        var handleAreaGo = CreateUIObject("HandleTouchArea", sliderGo);
        handleAreaGo.AddComponent<Image>().color = Color.clear;
        var haRT = handleAreaGo.GetComponent<RectTransform>();
        haRT.anchorMin = new Vector2(0f, 0f);
        haRT.anchorMax = new Vector2(1f, 1f);
        haRT.pivot = new Vector2(0.5f, 0.5f);
        haRT.anchoredPosition = Vector2.zero;
        haRT.sizeDelta = Vector2.zero;

        var handleVisGo = CreateUIObject("HandleVisual", handleAreaGo);
        var handleVisImg = handleVisGo.AddComponent<Image>();
        handleVisImg.sprite = btnSprite;
        handleVisImg.type = Image.Type.Sliced;
        handleVisImg.color = WarmWhite;
        handleVisImg.preserveAspect = true;
        var hvRT = handleVisGo.GetComponent<RectTransform>();
        hvRT.localScale = Vector3.one;
        hvRT.sizeDelta = new Vector2(28f, 28f);
        slider.handleRect = hvRT;

        // Node container (for key pose markers)
        var nodeContainerGo = CreateUIObject("NodeContainer", trackContainerGo);
        var ncRT = nodeContainerGo.GetComponent<RectTransform>();
        ncRT.anchorMin = new Vector2(0f, 0f);
        ncRT.anchorMax = new Vector2(1f, 1f);
        ncRT.pivot = new Vector2(0.5f, 0.5f);
        ncRT.anchoredPosition = Vector2.zero;
        ncRT.sizeDelta = Vector2.zero;

        // Timeline markers (sample markers — real ones populated by code)
        var markerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/App/Prefabs/TimelineMarker.prefab");

        // Bottom labels row
        var tlBottomLabelsGo = CreateUIObject("BottomLabels", timelineRootGo);
        SetCenterPosition(tlBottomLabelsGo.GetComponent<RectTransform>(), 0f, -28f, 300f, 14f);

        var startLabelGo = CreateUIObject("Mulai", tlBottomLabelsGo);
        var startLabel = startLabelGo.AddComponent<TextMeshProUGUI>();
        startLabel.textWrappingMode = TextWrappingModes.Normal;
        startLabel.text = "Mulai";
        startLabel.fontSize = 11f;
        startLabel.color = SecondaryText;
        startLabel.alignment = TextAlignmentOptions.Left;
        if (font != null) startLabel.font = font;
        SetCenterPosition(startLabelGo.GetComponent<RectTransform>(), -130f, 0f, 50f, 14f);

        var midLabelGo = CreateUIObject("Petunjuk", tlBottomLabelsGo);
        var midLabel = midLabelGo.AddComponent<TextMeshProUGUI>();
        midLabel.textWrappingMode = TextWrappingModes.Normal;
        midLabel.text = "Geser untuk memeriksa pose";
        midLabel.fontSize = 11f;
        midLabel.color = SecondaryText;
        midLabel.alignment = TextAlignmentOptions.Center;
        if (font != null) midLabel.font = font;
        SetCenterPosition(midLabelGo.GetComponent<RectTransform>(), 0f, 0f, 160f, 14f);

        var endLabelGo = CreateUIObject("Selesai", tlBottomLabelsGo);
        var endLabel = endLabelGo.AddComponent<TextMeshProUGUI>();
        endLabel.textWrappingMode = TextWrappingModes.Normal;
        endLabel.text = "Selesai";
        endLabel.fontSize = 11f;
        endLabel.color = SecondaryText;
        endLabel.alignment = TextAlignmentOptions.Right;
        if (font != null) endLabel.font = font;
        SetCenterPosition(endLabelGo.GetComponent<RectTransform>(), 130f, 0f, 50f, 14f);

        // PoseTimelineController wiring
        var timelineCtrl = timelineRootGo.AddComponent<PoseTimelineController>();
        var serialTimeline = new SerializedObject(timelineCtrl);
        serialTimeline.FindProperty("timelineSlider").objectReferenceValue = slider;
        serialTimeline.FindProperty("movementController").objectReferenceValue = movementController;
        serialTimeline.FindProperty("markerContainer").objectReferenceValue = nodeContainerGo;
        serialTimeline.FindProperty("markerPrefab").objectReferenceValue = markerPrefab;
        serialTimeline.ApplyModifiedProperties();

        // ═══════════════════════════════════════════════════════════
        // G06 — BOTTOM SHEET
        // ═══════════════════════════════════════════════════════════
        var sheetGo = CreateUIObject("BottomSheet", canvasGo);
        var sheetImg = sheetGo.AddComponent<Image>();
        sheetImg.sprite = roundTopSprite;
        sheetImg.type = Image.Type.Sliced;
        sheetImg.color = WarmWhite;
        var sheetRT = sheetGo.GetComponent<RectTransform>();
        sheetRT.anchorMin = new Vector2(0f, 0f);
        sheetRT.anchorMax = new Vector2(1f, 0f);
        sheetRT.pivot = new Vector2(0.5f, 1f);
        sheetRT.anchoredPosition = new Vector2(0f, 0f);
        sheetRT.sizeDelta = new Vector2(0f, 752f);

        // Header area — Deep Forest
        var sheetHeaderGo = CreateUIObject("SheetHeader", sheetGo);
        var sheetHeaderImg = sheetHeaderGo.AddComponent<Image>();
        sheetHeaderImg.sprite = btnSprite;
        sheetHeaderImg.type = Image.Type.Sliced;
        sheetHeaderImg.color = DeepForest;
        var shRT = sheetHeaderGo.GetComponent<RectTransform>();
        shRT.anchorMin = new Vector2(0f, 1f);
        shRT.anchorMax = new Vector2(1f, 1f);
        shRT.pivot = new Vector2(0.5f, 1f);
        shRT.anchoredPosition = Vector2.zero;
        shRT.sizeDelta = new Vector2(0f, 56f);

        // Grab handle
        var handleGo = CreateUIObject("GrabHandle", sheetGo);
        var handleImg = handleGo.AddComponent<Image>();
        handleImg.sprite = uiSolidRect;
        handleImg.type = Image.Type.Simple;
        handleImg.preserveAspect = false;
        handleImg.color = SoftSand;
        SetCenterPosition(handleGo.GetComponent<RectTransform>(), 0f, sheetRT.sizeDelta.y - 8f, 40f, 4f);

        // Category label
        var categoryGo = CreateUIObject("CategoryTypeLabel", sheetHeaderGo);
        var categoryTxt = categoryGo.AddComponent<TextMeshProUGUI>();
        categoryTxt.textWrappingMode = TextWrappingModes.Normal;
        categoryTxt.text = "GERAKAN UTAMA";
        categoryTxt.fontSize = 10f;
        categoryTxt.fontStyle = FontStyles.Bold;
        categoryTxt.color = SoftSand;
        if (font != null) categoryTxt.font = font;
        SetCenterPosition(categoryGo.GetComponent<RectTransform>(), -120f, -8f, 120f, 16f);

        var sheetTitleGo = CreateUIObject("MovementTitle", sheetHeaderGo);
        var sheetTitleText = sheetTitleGo.AddComponent<TextMeshProUGUI>();
        sheetTitleText.textWrappingMode = TextWrappingModes.Normal;
        sheetTitleText.text = "SQUAT";
        sheetTitleText.fontSize = 16f;
        sheetTitleText.fontStyle = FontStyles.Bold;
        sheetTitleText.color = WarmWhite;
        sheetTitleText.alignment = TextAlignmentOptions.Left;
        if (font != null) sheetTitleText.font = font;
        SetCenterPosition(sheetTitleGo.GetComponent<RectTransform>(), -120f, -32f, 160f, 20f);

        // Back to primary button (G07 -> G06)
        var backBtnGo = CreateUIObject("BackToPrimaryButton", sheetHeaderGo);
        var backBtnImg = backBtnGo.AddComponent<Image>();
        backBtnImg.sprite = btnSprite;
        backBtnImg.type = Image.Type.Sliced;
        backBtnImg.color = ForestGreen;
        var backBtn = backBtnGo.AddComponent<Button>();
        SetCenterPosition(backBtnGo.GetComponent<RectTransform>(), 110f, -20f, 80f, 28f);

        var backBtnTextGo = CreateUIObject("Text", backBtnGo);
        var backBtnText = backBtnTextGo.AddComponent<TextMeshProUGUI>();
        backBtnText.textWrappingMode = TextWrappingModes.Normal;
        backBtnText.text = "Kembali";
        backBtnText.fontSize = 11f;
        backBtnText.fontStyle = FontStyles.Bold;
        backBtnText.color = WarmWhite;
        backBtnText.alignment = TextAlignmentOptions.Center;
        if (font != null) backBtnText.font = font;
        StretchRect(backBtnTextGo.GetComponent<RectTransform>());

        // Close X button on header
        var sheetCloseGo = CreateUIObject("SheetCloseX", sheetHeaderGo);
        var sheetCloseImg = sheetCloseGo.AddComponent<Image>();
        sheetCloseImg.sprite = btnSprite;
        sheetCloseImg.type = Image.Type.Sliced;
        sheetCloseImg.color = ForestGreen;
        var sheetCloseBtn = sheetCloseGo.AddComponent<Button>();
        SetCenterPosition(sheetCloseGo.GetComponent<RectTransform>(), 150f, -20f, 32f, 32f);

        var sheetCloseTxtGo = CreateUIObject("Text", sheetCloseGo);
        var sheetCloseTxt = sheetCloseTxtGo.AddComponent<TextMeshProUGUI>();
        sheetCloseTxt.text = "X";
        sheetCloseTxt.fontSize = 14f;
        sheetCloseTxt.fontStyle = FontStyles.Bold;
        sheetCloseTxt.color = WarmWhite;
        sheetCloseTxt.alignment = TextAlignmentOptions.Center;
        if (font != null) sheetCloseTxt.font = font;
        StretchRect(sheetCloseTxtGo.GetComponent<RectTransform>());

        // Scroll view
        var scrollViewGo = CreateUIObject("ScrollView", sheetGo);
        var scrollRect = scrollViewGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        SetCenterPosition(scrollViewGo.GetComponent<RectTransform>(), 0f, -40f, 320f, sheetRT.sizeDelta.y - 80f);

        var viewportGo = CreateUIObject("Viewport", scrollViewGo);
        viewportGo.AddComponent<RectMask2D>();
        StretchRect(viewportGo.GetComponent<RectTransform>());
        scrollRect.viewport = viewportGo.GetComponent<RectTransform>();

        var contentGo = CreateUIObject("Content", viewportGo);
        var contentRT = contentGo.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0f, 1000f);

        var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 24f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var csf = contentGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.content = contentRT;

        // Section: Tentang Gerakan
        var aboutGroupGo = CreateUIObject("AboutSection", contentGo);
        var aboutVlg = aboutGroupGo.AddComponent<VerticalLayoutGroup>();
        aboutVlg.spacing = 8f;
        aboutVlg.childControlWidth = true;
        aboutVlg.childControlHeight = true;

        var aboutTitleGo = CreateUIObject("Title", aboutGroupGo);
        var aboutTitle = aboutTitleGo.AddComponent<TextMeshProUGUI>();
        aboutTitle.textWrappingMode = TextWrappingModes.Normal;
        aboutTitle.text = "TENTANG GERAKAN";
        aboutTitle.fontSize = 13f;
        aboutTitle.fontStyle = FontStyles.Bold;
        aboutTitle.color = DeepForest;
        if (font != null) aboutTitle.font = font;

        var descGo = CreateUIObject("Description", aboutGroupGo);
        var descText = descGo.AddComponent<TextMeshProUGUI>();
        descText.textWrappingMode = TextWrappingModes.Normal;
        descText.fontSize = 13f;
        descText.color = SecondaryText;
        if (font != null) descText.font = font;

        // Section: Cara Melakukan
        var stepsGroupGo = CreateUIObject("StepsSection", contentGo);
        var stepsVlg = stepsGroupGo.AddComponent<VerticalLayoutGroup>();
        stepsVlg.spacing = 12f;
        stepsVlg.childControlWidth = true;
        stepsVlg.childControlHeight = true;

        var stepsTitleGo = CreateUIObject("Title", stepsGroupGo);
        var stepsTitle = stepsTitleGo.AddComponent<TextMeshProUGUI>();
        stepsTitle.textWrappingMode = TextWrappingModes.Normal;
        stepsTitle.text = "CARA MELAKUKAN";
        stepsTitle.fontSize = 13f;
        stepsTitle.fontStyle = FontStyles.Bold;
        stepsTitle.color = DeepForest;
        if (font != null) stepsTitle.font = font;

        var stepsContainerGo = CreateUIObject("Container", stepsGroupGo);
        var stepsContVlg = stepsContainerGo.AddComponent<VerticalLayoutGroup>();
        stepsContVlg.spacing = 10f;
        stepsContVlg.childControlWidth = true;
        stepsContVlg.childControlHeight = true;

        // Section: Safety Tip Card
        var safetyTipCardGo = CreateUIObject("SafetyTipCard", contentGo);
        var safetyImg = safetyTipCardGo.AddComponent<Image>();
        safetyImg.sprite = btnSprite;
        safetyImg.type = Image.Type.Sliced;
        safetyImg.color = ForestGreen;
        safetyImg.color = new Color(0.12f, 0.365f, 0.259f, 0.1f);
        var safetyVlg = safetyTipCardGo.AddComponent<VerticalLayoutGroup>();
        safetyVlg.padding = new RectOffset(16, 16, 16, 16);

        var safetyTextGo = CreateUIObject("Text", safetyTipCardGo);
        var safetyText = safetyTextGo.AddComponent<TextMeshProUGUI>();
        safetyText.textWrappingMode = TextWrappingModes.Normal;
        safetyText.fontSize = 12f;
        safetyText.color = DeepForest;
        safetyText.fontStyle = FontStyles.Bold;
        if (font != null) safetyText.font = font;

        // Section: Full State Extras
        var fullExtrasGo = CreateUIObject("FullStateExtras", contentGo);
        var extrasVlg = fullExtrasGo.AddComponent<VerticalLayoutGroup>();
        extrasVlg.spacing = 24f;
        extrasVlg.childControlWidth = true;
        extrasVlg.childControlHeight = true;

        var mistakesTitleGo = CreateUIObject("MistakesTitle", fullExtrasGo);
        var mistakesTitle = mistakesTitleGo.AddComponent<TextMeshProUGUI>();
        mistakesTitle.textWrappingMode = TextWrappingModes.Normal;
        mistakesTitle.text = "HINDARI INI";
        mistakesTitle.fontSize = 13f;
        mistakesTitle.fontStyle = FontStyles.Bold;
        mistakesTitle.color = DeepForest;
        if (font != null) mistakesTitle.font = font;

        var mistakesContainerGo = CreateUIObject("MistakesContainer", fullExtrasGo);
        var mistakesContVlg = mistakesContainerGo.AddComponent<VerticalLayoutGroup>();
        mistakesContVlg.spacing = 8f;
        mistakesContVlg.childControlWidth = true;
        mistakesContVlg.childControlHeight = true;

        var trainedTitleGo = CreateUIObject("TrainedTitle", fullExtrasGo);
        var trainedTitle = trainedTitleGo.AddComponent<TextMeshProUGUI>();
        trainedTitle.textWrappingMode = TextWrappingModes.Normal;
        trainedTitle.text = "OTOT YANG TERLATIH";
        trainedTitle.fontSize = 13f;
        trainedTitle.fontStyle = FontStyles.Bold;
        trainedTitle.color = DeepForest;
        if (font != null) trainedTitle.font = font;

        var trainedContainerGo = CreateUIObject("TrainedContainer", fullExtrasGo);
        var trainedContVlg = trainedContainerGo.AddComponent<VerticalLayoutGroup>();
        trainedContVlg.spacing = 8f;
        trainedContVlg.childControlWidth = true;
        trainedContVlg.childControlHeight = true;

        // Section: Related Movements
        var relatedGroupGo = CreateUIObject("RelatedGroup", contentGo);
        var relVlg = relatedGroupGo.AddComponent<VerticalLayoutGroup>();
        relVlg.spacing = 12f;
        relVlg.childControlWidth = true;
        relVlg.childControlHeight = true;

        var relatedTitleGo = CreateUIObject("Title", relatedGroupGo);
        var relatedTitle = relatedTitleGo.AddComponent<TextMeshProUGUI>();
        relatedTitle.textWrappingMode = TextWrappingModes.Normal;
        relatedTitle.text = "GERAKAN SERUPA";
        relatedTitle.fontSize = 13f;
        relatedTitle.fontStyle = FontStyles.Bold;
        relatedTitle.color = DeepForest;
        if (font != null) relatedTitle.font = font;

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
        relContentRT.sizeDelta = new Vector2(800f, 180f);
        relScroll.content = relContentRT;

        var hlg = relContentGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        var relCsf = relContentGo.AddComponent<ContentSizeFitter>();
        relCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        relCsf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Scrim
        var scrimGo = CreateUIObject("Scrim", canvasGo);
        var scrimImg = scrimGo.AddComponent<Image>();
        scrimImg.color = new Color(0.07f, 0.216f, 0.165f, 0.32f);
        StretchRect(scrimGo.GetComponent<RectTransform>());
        scrimGo.AddComponent<Button>();
        scrimGo.SetActive(false);
        sheetGo.transform.SetAsLastSibling();

        var sheetCtrl = sheetGo.AddComponent<BottomSheetController>();
        var serialSheet = new SerializedObject(sheetCtrl);
        serialSheet.FindProperty("sheetRect").objectReferenceValue = sheetRT;
        serialSheet.FindProperty("scrim").objectReferenceValue = scrimGo;
        serialSheet.FindProperty("closeButton").objectReferenceValue = sheetCloseBtn;
        serialSheet.FindProperty("movementController").objectReferenceValue = movementController;
        serialSheet.ApplyModifiedProperties();

        var matCtrl = sheetGo.AddComponent<MaterialContentController>();
        var serialMat = new SerializedObject(matCtrl);

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
        serialUI.FindProperty("playPauseIcon").objectReferenceValue = null;
        serialUI.ApplyModifiedProperties();

        // Finish tracking wiring
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

    // ── Helper methods ────────────────────────────────────────────────

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
            trackables[0].TwoDImageFile != "C5.png" ||
            !Mathf.Approximately(trackables[0].TwoDImageWidth, 0.12f))
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

        if (Object.FindAnyObjectByType<ARUnityXURPBackgroundPresenter>() == null)
            throw new BuildFailedException("ARUnityX URP background presenter is missing.");

        Debug.Log("[GerakAR] ARUnityX scene validation passed.");
    }

    private static void ConfigureRearCamera(ARXVideoConfig videoConfig)
    {
        int index = videoConfig.configs.FindIndex(config => config.platform == RuntimePlatform.Android);
        if (index < 0)
            throw new System.InvalidOperationException("ARUnityX Android video configuration is missing.");

        var config = videoConfig.configs[index];
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

    // ── UI Creation Helpers ───────────────────────────────────────────

    private static GameObject CreateCanvas(string name, GameObject parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        if (parent != null) go.transform.SetParent(parent.transform, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(360f, 800f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    private static void CreateOnboardingCard(GameObject parent, string name, string number,
        string description, float x, float y, Sprite btnSprite, TMP_FontAsset font)
    {
        var cardGo = CreateUIObject(name, parent);
        var cardImg = cardGo.AddComponent<Image>();
        cardImg.sprite = btnSprite;
        cardImg.type = Image.Type.Sliced;
        cardImg.color = WarmWhite;
        SetCenterPosition(cardGo.GetComponent<RectTransform>(), x, y, 306.7f, 52f);

        var numCircleGo = CreateUIObject("NumCircle", cardGo);
        var numCircleImg = numCircleGo.AddComponent<Image>();
        numCircleImg.sprite = btnSprite;
        numCircleImg.type = Image.Type.Sliced;
        numCircleImg.color = ForestGreen;
        SetCenterPosition(numCircleGo.GetComponent<RectTransform>(), -126.7f, 0f, 28f, 28f);

        var numTextGo = CreateUIObject("Text", numCircleGo);
        var numText = numTextGo.AddComponent<TextMeshProUGUI>();
        numText.text = number;
        numText.fontSize = 14f;
        numText.fontStyle = FontStyles.Bold;
        numText.color = WarmWhite;
        numText.alignment = TextAlignmentOptions.Center;
        if (font != null) numText.font = font;
        StretchRect(numTextGo.GetComponent<RectTransform>());

        var descGo = CreateUIObject("Desc", cardGo);
        var desc = descGo.AddComponent<TextMeshProUGUI>();
        desc.textWrappingMode = TextWrappingModes.Normal;
        desc.text = description;
        desc.fontSize = 12f;
        desc.color = SecondaryText;
        desc.alignment = TextAlignmentOptions.Left;
        if (font != null) desc.font = font;
        SetCenterPosition(descGo.GetComponent<RectTransform>(), 20f, 0f, 220f, 30f);
    }

    private static (Button, GameObject, GameObject) CreateMovementCard(
        GameObject parent, string name, string icon, string title, string subtitle,
        float x, float y, Sprite btnSprite, TMP_FontAsset font)
    {
        var cardGo = CreateUIObject(name, parent);
        var cardImg = cardGo.AddComponent<Image>();
        cardImg.sprite = btnSprite;
        cardImg.type = Image.Type.Sliced;
        cardImg.color = WarmWhite;
        SetCenterPosition(cardGo.GetComponent<RectTransform>(), x, y, 300f, 56f);

        var iconGo = CreateUIObject("Icon", cardGo);
        var iconText = iconGo.AddComponent<TextMeshProUGUI>();
        iconText.text = icon;
        iconText.fontSize = 14f;
        iconText.fontStyle = FontStyles.Bold;
        iconText.color = ForestGreen;
        iconText.alignment = TextAlignmentOptions.Center;
        if (font != null) iconText.font = font;
        SetCenterPosition(iconGo.GetComponent<RectTransform>(), -120f, 0f, 30f, 30f);

        var titleGo = CreateUIObject("TitleText", cardGo);
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.textWrappingMode = TextWrappingModes.Normal;
        titleText.text = $"<b>{title}</b>\n<size=10><color=#716040>{subtitle}</color></size>";
        titleText.fontSize = 12f;
        titleText.color = DeepForest;
        titleText.alignment = TextAlignmentOptions.Left;
        if (font != null) titleText.font = font;
        SetCenterPosition(titleGo.GetComponent<RectTransform>(), 20f, 0f, 180f, 36f);

        var bukaGo = CreateUIObject("BukaButton", cardGo);
        var bukaImg = bukaGo.AddComponent<Image>();
        bukaImg.sprite = btnSprite;
        bukaImg.type = Image.Type.Sliced;
        bukaImg.color = ForestGreen;
        var bukaBtn = bukaGo.AddComponent<Button>();
        SetCenterPosition(bukaGo.GetComponent<RectTransform>(), 110f, 0f, 52f, 28f);

        var bukaTextGo = CreateUIObject("Text", bukaGo);
        var bukaText = bukaTextGo.AddComponent<TextMeshProUGUI>();
        bukaText.text = "Buka";
        bukaText.fontSize = 11f;
        bukaText.fontStyle = FontStyles.Bold;
        bukaText.color = WarmWhite;
        bukaText.alignment = TextAlignmentOptions.Center;
        if (font != null) bukaText.font = font;
        StretchRect(bukaTextGo.GetComponent<RectTransform>());

        return (bukaBtn, cardGo, bukaGo);
    }

    private static GameObject CreateFAB(GameObject parent, string name, Color bgColor, string iconText, TMP_FontAsset font)
    {
        var go = CreateUIObject(name, parent);
        var img = go.AddComponent<Image>();
        img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/MobileARTemplateAssets/UI/Sprites/ActivationButtonOpaque.png");
        img.type = Image.Type.Sliced;
        img.color = bgColor;
        go.AddComponent<Button>();

        var goRT = go.GetComponent<RectTransform>();
        goRT.sizeDelta = new Vector2(48f, 48f);

        var iconGo = CreateUIObject("Icon", go);
        var iconTxt = iconGo.AddComponent<TextMeshProUGUI>();
        iconTxt.text = iconText;
        iconTxt.fontSize = 18f;
        iconTxt.fontStyle = FontStyles.Bold;
        iconTxt.color = WarmWhite;
        iconTxt.alignment = TextAlignmentOptions.Center;
        if (font != null) iconTxt.font = font;
        StretchRect(iconGo.GetComponent<RectTransform>());

        return go;
    }

    private static GameObject CreateCollapsibleWarning(GameObject parent, Sprite btnSprite,
        TMP_FontAsset font, string iconPath)
    {
        var warnGo = CreateUIObject("CollapsibleWarning", parent);
        var warnImg = warnGo.AddComponent<Image>();
        warnImg.sprite = btnSprite;
        warnImg.type = Image.Type.Sliced;
        warnImg.color = ForestGreen;
        warnImg.color = new Color(0.12f, 0.365f, 0.259f, 0.1f);

        var layout = warnGo.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.padding = new RectOffset(12, 12, 10, 10);

        // Top row: icon + title + chevron
        var topRowGo = CreateUIObject("TopRow", warnGo);
        var topRowRT = topRowGo.GetComponent<RectTransform>();
        topRowRT.sizeDelta = new Vector2(0f, 24f);

        var iconGo = CreateUIObject("Icon", topRowGo);
        var iconTextGo = iconGo.AddComponent<TextMeshProUGUI>();
        iconTextGo.text = "i";
        iconTextGo.fontSize = 16f;
        iconTextGo.fontStyle = FontStyles.Bold;
        iconTextGo.color = ForestGreen;
        iconTextGo.alignment = TextAlignmentOptions.Center;
        if (font != null) iconTextGo.font = font;
        SetCenterPosition(iconGo.GetComponent<RectTransform>(), -130f, 0f, 22f, 22f);

        var titleWarnGo = CreateUIObject("Title", topRowGo);
        var titleWarnText = titleWarnGo.AddComponent<TextMeshProUGUI>();
        titleWarnText.text = "Mode tanpa kamera";
        titleWarnText.fontSize = 13f;
        titleWarnText.fontStyle = FontStyles.Bold;
        titleWarnText.color = DeepForest;
        titleWarnText.alignment = TextAlignmentOptions.Left;
        if (font != null) titleWarnText.font = font;
        SetCenterPosition(titleWarnGo.GetComponent<RectTransform>(), 0f, 0f, 180f, 22f);

        var chevronGo = CreateUIObject("Chevron", topRowGo);
        var chevronText = chevronGo.AddComponent<TextMeshProUGUI>();
        chevronText.text = "▼";
        chevronText.fontSize = 14f;
        chevronText.color = ForestGreen;
        chevronText.alignment = TextAlignmentOptions.Center;
        if (font != null) chevronText.font = font;
        SetCenterPosition(chevronGo.GetComponent<RectTransform>(), 130f, 0f, 22f, 22f);

        // Expanded content (hidden by default)
        var expandedGo = CreateUIObject("ExpandedContent", warnGo);
        var expandedText = expandedGo.AddComponent<TextMeshProUGUI>();
        expandedText.textWrappingMode = TextWrappingModes.Normal;
        expandedText.text = "Perangkat belum mendukung mode AR. Kamu tetap dapat mempelajari seluruh gerakan, materi, dan panduan audio tanpa kamera.";
        expandedText.fontSize = 12f;
        expandedText.color = SecondaryText;
        if (font != null) expandedText.font = font;
        expandedGo.SetActive(false);

        // Button for toggling
        var toggleBtn = warnGo.AddComponent<Button>();
        var serialWarn = new SerializedObject(toggleBtn);

        // Store references for runtime
        var toggleComp = warnGo.AddComponent<CollapsibleWarningToggle>();
        var serialToggle = new SerializedObject(toggleComp);
        serialToggle.FindProperty("expandedContent").objectReferenceValue = expandedGo;
        serialToggle.FindProperty("chevronText").objectReferenceValue = chevronText;
        serialToggle.ApplyModifiedProperties();

        return warnGo;
    }

    // ── RectTransform helpers ─────────────────────────────────────────

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

    private static void SetAnchorTop(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
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

    private static GameObject CreateUIObject(string name, GameObject parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        if (parent != null)
            go.transform.SetParent(parent.transform, false);
        return go;
    }
}
