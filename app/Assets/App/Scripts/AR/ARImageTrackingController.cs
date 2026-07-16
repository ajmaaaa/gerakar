using System.Collections;
using UnityEngine;
using GerakAR.Animation;
using GerakAR.Content;
using GerakAR.Core;
using GerakAR.UI;

namespace GerakAR.AR
{
    /// <summary>
    /// Adapts ARUnityX tracked-object events to the provider-independent
    /// GerakAR movement and UI lifecycle.
    /// </summary>
    public sealed class ARImageTrackingController : MonoBehaviour
    {
        [Header("ARUnityX Target")]
        [SerializeField] private ARXTrackable imageTarget;
        [SerializeField] private ARXTrackedObject trackedObject;
        [SerializeField] private string referenceImageName = "squat_target";

        [Header("Movement Presentation")]
        [SerializeField] private MovementDatabase movementDatabase;
        [SerializeField] private ModelPool modelPool;
        [SerializeField] private MovementController movementController;
        [SerializeField] private PoseTimelineController timelineController;
        [SerializeField] private MaterialContentController materialController;
        [SerializeField] private ARUIController uiController;

        [Header("Detection Feedback")]
        [SerializeField] [Range(0.6f, 3f)] private float confirmationDuration = 2.2f;

        private AppStateManager _stateManager;
        private MovementData _targetMovement;
        private Coroutine _detectionSequence;
        private bool _targetVisible;
        private bool _nonARPreviewStarted;

        private void OnEnable()
        {
            if (trackedObject != null)
            {
                trackedObject.OnTrackedObjectFound.AddListener(OnTargetFound);
                trackedObject.OnTrackedObjectLost.AddListener(OnTargetLost);
            }

            AppStateManager.OnStateChanged += OnStateChanged;
        }

        private void Start()
        {
            _stateManager = AppStateManager.Instance;
            _targetMovement = movementDatabase?.FindByReferenceImageName(referenceImageName);
        }

        private void OnDisable()
        {
            if (trackedObject != null)
            {
                trackedObject.OnTrackedObjectFound.RemoveListener(OnTargetFound);
                trackedObject.OnTrackedObjectLost.RemoveListener(OnTargetLost);
            }

            AppStateManager.OnStateChanged -= OnStateChanged;
            CancelDetection();
        }

        public void StartNonARPreview()
        {
            if (_nonARPreviewStarted) return;
            _nonARPreviewStarted = true;
            StartCoroutine(StartNonARPreviewNextFrame());
        }

        private IEnumerator StartNonARPreviewNextFrame()
        {
            yield return null;
            _stateManager = AppStateManager.Instance;
            string selectedMovementId = ActiveMovementContext.ActiveId;
            _targetMovement = !string.IsNullOrEmpty(selectedMovementId)
                ? movementDatabase?.FindById(selectedMovementId)
                : movementDatabase?.FindByReferenceImageName(referenceImageName);
            if (_targetMovement == null)
            {
                Debug.LogError($"[ARImageTrackingController] Movement data '{referenceImageName}' is missing.");
                _stateManager?.TransitionTo(AppState.NonARCatalog);
                yield break;
            }

            if (trackedObject != null)
            {
                trackedObject.transform.SetPositionAndRotation(
                    new Vector3(0f, -0.25f, 1.5f),
                    Quaternion.Euler(0f, 180f, 0f));
            }

            modelPool?.SetRootActive(true);
            PresentMovement(_targetMovement, AppState.NonARMovementPlayer);
        }

        private void OnTargetFound(Object trackedObjectEvent)
        {
            if (AppStateManager.RunInNonARMode || _targetVisible) return;
            _targetVisible = true;
            _targetMovement ??= movementDatabase?.FindByReferenceImageName(referenceImageName);

            if (_targetMovement == null)
            {
                Debug.LogWarning($"[ARImageTrackingController] No movement maps to '{referenceImageName}'.");
                return;
            }

            CancelDetection();
            _detectionSequence = StartCoroutine(ConfirmAndPresent(_targetMovement));
        }

        private IEnumerator ConfirmAndPresent(MovementData movement)
        {
            modelPool?.HideActive();
            _stateManager?.TransitionTo(AppState.TargetConfirmed);
            GerakAREvents.RaiseDetectionStarted(movement.movementId);

            yield return new WaitForSeconds(confirmationDuration);

            if (_targetVisible && _stateManager != null && _stateManager.Is(AppState.TargetConfirmed))
                PresentMovement(movement, AppState.TrackingLoop);

            _detectionSequence = null;
        }

        private void PresentMovement(MovementData movement, AppState presentationState)
        {
            ActiveMovementContext.ActiveId = movement.movementId;
            ActiveMovementContext.ActiveData = movement;

            modelPool?.SetRootActive(true);
            GameObject model = modelPool?.Activate(movement);
            if (model != null)
            {
                movementController?.Attach(model, movement);
                movementController?.StartLoop();
            }

            timelineController?.SetMovementData(movement);
            materialController?.SetMovement(movement);
            uiController?.SetMovementName(movement.displayName);

            GerakAREvents.RaiseMovementDetected(movement.movementId);
            _stateManager?.TransitionTo(presentationState);
        }

        private void OnTargetLost(Object trackedObjectEvent)
        {
            if (AppStateManager.RunInNonARMode) return;
            _targetVisible = false;
            CancelDetection();

            if (_targetMovement != null && modelPool?.ActiveMovementId == _targetMovement.movementId)
                GerakAREvents.RaiseTrackingLost(_targetMovement.movementId);

            movementController?.CancelReturnTimer();
            modelPool?.HideActive();
            ActiveMovementContext.Clear();
            _stateManager?.TransitionTo(AppState.Scanning);
        }

        private void OnStateChanged(AppState previous, AppState next)
        {
            if (next != AppState.Scanning || previous == AppState.TrackingLost)
                return;

            CancelDetection();
            movementController?.CancelReturnTimer();
            modelPool?.HideActive();
            ActiveMovementContext.Clear();
        }

        private void CancelDetection()
        {
            if (_detectionSequence == null) return;
            StopCoroutine(_detectionSequence);
            _detectionSequence = null;
        }
    }
}
