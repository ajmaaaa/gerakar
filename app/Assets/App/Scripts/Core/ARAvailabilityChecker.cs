// ============================================================
// MotionLearn – ARAvailabilityChecker.cs
// Checks whether the ARUnityX runtime can be attempted on this platform.
// Transitions: CheckingAR → LoadingARScene (Ready + permission granted)
//                         → UnsupportedNotice (Unsupported/InstallFailed)
// ============================================================
using System.Collections;
using UnityEngine;

namespace MotionLearn.Core
{
    /// <summary>
    /// Checks AR availability and handles proper routing for:
    /// - AR-supported devices → proceed to MainAR scene
    /// - AR-unsupported devices → route to Non-AR fallback experience
    /// Does NOT show panels directly; reports results to state machine.
    /// </summary>
    public class ARAvailabilityChecker : MonoBehaviour
    {
        private AppStateManager _stateMgr;

        // ── Unity lifecycle ───────────────────────────────────────────

        private void Start()
        {
            _stateMgr = AppStateManager.Instance;
            AppStateManager.OnStateChanged += OnStateChanged;
        }

        private void OnDestroy() =>
            AppStateManager.OnStateChanged -= OnStateChanged;

        private void OnStateChanged(AppState prev, AppState next)
        {
            if (next == AppState.CheckingAR)
                StartCoroutine(CheckAvailability());
        }

        // ── AR availability ───────────────────────────────────────────

        private IEnumerator CheckAvailability()
        {
            // Forced Non-AR mode for testing/debugging
            if (AppStateManager.RunInNonARMode)
            {
                Debug.Log("[ARAvailabilityChecker] Forced Non-AR mode active.");
                yield return new WaitForSeconds(0.3f);
                _stateMgr.TransitionTo(AppState.UnsupportedNotice);
                yield break;
            }

#if UNITY_EDITOR
            // Editor: simulate AR-supported for development
            Debug.Log("[ARAvailabilityChecker] Editor detected; simulating AR support.");
            yield return null;
            _stateMgr.TransitionTo(AppState.RequestingPermission);
            yield break;
#else
            yield return null;

#if UNITY_ANDROID
            // ARUnityX ships native ARMv7 and ARM64 libraries and does not depend
            // on Google Play Services for AR. Camera startup is validated in MainAR.
            _stateMgr.TransitionTo(AppState.RequestingPermission);
#else
            Debug.LogWarning("[ARAvailabilityChecker] ARUnityX production runtime is Android-only.");
            _stateMgr.TransitionTo(AppState.UnsupportedNotice);
#endif
#endif
        }
    }
}
