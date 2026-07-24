using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using MotionLearn.Core;
using MotionLearn.Content;

namespace MotionLearn.UI
{
    /// <summary>
    /// Coordinates UI panel visibility in the Bootstrap scene.
    /// Manages G01, G02, G08 (Non-AR Catalogue), G08_Detail (Non-AR Detail), and G09 (Camera Error) screens.
    /// </summary>
    public class BootstrapUIController : MonoBehaviour
    {
        public static BootstrapUIController Instance { get; private set; }
        public static bool CameraPreparedForOnboarding { get; private set; }

        [Header("UI Panels")]
        [SerializeField] private GameObject introPanel;
        [SerializeField] private GameObject onboardingPanel;
        [SerializeField] private GameObject unsupportedPanel; // Parent container
        [SerializeField] private GameObject nonARModePanel;   // G08 Catalog view
        [SerializeField] private GameObject cameraErrorPanel;  // G09 view

        [Header("G01 Persistent Loading References")]
        [SerializeField] private TextMeshProUGUI introStatusText;
        [SerializeField] private Image introLoadingFill;
        [SerializeField] private CanvasGroup introCanvasGroup;

        [Header("G09 Error Details")]
        [SerializeField] private TextMeshProUGUI errorTitleText;
        [SerializeField] private TextMeshProUGUI errorDescText;
        [SerializeField] private TextMeshProUGUI primaryBtnText;
        [SerializeField] private TextMeshProUGUI secondaryBtnText;

        [Header("G08 Non-AR Detail References")]
        [SerializeField] private GameObject nonARDetailPanel;
        [SerializeField] private TextMeshProUGUI detailCategoryText;
        [SerializeField] private TextMeshProUGUI detailTitleText;
        [SerializeField] private TextMeshProUGUI detailDescText;
        [SerializeField] private TextMeshProUGUI detailSafetyText;
        [SerializeField] private Transform detailStepsContainer;
        [SerializeField] private Transform detailMistakesContainer;
        [SerializeField] private Transform detailTrainedContainer;
        [SerializeField] private Transform detailRelatedContainer;
        [SerializeField] private Image detailPreviewImage;
        [SerializeField] private Button detailCloseButton;

        [Header("Prefabs for Dynamic Lists")]
        [SerializeField] private GameObject stepItemPrefab;
        [SerializeField] private GameObject bulletItemPrefab;
        [SerializeField] private GameObject muscleItemPrefab;
        [SerializeField] private GameObject relatedCardPrefab;

        [Header("Database")]
        [SerializeField] private MovementDatabase movementDatabase;

        private readonly List<GameObject> _nonARSpawnedItems = new();
        private readonly List<EventSystem> _disabledMainAREventSystems = new();
        private bool _onboardingTransitionScheduled;
        private bool _unloadingBootstrap;
        private Canvas _bootstrapCanvas;
        private Image _onboardingEdgeBackground;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSessionState()
        {
            CameraPreparedForOnboarding = false;
        }

        private void Awake()
        {
            Instance = this;
            _bootstrapCanvas = onboardingPanel != null
                ? onboardingPanel.GetComponentInParent<Canvas>()
                : null;
            ConfigureOnboardingEdgeBackground();
            ConfigureNonARScrolling();
            RefreshNonARPresentation();
            ApplyFrameRatePolicy(AppState.Intro);

            if (detailCloseButton != null)
            {
                detailCloseButton.onClick.AddListener(() =>
                {
                    if (nonARDetailPanel != null) nonARDetailPanel.SetActive(false);
                    if (nonARModePanel != null) nonARModePanel.SetActive(true);
                });
            }
        }

        private void Start()
        {
            AppStateManager.OnStateChanged += OnStateChanged;

            AppState state = AppStateManager.Instance != null ? AppStateManager.Instance.CurrentState : AppState.Intro;
            ApplyFrameRatePolicy(state);
            UpdatePanels(state);
        }

