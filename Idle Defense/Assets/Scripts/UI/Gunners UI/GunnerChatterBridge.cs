using Assets.Scripts.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunnerChatterBridge : MonoBehaviour
{
    [SerializeField] private GunnerChatterSystem chatter;
    [SerializeField] private bool useSceneSlotFallback = true; // optional

    private void Start()
    {
        if (chatter == null) chatter = FindFirstObjectByType<GunnerChatterSystem>();
        var gm = GunnerManager.Instance;
        if (gm != null)
        {
            gm.OnRosterChanged += HandleRosterChanged;
            gm.OnSlotGunnerChanged += HandleSlotChanged;
        }

        // Immediate and delayed refresh (handles late scene wiring)
        RebuildActiveList();
        StartCoroutine(DelayedRefresh());
    }

    private void OnDisable()
    {
        var gm = GunnerManager.Instance;
        if (gm != null)
        {
            gm.OnRosterChanged -= HandleRosterChanged;
            gm.OnSlotGunnerChanged -= HandleSlotChanged;
        }
    }

    private void HandleRosterChanged()
    {
        RebuildActiveList();
    }

    private void HandleSlotChanged(int _)
    {
        RebuildActiveList();
    }

    private IEnumerator DelayedRefresh()
    {
        yield return null;              // end of frame
        RebuildActiveList();
        yield return new WaitForSeconds(0.1f);
        RebuildActiveList();
    }

    private void RebuildActiveList()
    {
        if (chatter == null || GunnerManager.Instance == null) return;

        // 1) Preferred: query manager directly (scene-agnostic)
        var equipped = GunnerManager.Instance.GetAllEquippedGunners();
        if (equipped != null && equipped.Count > 0)
        {
            chatter.ActiveGunners = equipped;
            return;
        }

        // 2) Fallback: scan scene slots if requested
        if (!useSceneSlotFallback) { chatter.ActiveGunners = new List<GunnerSO>(); return; }

        var list = new List<GunnerSO>();
        var slots = FindObjectsByType<SlotWorldButton>(FindObjectsSortMode.None);
        for (int i = 0; i < slots.Length; i++)
        {
            int slotIndex = slots[i].slotIndex;
            string gid = GunnerManager.Instance.GetEquippedGunnerId(slotIndex);
            if (string.IsNullOrEmpty(gid)) continue;

            var so = GunnerManager.Instance.GetSO(gid);
            if (so != null && !list.Contains(so))
                list.Add(so);
        }
        chatter.ActiveGunners = list;
    }
}
