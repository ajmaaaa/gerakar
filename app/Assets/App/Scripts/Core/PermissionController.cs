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
    ///                                   → Unsupported (permanently denied)
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

            // If we start in RequestingPermission (Bootstrap sets this), begin immediately
            if (_stateMgr.Is(AppState.RequestingPermission))
                StartCoroutine(RequestCamera());
        }

        private void OnDestroy() =>
            AppStateManager.OnStateChanged -= OnStateChanged;

        private void OnStateChanged(AppState prev, AppState next)
        {
            if (next == AppState.RequestingPermission)
                StartCoroutine(RequestCamera());
        }

        private IEnumerator RequestCamera()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
                yield return new WaitForSeconds(dialogWaitSeconds);
            }

            if (Permission.HasUserAuthorizedPermission(Permission.Camera))
                _stateMgr.TransitionTo(AppState.CheckingAR);
            else
                _stateMgr.TransitionTo(AppState.Unsupported);
#else
            // Editor / non-Android: always granted
            yield return null;
            _stateMgr.TransitionTo(AppState.CheckingAR);
#endif
        }
    }
}
