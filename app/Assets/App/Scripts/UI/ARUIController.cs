// ============================================================
// MotionLearn – ARUIController.cs
// Manages visibility of all AR-screen UI elements based on
// AppState changes: scan overlay, movement label, timeline,
// and floating action buttons.
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using MotionLearn.Core;
using MotionLearn.Content;

namespace MotionLearn.UI
{
    /// <summary>
    /// Listens to <see cref="AppStateManager.OnStateChanged"/> and
    /// <see cref="MotionLearnEvents"/> to show/hide the correct UI elements
    /// for each state. No game logic lives here – only visibility control.
    ///
    /// Wiring (Inspector):
    ///   scanOverlay      → Panel with scan frame + hint text
    ///   arControls       → Group holding label + timeline + FABs
    ///   movementLabel    → TMP text component for movement name
    ///   closeButton      -> close floating action button
    ///   materialButton   -> material floating action button
    /// </summary>
    public class ARUIController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────

        [Header("Scan Overlay")]
        [SerializeField] private GameObject scanOverlay;
        [SerializeField] private GameObject scanLine;

        [Header("Detection Toast (green checkmark)")]
        [SerializeField] private GameObject detectionToast;

        [Header("Shared AR Header (G03-G05)")]
        [SerializeField] private GameObject appHeader;

        [Header("AR Controls (shown when tracking)")]
        [SerializeField] private GameObject arControls;
        [SerializeField] private TextMeshProUGUI movementNameLabel;

