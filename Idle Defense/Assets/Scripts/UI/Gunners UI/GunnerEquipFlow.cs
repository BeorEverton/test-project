using TMPro;
using UnityEngine;

public class GunnerEquipFlow : MonoBehaviour
{
    public static GunnerEquipFlow Instance { get; private set; }

    [Header("Optional: On-screen hint")]
    [SerializeField] private CanvasGroup clickHintGroup; // fade in/out
    [SerializeField] private TMP_Text clickHintLabel;    // "Click a turret slot to equip"

    [Header("Optional: Cursor tooltip")]
    [SerializeField] private GunnerCursorGhost cursorGhost;

    public bool IsAwaitingSlotSelection { get; private set; }
    public string SelectedGunnerId { get; private set; }

    // Tracks the last empty slot clicked during selection so a second click cancels.
    private int _lastEmptySlotIndex = -1;

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
        _lastEmptySlotIndex = -1;
        SetHint(true, "Click a slot with a turret to equip.\n(Empty slot: click twice to cancel)");

        // Start cursor ghost with this gunner's portrait (if available)
        if (cursorGhost && GunnerManager.Instance != null)
        {
            var so = GunnerManager.Instance.GetSO(gunnerId);
            var sprite = so != null && so.OnTurretSprite ? so.OnTurretSprite : (so != null ? so.IdleSprite : null);
            if (sprite) cursorGhost.Begin(sprite);
        }
    }


    public void CompleteSelection()
    {
        IsAwaitingSlotSelection = false;
        SelectedGunnerId = null;
        _lastEmptySlotIndex = -1;
        SetHint(false, "");
        if (cursorGhost) cursorGhost.End();
    }

    public void CancelSelection()
    {
        IsAwaitingSlotSelection = false;
        SelectedGunnerId = null;
        _lastEmptySlotIndex = -1;
        SetHint(false, "");
        if (cursorGhost) cursorGhost.End();
    }


    /// <summary>
    /// Call this when the player clicks an empty slot during selection.
    /// First click does nothing (selection stays active). Second click on the same
    /// empty slot cancels the selection.
    /// </summary>
    public void HandleEmptySlotClick(int slotIndex)
    {
        if (_lastEmptySlotIndex == slotIndex)
        {
            // Second click on the same empty slot: cancel selection.
            CancelSelection();
            return;
        }

        // First click on this empty slot: remember it and keep selection alive.
        _lastEmptySlotIndex = slotIndex;
        SetHint(true, "Slot is empty. Click it again to cancel.");
    }

    private void SetHint(bool show, string text)
    {
        if (clickHintGroup) clickHintGroup.alpha = show ? 1f : 0f;
        if (clickHintGroup) clickHintGroup.interactable = show;
        if (clickHintGroup) clickHintGroup.blocksRaycasts = show;
        if (clickHintLabel && !string.IsNullOrEmpty(text)) clickHintLabel.text = text;
    }
}
