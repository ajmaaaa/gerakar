using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GerakAR.UI
{
    public class CollapsibleWarningToggle : MonoBehaviour
    {
        [SerializeField] private GameObject expandedContent;
        [SerializeField] private TextMeshProUGUI chevronText;

        private bool _isExpanded;
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null)
                _button.onClick.AddListener(ToggleExpand);

            if (expandedContent != null)
                expandedContent.SetActive(false);
        }

        private void ToggleExpand()
        {
            _isExpanded = !_isExpanded;
            if (expandedContent != null)
                expandedContent.SetActive(_isExpanded);

            if (chevronText != null)
                chevronText.text = _isExpanded ? "▲" : "▼";
        }
    }
}
