using TMPro;
using UnityEngine;
using Assets.Scripts.Systems;

public class PlayerUpgradeButton : MonoBehaviour
{
    [Header("UI Elements (Auto-Assigned)")]
    [SerializeField] private TextMeshProUGUI statName, statValue, statUpgradeAmount, statUpgradeCost;

    [Header("Upgrade Type")]
    [SerializeField] private PlayerUpgradeType upgradeType;

    private PlayerBaseManager upgradeManager;

    private void Awake()
    {
        var tmpros = GetComponentsInChildren<TextMeshProUGUI>();
        if (tmpros.Length >= 4)
        {
            statName = tmpros[0];
            statValue = tmpros[1];
            statUpgradeAmount = tmpros[2];
            statUpgradeCost = tmpros[3];
        }
    }

    private void Start()
    {
        upgradeManager = PlayerBaseManager.Instance;
        statName.SetText(GetDisplayNameForUpgrade(upgradeType));
        UpdateDisplayFromType();
    }

    public void OnClick()
    {
        switch (upgradeType)
        {
            case PlayerUpgradeType.MaxHealth: upgradeManager.UpgradeMaxHealth(); break;
            case PlayerUpgradeType.RegenAmount: upgradeManager.UpgradeRegenAmount(); break;
            case PlayerUpgradeType.RegenInterval: upgradeManager.UpgradeRegenInterval(); break;
        }
        UpdateDisplayFromType();
    }

    public void UpdateStats(string value, string bonus, string cost)
    {
        statValue.SetText(value);
        statUpgradeAmount.SetText(bonus);
        statUpgradeCost.SetText(cost);
    }

    public void UpdateDisplayFromType()
    {
        switch (upgradeType)
        {
            case PlayerUpgradeType.MaxHealth: upgradeManager.UpdateMaxHealthDisplay(this); break;
            case PlayerUpgradeType.RegenAmount: upgradeManager.UpdateRegenAmountDisplay(this); break;
            case PlayerUpgradeType.RegenInterval: upgradeManager.UpdateRegenIntervalDisplay(this); break;
        }
    }

    private string GetDisplayNameForUpgrade(PlayerUpgradeType type)
    {
        return type switch
        {
            PlayerUpgradeType.MaxHealth => "Max Health",
            PlayerUpgradeType.RegenAmount => "Regen / Tick",
            PlayerUpgradeType.RegenInterval => "Regen Rate",
            _ => type.ToString()
        };
    }
}
