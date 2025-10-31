using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GunnerEquipButton : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text levelLabel;
    [SerializeField] private TMP_Text statusLabel;  // "Available", "On quest", "Equipped: Slot 2"
    [SerializeField] private Image iconImage;
    [SerializeField] private Button btn;
    [SerializeField] private TMPro.TMP_Text buyPriceText; // assign in prefab (or leave null to skip)
    [SerializeField] private GameObject currencyIconGO;    // assign your currency icon GO in prefab

    private string gunnerId;

    public void Init(GunnerSO so, GunnerRuntime runtime, System.Action<string> onClick)
    {
        gunnerId = so.GunnerId;

        if (nameLabel) nameLabel.text = so.DisplayName;
        if (iconImage) iconImage.sprite = so.gunnerSprite;
        if (!btn) btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => onClick?.Invoke(gunnerId));

        var gm = GunnerManager.Instance;
        bool owned = gm != null && gm.IsOwned(gunnerId);
        bool equipState = runtime.EquippedSlot >= 0;

        // Locked by prestige?
        bool requiresPrestige = gm != null && gm.RequiresPrestigeUnlock(gunnerId);
        bool prestigeUnlocked = PrestigeManager.Instance != null && PrestigeManager.Instance.IsGunnerUnlocked(gunnerId);

        if (!owned)
        {
            if (requiresPrestige && !prestigeUnlocked)
            {
                if (statusLabel) statusLabel.text = "Locked (Prestige)";
                if (levelLabel) levelLabel.text = "Lv —";
                if (buyPriceText) buyPriceText.text = "";
                if (currencyIconGO) currencyIconGO.SetActive(false);
                btn.interactable = false;
                return;
            }

            // Purchasable
            ulong cost = gm != null ? gm.GetFirstCopyCost(gunnerId) : 0UL;
            if (statusLabel) statusLabel.text = cost > 0 ? "Buy" : "Free";
            if (levelLabel) levelLabel.text = "Lv —";
            if (buyPriceText) buyPriceText.text = cost > 0 ? cost.ToString("N0") : "";
            if (currencyIconGO) currencyIconGO.SetActive(cost > 0);
            btn.interactable = true; // click - purchase in menu
            return;
        }

        // Owned - show runtime state (equip/quest)
        if (levelLabel) levelLabel.text = "Lv " + runtime.Level;
        if (buyPriceText) buyPriceText.text = "";
        if (currencyIconGO) currencyIconGO.SetActive(false);

        string status = "Available";
        if (runtime.IsOnQuest) status = "On quest";
        else if (equipState) status = $"Equipped: Slot {runtime.EquippedSlot + 1}";
        if (statusLabel) statusLabel.text = status;

        btn.interactable = runtime.IsAvailableNow() || equipState;
    }
}
