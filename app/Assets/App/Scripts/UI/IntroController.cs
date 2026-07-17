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
                var parentRT = loadingFillImage.transform.parent?.GetComponent<RectTransform>();
                if (parentRT != null)
                    _trackWidth = parentRT.rect.width;
                if (_fillRT != null)
                    _fillRT.sizeDelta = new Vector2(0f, _fillRT.sizeDelta.y);
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
                if (_fillRT != null && _trackWidth > 0f)
                {
                    float t = Mathf.Clamp01(elapsed / introDuration);
                    _fillRT.sizeDelta = new Vector2(Mathf.SmoothStep(0f, _trackWidth, t), _fillRT.sizeDelta.y);
                }
                yield return null;
            }

            if (_fillRT != null && _trackWidth > 0f)
                _fillRT.sizeDelta = new Vector2(_trackWidth, _fillRT.sizeDelta.y);

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
