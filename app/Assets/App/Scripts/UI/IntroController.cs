// ============================================================
// GerakAR – IntroController.cs
// Shows the intro/splash screen for ~1.5-2 seconds then
// transitions to RequestingPermission (or Onboarding check).
// ============================================================
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
    ///
    /// Visual: Warm Cream background, GerakAR text, placeholder cover.
    /// Cover sprite slot (introImage) can be set from Inspector when
    /// the final cover from components/ is imported.
    /// </summary>
    public class IntroController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────

        [Header("Timing")]
        [Tooltip("How long to show the intro before advancing. 1.5 – 2 seconds recommended.")]
        [SerializeField] [Range(1f, 4f)] private float introDuration = 1.8f;

        [Header("UI References")]
        [SerializeField] private CanvasGroup introCanvasGroup;
        [SerializeField] private Image introImage;       // Cover placeholder slot
        [SerializeField] private float fadeOutDuration = 0.35f;

        // ── Unity lifecycle ───────────────────────────────────────────

        private void Start()
        {
            if (introCanvasGroup != null)
                introCanvasGroup.alpha = 1f;

            StartCoroutine(IntroSequence());
        }

        // ── Sequence ──────────────────────────────────────────────────

        private IEnumerator IntroSequence()
        {
            // Show intro for the configured duration
            yield return new WaitForSeconds(introDuration);

            // Fade out
            if (introCanvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < fadeOutDuration)
                {
                    elapsed += Time.deltaTime;
                    introCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
                    yield return null;
                }
                introCanvasGroup.alpha = 0f;
                introCanvasGroup.gameObject.SetActive(false);
            }

            // Hand off to permission / onboarding flow
            AppStateManager.Instance?.TransitionTo(AppState.RequestingPermission);
        }
    }
}
