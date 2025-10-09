using Assets.Scripts.SO; // for TurretType etc.
using Assets.Scripts.Systems.Save;
using Assets.Scripts.Turrets;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PrestigeManager : MonoBehaviour
{
    public static PrestigeManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private PrestigeTreeSO activeTree;
    [SerializeField] private bool grantUnlocksOnLoad = true;

    [Header("State")]
    [SerializeField] private int prestigeLevel;        // optional: times reset/ascended
    [SerializeField] private int crimsonCore;          // currency
    [SerializeField] private List<string> ownedNodes = new();   // NodeIds purchased

    // ------------ DEBUG ------------
    [Header("Debug")]
    [SerializeField] private bool debugBypassPrereqs = false;   // ignore RequiresAny/All and MinPrestige
    [SerializeField] private bool debugFreePurchases = false;   // ignore CrimsonCost
    [Tooltip("Auto-own these node IDs on Awake (Editor only).")]
    [SerializeField] private List<string> debugAutoOwnOnStart = new();

    // Cache
    private readonly HashSet<string> owned = new();
    private readonly HashSet<string> unlockedGunners = new();
    private readonly HashSet<TurretType> unlockedTurrets = new();
    private readonly HashSet<LimitBreakType> unlockedLBs = new();

    private PrestigeEffects sum; // aggregated every time you buy

    public event Action OnPrestigeChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Editor-only: auto-own nodes for quick test
#if UNITY_EDITOR
        if (debugAutoOwnOnStart != null && debugAutoOwnOnStart.Count > 0)
        {
            foreach (var id in debugAutoOwnOnStart)
            {
                var n = Resolve(id);
                if (n != null && !owned.Contains(id))
                {
                    owned.Add(id);
                }
            }
        }
#endif

        RecomputeCaches();

        if (grantUnlocksOnLoad)
            ApplyUnlocksToGame();
    }


    /* ===================== PUBLIC API ===================== */

    public bool CanBuy(string nodeId, out string reason)
    {
        reason = "";
        var node = Resolve(nodeId);
        if (node == null) { reason = "Node not found."; return false; }
        if (owned.Contains(nodeId)) { reason = "Already owned."; return false; }

        // Cost check (unless debug free)
        if (!debugFreePurchases && crimsonCore < node.CrimsonCost)
        {
            reason = "Not enough Crimson Core.";
            return false;
        }

        if (!debugBypassPrereqs)
        {
            if (prestigeLevel < node.MinPrestigeLevel) { reason = "Prestige level too low."; return false; }

            if (node.RequiresAny != null && node.RequiresAny.Count > 0)
            {
                bool ok = node.RequiresAny.Any(r => owned.Contains(r));
                if (!ok) { reason = "Requires one of: " + string.Join(", ", node.RequiresAny); return false; }
            }
            if (node.RequiresAll != null && node.RequiresAll.Count > 0)
            {
                bool ok = node.RequiresAll.All(r => owned.Contains(r));
                if (!ok) { reason = "Requires all of: " + string.Join(", ", node.RequiresAll); return false; }
            }
        }
        return true;
    }

    public bool TryBuy(string nodeId)
    {
        if (!CanBuy(nodeId, out _)) return false;

        var node = Resolve(nodeId);

        if (!debugFreePurchases)
            crimsonCore -= Mathf.Max(0, node.CrimsonCost);

        owned.Add(nodeId);
        ownedNodes = owned.ToList();

        // aggregate immediately
        Accumulate(node);
        ApplyUnlocks(node);
        Save();

        OnPrestigeChanged?.Invoke();
        return true;
    }

    public void GrantCrimson(int amount)
    {
        crimsonCore += Mathf.Max(0, amount);
        OnPrestigeChanged?.Invoke();
    }

    public int GetCrimson() => crimsonCore;
    public int GetPrestigeLevel() => prestigeLevel;
    public bool Owns(string nodeId) => owned.Contains(nodeId);

    public bool IsGunnerUnlocked(string gunnerId) => unlockedGunners.Contains(gunnerId);
    public bool IsTurretUnlocked(TurretType t) => unlockedTurrets.Contains(t);
    public bool IsLimitBreakUnlocked(LimitBreakType lb) => unlockedLBs.Contains(lb);

    public float GetScrapsGainMultiplier() => 1f + sum.scrapsGainPct / 100f;
    public float GetBlackSteelGainMultiplier() => 1f + sum.blackSteelGainPct / 100f;

    // Called by your “Reset/Ascend” flow
    public void PerformPrestigeReset(int grantCrimson)
    {
        prestigeLevel++;
        crimsonCore += Mathf.Max(0, grantCrimson);

        // Keep owned nodes by design (true prestige), or wipe if you want seasonal ladders.
        // Here we KEEP nodes (permanent progression).

        Save();
        OnPrestigeChanged?.Invoke();
    }

    /* ===================== APPLY HOOKS ===================== */

    // 1) Turret: apply global stat % to the effective scratch (after turret+gunner merge).
    public void ApplyToTurretStats(TurretStatsInstance s)
    {
        if (s == null) return;

        // Multiplicative applies for base rates
        s.Damage *= (1f + sum.dmgPct / 100f);
        s.FireRate *= (1f + sum.fireRatePct / 100f);
        s.Range *= (1f + sum.rangePct / 100f);
        s.RotationSpeed *= (1f + sum.rotationSpeedPct / 100f);

        // Additive (percentage/flat style fields)
        s.CriticalChance += sum.critChancePct;
        s.CriticalDamageMultiplier += sum.critDmgPct;
        s.PierceChance += sum.pierceChancePct;

        // Effects and patterns
        s.ExplosionRadius *= (1f + sum.explosionRadiusPct / 100f);
        s.SplashDamage *= (1f + sum.splashDamagePct / 100f);

        // Decreasing stats (use negative values in nodes to reduce)
        s.PierceDamageFalloff *= Mathf.Max(0f, 1f + sum.pierceDamageFalloffPct / 100f);
        s.DamageFalloffOverDistance *= Mathf.Max(0f, 1f + sum.damageFalloffOverDistancePct / 100f);

        // Pellets: treat % as proportional add, then floor to int
        if (sum.pelletCountPct != 0f)
        {
            float pellets = s.PelletCount * (1f + sum.pelletCountPct / 100f);
            s.PelletCount = Mathf.Max(0, Mathf.FloorToInt(pellets));
        }

        s.PercentBonusDamagePerSec += sum.percentBonusDpsPct;
        s.SlowEffect += sum.slowEffectPct;
        s.KnockbackStrength *= (1f + sum.knockbackStrengthPct / 100f);

        // Bounce set
        if (sum.bounceCountPct != 0f)
        {
            float bc = s.BounceCount * (1f + sum.bounceCountPct / 100f);
            s.BounceCount = Mathf.Max(0, Mathf.FloorToInt(bc));
        }
        s.BounceRange *= (1f + sum.bounceRangePct / 100f);
        s.BounceDelay *= Mathf.Max(0.0001f, 1f + sum.bounceDelayPct / 100f);
        s.BounceDamagePct *= Mathf.Max(0f, 1f + sum.bounceDamagePctPct / 100f);

        // Cone / Delay / Trap
        s.ConeAngle *= (1f + sum.coneAnglePct / 100f);
        s.ExplosionDelay *= Mathf.Max(0.0001f, 1f + sum.explosionDelayPct / 100f);
        s.AheadDistance *= (1f + sum.aheadDistancePct / 100f);
        if (sum.maxTrapsActivePct != 0f)
        {
            float mta = s.MaxTrapsActive * (1f + sum.maxTrapsActivePct / 100f);
            s.MaxTrapsActive = Mathf.Max(0, Mathf.FloorToInt(mta));
        }

        // Armor Penetration (0..100 clamped elsewhere)
        s.ArmorPenetration += sum.armorPenPct;
    }

    // 2) Enemy scaling: call this when computing per-wave HP/spawn
    public void ApplyToEnemyGeneratedStats(ref float maxHealth, ref int count)
    {
        maxHealth *= (1f + sum.enemyHealthPct / 100f);
        float c = count * (1f + sum.enemyCountPct / 100f);
        count = Mathf.Max(1, Mathf.RoundToInt(c));
    }

    // 3) Speed cap: add to your existing speed-up feature limit (UI gated elsewhere)
    public float GetAdditionalSpeedCap() => sum.speedCapBonus;

    // 4) Upgrade costs: call this when computing upgrade costs
    public float GetUpgradeCostMultiplier(Assets.Scripts.Systems.TurretUpgradeType type)
    {
        // Combine global and per-type discounts, clamp to [0.05 .. 1] so it never becomes free.
        float totalPct = Mathf.Max(0f, sum.allUpgradeCostPct);

        if (sum.perTypeDiscounts != null && sum.perTypeDiscounts.TryGetValue(type, out float typePct))
            totalPct += Mathf.Max(0f, typePct);

        float mult = 1f - (totalPct / 100f);
        return Mathf.Clamp(mult, 0.05f, 1f);
    }

    /* ===================== INTERNAL ===================== */

    private PrestigeNodeSO Resolve(string nodeId) =>
        activeTree != null ? activeTree.Nodes.FirstOrDefault(n => n != null && n.NodeId == nodeId) : null;

    private void RecomputeCaches()
    {
        owned.Clear();
        foreach (var n in ownedNodes)
            if (!string.IsNullOrEmpty(n)) owned.Add(n);

        sum = new PrestigeEffects();
        unlockedGunners.Clear();
        unlockedTurrets.Clear();
        unlockedLBs.Clear();

        if (activeTree == null || activeTree.Nodes == null) return;

        foreach (var node in activeTree.Nodes)
        {
            if (node == null || !owned.Contains(node.NodeId)) continue;
            Accumulate(node);
            ApplyUnlocks(node);
        }
    }

    private void Accumulate(PrestigeNodeSO n)
    {
        var add = new PrestigeEffects
        {
            dmgPct = n.GlobalDamagePct,
            fireRatePct = n.GlobalFireRatePct,
            critChancePct = n.GlobalCritChancePct,
            critDmgPct = n.GlobalCritDamagePct,
            pierceChancePct = n.GlobalPierceChancePct,

            rangePct = n.RangePct,
            rotationSpeedPct = n.RotationSpeedPct,
            explosionRadiusPct = n.ExplosionRadiusPct,
            splashDamagePct = n.SplashDamagePct,
            pierceDamageFalloffPct = n.PierceDamageFalloffPct,
            pelletCountPct = n.PelletCountPct,
            damageFalloffOverDistancePct = n.DamageFalloffOverDistancePct,
            percentBonusDpsPct = n.PercentBonusDamagePerSecPct,
            slowEffectPct = n.SlowEffectPct,
            knockbackStrengthPct = n.KnockbackStrengthPct,

            bounceCountPct = n.BounceCountPct,
            bounceRangePct = n.BounceRangePct,
            bounceDelayPct = n.BounceDelayPct,
            bounceDamagePctPct = n.BounceDamagePctPct,

            coneAnglePct = n.ConeAnglePct,
            explosionDelayPct = n.ExplosionDelayPct,
            aheadDistancePct = n.AheadDistancePct,
            maxTrapsActivePct = n.MaxTrapsActivePct,

            armorPenPct = n.ArmorPenetrationPct,

            scrapsGainPct = n.ScrapsGainPct,
            blackSteelGainPct = n.BlackSteelGainPct,

            enemyHealthPct = n.EnemyHealthPct,
            enemyCountPct = n.EnemyCountPct,

            speedCapBonus = n.SpeedMultiplierCapBonus,
            allUpgradeCostPct = n.AllUpgradeCostPct,
            perTypeDiscounts = new Dictionary<Assets.Scripts.Systems.TurretUpgradeType, float>()
        };

        if (n.PerUpgradeTypeDiscounts != null)
        {
            foreach (var d in n.PerUpgradeTypeDiscounts)
            {
                if (!add.perTypeDiscounts.ContainsKey(d.Type))
                    add.perTypeDiscounts[d.Type] = 0f;
                add.perTypeDiscounts[d.Type] += d.DiscountPct;
            }
        }

        sum = sum + add;
    }


    private void ApplyUnlocks(PrestigeNodeSO n)
    {
        foreach (var gid in n.UnlockGunnerIds) unlockedGunners.Add(gid);
        foreach (var tt in n.UnlockTurretTypes) unlockedTurrets.Add(tt);
        foreach (var lb in n.UnlockLimitBreaks) unlockedLBs.Add(lb);
    }

    private void ApplyUnlocksToGame()
    {
        // Example:
        // - Turrets: you likely read unlocks from your table; here we provide a query so your unlock gate can consult us.
        // - Gunners: GunnerManager can call PrestigeManager.Instance.IsGunnerUnlocked(gid) for gates.
        // - Limit Breaks: LimitBreakManager can call IsLimitBreakUnlocked.
    }

    /* ===================== SAVE (stub) ===================== */

    private void Save() { SaveGameManager.Instance.SaveGame(); }

    // Public DTO bridge for SaveGameManager
    public PrestigeDTO ExportDTO()
    {
        return new PrestigeDTO
        {
            PrestigeLevel = prestigeLevel,
            CrimsonCore = crimsonCore,
            OwnedNodeIds = ownedNodes != null ? new List<string>(ownedNodes) : new List<string>()
        };
    }

    public void ImportDTO(PrestigeDTO dto)
    {
        prestigeLevel = Mathf.Max(0, dto.PrestigeLevel);
        crimsonCore = Mathf.Max(0, dto.CrimsonCore);

        ownedNodes = dto.OwnedNodeIds != null ? new List<string>(dto.OwnedNodeIds) : new List<string>();
        RecomputeCaches();

        if (grantUnlocksOnLoad)
            ApplyUnlocksToGame();

        OnPrestigeChanged?.Invoke();
    }

    // ===================== DEBUG UTILITIES =====================
    #region DEBUG_TOOLS_RUNTIME_SAFE

    [ContextMenu("DEBUG/Print Summary")]
    public void Debug_PrintSummary()
    {
        var ownedList = string.Join(", ", owned.OrderBy(x => x));
        Debug.Log($"[Prestige] Lvl={prestigeLevel} Crimson={crimsonCore} Owned({owned.Count})=[{ownedList}]");
        Debug.Log($"[Prestige] Effects: " +
                  $"DMG+{sum.dmgPct}% FR+{sum.fireRatePct}% CRIT+{sum.critChancePct}% CDMG+{sum.critDmgPct}% " +
                  $"Pierce+{sum.pierceChancePct}% APen+{sum.armorPenPct}% Range+{sum.rangePct}% Rot+{sum.rotationSpeedPct}%");
        Debug.Log($"[Prestige] Upgrade discounts: all {sum.allUpgradeCostPct}% " +
                  $"{(sum.perTypeDiscounts == null ? 0 : sum.perTypeDiscounts.Count)} types");
    }

    [ContextMenu("DEBUG/Grant 100 Crimson")]
    public void Debug_Grant100Crimson() { GrantCrimson(100); }

    [ContextMenu("DEBUG/Reset Save (wipe nodes & currency)")]
    public void Debug_ResetSave()
    {
        owned.Clear();
        ownedNodes.Clear();
        crimsonCore = 0;
        prestigeLevel = 0;
        RecomputeCaches();
        Save();
        OnPrestigeChanged?.Invoke();
        Debug.Log("[Prestige] Save wiped.");
    }

    // Force-own a node (ignores cost/prereqs)
    public bool Debug_ForceOwnNode(string nodeId)
    {
        var n = Resolve(nodeId);
        if (n == null) { Debug.LogWarning($"[Prestige] Node '{nodeId}' not found."); return false; }
        if (owned.Contains(nodeId)) return true;

        owned.Add(nodeId);
        ownedNodes = owned.ToList();
        Accumulate(n);
        ApplyUnlocks(n);
        Save();
        OnPrestigeChanged?.Invoke();
        Debug.Log($"[Prestige] Force-owned node '{nodeId}'.");
        return true;
    }

    [ContextMenu("DEBUG/Unlock All Nodes")]
    public void Debug_UnlockAll()
    {
        if (activeTree == null || activeTree.Nodes == null) return;
        foreach (var n in activeTree.Nodes)
            if (n != null) Debug_ForceOwnNode(n.NodeId);
        Debug_PrintSummary();
    }

    public void Debug_UnlockBranch(string branchId)
    {
        if (activeTree == null || activeTree.Nodes == null) return;
        foreach (var n in activeTree.Nodes)
            if (n != null && n.BranchId == branchId)
                Debug_ForceOwnNode(n.NodeId);
        Debug.Log($"[Prestige] Branch '{branchId}' unlocked.");
    }

    public void Debug_LockAll()
    {
        owned.Clear();
        ownedNodes.Clear();
        RecomputeCaches();
        Save();
        OnPrestigeChanged?.Invoke();
        Debug.Log("[Prestige] All nodes locked.");
    }

    public void Debug_Rebuild()
    {
        RecomputeCaches();
        OnPrestigeChanged?.Invoke();
        Debug.Log("[Prestige] Rebuilt caches.");
    }

    // Optional: buy ignoring all checks and cost
    public bool Debug_BuyIgnoreAll(string nodeId)
    {
        var n = Resolve(nodeId);
        if (n == null) return false;
        if (owned.Contains(nodeId)) return true;

        owned.Add(nodeId);
        ownedNodes = owned.ToList();
        Accumulate(n);
        ApplyUnlocks(n);
        Save();
        OnPrestigeChanged?.Invoke();
        return true;
    }

    #endregion

}


