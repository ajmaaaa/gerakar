// ============================================================
// GerakAR – PermissionController.cs
// Requests camera permission on Android via Unity's permission API
// and advances the AppState accordingly.
// ============================================================
using System.Collections;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
using GerakAR.Core;

namespace GerakAR.Core
{
    /// <summary>
    /// Checks and requests camera permission on Android.
    /// On other platforms (Editor, iOS) it assumes permission is granted.
    /// Transitions: RequestingPermission → CheckingAR (granted)
    ///                                   → CameraDenied (denied/permanently denied)
    /// </summary>
    public class PermissionController : MonoBehaviour
    {
        [Tooltip("Seconds to wait for the permission dialog before checking result.")]
        [SerializeField] [Range(0.5f, 5f)] private float dialogWaitSeconds = 2.5f;

        private AppStateManager _stateMgr;

        private void Start()
        {
            _stateMgr = AppStateManager.Instance;

            AppStateManager.OnStateChanged += OnStateChanged;

            // Begin check immediately if we start in RequestingPermission
            if (_stateMgr.Is(AppState.RequestingPermission))
                CheckAndRequestPermission();
        }

        private void OnDestroy() =>
            AppStateManager.OnStateChanged -= OnStateChanged;

        private void OnStateChanged(AppState prev, AppState next)
        {
            if (next == AppState.RequestingPermission)
                CheckAndRequestPermission();
        }

        private void CheckAndRequestPermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionGranted += OnPermissionGranted;
                callbacks.PermissionDenied += OnPermissionDenied;
                callbacks.PermissionDeniedAndDontAskAgain += OnPermissionDenied;
                Permission.RequestUserPermission(Permission.Camera, callbacks);
            }
            else
            {
                _stateMgr.TransitionTo(AppState.CheckingAR);
            }
#else
            // Non-Android platforms
            _stateMgr.TransitionTo(AppState.CheckingAR);
#endif
        }

        public static bool CameraPermissionDenied { get; private set; } = false;

        private void OnPermissionGranted(string permissionName)
        {
            CameraPermissionDenied = false;
            if (_stateMgr != null)
                _stateMgr.TransitionTo(AppState.CheckingAR);
        }

        private void OnPermissionDenied(string permissionName)
        {
            CameraPermissionDenied = true;
            if (_stateMgr != null)
                _stateMgr.TransitionTo(AppState.CameraDenied);
        }
    }
}
