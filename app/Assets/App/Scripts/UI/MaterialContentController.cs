// ============================================================
// GerakAR – MaterialContentController.cs
// Populates the bottom sheet with content from MovementData.
// Handles the half/full state display and related movement cards.
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GerakAR.Content;
using GerakAR.Core;

namespace GerakAR.UI
{
    /// <summary>
    /// Populates the bottom sheet UI from a <see cref="MovementData"/> asset.
    ///
    /// Half-state content:
    ///   - Movement name + accent color bar
    ///   - Short description
    ///   - Up to 3 steps
    ///   - 1 safety tip
    ///   - Related movement cards (horizontal scroll)
    ///
    /// Full-state content (extends half):
    ///   - Trained body areas
    ///   - Common mistakes
    ///   - Related movement cards (full list)
    ///
    /// When a related movement card is tapped, the sheet shows its
    /// detail view with a "Materi Tambahan" label and a back button.
    /// The main AR model is NOT replaced.
    /// </summary>
    public class MaterialContentController : MonoBehaviour
    {
        // ── Inspector – Primary movement ──────────────────────────────

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI categoryTypeLabel;  // Shows "Gerakan Utama" or "Materi Tambahan"
        [SerializeField] private TextMeshProUGUI movementNameText;
        [SerializeField] private Image categoryAccentBar;
        [SerializeField] private Button backToPrimaryButton;        // Button to return to main movement

        [Header("Half-state content")]
        [SerializeField] private TextMeshProUGUI shortDescriptionText;
        [SerializeField] private Transform stepsContainer;
        [SerializeField] private GameObject stepItemPrefab;         // TMP label
        [SerializeField] private TextMeshProUGUI safetyTipText;

        [Header("Full-state extras")]
        [SerializeField] private GameObject fullStateExtras;        // Parent GO
        [SerializeField] private Transform trainedAreasContainer;
        [SerializeField] private Transform commonMistakesContainer;
        [SerializeField] private GameObject bulletItemPrefab;       // TMP label

        [Header("Related movements")]
        [SerializeField] private Transform relatedCardsContainer;
        [SerializeField] private GameObject relatedCardPrefab;

        // ── Private state ─────────────────────────────────────────────

        private MovementData _currentMovement;
        private bool _isViewingRelated;
        private readonly List<GameObject> _spawnedItems = new();
        private readonly List<GameObject> _spawnedCards = new();

        // ── Unity lifecycle ───────────────────────────────────────────

        private void Start()
        {
            BottomSheetController.OnSheetStateChanged += OnSheetStateChanged;
            GerakAREvents.OnMovementDetected += OnMovementDetected;

            if (backToPrimaryButton != null)
            {
                backToPrimaryButton.onClick.AddListener(RestorePrimaryContent);
                backToPrimaryButton.gameObject.SetActive(false);
            }

            if (categoryTypeLabel != null)
                categoryTypeLabel.text = "Gerakan Utama";
        }

        private void OnDestroy()
        {
            BottomSheetController.OnSheetStateChanged -= OnSheetStateChanged;
            GerakAREvents.OnMovementDetected -= OnMovementDetected;
        }

        // ── Public API ────────────────────────────────────────────────

        /// <summary>Load content for a new movement (called by tracking controller bridge).</summary>
        public void SetMovement(MovementData data)
        {
            _currentMovement = data;
            _isViewingRelated = false;
            if (backToPrimaryButton != null)
                backToPrimaryButton.gameObject.SetActive(false);
            if (categoryTypeLabel != null)
                categoryTypeLabel.text = "Gerakan Utama";

            PopulatePrimary(data);
        }

        // ── Event handlers ────────────────────────────────────────────

        private void OnMovementDetected(string movementId)
        {
            // Content will be pushed via SetMovement from the tracking bridge
        }

        private void OnSheetStateChanged(BottomSheetController.SheetState state)
        {
            if (fullStateExtras != null)
                fullStateExtras.SetActive(state == BottomSheetController.SheetState.Full);
        }

