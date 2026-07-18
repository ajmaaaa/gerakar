using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using GerakAR.Core;
using GerakAR.Content;

namespace GerakAR.UI
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSessionState()
        {
            CameraPreparedForOnboarding = false;
        }

        private void Awake()
        {
            Instance = this;

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
            
            // Set initial state visibility
            UpdatePanels(AppStateManager.Instance != null ? AppStateManager.Instance.CurrentState : AppState.Intro);
        }

        private void OnDestroy()
        {
            AppStateManager.OnStateChanged -= OnStateChanged;
            if (Instance == this) Instance = null;
        }

        private void OnStateChanged(AppState prev, AppState next)
        {
            UpdatePanels(next);
        }

        private void UpdatePanels(AppState state)
        {
            if (state == AppState.Scanning && !OnboardingController.IsCompleted)
            {
                // The first-run camera is fully ready behind Bootstrap. Show G02
                // now so MULAI only dismisses onboarding and reveals G03.
                Scene preparedScene = SceneManager.GetSceneByName("MainAR");
                if (preparedScene.IsValid())
                    SceneManager.SetActiveScene(preparedScene);
                CameraPreparedForOnboarding = true;
                AppStateManager.Instance?.TransitionTo(AppState.Onboarding);
                return;
            }

            // Jangan pernah nonaktifkan introPanel selama transisi cepat (Intro → CheckingAR → LoadingARScene)
            // Panel tetap alpha=1 (tidak fadeOut), hanya teks yang crossfade di LoadCameraSequence.
            // Kita hanya nonaktifkan ketika state benar-benar meninggalkan flow loading (misalnya NonAR, CameraDenied, atau sudah Scanning).
            if (introPanel != null)
            {
                bool isIntroFlow = state == AppState.Intro || state == AppState.Onboarding ||
                                   state == AppState.CheckingAR || state == AppState.RequestingPermission ||
                                   state == AppState.LoadingARScene;
                introPanel.SetActive(isIntroFlow);
            }

            if (state == AppState.UnsupportedNotice || state == AppState.ARInstallFailed)
            {
                Invoke(nameof(RouteToNonARCatalog), 1.5f);
            }

            // Onboarding panel — hanya tampil saat Onboarding state (sekali seumur instalasi)
            if (onboardingPanel != null)
                onboardingPanel.SetActive(state == AppState.Onboarding);

            if (unsupportedPanel != null)
            {
                bool showNonAR = state == AppState.NonARCatalog || state == AppState.UnsupportedNotice || state == AppState.ARInstallFailed;
                bool showCamDenied = state == AppState.CameraDenied;
                
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
                                ? "Izinkan akses kamera agar GerakAR dapat melihat gambar gerakan."
                                : "Periksa izin kamera, atau gunakan mode tanpa kamera.";
                        
                        if (primaryBtnText != null)
                            primaryBtnText.text = permissionDenied ? "BUKA PENGATURAN" : "Coba Lagi";
                        
                        if (secondaryBtnText != null)
                            secondaryBtnText.text = permissionDenied ? "Coba Lagi" : "Belajar Tanpa Kamera";

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
                // Kamera sudah siap — fade out Bootstrap canvas lalu unload
                Scene mainArScene = SceneManager.GetSceneByName("MainAR");
                if (mainArScene.IsValid())
                    SceneManager.SetActiveScene(mainArScene);
                if (introCanvasGroup != null)
                    StartCoroutine(FadeOutAndUnloadBootstrap());
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
                Sprite modelSprite = null;
#if UNITY_EDITOR
                if (data.movementId.Contains("squat")) modelSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Related/squat.png");
                else if (data.movementId.Contains("stretch") || data.movementId.Contains("dynamic")) modelSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Related/dynamic_stretch.png");
                else if (data.movementId.Contains("ladder")) modelSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Related/ladder_drill.png");
#endif
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
                    var textTrans = item.transform.Find("Text");
                    var text = textTrans != null ? textTrans.GetComponent<TextMeshProUGUI>() : null;
                    if (text != null) text.text = area;
                    _nonARSpawnedItems.Add(item);
                }
            }

            // Populate related cards
            if (detailRelatedContainer != null && relatedCardPrefab != null && data.relatedMovements != null)
            {
                foreach (var rel in data.relatedMovements)
                {
                    var item = Instantiate(relatedCardPrefab, detailRelatedContainer);
                    var thumbImg = item.transform.Find("Thumbnail")?.GetComponent<Image>();
                    if (thumbImg != null && rel.thumbnail != null) thumbImg.sprite = rel.thumbnail;

                    var titleTmp = item.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
                    if (titleTmp != null) titleTmp.text = rel.title;

                    var btn = item.GetComponent<Button>();
                    var relCopy = rel;
                    btn?.onClick.AddListener(() => ShowNonARDetail(relCopy.title)); // search by title

                    _nonARSpawnedItems.Add(item);
                }
            }
        }
    }
}
