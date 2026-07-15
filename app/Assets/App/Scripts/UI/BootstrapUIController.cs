using UnityEngine;
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

            // Onboarding panel is shown in Scanning state (if first run)
            if (onboardingPanel != null)
                onboardingPanel.SetActive(state == AppState.Scanning);

            // Unsupported state houses both G08 (Non-AR Catalog) and G09 (Camera Error)
            if (unsupportedPanel != null)
            {
                bool isUnsupported = state == AppState.Unsupported;
                unsupportedPanel.SetActive(isUnsupported);

                if (isUnsupported)
                {
                    bool isCameraDenied = PermissionController.CameraPermissionDenied;
                    if (nonARModePanel != null)
                        nonARModePanel.SetActive(!isCameraDenied);
                    if (cameraErrorPanel != null)
                        cameraErrorPanel.SetActive(isCameraDenied);
                }
            }
        }
    }
}
