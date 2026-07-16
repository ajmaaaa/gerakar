using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GerakAR.AR
{
    /// <summary>
    /// Adapts ARUnityX's Built-In two-camera video background to URP camera stacking.
    /// </summary>
    public sealed class ARUnityXURPBackgroundPresenter : MonoBehaviour
    {
        [SerializeField] private Camera foregroundCamera;
        [SerializeField] [Range(0.5f, 5f)] private float textureUpdateTimeoutSeconds = 3f;

        private Camera _backgroundCamera;
        private UniversalAdditionalCameraData _backgroundCameraData;
        private UniversalAdditionalCameraData _foregroundCameraData;
        private Texture _videoTexture;
        private Coroutine _textureValidation;

        public bool Present()
        {
            if (foregroundCamera == null)
            {
                Debug.LogError("[ARUnityXURPBackgroundPresenter] Foreground camera is missing.");
                return false;
            }

            GameObject backgroundObject = GameObject.Find("Video background");
            GameObject videoObject = GameObject.Find("Video source");
            _backgroundCamera = backgroundObject != null ? backgroundObject.GetComponent<Camera>() : null;
            Renderer videoRenderer = videoObject != null ? videoObject.GetComponent<Renderer>() : null;
            Material videoMaterial = videoRenderer != null ? videoRenderer.sharedMaterial : null;
            _videoTexture = videoMaterial != null ? videoMaterial.mainTexture : null;

            if (_backgroundCamera == null || videoRenderer == null || videoMaterial == null || _videoTexture == null)
            {
                Debug.LogError(
                    "[ARUnityXURPBackgroundPresenter] Video background camera, renderer, material, or texture is missing.");
                return false;
            }

            if (_videoTexture.width <= 0 || _videoTexture.height <= 0 ||
                !videoRenderer.enabled || !videoRenderer.gameObject.activeInHierarchy)
            {
                Debug.LogError(
                    $"[ARUnityXURPBackgroundPresenter] Invalid video presentation: " +
                    $"texture={_videoTexture.width}x{_videoTexture.height}, " +
                    $"rendererEnabled={videoRenderer.enabled}, active={videoRenderer.gameObject.activeInHierarchy}.");
                return false;
            }

            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)
            {
                _backgroundCameraData = _backgroundCamera.GetUniversalAdditionalCameraData();
                _foregroundCameraData = foregroundCamera.GetUniversalAdditionalCameraData();
                _backgroundCameraData.renderType = CameraRenderType.Base;
                _foregroundCameraData.renderType = CameraRenderType.Overlay;

                if (_backgroundCameraData.cameraStack == null)
                {
                    Debug.LogError("[ARUnityXURPBackgroundPresenter] Active URP renderer does not support camera stacking.");
                    return false;
                }

                _backgroundCameraData.cameraStack.Clear();
                _backgroundCameraData.cameraStack.Add(foregroundCamera);
            }

            Debug.Log(
                $"[ARUnityXURPBackgroundPresenter] Camera feed configured: " +
                $"texture={_videoTexture.width}x{_videoTexture.height}, " +
                $"material={videoMaterial.shader.name}, rendererLayer={videoObject.layer}, " +
                $"backgroundMask={_backgroundCamera.cullingMask}, foregroundMask={foregroundCamera.cullingMask}.");

            if (_textureValidation != null)
                StopCoroutine(_textureValidation);
            _textureValidation = StartCoroutine(ValidateTextureUpdates());
            return true;
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
