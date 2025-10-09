using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GunnerStatRowUI : MonoBehaviour
{
    [Header("Refs")]
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI valueText;     // shows "current"
    public TextMeshProUGUI nextValueText; // shows "-> next"
    public TextMeshProUGUI lockText;      // "Unlocks at Lv X"
    public Button upgradeButton;          // small "+" button

    private Action _onUpgrade;

    public void SetUnlocked(Sprite s, string nameStr, string valueStr)
    {
        if (icon) { icon.gameObject.SetActive(true); icon.sprite = s; }
        if (nameText) { nameText.gameObject.SetActive(true); nameText.text = nameStr; }
        if (valueText) { valueText.gameObject.SetActive(true); valueText.text = valueStr; }
        if (nextValueText) nextValueText.gameObject.SetActive(false);
        if (lockText) lockText.gameObject.SetActive(false);
        if (upgradeButton) { upgradeButton.gameObject.SetActive(false); upgradeButton.onClick.RemoveAllListeners(); }
        gameObject.SetActive(true);
    }

    // Unlocked + upgradable
    public void SetUnlockedWithUpgrade(Sprite s, string nameStr, string valueStr, string nextStr, Action onUpgrade)
    {
        if (icon) { icon.gameObject.SetActive(true); icon.sprite = s; }
        if (nameText) { nameText.gameObject.SetActive(true); nameText.text = nameStr; }
        if (valueText) { valueText.gameObject.SetActive(true); valueText.text = valueStr; }

        if (nextValueText)
        {
            nextValueText.gameObject.SetActive(true);
            nextValueText.text = "> " + nextStr;
        }

        if (lockText) lockText.gameObject.SetActive(false);

        _onUpgrade = onUpgrade;
        if (upgradeButton)
        {
            upgradeButton.gameObject.SetActive(true);
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(() => _onUpgrade?.Invoke());
        }

        gameObject.SetActive(true);
    }

    public void SetLocked(Sprite s, string nameStr, int unlockLevel)
    {
        if (icon) icon.gameObject.SetActive(false);
        if (nameText) nameText.gameObject.SetActive(false);
        if (valueText) valueText.gameObject.SetActive(false);
        if (nextValueText) nextValueText.gameObject.SetActive(false);
        if (upgradeButton) { upgradeButton.gameObject.SetActive(false); upgradeButton.onClick.RemoveAllListeners(); }

        if (lockText)
        {
            lockText.gameObject.SetActive(true);
            lockText.text = "Unlocks at Lv " + unlockLevel;
        }
        gameObject.SetActive(true);
    }
}