        // ── Population ────────────────────────────────────────────────

        private void PopulatePrimary(MovementData data)
        {
            if (data == null) return;

            ClearDynamic();

            // Header
            if (movementNameText != null)
                movementNameText.text = data.displayName.ToUpper();
            if (categoryAccentBar != null)
                categoryAccentBar.color = data.categoryColor;

            // Description
            if (shortDescriptionText != null)
                shortDescriptionText.text = data.shortDescription;

            // Steps (max 3)
            int stepCount = Mathf.Min(data.steps?.Count ?? 0, 3);
            for (int i = 0; i < stepCount; i++)
                SpawnBullet(stepsContainer, $"{i + 1}. {data.steps[i]}", stepItemPrefab);

            // Safety tip (first one)
            if (safetyTipText != null && data.safetyTips?.Count > 0)
                safetyTipText.text = $"⚠ {data.safetyTips[0]}";

            // Full-state extras
            foreach (var area in data.trainedAreas ?? new List<string>())
                SpawnBullet(trainedAreasContainer, $"• {area}", bulletItemPrefab);
            foreach (var mistake in data.commonMistakes ?? new List<string>())
                SpawnBullet(commonMistakesContainer, $"• {mistake}", bulletItemPrefab);

            // Related movement cards
            PopulateRelatedCards(data.relatedMovements);
        }

        private void PopulateRelatedCards(List<RelatedMovementData> related)
        {
            ClearCards();
            if (relatedCardsContainer == null || relatedCardPrefab == null) return;
            if (related == null) return;

            foreach (var rel in related)
            {
                var card = Instantiate(relatedCardPrefab, relatedCardsContainer);
                _spawnedCards.Add(card);

                // Wire thumbnail
                var thumbImg = card.transform.Find("Thumbnail")?.GetComponent<Image>();
                if (thumbImg != null && rel.thumbnail != null)
                    thumbImg.sprite = rel.thumbnail;

                // Wire title
                var titleTmp = card.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
                if (titleTmp != null) titleTmp.text = rel.title;

                // Wire tap to show related movement inside the main sheet view
                var btn = card.GetComponent<Button>();
                var relCopy = rel; // capture for lambda
                btn?.onClick.AddListener(() => ViewRelatedMovement(relCopy));
            }
        }

        private void ViewRelatedMovement(RelatedMovementData rel)
        {
            _isViewingRelated = true;
            if (backToPrimaryButton != null)
                backToPrimaryButton.gameObject.SetActive(true);
            if (categoryTypeLabel != null)
                categoryTypeLabel.text = "Materi Tambahan";

            ClearDynamic();

            // Populate from related data
            if (movementNameText != null)
                movementNameText.text = rel.title.ToUpper();

            if (shortDescriptionText != null)
                shortDescriptionText.text = rel.shortDescription;

            // Steps
            int stepCount = Mathf.Min(rel.steps?.Count ?? 0, 3);
            for (int i = 0; i < stepCount; i++)
                SpawnBullet(stepsContainer, $"{i + 1}. {rel.steps[i]}", stepItemPrefab);

            // Safety tip
            if (safetyTipText != null && rel.safetyTips?.Count > 0)
                safetyTipText.text = $"⚠ {rel.safetyTips[0]}";

            // Related movements (in related detail view, let's keep the related list visible at the bottom so they can tap other ones!)
        }

        private void RestorePrimaryContent()
        {
            if (_currentMovement != null)
            {
                SetMovement(_currentMovement);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────

        private void SpawnBullet(Transform parent, string text, GameObject prefab)
        {
            if (parent == null || prefab == null) return;
            var go = Instantiate(prefab, parent);
            _spawnedItems.Add(go);
            var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = text;
        }

        private void ClearDynamic()
        {
            foreach (var go in _spawnedItems)
                if (go != null) Destroy(go);
            _spawnedItems.Clear();
        }

        private void ClearCards()
        {
            foreach (var card in _spawnedCards)
                if (card != null) Destroy(card);
            _spawnedCards.Clear();
        }
    }
}
