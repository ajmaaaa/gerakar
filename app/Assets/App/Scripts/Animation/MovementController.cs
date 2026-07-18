// ============================================================
// GerakAR – MovementController.cs
// Controls the Animator on the active model.
//
// Loop mode  : animator.speed = 1, clip plays from beginning.
// Inspect mode: animator.speed = 0, slider drives normalizedTime.
// After slider release:
//   1. Hold selected pose for holdDuration seconds.
//   2. Blend back to pose 0 over blendDuration seconds.
//   3. Return to loop mode.
// ============================================================
using System.Collections;
using UnityEngine;
using GerakAR.Core;
using GerakAR.Content;

namespace GerakAR.Animation
{
    /// <summary>
    /// Attached (or addressed) to the model root. Receives MovementData
    /// and controls the Animator via speed + normalizedTime approach.
    /// </summary>
    public class MovementController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────

        [Header("Timing")]
        [Tooltip("Seconds the selected pose is held after the slider is released.")]
        [SerializeField] [Range(1f, 5f)] private float holdDuration = 2.5f;

        [Tooltip("Seconds to blend from the selected pose back to the start.")]
        [SerializeField] [Range(0.1f, 1f)] private float blendDuration = 0.4f;

        // ── Runtime references ────────────────────────────────────────

        private Animator _animator;
        private AppStateManager _stateMgr;

        // Cache the hash of the single animation state in the controller
        private int _stateHash;
        private float _currentNormalizedTime;
        private Coroutine _returnToLoopCo;
        private AppState _inspectionReturnState = AppState.TrackingLoop;

        public bool CanInspect => _animator != null;

        public float CurrentNormalizedTime
        {
            get
            {
                if (_animator == null) return 0f;
                if (_stateMgr != null && _stateMgr.Is(AppState.InspectingPose))
                    return _currentNormalizedTime;
                
                var info = _animator.GetCurrentAnimatorStateInfo(0);
                return info.normalizedTime % 1f;
            }
        }

        // ── Setup ─────────────────────────────────────────────────────

        private void Awake()
        {
            _stateMgr = AppStateManager.Instance;
        }

        /// <summary>
        /// Called by <see cref="AR.ARImageTrackingController"/> (via
        /// <see cref="GerakAREvents.OnMovementDetected"/>) to attach this
        /// controller to a specific model's Animator.
        /// </summary>
        public void Attach(GameObject modelGo, MovementData data)
        {
            _animator = modelGo.GetComponentInChildren<Animator>();

            if (_animator == null)
            {
                Debug.LogWarning($"[MovementController] No Animator found on '{modelGo.name}'.");
                return;
            }

            if (data.animationClip != null)
            {
                // Override the clip in the controller's first state if using
                // AnimatorOverrideController. For the placeholder we skip this.
                var overrideController = _animator.runtimeAnimatorController
                    as AnimatorOverrideController;
                if (overrideController != null)
                {
                    // Assumes a single overrideable slot named "MovementClip"
                    overrideController["MovementClip"] = data.animationClip;
                }
            }

            // Grab the hash of layer 0's current state
            _stateHash = _animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
        }

        // ── Loop mode ─────────────────────────────────────────────────

        /// <summary>Start the loop from the beginning of the clip.</summary>
        public void StartLoop()
        {
            if (_animator == null) return;

            CancelReturnTimer();

            _animator.speed = 1f;
            _animator.Play(_stateHash, 0, 0f);
            _currentNormalizedTime = 0f;

            if (ActiveMovementContext.ActiveId != null)
                GerakAREvents.RaiseLoopStarted(ActiveMovementContext.ActiveId);
        }

        /// <summary>Pause or resume the loop (used when bottom sheet opens/closes).</summary>
        public void SetLoopPaused(bool paused)
        {
            if (_animator == null) return;
            _animator.speed = paused ? 0f : 1f;
        }

        // ── Inspect / scrub mode ──────────────────────────────────────

        /// <summary>
        /// Called by <see cref="UI.PoseTimelineController"/> when the
        /// user first touches the slider handle.
        /// </summary>
        public void BeginInspect(float normalizedTime, AppState returnState = AppState.TrackingLoop)
        {
            if (_animator == null) return;

            CancelReturnTimer();
            _inspectionReturnState = returnState;

            _animator.speed = 0f;
            ScrubTo(normalizedTime);

            GerakAREvents.RaisePoseInspectionStarted(normalizedTime);
        }

        /// <summary>
        /// Update the pose while the user drags the slider.
        /// Called every frame during a drag.
        /// </summary>
        public void ScrubTo(float normalizedTime)
        {
            if (_animator == null) return;
            _currentNormalizedTime = Mathf.Clamp01(normalizedTime);
            _animator.Play(_stateHash, 0, _currentNormalizedTime);
            _animator.Update(0f); // Force the animator to apply the pose immediately
        }

        /// <summary>
        /// Called by <see cref="UI.PoseTimelineController"/> when the
        /// user releases the slider. Begins the hold → blend → loop sequence.
        /// </summary>
        public void EndInspect()
        {
            if (_animator == null) return;

            GerakAREvents.RaisePoseInspectionEnded();
            _returnToLoopCo = StartCoroutine(ReturnToLoopSequence());
        }

        // ── Cancel helpers ────────────────────────────────────────────

        /// <summary>
        /// Cancel any in-progress return-to-loop sequence.
        /// Call when tracking is lost or when a new target is found.
        /// </summary>
        public void CancelReturnTimer()
        {
            if (_returnToLoopCo != null)
            {
                StopCoroutine(_returnToLoopCo);
                _returnToLoopCo = null;
            }
        }

        // ── Coroutine: hold → blend → loop ───────────────────────────

        private IEnumerator ReturnToLoopSequence()
        {
            // 1. Hold the selected pose
            yield return new WaitForSeconds(holdDuration);

            if (!ShouldContinueReturn()) yield break;

            // 2. Smooth blend from current normalizedTime → 0
            float startTime = _currentNormalizedTime;
            float elapsed = 0f;

            while (elapsed < blendDuration)
            {
                if (!ShouldContinueReturn()) yield break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / blendDuration);
                float lerped = Mathf.Lerp(startTime, 0f, t);
                _animator.Play(_stateHash, 0, lerped);
                _animator.Update(0f);
                yield return null;
            }

            if (!ShouldContinueReturn()) yield break;

            // 3. Resume loop
            _stateMgr?.TransitionTo(_inspectionReturnState);
            StartLoop();
            _returnToLoopCo = null;
        }

        private bool ShouldContinueReturn()
        {
            if (_animator == null) return false;
            // Abort if state changed away from InspectingPose
            // (e.g. tracking lost, material opened, new target)
            if (_stateMgr == null) return true;
            return _stateMgr.IsAny(AppState.InspectingPose, _inspectionReturnState);
        }
    }

}
