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
        private static readonly Color ForestGreen = new Color(0.12f, 0.365f, 0.259f, 1f); // #1F5D42
        private static readonly Color SecondaryText = new Color(0.443f, 0.376f, 0.251f, 1f);  // #716040
        // ── Inspector – Primary movement ──────────────────────────────

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI categoryTypeLabel;  // Shows "Gerakan Utama" or "Materi Tambahan"
        [SerializeField] private TextMeshProUGUI movementNameText;
        [SerializeField] private Image categoryAccentBar;
        [SerializeField] private TextMeshProUGUI movementSubtitleText;
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
        [SerializeField] private GameObject mistakesTitleText;
        [SerializeField] private GameObject trainedTitleText;
        [SerializeField] private GameObject bulletItemPrefab;       // TMP label
        [SerializeField] private GameObject muscleItemPrefab;       // Grid card label

        [Header("Related movements")]
        [SerializeField] private Transform relatedCardsContainer;
        [SerializeField] private GameObject relatedCardPrefab;

        // ── Private state ─────────────────────────────────────────────

        private MovementData _currentMovement;
        private readonly List<GameObject> _spawnedItems = new();
        private readonly List<GameObject> _spawnedCards = new();

        // ── Unity lifecycle ───────────────────────────────────────────

        private void Start()
        {
            UIRuntimeStyler.NormalizeMuscleContainer(trainedAreasContainer);
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
            UIRuntimeStyler.NormalizeMuscleContainer(trainedAreasContainer);
            _currentMovement = data;
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
            {
                categoryAccentBar.color = new Color(data.categoryColor.r, data.categoryColor.g, data.categoryColor.b, 0.12f);
            }
            if (categoryTypeLabel != null)
            {
                string categoryName = "GERAKAN UTAMA";
                if (data.movementId.Contains("squat")) categoryName = "GERAKAN UTAMA";
                else if (data.movementId.Contains("stretch") || data.movementId.Contains("dynamic")) categoryName = "DYNAMIC STRETCHING";
                else if (data.movementId.Contains("ladder")) categoryName = "LADDER DRILL";

                categoryTypeLabel.text = categoryName;
                categoryTypeLabel.color = SecondaryText; // Matches mockup precisely
            }
            if (movementSubtitleText != null)
            {
                string subtitle = "Latihan Olahraga SD";
                if (data.movementId.Contains("squat")) subtitle = "Latihan kekuatan kaki dan keseimbangan.";
                else if (data.movementId.Contains("stretch") || data.movementId.Contains("dynamic")) subtitle = "Latihan kelenturan dan pemanasan.";
                else if (data.movementId.Contains("ladder")) subtitle = "Latihan kelincahan dan koordinasi.";

                movementSubtitleText.text = subtitle;
            }

            // Description
            if (shortDescriptionText != null)
                shortDescriptionText.text = data.shortDescription;

            // Steps (max 3)
            int stepCount = Mathf.Min(data.steps?.Count ?? 0, 3);
            for (int i = 0; i < stepCount; i++)
                SpawnBullet(stepsContainer, $"{i + 1}. {data.steps[i]}", stepItemPrefab);

            // Safety tip (first one)
            if (safetyTipText != null && data.safetyTips?.Count > 0)
                safetyTipText.text = data.safetyTips[0];

            // Full-state extras
            if (mistakesTitleText != null) mistakesTitleText.SetActive(true);
            if (commonMistakesContainer != null) commonMistakesContainer.gameObject.SetActive(true);
            if (trainedTitleText != null) trainedTitleText.SetActive(true);
            if (trainedAreasContainer != null) trainedAreasContainer.gameObject.SetActive(true);

            foreach (var area in data.trainedAreas ?? new List<string>())
            {
                if (muscleItemPrefab != null)
                {
                    GameObject item = Instantiate(muscleItemPrefab, trainedAreasContainer);
                    UIRuntimeStyler.NormalizeMuscleItem(item);
                    var tmp = item.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmp != null) tmp.text = area;
                    _spawnedItems.Add(item);
                }
                else
                {
                    SpawnBullet(trainedAreasContainer, $"- {area}", bulletItemPrefab);
                }
            }
            foreach (var mistake in data.commonMistakes ?? new List<string>())
                SpawnBullet(commonMistakesContainer, $"- {mistake}", bulletItemPrefab);

            // Related movement cards
            PopulateRelatedCards(data.relatedMovements);
        }

        private void PopulateRelatedCards(List<RelatedMovementData> related)
        {
            ClearCards();
            if (relatedCardsContainer == null || relatedCardPrefab == null) return;
            if (related == null) return;

            RelatedMovementCardView.ConfigureContainer(relatedCardsContainer);

            foreach (var rel in related)
            {
                var card = Instantiate(relatedCardPrefab, relatedCardsContainer);
                _spawnedCards.Add(card);
                RelatedMovementCardView.Configure(card, rel);

                // Wire tap to show related movement inside the main sheet view
                var btn = card.GetComponent<Button>();
                var relCopy = rel; // capture for lambda
                btn?.onClick.AddListener(() => ViewRelatedMovement(relCopy));
            }
        }

        private void ViewRelatedMovement(RelatedMovementData rel)
        {
            AppStateManager.Instance?.TransitionTo(AppState.ShowingRelatedMaterial);
            if (backToPrimaryButton != null)
                backToPrimaryButton.gameObject.SetActive(false);
            if (categoryTypeLabel != null)
            {
                categoryTypeLabel.text = "MATERI TAMBAHAN";
                categoryTypeLabel.color = SecondaryText; // Matches mockup precisely
            }
            if (categoryAccentBar != null)
            {
                categoryAccentBar.color = new Color(ForestGreen.r, ForestGreen.g, ForestGreen.b, 0.12f);
            }
            if (movementSubtitleText != null)
            {
                movementSubtitleText.text = "Variasi gerakan latihan serupa.";
            }

            if (mistakesTitleText != null) mistakesTitleText.SetActive(false);
            if (commonMistakesContainer != null) commonMistakesContainer.gameObject.SetActive(false);
            if (trainedTitleText != null) trainedTitleText.SetActive(false);
            if (trainedAreasContainer != null) trainedAreasContainer.gameObject.SetActive(false);

            ClearDynamic();

            // Populate from related data
            if (movementNameText != null)
                movementNameText.text = rel.title;

            if (shortDescriptionText != null)
                shortDescriptionText.text = rel.shortDescription;

            // Steps
            int stepCount = Mathf.Min(rel.steps?.Count ?? 0, 3);
            for (int i = 0; i < stepCount; i++)
                SpawnBullet(stepsContainer, $"{i + 1}. {rel.steps[i]}", stepItemPrefab);

            // Safety tip
            if (safetyTipText != null && rel.safetyTips?.Count > 0)
                safetyTipText.text = rel.safetyTips[0];

            // Related movements (in related detail view, let's keep the related list visible at the bottom so they can tap other ones!)
        }

        private void RestorePrimaryContent()
        {
            if (_currentMovement != null)
            {
                SetMovement(_currentMovement);
                AppStateManager.Instance?.TransitionTo(AppState.ShowingMaterial);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────

        private void SpawnBullet(Transform parent, string text, GameObject prefab)
        {
            if (parent == null || prefab == null) return;
            var go = Instantiate(prefab, parent);
            _spawnedItems.Add(go);

            // Handle numbered steps
            int dotIndex = text.IndexOf('.');
            if (dotIndex > 0 && int.TryParse(text.Substring(0, dotIndex), out int num) && go.transform.Find("Badge") != null)
            {
                var numText = go.transform.Find("Badge/Text")?.GetComponent<TextMeshProUGUI>();
                if (numText != null) numText.text = num.ToString();

                var contentText = go.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
                if (contentText != null) contentText.text = text.Substring(dotIndex + 1).Trim();
            }
            else
            {
                // Strip bullet points if any
                string cleanText = text;
                if (cleanText.StartsWith("- "))
                    cleanText = cleanText.Substring(2);

                var contentText = go.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
                if (contentText != null)
                {
                    contentText.text = cleanText;
                }
                else
                {
                    var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmp != null) tmp.text = cleanText;
                }

                // Hide the icon badge if parent is the TrainedContainer or if text doesn't start with "- "
                var badgeGo = go.transform.Find("Badge")?.gameObject;
                bool isTrainedArea = parent.name == "TrainedContainer" || parent.name.Contains("Trained");
                if (badgeGo != null && (isTrainedArea || !text.StartsWith("- ")))
                {
                    badgeGo.SetActive(false);
                    var textRT = go.transform.Find("Text")?.GetComponent<RectTransform>();
                    if (textRT != null)
                    {
                        textRT.anchoredPosition = new Vector2(12f, 0f);
                        textRT.sizeDelta = new Vector2(-24f, 0f);
                    }
                }
            }
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
