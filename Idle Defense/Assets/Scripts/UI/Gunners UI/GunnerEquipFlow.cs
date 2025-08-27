using TMPro;
using UnityEngine;

public class GunnerEquipFlow : MonoBehaviour
{
    public static GunnerEquipFlow Instance { get; private set; }

    [Header("Optional: On-screen hint")]
    [SerializeField] private CanvasGroup clickHintGroup; // fade in/out
    [SerializeField] private TMP_Text clickHintLabel;    // "Click a turret slot to equip"

    public bool IsAwaitingSlotSelection { get; private set; }
    public string SelectedGunnerId { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SetHint(false, "");
    }

    public void BeginSelectSlot(string gunnerId)
    {
        SelectedGunnerId = gunnerId;
        IsAwaitingSlotSelection = true;
        SetHint(true, "Click a turret slot to equip");
    }

    public void CompleteSelection()
    {
        IsAwaitingSlotSelection = false;
        SelectedGunnerId = null;
        SetHint(false, "");
    }

    public void CancelSelection()
    {
        IsAwaitingSlotSelection = false;
        SelectedGunnerId = null;
        SetHint(false, "");
    }

    private void SetHint(bool show, string text)
    {
        if (clickHintGroup) clickHintGroup.alpha = show ? 1f : 0f;
        if (clickHintGroup) clickHintGroup.interactable = show;
        if (clickHintGroup) clickHintGroup.blocksRaycasts = show;
        if (clickHintLabel && !string.IsNullOrEmpty(text)) clickHintLabel.text = text;
    }
}
