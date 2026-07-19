using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MoveMotion.Core;

namespace MoveMotion.UI
{
    /// <summary>
    /// Handles wiring and click logic for Bootstrap scene fallback and simulation buttons (G02, G08, G09).
    /// </summary>
    public class BootstrapButtonController : MonoBehaviour
    {
        [Header("G02 Onboarding Fallback Buttons")]
        [SerializeField] private Button nonARModeLink;
        [SerializeField] private Button cameraErrorLink;

        [Header("G08 Non-AR Catalogue Buttons")]
        [SerializeField] private Button squatBukaBtn;
        [SerializeField] private Button dynamicStretchBukaBtn;
        [SerializeField] private Button ladderDrillBukaBtn;
        [SerializeField] private Button catalogBackBtn;
        [SerializeField] private Button infoBadgeButton;
        [SerializeField] private GameObject warningPanel;

        [Header("G09 Camera Error Buttons")]
        [SerializeField] private Button settingsBtn;
        [SerializeField] private Button retryBtn;

        private void Start()
        {
            ConfigureButtons(PermissionController.CameraPermissionDenied);
        }

        public void ConfigureButtons(bool permissionDenied)
        {
            nonARModeLink?.onClick.RemoveAllListeners();
            cameraErrorLink?.onClick.RemoveAllListeners();
            squatBukaBtn?.onClick.RemoveAllListeners();
            dynamicStretchBukaBtn?.onClick.RemoveAllListeners();
            ladderDrillBukaBtn?.onClick.RemoveAllListeners();
            catalogBackBtn?.onClick.RemoveAllListeners();
            infoBadgeButton?.onClick.RemoveAllListeners();
            settingsBtn?.onClick.RemoveAllListeners();
            retryBtn?.onClick.RemoveAllListeners();

            nonARModeLink?.onClick.AddListener(OnNonARModeLinkClicked);
            cameraErrorLink?.onClick.AddListener(OnCameraErrorLinkClicked);
            
            squatBukaBtn?.onClick.AddListener(OnSquatBukaClicked);
            dynamicStretchBukaBtn?.onClick.AddListener(() => OpenNonARMovement("dynamic_stretch"));
            ladderDrillBukaBtn?.onClick.AddListener(() => OpenNonARMovement("ladder_drill"));
            catalogBackBtn?.onClick.AddListener(OnCatalogBackClicked);

            if (infoBadgeButton != null && warningPanel != null)
            {
                infoBadgeButton.onClick.AddListener(() => {
                    warningPanel.SetActive(!warningPanel.activeSelf);
                });
            }

            if (permissionDenied)
            {
                // G09a: Denied
                // settingsBtn (Forest Green primary) opens settings
                settingsBtn?.onClick.AddListener(OpenDeviceSettings);
                // retryBtn (Warm Cream secondary) retries permission
                retryBtn?.onClick.AddListener(OnRetryClicked);
            }
            else
            {
                // G09b: Timeout
                // settingsBtn (Forest Green primary) retries camera
                settingsBtn?.onClick.AddListener(OnRetryClicked);
                // retryBtn (Warm Cream secondary) routes to catalog
                retryBtn?.onClick.AddListener(GoToNonARMode);
            }
        }

        private void OnNonARModeLinkClicked()
        {
            AppStateManager.Instance?.TransitionTo(AppState.NonARCatalog);
        }

        private void OnCameraErrorLinkClicked()
        {
            PermissionController.SetCameraPermissionDeniedForSimulation(true);
            AppStateManager.Instance?.TransitionTo(AppState.CameraDenied);
        }

        private void OnSquatBukaClicked()
        {
            OpenNonARMovement("squat");
        }

        private static void OpenNonARMovement(string movementId)
        {
            if (BootstrapUIController.Instance != null)
            {
                BootstrapUIController.Instance.ShowNonARDetail(movementId);
            }
            else
            {
                AppStateManager.RunInNonARMode = true;
                ActiveMovementContext.ActiveId = movementId;
                ActiveMovementContext.ActiveData = null;
                SceneManager.LoadScene("MainAR");
            }
        }

        private void OnCatalogBackClicked()
        {
            AppStateManager.RunInNonARMode = false;
            ActiveMovementContext.Clear();
            AppStateManager.Instance?.TransitionTo(AppState.Onboarding);
        }

        private void OpenDeviceSettings()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    using (var currentActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        using (var intent = new AndroidJavaObject("android.content.Intent", "android.settings.APPLICATION_DETAILS_SETTINGS"))
                        {
                            using (var uriClass = new AndroidJavaClass("android.net.Uri"))
                            {
                                using (var uri = uriClass.CallStatic<AndroidJavaObject>("parse", "package:" + Application.identifier))
                                {
                                    intent.Call<AndroidJavaObject>("setData", uri);
                                    currentActivity.Call("startActivity", intent);
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[BootstrapButtonController] Failed to open settings: " + ex.Message);
            }
#else
            Debug.Log("[BootstrapButtonController] Opening device settings simulated.");
#endif
        }

        private void GoToNonARMode()
        {
            AppStateManager.RunInNonARMode = true;
            AppStateManager.Instance?.TransitionTo(AppState.NonARCatalog);
        }

        private void OnRetryClicked()
        {
            PermissionController.SetCameraPermissionDeniedForSimulation(false);
            AppStateManager.Instance?.TransitionTo(AppState.RequestingPermission);
        }
    }
}