        private void OnDestroy()
        {
            AppStateManager.OnStateChanged -= OnStateChanged;
            if (Instance == this) Instance = null;
        }

        private void OnStateChanged(AppState prev, AppState next)
        {
            ApplyFrameRatePolicy(next);
            UpdatePanels(next);
        }

        private void ConfigureNonARScrolling()
        {
            Transform outerScrollTransform = nonARDetailPanel?.transform.Find("ScrollView");
            ScrollRect outerScroll = outerScrollTransform?.GetComponent<ScrollRect>();
            Transform relatedScrollTransform = outerScrollTransform?.Find("Viewport/Content/RelatedGroup/RelatedScrollView");
            if (outerScroll == null || relatedScrollTransform == null)
                return;

            NestedScrollRouter router = relatedScrollTransform.GetComponent<NestedScrollRouter>();
            if (router == null)
                router = relatedScrollTransform.gameObject.AddComponent<NestedScrollRouter>();
            router.SetParentScrollRect(outerScroll);
        }

        private static void ApplyFrameRatePolicy(AppState state)
        {
            bool nonAR = state is AppState.UnsupportedNotice or AppState.ARInstallFailed or
                AppState.NonARCatalog or AppState.NonARMovementPlayer ||
                (state == AppState.ShowingMaterial && AppStateManager.RunInNonARMode);
            int refreshRate = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
            if (refreshRate < 30)
                refreshRate = 60;

            Application.targetFrameRate = HighestRefreshDivisor(refreshRate, nonAR ? 90 : 60);
        }

        private static int HighestRefreshDivisor(int refreshRate, int ceiling)
        {
            for (int candidate = Mathf.Min(refreshRate, ceiling); candidate >= 30; candidate--)
            {
                float ratio = refreshRate / (float)candidate;
                if (Mathf.Abs(ratio - Mathf.Round(ratio)) < 0.01f)
                    return candidate;
            }
            return 30;
        }

        private void UpdatePanels(AppState state)
        {
            if (state == AppState.Scanning && !OnboardingController.IsCompleted)
            {
                if (!_onboardingTransitionScheduled)
                {
                    _onboardingTransitionScheduled = true;
                    StartCoroutine(ShowOnboardingOverPreparedCamera());
                }
                return;
            }

            // Jangan pernah nonaktifkan introPanel selama transisi cepat (Intro → CheckingAR → LoadingARScene)
            // Panel tetap alpha=1 (tidak fadeOut), hanya teks yang crossfade di LoadCameraSequence.
            // Kita hanya nonaktifkan ketika state benar-benar meninggalkan flow loading (misalnya NonAR, CameraDenied, atau sudah Scanning).
            bool showOnboarding = state == AppState.Onboarding && !OnboardingController.IsCompleted;
            bool showNonAR = state == AppState.NonARCatalog || state == AppState.UnsupportedNotice || state == AppState.ARInstallFailed;
            bool showCamDenied = state == AppState.CameraDenied;
            if (introPanel != null)
            {
                bool isIntroFlow = !showOnboarding &&
                                   (state == AppState.Intro || state == AppState.Onboarding ||
                                    state == AppState.CheckingAR || state == AppState.RequestingPermission ||
                                    state == AppState.LoadingARScene);
                introPanel.SetActive(isIntroFlow);
            }

            if (_onboardingEdgeBackground != null)
                _onboardingEdgeBackground.enabled = showOnboarding || showNonAR || showCamDenied;

            if (state == AppState.UnsupportedNotice || state == AppState.ARInstallFailed)
            {
                Invoke(nameof(RouteToNonARCatalog), 1.5f);
            }

            // Onboarding panel — hanya tampil saat Onboarding state (sekali seumur instalasi)
            if (onboardingPanel != null)
                onboardingPanel.SetActive(showOnboarding);

            if (unsupportedPanel != null)
            {
                unsupportedPanel.SetActive(showNonAR || showCamDenied);

                if (nonARModePanel != null)
                {
                    // If showing catalog, keep detail panel closed
                    if (showNonAR)
                    {
                        nonARModePanel.SetActive(true);
                        if (nonARDetailPanel != null) nonARDetailPanel.SetActive(false);
                    }
                    else
                    {
                        nonARModePanel.SetActive(false);
                    }
                }

                if (cameraErrorPanel != null)
                {
                    cameraErrorPanel.SetActive(showCamDenied);
                    if (showCamDenied)
                    {
                        bool permissionDenied = PermissionController.CameraPermissionDenied;
                        
                        if (errorTitleText != null)
                            errorTitleText.text = permissionDenied ? "Kamera Belum Aktif" : "Kamera belum dapat dibuka";
                        
                        if (errorDescText != null)
                            errorDescText.text = permissionDenied 
                                ? "Izinkan akses kamera agar MotionLearn dapat melihat gambar gerakan, atau gunakan mode 3D tanpa kamera."
                                : "Periksa izin kamera, atau gunakan mode tanpa kamera.";
                        
                        if (primaryBtnText != null)
                            primaryBtnText.text = permissionDenied ? "BUKA PENGATURAN" : "Coba Lagi";
                        
                        if (secondaryBtnText != null)
                            secondaryBtnText.text = "Belajar Tanpa Kamera";

                        var btnController = FindAnyObjectByType<BootstrapButtonController>();
                        if (btnController != null)
                        {
                            btnController.ConfigureButtons(permissionDenied);
                        }
                    }
                }
            }

            if (state == AppState.LoadingARScene)
            {
                // introPanel diaktifkan oleh UpdatePanels di atas
                // Langsung jalankan sequence loading kamera
                StartCoroutine(LoadCameraSequence());
            }
            else if (state == AppState.Scanning)
            {
                if (_unloadingBootstrap)
                    return;

                // Kamera sudah siap — fade out Bootstrap canvas lalu unload
                Scene mainArScene = SceneManager.GetSceneByName("MainAR");
                if (mainArScene.IsValid())
                    SceneManager.SetActiveScene(mainArScene);
                if (introCanvasGroup != null)
                    StartCoroutine(FadeOutAndUnloadBootstrap());
            }
        }

