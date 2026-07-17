// ============================================================
// GerakAR – BottomSheetController.cs
// Draggable bottom sheet with three snap points:
//   Closed  → panel completely below screen
//   Half    → ~48% of screen height (default material state)
//   Full    → ~94% of screen height
//
// The sheet can be opened from the material FAB, dragged, and
// closed by dragging down, tapping scrim, or pressing the close icon.
// ============================================================
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using GerakAR.Core;
using GerakAR.Animation;

namespace GerakAR.UI
{
    /// <summary>
    /// Three-state draggable bottom sheet.
    /// Attach to the root RectTransform of the sheet panel.
    /// </summary>
    public class BottomSheetController : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // ── Snap point config ─────────────────────────────────────────

        public enum SheetState { Closed, Half, Full }

        [Header("Snap Heights (fraction of screen height, 0-1)")]
        [SerializeField] [Range(0.3f, 0.7f)] private float halfFraction = 0.48f;
        [SerializeField] [Range(0.7f, 1f)] private float fullFraction  = 0.94f;

        [Header("Animation")]
        [SerializeField] [Range(0.1f, 0.4f)] private float snapDuration = 0.22f;

        [Header("References")]
        [SerializeField] private RectTransform sheetRect;
        [SerializeField] private GameObject scrim;
        [SerializeField] private Button closeButton;
        [SerializeField] private MovementController movementController;

        // ── Events ────────────────────────────────────────────────────

        public static event Action<SheetState> OnSheetStateChanged;

        // ── Private state ─────────────────────────────────────────────

        private SheetState _state = SheetState.Closed;
        private AppStateManager _stateMgr;

        private float _screenHeight;
        private float _dragStartY;
        private float _sheetStartAnchoredY;

        // Cache target Y positions (anchoredPosition.y of sheetRect)
        private float _closedY, _halfY, _fullY;
        private AppState _returnState = AppState.TrackingLoop;

        // ── Unity lifecycle ───────────────────────────────────────────

        private void Awake()
        {
            _screenHeight = Screen.height;
            RecalculateSnapPoints();
        }

        private void Start()
        {
            _stateMgr = AppStateManager.Instance;
            AppStateManager.OnStateChanged += OnAppStateChanged;

            closeButton?.onClick.AddListener(CloseSheet);
            if (scrim != null)
            {
                var scrimBtn = scrim.GetComponent<Button>();
                scrimBtn?.onClick.AddListener(CloseSheet);
            }

            // Start closed
            SnapImmediate(SheetState.Closed);
        }

        private void OnDestroy()
        {
            AppStateManager.OnStateChanged -= OnAppStateChanged;
        }

        // ── Snap points ───────────────────────────────────────────────

        private void RecalculateSnapPoints()
        {
            var parentRT = transform.parent as RectTransform;
            _screenHeight = parentRT != null ? parentRT.rect.height : Screen.height;
            _closedY = -(sheetRect != null ? sheetRect.rect.height : 0f);
            _halfY   = _screenHeight * halfFraction;
            _fullY   = _screenHeight * fullFraction;
        }

        // ── Public API ────────────────────────────────────────────────

        /// <summary>Open material directly to full height (~94%).</summary>
        public void Open() => SnapTo(SheetState.Full);

        /// <summary>Close the sheet.</summary>
        public void CloseSheet()
        {
            SnapTo(SheetState.Closed);
        }

        public SheetState State => _state;

        // ── Drag handlers ─────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData e)
        {
            StopAllCoroutines();
            _dragStartY = e.position.y;
            _sheetStartAnchoredY = sheetRect.anchoredPosition.y;
        }

        public void OnDrag(PointerEventData e)
        {
            if (sheetRect == null) return;
            float delta = e.position.y - _dragStartY;
            float newY = Mathf.Clamp(_sheetStartAnchoredY + delta, _closedY, _fullY);
            sheetRect.anchoredPosition = new Vector2(sheetRect.anchoredPosition.x, newY);
            UpdateScrim(newY);
        }

