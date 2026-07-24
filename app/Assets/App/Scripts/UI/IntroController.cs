using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using MotionLearn.Core;

namespace MotionLearn.UI
{
    /// <summary>
    /// Manages the Bootstrap scene intro screen.
    /// Shows for <see cref="introDuration"/> seconds then hands off
    /// to <see cref="OnboardingController"/> or permission flow.
    /// </summary>
    public class IntroController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────

        [Header("Timing")]
        [Tooltip("How long to show the intro before advancing.")]
        [SerializeField] [Range(0.3f, 4f)] private float introDuration = 0.8f;

        [Header("UI References")]
        [SerializeField] private CanvasGroup introCanvasGroup;
        [SerializeField] private Image loadingFillImage; // Progress bar fill reference

        private RectTransform _fillRT;

        // ── Unity lifecycle ───────────────────────────────────────────

        private void Start()
        {
            if (AppStateManager.Instance == null || !AppStateManager.Instance.Is(AppState.Intro))
                return;

            if (introCanvasGroup != null)
                introCanvasGroup.alpha = 1f;

            if (loadingFillImage != null)
            {
                _fillRT = loadingFillImage.GetComponent<RectTransform>();
                if (_fillRT != null)
                {
                    _fillRT.anchorMin = new Vector2(0f, 0f);
                    _fillRT.anchorMax = new Vector2(0f, 1f);
                    _fillRT.pivot = new Vector2(0f, 0.5f);
                    _fillRT.anchoredPosition = Vector2.zero;
                    _fillRT.sizeDelta = Vector2.zero;
                }
            }

            StartCoroutine(IntroSequence());
        }

        // ── Sequence ──────────────────────────────────────────────────

        private IEnumerator IntroSequence()
        {
            // Start camera preparation behind G01 for both first and subsequent
            // launches. G01 remains visible until MainAR reports a usable stream.
            yield return null;
            AppStateManager.Instance?.TransitionTo(AppState.CheckingAR);

            // Bar berjalan dari 0 ke 45% (berhenti di tengah, akan dilanjutkan saat LoadingARScene)
            float elapsed = 0f;
            while (elapsed < introDuration &&
                   (AppStateManager.Instance == null ||
                    !AppStateManager.Instance.Is(AppState.LoadingARScene)))
            {
                elapsed += Time.deltaTime;
                if (_fillRT != null)
                {
                    float t = Mathf.Clamp01(elapsed / introDuration);
                    // SmoothStep: mulai cepat, melambat saat mendekati 45%
                    _fillRT.anchorMax = new Vector2(Mathf.SmoothStep(0f, 0.45f, t), 1f);
                }
                yield return null;
            }

            if (_fillRT != null &&
                (AppStateManager.Instance == null ||
                 !AppStateManager.Instance.Is(AppState.LoadingARScene)))
                _fillRT.anchorMax = new Vector2(0.45f, 1f); // Berhenti di 45%

            // BootstrapUIController owns the handoff after camera readiness:
            // first launch goes to G02, later launches go directly to G03.
        }
    }
}
