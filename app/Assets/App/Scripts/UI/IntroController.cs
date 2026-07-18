using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using GerakAR.Core;

namespace GerakAR.UI
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
        [SerializeField] [Range(1f, 4f)] private float introDuration = 1.8f;

        [Header("UI References")]
        [SerializeField] private CanvasGroup introCanvasGroup;
        [SerializeField] private Image loadingFillImage; // Progress bar fill reference
        [SerializeField] private float fadeOutDuration = 0.35f;

        private RectTransform _fillRT;
        private float _trackWidth;

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
            float elapsed = 0f;
            while (elapsed < introDuration)
            {
                elapsed += Time.deltaTime;
                if (_fillRT != null)
                {
                    float t = Mathf.Clamp01(elapsed / introDuration);
                    _fillRT.anchorMax = new Vector2(Mathf.SmoothStep(0f, 1f, t), 1f);
                }
                yield return null;
            }

            if (_fillRT != null)
                _fillRT.anchorMax = new Vector2(1f, 1f);

            // Fade out
            if (introCanvasGroup != null)
            {
                float fadeElapsed = 0f;
                while (fadeElapsed < fadeOutDuration)
                {
                    fadeElapsed += Time.deltaTime;
                    introCanvasGroup.alpha = 1f - Mathf.Clamp01(fadeElapsed / fadeOutDuration);
                    yield return null;
                }
                introCanvasGroup.alpha = 0f;
                introCanvasGroup.gameObject.SetActive(false);
            }

            // Hand off to onboarding flow
            AppStateManager.Instance?.TransitionTo(AppState.Onboarding);
        }
    }
}
