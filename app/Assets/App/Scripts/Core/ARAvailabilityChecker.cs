// ============================================================
// GerakAR – ARAvailabilityChecker.cs
// Checks whether ARCore is available on this device using the
// AR Foundation ARSession API and routes appropriately.
// Transitions: CheckingAR → LoadingARScene (Ready + permission granted)
//                         → UnsupportedNotice (Unsupported/InstallFailed)
// ============================================================
using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using GerakAR.Core;

namespace GerakAR.Core
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
#endif

            Debug.Log("[ARAvailabilityChecker] Checking AR availability...");
            yield return ARSession.CheckAvailability();

            ARSessionState state = ARSession.state;
            Debug.Log($"[ARAvailabilityChecker] AR session state: {state}");

            switch (state)
            {
                case ARSessionState.Ready:
                case ARSessionState.SessionInitializing:
                case ARSessionState.SessionTracking:
                    // Device supports AR and services are ready
                    _stateMgr.TransitionTo(AppState.RequestingPermission);
                    break;

                case ARSessionState.NeedsInstall:
                    // Device supports AR but needs Google Play Services for AR
                    Debug.Log("[ARAvailabilityChecker] AR services need installation. Installing...");
                    yield return ARSession.Install();
                    
                    // Wait for the installation state to stabilize
                    while (ARSession.state == ARSessionState.Installing)
                    {
                        yield return null;
                    }
                    
                    // Check result after installation attempt
                    if (ARSession.state == ARSessionState.Ready)
                    {
                        Debug.Log("[ARAvailabilityChecker] AR services installed successfully.");
                        _stateMgr.TransitionTo(AppState.RequestingPermission);
                    }
                    else
                    {
                        Debug.LogWarning($"[ARAvailabilityChecker] AR service installation failed. State: {ARSession.state}");
                        _stateMgr.TransitionTo(AppState.ARInstallFailed);
                    }
                    break;

                case ARSessionState.Unsupported:
                default:
                    // Device does not support AR
                    Debug.LogWarning($"[ARAvailabilityChecker] AR not supported. State: {state}");
                    _stateMgr.TransitionTo(AppState.UnsupportedNotice);
                    break;
            }
        }
    }
}
