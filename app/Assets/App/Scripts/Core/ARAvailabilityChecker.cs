// ============================================================
// GerakAR – ARAvailabilityChecker.cs
// Checks whether ARCore is available on this device using the
// AR Foundation ARSession API and handles the Unsupported state.
// ============================================================
using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using GerakAR.Core;

namespace GerakAR.Core
{
    /// <summary>
    /// Checks AR availability after camera permission is granted.
    /// Transitions: CheckingAR → Scanning (available)
    ///                         → Unsupported (not available / not installed)
    /// The <see cref="unsupportedPanel"/> is shown with a friendly message
    /// when ARCore is not supported.
    /// </summary>
    public class ARAvailabilityChecker : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────

        [Header("UI")]
        [Tooltip("Panel shown when the device does not support AR.")]
        [SerializeField] private GameObject unsupportedPanel;

        [Tooltip("Optional: message text component to show reason.")]
        [SerializeField] private TMPro.TextMeshProUGUI unsupportedMessageText;

        // ── Private ───────────────────────────────────────────────────

        private AppStateManager _stateMgr;

        // ── Unity lifecycle ───────────────────────────────────────────

        private void Start()
        {
            _stateMgr = AppStateManager.Instance;
            AppStateManager.OnStateChanged += OnStateChanged;

            if (unsupportedPanel != null)
                unsupportedPanel.SetActive(false);
        }

        private void OnDestroy() =>
            AppStateManager.OnStateChanged -= OnStateChanged;

        private void OnStateChanged(AppState prev, AppState next)
        {
            if (next == AppState.CheckingAR)
                StartCoroutine(CheckAvailability());

            if (next == AppState.Unsupported)
                ShowUnsupported();
        }

        // ── AR availability ───────────────────────────────────────────

        private IEnumerator CheckAvailability()
        {
#if UNITY_EDITOR
            // Always supported in Editor for development
            yield return null;
            _stateMgr.TransitionTo(AppState.Scanning);
            yield break;
#endif
            // Start the AR check
            yield return ARSession.CheckAvailability();

            switch (ARSession.state)
            {
                case ARSessionState.Ready:
                case ARSessionState.SessionInitializing:
                case ARSessionState.SessionTracking:
                    _stateMgr.TransitionTo(AppState.Scanning);
                    break;

                case ARSessionState.NeedsInstall:
                    yield return ARSession.Install();
                    if (ARSession.state == ARSessionState.Ready)
                        _stateMgr.TransitionTo(AppState.Scanning);
                    else
                        _stateMgr.TransitionTo(AppState.Unsupported);
                    break;

                default:
                    _stateMgr.TransitionTo(AppState.Unsupported);
                    break;
            }
        }

        private void ShowUnsupported()
        {
            if (unsupportedPanel != null)
                unsupportedPanel.SetActive(true);

            string msg = "Perangkat ini tidak mendukung AR.\nMinta guru atau orang tua untuk membantu.";
            if (unsupportedMessageText != null)
                unsupportedMessageText.text = msg;
            else
                Debug.LogWarning($"[ARAvailabilityChecker] Unsupported: {msg}");
        }
    }
}
