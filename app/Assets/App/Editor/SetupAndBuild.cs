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
using System.IO;
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
        // Automatically generate fonts & shape sprites first
        FontSetupHelper.SetupPoppinsFonts();
        CreateUIShapeSprites.Execute();
        ImportRelatedMovementsSprites();
        AssignRelatedMovementsSprites();
        Debug.Log("[GerakAR] Memulai setup scene...");

        var poppinsRegular = FontSetupHelper.LoadPoppinsFont("Regular") ??
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/MobileARTemplateAssets/UI/Fonts/Inter-Regular_SDF.asset");
        var poppinsMedium = FontSetupHelper.LoadPoppinsFont("Medium") ?? poppinsRegular;
        var poppinsSemiBold = FontSetupHelper.LoadPoppinsFont("SemiBold") ?? poppinsRegular;
        var poppinsBold = FontSetupHelper.LoadPoppinsFont("Bold") ?? poppinsRegular;
        var fonts = new FontSet(poppinsRegular, poppinsMedium, poppinsSemiBold, poppinsBold);
        var btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
        var uiSolidRect = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/App/UI/Sprites/Shapes/UISolidRectangle.png");
        var roundTopSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/App/UI/Sprites/Shapes/RoundedRect-24.png");

        GeneratePrefabs(fonts, btnSprite, uiSolidRect);
        CreateBootstrapScene(fonts, btnSprite, uiSolidRect);
        CreateMainARScene(fonts, btnSprite, uiSolidRect, roundTopSprite);
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

        QualitySettings.antiAliasing = 4;

        var urpAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(
            "Assets/Settings/URP-Performant.asset");
        if (urpAsset != null)
        {
            var so = new SerializedObject(urpAsset);
            var msaaProp = so.FindProperty("m_MSAA");
            if (msaaProp != null) msaaProp.intValue = 4;
            so.ApplyModifiedProperties();
            so.Dispose();
        }

        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.allowDebugging = false;
    }

    private static void GeneratePrefabs(FontSet fonts, Sprite btnSprite, Sprite uiSolidRect)
    {
        System.IO.Directory.CreateDirectory("Assets/App/Prefabs");

        var roundRect08 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-08.png");
        var roundRect12 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
        var roundRect16 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-16.png");
        var roundRect24 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-24.png");

        // 1. StepItem prefab (numbered card)
        var stepGo = new GameObject("StepItem", typeof(RectTransform));
        var stepImg = stepGo.AddComponent<Image>();
        stepImg.sprite = roundRect12;
        stepImg.type = Image.Type.Sliced;
        stepImg.color = WarmWhite;
        var stepOutline = stepGo.AddComponent<Outline>();
        stepOutline.effectColor = SoftSand;
        stepOutline.effectDistance = new Vector2(1f, 1f);
        var stepRT = stepGo.GetComponent<RectTransform>();
        stepRT.sizeDelta = new Vector2(320f, 60f); // Default, but dynamic

        var stepHlg = stepGo.AddComponent<HorizontalLayoutGroup>();
        stepHlg.padding = new RectOffset(16, 16, 12, 12);
        stepHlg.spacing = 12f;
        stepHlg.childAlignment = TextAnchor.MiddleLeft;
        stepHlg.childControlWidth = true;
        stepHlg.childControlHeight = true;
        stepHlg.childForceExpandWidth = false;
        stepHlg.childForceExpandHeight = false;

        var stepCsf = stepGo.AddComponent<ContentSizeFitter>();
        stepCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        stepCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Badge
        var badgeGo = CreateUIObject("Badge", stepGo);
        var badgeImg = badgeGo.AddComponent<Image>();
        badgeImg.sprite = roundRect08;
        badgeImg.type = Image.Type.Sliced;
        badgeImg.color = ForestGreen;
        var badgeLE = badgeGo.AddComponent<LayoutElement>();
        badgeLE.minWidth = 28f;
        badgeLE.minHeight = 28f;
        badgeLE.preferredWidth = 28f;
        badgeLE.preferredHeight = 28f;

        var badgeTextGo = CreateUIObject("Text", badgeGo);
        var badgeText = badgeTextGo.AddComponent<TextMeshProUGUI>();
        badgeText.text = "1";
        badgeText.fontSize = 12f;
        badgeText.fontStyle = FontStyles.Bold;
        badgeText.color = WarmWhite;
        badgeText.alignment = TextAlignmentOptions.Center;
        if (fonts != null) badgeText.font = fonts.Heading;
        StretchRect(badgeTextGo.GetComponent<RectTransform>());

        // Text
        var textGo = CreateUIObject("Text", stepGo);
        var textTmp = textGo.AddComponent<TextMeshProUGUI>();
        textTmp.textWrappingMode = TextWrappingModes.Normal;
        textTmp.overflowMode = TextOverflowModes.Overflow;
        textTmp.fontSize = 13f;
        textTmp.color = SecondaryText;
        textTmp.alignment = TextAlignmentOptions.Left;
        if (fonts != null) textTmp.font = fonts.Body;
        var textLE = textGo.AddComponent<LayoutElement>();
        textLE.flexibleWidth = 1f;

        PrefabUtility.SaveAsPrefabAsset(stepGo, "Assets/App/Prefabs/StepItem.prefab");
        Object.DestroyImmediate(stepGo);

        // 2. BulletItem prefab (mistakes card with x-circle SVG icon)
        var bulletGo = new GameObject("BulletItem", typeof(RectTransform));
        var bulletImg = bulletGo.AddComponent<Image>();
        bulletImg.sprite = roundRect12;
        bulletImg.type = Image.Type.Sliced;
        bulletImg.color = WarmWhite;
        var bulletOutline = bulletGo.AddComponent<Outline>();
        bulletOutline.effectColor = SoftSand;
        bulletOutline.effectDistance = new Vector2(1f, 1f);
        var bulletRT = bulletGo.GetComponent<RectTransform>();
        bulletRT.sizeDelta = new Vector2(320f, 48f); // Default, but dynamic

        var bulletHlg = bulletGo.AddComponent<HorizontalLayoutGroup>();
        bulletHlg.padding = new RectOffset(16, 16, 12, 12);
        bulletHlg.spacing = 12f;
        bulletHlg.childAlignment = TextAnchor.MiddleLeft;
        bulletHlg.childControlWidth = true;
        bulletHlg.childControlHeight = true;
        bulletHlg.childForceExpandWidth = false;
        bulletHlg.childForceExpandHeight = false;

        var bulletCsf = bulletGo.AddComponent<ContentSizeFitter>();
        bulletCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        bulletCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Icon container badge
        var bulletIconGo = CreateUIObject("Badge", bulletGo);
        var bulletIconImg = bulletIconGo.AddComponent<Image>();
        bulletIconImg.sprite = roundRect08;
        bulletIconImg.type = Image.Type.Sliced;
        bulletIconImg.color = SoftSand;
        var bulletIconLE = bulletIconGo.AddComponent<LayoutElement>();
        bulletIconLE.minWidth = 24f;
        bulletIconLE.minHeight = 24f;
        bulletIconLE.preferredWidth = 24f;
        bulletIconLE.preferredHeight = 24f;

        var bulletIconSvgGo = CreateUIObject("SvgIcon", bulletIconGo);
        var bulletIconSvg = bulletIconSvgGo.AddComponent<Image>();
        bulletIconSvg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Icons/Lucide/x-circle.svg");
        bulletIconSvg.preserveAspect = true;
        bulletIconSvg.raycastTarget = false;
        bulletIconSvg.color = Error;
        var bulletIconSvgRT = bulletIconSvgGo.GetComponent<RectTransform>();
        bulletIconSvgRT.localScale = Vector3.one;
        SetCenterPosition(bulletIconSvgRT, 0f, 0f, 16f, 16f);

        // Text
        var bulletTextGo = CreateUIObject("Text", bulletGo);
        var bulletText = bulletTextGo.AddComponent<TextMeshProUGUI>();
        bulletText.textWrappingMode = TextWrappingModes.Normal;
        bulletText.overflowMode = TextOverflowModes.Overflow;
        bulletText.fontSize = 12f;
        bulletText.color = SecondaryText;
        bulletText.alignment = TextAlignmentOptions.Left;
        if (fonts != null) bulletText.font = fonts.Body;
        var bulletTextLE = bulletTextGo.AddComponent<LayoutElement>();
        bulletTextLE.flexibleWidth = 1f;

        PrefabUtility.SaveAsPrefabAsset(bulletGo, "Assets/App/Prefabs/BulletItem.prefab");
        Object.DestroyImmediate(bulletGo);

        // 3. RelatedCard prefab
        var cardGo = new GameObject("RelatedCard", typeof(RectTransform));
        var cardImg = cardGo.AddComponent<Image>();
        cardImg.sprite = roundRect16;
        cardImg.type = Image.Type.Sliced;
        cardImg.color = WarmWhite;
        var cardOutline = cardGo.AddComponent<Outline>();
        cardOutline.effectColor = SoftSand;
        cardOutline.effectDistance = new Vector2(1f, 1f);
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
        if (fonts != null) titleText.font = fonts.Display;
        SetCenterPosition(titleGo.GetComponent<RectTransform>(), 0f, -16.7f, 66.7f, 13.3f);

        PrefabUtility.SaveAsPrefabAsset(cardGo, "Assets/App/Prefabs/RelatedCard.prefab");
        Object.DestroyImmediate(cardGo);

        // 4. TimelineMarker prefab (small circle node)
        var markerGo = new GameObject("TimelineMarker", typeof(RectTransform));
        var markerImg = markerGo.AddComponent<Image>();
        markerImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/Circle-24.png");
        markerImg.type = Image.Type.Simple;
        markerImg.preserveAspect = true;
        var markerRT = markerGo.GetComponent<RectTransform>();
        markerRT.sizeDelta = new Vector2(12f, 12f);
        PrefabUtility.SaveAsPrefabAsset(markerGo, "Assets/App/Prefabs/TimelineMarker.prefab");
        Object.DestroyImmediate(markerGo);

        // 5. MuscleItem prefab (clean grid card badge)
        var muscleGo = new GameObject("MuscleItem", typeof(RectTransform));
        var muscleImg = muscleGo.AddComponent<Image>();
        muscleImg.sprite = roundRect12;
        muscleImg.type = Image.Type.Sliced;
        muscleImg.color = WarmWhite;
        var muscleOutline = muscleGo.AddComponent<Outline>();
        muscleOutline.effectColor = SoftSand;
        muscleOutline.effectDistance = new Vector2(1f, 1f);
        var muscleRT = muscleGo.GetComponent<RectTransform>();
        muscleRT.sizeDelta = new Vector2(146f, 32f);

        var muscleTextGo = CreateUIObject("Text", muscleGo);
        var muscleText = muscleTextGo.AddComponent<TextMeshProUGUI>();
        muscleText.text = "Muscle";
        muscleText.fontSize = 11f;
        muscleText.fontStyle = FontStyles.Bold;
        muscleText.color = DeepForest;
        muscleText.alignment = TextAlignmentOptions.Center;
        if (fonts != null) muscleText.font = fonts.Heading;
        StretchRect(muscleTextGo.GetComponent<RectTransform>());

        PrefabUtility.SaveAsPrefabAsset(muscleGo, "Assets/App/Prefabs/MuscleItem.prefab");
        Object.DestroyImmediate(muscleGo);
    }

    // ─────────────────────────────────────────────────────────────────
    // BOOTSTRAP SCENE
    // ─────────────────────────────────────────────────────────────────
    private static void CreateBootstrapScene(FontSet fonts, Sprite btnSprite, Sprite uiSolidRect)
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

        var canvasStruct = CreateResponsiveCanvas("Canvas");
        canvasStruct.CanvasGo.transform.SetParent(null);

        var eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemGo.AddComponent<InputSystemUIInputModule>();

        // ═══════════════════════════════════════════════════════════
        // G01 — OPENING FULL-SCREEN COVER (Stretched on FullScreenBg)
        // ═══════════════════════════════════════════════════════════
        var introGo = CreateUIObject("IntroPanel", canvasStruct.FullScreenBgGo);
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

        // Top identity Left
        var topIdGo = CreateUIObject("TopIdentity", introGo);
        SetAnchorTopLeft(topIdGo.GetComponent<RectTransform>(), 20f, -20f, 200f, 40f);

        var metaTextGo = CreateUIObject("MetadataText", topIdGo);
        var metaText = metaTextGo.AddComponent<TextMeshProUGUI>();
        metaText.textWrappingMode = TextWrappingModes.Normal;
        metaText.overflowMode = TextOverflowModes.Overflow;
        metaText.text = "<b>Media Pembelajaran</b>\n<color=#EADDCF>Skripsi Pendidikan SD</color>";
        metaText.fontSize = 11f;
        metaText.color = WarmWhite;
        metaText.alignment = TextAlignmentOptions.Left;
        if (fonts != null) metaText.font = fonts.Display;
        StretchRect(metaTextGo.GetComponent<RectTransform>());

        // Top identity Right (UNP Branding Placeholder)
        var topIdRightGo = CreateUIObject("TopIdentityRight", introGo);
        SetAnchorTopRight(topIdRightGo.GetComponent<RectTransform>(), -20f, -20f, 150f, 40f);

        var unpTextGo = CreateUIObject("UNPText", topIdRightGo);
        var unpText = unpTextGo.AddComponent<TextMeshProUGUI>();
        unpText.textWrappingMode = TextWrappingModes.Normal;
        unpText.overflowMode = TextOverflowModes.Overflow;
        unpText.text = "<align=right><b>UNP</b>\n<size=8><color=#EADDCF>Universitas Negeri Padang</color></size></align>";
        unpText.fontSize = 11f;
        unpText.color = WarmWhite;
        unpText.alignment = TextAlignmentOptions.Right;
        if (fonts != null) unpText.font = fonts.Display;
        StretchRect(unpTextGo.GetComponent<RectTransform>());

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
        if (fonts != null) titleText.font = fonts.Display;
        SetCenterPosition(titleGo.GetComponent<RectTransform>(), 0f, 45f, 266.7f, 36f);

        var subtitleGo = CreateUIObject("SubtitleText", brandGroupGo);
        var subtitleText = subtitleGo.AddComponent<TextMeshProUGUI>();
        subtitleText.textWrappingMode = TextWrappingModes.Normal;
        subtitleText.text = "Belajar Gerak Jadi Seru";
        subtitleText.fontSize = 12f;
        subtitleText.color = SoftSand;
        subtitleText.alignment = TextAlignmentOptions.Center;
        if (fonts != null) subtitleText.font = fonts.Medium;
        SetCenterPosition(subtitleGo.GetComponent<RectTransform>(), 0f, 15f, 266.7f, 16.7f);

        // StraightProgressBar — UISolidRectangle anti-taper
        var progressTrackGo = CreateUIObject("ProgressTrack", brandGroupGo);
        var trackImg = progressTrackGo.AddComponent<Image>();
        trackImg.sprite = uiSolidRect;
        trackImg.type = Image.Type.Simple;
        trackImg.preserveAspect = false;
        trackImg.color = SoftSand;
        trackImg.GetComponent<RectTransform>().localScale = Vector3.one;
        SetCenterPosition(progressTrackGo.GetComponent<RectTransform>(), 0f, -20f, 180f, 4f); // height 4 units, ujung datar

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
        if (fonts != null) loadingLabel.font = fonts.Medium;
        SetCenterPosition(loadingLabelGo.GetComponent<RectTransform>(), 0f, -50f, 200f, 14f);

        var serialIntro = new SerializedObject(introController);
        serialIntro.FindProperty("introCanvasGroup").objectReferenceValue = introCanvasGroup;
        serialIntro.FindProperty("loadingFillImage").objectReferenceValue = fillImg;
        serialIntro.ApplyModifiedProperties();

        // ═══════════════════════════════════════════════════════════
        // G02 — ONBOARDING (Child of SafeArea)
        // ═══════════════════════════════════════════════════════════
        var onboardGo = CreateUIObject("OnboardingPanel", canvasStruct.SafeAreaGo);
        var onboardImg = onboardGo.AddComponent<Image>();
        onboardImg.color = WarmCream;
        StretchRect(onboardGo.GetComponent<RectTransform>());
        onboardGo.SetActive(false);

        // (HeaderBar removed per request)

        var safetyHeaderGo = CreateUIObject("SafetyHeader", onboardGo);
        SetAnchorTop(safetyHeaderGo.GetComponent<RectTransform>(), 0f, -60f, 320f, 160f);

        // G02 Safety Shield Icon above title
        var shieldIconGo = CreateUIObject("ShieldIcon", safetyHeaderGo);
        var shieldIconImg = shieldIconGo.AddComponent<Image>();
        shieldIconImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Icons/Lucide/shield-check.svg");
        shieldIconImg.color = ForestGreen;
        shieldIconImg.preserveAspect = true;
        SetCenterPosition(shieldIconGo.GetComponent<RectTransform>(), 0f, 85f, 40f, 40f);

        var obTitleGo = CreateUIObject("OnboardingTitle", safetyHeaderGo);
        var obTitle = obTitleGo.AddComponent<TextMeshProUGUI>();
        obTitle.textWrappingMode = TextWrappingModes.Normal;
        obTitle.text = "Sebelum Mulai";
        obTitle.fontSize = 24f;
        obTitle.fontStyle = FontStyles.Bold;
        obTitle.color = DeepForest;
        obTitle.alignment = TextAlignmentOptions.Center;
        if (fonts != null) obTitle.font = fonts.Heading;
        SetCenterPosition(obTitleGo.GetComponent<RectTransform>(), 0f, 30f, 266.7f, 30f);

        var obSubtitleGo = CreateUIObject("OnboardingSubtitle", safetyHeaderGo);
        var obSubtitle = obSubtitleGo.AddComponent<TextMeshProUGUI>();
        obSubtitle.textWrappingMode = TextWrappingModes.Normal;
        obSubtitle.text = "Ayo bergerak dengan aman dan nyaman.";
        obSubtitle.fontSize = 12f;
        obSubtitle.color = SecondaryText;
        obSubtitle.alignment = TextAlignmentOptions.Center;
        if (fonts != null) obSubtitle.font = fonts.Medium;
        SetCenterPosition(obSubtitleGo.GetComponent<RectTransform>(), 0f, -5f, 266.7f, 16.7f);

        var obListGo = CreateUIObject("InstructionList", onboardGo);
        SetCenterPosition(obListGo.GetComponent<RectTransform>(), 0f, -10f, 320f, 190f);

        // Card 1
        CreateOnboardingCard(obListGo, "Card1", "1", "Gunakan di tempat yang cukup luas.",
            0f, 65f, btnSprite, fonts);
        // Card 2
        CreateOnboardingCard(obListGo, "Card2", "2", "Minta guru atau orang tua mendampingi.",
            0f, 0f, btnSprite, fonts);
        // Card 3
        CreateOnboardingCard(obListGo, "Card3", "3", "Izinkan kamera untuk melihat gerakan.",
            0f, -65f, btnSprite, fonts);

        var btnGroupGo = CreateUIObject("ButtonGroup", onboardGo);
        SetAnchorBottom(btnGroupGo.GetComponent<RectTransform>(), 0f, 60f, 320f, 100f);

        // MULAI button — NO camera icon, text centered
        var startBtnGo = CreateUIObject("MulaiButton", btnGroupGo);
        var startBtnImg = startBtnGo.AddComponent<Image>();
        startBtnImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
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
        if (fonts != null) btnText.font = fonts.Heading;
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
        if (fonts != null) nonARLinkText.font = fonts.Heading;
        var nonARBtn = nonARLinkGo.AddComponent<Button>();
        SetCenterPosition(nonARLinkGo.GetComponent<RectTransform>(), -73.3f, 0f, 113.3f, 16.7f);

        var camErrorLinkGo = CreateUIObject("CameraErrorLink", fallbackLinksGo);
        var camErrorLinkText = camErrorLinkGo.AddComponent<TextMeshProUGUI>();
        camErrorLinkText.textWrappingMode = TextWrappingModes.Normal;
        camErrorLinkText.text = "<u>Simulasi Kendala Kamera</u>";
        camErrorLinkText.fontSize = 10f;
        camErrorLinkText.color = DeepForest;
        camErrorLinkText.alignment = TextAlignmentOptions.Center;
        if (fonts != null) camErrorLinkText.font = fonts.Heading;
        var camErrorBtn = camErrorLinkGo.AddComponent<Button>();
        SetCenterPosition(camErrorLinkGo.GetComponent<RectTransform>(), 73.3f, 0f, 113.3f, 16.7f);

        var serialOnboard = new SerializedObject(managersGo.GetComponent<OnboardingController>());
        serialOnboard.FindProperty("onboardingPanel").objectReferenceValue = onboardGo;
        serialOnboard.ApplyModifiedProperties();

        // ═══════════════════════════════════════════════════════════
        // UNSUPPORTED PANEL (Parent for G08 & G09, child of SafeArea)
        // ═══════════════════════════════════════════════════════════
        var unsupGo = CreateUIObject("UnsupportedPanel", canvasStruct.SafeAreaGo);
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
        if (fonts != null) g08BrandText.font = fonts.Display;
        SetCenterPosition(g08BrandGo.GetComponent<RectTransform>(), -100f, 0f, 120f, 28f);

        var g08BadgeGo = CreateUIObject("ModeBadge", g08HeaderBarGo);
        var g08BadgeImg = g08BadgeGo.AddComponent<Image>();
        g08BadgeImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-08.png");
        g08BadgeImg.type = Image.Type.Sliced;
        g08BadgeImg.color = ForestGreen;
        SetCenterPosition(g08BadgeGo.GetComponent<RectTransform>(), 85f, 0f, 110f, 24f);

        var g08BadgeTextGo = CreateUIObject("Text", g08BadgeGo);
        var g08BadgeText = g08BadgeTextGo.AddComponent<TextMeshProUGUI>();
        g08BadgeText.textWrappingMode = TextWrappingModes.Normal;
        g08BadgeText.text = "Mode Tanpa Kamera";
        g08BadgeText.fontSize = 10f;
        g08BadgeText.fontStyle = FontStyles.Bold;
        g08BadgeText.color = WarmWhite;
        g08BadgeText.alignment = TextAlignmentOptions.Center;
        if (fonts != null) g08BadgeText.font = fonts.Heading;
        StretchRect(g08BadgeTextGo.GetComponent<RectTransform>());

        // G08 Collapsible Warning
        var collapsibleWarnGo = CreateCollapsibleWarning(
            nonARModePanelGo, btnSprite, fonts, "Assets/App/UI/Icons/Lucide/info.svg");
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
        catTitle.text = "Belajar Gerakan";
        catTitle.fontSize = 14f;
        catTitle.fontStyle = FontStyles.Bold;
        catTitle.color = DeepForest;
        catTitle.alignment = TextAlignmentOptions.Left;
        if (fonts != null) catTitle.font = fonts.Heading;
        SetCenterPosition(catTitleGo.GetComponent<RectTransform>(), 0f, 145f, 300f, 16f);

        // Squat card
        var (squatBukaBtn, _, _) = CreateMovementCard(
            catalogCatalogGo, "CardSquat", "SQ", "Gerakan Squat",
            "Melatih otot paha dan sendi lutut",
            0f, 95f, btnSprite, fonts);

        // Dynamic Stretching card
        var (dynamicStretchBukaBtn, _, _) = CreateMovementCard(
            catalogCatalogGo, "CardDynamicStretch", "DS", "Dynamic Stretching",
            "Peregangan aktif sebelum bergerak",
            0f, 30f, btnSprite, fonts);

        // Ladder Drill card
        var (ladderDrillBukaBtn, _, _) = CreateMovementCard(
            catalogCatalogGo, "CardLadderDrill", "LD", "Ladder Drill",
            "Melatih kelincahan dan langkah kaki",
            0f, -35f, btnSprite, fonts);

        // Back button
        var catalogBackGo = CreateUIObject("CatalogBackButton", nonARModePanelGo);
        var catalogBackImg = catalogBackGo.AddComponent<Image>();
        catalogBackImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
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
        if (fonts != null) catalogBackText.font = fonts.Heading;
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
        camCardImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
        camCardImg.type = Image.Type.Sliced;
        camCardImg.color = WarmWhite;
        var camCardOutline = camCardGo.AddComponent<Outline>();
        camCardOutline.effectColor = SoftSand;
        camCardOutline.effectDistance = new Vector2(1f, 1f);
        SetCenterPosition(camCardGo.GetComponent<RectTransform>(), 0f, 20f, 280f, 280f);

        // Camera-off icon (x-circle SVG)
        var camOffIconGo = CreateUIObject("CamOffIcon", camCardGo);
        var camOffIconImg = camOffIconGo.AddComponent<Image>();
        camOffIconImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
        camOffIconImg.type = Image.Type.Sliced;
        camOffIconImg.color = SoftSand;
        SetCenterPosition(camOffIconGo.GetComponent<RectTransform>(), 0f, 90f, 44f, 44f);

        var camOffIconSvgGo = CreateUIObject("SvgIcon", camOffIconGo);
        var camOffIconSvg = camOffIconSvgGo.AddComponent<Image>();
        camOffIconSvg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Icons/Lucide/x-circle.svg");
        camOffIconSvg.preserveAspect = true;
        camOffIconSvg.raycastTarget = false;
        camOffIconSvg.color = Error;
        var camOffIconSvgRT = camOffIconSvgGo.GetComponent<RectTransform>();
        camOffIconSvgRT.localScale = Vector3.one;
        SetCenterPosition(camOffIconSvgRT, 0f, 0f, 24f, 24f);

        var camErrorTitleGo = CreateUIObject("Title", camCardGo);
        var camErrorTitle = camErrorTitleGo.AddComponent<TextMeshProUGUI>();
        camErrorTitle.textWrappingMode = TextWrappingModes.Normal;
        camErrorTitle.text = "Kamera belum dapat dibuka";
        camErrorTitle.fontSize = 18f;
        camErrorTitle.fontStyle = FontStyles.Bold;
        camErrorTitle.color = DeepForest;
        camErrorTitle.alignment = TextAlignmentOptions.Center;
        if (fonts != null) camErrorTitle.font = fonts.Heading;
        SetCenterPosition(camErrorTitleGo.GetComponent<RectTransform>(), 0f, 45f, 240f, 28f);

        var camErrorDescGo = CreateUIObject("Desc", camCardGo);
        var camErrorDesc = camErrorDescGo.AddComponent<TextMeshProUGUI>();
        camErrorDesc.textWrappingMode = TextWrappingModes.Normal;
        camErrorDesc.text = "Periksa izin kamera, lalu coba lagi.";
        camErrorDesc.fontSize = 13f;
        camErrorDesc.color = SecondaryText;
        camErrorDesc.alignment = TextAlignmentOptions.Center;
        if (fonts != null) camErrorDesc.font = fonts.Medium;
        SetCenterPosition(camErrorDescGo.GetComponent<RectTransform>(), 0f, 10f, 240f, 40f);

        // Belajar Tanpa Kamera button (replacing settingBtn detail)
        var settingsGo = CreateUIObject("SettingsButton", camCardGo);
        var settingsImg = settingsGo.AddComponent<Image>();
        settingsImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
        settingsImg.type = Image.Type.Sliced;
        settingsImg.color = ForestGreen;
        var settingsBtn = settingsGo.AddComponent<Button>();
        SetCenterPosition(settingsGo.GetComponent<RectTransform>(), 0f, -35f, 240f, 44f);

        var settingsTextGo = CreateUIObject("Text", settingsGo);
        var settingsText = settingsTextGo.AddComponent<TextMeshProUGUI>();
        settingsText.textWrappingMode = TextWrappingModes.Normal;
        settingsText.text = "Belajar Tanpa Kamera";
        settingsText.fontSize = 14f;
        settingsText.fontStyle = FontStyles.Bold;
        settingsText.color = WarmWhite;
        settingsText.alignment = TextAlignmentOptions.Center;
        if (fonts != null) settingsText.font = fonts.Heading;
        StretchRect(settingsTextGo.GetComponent<RectTransform>());

        // Coba Lagi button
        var retryGo = CreateUIObject("RetryButton", camCardGo);
        var retryBtnImg = retryGo.AddComponent<Image>();
        retryBtnImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
        retryBtnImg.type = Image.Type.Sliced;
        retryBtnImg.color = WarmCream;
        var retryBtnOutline = retryGo.AddComponent<Outline>();
        retryBtnOutline.effectColor = SoftSand;
        retryBtnOutline.effectDistance = new Vector2(1f, 1f);
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
        if (fonts != null) retryText.font = fonts.Heading;
        StretchRect(retryTextGo.GetComponent<RectTransform>());

        // Helper text
        var helperGo = CreateUIObject("HelperText", cameraErrorPanelGo);
        var helperText = helperGo.AddComponent<TextMeshProUGUI>();
        helperText.textWrappingMode = TextWrappingModes.Normal;
        helperText.text = "Minta bantuan guru atau orang tua jika diperlukan.";
        helperText.fontSize = 11f;
        helperText.color = SoftSand;
        helperText.alignment = TextAlignmentOptions.Center;
        if (fonts != null) helperText.font = fonts.Medium;
        SetAnchorBottom(helperGo.GetComponent<RectTransform>(), 0f, 20f, 280f, 16f);

        // BootstrapUIController wiring
        var bootstrapUI = managersGo.AddComponent<BootstrapUIController>();
        var serialBUI = new SerializedObject(bootstrapUI);
        serialBUI.FindProperty("introPanel").objectReferenceValue = introGo;
        serialBUI.FindProperty("onboardingPanel").objectReferenceValue = onboardGo;
        serialBUI.FindProperty("unsupportedPanel").objectReferenceValue = unsupGo;
        serialBUI.FindProperty("nonARModePanel").objectReferenceValue = nonARModePanelGo;
        serialBUI.FindProperty("cameraErrorPanel").objectReferenceValue = cameraErrorPanelGo;
        serialBUI.FindProperty("errorTitleText").objectReferenceValue = camErrorTitle;
        serialBUI.FindProperty("errorDescText").objectReferenceValue = camErrorDesc;
        serialBUI.FindProperty("primaryBtnText").objectReferenceValue = settingsText;
        serialBUI.FindProperty("secondaryBtnText").objectReferenceValue = retryText;
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
    private static void CreateMainARScene(FontSet fonts, Sprite btnSprite, Sprite uiSolidRect, Sprite roundTopSprite)
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
        var canvasStruct = CreateResponsiveCanvas("UI Canvas");
        canvasStruct.CanvasGo.transform.SetParent(null);

        var eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemGo.AddComponent<InputSystemUIInputModule>();

        var arUI = canvasStruct.CanvasGo.AddComponent<ARUIController>();

        var playIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Icons/Lucide/play.svg");
        var pauseIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Icons/Lucide/pause.svg");
        var materiIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Icons/Lucide/book-open.svg");
        var closeIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Icons/Lucide/x.svg");
        var scanLineIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Icons/Lucide/scan-line.svg");
        var infoIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Icons/Lucide/info.svg");
        var chevronRightIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Icons/Lucide/chevron-right.svg");
        var shieldCheckIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Icons/Lucide/shield-check.svg");

        // ═══════════════════════════════════════════════════════════
        // G01 LOOKALIKE — CAMERA READY COVER (Stretched on FullScreenBg)
        // ═══════════════════════════════════════════════════════════
        var readyCoverGo = CreateUIObject("CameraReadyCover", canvasStruct.FullScreenBgGo);
        var readyCoverImg = readyCoverGo.AddComponent<Image>();
        readyCoverImg.color = DeepForest;
        StretchRect(readyCoverGo.GetComponent<RectTransform>());
        var readyCoverGroup = readyCoverGo.AddComponent<CanvasGroup>();
        readyCoverGroup.alpha = 1f;

        var readyBrandGroup = CreateUIObject("BrandGroup", readyCoverGo);
        SetAnchorBottom(readyBrandGroup.GetComponent<RectTransform>(), 0f, 100f, 320f, 130f);

        var readyTitleGo = CreateUIObject("TitleText", readyBrandGroup);
        var readyTitleText = readyTitleGo.AddComponent<TextMeshProUGUI>();
        readyTitleText.text = "GerakAR";
        readyTitleText.fontSize = 34f;
        readyTitleText.fontStyle = FontStyles.Bold;
        readyTitleText.color = WarmWhite;
        readyTitleText.alignment = TextAlignmentOptions.Center;
        if (fonts != null) readyTitleText.font = fonts.Display;
        SetCenterPosition(readyTitleGo.GetComponent<RectTransform>(), 0f, 45f, 266.7f, 36f);

        var readyProgressGo = CreateUIObject("ProgressTrack", readyBrandGroup);
        var readyProgressImg = readyProgressGo.AddComponent<Image>();
        readyProgressImg.sprite = uiSolidRect;
        readyProgressImg.type = Image.Type.Simple;
        readyProgressImg.preserveAspect = false;
        readyProgressImg.color = SoftSand;
        SetCenterPosition(readyProgressGo.GetComponent<RectTransform>(), 0f, -20f, 180f, 4f);

        var readyProgressFill = CreateUIObject("ProgressFill", readyProgressGo);
        var readyFillImg = readyProgressFill.AddComponent<Image>();
        readyFillImg.sprite = uiSolidRect;
        readyFillImg.type = Image.Type.Simple;
        readyFillImg.preserveAspect = false;
        readyFillImg.color = WarmWhite;
        var rFillRT = readyProgressFill.GetComponent<RectTransform>();
        rFillRT.anchorMin = new Vector2(0f, 0f);
        rFillRT.anchorMax = new Vector2(0f, 1f);
        rFillRT.pivot = new Vector2(0f, 0.5f);
        rFillRT.anchoredPosition = Vector2.zero;
        rFillRT.sizeDelta = new Vector2(0f, 0f);

        var readyLabelGo = CreateUIObject("LoadingLabel", readyBrandGroup);
        var readyLabel = readyLabelGo.AddComponent<TextMeshProUGUI>();
        readyLabel.text = "Menyiapkan Kamera";
        readyLabel.fontSize = 10f;
        readyLabel.color = SoftSand;
        readyLabel.alignment = TextAlignmentOptions.Center;
        if (fonts != null) readyLabel.font = fonts.Medium;
        SetCenterPosition(readyLabelGo.GetComponent<RectTransform>(), 0f, -50f, 200f, 14f);

        // ═══════════════════════════════════════════════════════════
        // G03 — SCANNER (Child of SafeArea)
        // ═══════════════════════════════════════════════════════════
        var scanGo = CreateUIObject("ScanOverlay", canvasStruct.SafeAreaGo);
        StretchRect(scanGo.GetComponent<RectTransform>());

        // G03 Header
        var scanTitleGo = CreateUIObject("HeaderTitle", scanGo); // parented under scanGo!
        var scanTitle = scanTitleGo.AddComponent<TextMeshProUGUI>();
        scanTitle.textWrappingMode = TextWrappingModes.Normal;
        scanTitle.text = "GerakAR";
        scanTitle.fontSize = 20f;
        scanTitle.fontStyle = FontStyles.Bold;
        scanTitle.color = WarmWhite;
        scanTitle.alignment = TextAlignmentOptions.Center;
        if (fonts != null) scanTitle.font = fonts.Display;
        SetAnchorTop(scanTitleGo.GetComponent<RectTransform>(), 0f, -48f, 200f, 28f);

        var scanSubGo = CreateUIObject("HeaderSub", scanGo);
        var scanSub = scanSubGo.AddComponent<TextMeshProUGUI>();
        scanSub.textWrappingMode = TextWrappingModes.Normal;
        scanSub.text = "Belajar Gerak Jadi Seru";
        scanSub.fontSize = 10f;
        scanSub.color = SoftSand;
        scanSub.alignment = TextAlignmentOptions.Center;
        if (fonts != null) scanSub.font = fonts.Medium;
        SetAnchorTop(scanSubGo.GetComponent<RectTransform>(), 0f, -76f, 200f, 16f);

        // G03 Central Scan Guide Frame — Solid Warm White corners
        var scanFrameGo = CreateUIObject("ScanFrame", scanGo);
        SetCenterPosition(scanFrameGo.GetComponent<RectTransform>(), 0f, 60f, 232f, 232f); // shift up slightly

        // Helper for solid L-corner brackets (3 width, 24 length, solid Warm White)
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

        CreateSolidCorner("TopLeft", -116f, 116f, 0f, 1f);
        CreateSolidCorner("TopRight", 116f, 116f, 1f, 1f);
        CreateSolidCorner("BottomLeft", -116f, -116f, 0f, 0f);
        CreateSolidCorner("BottomRight", 116f, -116f, 1f, 0f);

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

        // Scan target pill — White semi-transparent background with deep forest text, rounder corners
        var scanPillGo = CreateUIObject("ScanTargetPill", scanGo);
        var scanPillImg = scanPillGo.AddComponent<Image>();
        scanPillImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
        scanPillImg.type = Image.Type.Sliced;
        scanPillImg.color = new Color(1f, 1f, 1f, 0.85f);
        SetCenterPosition(scanPillGo.GetComponent<RectTransform>(), 0f, -100f, 180f, 30f);

        var scanPillTextGo = CreateUIObject("Text", scanPillGo);
        var scanPillText = scanPillTextGo.AddComponent<TextMeshProUGUI>();
        scanPillText.textWrappingMode = TextWrappingModes.Normal;
        scanPillText.text = "PINDAI TARGET GAMBAR";
        scanPillText.fontSize = 11f;
        scanPillText.fontStyle = FontStyles.Bold;
        scanPillText.color = DeepForest;
        scanPillText.alignment = TextAlignmentOptions.Center;
        if (fonts != null) scanPillText.font = fonts.Heading;
        StretchRect(scanPillTextGo.GetComponent<RectTransform>());

        // Instruction card — White semi-transparent background card positioned at the bottom
        var instructionCardGo = CreateUIObject("InstructionCard", scanGo); // parented under scanGo!
        var instCardImg = instructionCardGo.AddComponent<Image>();
        instCardImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
        instCardImg.type = Image.Type.Sliced;
        instCardImg.color = new Color(1f, 1f, 1f, 0.85f);
        SetAnchorBottom(instructionCardGo.GetComponent<RectTransform>(), 0f, 80f, 280f, 72f);

        var hintGo = CreateUIObject("HintText", instructionCardGo);
        var hintText = hintGo.AddComponent<TextMeshProUGUI>();
        hintText.textWrappingMode = TextWrappingModes.Normal;
        hintText.text = "Arahkan kamera ke gambar gerakan";
        hintText.fontSize = 13f;
        hintText.fontStyle = FontStyles.Bold;
        hintText.color = DeepForest;
        hintText.alignment = TextAlignmentOptions.Center;
        if (fonts != null) hintText.font = fonts.Body;
        SetCenterPosition(hintGo.GetComponent<RectTransform>(), 0f, 14f, 260f, 20f);

        var instSubtitleGo = CreateUIObject("InstructionSubtitle", instructionCardGo); // parented under instructionCardGo!
        var instSubtitle = instSubtitleGo.AddComponent<TextMeshProUGUI>();
        instSubtitle.textWrappingMode = TextWrappingModes.Normal;
        instSubtitle.text = "Pastikan seluruh gambar terlihat jelas";
        instSubtitle.fontSize = 10f;
        instSubtitle.color = ForestGreen;
        instSubtitle.alignment = TextAlignmentOptions.Center;
        if (fonts != null) instSubtitle.font = fonts.Medium;
        SetCenterPosition(instSubtitleGo.GetComponent<RectTransform>(), 0f, -14f, 260f, 16f);

        // ═══════════════════════════════════════════════════════════
        // G04 — DETECTION TOAST (Child of CenterContent)
        // ═══════════════════════════════════════════════════════════
        var toastGo = CreateUIObject("DetectionToast", canvasStruct.CenterContentGo);
        var toastImg = toastGo.AddComponent<Image>();
        toastImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
        toastImg.type = Image.Type.Sliced;
        toastImg.color = WarmWhite;
        var toastOutline = toastGo.AddComponent<Outline>();
        toastOutline.effectColor = SoftSand;
        toastOutline.effectDistance = new Vector2(1f, 1f);
        SetCenterPosition(toastGo.GetComponent<RectTransform>(), 0f, 0f, 200f, 130f);

        var toastCircleGo = CreateUIObject("SuccessCircle", toastGo);
        var toastCircleImg = toastCircleGo.AddComponent<Image>();
        toastCircleImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-24.png");
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
        if (fonts != null) toastCheck.font = fonts.Medium;
        StretchRect(toastCheckGo.GetComponent<RectTransform>());

        var toastTextGo = CreateUIObject("TitleText", toastGo);
        var toastText = toastTextGo.AddComponent<TextMeshProUGUI>();
        toastText.textWrappingMode = TextWrappingModes.Normal;
        toastText.text = "Gerakan Ditemukan!";
        toastText.fontSize = 14f;
        toastText.fontStyle = FontStyles.Bold;
        toastText.color = DeepForest;
        toastText.alignment = TextAlignmentOptions.Center;
        if (fonts != null) toastText.font = fonts.Heading;
        SetCenterPosition(toastTextGo.GetComponent<RectTransform>(), 0f, -16f, 180f, 20f);

        var toastPillGo = CreateUIObject("MovementPill", toastGo);
        var toastPillImg = toastPillGo.AddComponent<Image>();
        toastPillImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
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
        if (fonts != null) toastPillText.font = fonts.Heading;
        StretchRect(toastPillTextGo.GetComponent<RectTransform>());
        toastGo.SetActive(false);

        // ═══════════════════════════════════════════════════════════
        // G05 — TRACKING HUD + SLIDER (SafeArea children)
        // ═══════════════════════════════════════════════════════════
        var arControlsGo = CreateUIObject("ARControls", canvasStruct.SafeAreaGo);
        StretchRect(arControlsGo.GetComponent<RectTransform>());
        arControlsGo.SetActive(false);

        // G05 Header
        var arHeaderTitleGo = CreateUIObject("HeaderTitle", arControlsGo); // parented under arControlsGo!
        var arHeaderTitle = arHeaderTitleGo.AddComponent<TextMeshProUGUI>();
        arHeaderTitle.textWrappingMode = TextWrappingModes.Normal;
        arHeaderTitle.text = "GerakAR";
        arHeaderTitle.fontSize = 20f;
        arHeaderTitle.fontStyle = FontStyles.Bold;
        arHeaderTitle.color = WarmWhite;
        arHeaderTitle.alignment = TextAlignmentOptions.Center;
        if (fonts != null) arHeaderTitle.font = fonts.Display;
        SetAnchorTop(arHeaderTitleGo.GetComponent<RectTransform>(), 0f, -48f, 200f, 28f);

        var arHeaderSubGo = CreateUIObject("HeaderSub", arControlsGo); // parented under arControlsGo!
        var arHeaderSub = arHeaderSubGo.AddComponent<TextMeshProUGUI>();
        arHeaderSub.textWrappingMode = TextWrappingModes.Normal;
        arHeaderSub.text = "Belajar Gerak Jadi Seru";
        arHeaderSub.fontSize = 10f;
        arHeaderSub.color = SoftSand;
        arHeaderSub.alignment = TextAlignmentOptions.Center;
        if (fonts != null) arHeaderSub.font = fonts.Medium;
        SetAnchorTop(arHeaderSubGo.GetComponent<RectTransform>(), 0f, -76f, 200f, 16f);

        // FAB column on FloatingActions container
        var fabColumnGo = CreateUIObject("FABColumn", arControlsGo); // parented under arControlsGo!
        var fabVlg = fabColumnGo.AddComponent<VerticalLayoutGroup>();
        fabVlg.spacing = 10f; // Gap floating buttons 10-12
        fabVlg.childAlignment = TextAnchor.MiddleCenter;
        fabVlg.childControlWidth = true;
        fabVlg.childControlHeight = true;
        fabVlg.childForceExpandWidth = false;
        fabVlg.childForceExpandHeight = false;
        var fabCSF = fabColumnGo.AddComponent<ContentSizeFitter>();
        fabCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fabCSF.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        SetAnchorBottomRight(fabColumnGo.GetComponent<RectTransform>(), -16f, 150f, 52f, 180f);

        // FABs: PlayPause, Material, Close (SVG icons)
        var playPauseGo = CreateFAB(fabColumnGo, "PlayPauseButton", DeepForest, playIcon);
        var playPauseBtn = playPauseGo.GetComponent<Button>();

        var matBtnGo = CreateFAB(fabColumnGo, "MaterialButton", DeepForest, materiIcon);
        var matBtn = matBtnGo.GetComponent<Button>();

        var closeGo = CreateFAB(fabColumnGo, "CloseButton", DeepForest, closeIcon);
        var closeBtn = closeGo.GetComponent<Button>();

        // Timeline Card on BottomContent
        var timelineRootGo = CreateUIObject("TimelineCard", arControlsGo); // parented under arControlsGo!
        var tlCardImg = timelineRootGo.AddComponent<Image>();
        tlCardImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
        tlCardImg.type = Image.Type.Sliced;
        tlCardImg.color = WarmWhite;
        var tlCardOutline = timelineRootGo.AddComponent<Outline>();
        tlCardOutline.effectColor = SoftSand;
        tlCardOutline.effectDistance = new Vector2(1f, 1f);
        SetAnchorBottom(timelineRootGo.GetComponent<RectTransform>(), 0f, 16f, 328f, 120f);

        // Info row: status (centered, movement name label removed)
        var tlInfoRowGo = CreateUIObject("InfoRow", timelineRootGo);
        SetCenterPosition(tlInfoRowGo.GetComponent<RectTransform>(), 0f, 32f, 300f, 20f);

        var statusTagGo = CreateUIObject("StatusTag", tlInfoRowGo);
        var statusTag = statusTagGo.AddComponent<TextMeshProUGUI>();
        statusTag.textWrappingMode = TextWrappingModes.Normal;
        statusTag.text = "Status: Loop";
        statusTag.fontSize = 11f;
        statusTag.fontStyle = FontStyles.Bold;
        statusTag.color = ForestGreen;
        statusTag.alignment = TextAlignmentOptions.Center;
        if (fonts != null) statusTag.font = fonts.Heading;
        SetCenterPosition(statusTagGo.GetComponent<RectTransform>(), 0f, 0f, 200f, 14f);

        // Pose Slider
        var sliderGo = CreateUIObject("PoseSlider", timelineRootGo);
        var slider = sliderGo.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        SetCenterPosition(sliderGo.GetComponent<RectTransform>(), 0f, 2f, 300f, 32f);

        var trackContainerGo = CreateUIObject("TrackContainer", sliderGo);
        var tcRT = trackContainerGo.GetComponent<RectTransform>();
        tcRT.anchorMin = new Vector2(0f, 0.5f);
        tcRT.anchorMax = new Vector2(1f, 0.5f);
        tcRT.pivot = new Vector2(0.5f, 0.5f);
        tcRT.anchoredPosition = Vector2.zero;
        tcRT.sizeDelta = new Vector2(-24f, 12f); // Thick capsule height 12f

        var roundRect12 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
        var trackBgGo = CreateUIObject("TrackBackground", trackContainerGo);
        var trackBgImg = trackBgGo.AddComponent<Image>();
        trackBgImg.sprite = roundRect12;
        trackBgImg.type = Image.Type.Sliced;
        trackBgImg.preserveAspect = false;
        trackBgImg.color = new Color(0.663f, 0.745f, 0.635f, 0.4f); // Light sage green transparent capsule background
        var tbgRT = trackBgGo.GetComponent<RectTransform>();
        tbgRT.anchorMin = new Vector2(0f, 0f);
        tbgRT.anchorMax = new Vector2(1f, 1f);
        tbgRT.pivot = new Vector2(0.5f, 0.5f);
        tbgRT.anchoredPosition = Vector2.zero;
        tbgRT.sizeDelta = new Vector2(12f, 0f); // Extended by 6f on left and right to match node curvature

        var fillAreaGo = CreateUIObject("FillArea", trackContainerGo);
        var faRT = fillAreaGo.GetComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0f, 0f);
        faRT.anchorMax = new Vector2(1f, 1f);
        faRT.pivot = new Vector2(0.5f, 0.5f);
        faRT.anchoredPosition = Vector2.zero;
        faRT.sizeDelta = new Vector2(12f, 0f); // Extended by 6f on left and right to allow active fill to cover capsule caps

        var activeFillGo = CreateUIObject("ActiveFill", fillAreaGo);
        var fillImg = activeFillGo.AddComponent<Image>();
        fillImg.sprite = roundRect12;
        fillImg.type = Image.Type.Sliced;
        fillImg.preserveAspect = false;
        fillImg.color = ForestGreen; // Thick Forest Green active fill
        var afRT = activeFillGo.GetComponent<RectTransform>();
        afRT.anchorMin = new Vector2(0f, 0f);
        afRT.anchorMax = new Vector2(0f, 1f);
        afRT.pivot = new Vector2(0f, 0.5f);
        afRT.anchoredPosition = Vector2.zero;
        afRT.sizeDelta = Vector2.zero; // driven by slider
        slider.fillRect = afRT;

        var handleAreaGo = CreateUIObject("HandleTouchArea", sliderGo);
        handleAreaGo.AddComponent<Image>().color = Color.clear;
        var haRT = handleAreaGo.GetComponent<RectTransform>();
        haRT.anchorMin = new Vector2(0f, 0.5f);
        haRT.anchorMax = new Vector2(1f, 0.5f);
        haRT.pivot = new Vector2(0.5f, 0.5f);
        haRT.anchoredPosition = Vector2.zero;
        haRT.sizeDelta = new Vector2(-24f, 32f); // Same horizontal bounds as TrackContainer, height 32 for ease of touch

        // Perfect round handle container that the slider will position horizontally
        var sliderHandleGo = CreateUIObject("Handle", handleAreaGo);
        var hRT = sliderHandleGo.GetComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0f, 0f);
        hRT.anchorMax = new Vector2(0f, 1f);
        hRT.pivot = new Vector2(0.5f, 0.5f);
        hRT.anchoredPosition = Vector2.zero;
        hRT.sizeDelta = new Vector2(28f, 0f);
        slider.handleRect = hRT;

        // Circular knob visual as a child of the handle
        var handleVisGo = CreateUIObject("HandleVisual", sliderHandleGo);
        var handleVisImg = handleVisGo.AddComponent<Image>();
        handleVisImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/Circle-24.png");
        handleVisImg.type = Image.Type.Simple;
        handleVisImg.color = WarmWhite;
        var hvRT = handleVisGo.GetComponent<RectTransform>();
        hvRT.anchorMin = new Vector2(0.5f, 0.5f);
        hvRT.anchorMax = new Vector2(0.5f, 0.5f);
        hvRT.pivot = new Vector2(0.5f, 0.5f);
        hvRT.anchoredPosition = Vector2.zero;
        hvRT.sizeDelta = new Vector2(28f, 28f); // 28x28 white circle knob

        // Soft drop shadow to the white circle knob
        var shadow = handleVisGo.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.25f);
        shadow.effectDistance = new Vector2(0f, -2f);

        var nodeContainerGo = CreateUIObject("NodeContainer", trackContainerGo);
        var ncRT = nodeContainerGo.GetComponent<RectTransform>();
        ncRT.anchorMin = new Vector2(0f, 0f);
        ncRT.anchorMax = new Vector2(1f, 1f);
        ncRT.pivot = new Vector2(0.5f, 0.5f);
        ncRT.anchoredPosition = Vector2.zero;
        ncRT.sizeDelta = Vector2.zero;

        var markerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/App/Prefabs/TimelineMarker.prefab");

        var tlBottomLabelsGo = CreateUIObject("BottomLabels", timelineRootGo);
        SetCenterPosition(tlBottomLabelsGo.GetComponent<RectTransform>(), 0f, -28f, 300f, 14f);

        var startLabelGo = CreateUIObject("Mulai", tlBottomLabelsGo);
        var startLabel = startLabelGo.AddComponent<TextMeshProUGUI>();
        startLabel.textWrappingMode = TextWrappingModes.Normal;
        startLabel.text = "Mulai";
        startLabel.fontSize = 11f;
        startLabel.color = SecondaryText;
        startLabel.alignment = TextAlignmentOptions.Left;
        if (fonts != null) startLabel.font = fonts.Medium;
        SetCenterPosition(startLabelGo.GetComponent<RectTransform>(), -130f, 0f, 50f, 14f);

        var midLabelGo = CreateUIObject("Petunjuk", tlBottomLabelsGo);
        var midLabel = midLabelGo.AddComponent<TextMeshProUGUI>();
        midLabel.textWrappingMode = TextWrappingModes.Normal;
        midLabel.text = "Geser untuk memeriksa pose";
        midLabel.fontSize = 11f;
        midLabel.color = SecondaryText;
        midLabel.alignment = TextAlignmentOptions.Center;
        if (fonts != null) midLabel.font = fonts.Medium;
        SetCenterPosition(midLabelGo.GetComponent<RectTransform>(), 0f, 0f, 160f, 14f);

        var endLabelGo = CreateUIObject("Selesai", tlBottomLabelsGo);
        var endLabel = endLabelGo.AddComponent<TextMeshProUGUI>();
        endLabel.textWrappingMode = TextWrappingModes.Normal;
        endLabel.text = "Selesai";
        endLabel.fontSize = 11f;
        endLabel.color = SecondaryText;
        endLabel.alignment = TextAlignmentOptions.Right;
        if (fonts != null) endLabel.font = fonts.Medium;
        SetCenterPosition(endLabelGo.GetComponent<RectTransform>(), 130f, 0f, 50f, 14f);

        var timelineCtrl = timelineRootGo.AddComponent<PoseTimelineController>();
        var serialTimeline = new SerializedObject(timelineCtrl);
        serialTimeline.FindProperty("timelineSlider").objectReferenceValue = slider;
        serialTimeline.FindProperty("movementController").objectReferenceValue = movementController;
        serialTimeline.FindProperty("markerContainer").objectReferenceValue = nodeContainerGo;
        serialTimeline.FindProperty("markerPrefab").objectReferenceValue = markerPrefab;
        serialTimeline.ApplyModifiedProperties();

        // ═══════════════════════════════════════════════════════════
        // ═══════════════════════════════════════════════════════════
        // G06 — BOTTOM SHEET (Child of SafeArea)
        // ═══════════════════════════════════════════════════════════
        var sheetGo = CreateUIObject("BottomSheet", canvasStruct.SafeAreaGo);
        var sheetImg = sheetGo.AddComponent<Image>();
        sheetImg.sprite = roundTopSprite;
        sheetImg.type = Image.Type.Sliced;
        sheetImg.color = WarmCream; // WarmCream background for the entire sheet
        var sheetRT = sheetGo.GetComponent<RectTransform>();
        sheetRT.anchorMin = new Vector2(0f, 0f);
        sheetRT.anchorMax = new Vector2(1f, 0f);
        sheetRT.pivot = new Vector2(0.5f, 1f);
        sheetRT.anchoredPosition = new Vector2(0f, 0f);
        sheetRT.sizeDelta = new Vector2(0f, 752f);

        // Header area — Transparent/WarmCream with thin border at bottom
        var sheetHeaderGo = CreateUIObject("SheetHeader", sheetGo);
        var shRT = sheetHeaderGo.GetComponent<RectTransform>();
        shRT.anchorMin = new Vector2(0f, 1f);
        shRT.anchorMax = new Vector2(1f, 1f);
        shRT.pivot = new Vector2(0.5f, 1f);
        shRT.anchoredPosition = new Vector2(0f, -16f); // Offset slightly below grab handle
        shRT.sizeDelta = new Vector2(0f, 64f); // height 64f for kicker + title + subtitle

        // Bottom separator line for header
        var sepGo = CreateUIObject("Separator", sheetHeaderGo);
        var sepImg = sepGo.AddComponent<Image>();
        sepImg.sprite = uiSolidRect;
        sepImg.type = Image.Type.Simple;
        sepImg.color = new Color(0.663f, 0.745f, 0.635f, 0.2f); // #A9BEA2 with 20% alpha
        var sepRT = sepGo.GetComponent<RectTransform>();
        sepRT.anchorMin = new Vector2(0f, 0f);
        sepRT.anchorMax = new Vector2(1f, 0f);
        sepRT.pivot = new Vector2(0.5f, 0f);
        sepRT.anchoredPosition = Vector2.zero;
        sepRT.sizeDelta = new Vector2(0f, 1f);

        // Grab handle
        var handleGo = CreateUIObject("GrabHandle", sheetGo);
        var handleImg = handleGo.AddComponent<Image>();
        handleImg.sprite = uiSolidRect;
        handleImg.type = Image.Type.Simple;
        handleImg.preserveAspect = false;
        handleImg.color = SoftSand;
        SetCenterPosition(handleGo.GetComponent<RectTransform>(), 0f, sheetRT.sizeDelta.y - 8f, 40f, 4f); // radius atas 28, handle 40x4

        // Left text group
        var leftGroupGo = CreateUIObject("LeftGroup", sheetHeaderGo);
        var lgRT = leftGroupGo.GetComponent<RectTransform>();
        lgRT.anchorMin = new Vector2(0f, 0.5f);
        lgRT.anchorMax = new Vector2(0f, 0.5f);
        lgRT.pivot = new Vector2(0f, 0.5f);
        lgRT.anchoredPosition = new Vector2(20f, 0f);
        lgRT.sizeDelta = new Vector2(200f, 52f);

        var leftVlg = leftGroupGo.AddComponent<VerticalLayoutGroup>();
        leftVlg.spacing = 2f;
        leftVlg.childAlignment = TextAnchor.MiddleLeft;
        leftVlg.childControlWidth = true;
        leftVlg.childControlHeight = true;
        leftVlg.childForceExpandWidth = true;
        leftVlg.childForceExpandHeight = false;

        // Kicker badge
        var kickerBadgeGo = CreateUIObject("KickerBadge", leftGroupGo);
        var kickerBadgeImg = kickerBadgeGo.AddComponent<Image>();
        kickerBadgeImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-24.png");
        kickerBadgeImg.type = Image.Type.Sliced;
        kickerBadgeImg.color = new Color(0.7215f, 0.4078f, 0.2902f, 0.12f); // Default Terracotta with 12% alpha

        var badgeHlg = kickerBadgeGo.AddComponent<HorizontalLayoutGroup>();
        badgeHlg.padding = new RectOffset(8, 8, 2, 2);
        badgeHlg.childAlignment = TextAnchor.MiddleCenter;
        badgeHlg.childControlWidth = true;
        badgeHlg.childControlHeight = true;
        badgeHlg.childForceExpandWidth = false;
        badgeHlg.childForceExpandHeight = false;

        var categoryGo = CreateUIObject("CategoryTypeLabel", kickerBadgeGo);
        var categoryTxt = categoryGo.AddComponent<TextMeshProUGUI>();
        categoryTxt.textWrappingMode = TextWrappingModes.Normal;
        categoryTxt.text = "SQUAT";
        categoryTxt.fontSize = 9f;
        categoryTxt.fontStyle = FontStyles.Bold;
        categoryTxt.color = new Color(0.7215f, 0.4078f, 0.2902f, 1f); // Default Terracotta
        categoryTxt.alignment = TextAlignmentOptions.Center;
        if (fonts != null) categoryTxt.font = fonts.Heading;

        var kickerLE = kickerBadgeGo.AddComponent<LayoutElement>();
        kickerLE.preferredHeight = 16f;
        kickerLE.flexibleWidth = 0f;

        // Title
        var sheetTitleGo = CreateUIObject("MovementTitle", leftGroupGo);
        var sheetTitleText = sheetTitleGo.AddComponent<TextMeshProUGUI>();
        sheetTitleText.textWrappingMode = TextWrappingModes.Normal;
        sheetTitleText.text = "Squat";
        sheetTitleText.fontSize = 18f;
        sheetTitleText.fontStyle = FontStyles.Bold;
        sheetTitleText.color = DeepForest;
        sheetTitleText.alignment = TextAlignmentOptions.Left;
        if (fonts != null) sheetTitleText.font = fonts.Heading;
        var titleLE = sheetTitleGo.AddComponent<LayoutElement>();
        titleLE.preferredHeight = 22f;

        // Subtitle
        var subtitleGo = CreateUIObject("MovementSubtitle", leftGroupGo);
        var subtitleTxt = subtitleGo.AddComponent<TextMeshProUGUI>();
        subtitleTxt.textWrappingMode = TextWrappingModes.Normal;
        subtitleTxt.text = "Latihan kekuatan kaki dan keseimbangan.";
        subtitleTxt.fontSize = 11f;
        subtitleTxt.color = SecondaryText;
        subtitleTxt.alignment = TextAlignmentOptions.Left;
        if (fonts != null) subtitleTxt.font = fonts.Body;
        var subLE = subtitleGo.AddComponent<LayoutElement>();
        subLE.preferredHeight = 14f;

        // Right buttons group
        var rightGroupGo = CreateUIObject("RightGroup", sheetHeaderGo);
        var rgRT = rightGroupGo.GetComponent<RectTransform>();
        rgRT.anchorMin = new Vector2(1f, 0.5f);
        rgRT.anchorMax = new Vector2(1f, 0.5f);
        rgRT.pivot = new Vector2(1f, 0.5f);
        rgRT.anchoredPosition = new Vector2(-20f, 0f);
        rgRT.sizeDelta = new Vector2(140f, 44f);

        var rightHlg = rightGroupGo.AddComponent<HorizontalLayoutGroup>();
        rightHlg.spacing = 8f;
        rightHlg.childAlignment = TextAnchor.MiddleRight;
        rightHlg.childControlWidth = false;
        rightHlg.childControlHeight = false;
        rightHlg.childForceExpandWidth = false;
        rightHlg.childForceExpandHeight = false;

        // Back to primary button (G07 -> G06)
        var backBtnGo = CreateUIObject("BackToPrimaryButton", rightGroupGo);
        var backBtnImg = backBtnGo.AddComponent<Image>();
        backBtnImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-24.png"); // pill shape
        backBtnImg.type = Image.Type.Sliced;
        backBtnImg.color = ForestGreen;
        var backBtn = backBtnGo.AddComponent<Button>();
        var backBtnRT = backBtnGo.GetComponent<RectTransform>();
        backBtnRT.sizeDelta = new Vector2(72f, 32f); // compact pill size

        var backBtnTextGo = CreateUIObject("Text", backBtnGo);
        var backBtnText = backBtnTextGo.AddComponent<TextMeshProUGUI>();
        backBtnText.textWrappingMode = TextWrappingModes.Normal;
        backBtnText.text = "Kembali";
        backBtnText.fontSize = 11f;
        backBtnText.fontStyle = FontStyles.Bold;
        backBtnText.color = WarmWhite;
        backBtnText.alignment = TextAlignmentOptions.Center;
        if (fonts != null) backBtnText.font = fonts.Heading;
        StretchRect(backBtnTextGo.GetComponent<RectTransform>());

        // Close X button (circle visual 34 size, touch target 44)
        var sheetCloseGo = CreateUIObject("SheetCloseX", rightGroupGo);
        var sheetCloseImg = sheetCloseGo.AddComponent<Image>();
        sheetCloseImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/Circle-24.png"); // perfect circle!
        sheetCloseImg.type = Image.Type.Simple;
        sheetCloseImg.color = new Color(0.125f, 0.149f, 0.125f, 0.05f); // #202620 with 5% alpha
        var sheetCloseBtn = sheetCloseGo.AddComponent<Button>();
        var closeRT = sheetCloseGo.GetComponent<RectTransform>();
        closeRT.sizeDelta = new Vector2(34f, 34f); // visual size 34x34

        var xIconGo = CreateUIObject("Icon", sheetCloseGo);
        var xIconImg = xIconGo.AddComponent<Image>();
        xIconImg.sprite = closeIcon;
        xIconImg.preserveAspect = true;
        xIconImg.raycastTarget = false;
        xIconImg.color = new Color(0.125f, 0.149f, 0.125f, 1f); // #202620 (Charcoal)
        SetCenterPosition(xIconGo.GetComponent<RectTransform>(), 0f, 0f, 16f, 16f);

        // Bottom sheet scroll view
        var scrollViewGo = CreateUIObject("ScrollView", sheetGo);
        var scrollRect = scrollViewGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        var svRT = scrollViewGo.GetComponent<RectTransform>();
        svRT.anchorMin = new Vector2(0f, 0f);
        svRT.anchorMax = new Vector2(1f, 1f);
        svRT.pivot = new Vector2(0.5f, 0.5f);
        svRT.offsetMin = new Vector2(20f, 20f);    // 20px bottom, left margin
        svRT.offsetMax = new Vector2(-20f, -80f);  // 20px right margin, starts 80px below top (below header)

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
        if (fonts != null) aboutTitle.font = fonts.Heading;

        var descGo = CreateUIObject("Description", aboutGroupGo);
        var descText = descGo.AddComponent<TextMeshProUGUI>();
        descText.textWrappingMode = TextWrappingModes.Normal;
        descText.fontSize = 13f;
        descText.color = SecondaryText;
        if (fonts != null) descText.font = fonts.Body;

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
        if (fonts != null) stepsTitle.font = fonts.Heading;

        var stepsContainerGo = CreateUIObject("Container", stepsGroupGo);
        var stepsContVlg = stepsContainerGo.AddComponent<VerticalLayoutGroup>();
        stepsContVlg.spacing = 10f;
        stepsContVlg.childControlWidth = true;
        stepsContVlg.childControlHeight = true;

        // Section: Safety Tip Card
        var safetyTipCardGo = CreateUIObject("SafetyTipCard", contentGo);
        var safetyImg = safetyTipCardGo.AddComponent<Image>();
        safetyImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
        safetyImg.type = Image.Type.Sliced;
        safetyImg.color = new Color(0.12f, 0.365f, 0.259f, 0.08f); // 8% ForestGreen background

        var safetyOutline = safetyTipCardGo.AddComponent<Outline>();
        safetyOutline.effectColor = SoftSand;
        safetyOutline.effectDistance = new Vector2(1f, 1f);

        var safetyHlg = safetyTipCardGo.AddComponent<HorizontalLayoutGroup>();
        safetyHlg.padding = new RectOffset(16, 16, 16, 16);
        safetyHlg.spacing = 12f;
        safetyHlg.childAlignment = TextAnchor.MiddleLeft;
        safetyHlg.childControlWidth = true;
        safetyHlg.childControlHeight = true;
        safetyHlg.childForceExpandWidth = false;
        safetyHlg.childForceExpandHeight = false;

        // Icon child
        var safetyIconGo = CreateUIObject("Icon", safetyTipCardGo);
        var safetyIconImg = safetyIconGo.AddComponent<Image>();
        safetyIconImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Icons/Lucide/shield-check.svg");
        safetyIconImg.color = ForestGreen;
        safetyIconImg.preserveAspect = true;
        var safetyIconRT = safetyIconGo.GetComponent<RectTransform>();
        safetyIconRT.sizeDelta = new Vector2(24f, 24f);

        // Text child
        var safetyTextGo = CreateUIObject("Text", safetyTipCardGo);
        var safetyText = safetyTextGo.AddComponent<TextMeshProUGUI>();
        safetyText.textWrappingMode = TextWrappingModes.Normal;
        safetyText.fontSize = 12f;
        safetyText.color = DeepForest;
        safetyText.fontStyle = FontStyles.Bold;
        if (fonts != null) safetyText.font = fonts.Heading;
        var safetyTextLE = safetyTextGo.AddComponent<LayoutElement>();
        safetyTextLE.flexibleWidth = 1f;

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
        if (fonts != null) mistakesTitle.font = fonts.Heading;

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
        if (fonts != null) trainedTitle.font = fonts.Heading;

        var trainedContainerGo = CreateUIObject("TrainedContainer", fullExtrasGo);
        var trainedGrid = trainedContainerGo.AddComponent<GridLayoutGroup>();
        trainedGrid.spacing = new Vector2(8f, 8f);
        trainedGrid.cellSize = new Vector2(146f, 32f); // two-column grid cards of size 146x32
        trainedGrid.childAlignment = TextAnchor.UpperLeft;
        trainedGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        trainedGrid.constraintCount = 2;

        var trainedCSF = trainedContainerGo.AddComponent<ContentSizeFitter>();
        trainedCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        trainedCSF.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

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
        if (fonts != null) relatedTitle.font = fonts.Heading;

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
        var scrimGo = CreateUIObject("Scrim", canvasStruct.SafeAreaGo);
        var scrimImg = scrimGo.AddComponent<Image>();
        scrimImg.color = new Color(0.07f, 0.216f, 0.165f, 0.32f); // camera scrim opacity
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

        var stepItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/App/Prefabs/StepItem.prefab");
        var bulletItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/App/Prefabs/BulletItem.prefab");
        var relatedCardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/App/Prefabs/RelatedCard.prefab");
        var muscleItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/App/Prefabs/MuscleItem.prefab");

        var serialMat = new SerializedObject(matCtrl);
        serialMat.FindProperty("categoryTypeLabel").objectReferenceValue = categoryTxt;
        serialMat.FindProperty("movementNameText").objectReferenceValue = sheetTitleText;
        serialMat.FindProperty("categoryAccentBar").objectReferenceValue = kickerBadgeImg;
        serialMat.FindProperty("movementSubtitleText").objectReferenceValue = subtitleTxt;
        serialMat.FindProperty("backToPrimaryButton").objectReferenceValue = backBtn;
        serialMat.FindProperty("shortDescriptionText").objectReferenceValue = descText;
        serialMat.FindProperty("stepsContainer").objectReferenceValue = stepsContainerGo.transform;
        serialMat.FindProperty("stepItemPrefab").objectReferenceValue = stepItemPrefab;
        serialMat.FindProperty("safetyTipText").objectReferenceValue = safetyText;
        serialMat.FindProperty("fullStateExtras").objectReferenceValue = fullExtrasGo;
        serialMat.FindProperty("trainedAreasContainer").objectReferenceValue = trainedContainerGo.transform;
        serialMat.FindProperty("commonMistakesContainer").objectReferenceValue = mistakesContainerGo.transform;
        serialMat.FindProperty("mistakesTitleText").objectReferenceValue = mistakesTitleGo;
        serialMat.FindProperty("trainedTitleText").objectReferenceValue = trainedTitleGo;
        serialMat.FindProperty("bulletItemPrefab").objectReferenceValue = bulletItemPrefab;
        serialMat.FindProperty("muscleItemPrefab").objectReferenceValue = muscleItemPrefab;
        serialMat.FindProperty("relatedCardsContainer").objectReferenceValue = relContentGo.transform;
        serialMat.FindProperty("relatedCardPrefab").objectReferenceValue = relatedCardPrefab;
        serialMat.ApplyModifiedProperties();

        // Link ARUIController
        var serialUI = new SerializedObject(arUI);
        serialUI.FindProperty("scanOverlay").objectReferenceValue = scanGo;
        serialUI.FindProperty("scanLine").objectReferenceValue = scanLineGo;
        serialUI.FindProperty("detectionToast").objectReferenceValue = toastGo;
        serialUI.FindProperty("arControls").objectReferenceValue = arControlsGo;
        serialUI.FindProperty("movementNameLabel").objectReferenceValue = null;
        serialUI.FindProperty("closeButton").objectReferenceValue = closeBtn;
        serialUI.FindProperty("materialButton").objectReferenceValue = matBtn;
        serialUI.FindProperty("timelineRoot").objectReferenceValue = timelineRootGo;
        serialUI.FindProperty("playPauseButton").objectReferenceValue = playPauseBtn;
        serialUI.FindProperty("playPauseIcon").objectReferenceValue = playPauseGo.transform.Find("Icon").GetComponent<Image>();
        serialUI.FindProperty("playSprite").objectReferenceValue = playIcon;
        serialUI.FindProperty("pauseSprite").objectReferenceValue = pauseIcon;
        serialUI.FindProperty("fullScreenBackground").objectReferenceValue = canvasStruct.FullScreenBgGo;
        serialUI.FindProperty("cameraReadyCover").objectReferenceValue = readyCoverGroup;
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
        string description, float x, float y, Sprite btnSprite, FontSet fonts)
    {
        var cardGo = CreateUIObject(name, parent);
        var cardImg = cardGo.AddComponent<Image>();
        cardImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
        cardImg.type = Image.Type.Sliced;
        cardImg.color = WarmWhite;

        var cardOutline = cardGo.AddComponent<Outline>();
        cardOutline.effectColor = SoftSand;
        cardOutline.effectDistance = new Vector2(1f, 1f);

        SetCenterPosition(cardGo.GetComponent<RectTransform>(), x, y, 306.7f, 52f);

        var numCircleGo = CreateUIObject("NumCircle", cardGo);
        var numCircleImg = numCircleGo.AddComponent<Image>();
        numCircleImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-08.png");
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
        if (fonts != null) numText.font = fonts.Body;
        StretchRect(numTextGo.GetComponent<RectTransform>());

        var descGo = CreateUIObject("Desc", cardGo);
        var desc = descGo.AddComponent<TextMeshProUGUI>();
        desc.textWrappingMode = TextWrappingModes.Normal;
        desc.text = description;
        desc.fontSize = 12f;
        desc.color = SecondaryText;
        desc.alignment = TextAlignmentOptions.Left;
        if (fonts != null) desc.font = fonts.Body;
        SetCenterPosition(descGo.GetComponent<RectTransform>(), 20f, 0f, 220f, 30f);
    }

    private static (Button, GameObject, GameObject) CreateMovementCard(
        GameObject parent, string name, string icon, string title, string subtitle,
        float x, float y, Sprite btnSprite, FontSet fonts)
    {
        var cardGo = CreateUIObject(name, parent);
        var cardImg = cardGo.AddComponent<Image>();
        cardImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
        cardImg.type = Image.Type.Sliced;
        cardImg.color = WarmWhite;

        var cardOutline = cardGo.AddComponent<Outline>();
        cardOutline.effectColor = SoftSand;
        cardOutline.effectDistance = new Vector2(1f, 1f);

        SetCenterPosition(cardGo.GetComponent<RectTransform>(), x, y, 300f, 56f);

        var iconGo = CreateUIObject("Icon", cardGo);
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-08.png");
        iconImg.type = Image.Type.Sliced;
        iconImg.color = SoftSand;
        SetCenterPosition(iconGo.GetComponent<RectTransform>(), -120f, 0f, 32f, 32f);

        var iconTextGo = CreateUIObject("Text", iconGo);
        var iconText = iconTextGo.AddComponent<TextMeshProUGUI>();
        iconText.text = icon;
        iconText.fontSize = 11f;
        iconText.fontStyle = FontStyles.Bold;
        iconText.color = ForestGreen;
        iconText.alignment = TextAlignmentOptions.Center;
        if (fonts != null) iconText.font = fonts.Heading;
        StretchRect(iconTextGo.GetComponent<RectTransform>());

        var titleGo = CreateUIObject("TitleText", cardGo);
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.textWrappingMode = TextWrappingModes.Normal;
        titleText.text = $"<b>{title}</b>\n<size=10><color=#1F5D42>{subtitle}</color></size>";
        titleText.fontSize = 12f;
        titleText.color = DeepForest;
        titleText.alignment = TextAlignmentOptions.Left;
        if (fonts != null) titleText.font = fonts.Display;
        SetCenterPosition(titleGo.GetComponent<RectTransform>(), 10f, 0f, 180f, 36f);

        var bukaGo = CreateUIObject("BukaButton", cardGo);
        var bukaImg = bukaGo.AddComponent<Image>();
        bukaImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Icons/Lucide/chevron-right.svg");
        bukaImg.preserveAspect = true;
        bukaImg.raycastTarget = true;
        bukaImg.color = ForestGreen;
        var bukaBtn = bukaGo.AddComponent<Button>();
        SetCenterPosition(bukaGo.GetComponent<RectTransform>(), 124f, 0f, 24f, 24f);

        return (bukaBtn, cardGo, bukaGo);
    }

    private static GameObject CreateFAB(GameObject parent, string name, Color bgColor, Sprite iconSprite)
    {
        var go = CreateUIObject(name, parent);
        var img = go.AddComponent<Image>();
        img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-24.png");
        img.type = Image.Type.Sliced;
        img.color = bgColor;
        go.AddComponent<Button>();

        var goRT = go.GetComponent<RectTransform>();
        goRT.sizeDelta = new Vector2(52f, 52f);

        var iconGo = CreateUIObject("Icon", go);
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.sprite = iconSprite;
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;
        iconImg.color = WarmWhite;
        
        var iconRT = iconGo.GetComponent<RectTransform>();
        iconRT.localScale = Vector3.one;
        SetCenterPosition(iconRT, 0f, 0f, 24f, 24f);

        return go;
    }

    private static GameObject CreateCollapsibleWarning(GameObject parent, Sprite btnSprite,
        FontSet fonts, string iconPath)
    {
        var warnGo = CreateUIObject("CollapsibleWarning", parent);
        var warnImg = warnGo.AddComponent<Image>();
        warnImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
        warnImg.type = Image.Type.Sliced;
        warnImg.color = new Color(0.12f, 0.365f, 0.259f, 0.08f); // 8% alpha ForestGreen background

        var warnOutline = warnGo.AddComponent<Outline>();
        warnOutline.effectColor = SoftSand;
        warnOutline.effectDistance = new Vector2(1f, 1f);

        var layout = warnGo.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.padding = new RectOffset(12, 12, 10, 10);

        // Top row: icon + title + chevron
        var topRowGo = CreateUIObject("TopRow", warnGo);
        var topRowRT = topRowGo.GetComponent<RectTransform>();
        topRowRT.sizeDelta = new Vector2(0f, 24f);

        var iconGo = CreateUIObject("Icon", topRowGo);
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Icons/Lucide/info.svg");
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;
        iconImg.color = ForestGreen;
        SetCenterPosition(iconGo.GetComponent<RectTransform>(), -130f, 0f, 20f, 20f);

        var titleWarnGo = CreateUIObject("Title", topRowGo);
        var titleWarnText = titleWarnGo.AddComponent<TextMeshProUGUI>();
        titleWarnText.text = "Mode tanpa kamera";
        titleWarnText.fontSize = 13f;
        titleWarnText.fontStyle = FontStyles.Bold;
        titleWarnText.color = DeepForest;
        titleWarnText.alignment = TextAlignmentOptions.Left;
        if (fonts != null) titleWarnText.font = fonts.Heading;
        SetCenterPosition(titleWarnGo.GetComponent<RectTransform>(), 0f, 0f, 180f, 22f);

        var chevronGo = CreateUIObject("Chevron", topRowGo);
        var chevronImg = chevronGo.AddComponent<Image>();
        chevronImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Icons/Lucide/chevron-down.svg");
        chevronImg.preserveAspect = true;
        chevronImg.raycastTarget = false;
        chevronImg.color = ForestGreen;
        SetCenterPosition(chevronGo.GetComponent<RectTransform>(), 130f, 0f, 20f, 20f);

        // Supporting content (shown when collapsed)
        var supportingGo = CreateUIObject("SupportingContent", warnGo);
        var supportingText = supportingGo.AddComponent<TextMeshProUGUI>();
        supportingText.textWrappingMode = TextWrappingModes.Normal;
        supportingText.text = "Ketuk untuk melihat penjelasan";
        supportingText.fontSize = 11f;
        supportingText.color = SecondaryText;
        if (fonts != null) supportingText.font = fonts.Medium;

        // Expanded content (hidden by default)
        var expandedGo = CreateUIObject("ExpandedContent", warnGo);
        var expandedText = expandedGo.AddComponent<TextMeshProUGUI>();
        expandedText.textWrappingMode = TextWrappingModes.Normal;
        expandedText.text = "Perangkat belum mendukung mode AR. Kamu tetap dapat mempelajari seluruh gerakan, materi, dan panduan audio tanpa kamera.";
        expandedText.fontSize = 12f;
        expandedText.color = SecondaryText;
        if (fonts != null) expandedText.font = fonts.Medium;
        expandedGo.SetActive(false);

        // Button for toggling
        var toggleBtn = warnGo.AddComponent<Button>();

        // Store references for runtime
        var toggleComp = warnGo.AddComponent<CollapsibleWarningToggle>();
        var chevronDownSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Icons/Lucide/chevron-down.svg");
        var chevronUpSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Icons/Lucide/chevron-up.svg");

        var serialToggle = new SerializedObject(toggleComp);
        serialToggle.FindProperty("expandedContent").objectReferenceValue = expandedGo;
        serialToggle.FindProperty("supportingContent").objectReferenceValue = supportingGo;
        serialToggle.FindProperty("chevronImage").objectReferenceValue = chevronImg;
        serialToggle.FindProperty("chevronDownSprite").objectReferenceValue = chevronDownSprite;
        serialToggle.FindProperty("chevronUpSprite").objectReferenceValue = chevronUpSprite;
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

    public struct CanvasStructure
    {
        public GameObject CanvasGo;
        public GameObject CameraBgGo;
        public GameObject FullScreenBgGo;
        public GameObject SafeAreaGo;
        public GameObject TopContentGo;
        public GameObject CenterContentGo;
        public GameObject FloatingActionsGo;
        public GameObject BottomContentGo;
    }

    private static CanvasStructure CreateResponsiveCanvas(string name)
    {
        var canvasGo = new GameObject(name, typeof(RectTransform));
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(360f, 800f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // CameraBackground
        var camBg = CreateUIObject("CameraBackground", canvasGo);
        StretchRect(camBg.GetComponent<RectTransform>());

        // FullScreenBackground
        var fullBg = CreateUIObject("FullScreenBackground", canvasGo);
        StretchRect(fullBg.GetComponent<RectTransform>());

        // SafeArea
        var safeArea = CreateUIObject("SafeArea", canvasGo);
        StretchRect(safeArea.GetComponent<RectTransform>());
        safeArea.AddComponent<GerakAR.UI.SafeAreaController>();

        // TopContent
        var top = CreateUIObject("TopContent", safeArea);
        var topRT = top.GetComponent<RectTransform>();
        topRT.anchorMin = new Vector2(0f, 1f);
        topRT.anchorMax = new Vector2(1f, 1f);
        topRT.pivot = new Vector2(0.5f, 1f);
        topRT.anchoredPosition = Vector2.zero;
        topRT.sizeDelta = new Vector2(0f, 80f);

        // CenterContent
        var center = CreateUIObject("CenterContent", safeArea);
        var centerRT = center.GetComponent<RectTransform>();
        centerRT.anchorMin = Vector2.zero;
        centerRT.anchorMax = Vector2.one;
        centerRT.pivot = new Vector2(0.5f, 0.5f);
        centerRT.anchoredPosition = Vector2.zero;
        centerRT.sizeDelta = Vector2.zero;

        // FloatingActions
        var floating = CreateUIObject("FloatingActions", safeArea);
        var floatRT = floating.GetComponent<RectTransform>();
        floatRT.anchorMin = new Vector2(1f, 0.5f);
        floatRT.anchorMax = new Vector2(1f, 0.5f);
        floatRT.pivot = new Vector2(1f, 0.5f);
        floatRT.anchoredPosition = new Vector2(-16f, 0f);
        floatRT.sizeDelta = new Vector2(52f, 200f);

        // BottomContent
        var bottom = CreateUIObject("BottomContent", safeArea);
        var bottomRT = bottom.GetComponent<RectTransform>();
        bottomRT.anchorMin = new Vector2(0f, 0f);
        bottomRT.anchorMax = new Vector2(1f, 0f);
        bottomRT.pivot = new Vector2(0.5f, 0f);
        bottomRT.anchoredPosition = Vector2.zero;
        bottomRT.sizeDelta = new Vector2(0f, 180f);

        return new CanvasStructure
        {
            CanvasGo = canvasGo,
            CameraBgGo = camBg,
            FullScreenBgGo = fullBg,
            SafeAreaGo = safeArea,
            TopContentGo = top,
            CenterContentGo = center,
            FloatingActionsGo = floating,
            BottomContentGo = bottom
        };
    }

    private static void ImportRelatedMovementsSprites()
    {
        string srcDir = "../components";
        string destDir = "Assets/App/UI/Sprites/Related";
        Directory.CreateDirectory(destDir);

        var mappings = new Dictionary<string, string>
        {
            { "Bodyweight Squat", "1-High-Bar Back Squat.png" },
            { "Squat Jump", "5-Jumping Squat.png" },
            { "Pistol Squat", "6-Single-Leg Squat.png" },
            { "Front Squat", "3-Front Squat.png" },

            { "Standing Toe Touch", "13-Standing Toe Touch.png" },
            { "Diagonal Reach", "10-Low_High Jacks.png" },
            { "Trunk Rotation", "14-Stepping Trunk Turn.png" },
            { "High Knee March", "11-High-Knee March.png" },

            { "One-In", "L1.png" },
            { "Two-In", "L2.png" },
            { "Side Shuffle", "L3.png" },
            { "Two-Foot Hop", "24-2-Foot Hops.png" }
        };

        foreach (var kvp in mappings)
        {
            string srcPath = Path.Combine(srcDir, kvp.Value);
            string destPath = Path.Combine(destDir, kvp.Value);
            if (File.Exists(srcPath))
            {
                File.Copy(srcPath, destPath, true);
                AssetDatabase.ImportAsset(destPath);
                
                // Configure texture importer for UI Sprite
                var importer = AssetImporter.GetAtPath(destPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                }
            }
            else
            {
                Debug.LogWarning($"[SetupAndBuild] Sprite source file not found: {srcPath}");
            }
        }
        AssetDatabase.Refresh();
    }

    private static void AssignRelatedMovementsSprites()
    {
        string destDir = "Assets/App/UI/Sprites/Related";
        var mappings = new Dictionary<string, string>
        {
            { "Bodyweight Squat", "1-High-Bar Back Squat.png" },
            { "Squat Jump", "5-Jumping Squat.png" },
            { "Pistol Squat", "6-Single-Leg Squat.png" },
            { "Front Squat", "3-Front Squat.png" },

            { "Standing Toe Touch", "13-Standing Toe Touch.png" },
            { "Diagonal Reach", "10-Low_High Jacks.png" },
            { "Trunk Rotation", "14-Stepping Trunk Turn.png" },
            { "High Knee March", "11-High-Knee March.png" },

            { "One-In", "L1.png" },
            { "Two-In", "L2.png" },
            { "Side Shuffle", "L3.png" },
            { "Two-Foot Hop", "24-2-Foot Hops.png" }
        };

        string[] assetGuids = AssetDatabase.FindAssets("t:MovementData");
        foreach (string guid in assetGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var data = AssetDatabase.LoadAssetAtPath<MovementData>(path);
            if (data != null && data.relatedMovements != null)
            {
                bool changed = false;
                foreach (var rel in data.relatedMovements)
                {
                    if (mappings.TryGetValue(rel.title, out string fileName))
                    {
                        string spritePath = Path.Combine(destDir, fileName);
                        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                        if (sprite != null)
                        {
                            rel.thumbnail = sprite;
                            changed = true;
                        }
                    }
                }
                if (changed)
                {
                    EditorUtility.SetDirty(data);
                }
            }
        }
        AssetDatabase.SaveAssets();
    }
}
