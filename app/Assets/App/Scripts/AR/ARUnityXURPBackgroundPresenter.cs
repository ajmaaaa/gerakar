using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MoveMotion.AR
{
    /// <summary>
    /// Adapts ARUnityX's Built-In two-camera video background to URP camera stacking.
    /// Menangani mirror flip horizontal, green bar, dan pencarian object runtime.
    /// </summary>
    public sealed class ARUnityXURPBackgroundPresenter : MonoBehaviour
    {
        [SerializeField] private Camera foregroundCamera;
        [SerializeField] [Range(0.5f, 10f)] private float textureUpdateTimeoutSeconds = 5f;

        /// <summary>Dipanggil ketika setup background gagal total.</summary>
        public System.Action<string> OnPresentFailed;
        public System.Action OnTextureReady;

        private Camera _backgroundCamera;
        private UniversalAdditionalCameraData _backgroundCameraData;
        private UniversalAdditionalCameraData _foregroundCameraData;
        private Texture _videoTexture;
        private Renderer _videoRenderer;
        private Coroutine _textureValidation;
        private Coroutine _findBackgroundCoroutine;
        private bool _isPresenting;

        /// <summary>
        /// Mulai pencarian background camera secara async.
        /// ARUnityX membuat object "Video background" dan "Video source" saat runtime,
        /// jadi kita perlu menunggu beberapa frame.
        /// </summary>
        public bool Present()
        {
            if (_isPresenting)
            {
                Debug.Log("[ARUnityXURPBackgroundPresenter] Already presenting; skipping duplicate call.");
                return true;
            }

            if (foregroundCamera == null)
            {
                Debug.LogError("[ARUnityXURPBackgroundPresenter] Foreground camera is missing.");
                return false;
            }

            _isPresenting = true;

            // Pastikan foreground camera di-setup dengan benar untuk overlay
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)
            {
                _foregroundCameraData = foregroundCamera.GetUniversalAdditionalCameraData();
                if (_foregroundCameraData != null)
                {
                    _foregroundCameraData.renderType = CameraRenderType.Overlay;
                }
            }

            // Mulai coroutine untuk mencari background camera secara async
            if (_findBackgroundCoroutine != null)
                StopCoroutine(_findBackgroundCoroutine);
            _findBackgroundCoroutine = StartCoroutine(FindAndConfigureBackground());

            return true;
        }

        private IEnumerator FindAndConfigureBackground()
        {
            float timeout = Time.realtimeSinceStartup + 5f;
            GameObject backgroundObject = null;
            GameObject videoObject = null;

            // Tunggu sampai object runtime ARUnityX muncul
            while (Time.realtimeSinceStartup < timeout)
            {
                backgroundObject = GameObject.Find("Video background");
                videoObject = GameObject.Find("Video source");
                if (backgroundObject != null && videoObject != null)
                    break;
                yield return new WaitForSeconds(0.1f);
            }

            if (backgroundObject == null || videoObject == null)
            {
                string msg = "Video background/source objects not found after timeout.";
                Debug.LogError("[ARUnityXURPBackgroundPresenter] " + msg);
                OnPresentFailed?.Invoke(msg);
                _findBackgroundCoroutine = null;
                _isPresenting = false;
                yield break;
            }

            _backgroundCamera = backgroundObject.GetComponent<Camera>();
            Renderer videoRenderer = videoObject.GetComponent<Renderer>();
            Material videoMaterial = videoRenderer != null ? videoRenderer.sharedMaterial : null;
            _videoTexture = videoMaterial != null ? videoMaterial.mainTexture : null;

            if (_backgroundCamera == null || videoRenderer == null || videoMaterial == null || _videoTexture == null)
            {
                string msg = "Video background components missing after found.";
                Debug.LogError("[ARUnityXURPBackgroundPresenter] " + msg);
                OnPresentFailed?.Invoke(msg);
                _findBackgroundCoroutine = null;
                _isPresenting = false;
                yield break;
            }

            Scene ownerScene = foregroundCamera.gameObject.scene;
            MoveRuntimeRootToScene(backgroundObject, ownerScene);
            MoveRuntimeRootToScene(videoObject, ownerScene);

            _videoRenderer = videoRenderer;
            _videoRenderer.enabled = false;
            _backgroundCamera.backgroundColor = Color.black;
            foregroundCamera.backgroundColor = Color.black;

            // Setup background camera untuk URP stacking
            // JANGAN ubah cullingMask atau clearFlags background camera —
            // ARUnityX sudah mengaturnya dengan benar untuk rendering video.
            // Kita hanya perlu stacking: bg sebagai Base, fg sebagai Overlay.
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)
            {
                _backgroundCameraData = _backgroundCamera.GetUniversalAdditionalCameraData();
                _foregroundCameraData = foregroundCamera.GetUniversalAdditionalCameraData();

                if (_backgroundCameraData != null)
                    _backgroundCameraData.renderType = CameraRenderType.Base;

                if (_foregroundCameraData != null)
                    _foregroundCameraData.renderType = CameraRenderType.Overlay;

                if (_backgroundCameraData != null && _backgroundCameraData.cameraStack == null)
                {
                    Debug.LogError("[ARUnityXURPBackgroundPresenter] Active URP renderer does not support camera stacking.");
                    _findBackgroundCoroutine = null;
                    _isPresenting = false;
                    yield break;
                }

                if (_backgroundCameraData != null)
                {
                    _backgroundCameraData.cameraStack.Clear();
                    _backgroundCameraData.cameraStack.Add(foregroundCamera);
                }
            }

            // Keep ARUnityX's generated mesh UVs; they already encode the required
            // Android vertical flip. Only replace its fixed-function shader.
            Shader cameraShader = Resources.Load<Shader>("GerakARCameraBackground");
            if (cameraShader == null || !cameraShader.isSupported)
            {
                string msg = "Bundled URP camera background shader is unavailable.";
                Debug.LogError("[ARUnityXURPBackgroundPresenter] " + msg);
                OnPresentFailed?.Invoke(msg);
                _findBackgroundCoroutine = null;
                _isPresenting = false;
                yield break;
            }

            Texture cameraTexture = videoMaterial.mainTexture;
            videoMaterial.shader = cameraShader;
            videoMaterial.mainTexture = cameraTexture;
            videoMaterial.color = Color.white;

            // ARUnityX uses Fill mode and an orthographic projection to crop a
            // landscape sensor frame into a portrait viewport. Reapply this after
            // changing the foreground camera to an URP overlay camera.
            ARXCamera arxCamera = foregroundCamera.GetComponent<ARXCamera>();
            ARXVideoBackground arxBackground = foregroundCamera.GetComponent<ARXVideoBackground>();
            if (arxCamera == null || arxBackground == null)
            {
                string msg = "ARUnityX camera/background components are missing.";
                Debug.LogError("[ARUnityXURPBackgroundPresenter] " + msg);
                OnPresentFailed?.Invoke(msg);
                _findBackgroundCoroutine = null;
                _isPresenting = false;
                yield break;
            }

            arxCamera.CameraContentMode = ARXCamera.ContentMode.Fill;
            arxBackground.OnScreenGeometryChanged();

            // ARUnityX rotates its background Camera GameObject for portrait.
            // URP camera stacking does not apply that transform consistently,
            // leaving the landscape mesh letterboxed. Move the same relative
            // rotation onto the video mesh, which URP renders deterministically.
            Quaternion screenRotation = _backgroundCamera.transform.localRotation;
            videoObject.transform.localRotation =
                Quaternion.Inverse(screenRotation) * videoObject.transform.localRotation;
            _backgroundCamera.transform.localRotation = Quaternion.identity;

            // AR Camera foreground: Depth Only agar tidak timpa background camera
            // Background camera tetap SolidColor (setting ARUnityX asli)
            foregroundCamera.clearFlags = CameraClearFlags.Depth;

            // Diagnostic log untuk debug flip/orientasi
            {
                Renderer r = videoObject.GetComponent<Renderer>();
                Vector3 bgScale = backgroundObject.transform.localScale;
                Vector3 bgRot = backgroundObject.transform.localEulerAngles;
                Vector3 meshScale = videoObject.transform.localScale;
                Vector3 meshRot = videoObject.transform.localEulerAngles;
                Debug.Log(
                    $"[ARUnityXURPBackgroundPresenter] Camera feed configured: " +
                    $"tex={_videoTexture.width}x{_videoTexture.height}, " +
                    $"screen={Screen.width}x{Screen.height}, " +
                    $"bgCam='{_backgroundCamera.name}' layer={_backgroundCamera.gameObject.layer} " +
                    $"mask={_backgroundCamera.cullingMask}, " +
                    $"clearFlags={_backgroundCamera.clearFlags}, " +
                    $"fgCam='{foregroundCamera.name}', " +
                    $"orientation={Screen.orientation}, " +
                    $"bgScale=({bgScale.x:F3},{bgScale.y:F3},{bgScale.z:F3}), " +
                    $"bgRot=({bgRot.x:F1},{bgRot.y:F1},{bgRot.z:F1}), " +
                    $"meshScale=({meshScale.x:F3},{meshScale.y:F3},{meshScale.z:F3}), " +
                    $"meshRot=({meshRot.x:F1},{meshRot.y:F1},{meshRot.z:F1}), " +
                    $"material={r?.sharedMaterial?.name}, " +
                    $"shader={r?.sharedMaterial?.shader?.name}, " +
                    $"contentMode={arxCamera.CameraContentMode}, " +
                    $"bgOrtho={_backgroundCamera.orthographic}, " +
                    $"bgRect={_backgroundCamera.pixelRect}");
            }

            // Validasi texture
            if (_textureValidation != null)
                StopCoroutine(_textureValidation);
            _textureValidation = StartCoroutine(ValidateTextureUpdates());
            _findBackgroundCoroutine = null;
            _isPresenting = false;
        }

        public void ResetPresentation()
        {
            if (_findBackgroundCoroutine != null)
            {
                StopCoroutine(_findBackgroundCoroutine);
                _findBackgroundCoroutine = null;
            }
            if (_textureValidation != null)
            {
                StopCoroutine(_textureValidation);
                _textureValidation = null;
            }

            if (_videoRenderer != null)
                _videoRenderer.enabled = false;

            if (_backgroundCameraData != null && _backgroundCameraData.cameraStack != null)
                _backgroundCameraData.cameraStack.Clear();
            if (_foregroundCameraData != null)
                _foregroundCameraData.renderType = CameraRenderType.Base;

            _backgroundCamera = null;
            _backgroundCameraData = null;
            _foregroundCameraData = null;
            _videoTexture = null;
            _videoRenderer = null;
            _isPresenting = false;
        }

        private static void MoveRuntimeRootToScene(GameObject runtimeObject, Scene ownerScene)
        {
            if (runtimeObject == null || runtimeObject.transform.parent != null)
                return;
            if (!ownerScene.IsValid() || !ownerScene.isLoaded || runtimeObject.scene == ownerScene)
                return;

            SceneManager.MoveGameObjectToScene(runtimeObject, ownerScene);
            Debug.Log(
                $"[ARUnityXURPBackgroundPresenter] Moved '{runtimeObject.name}' " +
                $"from Bootstrap ownership to '{ownerScene.name}'.");
        }

        private IEnumerator ValidateTextureUpdates()
        {
            uint initialUpdateCount = _videoTexture.updateCount;
            float deadline = Time.realtimeSinceStartup + textureUpdateTimeoutSeconds;

            while (_videoTexture != null && Time.realtimeSinceStartup < deadline)
            {
                if (_videoTexture.updateCount > initialUpdateCount)
                {
                    if (_videoRenderer != null)
                        _videoRenderer.enabled = true;
                    OnTextureReady?.Invoke();
                    Debug.Log(
                        $"[ARUnityXURPBackgroundPresenter] Camera texture is updating " +
                        $"(updateCount {initialUpdateCount} -> {_videoTexture.updateCount}).");
                    _textureValidation = null;
                    yield break;
                }

                yield return null;
            }

            Debug.LogError("[ARUnityXURPBackgroundPresenter] Camera texture did not update before timeout.");
            if (_videoRenderer != null)
                _videoRenderer.enabled = false;
            _textureValidation = null;
        }
    }
}
