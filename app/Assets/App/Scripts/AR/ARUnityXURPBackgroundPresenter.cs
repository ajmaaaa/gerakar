using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GerakAR.AR
{
    /// <summary>
    /// Adapts ARUnityX's Built-In two-camera video background to URP camera stacking.
    /// Menangani mirror flip horizontal, green bar, dan pencarian object runtime.
    /// </summary>
    public sealed class ARUnityXURPBackgroundPresenter : MonoBehaviour
    {
        [SerializeField] private Camera foregroundCamera;
        [SerializeField] [Range(0.5f, 10f)] private float textureUpdateTimeoutSeconds = 5f;
        [SerializeField] private int backgroundLayer = 8;

        [Header("Flip Correction")]
        [SerializeField] [Tooltip("Flip video horizontally (X axis) if camera appears mirrored.")]
        private bool flipHorizontally = false;
        [SerializeField] [Tooltip("Flip video vertically (Y axis) if camera appears upside down.")]
        private bool flipVertically = false;

        /// <summary>Dipanggil ketika setup background gagal total.</summary>
        public System.Action<string> OnPresentFailed;

        private Camera _backgroundCamera;
        private UniversalAdditionalCameraData _backgroundCameraData;
        private UniversalAdditionalCameraData _foregroundCameraData;
        private Texture _videoTexture;
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

            // Perbaiki mirror/flip: toggle di Inspector untuk coba flip X/Y
            if (videoMaterial != null)
            {
                float sx = flipHorizontally ? -1f : 1f;
                float sy = flipVertically ? -1f : 1f;
                videoMaterial.mainTextureScale = new Vector2(sx, sy);
                videoMaterial.mainTextureOffset = Vector2.zero;
                Debug.Log($"[ARUnityXURPBackgroundPresenter] Texture scale set to ({sx}, {sy})");
            }

            // Ubah clear color background camera dari hijau (default ARUnityX) ke hitam
            // agar area yang tidak tertutup quad tidak hijau.
            _backgroundCamera.backgroundColor = Color.black;

            // Shader VideoPlaneNoLight (Built-in) mungkin tidak render rotasi
            // dengan benar di URP. Coba pakai Unlit/Texture yang URP native.
            if (videoMaterial != null && videoMaterial.shader != null)
            {
                bool isURPShader = videoMaterial.shader.name.Contains("Universal Render Pipeline")
                                || videoMaterial.shader.name.Contains("URP")
                                || videoMaterial.shader.name == "Unlit/Texture";
                if (!isURPShader)
                {
                    Shader unlit = Shader.Find("Unlit/Texture");
                    if (unlit != null && unlit.isSupported)
                    {
                        videoMaterial.shader = unlit;
                        Debug.Log("[ARUnityXURPBackgroundPresenter] Switched to Unlit/Texture for URP compatibility.");
                    }
                }
            }

            // AR Camera foreground: Depth Only agar tidak timpa background camera
            // Background camera tetap SolidColor (setting ARUnityX asli)
            foregroundCamera.clearFlags = CameraClearFlags.Depth;

            // Diagnostic log untuk debug flip/orientasi
            {
                Renderer r = videoObject.GetComponent<Renderer>();
                Vector3 qScale = backgroundObject.transform.localScale;
                Vector3 qRot = backgroundObject.transform.localEulerAngles;
                Debug.Log(
                    $"[ARUnityXURPBackgroundPresenter] Camera feed configured: " +
                    $"tex={_videoTexture.width}x{_videoTexture.height}, " +
                    $"screen={Screen.width}x{Screen.height}, " +
                    $"bgCam='{_backgroundCamera.name}' layer={_backgroundCamera.gameObject.layer} " +
                    $"mask={_backgroundCamera.cullingMask}, " +
                    $"clearFlags={_backgroundCamera.clearFlags}, " +
                    $"fgCam='{foregroundCamera.name}', " +
                    $"qScale=({qScale.x:F3},{qScale.y:F3},{qScale.z:F3}), " +
                    $"qRot=({qRot.x:F1},{qRot.y:F1},{qRot.z:F1}), " +
                    $"material={r?.sharedMaterial?.name}, " +
                    $"shader={r?.sharedMaterial?.shader?.name}, " +
                    $"texScale={r?.sharedMaterial?.mainTextureScale}, " +
                    $"flipH={flipHorizontally}, flipV={flipVertically}");
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
            if (_textureValidation != null)
            {
                StopCoroutine(_textureValidation);
                _textureValidation = null;
            }

            if (_backgroundCameraData != null && _backgroundCameraData.cameraStack != null)
                _backgroundCameraData.cameraStack.Clear();
            if (_foregroundCameraData != null)
                _foregroundCameraData.renderType = CameraRenderType.Base;

            _backgroundCamera = null;
            _backgroundCameraData = null;
            _foregroundCameraData = null;
            _videoTexture = null;
        }

        private IEnumerator ValidateTextureUpdates()
        {
            uint initialUpdateCount = _videoTexture.updateCount;
            float deadline = Time.realtimeSinceStartup + textureUpdateTimeoutSeconds;

            while (_videoTexture != null && Time.realtimeSinceStartup < deadline)
            {
                if (_videoTexture.updateCount > initialUpdateCount)
                {
                    Debug.Log(
                        $"[ARUnityXURPBackgroundPresenter] Camera texture is updating " +
                        $"(updateCount {initialUpdateCount} -> {_videoTexture.updateCount}).");
                    _textureValidation = null;
                    yield break;
                }

                yield return null;
            }

            Debug.LogError("[ARUnityXURPBackgroundPresenter] Camera texture did not update before timeout.");
            _textureValidation = null;
        }
    }
}
