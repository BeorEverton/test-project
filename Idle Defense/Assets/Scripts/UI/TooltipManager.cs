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

        private float hoverTimer;
        private bool waitingToShow;
        private string pendingDescription;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

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
        }
    }
}