        [Header("Floating Action Buttons")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button materialButton;

        [Header("Timeline")]
        [SerializeField] private GameObject timelineRoot;
        [SerializeField] private Button playPauseButton;
        [SerializeField] private Image playPauseIcon;
        [SerializeField] private Sprite playSprite;
        [SerializeField] private Sprite pauseSprite;

        [Header("Background")]
        [SerializeField] private GameObject fullScreenBackground;

        // ── Private ───────────────────────────────────────────────────

        private AppStateManager _stateMgr;

        // ── Unity lifecycle ───────────────────────────────────────────

        private void Start()
        {
            _stateMgr = AppStateManager.Instance;
            EnsureSharedHeader();
            UIRuntimeStyler.EnsureHeaderContrast(appHeader?.transform);
            EnsureDetectionSuccessIcon();
            ApplyDetectionChipStyle(detectionToast);

            AppStateManager.OnStateChanged += OnStateChanged;
            MotionLearnEvents.OnMovementDetected += OnMovementDetected;
            MotionLearnEvents.OnLoopStarted += OnLoopStarted;
            Audio.AudioGuideController.OnAudioAvailabilityChanged += OnAudioAvailabilityChanged;

            // Wire buttons
            closeButton?.onClick.AddListener(OnClosePressed);
            materialButton?.onClick.AddListener(OnMaterialPressed);
            if (playPauseButton != null)
                playPauseButton.onClick.AddListener(OnPlayPausePressed);

            // Jangan langsung ApplyState(Scanning) — MainAR dimuat additive saat Bootstrap masih di atas.
            // Tunggu state aktual dari state machine agar tidak menampilkan scanning overlay sebelum waktunya.
            SetActive(detectionToast, false);
            if (_stateMgr != null)
                ApplyState(_stateMgr.CurrentState);
            else
                SetActive(scanOverlay, false);
        }

        private void OnDestroy()
        {
            AppStateManager.OnStateChanged -= OnStateChanged;
            MotionLearnEvents.OnMovementDetected -= OnMovementDetected;
            MotionLearnEvents.OnLoopStarted -= OnLoopStarted;
            Audio.AudioGuideController.OnAudioAvailabilityChanged -= OnAudioAvailabilityChanged;
        }

        // ── State changes ─────────────────────────────────────────────

        private void OnStateChanged(AppState prev, AppState next) => ApplyState(next);

        private void ApplyState(AppState state)
        {
            bool scanning = state == AppState.Scanning || state == AppState.TrackingLost;
            bool detecting = state == AppState.TargetConfirmed;
            bool tracking = state is AppState.TrackingLoop or AppState.InspectingPose or AppState.NonARMovementPlayer;
            bool arTracking = state is AppState.TrackingLoop or AppState.InspectingPose;
            bool showMaterial = state == AppState.ShowingMaterial;

            if (detecting)
            {
                // In detection scanning phase, show L-brackets and laser line
                SetActive(scanOverlay, true);
                SetActive(scanLine, true);
                SetActive(detectionToast, false);
                StopAllCoroutines();
                StartCoroutine(DetectionUISequence());
            }
            else
            {
                StopAllCoroutines();
                SetActive(scanOverlay, scanning);
                SetActive(scanLine, false);
                SetActive(detectionToast, false);

            }

            SetActive(arControls, tracking || showMaterial);
            SetActive(appHeader, scanning || detecting || arTracking);

            // Timeline: only when tracking, not when material is open
            SetActive(timelineRoot, tracking);

            // FABs: only when tracking or material open
            bool fabsVisible = tracking || showMaterial;
            SetActive(closeButton?.gameObject, fabsVisible);
            SetActive(materialButton?.gameObject, fabsVisible && !showMaterial);
            SetActive(playPauseButton?.gameObject, fabsVisible && !showMaterial);

            if (fullScreenBackground != null)
            {
                bool nonAR = state == AppState.NonARMovementPlayer || (state == AppState.ShowingMaterial && AppStateManager.RunInNonARMode);
                fullScreenBackground.SetActive(nonAR);
            }

            // Synchronize Play/Pause icon state
            UpdatePlayPauseUI();
        }

        private void EnsureSharedHeader()
        {
            if (appHeader != null || arControls == null)
                return;

            Transform controlsTransform = arControls.transform;
            Transform title = controlsTransform.Find("HeaderTitle");
            Transform subtitle = controlsTransform.Find("HeaderSub");
            if (title == null && subtitle == null)
                return;

            var headerObject = new GameObject("ARAppHeader", typeof(RectTransform));
            headerObject.layer = arControls.layer;
            var headerRect = (RectTransform)headerObject.transform;
            headerRect.SetParent(controlsTransform.parent, false);
            headerRect.anchorMin = Vector2.zero;
            headerRect.anchorMax = Vector2.one;
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;
            headerRect.SetSiblingIndex(controlsTransform.GetSiblingIndex());

            title?.SetParent(headerRect, false);
            subtitle?.SetParent(headerRect, false);
            appHeader = headerObject;
        }

        private void EnsureDetectionSuccessIcon()
        {
            Transform successCircle = detectionToast?.transform.Find("SuccessCircle");
            if (successCircle == null || successCircle.Find("CheckIcon") != null)
                return;

            Transform legacyText = successCircle.Find("Text");
            if (legacyText == null)
                return;

            legacyText.gameObject.SetActive(false);
            CreateProceduralCheckIcon(successCircle);
        }

        public static GameObject CreateProceduralCheckIcon(Transform parent)
        {
            var root = new GameObject("CheckIcon", typeof(RectTransform));
            root.layer = parent.gameObject.layer;
            var rootRect = (RectTransform)root.transform;
            rootRect.SetParent(parent, false);
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(24f, 24f);

            CreateCheckStroke(rootRect, "ShortStroke", new Vector2(-4f, -2f), new Vector2(10f, 3f), -45f);
            CreateCheckStroke(rootRect, "LongStroke", new Vector2(3f, 1f), new Vector2(17f, 3f), 45f);
            return root;
        }

        public static void ApplyDetectionChipStyle(GameObject toast)
        {
            if (toast == null) return;

            // 1. Full Screen Warm Beige Background View (100% x 100%)
            RectTransform toastRect = toast.GetComponent<RectTransform>();
            if (toastRect != null)
            {
                toastRect.anchorMin = new Vector2(0f, 0f);
                toastRect.anchorMax = new Vector2(1f, 1f);
                toastRect.pivot = new Vector2(0.5f, 0.5f);
                toastRect.offsetMin = Vector2.zero;
                toastRect.offsetMax = Vector2.zero;
            }

            Image toastBg = toast.GetComponent<Image>();
            if (toastBg != null)
            {
                toastBg.color = new Color(0.98f, 0.97f, 0.95f, 1.0f); // Solid Warm White / Beige (#FAF8F5)
                toastBg.type = Image.Type.Simple;
            }

            Outline toastOutline = toast.GetComponent<Outline>();
            if (toastOutline != null)
            {
                toastOutline.enabled = false;
            }

            // 2. MotionLearn Header Title Text ("MOTIONLEARN") at top
            Transform headerTrans = toast.transform.Find("HeaderTitleText");
            if (headerTrans == null)
            {
                var headerGo = new GameObject("HeaderTitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
                headerGo.layer = toast.layer;
                headerTrans = headerGo.transform;
                headerTrans.SetParent(toast.transform, false);
            }
            TextMeshProUGUI headerTxt = headerTrans.GetComponent<TextMeshProUGUI>();
            if (headerTxt != null)
            {
                var headerRect = headerTrans as RectTransform;
                headerRect.anchorMin = new Vector2(0.5f, 1f);
                headerRect.anchorMax = new Vector2(0.5f, 1f);
                headerRect.pivot = new Vector2(0.5f, 1f);
                headerRect.anchoredPosition = new Vector2(0f, -54f);
                headerRect.sizeDelta = new Vector2(300f, 32f);

                headerTxt.text = "MOTIONLEARN";
                headerTxt.fontSize = 16f;
                headerTxt.fontStyle = FontStyles.Bold;
                headerTxt.characterSpacing = 2f;
                headerTxt.color = new Color(0.06f, 0.15f, 0.09f, 1.0f);
                headerTxt.alignment = TextAlignmentOptions.Center;
            }

            // 3. Centered Checkmark Badge (64px x 64px at center y: 50f)
            Transform circleTrans = toast.transform.Find("SuccessCircle");
            if (circleTrans != null)
            {
                var circleRect = circleTrans as RectTransform;
                circleRect.anchorMin = new Vector2(0.5f, 0.5f);
                circleRect.anchorMax = new Vector2(0.5f, 0.5f);
                circleRect.pivot = new Vector2(0.5f, 0.5f);
                circleRect.anchoredPosition = new Vector2(0f, 50f);
                circleRect.sizeDelta = new Vector2(64f, 64f);

                Image circleImg = circleTrans.GetComponent<Image>();
                if (circleImg != null)
                {
#if UNITY_EDITOR
                    circleImg.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/Circle-24.png");
#endif
                    circleImg.type = Image.Type.Simple;
                    circleImg.color = new Color(0.13f, 0.65f, 0.32f, 1.0f);
                }

                Transform checkIcon = circleTrans.Find("CheckIcon");
                if (checkIcon != null)
                {
                    var checkRect = checkIcon as RectTransform;
                    checkRect.anchoredPosition = Vector2.zero;
                    checkRect.sizeDelta = new Vector2(32f, 32f);
                }
            }

            // 4. Centered Kicker Text ("GERAKAN TERDETEKSI") at y: -10f
            Transform kickerTrans = toast.transform.Find("KickerText");
            if (kickerTrans == null)
            {
                var kickerGo = new GameObject("KickerText", typeof(RectTransform), typeof(TextMeshProUGUI));
                kickerGo.layer = toast.layer;
                kickerTrans = kickerGo.transform;
                kickerTrans.SetParent(toast.transform, false);
            }
            TextMeshProUGUI kickerTxt = kickerTrans.GetComponent<TextMeshProUGUI>();
            if (kickerTxt != null)
            {
                var kickerRect = kickerTrans as RectTransform;
                kickerRect.anchorMin = new Vector2(0.5f, 0.5f);
                kickerRect.anchorMax = new Vector2(0.5f, 0.5f);
                kickerRect.pivot = new Vector2(0.5f, 0.5f);
                kickerRect.anchoredPosition = new Vector2(0f, -10f);
                kickerRect.sizeDelta = new Vector2(300f, 20f);

                kickerTxt.text = "GERAKAN TERDETEKSI";
                kickerTxt.fontSize = 12f;
                kickerTxt.fontStyle = FontStyles.Bold;
                kickerTxt.characterSpacing = 1.5f;
                kickerTxt.color = new Color(0.09f, 0.40f, 0.20f, 1.0f);
                kickerTxt.alignment = TextAlignmentOptions.Center;
                kickerTxt.textWrappingMode = TextWrappingModes.NoWrap;
            }

            // 5. Centered Movement Name Title Text ("Air Squat") at y: -50f
            Transform titleTrans = toast.transform.Find("TitleText");
            if (titleTrans != null)
            {
                var titleRect = titleTrans as RectTransform;
                titleRect.anchorMin = new Vector2(0.5f, 0.5f);
                titleRect.anchorMax = new Vector2(0.5f, 0.5f);
                titleRect.pivot = new Vector2(0.5f, 0.5f);
                titleRect.anchoredPosition = new Vector2(0f, -50f);
                titleRect.sizeDelta = new Vector2(320f, 36f);

                TextMeshProUGUI titleTxt = titleTrans.GetComponent<TextMeshProUGUI>();
                if (titleTxt != null)
                {
                    if (string.IsNullOrEmpty(titleTxt.text) || titleTxt.text == "Gerakan Ditemukan!")
                        titleTxt.text = "Air Squat";
                    titleTxt.fontSize = 28f;
                    titleTxt.fontStyle = FontStyles.Bold;
                    titleTxt.color = new Color(0.06f, 0.15f, 0.09f, 1.0f);
                    titleTxt.alignment = TextAlignmentOptions.Center;
                    titleTxt.overflowMode = TextOverflowModes.Ellipsis;
                }
            }

            // 6. Hide MovementPill / image container
            Transform pillTrans = toast.transform.Find("MovementPill");
            if (pillTrans != null)
            {
                pillTrans.gameObject.SetActive(false);
            }
        }

        private static void CreateCheckStroke(Transform parent, string name, Vector2 position, Vector2 size, float rotation)
        {
            var stroke = new GameObject(name, typeof(RectTransform), typeof(Image));
            stroke.layer = parent.gameObject.layer;
            var rect = (RectTransform)stroke.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            var image = stroke.GetComponent<Image>();
#if UNITY_EDITOR
            image.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/App/UI/Sprites/Shapes/RoundedRect-08.png");
            image.type = Image.Type.Sliced;
#endif
            image.color = Color.white;
            image.raycastTarget = false;
        }

        private System.Collections.IEnumerator DetectionUISequence()
        {
            // Show detection sweep (one-shot from top to bottom)
            SetActive(scanLine, true);
            var laserAnim = scanLine?.GetComponent<LaserLineAnimator>();
            if (laserAnim != null)
                laserAnim.enabled = true;

            // Wait for sweep to complete (~1.0s)
            yield return new WaitForSeconds(1.0f);

            // Hide the scan line and guide frame
            SetActive(scanOverlay, false);
            SetActive(scanLine, false);

            // Show the checkmark pop-up card (G04)
            SetActive(detectionToast, true);
        }

        private void OnMovementDetected(string movementId)
        {
            UpdatePlayPauseUI();
        }

        private void OnLoopStarted(string movementId)
        {
            UpdatePlayPauseUI();
        }

        private void OnAudioAvailabilityChanged(bool available) => UpdatePlayPauseUI();

        // ── Label update ──────────────────────────────────────────────

        /// <summary>
        /// Called by <see cref="ARImageTrackingController"/> (or a bridge)
        /// when a new movement is detected, to update the name label and photo.
        /// </summary>
        public void SetMovementName(string displayName, Sprite thumbnailSprite = null)
        {
            if (movementNameLabel != null)
                movementNameLabel.text = displayName;

            if (detectionToast != null)
            {
                var toastTitle = detectionToast.transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
                if (toastTitle != null && !string.IsNullOrEmpty(displayName))
                {
                    toastTitle.text = displayName;
                }

                if (thumbnailSprite != null)
                {
                    SetDetectionToastThumbnail(thumbnailSprite);
                }
            }
        }

        public void SetDetectionToastThumbnail(Sprite sprite)
        {
            if (detectionToast == null || sprite == null) return;
            Transform photoTrans = detectionToast.transform.Find("MovementPill");
            if (photoTrans != null)
            {
                Image photoImg = photoTrans.GetComponent<Image>();
                if (photoImg != null)
                {
                    photoImg.sprite = sprite;
                    photoImg.color = Color.white; // Direct photo without extra background layer
                    photoImg.preserveAspect = true;
                }
            }
        }

        // ── Button handlers ───────────────────────────────────────────

        private void OnClosePressed()
        {
            if (AppStateManager.RunInNonARMode)
            {
                ActiveMovementContext.Clear();
                _stateMgr?.TransitionTo(AppState.NonARCatalog);
                SceneManager.LoadScene("Bootstrap");
                return;
            }

            _stateMgr?.TransitionTo(AppState.Scanning);
        }

        private void OnMaterialPressed()
        {
            _stateMgr?.TransitionTo(AppState.ShowingMaterial);
            string activeId = ActiveMovementContext.ActiveId ?? string.Empty;
            MotionLearnEvents.RaiseMaterialOpened(activeId);
        }

        private void OnPlayPausePressed()
        {
            if (Audio.AudioGuideController.Instance != null)
            {
                Audio.AudioGuideController.Instance.TogglePlayPause();
                UpdatePlayPauseUI();
            }
        }

        private void UpdatePlayPauseUI()
        {
            var audioController = Audio.AudioGuideController.Instance;
            bool available = audioController != null && audioController.HasAudio;
            if (playPauseButton != null)
            {
                ColorBlock colors = playPauseButton.colors;
                colors.disabledColor = Color.white;
                colors.colorMultiplier = 1f;
                playPauseButton.colors = colors;
                playPauseButton.interactable = available;
            }
            if (playPauseIcon == null) return;

            bool isPlaying = audioController != null && audioController.IsPlaying;
            playPauseIcon.sprite = isPlaying ? pauseSprite : playSprite;
            playPauseIcon.color = available ? Color.white : new Color(1f, 1f, 1f, 0.55f);
        }



        // ── Helpers ───────────────────────────────────────────────────

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active)
                go.SetActive(active);
        }
    }
}
