using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI
{
    public class TooltipManager : MonoBehaviour
    {
        public static TooltipManager Instance;

        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private TextMeshProUGUI tooltipText;
        [SerializeField] private Vector3 offset = new Vector3(-150, -20, 0);
        [SerializeField] private float showDelay = 0.5f;
        [SerializeField] private Vector2 padding = new Vector2(24f, 16f);

        private float hoverTimer;
        private bool waitingToShow;
        private string pendingDescription;

        private RectTransform panelRect;
        private Vector2 defaultPanelSize;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            panelRect = tooltipPanel.GetComponent<RectTransform>();
            defaultPanelSize = panelRect.sizeDelta;

            tooltipPanel.SetActive(false);
        }

        private void Update()
        {
            if (SettingsManager.Instance.AllowTooltips == false)
            {
                if (tooltipPanel.activeInHierarchy)
                    HideTooltip();
                return;
            }

            if (waitingToShow)
            {
                hoverTimer += Time.unscaledDeltaTime;
                if (hoverTimer >= showDelay)
                {
                    tooltipPanel.SetActive(true);
                    tooltipText.text = pendingDescription;

                    ResizeTooltipToFitText();

                    waitingToShow = false;
                }
            }

            if (tooltipPanel.activeInHierarchy)
                tooltipPanel.transform.position = Input.mousePosition + offset;
        }

        public void ShowTooltip(string description)
        {
            pendingDescription = description;
            hoverTimer = 0f;
            waitingToShow = true;
        }

        public void HideTooltip()
        {
            tooltipPanel.SetActive(false);
            waitingToShow = false;
            hoverTimer = 0f;

            // Reset to default size so next tooltip starts clean
            panelRect.sizeDelta = defaultPanelSize;
        }

        private void ResizeTooltipToFitText()
        {
            // Force TMP to update its internal layout data
            tooltipText.ForceMeshUpdate();

            float requiredWidth = tooltipText.preferredWidth + padding.x;
            float requiredHeight = tooltipText.preferredHeight + padding.y;

            float finalWidth = Mathf.Max(defaultPanelSize.x, requiredWidth);
            float finalHeight = Mathf.Max(defaultPanelSize.y, requiredHeight);

            panelRect.sizeDelta = new Vector2(finalWidth, finalHeight);
        }
    }
}
