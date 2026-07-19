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
        [SerializeField] [Min(48f)] private float dragHandleHeight = 72f;

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
        private float _lastParentHeight = -1f;
        private bool _draggingSheet;
        private ScrollRect _contentScrollRect;

        // ── Unity lifecycle ───────────────────────────────────────────

        private void Awake()
        {
            NormalizeSheetChrome();
            RecalculateSnapPoints();
        }

        private void Start()
        {
            _stateMgr = AppStateManager.Instance;
            AppStateManager.OnStateChanged += OnAppStateChanged;

            Canvas.ForceUpdateCanvases();
            RecalculateSnapPoints();
            UpdateContentViewport(TargetY(_state));

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
            _lastParentHeight = _screenHeight;
            _closedY = -(sheetRect != null ? sheetRect.rect.height : 0f);
            _halfY   = _screenHeight * halfFraction;
            _fullY   = _screenHeight * fullFraction;
        }

        // ── Public API ────────────────────────────────────────────────

        /// <summary>Open material at the compact reading height first.</summary>
        public void Open()
        {
            ResetScrollPosition();
            SnapTo(SheetState.Half);
        }

        /// <summary>Close the sheet.</summary>
        public void CloseSheet()
        {
            SnapTo(SheetState.Closed);
        }

        public SheetState State => _state;

        public void ApplyRuntimeLayout()
        {
            NormalizeSheetChrome();
            RecalculateSnapPoints();
        }

        // ── Drag handlers ─────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData e)
        {
            _draggingSheet = IsInSheetDragArea(e);
            if (!_draggingSheet)
                return;

            StopAllCoroutines();
            RecalculateSnapPoints();
            _dragStartY = e.position.y;
            _sheetStartAnchoredY = sheetRect.anchoredPosition.y;
        }

        public void OnDrag(PointerEventData e)
        {
            if (!_draggingSheet || sheetRect == null) return;
            float delta = e.position.y - _dragStartY;
            float newY = Mathf.Clamp(_sheetStartAnchoredY + delta, _closedY, _fullY);
            sheetRect.anchoredPosition = new Vector2(sheetRect.anchoredPosition.x, newY);
            UpdateContentViewport(newY);
            UpdateScrim(newY);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!_draggingSheet)
                return;
            _draggingSheet = false;

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

        private bool IsInSheetDragArea(PointerEventData eventData)
        {
            if (sheetRect == null ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    sheetRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
                return false;

            return localPoint.y >= -dragHandleHeight;
        }

        // ── Snap animation ────────────────────────────────────────────

        private void SnapTo(SheetState state)
        {
            StopAllCoroutines();
            RecalculateSnapPoints();
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
            UpdateContentViewport(TargetY(state));
            Debug.Log($"[BottomSheetController] Snap {state}: parentHeight={_screenHeight:F1}, targetY={TargetY(state):F1}.");
            StartCoroutine(SnapCoroutine(TargetY(state)));
        }

        private void SnapImmediate(SheetState state)
        {
            RecalculateSnapPoints();
            _state = state;
            if (sheetRect != null)
            {
                float y = TargetY(state);
                sheetRect.anchoredPosition = new Vector2(sheetRect.anchoredPosition.x, y);
                UpdateContentViewport(y);
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
                UpdateContentViewport(y);
                UpdateScrim(y);
                yield return null;
            }

            sheetRect.anchoredPosition = new Vector2(sheetRect.anchoredPosition.x, targetY);
            UpdateContentViewport(targetY);
        }

        private float TargetY(SheetState state) => state switch
        {
            SheetState.Closed => _closedY,
            SheetState.Half   => _halfY,
            SheetState.Full   => _fullY,
            _ => _closedY
        };

        private void OnRectTransformDimensionsChange()
        {
            if (!isActiveAndEnabled || sheetRect == null)
                return;

            var parentRT = transform.parent as RectTransform;
            float parentHeight = parentRT != null ? parentRT.rect.height : Screen.height;
            if (Mathf.Approximately(parentHeight, _lastParentHeight))
                return;

            RecalculateSnapPoints();
            if (Application.isPlaying)
            {
                float targetY = TargetY(_state);
                sheetRect.anchoredPosition = new Vector2(sheetRect.anchoredPosition.x, targetY);
                UpdateContentViewport(targetY);
                UpdateScrim(targetY);
            }
        }

        private void NormalizeSheetChrome()
        {
            Canvas rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (rootCanvas != null && transform.parent != rootCanvas.transform)
            {
                transform.SetParent(rootCanvas.transform, false);
                RectTransform rect = sheetRect != null ? sheetRect : transform as RectTransform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(0f, rect.sizeDelta.y);
            }

            if (scrim != null && transform.parent != null)
            {
                RectTransform scrimRect = scrim.transform as RectTransform;
                if (scrim.transform.parent != transform.parent)
                    scrim.transform.SetParent(transform.parent, false);
                scrimRect.anchorMin = Vector2.zero;
                scrimRect.anchorMax = Vector2.one;
                scrimRect.offsetMin = Vector2.zero;
                scrimRect.offsetMax = Vector2.zero;
                scrim.transform.SetSiblingIndex(transform.GetSiblingIndex());
                transform.SetAsLastSibling();

                Image scrimImage = scrim.GetComponent<Image>();
                if (scrimImage != null)
                {
                    Color color = scrimImage.color;
                    color.a = 0f;
                    scrimImage.color = color;
                    scrimImage.raycastTarget = true;
                }
            }

            Transform handle = transform.Find("GrabHandle");
            if (handle != null)
            {
                RectTransform rect = handle as RectTransform;
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, -10f);
                rect.sizeDelta = new Vector2(40f, 4f);

                Image image = handle.GetComponent<Image>();
                if (image != null)
                {
                    image.color = new Color32(31, 93, 66, 255);
                    image.raycastTarget = false;
                }
            }

            UIRuntimeStyler.NormalizeCloseButton(closeButton);
        }

        private void ResetScrollPosition()
        {
            ScrollRect scrollRect = FindContentScrollRect();
            if (scrollRect == null)
                return;

            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 1f;
        }

        private ScrollRect FindContentScrollRect()
        {
            if (_contentScrollRect != null)
                return _contentScrollRect;

            foreach (ScrollRect candidate in GetComponentsInChildren<ScrollRect>(true))
            {
                if (!candidate.vertical)
                    continue;
                _contentScrollRect = candidate;
                break;
            }
            return _contentScrollRect;
        }

        private void UpdateContentViewport(float visibleHeight)
        {
            ScrollRect scrollRect = FindContentScrollRect();
            RectTransform rect = scrollRect != null ? scrollRect.transform as RectTransform : null;
            if (rect == null)
                return;
 
            const float topInset = 80f;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -topInset);
            rect.sizeDelta = new Vector2(0f, Mathf.Max(0f, visibleHeight - topInset)); // No side delta subtraction (was -40f, -bottomInset)
        }

        // ── Scrim ─────────────────────────────────────────────────────

        private void SetScrimVisible(bool visible)
        {
            if (scrim != null) scrim.SetActive(visible);
        }

        private void UpdateScrim(float sheetY)
        {
            if (scrim == null) return;
            var img = scrim.GetComponent<Image>();
            if (img != null)
            {
                var c = img.color;
                c.a = 0f;
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
