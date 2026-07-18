using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using GerakAR.Core;
using GerakAR.UI;

namespace GerakAR.AR
{
    /// <summary>
    /// Owns the ARUnityX camera lifecycle and converts native startup failures
    /// into GerakAR states instead of leaving the user on a black camera view.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
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

        private static readonly FieldInfo RunningField = typeof(ARXController).GetField(
            "_running",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo RunOnUnpauseField = typeof(ARXController).GetField(
            "_runOnUnpause",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private Coroutine _startupTimeout;
        private Coroutine _unexpectedStopCheck;
        private AppStateManager _stateManager;
        private bool _videoStarted;
        private bool _sessionReady;
        private bool _routingAway;
        private bool _applicationPaused;
        private bool _applicationFocused = true;
        private int _videoFrameCount;
        private Task<bool> _nativeStopTask;
        private Coroutine _resumeRecovery;
        private bool _asyncLifecycleStop;
        private GameObject _cameraRestartCover;

        private void Start()
        {
            _stateManager = AppStateManager.Instance;

            // ARUnityX creates its video camera/source as root objects in the
            // active scene. MainAR must own them so unloading Bootstrap cannot
            // destroy the native camera presentation.
            Scene ownScene = gameObject.scene;
            if (ownScene.IsValid() && ownScene.isLoaded)
                SceneManager.SetActiveScene(ownScene);

#if UNITY_ANDROID && !UNITY_EDITOR
            CreateCameraRestartCover();
#endif

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
            Debug.Log("[ARUnityXSessionController] Simulating camera startup (editor).");
            // Jangan langsung Scanning — biarkan loading bar Bootstrap tampil dulu
            StartCoroutine(SimulateCameraStartup());
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
            arController.onVideoFrame.RemoveListener(OnVideoFrame);
        }

        private void OnApplicationPause(bool paused)
        {
            _applicationPaused = paused;

#if UNITY_ANDROID && !UNITY_EDITOR
            if (paused)
                BeginAsyncNativeStop();
            else
                BeginAsyncResume();
#endif
        }

        private void OnApplicationFocus(bool focused)
        {
            _applicationFocused = focused;

#if UNITY_ANDROID && !UNITY_EDITOR
            if (focused)
            {
                if (_asyncLifecycleStop)
                    BeginAsyncResume();
            }
            else
            {
                // Android focus loss arrives before ARXController's blocking
                // pause callback, giving us time to take over native shutdown.
                BeginAsyncNativeStop();
            }
#endif
        }

        private void BeginAsyncNativeStop()
        {
            if (_routingAway || arController == null || !arController.IsRunning)
                return;
            if (RunningField == null || RunOnUnpauseField == null)
            {
                Debug.LogError("[ARUnityXSessionController] ARUnityX lifecycle fields are unavailable.");
                return;
            }

            _asyncLifecycleStop = true;
            ShowCameraRestartCover();
            backgroundPresenter?.ResetPresentation();

            // ARXController stops synchronously in its own OnApplicationPause,
            // which blocks Unity's Android pause handshake on this device. Mark
            // it stopped first so that callback becomes a no-op, then close the
            // native camera away from Unity's main thread.
            RunningField.SetValue(arController, false);
            RunOnUnpauseField.SetValue(arController, false);
            Screen.sleepTimeout = SleepTimeout.SystemSetting;

            _nativeStopTask = Task.Run(() => ARX_pinvoke.arwStopRunning());
            Debug.Log("[ARUnityXSessionController] Native camera stop scheduled asynchronously.");
        }

        private void BeginAsyncResume()
        {
            if (_routingAway || arController == null || !_asyncLifecycleStop)
                return;

            if (_resumeRecovery != null)
                StopCoroutine(_resumeRecovery);
            _resumeRecovery = StartCoroutine(ResumeAfterNativeStop());
        }

        private IEnumerator ResumeAfterNativeStop()
        {
            while (_nativeStopTask != null && !_nativeStopTask.IsCompleted)
                yield return null;

            if (_nativeStopTask == null || _nativeStopTask.IsFaulted || !_nativeStopTask.Result)
            {
                string reason = _nativeStopTask?.Exception?.GetBaseException().Message
                    ?? "Native camera stop failed.";
                _nativeStopTask = null;
                _resumeRecovery = null;
                _asyncLifecycleStop = false;
                RouteToFallback(reason, false);
                yield break;
            }

            _nativeStopTask = null;
            arController.onVideoStopped.Invoke();
            yield return null;

            arController.StartAR();
            _asyncLifecycleStop = false;
            _resumeRecovery = null;
            Debug.Log("[ARUnityXSessionController] Native camera restart scheduled after resume.");
        }

        private void CreateCameraRestartCover()
        {
            if (_cameraRestartCover != null)
                return;

            _cameraRestartCover = new GameObject("CameraRestartCover", typeof(RectTransform), typeof(Canvas));
            _cameraRestartCover.transform.SetParent(transform, false);

            Canvas canvas = _cameraRestartCover.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = short.MaxValue;

            var imageObject = new GameObject("Black", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(_cameraRestartCover.transform, false);
            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;

            Image image = imageObject.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = true;

            _cameraRestartCover.SetActive(false);
        }

        private void ShowCameraRestartCover()
        {
            if (_cameraRestartCover != null)
                _cameraRestartCover.SetActive(true);
        }

        private void HideCameraRestartCover()
        {
            if (_cameraRestartCover != null)
                _cameraRestartCover.SetActive(false);
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

            // Beri waktu satu frame agar native resources siap
            yield return null;

            try
            {
                imageTarget.ConfigureAsTwoD(cachedPath, physicalTargetWidthMeters);
                imageTarget.enabled = true;
                arController.onVideoStarted.AddListener(OnVideoStarted);
                arController.onVideoStopped.AddListener(OnVideoStopped);
                arController.onVideoFrame.AddListener(OnVideoFrame);
                arController.enabled = true;
            }
            catch (System.Exception exception)
            {
                RouteToFallback($"Failed to initialize AR camera: {exception.Message}", false);
                yield break;
            }

            _startupTimeout = StartCoroutine(WaitForVideoStartup());
        }

        private void OnVideoStarted()
        {
            _videoStarted = true;

            if (_unexpectedStopCheck != null)
            {
                StopCoroutine(_unexpectedStopCheck);
                _unexpectedStopCheck = null;
            }

            if (backgroundPresenter == null)
            {
                RouteToFallback("ARUnityX background presenter is missing.", false);
                return;
            }

            // Wire failure callback
            backgroundPresenter.OnPresentFailed = reason =>
            {
                if (!_routingAway)
                    RouteToFallback("Camera background setup failed: " + reason, false);
            };
            backgroundPresenter.OnTextureReady = HideCameraRestartCover;

            if (!backgroundPresenter.Present())
            {
                RouteToFallback("ARUnityX camera background failed to render.", false);
                return;
            }

            StartCoroutine(WaitForCameraReady());
        }

        private void OnVideoFrame()
        {
            _videoFrameCount++;
        }

        private IEnumerator WaitForCameraReady()
        {
            float timeout = Time.realtimeSinceStartup + startupTimeoutSeconds;
            Texture videoTex = null;
            GameObject videoObject = null;
            Renderer videoRenderer = null;
            Material videoMaterial = null;

            // Tunggu object "Video source" dibuat oleh ARUnityX runtime
            while (videoObject == null && Time.realtimeSinceStartup < timeout)
            {
                videoObject = GameObject.Find("Video source");
                yield return null;
            }

            if (videoObject == null)
            {
                RouteToFallback("Video source object not found.", false);
                yield break;
            }

            videoRenderer = videoObject.GetComponent<Renderer>();
            videoMaterial = videoRenderer != null ? videoRenderer.sharedMaterial : null;

            while (videoTex == null && Time.realtimeSinceStartup < timeout)
            {
                if (videoMaterial != null)
                    videoTex = videoMaterial.mainTexture;
                yield return null;
            }

            if (videoTex == null)
            {
                RouteToFallback("Camera texture is missing.", false);
                yield break;
            }

            // Tunggu texture native benar-benar bergerak sebelum membuka UI kamera.
            int stableFrames = 0;
            uint lastUpdateCount = videoTex.updateCount;

            while (stableFrames < 2 && Time.realtimeSinceStartup < timeout)
            {
                if (videoTex.width > 0 && videoTex.height > 0)
                {
                    if (videoTex.updateCount > lastUpdateCount)
                    {
                        lastUpdateCount = videoTex.updateCount;
                        stableFrames++;
                    }
                }
                yield return null;
            }

            if (stableFrames < 2)
            {
                RouteToFallback("Camera stream failed to stabilize.", false);
                yield break;
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
            if (_routingAway || AppStateManager.RunInNonARMode || !Application.isPlaying)
                return;

            Debug.LogWarning(
                $"[ARUnityXSessionController] Camera stream stopped " +
                $"(paused={_applicationPaused}, focused={_applicationFocused}, " +
                $"running={arController.IsRunning}).");

            if (_asyncLifecycleStop)
                return;

            if (_unexpectedStopCheck != null)
                StopCoroutine(_unexpectedStopCheck);
            _unexpectedStopCheck = StartCoroutine(VerifyUnexpectedStop());
        }

        private IEnumerator VerifyUnexpectedStop()
        {
            // ARXController deliberately stops video on Android pause and starts it
            // again on resume. Wait for those lifecycle callbacks to settle.
            yield return new WaitForSecondsRealtime(1f);
            _unexpectedStopCheck = null;

            if (_routingAway || _applicationPaused || !_applicationFocused)
                yield break;
            if (arController != null && arController.IsRunning)
                yield break;

            RouteToFallback("Camera stream stopped while the application was active.", false);
        }

        private IEnumerator SimulateCameraStartup()
        {
            // Simulasi inisialisasi kamera — biarkan loading bar terlihat
            yield return new WaitForSeconds(0.5f);
            _sessionReady = true;
            _stateManager?.TransitionTo(AppState.Scanning);
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

            // MATIKAN ARUnityX DULU sebelum cleanup apapun — cegah native crash
            if (arController != null)
            {
                arController.onVideoStarted.RemoveListener(OnVideoStarted);
                arController.onVideoStopped.RemoveListener(OnVideoStopped);
                arController.onVideoFrame.RemoveListener(OnVideoFrame);
                arController.enabled = false;
            }

            backgroundPresenter?.ResetPresentation();

            StopAllCoroutines();

            AppStateManager.RunInNonARMode = !permissionDenied;
            if (permissionDenied)
                PermissionController.SetCameraPermissionDeniedForSimulation(true);
            _stateManager?.TransitionTo(permissionDenied ? AppState.CameraDenied : AppState.UnsupportedNotice);

            SceneManager.LoadScene("Bootstrap");
        }
    }
}
