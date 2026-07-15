using UnityEngine;
using UnityEngine.SceneManagement;
using GerakAR.Core;

namespace GerakAR.UI
{
    /// <summary>
    /// Coordinates UI panel visibility in the Bootstrap scene.
    /// Manages G01, G02, G08 (Non-AR Catalogue), and G09 (Camera Error) screens.
    /// </summary>
    public class BootstrapUIController : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject introPanel;
        [SerializeField] private GameObject onboardingPanel;
        [SerializeField] private GameObject unsupportedPanel; // Parent container
        [SerializeField] private GameObject nonARModePanel;   // G08 view
        [SerializeField] private GameObject cameraErrorPanel;  // G09 view

        private void Start()
        {
            AppStateManager.OnStateChanged += OnStateChanged;
            
            // Set initial state visibility
            UpdatePanels(AppStateManager.Instance != null ? AppStateManager.Instance.CurrentState : AppState.Intro);
        }

        private void OnDestroy()
        {
            AppStateManager.OnStateChanged -= OnStateChanged;
        }

        private void OnStateChanged(AppState prev, AppState next)
        {
            UpdatePanels(next);
        }

        private void UpdatePanels(AppState state)
        {
            if (introPanel != null)
                introPanel.SetActive(state == AppState.Intro);

            if (state == AppState.UnsupportedNotice)
            {
                Invoke(nameof(RouteToNonARCatalog), 1.5f);
            }

            // Onboarding panel is shown in Onboarding state
            if (onboardingPanel != null)
                onboardingPanel.SetActive(state == AppState.Onboarding);

            if (unsupportedPanel != null)
            {
                bool showNonAR = state == AppState.NonARCatalog || state == AppState.UnsupportedNotice;
                bool showCamDenied = state == AppState.CameraDenied;
                
                unsupportedPanel.SetActive(showNonAR || showCamDenied);

                if (nonARModePanel != null)
                    nonARModePanel.SetActive(showNonAR);
                if (cameraErrorPanel != null)
                    cameraErrorPanel.SetActive(showCamDenied);
            }

            if (state == AppState.LoadingARScene)
            {
                SceneManager.LoadSceneAsync("MainAR");
            }
        }

        private void RouteToNonARCatalog()
        {
            if (AppStateManager.Instance != null && AppStateManager.Instance.Is(AppState.UnsupportedNotice))
                AppStateManager.Instance.TransitionTo(AppState.NonARCatalog);
        }
    }
}