        private System.Collections.IEnumerator ShowOnboardingOverPreparedCamera()
        {
            Scene preparedScene = SceneManager.GetSceneByName("MainAR");
            if (preparedScene.IsValid())
                SceneManager.SetActiveScene(preparedScene);

            CameraPreparedForOnboarding = true;
            SetBootstrapCanvasPriority(true);
            SetMainAREventSystemsEnabled(false);

            // Avoid a nested state transition inside the Scanning event. Without
            // this delay, a stale Scanning listener can show G03 after G02.
            yield return null;
            AppStateManager.Instance?.TransitionTo(AppState.Onboarding);
            _onboardingTransitionScheduled = false;
        }

        public void RevealPreparedCamera()
        {
            if (!CameraPreparedForOnboarding || _unloadingBootstrap)
                return;

            _unloadingBootstrap = true;
            OnboardingController.MarkCompleted();
            CameraPreparedForOnboarding = false;
            if (onboardingPanel != null)
                onboardingPanel.SetActive(false);

            // Bootstrap owns input while G02 is visible. Transfer input to MainAR
            // only after fresh frames arrive so MULAI cannot hit G03 below.
            SetBootstrapEventSystemsEnabled(false);
            SetMainAREventSystemsEnabled(true);
            SetBootstrapCanvasPriority(false);
            SetBootstrapRenderingEnabled(false);

            Debug.Log("[BootstrapUIController] Revealing prepared MainAR and unloading Bootstrap.");
            AppStateManager.Instance?.TransitionTo(AppState.Scanning);
            StartCoroutine(UnloadBootstrapAfterPreparedReveal());
        }

        private System.Collections.IEnumerator UnloadBootstrapAfterPreparedReveal()
        {
            yield return null;
            Scene bootstrapScene = gameObject.scene;
            if (bootstrapScene.IsValid() && bootstrapScene.isLoaded)
                SceneManager.UnloadSceneAsync(bootstrapScene);
        }

