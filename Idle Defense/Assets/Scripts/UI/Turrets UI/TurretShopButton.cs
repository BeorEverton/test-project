using Assets.Scripts.SO;
using Assets.Scripts.Systems;
using Assets.Scripts.WaveSystem;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class TurretShopButton : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private TurretType turretType;
        [SerializeField] private Image icon;          // UI Image
        [SerializeField] private TextMeshProUGUI countText;     // “x3”
        [SerializeField] private TextMeshProUGUI costText;      // “$10k”
        //[SerializeField] private TextMeshProUGUI lockText;      // “Wave 30”
        [SerializeField] private TextMeshProUGUI dpsText;
        [SerializeField] private TextMeshProUGUI turretName;
        [SerializeField] private Image lockIcon;
        [SerializeField] private Button buyButton;


        private void Start()
        {
            buyButton.onClick.AddListener(TryBuy);
            GameManager.Instance.OnCurrencyChanged += HandleCurrencyChanged;

            turretName.SetText(ToSpacedString(turretType).ToUpper());

            TurretInventoryManager.Instance.OnInventoryChanged += Refresh;
            WaveManager.Instance.OnWaveStarted += (_, __) => Refresh();

            if (PrestigeManager.Instance != null)
                PrestigeManager.Instance.OnPrestigeChanged += Refresh;

            Refresh();
        }

        private void OnDestroy()
        {
            buyButton.onClick.RemoveListener(TryBuy);
            if (GameManager.Instance != null)
                GameManager.Instance.OnCurrencyChanged -= HandleCurrencyChanged;

            if (TurretInventoryManager.Instance != null)
                TurretInventoryManager.Instance.OnInventoryChanged -= Refresh;

            if (WaveManager.Instance != null)
                WaveManager.Instance.OnWaveStarted -= (_, __) => Refresh();

            if (PrestigeManager.Instance != null)
                PrestigeManager.Instance.OnPrestigeChanged -= Refresh;
        }

        private void Refresh()
        {
            TurretInventoryManager inv = TurretInventoryManager.Instance;
            int owned = inv.Owned.Count(o => o.TurretType == turretType);

            if (owned >= 5)
            {
                icon.color = Color.white;
                lockIcon.gameObject.SetActive(false);

                countText.text = $"x{owned}";
                costText.text = "Max";
                costText.color = Color.black;
                buyButton.interactable = false;
                dpsText.gameObject.SetActive(false);
                return;
            }

            // Prestige gate check (only for first acquisition)
            bool requiresPrestige = TurretInventoryManager.Instance.RequiresPrestigeUnlock(turretType);
            bool prestigeUnlocked = PrestigeManager.Instance != null && PrestigeManager.Instance.IsTurretUnlocked(turretType);

            if (owned == 0 && requiresPrestige && !prestigeUnlocked)
            {
                // Locked by prestige tree
                icon.color = Color.black;                // your locked visual
                lockIcon.gameObject.SetActive(true);

                countText.text = "x0";
                costText.text = "Unlock with Crimson Core";
                costText.color = Color.red;

                buyButton.interactable = false;
                dpsText.gameObject.SetActive(false);     // optional: hide DPS when locked
                return;
            }

            // otherwise: normal flow (either already own at least one, or prestige gate passed)
            icon.color = Color.white;                     // show as buyable
            lockIcon.gameObject.SetActive(false);


            // Continue with normal pricing
            (Currency currency, ulong cost) = inv.GetCostAndCurrency(turretType, owned);
            countText.text = $"x{owned}";
            costText.text = $"{UIManager.GetCurrencyIcon(currency)}{UIManager.AbbreviateNumber(cost)}";

            bool afford = GameManager.Instance.GetCurrency(currency) >= cost;

            buyButton.interactable = afford;
            costText.color = afford ? Color.black : Color.red;

            UpdateDPSDisplay();
            /* These controls unlocking the turrets based on wave number
            bool unlocked = inv.IsTurretTypeUnlocked(turretType);
            int curWave = WaveManager.Instance.GetCurrentWaveIndex();

            if (unlocked)
            {
                // unlocked state
                icon.color = Color.white;
                lockText.gameObject.SetActive(false);

                ulong cost = inv.GetCost(turretType, owned);
                countText.text = $"x{owned}";
                costText.text = $"${UIManager.AbbreviateNumber(cost)}";

                bool afford = GameManager.Instance.Money >= cost;
                buyButton.interactable = afford;
                costText.color = afford ? Color.black : Color.red;
            }
            else
            {
                // locked state
                icon.color = Color.black;
                countText.text = "";
                costText.text = "";

                int waveReq = inv.WaveRequirement(turretType);
                lockText.gameObject.SetActive(true);
                lockText.text = $"Wave {waveReq}";

                // only show price when wave reached
                if (curWave >= waveReq)
                    costText.text = $"${UIManager.AbbreviateNumber(inv.GetCost(turretType, 0))}";

                buyButton.interactable = false;
            }*/
        }

        private void UpdateDPSDisplay()
        {
            TurretInfoSO info = TurretInventoryManager.Instance.GetInfoSO(turretType);
            float dps = TurretStatsCalculator.CalculateDPS(info);
            string dpsPrefix = "";
            if (info.TurretType == TurretType.ObsidianLens || info.TurretType == TurretType.VaporJetTurret)
            {
                dpsPrefix = ">";
            }
            dpsText.text = dpsPrefix + $"{dps:F1} DPS";
        }

        private void TryBuy()
        {
            UIManager.Instance.ShowToast(TurretInventoryManager.Instance.TryPurchase(turretType)
                ? "Turret bought!"
                : "Need more coins");
            Refresh();
        }

        private void HandleCurrencyChanged(Currency changed, ulong _)
        {
            // Refresh only if relevant, or always for now
            Refresh();
        }

        private string ToSpacedString(TurretType type)
        {
            // Split PascalCase into words with spaces
            return Regex.Replace(type.ToString(), "(\\B[A-Z])", " $1");
        }

    }
}