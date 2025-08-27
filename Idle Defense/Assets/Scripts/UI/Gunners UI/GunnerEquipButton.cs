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

    private string gunnerId;

    public void Init(GunnerSO so, GunnerRuntime runtime, System.Action<string> onClick)
    {
        gunnerId = so.GunnerId;

        if (nameLabel) nameLabel.text = so.DisplayName;
        if (levelLabel) levelLabel.text = "Lv " + runtime.Level;

        string status = "Available";
        if (runtime.IsOnQuest) status = "On quest";
        else if (runtime.EquippedSlot >= 0) status = $"Equipped: Slot {runtime.EquippedSlot + 1}";
        if (statusLabel) statusLabel.text = status;

        if (iconImage) iconImage.sprite = so.IdleSprite;

        if (!btn) btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => onClick?.Invoke(gunnerId));
        btn.interactable = runtime.IsAvailableNow() || runtime.EquippedSlot >= 0;
    }
}
