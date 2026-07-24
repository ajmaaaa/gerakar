using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using MotionLearn.UI;

public static class ScreenshotCapturer
{
    [MenuItem("Build/Capture All Screens")]
    public static void CaptureScreens()
    {
        Debug.Log("[ScreenshotCapturer] Generating shape sprites & rebuilding UI scenes...");
        CreateUIShapeSprites.Execute();
        SetupAndBuild.ExecuteSetupOnly();

        var outDir = System.Environment.GetEnvironmentVariable("SCREENSHOT_OUT_DIR");
        if (string.IsNullOrEmpty(outDir))
        {
            outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Screenshots"));
        }
        Directory.CreateDirectory(outDir);
        Debug.Log($"[ScreenshotCapturer] Output directory: {outDir}");

        // Process Bootstrap Scene
        CaptureBootstrap(outDir);

        // Process MainAR Scene
        CaptureMainAR(outDir);

        Debug.Log("[ScreenshotCapturer] Finished capturing all screens!");
    }

    private static void PrintChildren(Transform trans, string prefix)
    {
        foreach (Transform child in trans)
        {
            Debug.Log($"[ScreenshotCapturer] Child: {prefix}/{child.name}");
            PrintChildren(child, prefix + "/" + child.name);
        }
    }

    private static void CaptureBootstrap(string outDir)
    {
        var scenePath = "Assets/App/Scenes/Bootstrap.unity";
        if (!File.Exists(scenePath))
        {
            Debug.LogError($"[ScreenshotCapturer] Scene file not found: {scenePath}");
            return;
        }

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        var canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null)
        {
            Debug.LogError("[ScreenshotCapturer] Canvas GameObject 'Canvas' not found in Bootstrap!");
            return;
        }

