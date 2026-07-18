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

        /// <summary>Dipanggil ketika setup background gagal total.</summary>
        public System.Action<string> OnPresentFailed;

        private Camera _backgroundCamera;
        private UniversalAdditionalCameraData _backgroundCameraData;
        private UniversalAdditionalCameraData _foregroundCameraData;
        private Texture _videoTexture;
        private Coroutine _textureValidation;
        private Coroutine _findBackgroundCoroutine;

        /// <summary>
        /// Mulai pencarian background camera secara async.
        /// ARUnityX membuat object "Video background" dan "Video source" saat runtime,
        /// jadi kita perlu menunggu beberapa frame.
        /// </summary>
        public bool Present()
        {
            if (foregroundCamera == null)
            {
                Debug.LogError("[ARUnityXURPBackgroundPresenter] Foreground camera is missing.");
                return false;
            }

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
                yield break;
            }

            // Setup background camera untuk URP stacking
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)
            {
                _backgroundCameraData = _backgroundCamera.GetUniversalAdditionalCameraData();
                _foregroundCameraData = foregroundCamera.GetUniversalAdditionalCameraData();

                if (_backgroundCameraData != null)
                {
                    _backgroundCameraData.renderType = CameraRenderType.Base;

                    // Setup culling — hanya render layer background
                    _backgroundCamera.cullingMask = 1 << backgroundLayer;

                    // Clear flags: Depth Only agar tidak menimpa dengan warna solid
                    _backgroundCamera.clearFlags = CameraClearFlags.Depth;
                }

                if (_foregroundCameraData != null)
                {
                    _foregroundCameraData.renderType = CameraRenderType.Overlay;
                }

                if (_backgroundCameraData != null && _backgroundCameraData.cameraStack == null)
                {
                    Debug.LogError("[ARUnityXURPBackgroundPresenter] Active URP renderer does not support camera stacking.");
                    _findBackgroundCoroutine = null;
                    yield break;
                }

                if (_backgroundCameraData != null)
                {
                    _backgroundCameraData.cameraStack.Clear();
                    _backgroundCameraData.cameraStack.Add(foregroundCamera);
                }
            }

            // Perbaiki mirror/flip: reset texture transform ke identity
            // Back camera tidak perlu horizontal flip
            if (videoMaterial != null)
            {
                videoMaterial.mainTextureScale = new Vector2(1f, 1f);
                videoMaterial.mainTextureOffset = Vector2.zero;
            }

            // Skala video quad untuk mengisi layar penuh (Scale to Fill)
            // Mencegah green bar di tepi karena aspect ratio tidak cocok
            ScaleQuadToFillScreen(backgroundObject, videoObject, _videoTexture);

            // Pastikan material menggunakan shader yang tepat untuk URP
            // Shader ARUnityX Built-In tidak kompatibel dengan URP, gunakan UnityUnlit
            if (videoMaterial != null && videoMaterial.shader != null && videoMaterial.shader.name.Contains("Standard"))
            {
                Shader unlitShader = Shader.Find("Unlit/Texture");
                if (unlitShader != null)
                    videoMaterial.shader = unlitShader;
            }

            // Pastikan AR Camera foreground memiliki clear flags yang benar
            // Depth Only agar tidak menimpa background dengan warna solid
            foregroundCamera.clearFlags = CameraClearFlags.Depth;

            Debug.Log(
                $"[ARUnityXURPBackgroundPresenter] Camera feed configured: " +
                $"texture={_videoTexture.width}x{_videoTexture.height}, " +
                $"bgCam={_backgroundCamera.name}, fgCam={foregroundCamera.name}.");

            // Validasi texture
            if (_textureValidation != null)
                StopCoroutine(_textureValidation);
            _textureValidation = StartCoroutine(ValidateTextureUpdates());
            _findBackgroundCoroutine = null;
        }

        private void ScaleQuadToFillScreen(GameObject backgroundObject, GameObject videoObject, Texture videoTex)
        {
            if (backgroundObject == null || videoObject == null || videoTex == null)
                return;

            float texAspect = (float)videoTex.width / videoTex.height;
            float screenAspect = (float)Screen.width / Screen.height;

            // Scale to Fill: pastikan quad menutupi seluruh layar
            // Jika tex lebih lebar dari screen, sesuaikan height; jika lebih sempit, sesuaikan width
            float scaleX = 1f;
            float scaleY = 1f;
            if (texAspect > screenAspect)
            {
                // Texture lebih lebar — samakan width, scale up height
                scaleY = texAspect / screenAspect;
            }
            else
            {
                // Screen lebih lebar — samakan height, scale up width
                scaleX = screenAspect / texAspect;
            }

            Transform bgTransform = backgroundObject.transform;
            bgTransform.localScale = new Vector3(
                bgTransform.localScale.x * scaleX,
                bgTransform.localScale.y * scaleY,
                bgTransform.localScale.z
            );

            Debug.Log(
                $"[ARUnityXURPBackgroundPresenter] Quad scaled to fill: " +
                $"texture {videoTex.width}x{videoTex.height}, " +
                $"screen {Screen.width}x{Screen.height}, " +
                $"scale {scaleX:F3}x{scaleY:F3}");
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
