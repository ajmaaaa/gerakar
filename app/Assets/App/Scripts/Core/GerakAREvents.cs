// ============================================================
// GerakAR – GerakAREvents.cs
// Central static event bus for decoupled inter-system messaging.
// Audio and future analytics hooks listen to these events.
// ============================================================
using System;

namespace GerakAR.Core
{
    /// <summary>
    /// Static event bus. Systems raise events here; audio, analytics,
    /// and UI systems subscribe without direct coupling.
    /// </summary>
    public static class GerakAREvents
    {
        // ── AR Tracking ──────────────────────────────────────────────

        /// <summary>
        /// Raised when a movement image target is first detected (toast starts).
        /// Payload: movementId (string).
        /// </summary>
        public static event Action<string> OnDetectionStarted;

        /// <summary>
        /// Raised when a movement image target is first detected.
        /// Payload: movementId (string).
        /// </summary>
        public static event Action<string> OnMovementDetected;

        /// <summary>
        /// Raised when the tracking of a movement is lost (after grace period).
        /// Payload: movementId (string).
        /// </summary>
        public static event Action<string> OnTrackingLost;

        // ── Animation ────────────────────────────────────────────────

        /// <summary>
        /// Raised when the loop animation starts or restarts.
        /// Payload: movementId (string).
        /// </summary>
        public static event Action<string> OnLoopStarted;

        /// <summary>
        /// Raised when the user touches the timeline and enters pose-inspection mode.
        /// Payload: normalizedTime (float) at the moment the slider was touched.
        /// </summary>
        public static event Action<float> OnPoseInspectionStarted;

        /// <summary>
        /// Raised when the user releases the timeline slider.
        /// </summary>
        public static event Action OnPoseInspectionEnded;

        // ── UI ───────────────────────────────────────────────────────

        /// <summary>
        /// Raised when the material bottom sheet opens.
        /// Payload: movementId (string) of the currently tracked movement.
        /// </summary>
        public static event Action<string> OnMaterialOpened;

        // ── Invoke helpers (call these from raising systems) ─────────

        public static void RaiseDetectionStarted(string movementId) =>
            OnDetectionStarted?.Invoke(movementId);

        public static void RaiseMovementDetected(string movementId) =>
            OnMovementDetected?.Invoke(movementId);

        public static void RaiseTrackingLost(string movementId) =>
            OnTrackingLost?.Invoke(movementId);

        public static void RaiseLoopStarted(string movementId) =>
            OnLoopStarted?.Invoke(movementId);

        public static void RaisePoseInspectionStarted(float normalizedTime) =>
            OnPoseInspectionStarted?.Invoke(normalizedTime);

        public static void RaisePoseInspectionEnded() =>
            OnPoseInspectionEnded?.Invoke();

        public static void RaiseMaterialOpened(string movementId) =>
            OnMaterialOpened?.Invoke(movementId);
    }
}
