using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PrestigeTooltipUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static PrestigeTooltipUI Instance { get; private set; }

    [Header("Refs")]
    public RectTransform root;   // the panel
    public TMP_Text title;
    public TMP_Text description;
    public TMP_Text effects;     // “what it does”
    public TMP_Text cost;
    public Button buyButton;

    [Header("Icons")]
    public GameIconLibrarySO iconLib;
    public Transform effectsIconRow;        // horizontal layout for unlock/effect icons (optional)
    public GameObject effectIconPrefab;     // prefab with Image only

    [Header("Behavior")]
    [Tooltip("How long to wait before hiding a peeked tooltip after the pointer exits both node and tooltip.")]
    public float hideDelaySeconds = 0.08f;

    private PrestigeNodeSO current;
    private RectTransform anchor;
    private Camera uiCam;

    // Peek/lock state
    private bool isLocked;
    private bool pointerOverSelf;
    private Coroutine hideRoutine;

    public bool IsLocked => isLocked;
    public string CurrentNodeId => current ? current.NodeId : null;

    void Awake()
    {
        Instance = this;
        var canvas = GetComponentInParent<Canvas>();
        uiCam = canvas ? canvas.worldCamera : null;
        HideImmediate();
    }

    // --------- Public API ---------

    // Hover-only: show and allow auto-hide
    public void Peek(PrestigeNodeSO node, RectTransform anchorRt)
    {
        isLocked = false;
        ShowInternal(node, anchorRt);
    }

    // Click: show and keep open until explicitly hidden/unlocked
    public void Lock(PrestigeNodeSO node, RectTransform anchorRt)
    {
        isLocked = true;
        ShowInternal(node, anchorRt);
    }

    // Called by nodes on pointer-exit; will close only if not locked and mouse not over the tooltip.
    public void TryHideTransient()
    {
        if (isLocked) return;              // locked overrides peek
        if (pointerOverSelf) return;       // still hovering the tooltip
        RestartHideTimer();
    }

    // Force-close (e.g., after purchase or when clicking another node)
    public void UnlockAndHide()
    {
        isLocked = false;
        HideImmediate();
    }

    public void OnPointerEnter(PointerEventData e)
    {
        pointerOverSelf = true;
        CancelHideTimer();
    }

    public void OnPointerExit(PointerEventData e)
    {
        pointerOverSelf = false;
        if (!isLocked) RestartHideTimer();
    }

    // --------- Internals ---------

    private void ShowInternal(PrestigeNodeSO node, RectTransform anchorRt)
    {
        current = node; anchor = anchorRt;
        if (!node) return;

        title.text = string.IsNullOrEmpty(node.DisplayName) ? node.NodeId : node.DisplayName;
        description.text = node.Description;

        effects.text = BuildEffectsText(node);
        PopulateIconsRow(node);

        cost.text = $"Cost: {node.CrimsonCost}";

        bool canBuy = PrestigeManager.Instance && PrestigeManager.Instance.CanBuy(node.NodeId, out _);
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(TryBuyCurrent);
        buyButton.interactable = canBuy && !(PrestigeManager.Instance?.Owns(node.NodeId) ?? false);

        root.gameObject.SetActive(true);
        Follow(anchorRt);

        // visible => cancel any pending hide
        CancelHideTimer();
    }

    private void TryBuyCurrent()
    {
        if (!current || PrestigeManager.Instance == null) return;
        if (PrestigeManager.Instance.TryBuy(current.NodeId))   // commits and raises event
        {
            UnlockAndHide(); // close after purchase
        }
    }

    public void Follow(RectTransform rt)
    {
        if (!rt) return;
        Vector3 world = rt.TransformPoint(new Vector3(rt.rect.width * 0.6f, 0f, 0f));
        Vector3 screen = RectTransformUtility.WorldToScreenPoint(uiCam, world);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root.parent as RectTransform, screen, uiCam, out var local);
        root.anchoredPosition = local;
    }

    private void HideImmediate()
    {
        CancelHideTimer();
        root.gameObject.SetActive(false);
        current = null;
        buyButton.onClick.RemoveAllListeners();
    }

    private void RestartHideTimer()
    {
        CancelHideTimer();
        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private void CancelHideTimer()
    {
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(hideDelaySeconds);
        if (!isLocked && !pointerOverSelf)
            HideImmediate();
    }

    // --------- Content builders ---------

    private string BuildEffectsText(PrestigeNodeSO n)
    {
        var sb = new StringBuilder();

        void Add(string label, float v, bool pct = true)
        {
            if (Mathf.Abs(v) < 0.0001f) return;
            sb.AppendLine($"{label} {(v > 0 ? "+" : "")}{(pct ? $"{v:0.#}%" : $"{v:0.#}")}");
        }

        // Numeric effects (top to bottom)
        Add("Damage", n.GlobalDamagePct);
        Add("Fire Rate", n.GlobalFireRatePct);
        Add("Crit Chance", n.GlobalCritChancePct);
        Add("Crit Damage", n.GlobalCritDamagePct);
        Add("Pierce Chance", n.GlobalPierceChancePct);
        Add("Range", n.RangePct);
        Add("Rotation Speed", n.RotationSpeedPct);
        Add("Explosion Radius", n.ExplosionRadiusPct);
        Add("Splash Damage", n.SplashDamagePct);
        Add("Pierce Falloff", -n.PierceDamageFalloffPct);
        Add("Pellet Count", n.PelletCountPct);
        Add("Distance Falloff", -n.DamageFalloffOverDistancePct);
        Add("Bonus DPS", n.PercentBonusDamagePerSecPct);
        Add("Slow", n.SlowEffectPct);
        Add("Knockback", n.KnockbackStrengthPct);
        Add("Bounce Count", n.BounceCountPct);
        Add("Bounce Range", n.BounceRangePct);
        Add("Bounce Delay", -n.BounceDelayPct);
        Add("Bounce Loss", -n.BounceDamagePctPct);
        Add("Cone Angle", n.ConeAnglePct);
        Add("Explosion Delay", -n.ExplosionDelayPct);
        Add("Ahead Distance", n.AheadDistancePct);
        Add("Max Traps", n.MaxTrapsActivePct);
        Add("Armor Pen", n.ArmorPenetrationPct);
        Add("Scraps Gain", n.ScrapsGainPct);
        Add("Black Steel Gain", n.BlackSteelGainPct);
        Add("Enemy Health", -n.EnemyHealthPct);
        Add("Enemy Count", -n.EnemyCountPct);
        Add("Speed Cap", n.SpeedMultiplierCapBonus, pct: false);
        Add("Damage Cap", n.DamageMultiplierCapBonus, pct: false);

        if (n.UnlockTurretTypes != null && n.UnlockTurretTypes.Count > 0) sb.AppendLine("Unlocks: Turret(s)");
        if (n.UnlockGunnerIds != null && n.UnlockGunnerIds.Count > 0) sb.AppendLine("Unlocks: Gunner(s)");
        if (n.UnlockLimitBreaks != null && n.UnlockLimitBreaks.Count > 0) sb.AppendLine("Unlocks: Limit Break");

        return sb.Length > 0 ? sb.ToString().TrimEnd() : "—";
    }

    private void PopulateIconsRow(PrestigeNodeSO n)
    {
        if (!effectsIconRow) return;
        foreach (Transform c in effectsIconRow) Destroy(c.gameObject);

        void AddIcon(string key)
        {
            var s = iconLib ? iconLib.Get(key) : null;
            if (!s) return;
            var go = Instantiate(effectIconPrefab, effectsIconRow);
            var img = go.GetComponentInChildren<Image>();
            if (img) img.sprite = s;
        }

        // 1) Unlock icons (existing behavior)
        if (n.UnlockTurretTypes != null && n.UnlockTurretTypes.Count > 0) AddIcon(GameIconKeys.UnlockTurret);
        if (n.UnlockGunnerIds != null && n.UnlockGunnerIds.Count > 0) AddIcon(GameIconKeys.UnlockGunner);
        if (n.UnlockLimitBreaks != null && n.UnlockLimitBreaks.Count > 0) AddIcon(GameIconKeys.UnlockLimitBreak);

        // 2) Top-2 stat icons by absolute magnitude
        var ranked = new List<(string key, float weight)>();
        void Add(string key, float v) { if (Mathf.Abs(v) > 0.0001f) ranked.Add((key, Mathf.Abs(v))); }

        Add(GameIconKeys.Damage, n.GlobalDamagePct);
        Add(GameIconKeys.FireRate, n.GlobalFireRatePct);
        Add(GameIconKeys.CritChance, n.GlobalCritChancePct);
        Add(GameIconKeys.CritDamage, n.GlobalCritDamagePct);
        Add(GameIconKeys.PierceChance, n.GlobalPierceChancePct);
        Add(GameIconKeys.Range, n.RangePct);
        Add(GameIconKeys.Rotation, n.RotationSpeedPct);
        Add(GameIconKeys.ExploRadius, n.ExplosionRadiusPct);
        Add(GameIconKeys.Splash, n.SplashDamagePct);
        Add(GameIconKeys.PierceFalloff, -n.PierceDamageFalloffPct);
        Add(GameIconKeys.Pellet, n.PelletCountPct);
        Add(GameIconKeys.DistFalloff, -n.DamageFalloffOverDistancePct);
        Add(GameIconKeys.BonusDps, n.PercentBonusDamagePerSecPct);
        Add(GameIconKeys.Slow, n.SlowEffectPct);
        Add(GameIconKeys.Knockback, n.KnockbackStrengthPct);
        Add(GameIconKeys.BounceCount, n.BounceCountPct);
        Add(GameIconKeys.BounceRange, n.BounceRangePct);
        Add(GameIconKeys.BounceDelay, -n.BounceDelayPct);
        Add(GameIconKeys.BounceLoss, -n.BounceDamagePctPct);
        Add(GameIconKeys.ConeAngle, n.ConeAnglePct);
        Add(GameIconKeys.ExploDelay, -n.ExplosionDelayPct);
        Add(GameIconKeys.AheadDist, n.AheadDistancePct);
        Add(GameIconKeys.MaxTraps, n.MaxTrapsActivePct);
        Add(GameIconKeys.ArmorPen, n.ArmorPenetrationPct);
        Add(GameIconKeys.Scraps, n.ScrapsGainPct);
        Add(GameIconKeys.BlackSteel, n.BlackSteelGainPct);
        Add(GameIconKeys.Health, -n.EnemyHealthPct);  // if negative = good for player

        foreach (var (key, _) in ranked.OrderByDescending(r => r.weight).Select(r => (r.key, r.weight)).Take(2))
            AddIcon(key);
    }

}