        private void SetBootstrapCanvasPriority(bool elevated)
        {
            if (_bootstrapCanvas == null)
                return;

            _bootstrapCanvas.overrideSorting = elevated;
            _bootstrapCanvas.sortingOrder = elevated ? 1000 : 0;

            if (onboardingPanel == null)
                return;

            CanvasGroup group = onboardingPanel.GetComponent<CanvasGroup>();
            if (group == null)
                group = onboardingPanel.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            group.interactable = elevated;
            group.blocksRaycasts = elevated;
        }

        private void ConfigureOnboardingEdgeBackground()
        {
            if (_bootstrapCanvas == null)
                return;

            Transform backgroundTransform = _bootstrapCanvas.transform.Find("FullScreenBackground");
            if (backgroundTransform == null)
                return;

            _onboardingEdgeBackground = backgroundTransform.GetComponent<Image>();
            if (_onboardingEdgeBackground == null)
                _onboardingEdgeBackground = backgroundTransform.gameObject.AddComponent<Image>();

            _onboardingEdgeBackground.color = new Color32(244, 240, 230, 255);
            _onboardingEdgeBackground.raycastTarget = false;
            _onboardingEdgeBackground.enabled = false;
        }

        private void SetMainAREventSystemsEnabled(bool enabled)
        {
            Scene mainARScene = SceneManager.GetSceneByName("MainAR");
            if (!mainARScene.IsValid() || !mainARScene.isLoaded)
                return;

            if (enabled)
            {
                foreach (EventSystem eventSystem in _disabledMainAREventSystems)
                {
                    if (eventSystem != null)
                        eventSystem.enabled = true;
                }
                _disabledMainAREventSystems.Clear();
                return;
            }

            _disabledMainAREventSystems.Clear();
            foreach (GameObject root in mainARScene.GetRootGameObjects())
            {
                foreach (EventSystem eventSystem in root.GetComponentsInChildren<EventSystem>(true))
                {
                    if (!eventSystem.enabled)
                        continue;
                    eventSystem.enabled = false;
                    _disabledMainAREventSystems.Add(eventSystem);
                }
            }
        }

        private void SetBootstrapEventSystemsEnabled(bool enabled)
        {
            Scene bootstrapScene = gameObject.scene;
            foreach (GameObject root in bootstrapScene.GetRootGameObjects())
            {
                foreach (EventSystem eventSystem in root.GetComponentsInChildren<EventSystem>(true))
                    eventSystem.enabled = enabled;
            }
        }

        private void SetBootstrapRenderingEnabled(bool enabled)
        {
            if (_bootstrapCanvas != null)
                _bootstrapCanvas.enabled = enabled;

            Scene bootstrapScene = gameObject.scene;
            foreach (GameObject root in bootstrapScene.GetRootGameObjects())
            {
                foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
                    camera.enabled = enabled;
            }
        }

        private System.Collections.IEnumerator LoadCameraSequence()
        {
            if (introCanvasGroup != null) introCanvasGroup.alpha = 1f;
            if (onboardingPanel != null) onboardingPanel.SetActive(false);

            SceneManager.LoadSceneAsync("MainAR", LoadSceneMode.Additive);

            // Bar + text crossfade di coroutine TERPISAH agar halus dan tidak saling ganggu
            StartCoroutine(AnimateLoadingBar());
            yield return StartCoroutine(CrossfadeStatusText());
        }

