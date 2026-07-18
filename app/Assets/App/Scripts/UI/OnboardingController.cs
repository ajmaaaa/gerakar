// ============================================================
// GerakAR – OnboardingController.cs
// Shows the first-use instructions panel ("Sebelum Mulai").
// Uses PlayerPrefs to display only once per installation.
// ============================================================
using UnityEngine;
using UnityEngine.SceneManagement;
using GerakAR.Core;

namespace GerakAR.UI
{
    /// <summary>
    /// Manages the first-time onboarding panel that appears between
    /// the intro and the AR camera screen.
    ///
    /// Storage key: <see cref="OnboardingKey"/> stored in PlayerPrefs.
    /// No personal data is collected; only a boolean flag.
    ///
    /// Flow:
    ///   - If onboarding not done → show panel, wait for MULAI button.
    ///   - If onboarding done → immediately load MainAR scene.
    /// </summary>
    public class OnboardingController : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────

        private const string OnboardingKey = "gerakar.onboarding.completed.v1";
        private const string MainARScene = "MainAR";

        // ── Inspector ─────────────────────────────────────────────────

        [Header("UI References")]
        [SerializeField] private GameObject onboardingPanel;

        // ── Unity lifecycle ───────────────────────────────────────────

        private void Start()
        {
            AppStateManager.OnStateChanged += OnStateChanged;
        }

        private void OnDestroy()
        {
            AppStateManager.OnStateChanged -= OnStateChanged;
        }

        private void OnStateChanged(AppState prev, AppState next)
        {
            if (next != AppState.Onboarding) return;

            if (!PlayerPrefs.HasKey(OnboardingKey) || PlayerPrefs.GetInt(OnboardingKey) == 0)
            {
                ShowOnboarding();
            }
            else
            {
                AppStateManager.Instance?.TransitionTo(AppState.CheckingAR);
            }
        }

        // ── Onboarding panel ──────────────────────────────────────────

        private void ShowOnboarding()
        {
            if (onboardingPanel != null)
                onboardingPanel.SetActive(true);
        }

        /// <summary>
        /// Called by the MULAI button in the onboarding panel (wire in Inspector).
        /// </summary>
        public void OnMulaiPressed()
        {
            PlayerPrefs.SetInt(OnboardingKey, 1);
            PlayerPrefs.Save();

            if (onboardingPanel != null)
                onboardingPanel.SetActive(false);

            AppStateManager.Instance?.TransitionTo(AppState.CheckingAR);
        }
    }
}
