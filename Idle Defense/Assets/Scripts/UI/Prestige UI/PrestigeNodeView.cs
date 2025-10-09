using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum PrestigeNodeVisualState
{
    LockedUnavailable,  // near-black, not faded
    Available,          // natural color, faded
    Unlocked            // natural color, full alpha
}

public class PrestigeNodeView : MonoBehaviour
{
    [Header("Refs")]
    public Image iconImage;         // assign in prefab: the icon
    public Button button;           // assign in prefab
    public TMP_Text costText;       // optional; can be null
    public GameObject highlight;    // optional visual for “available”

    [Header("Runtime")]
    [HideInInspector] public string NodeId;
    public PrestigeNodeSO NodeSO;

    // Visual config
    private static readonly Color NearBlack = new Color(0.06f, 0.06f, 0.06f, 1f);
    private const float FadeAlpha = 0.45f;

    public void Init(PrestigeNodeSO node)
    {
        NodeSO = node;
        NodeId = node.NodeId;

        if (iconImage != null) iconImage.sprite = node.Icon;
        RefreshVisual();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClickBuy);
        }
    }

    public void RefreshVisual()
    {
        var pm = PrestigeManager.Instance;
        if (pm == null || NodeSO == null) return;

        bool owned = pm.Owns(NodeId);
        bool prereqsMet = ComputePrereqsMet(pm, NodeSO);  // ignores currency on purpose
        PrestigeNodeVisualState state = owned
            ? PrestigeNodeVisualState.Unlocked
            : (prereqsMet ? PrestigeNodeVisualState.Available : PrestigeNodeVisualState.LockedUnavailable);

        // apply colors
        ApplyState(state);

        // cost text (optional)
        if (costText != null)
        {
            if (owned)
            {
                costText.text = "Owned";
            }
            else
            {
                costText.text = NodeSO.CrimsonCost.ToString();
            }
        }

        // button interactability: allow click only if CanBuy is actually true (cost/prereqs both ok)
        if (button != null)
        {
            string r;
            bool canBuy = pm.CanBuy(NodeId, out r);
            button.interactable = canBuy;
        }
    }

    private void ApplyState(PrestigeNodeVisualState state)
    {
        if (iconImage == null) return;

        switch (state)
        {
            case PrestigeNodeVisualState.LockedUnavailable:
                iconImage.color = NearBlack;                 // near black, NOT faded
                break;
            case PrestigeNodeVisualState.Available:
                iconImage.color = Color.white;               // natural color but faded via alpha
                iconImage.canvasRenderer.SetAlpha(FadeAlpha);
                break;
            case PrestigeNodeVisualState.Unlocked:
                iconImage.color = Color.white;               // natural, full alpha
                iconImage.canvasRenderer.SetAlpha(1f);
                break;
        }

        if (highlight != null)
            highlight.SetActive(state == PrestigeNodeVisualState.Available);
    }

    private void OnClickBuy()
    {
        var pm = PrestigeManager.Instance;
        if (pm == null) return;

        if (pm.TryBuy(NodeId))
        {
            // purchase succeeded -> visuals will be refreshed by the TreeUI listening to OnPrestigeChanged
        }
        else
        {
            // optional: flash error
        }
    }

    private static bool ComputePrereqsMet(PrestigeManager pm, PrestigeNodeSO node)
    {
        // RequiresAll: all must be owned
        if (node.RequiresAll != null && node.RequiresAll.Count > 0)
        {
            for (int i = 0; i < node.RequiresAll.Count; i++)
                if (!pm.Owns(node.RequiresAll[i])) return false;
        }

        // RequiresAny: if there are entries, at least one must be owned
        if (node.RequiresAny != null && node.RequiresAny.Count > 0)
        {
            bool anyOwned = false;
            for (int i = 0; i < node.RequiresAny.Count; i++)
                if (pm.Owns(node.RequiresAny[i])) { anyOwned = true; break; }
            if (!anyOwned) return false;
        }

        return true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Ensure NodeId is set even in Edit mode
        if (NodeSO != null)
        {
            NodeId = NodeSO.NodeId;
            if (iconImage != null && NodeSO.Icon != null)
                iconImage.sprite = NodeSO.Icon;
            // Optional: keep the icon position in sync with the SO
            // var rt = transform as RectTransform;
            // if (rt != null) rt.anchoredPosition = NodeSO.AnchoredPosition;
        }
    }
#endif

}