        var canvas = canvasGo.GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[ScreenshotCapturer] Canvas component not found on 'Canvas' in Bootstrap!");
            return;
        }

        // Create temporary rendering camera and RT
        var camGo = new GameObject("TempCaptureCamera");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Color;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1000f;

        var rt = new RenderTexture(360, 800, 24);
        cam.targetTexture = rt;

        // Configure Canvas for Camera rendering
        var origMode = canvas.renderMode;
        var origCam = canvas.worldCamera;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;

        // Find full screen background and safe area transforms
        var bgTrans = canvas.transform.Find("FullScreenBackground");
        var safeAreaTrans = canvas.transform.Find("SafeArea");

        if (safeAreaTrans == null)
        {
            Debug.LogError("[ScreenshotCapturer] SafeArea transform not found in Bootstrap!");
            return;
        }

        // Find panels (IntroPanel is under FullScreenBackground, others are under SafeArea)
        var introTrans = bgTrans != null ? bgTrans.Find("IntroPanel") : null;
        var onboardTrans = safeAreaTrans.Find("OnboardingPanel");
        var unsupTrans = safeAreaTrans.Find("UnsupportedPanel");
        
        var intro = introTrans != null ? introTrans.gameObject : null;
        var onboard = onboardTrans != null ? onboardTrans.gameObject : null;
        var unsup = unsupTrans != null ? unsupTrans.gameObject : null;
        
        var cameraError = unsupTrans != null ? unsupTrans.Find("CameraErrorPanel")?.gameObject : null;
        var nonARMode = unsupTrans != null ? unsupTrans.Find("NonARModePanel")?.gameObject : null;
        var nonARDetail = unsupTrans != null ? unsupTrans.Find("NonARDetailPanel")?.gameObject : null;
        var bootstrapUI = Object.FindAnyObjectByType<BootstrapUIController>();
        bootstrapUI?.RefreshNonARPresentation();

        // Helper to reset states
        System.Action deactivateAll = () => {
            if (intro != null) intro.SetActive(false);
            if (onboard != null) onboard.SetActive(false);
            if (unsup != null) unsup.SetActive(false);
            if (cameraError != null) cameraError.SetActive(false);
            if (nonARMode != null) nonARMode.SetActive(false);
            if (nonARDetail != null) nonARDetail.SetActive(false);
        };

        // G01 — Intro
        if (intro != null)
        {
            deactivateAll();
            intro.SetActive(true);
            var fillTrans = introTrans.Find("ProgressContainer/ProgressBar/ActiveFill");
            if (fillTrans != null)
            {
                var rtFill = fillTrans.GetComponent<RectTransform>();
                rtFill.anchorMax = new Vector2(0.4f, 1f);
            }
            SaveRTToPNG(cam, rt, Path.Combine(outDir, "G01_Bootstrap.png"));
        }
        else
        {
            Debug.LogWarning("[ScreenshotCapturer] IntroPanel not found!");
        }

        // G02 — Onboarding
        if (onboard != null)
        {
            deactivateAll();
            onboard.SetActive(true);
            SaveRTToPNG(cam, rt, Path.Combine(outDir, "G02_Onboarding.png"));
        }
        else
        {
            Debug.LogWarning("[ScreenshotCapturer] OnboardingPanel not found!");
        }

        // G08 — Non-AR Mode Panel (Warning Collapsed)
        if (unsup != null && nonARMode != null)
        {
            deactivateAll();
            unsup.SetActive(true);
            nonARMode.SetActive(true);
            SaveRTToPNG(cam, rt, Path.Combine(outDir, "G08_NonAR_Collapsed.png"));
        }
        else
        {
            Debug.LogWarning("[ScreenshotCapturer] G08 panels not found!");
        }

        // G09 — Camera Error
        if (unsup != null && cameraError != null)
        {
            deactivateAll();
            unsup.SetActive(true);
            cameraError.SetActive(true);
            if (bootstrapUI != null)
            {
                var method = typeof(MotionLearn.UI.BootstrapUIController).GetMethod("UpdatePanels", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(bootstrapUI, new object[] { MotionLearn.Core.AppState.CameraDenied });
            }
            SaveRTToPNG(cam, rt, Path.Combine(outDir, "G09_CameraError.png"));
        }
        else
        {
            Debug.LogWarning("[ScreenshotCapturer] G09 panels not found!");
        }

        // G08 Detail — Non-AR Detail View
        if (unsup != null && nonARDetail != null)
        {
            deactivateAll();
            unsup.SetActive(true);
            var bui = bootstrapUI;
            if (bui != null)
            {
                bui.ShowNonARDetail("squat");
                RebuildAndScrollToBottom(nonARDetail);
            }
            else
            {
                nonARDetail.SetActive(true);
            }
            SaveRTToPNG(cam, rt, Path.Combine(outDir, "G08_NonAR_Detail.png"));
            SaveRTRegionToPNG(cam, rt, Path.Combine(outDir, "G08_RelatedCards.png"), new Rect(0f, 0f, 360f, 220f));
        }
        else
        {
            Debug.LogWarning("[ScreenshotCapturer] NonARDetailPanel not found!");
        }

        // Revert
        canvas.renderMode = origMode;
        canvas.worldCamera = origCam;

        // Cleanup
        cam.targetTexture = null;
        Object.DestroyImmediate(camGo);
        rt.Release();
        Object.DestroyImmediate(rt);
    }

    private static void CaptureMainAR(string outDir)
    {
        var scenePath = "Assets/App/Scenes/MainAR.unity";
        if (!File.Exists(scenePath))
        {
            Debug.LogError($"[ScreenshotCapturer] Scene file not found: {scenePath}");
            return;
        }

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var canvasGo = GameObject.Find("UI Canvas");
        if (canvasGo == null)
        {
            Debug.LogError("[ScreenshotCapturer] Canvas GameObject named 'UI Canvas' not found in MainAR!");
            return;
        }

        var canvas = canvasGo.GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[ScreenshotCapturer] Canvas component not found on 'UI Canvas' in MainAR!");
            return;
        }

        // Print hierarchy under UI Canvas
        Debug.Log("[ScreenshotCapturer] Listing hierarchy under UI Canvas:");
        PrintChildren(canvasGo.transform, "");

        // Create temporary rendering camera and RT
        var camGo = new GameObject("TempCaptureCamera");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Color;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1000f;

        var rt = new RenderTexture(360, 800, 24);
        cam.targetTexture = rt;

        var origMode = canvas.renderMode;
        var origCam = canvas.worldCamera;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;

        // Find HUD elements
        var safeAreaTrans = canvas.transform.Find("SafeArea");
        if (safeAreaTrans == null)
        {
            Debug.LogError("[ScreenshotCapturer] SafeArea transform not found in MainAR!");
            return;
        }

        var scanGo = safeAreaTrans.Find("ScanOverlay")?.gameObject;
        var arControlsGo = safeAreaTrans.Find("ARControls")?.gameObject;
        var appHeaderGo = safeAreaTrans.Find("ARAppHeader")?.gameObject;
        var centerContentTrans = safeAreaTrans.Find("CenterContent") ?? canvas.transform.Find("CenterContent");
        var detectionToastGo = centerContentTrans?.Find("DetectionToast")?.gameObject;
        ARUIController.ApplyDetectionChipStyle(detectionToastGo);
        var sheetGo = safeAreaTrans.Find("BottomSheet")?.gameObject;
        var fullBgGo = canvas.transform.Find("FullScreenBackground")?.gameObject;
        var readyCoverGo = canvas.transform.Find("CameraReadyCover")?.gameObject;

        Transform legacyHeaderTitle = null;
        Transform legacyHeaderSubtitle = null;
        Transform successCircle = detectionToastGo?.transform.Find("SuccessCircle");
        if (detectionToastGo != null)
        {
            ARUIController.ApplyDetectionChipStyle(detectionToastGo);
        }

        if (appHeaderGo == null && arControlsGo != null)
        {
            legacyHeaderTitle = arControlsGo.transform.Find("HeaderTitle");
            legacyHeaderSubtitle = arControlsGo.transform.Find("HeaderSub");
            if (legacyHeaderTitle != null || legacyHeaderSubtitle != null)
            {
                appHeaderGo = new GameObject("ARAppHeader", typeof(RectTransform));
                var headerRect = appHeaderGo.GetComponent<RectTransform>();
                headerRect.SetParent(safeAreaTrans, false);
                headerRect.anchorMin = Vector2.zero;
                headerRect.anchorMax = Vector2.one;
                headerRect.offsetMin = Vector2.zero;
                headerRect.offsetMax = Vector2.zero;
                legacyHeaderTitle?.SetParent(headerRect, false);
                legacyHeaderSubtitle?.SetParent(headerRect, false);
            }
        }
        UIRuntimeStyler.EnsureHeaderContrast(appHeaderGo?.transform);

        var sheetController = sheetGo != null ? sheetGo.GetComponent<BottomSheetController>() : null;
        sheetController?.ApplyRuntimeLayout();

        System.Action deactivateAll = () => {
            if (scanGo != null) scanGo.SetActive(false);
            if (appHeaderGo != null) appHeaderGo.SetActive(false);
            if (detectionToastGo != null) detectionToastGo.SetActive(false);
            if (arControlsGo != null) arControlsGo.SetActive(false);
            if (sheetGo != null) sheetGo.SetActive(false);
            if (fullBgGo != null) fullBgGo.SetActive(false);
            if (readyCoverGo != null) readyCoverGo.SetActive(false);
        };

        // G03 — Scan Guide
        if (scanGo != null)
        {
            deactivateAll();
            scanGo.SetActive(true);
            if (appHeaderGo != null) appHeaderGo.SetActive(true);
            SaveRTToPNG(cam, rt, Path.Combine(outDir, "G03_ScanGuide.png"));
        }
        else
        {
            Debug.LogWarning("[ScreenshotCapturer] ScanOverlay not found in MainAR!");
        }

        // G04 — Movement confirmation toast
        if (detectionToastGo != null)
        {
            deactivateAll();
            detectionToastGo.SetActive(true);
            if (appHeaderGo != null)
            {
                appHeaderGo.SetActive(true);
                UIRuntimeStyler.EnsureHeaderContrast(appHeaderGo.transform);
            }

            var titleTrans = detectionToastGo.transform.Find("TitleText");
            if (titleTrans != null)
            {
                var titleTxt = titleTrans.GetComponent<TMPro.TextMeshProUGUI>();
                if (titleTxt != null)
                {
                    titleTxt.text = "Air Squat";
                    titleTxt.color = new Color(0.06f, 0.15f, 0.09f, 1.0f);
                }
            }

            SaveRTToPNG(cam, rt, Path.Combine(outDir, "G04_DetectionToast.png"));
        }
        else
        {
            Debug.LogWarning("[ScreenshotCapturer] DetectionToast not found in MainAR!");
        }

        // G05 — Controls HUD (Timeline active)
        if (arControlsGo != null)
        {
            deactivateAll();
            arControlsGo.SetActive(true);
            if (appHeaderGo != null) appHeaderGo.SetActive(true);

            // Populate timeline with mock key poses
            var timelineCtrl = arControlsGo.GetComponentInChildren<MotionLearn.UI.PoseTimelineController>();
            var slider = arControlsGo.GetComponentInChildren<UnityEngine.UI.Slider>();
            if (slider != null)
            {
                slider.value = 0.5f;
            }

            if (timelineCtrl != null)
            {
                var mockData = ScriptableObject.CreateInstance<MotionLearn.Content.MovementData>();
                mockData.displayName = "Squat";
                mockData.keyPoses = new System.Collections.Generic.List<MotionLearn.Content.KeyPoseData>
                {
                    new MotionLearn.Content.KeyPoseData { normalizedTime = 0.0f, label = "Mulai" },
                    new MotionLearn.Content.KeyPoseData { normalizedTime = 0.2f, label = "Turun" },
                    new MotionLearn.Content.KeyPoseData { normalizedTime = 0.4f, label = "Tahan" },
                    new MotionLearn.Content.KeyPoseData { normalizedTime = 0.6f, label = "Naik" },
                    new MotionLearn.Content.KeyPoseData { normalizedTime = 0.8f, label = "Hampir" },
                    new MotionLearn.Content.KeyPoseData { normalizedTime = 1.0f, label = "Selesai" }
                };
                timelineCtrl.SetMovementData(mockData);
            }

            // Explicitly force layout positions for screenshot correctness
            var fillTrans = arControlsGo.transform.Find("TimelineCard/PoseSlider/TrackContainer/FillArea/ActiveFill");
            if (fillTrans != null)
            {
                var rtFill = fillTrans.GetComponent<RectTransform>();
                rtFill.anchorMin = new Vector2(0f, 0f);
                rtFill.anchorMax = new Vector2(0.5f, 1f);
                rtFill.sizeDelta = Vector2.zero;
            }

            var handleTrans = arControlsGo.transform.Find("TimelineCard/PoseSlider/HandleTouchArea/Handle");
            if (handleTrans != null)
            {
                var rtHandle = handleTrans.GetComponent<RectTransform>();
                rtHandle.anchorMin = new Vector2(0.5f, 0f);
                rtHandle.anchorMax = new Vector2(0.5f, 1f);
                rtHandle.anchoredPosition = Vector2.zero;
            }

            SaveRTToPNG(cam, rt, Path.Combine(outDir, "G05_ControlsHUD.png"));
        }
        else
        {
            Debug.LogWarning("[ScreenshotCapturer] ARControls not found in MainAR!");
        }

        // G06 — Bottom Sheet (Materi) open
        if (sheetGo != null && arControlsGo != null)
        {
            deactivateAll();
            arControlsGo.SetActive(true);
            sheetGo.SetActive(true);
            var sheetRT = sheetGo.GetComponent<RectTransform>();
            sheetRT.anchorMin = new Vector2(0f, 0f);
            sheetRT.anchorMax = new Vector2(1f, 0.94f);
            sheetRT.pivot = new Vector2(0.5f, 1f);
            sheetRT.offsetMin = Vector2.zero;
            sheetRT.offsetMax = Vector2.zero;

            var materialContent = sheetGo.GetComponent<MaterialContentController>();
            var database = AssetDatabase.LoadAssetAtPath<MotionLearn.Content.MovementDatabase>(
                "Assets/App/Content/MovementData/MovementDatabase.asset");
            if (materialContent != null && database != null)
            {
                var squat = database.FindById("squat") ?? database.FindByReferenceImageName("squat_target");
                materialContent.SetMovement(squat);
            }

            SaveRTToPNG(cam, rt, Path.Combine(outDir, "G06_BottomSheet.png"));
            RebuildAndScrollToBottom(sheetGo);
            SaveRTRegionToPNG(cam, rt, Path.Combine(outDir, "G06_RelatedCards.png"), new Rect(0f, 0f, 360f, 220f));
        }
        else
        {
            Debug.LogWarning("[ScreenshotCapturer] BottomSheet or ARControls not found in MainAR!");
        }

        canvas.renderMode = origMode;
        canvas.worldCamera = origCam;

        if (legacyHeaderTitle != null)
            legacyHeaderTitle.SetParent(arControlsGo.transform, false);
        if (legacyHeaderSubtitle != null)
            legacyHeaderSubtitle.SetParent(arControlsGo.transform, false);
        if (legacyHeaderTitle != null || legacyHeaderSubtitle != null)
            Object.DestroyImmediate(appHeaderGo);

        // Cleanup
        cam.targetTexture = null;
        Object.DestroyImmediate(camGo);
        rt.Release();
        Object.DestroyImmediate(rt);
    }

    private static void SaveRTToPNG(Camera cam, RenderTexture rt, string filePath)
    {
        cam.Render();

        var oldActive = RenderTexture.active;
        RenderTexture.active = rt;

        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        RenderTexture.active = oldActive;

        var bytes = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);

        File.WriteAllBytes(filePath, bytes);
        Debug.Log($"[ScreenshotCapturer] Saved screenshot to {filePath}");
    }

    private static void SaveRTRegionToPNG(Camera cam, RenderTexture rt, string filePath, Rect region)
    {
        cam.Render();

        var oldActive = RenderTexture.active;
        RenderTexture.active = rt;

        var tex = new Texture2D(Mathf.RoundToInt(region.width), Mathf.RoundToInt(region.height), TextureFormat.RGB24, false);
        tex.ReadPixels(region, 0, 0);
        tex.Apply();

        RenderTexture.active = oldActive;

        var bytes = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);

        File.WriteAllBytes(filePath, bytes);
        Debug.Log($"[ScreenshotCapturer] Saved screenshot region to {filePath}");
    }

    private static void RebuildAndScrollToBottom(GameObject root)
    {
        if (root == null) return;

        Canvas.ForceUpdateCanvases();
        foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

        foreach (var scroll in root.GetComponentsInChildren<ScrollRect>(true))
        {
            if (scroll.vertical)
                scroll.verticalNormalizedPosition = 0f;
        }
        Canvas.ForceUpdateCanvases();
    }
}