        public void OnEndDrag(PointerEventData e)
        {
            float currentY = sheetRect.anchoredPosition.y;
            float velocity = e.delta.y; // positive = dragging up

            SheetState target;
            if (velocity < -50f) // fast downward flick → close
                target = currentY > _halfY ? SheetState.Half : SheetState.Closed;
            else if (velocity > 50f) // fast upward flick → full
                target = currentY < _halfY ? SheetState.Half : SheetState.Full;
            else
            {
                // Snap to nearest
                float distClosed = Mathf.Abs(currentY - _closedY);
                float distHalf   = Mathf.Abs(currentY - _halfY);
                float distFull   = Mathf.Abs(currentY - _fullY);

                if (distClosed <= distHalf && distClosed <= distFull)
                    target = SheetState.Closed;
                else if (distHalf <= distFull)
                    target = SheetState.Half;
                else
                    target = SheetState.Full;
            }

            SnapTo(target);
        }

        // ── Snap animation ────────────────────────────────────────────

        private void SnapTo(SheetState state)
        {
            _state = state;
            OnSheetStateChanged?.Invoke(state);

            // Pause/resume animation
            if (movementController != null)
                movementController.SetLoopPaused(state != SheetState.Closed);

            // Update AppState
            if (state == SheetState.Closed && _stateMgr != null &&
                _stateMgr.IsAny(AppState.ShowingMaterial, AppState.ShowingRelatedMaterial))
            {
                _stateMgr.TransitionTo(_returnState);
                movementController?.StartLoop();
            }

            SetScrimVisible(state != SheetState.Closed);
            StartCoroutine(SnapCoroutine(TargetY(state)));
        }

        private void SnapImmediate(SheetState state)
        {
            _state = state;
            if (sheetRect != null)
            {
                float y = TargetY(state);
                sheetRect.anchoredPosition = new Vector2(sheetRect.anchoredPosition.x, y);
            }
            SetScrimVisible(state != SheetState.Closed);
        }

        private System.Collections.IEnumerator SnapCoroutine(float targetY)
        {
            float startY = sheetRect.anchoredPosition.y;
            float elapsed = 0f;

            while (elapsed < snapDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / snapDuration);
                float y = Mathf.Lerp(startY, targetY, t);
                sheetRect.anchoredPosition = new Vector2(sheetRect.anchoredPosition.x, y);
                UpdateScrim(y);
                yield return null;
            }

            sheetRect.anchoredPosition = new Vector2(sheetRect.anchoredPosition.x, targetY);
        }

        private float TargetY(SheetState state) => state switch
        {
            SheetState.Closed => _closedY,
            SheetState.Half   => _halfY,
            SheetState.Full   => _fullY,
            _ => _closedY
        };

        // ── Scrim ─────────────────────────────────────────────────────

        private void SetScrimVisible(bool visible)
        {
            if (scrim != null) scrim.SetActive(visible);
        }

        private void UpdateScrim(float sheetY)
        {
            if (scrim == null) return;
            float t = Mathf.InverseLerp(_closedY, _fullY, sheetY);
            var img = scrim.GetComponent<Image>();
            if (img != null)
            {
                var c = img.color;
                c.a = Mathf.Lerp(0f, 0.45f, t);
                img.color = c;
            }
        }

        // ── App state bridge ──────────────────────────────────────────

        private void OnAppStateChanged(AppState prev, AppState next)
        {
            if (next == AppState.ShowingMaterial && _state == SheetState.Closed)
            {
                _returnState = prev == AppState.NonARMovementPlayer
                    ? AppState.NonARMovementPlayer
                    : AppState.TrackingLoop;
                Open();
            }

            if (next is AppState.Scanning or AppState.TrackingLost && _state != SheetState.Closed)
                SnapImmediate(SheetState.Closed);
        }
    }
}
