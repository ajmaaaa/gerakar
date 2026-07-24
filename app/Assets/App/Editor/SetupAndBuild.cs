using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MotionLearn.Core;
using MotionLearn.AR;
using MotionLearn.Animation;
using MotionLearn.UI;
using MotionLearn.Audio;
using MotionLearn.Content;
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
        Debug.Log("[MotionLearn] Memulai setup scene...");

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
            "Assets/App/UI/Sprites/Shapes/RoundTop-24.png");

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
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "id.ac.unp.motionlearn");
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
        var stepShadow = stepGo.AddComponent<Shadow>();
        stepShadow.effectColor = new Color(0f, 0f, 0f, 0.06f);
        stepShadow.effectDistance = new Vector2(2f, -2f);
        var stepRT = stepGo.GetComponent<RectTransform>();
        stepRT.sizeDelta = new Vector2(320f, 56f); // 56f height like G02

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
        badgeImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/Circle-24.png"); // clean circle badge
        badgeImg.type = Image.Type.Sliced;
        badgeImg.color = new Color(0.918f, 0.867f, 0.812f, 0.6f); // SoftSand 60% background
        var badgeLE = badgeGo.AddComponent<LayoutElement>();
        badgeLE.minWidth = 32f;
        badgeLE.minHeight = 32f;
        badgeLE.preferredWidth = 32f;
        badgeLE.preferredHeight = 32f;

        var badgeTextGo = CreateUIObject("Text", badgeGo);
        var badgeText = badgeTextGo.AddComponent<TextMeshProUGUI>();
        badgeText.text = "1";
        badgeText.fontSize = 13f;
        badgeText.fontStyle = FontStyles.Bold;
        badgeText.color = DeepForest; // DeepForest text color for G02 style
        badgeText.alignment = TextAlignmentOptions.Center;
        if (fonts != null) badgeText.font = fonts.Heading;
        StretchRect(badgeTextGo.GetComponent<RectTransform>());

        // Text
        var textGo = CreateUIObject("Text", stepGo);
        var textTmp = textGo.AddComponent<TextMeshProUGUI>();
        textTmp.textWrappingMode = TextWrappingModes.Normal;
        textTmp.overflowMode = TextOverflowModes.Overflow;
        textTmp.fontSize = 12f;
        textTmp.color = SecondaryText;
        textTmp.alignment = TextAlignmentOptions.Left;
        if (fonts != null) textTmp.font = fonts.Medium;
        var textLE = textGo.AddComponent<LayoutElement>();
        textLE.flexibleWidth = 1f;

        PrefabUtility.SaveAsPrefabAsset(stepGo, "Assets/App/Prefabs/StepItem.prefab");
        Object.DestroyImmediate(stepGo);

        // 2. BulletItem prefab (mistakes card with x-circle SVG icon, styled like G02)
        var bulletGo = new GameObject("BulletItem", typeof(RectTransform));
        var bulletImg = bulletGo.AddComponent<Image>();
        bulletImg.sprite = roundRect12;
        bulletImg.type = Image.Type.Sliced;
        bulletImg.color = WarmWhite;
        var bulletOutline = bulletGo.AddComponent<Outline>();
        bulletOutline.effectColor = SoftSand;
        bulletOutline.effectDistance = new Vector2(1f, 1f);
        var bulletShadow = bulletGo.AddComponent<Shadow>();
        bulletShadow.effectColor = new Color(0f, 0f, 0f, 0.06f);
        bulletShadow.effectDistance = new Vector2(2f, -2f);
        var bulletRT = bulletGo.GetComponent<RectTransform>();
        bulletRT.sizeDelta = new Vector2(320f, 56f); // 56f height like G02

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
        bulletIconImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/Circle-24.png"); // clean circle badge
        bulletIconImg.type = Image.Type.Sliced;
        bulletIconImg.color = new Color(0.918f, 0.867f, 0.812f, 0.6f); // SoftSand 60% background
        var bulletIconLE = bulletIconGo.AddComponent<LayoutElement>();
        bulletIconLE.minWidth = 32f;
        bulletIconLE.minHeight = 32f;
        bulletIconLE.preferredWidth = 32f;
        bulletIconLE.preferredHeight = 32f;

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
        if (fonts != null) bulletText.font = fonts.Medium;
        var bulletTextLE = bulletTextGo.AddComponent<LayoutElement>();
        bulletTextLE.flexibleWidth = 1f;

        PrefabUtility.SaveAsPrefabAsset(bulletGo, "Assets/App/Prefabs/BulletItem.prefab");
        Object.DestroyImmediate(bulletGo);

        // 3. RelatedCard prefab (small horizontal card with top thumbnail and bottom title)
        var cardGo = new GameObject("RelatedCard", typeof(RectTransform));
        var cardImg = cardGo.AddComponent<Image>();
        cardImg.sprite = roundRect16;
        cardImg.type = Image.Type.Sliced;
        cardImg.color = WarmWhite;
        var cardOutline = cardGo.AddComponent<Outline>();
        cardOutline.effectColor = SoftSand;
        cardOutline.effectDistance = new Vector2(1f, 1f);
        var cardShadow = cardGo.AddComponent<Shadow>();
        cardShadow.effectColor = new Color(0f, 0f, 0f, 0.05f);
        cardShadow.effectDistance = new Vector2(2f, -2f);
        cardGo.AddComponent<Button>();
        var cardRT = cardGo.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(120f, 116f); // Small box card size

        var thumbGo = CreateUIObject("Thumbnail", cardGo);
        var thumbImg = thumbGo.AddComponent<Image>();
        thumbImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
        thumbImg.type = Image.Type.Sliced;
        thumbImg.preserveAspect = true;
        thumbImg.color = SoftSand;
        SetCenterPosition(thumbGo.GetComponent<RectTransform>(), 0f, 16f, 104f, 64f); // Top thumbnail positioning

        var titleGo = CreateUIObject("Title", cardGo);
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.textWrappingMode = TextWrappingModes.Normal;
        titleText.overflowMode = TextOverflowModes.Overflow;
        titleText.text = "Related";
        titleText.fontSize = 10f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = DeepForest;
        titleText.alignment = TextAlignmentOptions.Center;
        if (fonts != null) titleText.font = fonts.Heading;
        SetCenterPosition(titleGo.GetComponent<RectTransform>(), 0f, -34f, 110f, 26f); // Bottom title positioning

        RelatedMovementCardView.Configure(cardGo, null);

        PrefabUtility.SaveAsPrefabAsset(cardGo, "Assets/App/Prefabs/RelatedCard.prefab");
        Object.DestroyImmediate(cardGo);

        // 4. TimelineMarker prefab (small 10px circle node)
        var markerGo = new GameObject("TimelineMarker", typeof(RectTransform));
        var markerImg = markerGo.AddComponent<Image>();
        markerImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/Circle-24.png");
        markerImg.type = Image.Type.Simple;
        markerImg.preserveAspect = true;
        markerImg.color = ForestGreen;
        var markerRT = markerGo.GetComponent<RectTransform>();
        markerRT.sizeDelta = new Vector2(10f, 10f);
        PrefabUtility.SaveAsPrefabAsset(markerGo, "Assets/App/Prefabs/TimelineMarker.prefab");
        Object.DestroyImmediate(markerGo);

        // 5. MuscleItem prefab (clean full-width card with check icon, styled like G02)
        var muscleGo = new GameObject("MuscleItem", typeof(RectTransform));
        var muscleImg = muscleGo.AddComponent<Image>();
        muscleImg.sprite = roundRect12;
        muscleImg.type = Image.Type.Sliced;
        muscleImg.color = WarmWhite;
        var muscleOutline = muscleGo.AddComponent<Outline>();
        muscleOutline.effectColor = SoftSand;
        muscleOutline.effectDistance = new Vector2(1f, 1f);
        var muscleShadow = muscleGo.AddComponent<Shadow>();
        muscleShadow.effectColor = new Color(0f, 0f, 0f, 0.06f);
        muscleShadow.effectDistance = new Vector2(2f, -2f);
        var muscleRT = muscleGo.GetComponent<RectTransform>();
        muscleRT.sizeDelta = new Vector2(320f, 56f); // 56f height like G02

        var muscleHlg = muscleGo.AddComponent<HorizontalLayoutGroup>();
        muscleHlg.padding = new RectOffset(16, 16, 12, 12);
        muscleHlg.spacing = 12f;
        muscleHlg.childAlignment = TextAnchor.MiddleLeft;
        muscleHlg.childControlWidth = true;
        muscleHlg.childControlHeight = true;
        muscleHlg.childForceExpandWidth = false;
        muscleHlg.childForceExpandHeight = false;

        var muscleCsf = muscleGo.AddComponent<ContentSizeFitter>();
        muscleCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        muscleCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Check badge on the left
        var checkBadgeGo = CreateUIObject("Badge", muscleGo);
        var checkBadgeImg = checkBadgeGo.AddComponent<Image>();
        checkBadgeImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/Circle-24.png"); // clean circle badge
        checkBadgeImg.type = Image.Type.Sliced;
        checkBadgeImg.color = new Color(0.918f, 0.867f, 0.812f, 0.6f); // SoftSand 60% background
        var checkBadgeLE = checkBadgeGo.AddComponent<LayoutElement>();
        checkBadgeLE.minWidth = 32f;
        checkBadgeLE.minHeight = 32f;
        checkBadgeLE.preferredWidth = 32f;
        checkBadgeLE.preferredHeight = 32f;

        var checkIconGo = CreateUIObject("CheckIcon", checkBadgeGo);
        var checkIconImg = checkIconGo.AddComponent<Image>();
        checkIconImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Icons/Lucide/check.svg");
        checkIconImg.preserveAspect = true;
        checkIconImg.raycastTarget = false;
        checkIconImg.color = ForestGreen;
        var checkIconRT = checkIconGo.GetComponent<RectTransform>();
        checkIconRT.localScale = Vector3.one;
        SetCenterPosition(checkIconRT, 0f, 0f, 16f, 16f);

        // Muscle name text
        var muscleTextGo = CreateUIObject("Text", muscleGo);
        var muscleText = muscleTextGo.AddComponent<TextMeshProUGUI>();
        muscleText.text = "Muscle";
        muscleText.fontSize = 12f;
        muscleText.fontStyle = FontStyles.Bold;
        muscleText.color = DeepForest;
        muscleText.alignment = TextAlignmentOptions.Left;
        if (fonts != null) muscleText.font = fonts.Medium;
        var muscleTextLE = muscleTextGo.AddComponent<LayoutElement>();
        muscleTextLE.flexibleWidth = 1f;

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

        // Full-bleed cover image from components/background.png
        var coverGo = CreateUIObject("FullBleedCoverImage", introGo);
        var coverImg = coverGo.AddComponent<Image>();
        var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Branding/background.png");
        if (bgSprite != null)
        {
            coverImg.sprite = bgSprite;
            coverImg.type = Image.Type.Simple;
            coverImg.preserveAspect = false;
        }
        else
        {
            coverImg.color = DeepForest;
        }
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

        // Top identity Right (UNP Logo from components/unp.jpg)
        var topIdRightGo = CreateUIObject("TopIdentityRight", introGo);
        SetAnchorTopRight(topIdRightGo.GetComponent<RectTransform>(), -20f, -20f, 120f, 48f);

        var unpLogoGo = CreateUIObject("UNPLogo", topIdRightGo);
        var unpLogoImg = unpLogoGo.AddComponent<Image>();
        var unpLogoSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Branding/unp.png");
        if (unpLogoSprite != null)
        {
            unpLogoImg.sprite = unpLogoSprite;
            unpLogoImg.preserveAspect = true;
            unpLogoImg.raycastTarget = false;
        }
        StretchRect(unpLogoGo.GetComponent<RectTransform>());

        // Center visual placeholder
        var centerGo = CreateUIObject("CenterVisual", introGo);
        SetCenterPosition(centerGo.GetComponent<RectTransform>(), 0f, 50f, 200f, 200f);

        // Bottom brand group
        var brandGroupGo = CreateUIObject("BrandGroup", introGo);
        SetAnchorBottom(brandGroupGo.GetComponent<RectTransform>(), 0f, 100f, 320f, 130f);

        var titleGo = CreateUIObject("TitleText", brandGroupGo);
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.textWrappingMode = TextWrappingModes.Normal;
        titleText.text = "MotionLearn";
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
        trackImg.color = new Color(1f, 1f, 1f, 0.2f); // Semi-transparent white track background
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
        AddSoftShadow(startBtnGo, 2f, -3f, 0.12f); // Shadow on ForestGreen button
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

        // G08 Header (transparent)
        var g08HeaderBarGo = CreateUIObject("HeaderBar", nonARModePanelGo);
        var g08HeaderBarImg = g08HeaderBarGo.AddComponent<Image>();
        g08HeaderBarImg.color = Color.clear; // Transparent header background
        SetAnchorTop(g08HeaderBarGo.GetComponent<RectTransform>(), 0f, -16f, 360f, 56f);

        var g08BrandGo = CreateUIObject("Brand", g08HeaderBarGo);
        var g08BrandText = g08BrandGo.AddComponent<TextMeshProUGUI>();
        g08BrandText.textWrappingMode = TextWrappingModes.Normal;
        g08BrandText.text = "MODE PEMBELAJARAN MANDIRI";
        g08BrandText.fontSize = 11f;
        g08BrandText.fontStyle = FontStyles.Bold;
        g08BrandText.color = SecondaryText; // #716040
        g08BrandText.alignment = TextAlignmentOptions.Left;
        if (fonts != null) g08BrandText.font = fonts.Heading;
        SetCenterPosition(g08BrandGo.GetComponent<RectTransform>(), -60f, 0f, 200f, 24f);

        var g08BadgeGo = CreateUIObject("ModeBadge", g08HeaderBarGo);
        var g08BadgeImg = g08BadgeGo.AddComponent<Image>();
        g08BadgeImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-24.png"); // pill shape
        g08BadgeImg.type = Image.Type.Sliced;
        g08BadgeImg.color = new Color(0.918f, 0.867f, 0.812f, 0.6f); // 60% SoftSand tint background
        g08BadgeGo.AddComponent<Button>(); // For toggling warning card
        SetCenterPosition(g08BadgeGo.GetComponent<RectTransform>(), 110f, 0f, 100f, 28f);

        var g08BadgeTextGo = CreateUIObject("Text", g08BadgeGo);
        var g08BadgeText = g08BadgeTextGo.AddComponent<TextMeshProUGUI>();
        g08BadgeText.textWrappingMode = TextWrappingModes.Normal;
        g08BadgeText.text = "NON-AR MODE";
        g08BadgeText.fontSize = 9f;
        g08BadgeText.fontStyle = FontStyles.Bold;
        g08BadgeText.color = SecondaryText; // #716040
        g08BadgeText.alignment = TextAlignmentOptions.Center;
        if (fonts != null) g08BadgeText.font = fonts.Heading;
        StretchRect(g08BadgeTextGo.GetComponent<RectTransform>());

        // G08 Collapsible Warning (Popup Dialog Modal style)
        var warnOverlayGo = CreateUIObject("WarningPopupOverlay", nonARModePanelGo);
        var warnOverlayImg = warnOverlayGo.AddComponent<Image>();
        warnOverlayImg.color = new Color(0.07f, 0.216f, 0.165f, 0.32f); // #12372A with 32% opacity dim
        StretchRect(warnOverlayGo.GetComponent<RectTransform>());
        warnOverlayGo.SetActive(false); // Hidden by default

        var collapsibleWarnGo = CreateCollapsibleWarning(
            warnOverlayGo, btnSprite, fonts, "Assets/App/UI/Icons/Lucide/info.svg");
        var collapsibleWarnRT = collapsibleWarnGo.GetComponent<RectTransform>();
        SetCenterPosition(collapsibleWarnRT, 0f, 0f, 300f, 140f); // Centered modal dialog popup

        // G08 Catalog Content (shifted up to fill the empty top space)
        var catalogCatalogGo = CreateUIObject("CatalogCatalog", nonARModePanelGo);
        SetCenterPosition(catalogCatalogGo.GetComponent<RectTransform>(), 0f, 40f, 320f, 320f);

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
            catalogCatalogGo, "CardSquat", "SQ", "Squat",
            "Kekuatan kaki dan postur tubuh.",
            0f, 90f, btnSprite, fonts);

        // Dynamic Stretching card
        var (dynamicStretchBukaBtn, _, _) = CreateMovementCard(
            catalogCatalogGo, "CardDynamicStretch", "DS", "Dynamic Stretching",
            "Pemanasan aktif dan kelenturan tubuh.",
            0f, 10f, btnSprite, fonts);

        // Ladder Drill card
        var (ladderDrillBukaBtn, _, _) = CreateMovementCard(
            catalogCatalogGo, "CardLadderDrill", "LD", "Ladder Drill",
            "Kelincahan dan koordinasi gerakan.",
            0f, -70f, btnSprite, fonts);

        // Back button (hidden per user request)
        var catalogBackGo = CreateUIObject("CatalogBackButton", nonARModePanelGo);
        var catalogBackImg = catalogBackGo.AddComponent<Image>();
        catalogBackImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
        catalogBackImg.type = Image.Type.Sliced;
        catalogBackImg.color = Color.white;
        var catalogBackOutline = catalogBackGo.AddComponent<Outline>();
        catalogBackOutline.effectColor = SoftSand;
        catalogBackOutline.effectDistance = new Vector2(1f, 1f);
        AddSoftShadow(catalogBackGo, 2f, -2f, 0.06f); // Soft shadow
        var catalogBackBtn = catalogBackGo.AddComponent<Button>();
        SetAnchorBottom(catalogBackGo.GetComponent<RectTransform>(), 0f, 32f, 320f, 48f); // Wide card style
        catalogBackGo.SetActive(false);

        var catalogBackTextGo = CreateUIObject("Text", catalogBackGo);
        var catalogBackText = catalogBackTextGo.AddComponent<TextMeshProUGUI>();
        catalogBackText.textWrappingMode = TextWrappingModes.Normal;
        catalogBackText.text = "←      Kembali ke petunjuk";
        catalogBackText.fontSize = 13f;
        catalogBackText.fontStyle = FontStyles.Bold;
        catalogBackText.color = SecondaryText; // #716040
        catalogBackText.alignment = TextAlignmentOptions.Center;
        if (fonts != null) catalogBackText.font = fonts.Heading;
        StretchRect(catalogBackTextGo.GetComponent<RectTransform>());

        // ── G08 DETAIL — NON-AR DETAIL VIEW ──
        var nonARDetailPanelGo = CreateUIObject("NonARDetailPanel", unsupGo);
        var detailPanelImg = nonARDetailPanelGo.AddComponent<Image>();
        detailPanelImg.color = WarmCream;
        StretchRect(nonARDetailPanelGo.GetComponent<RectTransform>());
        nonARDetailPanelGo.SetActive(false);

        // Header for Non-AR Detail (consistent with G08 Catalog)
        var detailHeaderGo = CreateUIObject("HeaderBar", nonARDetailPanelGo);
        var detailHeaderImg = detailHeaderGo.AddComponent<Image>();
        detailHeaderImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-16.png");
        detailHeaderImg.type = Image.Type.Sliced;
        detailHeaderImg.color = WarmCream; // Solid background hides content scrolling underneath
        AddSoftShadow(detailHeaderGo, 0f, -4f, 0.15f); // Downward drop shadow on sticky header
        var dhRT = detailHeaderGo.GetComponent<RectTransform>();
        dhRT.anchorMin = new Vector2(0f, 1f);
        dhRT.anchorMax = new Vector2(1f, 1f);
        dhRT.pivot = new Vector2(0.5f, 1f);
        dhRT.anchoredPosition = Vector2.zero;
        dhRT.sizeDelta = new Vector2(0f, 76f);

        var detailLeftGroupGo = CreateUIObject("LeftGroup", detailHeaderGo);
        var dlgRT = detailLeftGroupGo.GetComponent<RectTransform>();
        dlgRT.anchorMin = new Vector2(0f, 0.5f);
        dlgRT.anchorMax = new Vector2(0f, 0.5f);
        dlgRT.pivot = new Vector2(0f, 0.5f);
        dlgRT.anchoredPosition = new Vector2(20f, -4f);
        dlgRT.sizeDelta = new Vector2(240f, 52f);

        var detailLeftVlg = detailLeftGroupGo.AddComponent<VerticalLayoutGroup>();
        detailLeftVlg.spacing = 2f;
        detailLeftVlg.childAlignment = TextAnchor.MiddleLeft;
        detailLeftVlg.childControlWidth = true;
        detailLeftVlg.childControlHeight = true;
        detailLeftVlg.childForceExpandWidth = true;
        detailLeftVlg.childForceExpandHeight = false;

        var detailKickerGo = CreateUIObject("CategoryTypeLabel", detailLeftGroupGo);
        var detailKickerTxt = detailKickerGo.AddComponent<TextMeshProUGUI>();
        detailKickerTxt.textWrappingMode = TextWrappingModes.Normal;
        detailKickerTxt.text = "GERAKAN UTAMA";
        detailKickerTxt.fontSize = 11f;
        detailKickerTxt.fontStyle = FontStyles.Bold;
        detailKickerTxt.color = ForestGreen;
        if (fonts != null) detailKickerTxt.font = fonts.Heading;
        var detailKickerLE = detailKickerGo.AddComponent<LayoutElement>();
        detailKickerLE.preferredHeight = 14f;

        var detailTitleGo = CreateUIObject("MovementTitle", detailLeftGroupGo);
        var detailTitleText = detailTitleGo.AddComponent<TextMeshProUGUI>();
        detailTitleText.textWrappingMode = TextWrappingModes.Normal;
        detailTitleText.text = "SQUAT";
        detailTitleText.fontSize = 22f;
        detailTitleText.fontStyle = FontStyles.Bold;
        detailTitleText.color = DeepForest;
        if (fonts != null) detailTitleText.font = fonts.Heading;
        var detailTitleLE = detailTitleGo.AddComponent<LayoutElement>();
        detailTitleLE.preferredHeight = 26f;

        // Right group close button
        var detailRightGroupGo = CreateUIObject("RightGroup", detailHeaderGo);
        var drgRT = detailRightGroupGo.GetComponent<RectTransform>();
        drgRT.anchorMin = new Vector2(1f, 0.5f);
        drgRT.anchorMax = new Vector2(1f, 0.5f);
        drgRT.pivot = new Vector2(1f, 0.5f);
        drgRT.anchoredPosition = new Vector2(-20f, 0f);
        drgRT.sizeDelta = new Vector2(44f, 44f);

        var detailCloseGo = CreateUIObject("DetailCloseX", detailRightGroupGo);
        var detailCloseImg = detailCloseGo.AddComponent<Image>();
        detailCloseImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
        detailCloseImg.type = Image.Type.Sliced;
        detailCloseImg.color = ForestGreen;
        AddSoftShadow(detailCloseGo, 2f, -2f, 0.1f);
        var detailCloseBtn = detailCloseGo.AddComponent<Button>();
        SetCenterPosition(detailCloseGo.GetComponent<RectTransform>(), 0f, 0f, 44f, 44f);

        var detailXIconGo = CreateUIObject("Icon", detailCloseGo);
        var detailXIconImg = detailXIconGo.AddComponent<Image>();
        detailXIconImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Icons/Lucide/x.svg");
        detailXIconImg.preserveAspect = true;
        detailXIconImg.raycastTarget = false;
        detailXIconImg.color = Color.white;
        SetCenterPosition(detailXIconGo.GetComponent<RectTransform>(), 0f, 0f, 14f, 14f);
        UIRuntimeStyler.NormalizeCloseButton(detailCloseBtn);

        var detailScrollViewGo = CreateUIObject("ScrollView", nonARDetailPanelGo);
        var detailScrollRect = detailScrollViewGo.AddComponent<ScrollRect>();
        detailScrollRect.horizontal = false;
        detailScrollRect.vertical = true;
        var dsvRT = detailScrollViewGo.GetComponent<RectTransform>();
        dsvRT.anchorMin = new Vector2(0f, 0f);
        dsvRT.anchorMax = new Vector2(1f, 1f);
        dsvRT.pivot = new Vector2(0.5f, 0.5f);
        dsvRT.offsetMin = new Vector2(0f, 0f);
        dsvRT.offsetMax = new Vector2(0f, -80f); // Leave space for header

        var dsvViewportGo = CreateUIObject("Viewport", detailScrollViewGo);
        dsvViewportGo.AddComponent<RectMask2D>();
        var dsvViewportRT = dsvViewportGo.GetComponent<RectTransform>();
        StretchRect(dsvViewportRT);
        detailScrollRect.viewport = dsvViewportRT;

        var dsvContentGo = CreateUIObject("Content", dsvViewportGo);
        var dsvContentRT = dsvContentGo.GetComponent<RectTransform>();
        dsvContentRT.anchorMin = new Vector2(0f, 1f);
        dsvContentRT.anchorMax = new Vector2(1f, 1f);
        dsvContentRT.pivot = new Vector2(0.5f, 1f);
        dsvContentRT.anchoredPosition = Vector2.zero;
        dsvContentRT.sizeDelta = new Vector2(0f, 1200f);
        detailScrollRect.content = dsvContentRT;

        var dsvContentVlg = dsvContentGo.AddComponent<VerticalLayoutGroup>();
        dsvContentVlg.spacing = 20f;
        dsvContentVlg.padding = new RectOffset(20, 20, 20, 20);
        dsvContentVlg.childControlWidth = true;
        dsvContentVlg.childControlHeight = true;
        dsvContentVlg.childForceExpandWidth = true;
        dsvContentVlg.childForceExpandHeight = false;

        var dsvContentCsf = dsvContentGo.AddComponent<ContentSizeFitter>();
        dsvContentCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        dsvContentCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Preview Illustration Box (2D Mannequin Visual)
        var detailPreviewCardGo = CreateUIObject("PreviewCard", dsvContentGo);
        var dpcImg = detailPreviewCardGo.AddComponent<Image>();
        dpcImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-16.png");
        dpcImg.type = Image.Type.Sliced;
        dpcImg.color = WarmWhite;
        var dpcOutline = detailPreviewCardGo.AddComponent<Outline>();
        dpcOutline.effectColor = SoftSand;
        dpcOutline.effectDistance = new Vector2(1f, 1f);
        AddSoftShadow(detailPreviewCardGo, 2f, -2f, 0.06f);
        var dpcLE = detailPreviewCardGo.AddComponent<LayoutElement>();
        dpcLE.preferredHeight = 160f;

        var detailPreviewImgGo = CreateUIObject("PreviewImage", detailPreviewCardGo);
        var detailPreviewImg = detailPreviewImgGo.AddComponent<Image>();
        detailPreviewImg.preserveAspect = true;
        detailPreviewImg.raycastTarget = false;
        StretchRect(detailPreviewImgGo.GetComponent<RectTransform>());
        var dpiRT = detailPreviewImgGo.GetComponent<RectTransform>();
        dpiRT.offsetMin = new Vector2(8f, 8f);
        dpiRT.offsetMax = new Vector2(-8f, -8f);

        // Short Description
        var detailDescGo = CreateUIObject("ShortDescription", dsvContentGo);
        var detailDescText = detailDescGo.AddComponent<TextMeshProUGUI>();
        detailDescText.textWrappingMode = TextWrappingModes.Normal;
        detailDescText.text = "Description";
        detailDescText.fontSize = 12f;
        detailDescText.color = SecondaryText;
        detailDescText.alignment = TextAlignmentOptions.Left;
        if (fonts != null) detailDescText.font = fonts.Medium;

        // Steps Container
        var detailStepsGo = CreateUIObject("StepsContainer", dsvContentGo);
        var dstepsVlg = detailStepsGo.AddComponent<VerticalLayoutGroup>();
        dstepsVlg.spacing = 8f;
        dstepsVlg.childControlWidth = true;
        dstepsVlg.childControlHeight = true;
        dstepsVlg.childForceExpandWidth = true;
        dstepsVlg.childForceExpandHeight = false;
        var dstepsCsf = detailStepsGo.AddComponent<ContentSizeFitter>();
        dstepsCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        dstepsCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Safety tip card (consistent with G06)
        var detailSafetyCardGo = CreateUIObject("SafetyCard", dsvContentGo);
        var dscImg = detailSafetyCardGo.AddComponent<Image>();
        dscImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
        dscImg.type = Image.Type.Sliced;
        dscImg.color = new Color(0.918f, 0.867f, 0.812f, 0.5f); // 50% SoftSand tint
        var dscOutline = detailSafetyCardGo.AddComponent<Outline>();
        dscOutline.effectColor = SoftSand;
        dscOutline.effectDistance = new Vector2(1f, 1f);
        var dscVlg = detailSafetyCardGo.AddComponent<VerticalLayoutGroup>();
        dscVlg.padding = new RectOffset(16, 16, 12, 12);
        dscVlg.spacing = 4f;
        dscVlg.childControlWidth = true;
        dscVlg.childControlHeight = true;
        dscVlg.childForceExpandWidth = true;
        dscVlg.childForceExpandHeight = false;
        var dscCsf = detailSafetyCardGo.AddComponent<ContentSizeFitter>();
        dscCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        dscCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        var dscTitleGo = CreateUIObject("Title", detailSafetyCardGo);
        var dscTitleText = dscTitleGo.AddComponent<TextMeshProUGUI>();
        dscTitleText.text = "TIPS KEAMANAN";
        dscTitleText.fontSize = 10f;
        dscTitleText.fontStyle = FontStyles.Bold;
        dscTitleText.color = ForestGreen;
        if (fonts != null) dscTitleText.font = fonts.Heading;

        var dscTextGo = CreateUIObject("Text", detailSafetyCardGo);
        var detailSafetyText = dscTextGo.AddComponent<TextMeshProUGUI>();
        detailSafetyText.textWrappingMode = TextWrappingModes.Normal;
        detailSafetyText.text = "Tips";
        detailSafetyText.fontSize = 11f;
        detailSafetyText.color = DeepForest;
        if (fonts != null) detailSafetyText.font = fonts.Medium;

        // Mistakes list section
        var dMistakesTitleGo = CreateUIObject("MistakesTitle", dsvContentGo);
        var dMistakesTitle = dMistakesTitleGo.AddComponent<TextMeshProUGUI>();
        dMistakesTitle.text = "HINDARI INI";
        dMistakesTitle.fontSize = 13f;
        dMistakesTitle.fontStyle = FontStyles.Bold;
        dMistakesTitle.color = DeepForest;
        if (fonts != null) dMistakesTitle.font = fonts.Heading;

        var detailMistakesGo = CreateUIObject("MistakesContainer", dsvContentGo);
        var dmistakesVlg = detailMistakesGo.AddComponent<VerticalLayoutGroup>();
        dmistakesVlg.spacing = 8f;
        dmistakesVlg.childControlWidth = true;
        dmistakesVlg.childControlHeight = true;
        dmistakesVlg.childForceExpandWidth = true;
        dmistakesVlg.childForceExpandHeight = false;
        var dmistakesCsf = detailMistakesGo.AddComponent<ContentSizeFitter>();
        dmistakesCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        dmistakesCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Trained muscles section
        var dTrainedTitleGo = CreateUIObject("TrainedTitle", dsvContentGo);
        var dTrainedTitle = dTrainedTitleGo.AddComponent<TextMeshProUGUI>();
        dTrainedTitle.text = "OTOT YANG TERLATIH";
        dTrainedTitle.fontSize = 13f;
        dTrainedTitle.fontStyle = FontStyles.Bold;
        dTrainedTitle.color = DeepForest;
        if (fonts != null) dTrainedTitle.font = fonts.Heading;

        var detailTrainedGo = CreateUIObject("TrainedContainer", dsvContentGo);
        var dtrainedVlg = detailTrainedGo.AddComponent<VerticalLayoutGroup>();
        dtrainedVlg.spacing = 8f;
        dtrainedVlg.childControlWidth = true;
        dtrainedVlg.childControlHeight = true;
        dtrainedVlg.childForceExpandWidth = true;
        dtrainedVlg.childForceExpandHeight = false;
        var dtrainedCsf = detailTrainedGo.AddComponent<ContentSizeFitter>();
        dtrainedCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        dtrainedCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Related movements section (horizontal swiping cards)
        var dRelatedGroupGo = CreateUIObject("RelatedGroup", dsvContentGo);
        var drgVlg = dRelatedGroupGo.AddComponent<VerticalLayoutGroup>();
        drgVlg.spacing = 12f;
        drgVlg.childControlWidth = true;
        drgVlg.childControlHeight = true;
        drgVlg.childForceExpandWidth = true;
        drgVlg.childForceExpandHeight = false;
        var drgCsf = dRelatedGroupGo.AddComponent<ContentSizeFitter>();
        drgCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        drgCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        var dRelatedTitleGo = CreateUIObject("Title", dRelatedGroupGo);
        var dRelatedTitle = dRelatedTitleGo.AddComponent<TextMeshProUGUI>();
        dRelatedTitle.text = "GERAKAN SERUPA";
        dRelatedTitle.fontSize = 13f;
        dRelatedTitle.fontStyle = FontStyles.Bold;
        dRelatedTitle.color = DeepForest;
        if (fonts != null) dRelatedTitle.font = fonts.Heading;

        var dRelatedScrollViewGo = CreateUIObject("RelatedScrollView", dRelatedGroupGo);
        var drelScroll = dRelatedScrollViewGo.AddComponent<ScrollRect>();
        var nestedScrollRouter = dRelatedScrollViewGo.AddComponent<NestedScrollRouter>();
        nestedScrollRouter.SetParentScrollRect(detailScrollRect);
        drelScroll.horizontal = true;
        drelScroll.vertical = false;
        var drelRT = dRelatedScrollViewGo.GetComponent<RectTransform>();
        drelRT.anchorMin = new Vector2(0f, 0.5f);
        drelRT.anchorMax = new Vector2(1f, 0.5f);
        drelRT.pivot = new Vector2(0.5f, 0.5f);
        drelRT.offsetMin = new Vector2(-20f, -90f); // Extend 20px to absolute left screen edge
        drelRT.offsetMax = new Vector2(20f, 90f);   // Extend 20px to absolute right screen edge
        var drelLE = dRelatedScrollViewGo.AddComponent<LayoutElement>();
        drelLE.preferredHeight = 180f;

        var drelViewportGo = CreateUIObject("Viewport", dRelatedScrollViewGo);
        drelViewportGo.AddComponent<RectMask2D>();
        StretchRect(drelViewportGo.GetComponent<RectTransform>());
        drelScroll.viewport = drelViewportGo.GetComponent<RectTransform>();

        var drelContentGo = CreateUIObject("Content", drelViewportGo);
        var drelContentRT = drelContentGo.GetComponent<RectTransform>();
        drelContentRT.anchorMin = new Vector2(0f, 0.5f);
        drelContentRT.anchorMax = new Vector2(0f, 0.5f);
        drelContentRT.pivot = new Vector2(0f, 0.5f);
        drelContentRT.anchoredPosition = Vector2.zero;
        drelContentRT.sizeDelta = new Vector2(800f, 180f);
        drelScroll.content = drelContentRT;

        var dhlg = drelContentGo.AddComponent<HorizontalLayoutGroup>();
        dhlg.padding = new RectOffset(20, 20, 4, 12);
        dhlg.spacing = 12f;
        dhlg.childControlWidth = true;
        dhlg.childControlHeight = true;

        var drelCsf = drelContentGo.AddComponent<ContentSizeFitter>();
        drelCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        drelCsf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        RelatedMovementCardView.ConfigureContainer(drelContentGo.transform);

        // Sticky header sits on top layer casting downward drop shadow over scrolling content
        detailHeaderGo.transform.SetAsLastSibling();

        // ── G09 — CAMERA DENIED ──
        var cameraErrorPanelGo = CreateUIObject("CameraErrorPanel", unsupGo);
        StretchRect(cameraErrorPanelGo.GetComponent<RectTransform>());
        cameraErrorPanelGo.SetActive(false);

        // Full transparent background to show the WarmCream parent background
        var camErrorBgGo = CreateUIObject("Background", cameraErrorPanelGo);
        var camErrorBgImg = camErrorBgGo.AddComponent<Image>();
        camErrorBgImg.color = Color.clear;
        StretchRect(camErrorBgGo.GetComponent<RectTransform>());

        // Transparent container for elements (no card layout)
        var camCardGo = CreateUIObject("CentralCard", cameraErrorPanelGo);
        var camCardImg = camCardGo.AddComponent<Image>();
        camCardImg.color = Color.clear; // Transparent container
        SetCenterPosition(camCardGo.GetComponent<RectTransform>(), 0f, 20f, 300f, 400f);

        // Camera-off icon (rounded square camera icon container)
        var camOffIconGo = CreateUIObject("CamOffIcon", camCardGo);
        var camOffIconImg = camOffIconGo.AddComponent<Image>();
        camOffIconImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
        camOffIconImg.type = Image.Type.Sliced;
        camOffIconImg.color = new Color(0.918f, 0.867f, 0.812f, 0.6f); // 60% SoftSand tint background
        SetCenterPosition(camOffIconGo.GetComponent<RectTransform>(), 0f, 140f, 64f, 64f);

        var camOffIconSvgGo = CreateUIObject("SvgIcon", camOffIconGo);
        var camOffIconSvg = camOffIconSvgGo.AddComponent<Image>();
        camOffIconSvg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Icons/Lucide/camera.svg");
        camOffIconSvg.preserveAspect = true;
        camOffIconSvg.raycastTarget = false;
        camOffIconSvg.color = DeepForest; // #12372A
        var camOffIconSvgRT = camOffIconSvgGo.GetComponent<RectTransform>();
        camOffIconSvgRT.localScale = Vector3.one;
        SetCenterPosition(camOffIconSvgRT, 0f, 0f, 28f, 28f);

        var camErrorTitleGo = CreateUIObject("Title", camCardGo);
        var camErrorTitle = camErrorTitleGo.AddComponent<TextMeshProUGUI>();
        camErrorTitle.textWrappingMode = TextWrappingModes.Normal;
        camErrorTitle.text = "Kamera Belum Aktif";
        camErrorTitle.fontSize = 20f;
        camErrorTitle.fontStyle = FontStyles.Bold;
        camErrorTitle.color = DeepForest; // #12372A
        camErrorTitle.alignment = TextAlignmentOptions.Center;
        if (fonts != null) camErrorTitle.font = fonts.Heading;
        SetCenterPosition(camErrorTitleGo.GetComponent<RectTransform>(), 0f, 70f, 280f, 28f);

        var camErrorDescGo = CreateUIObject("Desc", camCardGo);
        var camErrorDesc = camErrorDescGo.AddComponent<TextMeshProUGUI>();
        camErrorDesc.textWrappingMode = TextWrappingModes.Normal;
        camErrorDesc.text = "Izinkan akses kamera agar MotionLearn dapat melihat gambar gerakan.";
        camErrorDesc.fontSize = 12f;
        camErrorDesc.color = SecondaryText; // #716040
        camErrorDesc.alignment = TextAlignmentOptions.Center;
        if (fonts != null) camErrorDesc.font = fonts.Medium;
        SetCenterPosition(camErrorDescGo.GetComponent<RectTransform>(), 0f, 15f, 260f, 48f);

        // Buka Pengaturan Kamera button (primary button)
        var settingsGo = CreateUIObject("SettingsButton", camCardGo);
        var settingsImg = settingsGo.AddComponent<Image>();
        settingsImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
        settingsImg.type = Image.Type.Sliced;
        settingsImg.color = DeepForest; // Dark DeepForest background button
        var settingsBtn = settingsGo.AddComponent<Button>();
        AddSoftShadow(settingsGo, 2f, -3f, 0.12f); // Shadow on settings button
        SetCenterPosition(settingsGo.GetComponent<RectTransform>(), 0f, -50f, 260f, 48f);

        var settingsContentGo = CreateUIObject("Content", settingsGo);
        SetCenterPosition(settingsContentGo.GetComponent<RectTransform>(), 0f, 0f, 188f, 24f);

        var settingsIconGo = CreateUIObject("Icon", settingsContentGo);
        var settingsIconImg = settingsIconGo.AddComponent<Image>();
        settingsIconImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Icons/Lucide/camera.svg");
        settingsIconImg.preserveAspect = true;
        settingsIconImg.raycastTarget = false;
        settingsIconImg.color = Color.white;
        var sIconRT = settingsIconGo.GetComponent<RectTransform>();
        sIconRT.anchorMin = new Vector2(0f, 0.5f);
        sIconRT.anchorMax = new Vector2(0f, 0.5f);
        sIconRT.pivot = new Vector2(0f, 0.5f);
        sIconRT.anchoredPosition = Vector2.zero;
        sIconRT.sizeDelta = new Vector2(18f, 18f);

        var settingsTextGo = CreateUIObject("Text", settingsContentGo);
        var settingsText = settingsTextGo.AddComponent<TextMeshProUGUI>();
        settingsText.textWrappingMode = TextWrappingModes.NoWrap;
        settingsText.text = "BUKA PENGATURAN KAMERA";
        settingsText.fontSize = 10.5f;
        settingsText.fontStyle = FontStyles.Bold;
        settingsText.color = Color.white;
        settingsText.alignment = TextAlignmentOptions.MidlineLeft;
        if (fonts != null) settingsText.font = fonts.Heading;
        var sTextRT = settingsTextGo.GetComponent<RectTransform>();
        sTextRT.anchorMin = new Vector2(0f, 0f);
        sTextRT.anchorMax = new Vector2(1f, 1f);
        sTextRT.pivot = new Vector2(0f, 0.5f);
        sTextRT.offsetMin = new Vector2(26f, 0f); // 18px icon + 8px gap
        sTextRT.offsetMax = Vector2.zero;

        // Belajar Tanpa Kamera button (secondary button)
        var retryGo = CreateUIObject("RetryButton", camCardGo);
        var retryBtnImg = retryGo.AddComponent<Image>();
        retryBtnImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
        retryBtnImg.type = Image.Type.Sliced;
        retryBtnImg.color = new Color(0.918f, 0.867f, 0.812f, 0.6f); // SoftSand 60% opacity background
        var retryBtn = retryGo.AddComponent<Button>();
        AddSoftShadow(retryGo, 2f, -2f, 0.06f); // Shadow on retry button
        SetCenterPosition(retryGo.GetComponent<RectTransform>(), 0f, -108f, 260f, 48f);

        var retryContentGo = CreateUIObject("Content", retryGo);
        SetCenterPosition(retryContentGo.GetComponent<RectTransform>(), 0f, 0f, 170f, 24f);

        var retryIconGo = CreateUIObject("Icon", retryContentGo);
        var retryIconImg = retryIconGo.AddComponent<Image>();
        retryIconImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Icons/Lucide/book-open.svg");
        retryIconImg.preserveAspect = true;
        retryIconImg.raycastTarget = false;
        retryIconImg.color = DeepForest;
        var rIconRT = retryIconGo.GetComponent<RectTransform>();
        rIconRT.anchorMin = new Vector2(0f, 0.5f);
        rIconRT.anchorMax = new Vector2(0f, 0.5f);
        rIconRT.pivot = new Vector2(0f, 0.5f);
        rIconRT.anchoredPosition = Vector2.zero;
        rIconRT.sizeDelta = new Vector2(18f, 18f);

        var retryTextGo = CreateUIObject("Text", retryContentGo);
        var retryText = retryTextGo.AddComponent<TextMeshProUGUI>();
        retryText.textWrappingMode = TextWrappingModes.NoWrap;
        retryText.text = "BELAJAR TANPA KAMERA";
        retryText.fontSize = 10.5f;
        retryText.fontStyle = FontStyles.Bold;
        retryText.color = DeepForest;
        retryText.alignment = TextAlignmentOptions.MidlineLeft;
        if (fonts != null) retryText.font = fonts.Heading;
        var rTextRT = retryTextGo.GetComponent<RectTransform>();
        rTextRT.anchorMin = new Vector2(0f, 0f);
        rTextRT.anchorMax = new Vector2(1f, 1f);
        rTextRT.pivot = new Vector2(0f, 0.5f);
        rTextRT.offsetMin = new Vector2(26f, 0f); // 18px icon + 8px gap
        rTextRT.offsetMax = Vector2.zero;

        // Bottom helper tip text
        var tipGo = CreateUIObject("HelperTip", camCardGo);
        var tipText = tipGo.AddComponent<TextMeshProUGUI>();
        tipText.textWrappingMode = TextWrappingModes.Normal;
        tipText.text = "Minta bantuan guru atau orang tua jika diperlukan.";
        tipText.fontSize = 10f;
        tipText.color = SecondaryText;
        tipText.alignment = TextAlignmentOptions.Center;
        if (fonts != null) tipText.font = fonts.Medium;
        SetCenterPosition(tipGo.GetComponent<RectTransform>(), 0f, -145f, 280f, 20f);

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
        serialBUI.FindProperty("introStatusText").objectReferenceValue = loadingLabel;
        serialBUI.FindProperty("introLoadingFill").objectReferenceValue = fillImg;
        serialBUI.FindProperty("introCanvasGroup").objectReferenceValue = introCanvasGroup;

        // Serialize new G08 detail fields
        serialBUI.FindProperty("nonARDetailPanel").objectReferenceValue = nonARDetailPanelGo;
        serialBUI.FindProperty("detailCategoryText").objectReferenceValue = detailKickerTxt;
        serialBUI.FindProperty("detailTitleText").objectReferenceValue = detailTitleText;
        serialBUI.FindProperty("detailDescText").objectReferenceValue = detailDescText;
        serialBUI.FindProperty("detailSafetyText").objectReferenceValue = detailSafetyText;
        serialBUI.FindProperty("detailStepsContainer").objectReferenceValue = detailStepsGo.transform;
        serialBUI.FindProperty("detailMistakesContainer").objectReferenceValue = detailMistakesGo.transform;
        serialBUI.FindProperty("detailTrainedContainer").objectReferenceValue = detailTrainedGo.transform;
        serialBUI.FindProperty("detailRelatedContainer").objectReferenceValue = drelContentGo.transform;
        serialBUI.FindProperty("detailPreviewImage").objectReferenceValue = detailPreviewImg;
        serialBUI.FindProperty("detailCloseButton").objectReferenceValue = detailCloseBtn;

        // Prefabs and database
        var stepItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/App/Prefabs/StepItem.prefab");
        var bulletItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/App/Prefabs/BulletItem.prefab");
        var muscleItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/App/Prefabs/MuscleItem.prefab");
        var relatedCardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/App/Prefabs/RelatedCard.prefab");

        serialBUI.FindProperty("stepItemPrefab").objectReferenceValue = stepItemPrefab;
        serialBUI.FindProperty("bulletItemPrefab").objectReferenceValue = bulletItemPrefab;
        serialBUI.FindProperty("muscleItemPrefab").objectReferenceValue = muscleItemPrefab;
        serialBUI.FindProperty("relatedCardPrefab").objectReferenceValue = relatedCardPrefab;
        serialBUI.FindProperty("movementDatabase").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MovementDatabase>("Assets/App/Content/MovementData/MovementDatabase.asset");

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
        serialBtn.FindProperty("infoBadgeButton").objectReferenceValue = g08BadgeGo.GetComponent<Button>();
        serialBtn.FindProperty("warningPanel").objectReferenceValue = warnOverlayGo;
        serialBtn.FindProperty("settingsBtn").objectReferenceValue = settingsBtn;
        serialBtn.FindProperty("retryBtn").objectReferenceValue = retryBtn;
        serialBtn.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, "Assets/App/Scenes/Bootstrap.unity");
        Debug.Log("[MotionLearn] Scene Bootstrap selesai dibuat.");
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
        cam.backgroundColor = Color.black; // hitam, bukan hijau, untuk area di luar video kamera
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
        serialPool.FindProperty("presentationCamera").objectReferenceValue = cam;
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
        var checkIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Icons/Lucide/check.svg");

        // ═══════════════════════════════════════════════════════════
        // G03 — SCANNER (Child of SafeArea)
        // ═══════════════════════════════════════════════════════════
        var scanGo = CreateUIObject("ScanOverlay", canvasStruct.SafeAreaGo);
        var scanBgImg = scanGo.AddComponent<Image>();
        scanBgImg.color = Color.clear; // Clear background to see camera feed clearly
        StretchRect(scanGo.GetComponent<RectTransform>());

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

        // Scan target pill — Transparent gray background with white text
        var scanPillGo = CreateUIObject("ScanTargetPill", scanGo);
        var scanPillImg = scanPillGo.AddComponent<Image>();
        scanPillImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
        scanPillImg.type = Image.Type.Sliced;
        scanPillImg.color = new Color(0.1f, 0.1f, 0.1f, 0.45f); // Transparent gray matching instruction card
        SetCenterPosition(scanPillGo.GetComponent<RectTransform>(), 0f, -100f, 180f, 30f);

        var scanPillTextGo = CreateUIObject("Text", scanPillGo);
        var scanPillText = scanPillTextGo.AddComponent<TextMeshProUGUI>();
        scanPillText.textWrappingMode = TextWrappingModes.Normal;
        scanPillText.text = "PINDAI TARGET GAMBAR";
        scanPillText.fontSize = 11f;
        scanPillText.fontStyle = FontStyles.Bold;
        scanPillText.color = Color.white; // White text for visibility on transparent gray background
        scanPillText.alignment = TextAlignmentOptions.Center;
        if (fonts != null) scanPillText.font = fonts.Heading;
        StretchRect(scanPillTextGo.GetComponent<RectTransform>());

        // Instruction card — Transparent gray background card positioned at the bottom
        var instructionCardGo = CreateUIObject("InstructionCard", scanGo);
        var instCardImg = instructionCardGo.AddComponent<Image>();
        instCardImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
        instCardImg.type = Image.Type.Sliced;
        instCardImg.color = new Color(0.1f, 0.1f, 0.1f, 0.45f); // Transparent gray background
        SetAnchorBottom(instructionCardGo.GetComponent<RectTransform>(), 0f, 80f, 320f, 72f);

        var hintGo = CreateUIObject("HintText", instructionCardGo);
        var hintText = hintGo.AddComponent<TextMeshProUGUI>();
        hintText.textWrappingMode = TextWrappingModes.Normal;
        hintText.text = "Arahkan kamera ke gambar gerakan";
        hintText.fontSize = 13f;
        hintText.fontStyle = FontStyles.Bold;
        hintText.color = Color.white; // White text
        hintText.alignment = TextAlignmentOptions.Center; // Centered text alignment restored
        if (fonts != null) hintText.font = fonts.Heading;
        SetCenterPosition(hintGo.GetComponent<RectTransform>(), 0f, 14f, 280f, 20f);

        var instSubtitleGo = CreateUIObject("InstructionSubtitle", instructionCardGo);
        var instSubtitle = instSubtitleGo.AddComponent<TextMeshProUGUI>();
        instSubtitle.textWrappingMode = TextWrappingModes.Normal;
        instSubtitle.text = "Pastikan seluruh gambar terlihat dengan jelas";
        instSubtitle.fontSize = 10f;
        instSubtitle.color = new Color(0.9f, 0.9f, 0.9f, 1f); // Soft white subtitle text color
        instSubtitle.alignment = TextAlignmentOptions.Center; // Centered subtitle alignment restored
        if (fonts != null) instSubtitle.font = fonts.Medium;
        SetCenterPosition(instSubtitleGo.GetComponent<RectTransform>(), 0f, -14f, 280f, 16f);

        // ═══════════════════════════════════════════════════════════
        // G04 — DETECTION TOAST (Child of CenterContent)
        // ═══════════════════════════════════════════════════════════
        var toastGo = CreateUIObject("DetectionToast", canvasStruct.CenterContentGo);
        var toastImg = toastGo.AddComponent<Image>();
        toastImg.sprite = uiSolidRect; // Flat solid background - no rounded corner warping!
        toastImg.type = Image.Type.Simple;
        toastImg.color = WarmWhite;
        // 1. Full Screen Warm Beige Background View (100% x 100%)
        var toastRT = toastGo.GetComponent<RectTransform>();
        toastRT.anchorMin = new Vector2(0f, 0f);
        toastRT.anchorMax = new Vector2(1f, 1f);
        toastRT.pivot = new Vector2(0.5f, 0.5f);
        toastRT.offsetMin = Vector2.zero;
        toastRT.offsetMax = Vector2.zero;

        var circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/Circle-24.png");

        // Outer Ring 1 (Light Mint Green)
        var toastCircleGo = CreateUIObject("SuccessCircle", toastGo);
        var toastCircleImg = toastCircleGo.AddComponent<Image>();
        toastCircleImg.sprite = circleSprite;
        toastCircleImg.type = Image.Type.Simple;
        toastCircleImg.color = new Color(0.65f, 0.95f, 0.80f, 0.55f);
        SetCenterPosition(toastCircleGo.GetComponent<RectTransform>(), 0f, 50f, 104f, 104f);

        // Middle Ring 2 (Fresh Green)
        var midCircleGo = CreateUIObject("MiddleCircle", toastCircleGo);
        var midCircleImg = midCircleGo.AddComponent<Image>();
        midCircleImg.sprite = circleSprite;
        midCircleImg.type = Image.Type.Simple;
        midCircleImg.color = new Color(0.29f, 0.85f, 0.48f, 0.85f);
        SetCenterPosition(midCircleGo.GetComponent<RectTransform>(), 0f, 0f, 78f, 78f);

        // Center Circle 3 (Vibrant Deep Green)
        var innerCircleGo = CreateUIObject("InnerCircle", midCircleGo);
        var innerCircleImg = innerCircleGo.AddComponent<Image>();
        innerCircleImg.sprite = circleSprite;
        innerCircleImg.type = Image.Type.Simple;
        innerCircleImg.color = new Color(0.09f, 0.65f, 0.28f, 1.0f);
        SetCenterPosition(innerCircleGo.GetComponent<RectTransform>(), 0f, 0f, 54f, 54f);

        var toastCheckGo = CreateUIObject("CheckIcon", innerCircleGo);
        var toastCheck = toastCheckGo.AddComponent<Image>();
        toastCheck.sprite = checkIcon;
        toastCheck.preserveAspect = true;
        toastCheck.raycastTarget = false;
        toastCheck.color = WarmWhite;
        SetCenterPosition(toastCheckGo.GetComponent<RectTransform>(), 0f, 0f, 28f, 28f);

        var toastKickerGo = CreateUIObject("KickerText", toastGo);
        var toastKicker = toastKickerGo.AddComponent<TextMeshProUGUI>();
        toastKicker.textWrappingMode = TextWrappingModes.NoWrap;
        toastKicker.text = "GERAKAN TERDETEKSI";
        toastKicker.fontSize = 12f;
        toastKicker.fontStyle = FontStyles.Bold;
        toastKicker.characterSpacing = 1.5f;
        toastKicker.color = ForestGreen;
        toastKicker.alignment = TextAlignmentOptions.Center;
        if (fonts != null) toastKicker.font = fonts.Heading;
        SetCenterPosition(toastKickerGo.GetComponent<RectTransform>(), 0f, -10f, 300f, 20f);

        var toastTextGo = CreateUIObject("TitleText", toastGo);
        var toastText = toastTextGo.AddComponent<TextMeshProUGUI>();
        toastText.textWrappingMode = TextWrappingModes.Normal;
        toastText.text = "Air Squat";
        toastText.fontSize = 28f;
        toastText.fontStyle = FontStyles.Bold;
        toastText.color = DeepForest;
        toastText.alignment = TextAlignmentOptions.Center;
        if (fonts != null) toastText.font = fonts.Heading;
        SetCenterPosition(toastTextGo.GetComponent<RectTransform>(), 0f, -50f, 320f, 36f);

        var toastPillGo = CreateUIObject("MovementPill", toastGo);
        toastPillGo.SetActive(false);

        toastGo.SetActive(false);

        // ═══════════════════════════════════════════════════════════
        // G05 — TRACKING HUD + SLIDER (SafeArea children)
        // ═══════════════════════════════════════════════════════════
        var arControlsGo = CreateUIObject("ARControls", canvasStruct.SafeAreaGo);
        StretchRect(arControlsGo.GetComponent<RectTransform>());
        arControlsGo.SetActive(false);

        // Shared header remains visible through scanning, confirmation, and tracking.
        var appHeaderGo = CreateUIObject("ARAppHeader", canvasStruct.SafeAreaGo);
        StretchRect(appHeaderGo.GetComponent<RectTransform>());

        var arHeaderTitleGo = CreateUIObject("HeaderTitle", appHeaderGo);
        var arHeaderTitle = arHeaderTitleGo.AddComponent<TextMeshProUGUI>();
        arHeaderTitle.textWrappingMode = TextWrappingModes.Normal;
        arHeaderTitle.text = "MotionLearn";
        arHeaderTitle.fontSize = 20f;
        arHeaderTitle.fontStyle = FontStyles.Bold;
        arHeaderTitle.color = ForestGreen;
        arHeaderTitle.alignment = TextAlignmentOptions.Center;
        if (fonts != null) arHeaderTitle.font = fonts.Display;
        SetAnchorTop(arHeaderTitleGo.GetComponent<RectTransform>(), 0f, -48f, 200f, 28f);

        var arHeaderSubGo = CreateUIObject("HeaderSub", appHeaderGo);
        var arHeaderSub = arHeaderSubGo.AddComponent<TextMeshProUGUI>();
        arHeaderSub.textWrappingMode = TextWrappingModes.Normal;
        arHeaderSub.text = "Belajar Gerak Jadi Seru";
        arHeaderSub.fontSize = 10f;
        arHeaderSub.color = SoftSand;
        arHeaderSub.alignment = TextAlignmentOptions.Center;
        if (fonts != null) arHeaderSub.font = fonts.Medium;
        SetAnchorTop(arHeaderSubGo.GetComponent<RectTransform>(), 0f, -76f, 200f, 16f);
        UIRuntimeStyler.EnsureHeaderContrast(appHeaderGo.transform);

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

        // ── PREVIOUS IMPLEMENTATION (KEPT IN COMMENT FOR REFERENCE) ──
        // tcRT.anchorMin = new Vector2(0f, 0.5f);
        // tcRT.anchorMax = new Vector2(1f, 0.5f);
        // tcRT.pivot = new Vector2(0.5f, 0.5f);
        // tcRT.anchoredPosition = Vector2.zero;
        // tcRT.sizeDelta = new Vector2(-12f, 4f); // Sleek 4px line track spanning 288px (from x=6 to x=294)
        // var circlePill = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/CirclePill-24.png");
        // var trackBgGo = CreateUIObject("TrackBackground", trackContainerGo);
        // var trackBgImg = trackBgGo.AddComponent<Image>();
        // trackBgImg.sprite = circlePill;
        // trackBgImg.type = Image.Type.Sliced;
        // ─────────────────────────────────────────────────────────────

        // NEW IMPLEMENTATION: Rectangular line matched with node size (10px height) + Semi-Circle End Caps aligned to Node boundaries
        tcRT.anchorMin = new Vector2(0f, 0.5f);
        tcRT.anchorMax = new Vector2(1f, 0.5f);
        tcRT.pivot = new Vector2(0.5f, 0.5f);
        tcRT.offsetMin = new Vector2(12f, -5f); // Aligned to Node 0 center (x=12px), height 10px (-5 to +5)
        tcRT.offsetMax = new Vector2(-12f, 5f); // Aligned to Node N-1 center (x=288px), height 10px (-5 to +5)

        var solidRect = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/UISolidRectangle.png");
        var semiCircleLeft = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/SemiCircleLeft-24.png");
        var semiCircleRight = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/SemiCircleRight-24.png");

        // 1. Track Background (Solid slate rectangle + semi-circle end caps)
        var trackBgGo = CreateUIObject("TrackBackground", trackContainerGo);
        var trackBgImg = trackBgGo.AddComponent<Image>();
        trackBgImg.sprite = solidRect;
        trackBgImg.type = Image.Type.Simple;
        trackBgImg.preserveAspect = false;
        trackBgImg.color = new Color(0.88f, 0.91f, 0.94f, 1.0f); // Light slate (#E2E8F0)
        var tbgRT = trackBgGo.GetComponent<RectTransform>();
        tbgRT.anchorMin = new Vector2(0f, 0f);
        tbgRT.anchorMax = new Vector2(1f, 1f);
        tbgRT.pivot = new Vector2(0.5f, 0.5f);
        tbgRT.anchoredPosition = Vector2.zero;
        tbgRT.sizeDelta = Vector2.zero;

        // Left End Cap for TrackBackground (Semi-circle facing left: half covers node 0 end, half protrudes outward)
        var bgLeftCapGo = CreateUIObject("LeftCap", trackBgGo);
        var bgLeftCapImg = bgLeftCapGo.AddComponent<Image>();
        bgLeftCapImg.sprite = semiCircleLeft;
        bgLeftCapImg.type = Image.Type.Simple;
        bgLeftCapImg.preserveAspect = false;
        bgLeftCapImg.color = new Color(0.88f, 0.91f, 0.94f, 1.0f);
        var bglcRT = bgLeftCapGo.GetComponent<RectTransform>();
        bglcRT.anchorMin = new Vector2(0f, 0.5f);
        bglcRT.anchorMax = new Vector2(0f, 0.5f);
        bglcRT.pivot = new Vector2(1f, 0.5f); // Pivot at right flat edge (aligned with node 0 center x=0)
        bglcRT.anchoredPosition = Vector2.zero;
        bglcRT.sizeDelta = new Vector2(5f, 10f); // 5px width (half circle radius), 10px height

        // Right End Cap for TrackBackground (Semi-circle facing right: half covers node N-1 end, half protrudes outward)
        var bgRightCapGo = CreateUIObject("RightCap", trackBgGo);
        var bgRightCapImg = bgRightCapGo.AddComponent<Image>();
        bgRightCapImg.sprite = semiCircleRight;
        bgRightCapImg.type = Image.Type.Simple;
        bgRightCapImg.preserveAspect = false;
        bgRightCapImg.color = new Color(0.88f, 0.91f, 0.94f, 1.0f);
        var bgrcRT = bgRightCapGo.GetComponent<RectTransform>();
        bgrcRT.anchorMin = new Vector2(1f, 0.5f);
        bgrcRT.anchorMax = new Vector2(1f, 0.5f);
        bgrcRT.pivot = new Vector2(0f, 0.5f); // Pivot at left flat edge (aligned with node N-1 center x=L)
        bgrcRT.anchoredPosition = Vector2.zero;
        bgrcRT.sizeDelta = new Vector2(5f, 10f); // 5px width (half circle radius), 10px height

        var fillAreaGo = CreateUIObject("FillArea", trackContainerGo);
        var faRT = fillAreaGo.GetComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0f, 0f);
        faRT.anchorMax = new Vector2(1f, 1f);
        faRT.pivot = new Vector2(0.5f, 0.5f);
        faRT.anchoredPosition = Vector2.zero;
        faRT.sizeDelta = Vector2.zero;

        // 2. Active Fill (Solid Forest Green rectangle + semi-circle left cap)
        var activeFillGo = CreateUIObject("ActiveFill", fillAreaGo);
        var fillImg = activeFillGo.AddComponent<Image>();
        fillImg.sprite = solidRect;
        fillImg.type = Image.Type.Simple;
        fillImg.preserveAspect = false;
        fillImg.color = ForestGreen; // Forest Green active fill (#166534)
        var afRT = activeFillGo.GetComponent<RectTransform>();
        afRT.anchorMin = new Vector2(0f, 0f);
        afRT.anchorMax = new Vector2(0f, 1f);
        afRT.pivot = new Vector2(0f, 0.5f);
        afRT.anchoredPosition = Vector2.zero;
        afRT.sizeDelta = Vector2.zero; // Driven by slider!
        slider.fillRect = afRT;

        // Left End Cap for ActiveFill
        var fillLeftCapGo = CreateUIObject("LeftCap", activeFillGo);
        var fillLeftCapImg = fillLeftCapGo.AddComponent<Image>();
        fillLeftCapImg.sprite = semiCircleLeft;
        fillLeftCapImg.type = Image.Type.Simple;
        fillLeftCapImg.preserveAspect = false;
        fillLeftCapImg.color = ForestGreen;
        var flcRT = fillLeftCapGo.GetComponent<RectTransform>();
        flcRT.anchorMin = new Vector2(0f, 0.5f);
        flcRT.anchorMax = new Vector2(0f, 0.5f);
        flcRT.pivot = new Vector2(1f, 0.5f);
        flcRT.anchoredPosition = Vector2.zero;
        flcRT.sizeDelta = new Vector2(5f, 10f);

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
        hRT.sizeDelta = new Vector2(24f, 0f);
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
        hvRT.sizeDelta = new Vector2(24f, 24f); // 24x24 white circle knob

        // Soft drop shadow to the white circle knob
        var shadow = handleVisGo.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.25f);
        shadow.effectDistance = new Vector2(0f, -2f);

        var nodeContainerGo = CreateUIObject("NodeContainer", sliderGo);
        var ncRT = nodeContainerGo.GetComponent<RectTransform>();
        ncRT.anchorMin = new Vector2(0f, 0f);
        ncRT.anchorMax = new Vector2(1f, 1f);
        ncRT.pivot = new Vector2(0.5f, 0.5f);
        ncRT.offsetMin = new Vector2(12f, 0f); // Center of Node 0 at x=12px (exact center of left 12px end cap)
        ncRT.offsetMax = new Vector2(-12f, 0f); // Center of Node N-1 at x=288px (exact center of right 12px end cap)

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
        SetCenterPosition(startLabelGo.GetComponent<RectTransform>(), -120f, 0f, 60f, 14f);

        var midLabelGo = CreateUIObject("Petunjuk", tlBottomLabelsGo);
        var midLabel = midLabelGo.AddComponent<TextMeshProUGUI>();
        midLabel.textWrappingMode = TextWrappingModes.NoWrap;
        midLabel.text = "Geser untuk memeriksa pose";
        midLabel.fontSize = 11f;
        midLabel.color = SecondaryText;
        midLabel.alignment = TextAlignmentOptions.Center;
        if (fonts != null) midLabel.font = fonts.Medium;
        SetCenterPosition(midLabelGo.GetComponent<RectTransform>(), 0f, 0f, 180f, 14f);

        var endLabelGo = CreateUIObject("Selesai", tlBottomLabelsGo);
        var endLabel = endLabelGo.AddComponent<TextMeshProUGUI>();
        endLabel.textWrappingMode = TextWrappingModes.Normal;
        endLabel.text = "Selesai";
        endLabel.fontSize = 11f;
        endLabel.color = SecondaryText;
        endLabel.alignment = TextAlignmentOptions.Right;
        if (fonts != null) endLabel.font = fonts.Medium;
        SetCenterPosition(endLabelGo.GetComponent<RectTransform>(), 120f, 0f, 60f, 14f);

        var timelineCtrl = timelineRootGo.AddComponent<PoseTimelineController>();
        var serialTimeline = new SerializedObject(timelineCtrl);
        serialTimeline.FindProperty("timelineSlider").objectReferenceValue = slider;
        serialTimeline.FindProperty("movementController").objectReferenceValue = movementController;
        serialTimeline.FindProperty("markerContainer").objectReferenceValue = nodeContainerGo;
        serialTimeline.FindProperty("markerPrefab").objectReferenceValue = markerPrefab;
        serialTimeline.ApplyModifiedProperties();

        // ═══════════════════════════════════════════════════════════
        // G06 — BOTTOM SHEET (Child of SafeArea)
        // ═══════════════════════════════════════════════════════════
        var sheetGo = CreateUIObject("BottomSheet", canvasStruct.SafeAreaGo);
        var sheetImg = sheetGo.AddComponent<Image>();
        sheetImg.sprite = roundTopSprite;
        sheetImg.type = Image.Type.Sliced;
        sheetImg.color = WarmCream; // Consistent WarmCream background
        AddSoftShadow(sheetGo, 0f, 4f, 0.15f); // Upward shadow for sheet depth
        var sheetRT = sheetGo.GetComponent<RectTransform>();
        sheetRT.anchorMin = new Vector2(0f, 0f);
        sheetRT.anchorMax = new Vector2(1f, 0f);
        sheetRT.pivot = new Vector2(0.5f, 1f);
        sheetRT.anchoredPosition = new Vector2(0f, 0f);
        sheetRT.sizeDelta = new Vector2(0f, 752f);

        // Header area — Solid WarmCream background with downward drop shadow separator
        var sheetHeaderGo = CreateUIObject("SheetHeader", sheetGo);
        var sheetHeaderImg = sheetHeaderGo.AddComponent<Image>();
        sheetHeaderImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-16.png");
        sheetHeaderImg.type = Image.Type.Sliced;
        sheetHeaderImg.color = WarmCream; // Hides content scrolling underneath
        AddSoftShadow(sheetHeaderGo, 0f, -4f, 0.15f); // Downward drop shadow on sticky header
        var shRT = sheetHeaderGo.GetComponent<RectTransform>();
        shRT.anchorMin = new Vector2(0f, 1f);
        shRT.anchorMax = new Vector2(1f, 1f);
        shRT.pivot = new Vector2(0.5f, 1f);
        shRT.anchoredPosition = Vector2.zero;
        shRT.sizeDelta = new Vector2(0f, 76f);

        // Grab handle
        var handleGo = CreateUIObject("GrabHandle", sheetGo);
        var handleImg = handleGo.AddComponent<Image>();
        handleImg.sprite = uiSolidRect;
        handleImg.type = Image.Type.Simple;
        handleImg.preserveAspect = false;
        handleImg.color = ForestGreen;
        handleImg.raycastTarget = false;
        var handleRT = handleGo.GetComponent<RectTransform>();
        handleRT.anchorMin = new Vector2(0.5f, 1f);
        handleRT.anchorMax = new Vector2(0.5f, 1f);
        handleRT.pivot = new Vector2(0.5f, 0.5f);
        handleRT.anchoredPosition = new Vector2(0f, -10f);
        handleRT.sizeDelta = new Vector2(40f, 4f);

        // Left text group
        var leftGroupGo = CreateUIObject("LeftGroup", sheetHeaderGo);
        var lgRT = leftGroupGo.GetComponent<RectTransform>();
        lgRT.anchorMin = new Vector2(0f, 0.5f);
        lgRT.anchorMax = new Vector2(0f, 0.5f);
        lgRT.pivot = new Vector2(0f, 0.5f);
        lgRT.anchoredPosition = new Vector2(20f, -4f);
        lgRT.sizeDelta = new Vector2(240f, 52f);

        var leftVlg = leftGroupGo.AddComponent<VerticalLayoutGroup>();
        leftVlg.spacing = 2f;
        leftVlg.childAlignment = TextAnchor.MiddleLeft;
        leftVlg.childControlWidth = true;
        leftVlg.childControlHeight = true;
        leftVlg.childForceExpandWidth = true;
        leftVlg.childForceExpandHeight = false;

        // Kicker label (plain text Category, no badge!)
        var categoryGo = CreateUIObject("CategoryTypeLabel", leftGroupGo);
        var categoryTxt = categoryGo.AddComponent<TextMeshProUGUI>();
        categoryTxt.textWrappingMode = TextWrappingModes.Normal;
        categoryTxt.text = "GERAKAN UTAMA";
        categoryTxt.fontSize = 11f;
        categoryTxt.fontStyle = FontStyles.Bold;
        categoryTxt.color = ForestGreen; // ForestGreen category text
        if (fonts != null) categoryTxt.font = fonts.Heading;
        var kickerLE = categoryGo.AddComponent<LayoutElement>();
        kickerLE.preferredHeight = 14f;

        // Title (SQUAT, all caps)
        var sheetTitleGo = CreateUIObject("MovementTitle", leftGroupGo);
        var sheetTitleText = sheetTitleGo.AddComponent<TextMeshProUGUI>();
        sheetTitleText.textWrappingMode = TextWrappingModes.Normal;
        sheetTitleText.text = "SQUAT";
        sheetTitleText.fontSize = 22f;
        sheetTitleText.fontStyle = FontStyles.Bold;
        sheetTitleText.color = DeepForest; // #12372A
        sheetTitleText.alignment = TextAlignmentOptions.Left;
        if (fonts != null) sheetTitleText.font = fonts.Heading;
        var titleLE = sheetTitleGo.AddComponent<LayoutElement>();
        titleLE.preferredHeight = 26f;

        // Subtitle (not shown in mockup, but kept invisible or very small dummy for controller binding)
        var subtitleGo = CreateUIObject("MovementSubtitle", leftGroupGo);
        var subtitleTxt = subtitleGo.AddComponent<TextMeshProUGUI>();
        subtitleTxt.textWrappingMode = TextWrappingModes.Normal;
        subtitleTxt.text = "";
        subtitleTxt.fontSize = 0.1f;
        subtitleTxt.color = Color.clear;
        var subLE = subtitleGo.AddComponent<LayoutElement>();
        subLE.preferredHeight = 0.1f;

        // Right buttons group
        var rightGroupGo = CreateUIObject("RightGroup", sheetHeaderGo);
        var rgRT = rightGroupGo.GetComponent<RectTransform>();
        rgRT.anchorMin = new Vector2(1f, 0.5f);
        rgRT.anchorMax = new Vector2(1f, 0.5f);
        rgRT.pivot = new Vector2(1f, 0.5f);
        rgRT.anchoredPosition = new Vector2(-20f, 0f);
        rgRT.sizeDelta = new Vector2(44f, 44f);

        // Close X button (rounded square visual 44 size with ForestGreen color)
        var sheetCloseGo = CreateUIObject("SheetCloseX", rightGroupGo);
        var sheetCloseImg = sheetCloseGo.AddComponent<Image>();
        sheetCloseImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png"); // Rounded square!
        sheetCloseImg.type = Image.Type.Sliced;
        sheetCloseImg.color = ForestGreen; // ForestGreen close button background
        var sheetCloseBtn = sheetCloseGo.AddComponent<Button>();
        var closeRT = sheetCloseGo.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(0.5f, 0.5f);
        closeRT.anchorMax = new Vector2(0.5f, 0.5f);
        closeRT.pivot = new Vector2(0.5f, 0.5f);
        closeRT.anchoredPosition = Vector2.zero;
        closeRT.sizeDelta = new Vector2(44f, 44f);

        var xIconGo = CreateUIObject("Icon", sheetCloseGo);
        var xIconImg = xIconGo.AddComponent<Image>();
        xIconImg.sprite = closeIcon;
        xIconImg.preserveAspect = true;
        xIconImg.raycastTarget = false;
        xIconImg.color = Color.white; // White X icon
        SetCenterPosition(xIconGo.GetComponent<RectTransform>(), 0f, 0f, 14f, 14f);
        UIRuntimeStyler.NormalizeCloseButton(sheetCloseBtn);

        // Back to primary button (G07 -> G06) - now at the bottom of the Bottom Sheet (hidden per user request)
        var backBtnGo = CreateUIObject("BackToPrimaryButton", sheetGo);
        var backBtnImg = backBtnGo.AddComponent<Image>();
        backBtnImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
        backBtnImg.type = Image.Type.Sliced;
        backBtnImg.color = ForestGreen; // ForestGreen CTA button background
        var backBtn = backBtnGo.AddComponent<Button>();
        AddSoftShadow(backBtnGo, 2f, -2f, 0.1f); // Shadow on back CTA button
        var backBtnRT = backBtnGo.GetComponent<RectTransform>();
        backBtnRT.anchorMin = new Vector2(0.5f, 0f);
        backBtnRT.anchorMax = new Vector2(0.5f, 0f);
        backBtnRT.pivot = new Vector2(0.5f, 0f);
        backBtnRT.anchoredPosition = new Vector2(0f, 20f);
        backBtnRT.sizeDelta = new Vector2(320f, 48f); // full width CTA
        backBtnGo.SetActive(false);

        var arrowIconGo = CreateUIObject("ArrowIcon", backBtnGo);
        var arrowIconImg = arrowIconGo.AddComponent<Image>();
        arrowIconImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Icons/Lucide/arrow-left.svg");
        arrowIconImg.preserveAspect = true;
        arrowIconImg.raycastTarget = false;
        arrowIconImg.color = WarmWhite; // White arrow icon
        var arrowRT = arrowIconGo.GetComponent<RectTransform>();
        arrowRT.anchorMin = new Vector2(0f, 0.5f);
        arrowRT.anchorMax = new Vector2(0f, 0.5f);
        arrowRT.pivot = new Vector2(0f, 0.5f);
        arrowRT.anchoredPosition = new Vector2(16f, 0f);
        arrowRT.sizeDelta = new Vector2(16f, 16f);

        var backBtnTextGo = CreateUIObject("Text", backBtnGo);
        var backBtnText = backBtnTextGo.AddComponent<TextMeshProUGUI>();
        backBtnText.textWrappingMode = TextWrappingModes.Normal;
        backBtnText.text = "Kembali ke materi";
        backBtnText.fontSize = 13f;
        backBtnText.fontStyle = FontStyles.Bold;
        backBtnText.color = WarmWhite; // White CTA text
        backBtnText.alignment = TextAlignmentOptions.Center;
        if (fonts != null) backBtnText.font = fonts.Heading;
        StretchRect(backBtnTextGo.GetComponent<RectTransform>());
        var btnTxtRT = backBtnTextGo.GetComponent<RectTransform>();
        btnTxtRT.offsetMin = new Vector2(40f, 0f);
        btnTxtRT.offsetMax = new Vector2(-40f, 0f);

        // Bottom sheet scroll view
        var scrollViewGo = CreateUIObject("ScrollView", sheetGo);
        var scrollRect = scrollViewGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.elasticity = 0.1f;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.135f;
        scrollRect.scrollSensitivity = 2f;
        var svRT = scrollViewGo.GetComponent<RectTransform>();
        svRT.anchorMin = new Vector2(0f, 0f);
        svRT.anchorMax = new Vector2(1f, 1f);
        svRT.pivot = new Vector2(0.5f, 0.5f);
        svRT.offsetMin = new Vector2(0f, 0f);    // Full width and bottom to prevent clipping on outer edges
        svRT.offsetMax = new Vector2(0f, -80f);  // Starts 80px below top (below header)

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
        vlg.padding = new RectOffset(20, 20, 0, 24); // Move 20px horizontal margins inside scroll as padding
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
        safetyImg.color = new Color(0.918f, 0.867f, 0.812f, 0.5f); // 50% SoftSand tint background

        var safetyOutline = safetyTipCardGo.AddComponent<Outline>();
        safetyOutline.effectColor = SoftSand;
        safetyOutline.effectDistance = new Vector2(1f, 1f);

        var safetyHlg = safetyTipCardGo.AddComponent<HorizontalLayoutGroup>();
        safetyHlg.padding = new RectOffset(16, 16, 12, 12);
        safetyHlg.spacing = 12f;
        safetyHlg.childAlignment = TextAnchor.MiddleLeft;
        safetyHlg.childControlWidth = true;
        safetyHlg.childControlHeight = true;
        safetyHlg.childForceExpandWidth = false;
        safetyHlg.childForceExpandHeight = false;

        var safetyCsf = safetyTipCardGo.AddComponent<ContentSizeFitter>();
        safetyCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        safetyCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Left Icon Badge (36x36 white rounded square)
        var safetyBadgeGo = CreateUIObject("Badge", safetyTipCardGo);
        var safetyBadgeImg = safetyBadgeGo.AddComponent<Image>();
        safetyBadgeImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-08.png");
        safetyBadgeImg.type = Image.Type.Sliced;
        safetyBadgeImg.color = Color.white;
        var badgeLE = safetyBadgeGo.AddComponent<LayoutElement>();
        badgeLE.minWidth = 36f;
        badgeLE.minHeight = 36f;
        badgeLE.preferredWidth = 36f;
        badgeLE.preferredHeight = 36f;

        var safetyIconGo = CreateUIObject("Icon", safetyBadgeGo);
        var safetyIconImg = safetyIconGo.AddComponent<Image>();
        safetyIconImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Icons/Lucide/shield-check.svg");
        safetyIconImg.preserveAspect = true;
        safetyIconImg.raycastTarget = false;
        safetyIconImg.color = DeepForest;
        SetCenterPosition(safetyIconGo.GetComponent<RectTransform>(), 0f, 0f, 20f, 20f);

        // Right Text Container (Vertical layout group)
        var safetyTextContainerGo = CreateUIObject("TextContainer", safetyTipCardGo);
        var safetyVlg = safetyTextContainerGo.AddComponent<VerticalLayoutGroup>();
        safetyVlg.spacing = 2f;
        safetyVlg.childAlignment = TextAnchor.MiddleLeft;
        safetyVlg.childControlWidth = true;
        safetyVlg.childControlHeight = true;
        safetyVlg.childForceExpandWidth = true;
        safetyVlg.childForceExpandHeight = false;
        var textContainerLE = safetyTextContainerGo.AddComponent<LayoutElement>();
        textContainerLE.flexibleWidth = 1f;

        var safetyTitleGo = CreateUIObject("Title", safetyTextContainerGo);
        var safetyTitle = safetyTitleGo.AddComponent<TextMeshProUGUI>();
        safetyTitle.text = "INGAT, YA!";
        safetyTitle.fontSize = 11f;
        safetyTitle.fontStyle = FontStyles.Bold;
        safetyTitle.color = SecondaryText;
        if (fonts != null) safetyTitle.font = fonts.Heading;

        var safetyTextGo = CreateUIObject("Text", safetyTextContainerGo);
        var safetyText = safetyTextGo.AddComponent<TextMeshProUGUI>();
        safetyText.textWrappingMode = TextWrappingModes.Normal;
        safetyText.text = "Lakukan gerakan perlahan.";
        safetyText.fontSize = 11f;
        safetyText.color = SecondaryText;
        if (fonts != null) safetyText.font = fonts.Body;

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
        var trainedLayout = trainedContainerGo.AddComponent<VerticalLayoutGroup>();
        trainedLayout.spacing = 8f;
        trainedLayout.childAlignment = TextAnchor.UpperLeft;
        trainedLayout.childControlWidth = true;
        trainedLayout.childControlHeight = true;
        trainedLayout.childForceExpandWidth = true;
        trainedLayout.childForceExpandHeight = false;

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
        var relRT = relatedScrollViewGo.GetComponent<RectTransform>();
        relRT.anchorMin = new Vector2(0f, 0.5f);
        relRT.anchorMax = new Vector2(1f, 0.5f);
        relRT.pivot = new Vector2(0.5f, 0.5f);
        relRT.offsetMin = new Vector2(-20f, -90f); // Extend 20px to absolute left sheet edge
        relRT.offsetMax = new Vector2(20f, 90f);   // Extend 20px to absolute right sheet edge
        var relLE = relatedScrollViewGo.AddComponent<LayoutElement>();
        relLE.preferredHeight = 180f;

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
        hlg.padding = new RectOffset(20, 20, 4, 12);
        hlg.spacing = 12f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        var relCsf = relContentGo.AddComponent<ContentSizeFitter>();
        relCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        relCsf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        RelatedMovementCardView.ConfigureContainer(relContentGo.transform);

        // Sticky header sits on top layer casting downward drop shadow over scrolling content
        sheetHeaderGo.transform.SetAsLastSibling();

        // Scrim
        var scrimGo = CreateUIObject("Scrim", canvasStruct.SafeAreaGo);
        var scrimImg = scrimGo.AddComponent<Image>();
        scrimImg.color = new Color(0.07f, 0.216f, 0.165f, 0f);
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
        serialMat.FindProperty("categoryAccentBar").objectReferenceValue = null;
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
        serialUI.FindProperty("appHeader").objectReferenceValue = appHeaderGo;
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
        Debug.Log("[MotionLearn] Scene MainAR selesai dibuat.");
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
        Debug.Log("[MotionLearn] Build Settings scenes updated.");
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

        Debug.Log("[MotionLearn] ARUnityX scene validation passed.");
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

    [MenuItem("Build/Build Current Scenes APK")]
    public static void BuildAPK()
    {
        Debug.Log("[MotionLearn] Menjalankan build Android APK...");
        ConfigurePlayerSettings();

        string targetPath = "Builds/MotionLearn.apk";
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
            Debug.Log($"[MotionLearn] Build berhasil! File APK: {targetPath} ({summary.totalSize} bytes)");
        }
        else
        {
            Debug.LogError($"[MotionLearn] Build gagal dengan status: {summary.result}");
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

        AddSoftShadow(cardGo, 2f, -2f, 0.06f); // Soft shadow for modern depth

        SetCenterPosition(cardGo.GetComponent<RectTransform>(), x, y, 320f, 56f); // 320x56 Card

        var numCircleGo = CreateUIObject("NumCircle", cardGo);
        var numCircleImg = numCircleGo.AddComponent<Image>();
        numCircleImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/Circle-24.png"); // clean circle shape
        numCircleImg.type = Image.Type.Sliced;
        numCircleImg.color = new Color(0.918f, 0.867f, 0.812f, 0.6f); // 60% SoftSand tint background
        SetCenterPosition(numCircleGo.GetComponent<RectTransform>(), -130f, 0f, 32f, 32f);

        var numTextGo = CreateUIObject("Text", numCircleGo);
        var numText = numTextGo.AddComponent<TextMeshProUGUI>();
        numText.text = number;
        numText.fontSize = 13f;
        numText.fontStyle = FontStyles.Bold;
        numText.color = DeepForest; // consistent text color
        numText.alignment = TextAlignmentOptions.Center;
        if (fonts != null) numText.font = fonts.Heading;
        StretchRect(numTextGo.GetComponent<RectTransform>());

        var descGo = CreateUIObject("Desc", cardGo);
        var desc = descGo.AddComponent<TextMeshProUGUI>();
        desc.textWrappingMode = TextWrappingModes.Normal;
        desc.text = description;
        desc.fontSize = 12f;
        desc.color = SecondaryText;
        desc.alignment = TextAlignmentOptions.Left;
        if (fonts != null) desc.font = fonts.Medium;
        SetCenterPosition(descGo.GetComponent<RectTransform>(), 20f, 0f, 240f, 36f);
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

        AddSoftShadow(cardGo, 2f, -2f, 0.06f); // Soft shadow for depth

        SetCenterPosition(cardGo.GetComponent<RectTransform>(), x, y, 320f, 76f); // 320x76

        // Left Preview Image
        var prevGo = CreateUIObject("PreviewImage", cardGo);
        var prevImg = prevGo.AddComponent<Image>();
        prevImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
        prevImg.type = Image.Type.Sliced;
        prevImg.color = new Color(0.918f, 0.867f, 0.812f, 0.4f); // 40% SoftSand background
        SetCenterPosition(prevGo.GetComponent<RectTransform>(), -116f, 0f, 56f, 56f);

        // Try to load mannequin image if available
        Sprite modelSprite = null;
        if (icon == "SQ") modelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Primary/Squat.png");
        else if (icon == "DS") modelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Primary/DynamicStretching.png");
        else if (icon == "LD") modelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Primary/LadderDrill.png");
        
        if (modelSprite != null)
        {
            var innerGo = CreateUIObject("Mannequin", prevGo);
            var innerImg = innerGo.AddComponent<Image>();
            innerImg.sprite = modelSprite;
            innerImg.preserveAspect = true;
            StretchRect(innerGo.GetComponent<RectTransform>());
        }

        // Small badge on top-left of the preview image
        var badgeGo = CreateUIObject("Badge", prevGo);
        var badgeImg = badgeGo.AddComponent<Image>();
        badgeImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-08.png");
        badgeImg.type = Image.Type.Sliced;
        badgeImg.color = new Color(0.443f, 0.376f, 0.251f, 1f); // #716040 (SecondaryText)
        var badgeRT = badgeGo.GetComponent<RectTransform>();
        badgeRT.anchorMin = new Vector2(0f, 1f);
        badgeRT.anchorMax = new Vector2(0f, 1f);
        badgeRT.pivot = new Vector2(0f, 1f);
        badgeRT.anchoredPosition = new Vector2(-4f, 4f);
        badgeRT.sizeDelta = new Vector2(26f, 16f);

        var badgeTxtGo = CreateUIObject("Text", badgeGo);
        var badgeTxt = badgeTxtGo.AddComponent<TextMeshProUGUI>();
        badgeTxt.text = icon;
        badgeTxt.fontSize = 8f;
        badgeTxt.fontStyle = FontStyles.Bold;
        badgeTxt.color = Color.white;
        badgeTxt.alignment = TextAlignmentOptions.Center;
        if (fonts != null) badgeTxt.font = fonts.Heading;
        StretchRect(badgeTxtGo.GetComponent<RectTransform>());

        // Middle Text Area (Title & Subtitle)
        var titleGo = CreateUIObject("TitleText", cardGo);
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.textWrappingMode = TextWrappingModes.Normal;
        titleText.text = title;
        titleText.fontSize = 13f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = DeepForest;
        titleText.alignment = TextAlignmentOptions.Left;
        if (fonts != null) titleText.font = fonts.Display;
        SetCenterPosition(titleGo.GetComponent<RectTransform>(), 10f, 10f, 160f, 20f);

        var subGo = CreateUIObject("SubText", cardGo);
        var subText = subGo.AddComponent<TextMeshProUGUI>();
        subText.textWrappingMode = TextWrappingModes.Normal;
        subText.text = subtitle;
        subText.fontSize = 10f;
        subText.color = SecondaryText;
        subText.alignment = TextAlignmentOptions.Left;
        if (fonts != null) subText.font = fonts.Medium;
        SetCenterPosition(subGo.GetComponent<RectTransform>(), 10f, -12f, 160f, 24f);

        // Right Button
        var bukaGo = CreateUIObject("BukaButton", cardGo);
        var bukaImg = bukaGo.AddComponent<Image>();
        bukaImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
        bukaImg.type = Image.Type.Sliced;
        bukaImg.color = new Color(0.443f, 0.376f, 0.251f, 1f); // #716040 (SecondaryText)
        var bukaBtn = bukaGo.AddComponent<Button>();
        SetCenterPosition(bukaGo.GetComponent<RectTransform>(), 120f, 0f, 36f, 36f);

        var chevronGo = CreateUIObject("Chevron", bukaGo);
        var chevronImg = chevronGo.AddComponent<Image>();
        chevronImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Icons/Lucide/chevron-right.svg");
        chevronImg.preserveAspect = true;
        chevronImg.raycastTarget = false;
        chevronImg.color = Color.white;
        SetCenterPosition(chevronGo.GetComponent<RectTransform>(), 0f, 0f, 16f, 16f);

        return (bukaBtn, cardGo, bukaGo);
    }

    private static GameObject CreateFAB(GameObject parent, string name, Color bgColor, Sprite iconSprite)
    {
        var go = CreateUIObject(name, parent);
        var img = go.AddComponent<Image>();
        img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-24.png");
        img.type = Image.Type.Sliced;
        img.color = bgColor;
        var button = go.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.disabledColor = Color.white;
        colors.colorMultiplier = 1f;
        button.colors = colors;

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
        SetCenterPosition(iconRT, 0f, 0f, 16f, 16f);

        return go;
    }

    private static GameObject CreateCollapsibleWarning(GameObject parent, Sprite btnSprite,
        FontSet fonts, string iconPath)
    {
        var warnGo = CreateUIObject("CollapsibleWarning", parent);
        var warnImg = warnGo.AddComponent<Image>();
        warnImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-12.png");
        warnImg.type = Image.Type.Sliced;
        warnImg.color = WarmWhite; // Solid white background for modal dialog

        var warnOutline = warnGo.AddComponent<Outline>();
        warnOutline.effectColor = SoftSand;
        warnOutline.effectDistance = new Vector2(1f, 1f);

        AddSoftShadow(warnGo, 2f, -3f, 0.12f); // Shadow on popup modal

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
        safeArea.AddComponent<MotionLearn.UI.SafeAreaController>();

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

    private static void AddSoftShadow(GameObject go, float xOffset = 2f, float yOffset = -2f, float opacity = 0.08f)
    {
        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, opacity);
        shadow.effectDistance = new Vector2(xOffset, yOffset);
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
