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
            if (introPanel != null)
                introPanel.SetActive(state == AppState.Intro || state == AppState.LoadingARScene);

            if (state == AppState.UnsupportedNotice || state == AppState.ARInstallFailed)
            {
                Invoke(nameof(RouteToNonARCatalog), 1.5f);
            }

            // Onboarding panel is shown in Onboarding state
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
                // introPanel sudah diaktifkan oleh UpdatePanels di atas
                // introCanvasGroup.alpha = 0 (disisakan oleh IntroController)
                // Langsung jalankan sequence
                StartCoroutine(LoadCameraSequence());
            }
        }

        private System.Collections.IEnumerator LoadCameraSequence()
        {
            // 1. Fade in panel G01 yang SAMA (alpha 0 → 1, tanpa SetActive baru)
            if (introCanvasGroup != null)
            {
                float t = 0f;
                while (t < 0.2f)
                {
                    t += Time.deltaTime;
                    introCanvasGroup.alpha = Mathf.Clamp01(t / 0.2f);
                    yield return null;
                }
                introCanvasGroup.alpha = 1f;
            }

            // 2. Sembunyikan panel Onboarding
            if (onboardingPanel != null) onboardingPanel.SetActive(false);

            // 3. Crossfade HANYA teks label
            if (introStatusText != null)
            {
                Color c = introStatusText.color;
                float t = 0f;
                // Fade out teks lama
                while (t < 0.2f)
                {
                    t += Time.deltaTime;
                    introStatusText.color = new Color(c.r, c.g, c.b, 1f - Mathf.Clamp01(t / 0.2f));
                    yield return null;
                }
                introStatusText.text = "Memuat kamera";
                // Fade in teks baru
                t = 0f;
                while (t < 0.2f)
                {
                    t += Time.deltaTime;
                    introStatusText.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(t / 0.2f));
                    yield return null;
                }
                introStatusText.color = c;
            }

            // 4. Muat MainAR di background, tahan aktivasi sampai bar selesai
            var asyncOp = SceneManager.LoadSceneAsync("MainAR");
            asyncOp.allowSceneActivation = false;

            // 5. Animasi loading bar: proporsional dengan progress aktual + waktu minimum 1.5 detik
            float elapsed = 0f;
            float minTime = 1.5f;
            RectTransform fillRT = introLoadingFill != null
                ? introLoadingFill.GetComponent<RectTransform>() : null;
            float startX = fillRT != null ? fillRT.anchorMax.x : 0f;

            while (!asyncOp.isDone)
            {
                elapsed += Time.deltaTime;
                float loadProg = Mathf.Clamp01(asyncOp.progress / 0.9f);
                float timeProg = Mathf.Clamp01(elapsed / minTime);
                // Gunakan progress yang lebih lambat antara loading actual dan waktu minimum
                float barProg = Mathf.Min(loadProg, timeProg);
                if (fillRT != null)
                    fillRT.anchorMax = new Vector2(Mathf.Lerp(startX, 1f, barProg), 1f);

                if (asyncOp.progress >= 0.9f && elapsed >= minTime)
                {
                    if (fillRT != null) fillRT.anchorMax = new Vector2(1f, 1f);
                    asyncOp.allowSceneActivation = true;
                }
                yield return null;
            }
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
