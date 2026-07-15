// ============================================================
// GerakAR – ARImageTrackingController.cs
// Listens to ARTrackedImageManager events and drives the
// ModelPool, AppStateManager, and GerakAREvents accordingly.
// ============================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using GerakAR.Core;
using GerakAR.Content;
using GerakAR.UI;

namespace GerakAR.AR
{
    /// <summary>
    /// Receives Added / Updated / Removed events from
    /// <see cref="ARTrackedImageManager"/> and maps them to
    /// application state transitions and model lifecycle calls.
    ///
    /// Grace period: when tracking is lost or limited, the model
    /// is NOT hidden immediately. A coroutine waits
    /// <see cref="trackingLostGracePeriod"/> seconds before hiding.
    /// If the target is re-found during that window the model stays.
    /// </summary>
    [RequireComponent(typeof(ARTrackedImageManager))]
    public class ARImageTrackingController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private MovementDatabase movementDatabase;
        [SerializeField] private ModelPool modelPool;

        [Header("Tracking")]
        [Tooltip("Seconds before hiding the model after tracking is lost/limited.")]
        [SerializeField] [Range(0.2f, 2f)] private float trackingLostGracePeriod = 0.75f;

        // ── Private state ─────────────────────────────────────────────

        private ARTrackedImageManager _manager;
        private AppStateManager _stateMgr;

        // Key: tracking id → current ARTrackedImage
        private readonly Dictionary<TrackableId, ARTrackedImage> _trackedImages = new();

        // Grace coroutine handle (only one at a time; we track the single active model)
        private Coroutine _graceCo;

        // ── Unity lifecycle ───────────────────────────────────────────

        private void Awake()
        {
            _manager = GetComponent<ARTrackedImageManager>();
        }

        private void Start()
        {
            _stateMgr = AppStateManager.Instance;
            
            if (AppStateManager.RunInNonARMode)
            {
                var session = FindAnyObjectByType<ARSession>();
                if (session != null) session.enabled = false;
                if (_manager != null) _manager.enabled = false;

                var cam = Camera.main;
                if (cam != null)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0.12f, 0.17f, 0.15f, 1f); // #1E2B26 (Solid dark teal)
                }

