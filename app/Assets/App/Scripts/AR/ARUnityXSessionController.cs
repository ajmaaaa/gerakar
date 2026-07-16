using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using GerakAR.Core;

namespace GerakAR.AR
{
    /// <summary>
    /// Owns the ARUnityX camera lifecycle and converts native startup failures
    /// into GerakAR states instead of leaving the user on a black camera view.
    /// </summary>
    public sealed class ARUnityXSessionController : MonoBehaviour
    {
        [Header("ARUnityX")]
        [SerializeField] private ARXController arController;
        [SerializeField] private ARXTrackable imageTarget;
        [SerializeField] private ARXTrackedObject trackedObject;
        [SerializeField] private ARXCamera arCamera;
        [SerializeField] private ARXVideoBackground videoBackground;
        [SerializeField] private string targetImageFileName = "C5.png";
        [SerializeField]
        [Min(0.01f)]
        [Tooltip("ARUnityX image width in Unity units, where 1.0 equals 1 metre.")]
        private float physicalTargetWidthMeters = 0.12f;

        [Header("GerakAR")]
        [SerializeField] private ARImageTrackingController trackingController;
        [SerializeField] private ARUnityXURPBackgroundPresenter backgroundPresenter;
        [SerializeField] [Range(4f, 15f)] private float startupTimeoutSeconds = 8f;

        private static readonly FieldInfo ErrorVisibleField = typeof(ARXController).GetField(
            "showGUIErrorDialog",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo ErrorContentField = typeof(ARXController).GetField(
            "showGUIErrorDialogContent",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private Coroutine _startupTimeout;
        private AppStateManager _stateManager;
        private bool _videoStarted;
        private bool _sessionReady;
        private bool _routingAway;

        private void Start()
        {
            _stateManager = AppStateManager.Instance;

            if (AppStateManager.RunInNonARMode)
            {
                StartNonARPreview();
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            if (arController == null)
            {
                RouteToFallback("ARUnityX controller is missing.", false);
                return;
            }

            StartCoroutine(StartAndroidSession());
#else
            Debug.Log("[ARUnityXSessionController] Native camera starts only in an Android player.");
            _stateManager?.TransitionTo(AppState.Scanning);
#endif
        }

        private void Update()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_routingAway || arController == null || ErrorVisibleField == null)
                return;

            if (ErrorVisibleField.GetValue(arController) is bool hasError && hasError)
            {
                string message = ErrorContentField?.GetValue(arController) as string ?? string.Empty;
                ErrorVisibleField.SetValue(arController, false);
                bool permissionDenied = message.IndexOf("denied camera", System.StringComparison.OrdinalIgnoreCase) >= 0;
                RouteToFallback(message, permissionDenied);
            }
#endif
        }

        private void OnDestroy()
        {
            if (arController == null) return;
            arController.onVideoStarted.RemoveListener(OnVideoStarted);
            arController.onVideoStopped.RemoveListener(OnVideoStopped);
        }

        private IEnumerator WaitForVideoStartup()
        {
            yield return new WaitForSecondsRealtime(startupTimeoutSeconds);
            if (!_sessionReady)
            {
                string reason = _videoStarted
                    ? "ARUnityX C5 target failed to load."
                    : "ARUnityX camera startup timed out.";
                RouteToFallback(reason, false);
            }
        }

        private IEnumerator StartAndroidSession()
        {
            if (imageTarget == null)
            {
                RouteToFallback("ARUnityX C5 trackable is missing.", false);
                yield break;
            }

            string sourcePath = $"{Application.streamingAssetsPath.TrimEnd('/')}/{targetImageFileName}";
            string cachedPath = Path.Combine(Application.temporaryCachePath, "gerakar-c5.png");

            using (UnityWebRequest request = UnityWebRequest.Get(sourcePath))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    RouteToFallback($"Unable to load C5 target: {request.error}", false);
                    yield break;
                }

                try
                {
                    File.WriteAllBytes(cachedPath, request.downloadHandler.data);
                }
                catch (System.Exception exception)
                {
                    RouteToFallback($"Unable to cache C5 target: {exception.Message}", false);
                    yield break;
                }
            }

            imageTarget.ConfigureAsTwoD(cachedPath, physicalTargetWidthMeters);
            imageTarget.enabled = true;
            arController.onVideoStarted.AddListener(OnVideoStarted);
            arController.onVideoStopped.AddListener(OnVideoStopped);
            arController.enabled = true;
            _startupTimeout = StartCoroutine(WaitForVideoStartup());
        }

        private void OnVideoStarted()
        {
            _videoStarted = true;

            if (backgroundPresenter == null || !backgroundPresenter.Present())
            {
                RouteToFallback("ARUnityX camera background failed to render.", false);
                return;
            }

            StartCoroutine(WaitForTargetReady());
        }

        private IEnumerator WaitForTargetReady()
        {
            while (!_routingAway && imageTarget != null && imageTarget.UID == ARXTrackable.NO_ID)
                yield return null;

            if (_routingAway || imageTarget == null)
                yield break;

            _sessionReady = true;
            if (_startupTimeout != null)
            {
                StopCoroutine(_startupTimeout);
                _startupTimeout = null;
            }

            _stateManager?.TransitionTo(AppState.Scanning);
        }

        private void OnVideoStopped()
        {
            backgroundPresenter?.ResetPresentation();
            if (!_routingAway && !AppStateManager.RunInNonARMode && Application.isPlaying)
                Debug.LogWarning("[ARUnityXSessionController] Camera stream stopped.");
        }

        private void StartNonARPreview()
        {
            if (arController != null)
                arController.enabled = false;
            if (imageTarget != null)
                imageTarget.enabled = false;
            if (trackedObject != null)
                trackedObject.enabled = false;
            if (videoBackground != null)
                videoBackground.enabled = false;
            if (arCamera != null)
                arCamera.enabled = false;

            Camera sceneCamera = arCamera != null ? arCamera.GetComponent<Camera>() : Camera.main;
            if (sceneCamera != null)
            {
                sceneCamera.clearFlags = CameraClearFlags.SolidColor;
                sceneCamera.backgroundColor = new Color(0.07f, 0.216f, 0.165f, 1f);
            }

            trackingController?.StartNonARPreview();
        }

        private void RouteToFallback(string diagnostic, bool permissionDenied)
        {
            if (_routingAway) return;
            _routingAway = true;

            Debug.LogWarning($"[ARUnityXSessionController] {diagnostic}");
            if (_startupTimeout != null)
            {
                StopCoroutine(_startupTimeout);
                _startupTimeout = null;
            }

            if (arController != null && arController.enabled)
                arController.enabled = false;

            AppStateManager.RunInNonARMode = !permissionDenied;
            if (permissionDenied)
                PermissionController.SetCameraPermissionDeniedForSimulation(true);
            _stateManager?.TransitionTo(permissionDenied ? AppState.CameraDenied : AppState.UnsupportedNotice);
            SceneManager.LoadScene("Bootstrap");
        }
    }
}