        private System.Collections.IEnumerator CrossfadeStatusText()
        {
            if (introStatusText == null) yield break;
            Color c = introStatusText.color;
            float duration = 0.08f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                introStatusText.color = new Color(c.r, c.g, c.b, 1f - Mathf.Clamp01(t / duration));
                yield return null;
            }
            introStatusText.text = "Memuat kamera";
            t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                introStatusText.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(t / duration));
                yield return null;
            }
            introStatusText.color = c;
        }

        private System.Collections.IEnumerator AnimateLoadingBar()
        {
            RectTransform fillRT = introLoadingFill?.GetComponent<RectTransform>();
            if (fillRT == null) yield break;
            float startX = fillRT.anchorMax.x;
            float elapsed = 0f;
            float duration = 3.0f;
            while (elapsed < duration &&
                   AppStateManager.Instance != null &&
                   !AppStateManager.Instance.Is(AppState.Scanning))
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                fillRT.anchorMax = new Vector2(Mathf.Lerp(startX, 1f, Mathf.SmoothStep(0f, 1f, t)), 1f);
                yield return null;
            }
            if (fillRT != null) fillRT.anchorMax = new Vector2(1f, 1f);
        }

        private System.Collections.IEnumerator FadeOutAndUnloadBootstrap()
        {
            if (introCanvasGroup != null)
            {
                float elapsed = 0f;
                float duration = 0.3f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    introCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
                    yield return null;
                }
                introCanvasGroup.alpha = 0f;
            }
            SceneManager.UnloadSceneAsync("Bootstrap");
        }

        private void RouteToNonARCatalog()
        {
            if (AppStateManager.Instance != null && 
                (AppStateManager.Instance.Is(AppState.UnsupportedNotice) || AppStateManager.Instance.Is(AppState.ARInstallFailed)))
            {
                AppStateManager.Instance.TransitionTo(AppState.NonARCatalog);
            }
        }

        public void ShowNonARDetail(string movementId)
        {
            if (movementDatabase == null) return;
            var data = movementDatabase.FindById(movementId);
            if (data == null) data = movementDatabase.FindByReferenceImageName(movementId);
            if (data == null)
            {
                // Fallback: search case insensitive or by title match
                foreach (var m in movementDatabase.movements)
                {
                    if (m != null && (m.displayName.ToLower() == movementId.ToLower() || m.movementId.ToLower() == movementId.ToLower()))
                    {
                        data = m;
                        break;
                    }
                }
            }
            if (data == null) return;

            if (nonARModePanel != null) nonARModePanel.SetActive(false);
            if (nonARDetailPanel != null) nonARDetailPanel.SetActive(true);

            // Populate header and basic info
            if (detailTitleText != null) detailTitleText.text = data.displayName.ToUpper();
            if (detailDescText != null) detailDescText.text = data.shortDescription;
            if (detailSafetyText != null && data.safetyTips != null && data.safetyTips.Count > 0)
                detailSafetyText.text = data.safetyTips[0];

            if (detailCategoryText != null)
            {
                string categoryName = "GERAKAN UTAMA";
                if (data.movementId.Contains("squat")) categoryName = "GERAKAN UTAMA";
                else if (data.movementId.Contains("stretch") || data.movementId.Contains("dynamic")) categoryName = "DYNAMIC STRETCHING";
                else if (data.movementId.Contains("ladder")) categoryName = "LADDER DRILL";
                detailCategoryText.text = categoryName;
            }

            // Set preview image illustration
            if (detailPreviewImage != null)
            {
                Sprite modelSprite = data.thumbnail;
                if (modelSprite != null)
                {
                    detailPreviewImage.sprite = modelSprite;
                    detailPreviewImage.gameObject.SetActive(true);
                }
                else
                {
                    // Fallback to thumbnail from related if we have one
                    if (data.relatedMovements != null && data.relatedMovements.Count > 0 && data.relatedMovements[0].thumbnail != null)
                    {
                        detailPreviewImage.sprite = data.relatedMovements[0].thumbnail;
                        detailPreviewImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        detailPreviewImage.gameObject.SetActive(false);
                    }
                }
            }

            // Clear previous items
            foreach (var item in _nonARSpawnedItems)
            {
                if (item != null) Destroy(item);
            }
            _nonARSpawnedItems.Clear();

            // Populate steps
            if (detailStepsContainer != null && stepItemPrefab != null && data.steps != null)
            {
                int count = Mathf.Min(data.steps.Count, 3);
                for (int i = 0; i < count; i++)
                {
                    var item = Instantiate(stepItemPrefab, detailStepsContainer);
                    var textTrans = item.transform.Find("Text");
                    var text = textTrans != null ? textTrans.GetComponent<TextMeshProUGUI>() : null;
                    if (text != null) text.text = data.steps[i];

                    var badgeTrans = item.transform.Find("Badge/Text");
                    var badgeText = badgeTrans != null ? badgeTrans.GetComponent<TextMeshProUGUI>() : null;
                    if (badgeText != null) badgeText.text = (i + 1).ToString();

                    _nonARSpawnedItems.Add(item);
                }
            }

            // Populate mistakes (Hindari Ini)
            if (detailMistakesContainer != null && bulletItemPrefab != null && data.commonMistakes != null)
            {
                foreach (var mistake in data.commonMistakes)
                {
                    var item = Instantiate(bulletItemPrefab, detailMistakesContainer);
                    var textTrans = item.transform.Find("Text");
                    var text = textTrans != null ? textTrans.GetComponent<TextMeshProUGUI>() : null;
                    if (text != null) text.text = mistake;
                    _nonARSpawnedItems.Add(item);
                }
            }

            // Populate muscles (Otot yang Terlatih)
            if (detailTrainedContainer != null && muscleItemPrefab != null && data.trainedAreas != null)
            {
                foreach (var area in data.trainedAreas)
                {
                    var item = Instantiate(muscleItemPrefab, detailTrainedContainer);
                    UIRuntimeStyler.NormalizeMuscleItem(item);
                    var textTrans = item.transform.Find("Text");
                    var text = textTrans != null ? textTrans.GetComponent<TextMeshProUGUI>() : null;
                    if (text != null) text.text = area;
                    _nonARSpawnedItems.Add(item);
                }
            }

            // Populate related cards
            if (detailRelatedContainer != null && relatedCardPrefab != null && data.relatedMovements != null)
            {
                RelatedMovementCardView.ConfigureContainer(detailRelatedContainer);
                foreach (var rel in data.relatedMovements)
                {
                    var item = Instantiate(relatedCardPrefab, detailRelatedContainer);
                    RelatedMovementCardView.Configure(item, rel);

                    var btn = item.GetComponent<Button>();
                    var relCopy = rel;
                    btn?.onClick.AddListener(() => ShowNonARDetail(relCopy.title)); // search by title

                    _nonARSpawnedItems.Add(item);
                }
            }
        }

        private void ConfigureCatalogThumbnails()
        {
            ConfigureCatalogThumbnail("CatalogCatalog/CardSquat/PreviewImage", "squat");
            ConfigureCatalogThumbnail("CatalogCatalog/CardDynamicStretch/PreviewImage", "dynamic_stretch");
            ConfigureCatalogThumbnail("CatalogCatalog/CardLadderDrill/PreviewImage", "ladder_drill");
        }

        public void RefreshNonARPresentation()
        {
            ConfigureCatalogThumbnails();
            UIRuntimeStyler.NormalizeCloseButton(detailCloseButton);
            UIRuntimeStyler.NormalizeMuscleContainer(detailTrainedContainer);
        }

        private void ConfigureCatalogThumbnail(string path, string movementId)
        {
            Transform preview = nonARModePanel?.transform.Find(path);
            MovementData data = movementDatabase?.FindById(movementId);
            if (preview == null || data == null || data.thumbnail == null)
                return;

            Transform visual = preview.Find("Mannequin");
            if (visual == null)
            {
                var visualObject = new GameObject("Mannequin", typeof(RectTransform), typeof(Image));
                visual = visualObject.transform;
                visual.SetParent(preview, false);
                visual.SetAsFirstSibling();
            }

            Image image = visual.GetComponent<Image>();
            image.sprite = data.thumbnail;
            image.preserveAspect = true;
            image.raycastTarget = false;
            RectTransform rect = visual as RectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(4f, 4f);
            rect.offsetMax = new Vector2(-4f, -4f);
        }
    }
}