                StartCoroutine(SpawnNonARModelDelayed());
            }
        }

        private IEnumerator SpawnNonARModelDelayed()
        {
            yield return new WaitForSeconds(0.5f);
            if (movementDatabase != null && movementDatabase.movements != null && movementDatabase.movements.Count > 0)
            {
                var squatData = movementDatabase.movements[0];
                GameObject model = modelPool.Activate(squatData);
                if (model != null)
                {
                    ActiveMovementContext.ActiveId = squatData.movementId;
                    ActiveMovementContext.ActiveData = squatData;
                    model.transform.position = new Vector3(0f, -0.6f, 1.8f);
                    model.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Face the camera
                }
                
                _stateMgr.TransitionTo(AppState.TrackingLoop);
                
                var uiCtrl = FindAnyObjectByType<ARUIController>();
                if (uiCtrl != null)
                {
                    uiCtrl.SetMovementName(squatData.displayName);
                }
            }
        }

        private void OnEnable()
        {
            _manager.trackablesChanged.AddListener(OnTrackablesChanged);
        }

        private void OnDisable()
        {
            _manager.trackablesChanged.RemoveListener(OnTrackablesChanged);
        }

        // ── Event handler ─────────────────────────────────────────────

        private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
        {
            // Added
            foreach (var img in args.added)
            {
                _trackedImages[img.trackableId] = img;
                HandleImageActive(img);
            }

            // Updated
            foreach (var img in args.updated)
            {
                _trackedImages[img.trackableId] = img;
                if (img.trackingState == TrackingState.Tracking)
                    HandleImageActive(img);
                else
                    HandleImageLost(img);
            }

            // Removed
            foreach (var img in args.removed)
            {
                _trackedImages.Remove(img.Key);
                HandleImageLost(img.Value);
            }
        }

        private Coroutine _detectionCo;

        private void HandleImageActive(ARTrackedImage img)
        {
            // Cancel any pending hide
            if (_graceCo != null)
            {
                StopCoroutine(_graceCo);
                _graceCo = null;
            }

            string imageName = img.referenceImage.name;
            MovementData data = movementDatabase?.FindByReferenceImageName(imageName);

            if (data == null)
            {
                Debug.LogWarning($"[ARImageTrackingController] No MovementData for '{imageName}'");
                return;
            }

            // If this is a different target than what's active, close material sheet
            bool isNewTarget = modelPool.ActiveMovementId != data.movementId;
            if (isNewTarget && _stateMgr.Is(AppState.ShowingMaterial))
            {
                _stateMgr.TransitionTo(AppState.Scanning);
            }

            // Start detection flow if it is a new target or we were not in tracking loop
            if (isNewTarget || _stateMgr.Is(AppState.Scanning) || _stateMgr.Is(AppState.TrackingLost))
            {
                if (_detectionCo == null)
                {
                    _detectionCo = StartCoroutine(DetectionSequence(img, data));
                }
            }
            else
            {
                // Normal frame update only if not currently in detection toast
                if (_detectionCo == null)
                {
                    modelPool.UpdateAnchor(img.transform);
                }
            }
        }

        private IEnumerator DetectionSequence(ARTrackedImage img, MovementData data)
        {
            // Transition to TargetConfirmed state and notify UI
            _stateMgr.TransitionTo(AppState.TargetConfirmed);
            GerakAREvents.RaiseDetectionStarted(data.movementId);
            modelPool.HideActive();

            // Show green checkmark toast for 1.2 seconds
            yield return new WaitForSeconds(1.2f);

            // Double check we are still in TargetConfirmed state (not canceled by tracking lost)
            if (_stateMgr.Is(AppState.TargetConfirmed))
            {
                ActiveMovementContext.ActiveId = data.movementId;
                ActiveMovementContext.ActiveData = data;
                GameObject model = modelPool.Activate(data);
                modelPool.UpdateAnchor(img.transform);

                GerakAREvents.RaiseMovementDetected(data.movementId);
                _stateMgr.TransitionTo(AppState.TrackingLoop);
            }

            _detectionCo = null;
        }

        // ── Lost tracking ─────────────────────────────────────────────

        private void HandleImageLost(ARTrackedImage img)
        {
            // Cancel active detection sequence
            if (_detectionCo != null)
            {
                StopCoroutine(_detectionCo);
                _detectionCo = null;
            }

            string imageName = img.referenceImage.name;
            MovementData data = movementDatabase?.FindByReferenceImageName(imageName);
            if (data == null) return;

            // Only react if this is the currently active movement
            if (modelPool.ActiveMovementId != data.movementId) return;

            // Transition to TrackingLost if we are in a live state or detecting
            if (_stateMgr.IsAny(AppState.TrackingLoop, AppState.InspectingPose, AppState.ShowingMaterial, AppState.TargetConfirmed))
            {
                _stateMgr.TransitionTo(AppState.TrackingLost);
            }

            // Start grace period if not already running
            if (_graceCo == null)
            {
                _graceCo = StartCoroutine(TrackingLostGrace(data));
            }
        }

        private IEnumerator TrackingLostGrace(MovementData data)
        {
            yield return new WaitForSeconds(trackingLostGracePeriod);

            // After grace period: if still not tracking, hide model
            if (_stateMgr.Is(AppState.TrackingLost))
            {
                GerakAREvents.RaiseTrackingLost(data.movementId);
                modelPool.HideActive();
                ActiveMovementContext.Clear();
                _stateMgr.TransitionTo(AppState.Scanning);
            }

            _graceCo = null;
        }

        // ── Continuous anchor update ──────────────────────────────────

        private void LateUpdate()
        {
            // Keep active model locked to the image transform every frame
            if (modelPool.ActiveMovementId == null) return;

            foreach (var kvp in _trackedImages)
            {
                var img = kvp.Value;
                if (img.trackingState != TrackingState.Tracking) continue;

                MovementData data = movementDatabase?.FindByReferenceImageName(img.referenceImage.name);
                if (data == null) continue;
                if (data.movementId != modelPool.ActiveMovementId) continue;

                modelPool.UpdateAnchor(img.transform);
                break;
            }
        }
    }
}
