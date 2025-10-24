using TMPro;
using UnityEngine;

public class GunnerMenuUI : MonoBehaviour
{
    [Header("List")]
    [SerializeField] private Transform content;              // parent for buttons
    [SerializeField] private GunnerEquipButton buttonPrefab; // prefab with GunnerEquipButton

    [Header("Header/Status (optional)")]
    [SerializeField] private TMP_Text headerLabel;

    private void OnEnable()
    {
        BuildList();
        if (headerLabel) headerLabel.text = "Select a Gunner, then click a turret slot";
    }

    private void OnDisable()
    {
        if (GunnerEquipFlow.Instance) GunnerEquipFlow.Instance.CancelSelection();
    }

    private void BuildList()
    {
        // clear
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        // build
        var mgr = GunnerManager.Instance;
        if (mgr == null) return;

        // access all gunners via a simple reflection; you can expose a getter if preferred
        var soList = mgr.GetType()
                        .GetField("allGunners", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.GetValue(mgr) as System.Collections.Generic.List<GunnerSO>;

        if (soList == null) return;

        foreach (var so in soList)
        {
            var rt = mgr.GetRuntime(so.GunnerId);
            var btn = Instantiate(buttonPrefab, content);
            btn.Init(so, rt, OnGunnerClicked);
        }
    }

    private void OnGunnerClicked(string gunnerId)
    {
        var gm = GunnerManager.Instance;
        if (gm == null) return;

        // If not owned but purchasable - try buy first
        if (!gm.IsOwned(gunnerId))
        {
            if (gm.IsPurchasableNow(gunnerId) && gm.TryPurchaseGunner(gunnerId))
            {
                BuildList(); // refresh to show as owned
                return;
            }
            // Not purchasable (probably prestige lock)
            return;
        }

        // Owned - begin equip flow as before
        GunnerEquipFlow.Instance.BeginSelectSlot(gunnerId);
    }

}
