using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using GerakAR.Core;

namespace GerakAR.UI
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
        [SerializeField] private Button catalogBackBtn;

        [Header("G09 Camera Error Buttons")]
        [SerializeField] private Button settingsBtn;
        [SerializeField] private Button retryBtn;

        private void Start()
        {
            nonARModeLink?.onClick.AddListener(OnNonARModeLinkClicked);
            cameraErrorLink?.onClick.AddListener(OnCameraErrorLinkClicked);
            
            squatBukaBtn?.onClick.AddListener(OnSquatBukaClicked);
            catalogBackBtn?.onClick.AddListener(OnCatalogBackClicked);

            settingsBtn?.onClick.AddListener(OnSettingsClicked);
            retryBtn?.onClick.AddListener(OnRetryClicked);
        }

        private void OnNonARModeLinkClicked()
        {
            // Transition to Unsupported, which shows G08 Non-AR catalog panel
            AppStateManager.Instance?.TransitionTo(AppState.Unsupported);
        }

        private void OnCameraErrorLinkClicked()
        {
            // Force Camera Permission Denied to show G09 view
            // We use reflection to set the private setter of CameraPermissionDenied
            var deniedProp = typeof(PermissionController).GetProperty("CameraPermissionDenied");
            if (deniedProp != null)
            {
                deniedProp.SetValue(null, true);
            }
            AppStateManager.Instance?.TransitionTo(AppState.Unsupported);
        }

        private void OnSquatBukaClicked()
        {
            AppStateManager.RunInNonARMode = true;
            SceneManager.LoadScene("MainAR");
        }

        private void OnCatalogBackClicked()
        {
            // Go back to onboarding / intro
            AppStateManager.Instance?.TransitionTo(AppState.Scanning);
        }

        private void OnSettingsClicked()
        {
            // Try to open Android Settings (standard Unity helper)
            #if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        using (var intent = new AndroidJavaObject("android.content.Intent", "android.settings.APPLICATION_DETAILS_SETTINGS"))
                        {
                            var uri = new AndroidJavaClass("android.net.Uri");
                            var packageUri = uri.CallStatic<AndroidJavaObject>("fromParts", "package", currentActivity.Call<string>("getPackageName"), null);
                            intent.Call<AndroidJavaObject>("setData", packageUri);
                            currentActivity.Call("startActivity", intent);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[GerakAR] Failed to open application settings: " + ex.Message);
            }
            #else
            Debug.Log("[GerakAR] Open settings simulated.");
            #endif
        }

        private void OnRetryClicked()
        {
            // Reset permission state and retry from requesting permission
            var deniedProp = typeof(PermissionController).GetProperty("CameraPermissionDenied");
            if (deniedProp != null)
            {
                deniedProp.SetValue(null, false);
            }
            AppStateManager.Instance?.TransitionTo(AppState.RequestingPermission);
        }
    }
}
