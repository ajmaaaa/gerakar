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

        // ── Unity lifecycle ───────────────────────────────────────────

        private void Start()
        {
            if (introCanvasGroup != null)
                introCanvasGroup.alpha = 1f;

            if (loadingFillImage != null)
                loadingFillImage.fillAmount = 0f;

            StartCoroutine(IntroSequence());
        }

        // ── Sequence ──────────────────────────────────────────────────

        private IEnumerator IntroSequence()
        {
            float elapsed = 0f;
            while (elapsed < introDuration)
            {
                elapsed += Time.deltaTime;
                if (loadingFillImage != null)
                {
                    float t = Mathf.Clamp01(elapsed / introDuration);
                    loadingFillImage.fillAmount = Mathf.SmoothStep(0f, 1f, t);
                }
                yield return null;
            }

            if (loadingFillImage != null)
                loadingFillImage.fillAmount = 1f;

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
