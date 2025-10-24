using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum PrestigeNodeVisualState { LockedUnavailable, Available, Unlocked }

public class PrestigeNodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Main")]
    public PrestigeNodeSO NodeSO;
    [HideInInspector] public string NodeId;

    [Header("Icon & Cost")]
    public Image iconImage;                // big node icon
    public TMP_Text costText;              // “Owned” or numeric cost
    public CanvasGroup costPillGroup;      // fades when locked/unbuyable

    [Header("Quick Chips (max 2)")]
    public Image chipAIcon;
    public Image chipBIcon;
    private GameIconManager iconLib;        // Game-wide icon manager

    [Header("UX")]
    public Button button;                  // opens tooltip (no direct buy!)
    public GameObject highlight;           // e.g., glow when Available
    public RectTransform tooltipAnchor;    // usually this rect

    // Visual config
    private static readonly Color NearBlack = new Color(0.06f, 0.06f, 0.06f, 1f);
    private const float FadeAlpha = 0.45f;

    private void OnEnable()
    {
        if (NodeSO != null) Init(NodeSO);
    }

    public void Init(PrestigeNodeSO node)
    {
        NodeSO = node;
        NodeId = node.NodeId;

        if (iconLib == null)
            iconLib = GameIconManager.Instance;

        if (tooltipAnchor == null) tooltipAnchor = transform as RectTransform;
        if (iconImage != null) iconImage.sprite = node.Icon;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OpenTooltip);    // no purchase here
            button.gameObject.SetActive(true);
            button.interactable = true;
        }

        RefreshVisual();
    }

    public void RefreshVisual()
    {
        var pm = PrestigeManager.Instance;
        if (pm == null || NodeSO == null) return;

        if (string.IsNullOrEmpty(NodeId)) NodeId = NodeSO.NodeId;

        bool owned = pm.Owns(NodeId);
        bool prereqsMet = ComputePrereqsMet(pm, NodeSO);

        var state = owned
            ? PrestigeNodeVisualState.Unlocked
            : (prereqsMet ? PrestigeNodeVisualState.Available : PrestigeNodeVisualState.LockedUnavailable);

        ApplyState(state);

        // Cost text & pill
        if (costText != null)
            costText.text = owned ? "Owned" : NodeSO.CrimsonCost.ToString();

        bool canBuy = !owned && pm.CanBuy(NodeId, out _);
        if (costPillGroup != null)
            costPillGroup.alpha = canBuy ? 1f : 0.45f; // fade cost when locked/unbuyable

        // Quick chips (icons only, max 2)
        RenderQuickChips(NodeSO);
    }

    // ---------- Quick chips ----------
    private void RenderQuickChips(PrestigeNodeSO node)
    {
        if (iconLib == null) return;

        var keys = new List<(string key, float weight)>();

        // Unlocks first (priority chips)
        if (node.UnlockTurretTypes != null && node.UnlockTurretTypes.Count > 0) keys.Add(("unlock_turret", 999));
        if (node.UnlockGunnerIds != null && node.UnlockGunnerIds.Count > 0) keys.Add(("unlock_gunner", 998));
        if (node.UnlockLimitBreaks != null && node.UnlockLimitBreaks.Count > 0) keys.Add(("unlock_limitbreak", 997));

        // Numeric effects (weight by |value|)
        Add(keys, "damage", node.GlobalDamagePct);
        Add(keys, "firerate", node.GlobalFireRatePct);
        Add(keys, "crit_chance", node.GlobalCritChancePct);
        Add(keys, "crit_damage", node.GlobalCritDamagePct);
        Add(keys, "pierce_chance", node.GlobalPierceChancePct);
        Add(keys, "range", node.RangePct);
        Add(keys, "rotation", node.RotationSpeedPct);
        Add(keys, "explo_radius", node.ExplosionRadiusPct);
        Add(keys, "splash", node.SplashDamagePct);
        Add(keys, "pierce_falloff", -node.PierceDamageFalloffPct);
        Add(keys, "pellet", node.PelletCountPct);
        Add(keys, "dist_falloff", -node.DamageFalloffOverDistancePct);
        Add(keys, "bonus_dps", node.PercentBonusDamagePerSecPct);
        Add(keys, "slow", node.SlowEffectPct);
        Add(keys, "knockback", node.KnockbackStrengthPct);
        Add(keys, "bounce_count", node.BounceCountPct);
        Add(keys, "bounce_range", node.BounceRangePct);
        Add(keys, "bounce_delay", -node.BounceDelayPct);
        Add(keys, "bounce_loss", -node.BounceDamagePctPct);
        Add(keys, "cone_angle", node.ConeAnglePct);
        Add(keys, "explo_delay", -node.ExplosionDelayPct);
        Add(keys, "ahead_dist", node.AheadDistancePct);
        Add(keys, "max_traps", node.MaxTrapsActivePct);
        Add(keys, "armorpen", node.ArmorPenetrationPct);

        // Economy (show as stat icons, not currency icons)
        Add(keys, GameIconKeys.ScrapsGainStat.Replace("stat.", ""), node.ScrapsGainPct);
        Add(keys, GameIconKeys.BlackSteelGainStat.Replace("stat.", ""), node.BlackSteelGainPct);

        // Enemy modifiers (reductions are beneficial → negative value becomes positive weight)
        Add(keys, GameIconKeys.EnemyHealth.Replace("stat.", ""), -node.EnemyHealthPct);
        Add(keys, GameIconKeys.EnemyCount.Replace("stat.", ""), -node.EnemyCountPct);

        // Caps
        Add(keys, GameIconKeys.SpeedCap.Replace("stat.", ""), node.SpeedMultiplierCapBonus);
        Add(keys, GameIconKeys.DamageCap.Replace("stat.", ""), node.DamageMultiplierCapBonus);

        // Upgrade cost reductions (global + per-type)
        float largestDiscount = Mathf.Max(0f, node.AllUpgradeCostPct);
        if (node.PerUpgradeTypeDiscounts != null)
            for (int i = 0; i < node.PerUpgradeTypeDiscounts.Count; i++)
                largestDiscount = Mathf.Max(largestDiscount, node.PerUpgradeTypeDiscounts[i].DiscountPct);

        if (largestDiscount > 0.0001f)
            Add(keys, GameIconKeys.UpgradeCostAll.Replace("stat.", ""), largestDiscount);

        // ── choose two ───────────────────────────────────────────────────────────
        var chosen = keys
            .OrderByDescending(k => k.weight)
            .Select(k => k.key)
            .Distinct()
            .Take(2)
            .ToList();

        // Resolve icons
        SetIcon(chipAIcon, chosen.Count > 0 ? ResolveIcon(chosen[0]) : null);
        SetIcon(chipBIcon, chosen.Count > 1 ? ResolveIcon(chosen[1]) : null);
    }

    // Helpers: keep your existing Add/SetIcon; add this resolver:
    private Sprite ResolveIcon(string key)
    {
        // Unlocks live at root ("unlock.turret"), everything else is under "stat."
        if (key.StartsWith("unlock_"))
            return iconLib.Get(key.Replace('_', '.')); // "unlock_turret" → "unlock.turret"
        return iconLib.Get("stat." + key);
    }


    private static void Add(List<(string key, float weight)> list, string key, float v)
    {
        if (Mathf.Abs(v) > 0.0001f) list.Add((key, Mathf.Abs(v)));
    }

    private static void SetIcon(Image img, Sprite s)
    {
        if (!img) return;
        img.sprite = s;
        img.enabled = s != null;
    }

    // ---------- Tooltip wiring ----------
    public void OnPointerEnter(PointerEventData e)
    {
        if (PrestigeTooltipUI.Instance != null && NodeSO != null)
            PrestigeTooltipUI.Instance.Peek(NodeSO, tooltipAnchor);
    }

    public void OnPointerExit(PointerEventData e)
    {
        PrestigeTooltipUI.Instance?.TryHideTransient();
    }

    public void OnPointerClick(PointerEventData e) => OpenTooltip();

    private void OpenTooltip()
    {
        if (PrestigeTooltipUI.Instance == null || NodeSO == null) return;

        if (PrestigeTooltipUI.Instance.IsLocked && PrestigeTooltipUI.Instance.CurrentNodeId != NodeSO.NodeId)
        {
            PrestigeTooltipUI.Instance.Lock(NodeSO, tooltipAnchor);
            return;
        }
        PrestigeTooltipUI.Instance.Lock(NodeSO, tooltipAnchor);
    }

    // ---------- Visual state ----------
    private void ApplyState(PrestigeNodeVisualState state)
    {
        if (iconImage != null)
        {
            switch (state)
            {
                case PrestigeNodeVisualState.LockedUnavailable:
                    iconImage.color = NearBlack;
                    iconImage.canvasRenderer.SetAlpha(1f);
                    break;
                case PrestigeNodeVisualState.Available:
                    iconImage.color = Color.white;
                    iconImage.canvasRenderer.SetAlpha(FadeAlpha);
                    break;
                case PrestigeNodeVisualState.Unlocked:
                    iconImage.color = Color.white;
                    iconImage.canvasRenderer.SetAlpha(1f);
                    break;
            }
        }
        if (highlight != null)
            highlight.SetActive(state == PrestigeNodeVisualState.Available);
    }

    private static bool ComputePrereqsMet(PrestigeManager pm, PrestigeNodeSO node)
    {
        if (node.RequiresAll != null && node.RequiresAll.Count > 0)
            for (int i = 0; i < node.RequiresAll.Count; i++)
                if (!pm.Owns(node.RequiresAll[i])) return false;

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
        if (NodeSO != null)
        {
            NodeId = NodeSO.NodeId;
            if (iconImage != null && NodeSO.Icon != null)
                iconImage.sprite = NodeSO.Icon;
        }
        if (tooltipAnchor == null) tooltipAnchor = transform as RectTransform;
    }
#endif
}
