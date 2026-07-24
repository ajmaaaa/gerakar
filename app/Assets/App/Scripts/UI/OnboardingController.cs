// ============================================================
// MotionLearn – OnboardingController.cs
// Shows the first-use instructions panel ("Sebelum Mulai").
// Uses PlayerPrefs to display only once per installation.
// ============================================================
using UnityEngine;
using MotionLearn.Core;

namespace MotionLearn.UI
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

        private const string OnboardingKey = "motionlearn.onboarding.completed.v1";

        public static bool IsCompleted => PlayerPrefs.GetInt(OnboardingKey, 0) == 1;

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

            // Hanya tampilkan sekali seumur instalasi
            if (!IsCompleted)
            {
                ShowOnboarding();
            }
            else
            {
                // Skip jika sudah pernah
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
            if (BootstrapUIController.CameraPreparedForOnboarding &&
                BootstrapUIController.Instance != null)
            {
                BootstrapUIController.Instance.RevealPreparedCamera();
                return;
            }

            MarkCompleted();
            if (onboardingPanel != null)
                onboardingPanel.SetActive(false);
            AppStateManager.Instance?.TransitionTo(AppState.CheckingAR);
        }

        public static void MarkCompleted()
        {
            PlayerPrefs.SetInt(OnboardingKey, 1);
            PlayerPrefs.Save();
        }
    }
}
