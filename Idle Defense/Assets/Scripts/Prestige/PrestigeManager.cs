using Assets.Scripts.SO; // for TurretType etc.
using Assets.Scripts.Systems;
using Assets.Scripts.Systems.Save;
using Assets.Scripts.Turrets;
using Assets.Scripts.WaveSystem;
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

    [Header("Prestige Unlock Rule")]
    [Tooltip("Player can prestige once CURRENT wave number >= this value.")]
    [SerializeField] private int minWaveToPrestige = 10;

    // Cached eligibility (updated when WaveManager reports a new wave)
    private bool canPrestigeNow;

    /// <summary>Minimum wave (1-based) required to unlock prestige.</summary>
    public int GetMinWaveToPrestige() => minWaveToPrestige;

    /// <summary>True if current wave meets the unlock rule.</summary>
    public bool CanPrestigeNow() => canPrestigeNow;

    /// <summary>Raised when eligibility flips (e.g., crossing the unlock wave).</summary>
    public event System.Action<bool, int, int> OnPrestigeEligibilityChanged;

    [Header("Prestige Reset Config")]
    [Tooltip("Reset: remove all turrets from inventory and clear wave-unlocked types.")]
    [SerializeField] private bool resetAllTurrets = true;

    [Tooltip("Reset: remove all gunners from ownership.")]
    [SerializeField] private bool resetAllGunners = true;

    [Tooltip("Reset: wipe ALL turret upgrades (owned ones rebuilt from base SO).")]
    [SerializeField] private bool resetTurretUpgrades = true;

    [Tooltip("Reset: wipe ALL gunner per-stat upgrades and learned stat unlocks.")]
    [SerializeField] private bool resetGunnerUpgrades = true;

    [Tooltip("Reset: gunner LEVEL/XP/skill points (independent from per-stat upgrades).")]
    [SerializeField] private bool resetGunnerLevels = true;

    [Tooltip("Reset: set Scraps to 0. (Uses reflection if your GameManager lacks SetCurrency.)")]
    [SerializeField] private bool resetScraps = true;

    [Tooltip("Reset: set Black Steel to 0. (Uses reflection if your GameManager lacks SetCurrency.)")]
    [SerializeField] private bool resetBlackSteel = false;

    [Tooltip("Which wave to restart from after prestige. Wave 0 = first wave.")]
    [SerializeField] private int restartAtWaveIndex = 0;

    [Header("Crimson Core Rewards")]
    [Tooltip("Flat Crimson Core awarded on prestige regardless of progress.")]
    [SerializeField] private int flatCrimsonOnPrestige = 0;

    [Tooltip("Crimson per cleared wave beyond RestartAtWaveIndex. Example: 1 = +1 CC per wave.")]
    [SerializeField] private float crimsonPerWave = 1f;

    [Serializable]
    public struct WaveMilestone
    {
        public int WaveIndex;   // inclusive threshold
        public int BonusCrimson;
    }

    [Tooltip("Optional milestone bonuses (granted if best wave >= threshold).")]
    [SerializeField] private List<WaveMilestone> milestoneBonuses = new List<WaveMilestone>();

    [Tooltip("Optional cap for Crimson from waves (0 = no cap).")]
    [SerializeField] private int crimsonFromWavesCap = 0;

    [Header("State")]
    [SerializeField] private int prestigeLevel;        // optional: times reset/ascended
    [SerializeField] private List<string> ownedNodes = new();   // NodeIds purchased

    [Header("Prestige UI Helper")]
    public bool ResetTurretsOwnership => resetAllTurrets;
    public bool ResetTurretsUpgrades => resetTurretUpgrades;
    public bool ResetGunnersOwnership => resetAllGunners;
    public bool ResetGunnersUpgrades => resetGunnerUpgrades;
    public bool ResetGunnerLevels => resetGunnerLevels;
    public bool ResetScraps => resetScraps;
    public bool ResetBlackSteel => resetBlackSteel;
    public int RestartWaveIndexForUI() => restartAtWaveIndex + 1; // 1-based for players

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

    private void OnEnable()
    {
        // Initialize eligibility in case we enter mid-run
        int cur = WaveManager.Instance ? WaveManager.Instance.GetCurrentWaveIndex() : 0;
        canPrestigeNow = cur >= minWaveToPrestige;

        // Optional: also subscribe to be robust even if WM forgets to call us
        if (WaveManager.Instance)
            WaveManager.Instance.OnWaveStarted += HandleWaveStartedEvent;
    }

    private void OnDisable()
    {
        if (WaveManager.Instance)
            WaveManager.Instance.OnWaveStarted -= HandleWaveStartedEvent;
    }

    private void HandleWaveStartedEvent(object sender, WaveManager.OnWaveStartedEventArgs e)
    {
        NotifyWaveStarted(e.WaveNumber);
    }

    /* ===================== PUBLIC API ===================== */

    public bool CanBuy(string nodeId, out string reason)
    {
        reason = "";
        var node = Resolve(nodeId);
        if (node == null) { reason = "Node not found."; return false; }
        if (owned.Contains(nodeId)) { reason = "Already owned."; return false; }

        // Cost check (unless debug free)
        if (!debugFreePurchases)
        {
            var gmgr = GameManager.Instance;
            var balance = gmgr != null ? gmgr.GetCurrency(Currency.CrimsonCore) : 0UL;
            if (balance < (ulong)Mathf.Max(0, node.CrimsonCost))
            {
                reason = "Not enough Crimson Core.";
                return false;
            }
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
        Debug.Log($"[Prestige] Attempting to buy node '{nodeId}'...");
        
        if (!CanBuy(nodeId, out _)) return false;

        var node = Resolve(nodeId);

        if (!debugFreePurchases)
        {
            var gmgr = GameManager.Instance;
            if (gmgr != null)
            {
                var cost = (ulong)Mathf.Max(0, node.CrimsonCost);

                if (!gmgr.TrySpendCurrency(Currency.CrimsonCore, cost))
                {
                    return false;
                }                
            }
        }


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
        var gmgr = GameManager.Instance;
        if (gmgr != null && amount > 0)
            gmgr.AddCurrency(Currency.CrimsonCore, (ulong)amount);
        OnPrestigeChanged?.Invoke();
    }
    public int GetCrimson()
    {
        var gmgr = GameManager.Instance;
        return gmgr != null ? (int)Mathf.Min(int.MaxValue, (long)gmgr.GetCurrency(Currency.CrimsonCore)) : 0;
    }
    public int GetPrestigeLevel() => prestigeLevel;
    public bool Owns(string nodeId) => owned.Contains(nodeId);
    public bool IsGunnerUnlocked(string gunnerId) => unlockedGunners.Contains(gunnerId);
    public bool IsTurretUnlocked(TurretType t) => unlockedTurrets.Contains(t);
    public bool IsLimitBreakUnlocked(LimitBreakType lb) => unlockedLBs.Contains(lb);
    public float GetScrapsGainMultiplier() => 1f + sum.scrapsGainPct / 100f;
    public float GetBlackSteelGainMultiplier() => 1f + sum.blackSteelGainPct / 100f;

    /* ===================== ACTUAL PRESTIGE RESET ===================== */

    /// <summary>
    /// Perform the actual prestige:
    /// 1) Compute Crimson reward and add it.
    /// 2) Increment prestige level.
    /// 3) Reset systems according to toggles.
    /// 4) Ask the game to restart from the configured wave.
    /// </summary>
    [ContextMenu("DEBUG/Perform Prestige Now")]
    public void PerformPrestigeNow()
    {
        int grantCrimson = PreviewCrimsonForPrestige();
        prestigeLevel++;
        var gmgr = GameManager.Instance;
        if (gmgr != null && grantCrimson > 0)
            gmgr.AddCurrency(Currency.CrimsonCore, (ulong)grantCrimson);

        // ---- resets ----
        if (resetAllTurrets || resetTurretUpgrades)
            TryResetTurrets(resetAllTurrets, resetTurretUpgrades);

        if (resetAllGunners || resetGunnerUpgrades || resetGunnerLevels)
            TryResetGunners(resetAllGunners, resetGunnerUpgrades, resetGunnerLevels);

        if (resetScraps || resetBlackSteel)
            TryResetCurrencies(resetScraps, resetBlackSteel);

        // Reaffirm anchors so parked/equipped visuals hook back up cleanly
        var slotMgr = Assets.Scripts.Systems.TurretSlotManager.Instance;
        if (slotMgr != null && GunnerManager.Instance != null)
        {
            // If a preferred starter was equipped to slot 0, make sure the anchor is known
            if (slotMgr.Get(0) != null)
            {
                // GunnerManager will lazily rebuild anchors if null; this just nudges visuals
                GunnerManager.Instance.NotifyTurretPresent(
                    slotIndex: 0,
                    turretAnchor: null,
                    hasTurret: true
                );
            }
        }


        if (PlayerBaseManager.Instance != null)
        {
            // Reset base permanent upgrades if desired. (You can refine later.)
            // Using the built-in reset to base stats + visuals.
            PlayerBaseManager.Instance.ResetPlayerBase();
            PlayerBaseManager.Instance.InitializeGame(usePermanentStats: true);
        }

        // Keep prestige nodes (permanent meta). If you ever want seasonal wipe, clear owned here.

        Save();
        OnPrestigeChanged?.Invoke();

        // ---- restart run ----
        RequestRestartAtWave(restartAtWaveIndex);
    }

    /// <summary>
    /// Emits a restart request. WaveManager (or a bootstrapper) should subscribe and handle the transition.
    /// </summary>
    public event Action<int> OnRequestRestartRun;

    private void RequestRestartAtWave(int waveIndex0Based)
    {
        // Notify listeners (UI, analytics, etc.)
        OnRequestRestartRun?.Invoke(Mathf.Max(0, waveIndex0Based));

        var wm = WaveManager.Instance;
        if (wm == null) return;

        // 1) Hard-abort current wave with suppression and full despawn.
        wm.AbortWaveAndDespawnAll();

        // 2) Convert to 1-based external wave number.
        int targetWaveNumber = Mathf.Max(1, waveIndex0Based + 1);

        // 3) Reset waves, load target, and restart cleanly.
        wm.ResetWave();
        wm.LoadWave(targetWaveNumber);
        wm.ForceRestartWave();
    }


    /// <summary>
    /// Computes how much Crimson the player would receive if they prestige now,
    /// using current wave and (optionally) best-cleared wave if you track it.
    /// </summary>
    public int PreviewCrimsonForPrestige(int? currentWaveOverride = null, int? bestWaveOverride = null)
    {
        int current = currentWaveOverride ?? (WaveManager.Instance != null ? WaveManager.Instance.GetCurrentWaveIndex() : 0);
        int best = bestWaveOverride ?? current;

        int wavesClearedBeyondRestart = Mathf.Max(0, best - restartAtWaveIndex);
        float fromWaves = wavesClearedBeyondRestart * crimsonPerWave;

        if (crimsonFromWavesCap > 0)
            fromWaves = Mathf.Min(fromWaves, crimsonFromWavesCap);

        int bonus = 0;
        if (milestoneBonuses != null)
        {
            for (int i = 0; i < milestoneBonuses.Count; i++)
                if (best >= milestoneBonuses[i].WaveIndex) bonus += milestoneBonuses[i].BonusCrimson;
        }

        int total = flatCrimsonOnPrestige + Mathf.RoundToInt(fromWaves) + bonus;
        return Mathf.Max(0, total);
    }

    /// <summary>
    /// A short string you can show in a tooltip on the prestige button.
    /// </summary>
    public string GetPrestigeTooltipPreview()
    {
        int best = WaveManager.Instance != null ? WaveManager.Instance.GetCurrentWaveIndex() : 0;
        int cc = PreviewCrimsonForPrestige(bestWaveOverride: best);

        return $"Prestige now to earn +{cc} Crimson Core.\n" +
               $"Restarts at Wave {restartAtWaveIndex + 1}.";
    }

    private void TryResetTurrets(bool wipeOwnership, bool wipeUpgrades)
    {
        var inv = TurretInventoryManager.Instance;
        if (inv == null) return;

        // 1) Clear slots first (do NOT auto-equip here)
        var slots = Assets.Scripts.Systems.TurretSlotManager.Instance;
        if (slots != null)
            slots.UnequipAll(autoEquipStarter: false);

        // 2) Reset inventory (this seeds the starter and may equip it if SlotMgr is alive)
        inv.ResetAll(wipeOwnership, wipeUpgrades);

        // 3) If nothing is equipped (e.g., due to timing), force-equip the starter like delete-save does
        if (slots != null && !slots.IsAnyTurretEquipped())
            slots.UnequipAll(autoEquipStarter: true);
    }


    private void TryResetGunners(bool wipeOwnership, bool wipeUpgrades, bool resetLevels)
    {
        var gm = GunnerManager.Instance;
        if (gm == null) return;

        gm.ResetAll(wipeOwnership, wipeUpgrades, resetLevels);
    }

    /// <summary>
    /// Resets Scraps/Black Steel to zero. Uses reflection in case your GameManager
    /// has no SetCurrency API (keeps this file decoupled).
    /// </summary>
    private void TryResetCurrencies(bool resetScrapsFlag, bool resetSteelFlag)
    {
        var game = GameManager.Instance;
        if (game == null) return;

        var t = game.GetType();
        var mi = t.GetMethod("SetCurrency", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

        if (mi != null)
        {
            if (resetScrapsFlag) mi.Invoke(game, new object[] { Currency.Scraps, (ulong)0 });
            if (resetSteelFlag) mi.Invoke(game, new object[] { Currency.BlackSteel, (ulong)0 });
            return;
        }

        Debug.LogWarning("[Prestige] GameManager.SetCurrency not found. Implement it or attach a listener to PrestigeManager.OnPrestigeChanged to zero currencies.");
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
    public float GetEnemyHealthMultiplier()
    {
        // negative percentages will reduce health, but never below 0
        return Mathf.Max(0f, 1f + sum.enemyHealthPct / 100f);
    }

    public float GetEnemyCountMultiplier()
    {
        // negative percentages will reduce count, but keep at least 1 enemy later
        return Mathf.Max(0f, 1f + sum.enemyCountPct / 100f);
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

    /// <summary>
    /// Called by WaveManager (or by our own subscription) when a new wave starts.
    /// Updates internal eligibility and notifies listeners if it changed.
    /// </summary>
    public void NotifyWaveStarted(int currentWaveNumber)
    {
        bool newState = currentWaveNumber >= minWaveToPrestige;
        if (newState != canPrestigeNow)
        {
            canPrestigeNow = newState;
            OnPrestigeEligibilityChanged?.Invoke(canPrestigeNow, currentWaveNumber, minWaveToPrestige);
        }
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
        //Nudge systems that draw from prestige unlocks to refresh their UI / state.
        if (Assets.Scripts.Systems.TurretInventoryManager.Instance != null)
            Assets.Scripts.Systems.TurretInventoryManager.Instance.NotifyExternalUnlocksChanged();

        if (GunnerManager.Instance != null)
            GunnerManager.Instance.NotifyExternalUnlocksChanged();
    }

    /* ===================== SAVE (stub) ===================== */

    private void Save() { SaveGameManager.Instance.SaveGame(); }

    // Public DTO bridge for SaveGameManager
    public PrestigeDTO ExportDTO()
    {
        return new PrestigeDTO
        {
            PrestigeLevel = prestigeLevel,
            OwnedNodeIds = ownedNodes != null ? new List<string>(ownedNodes) : new List<string>()
        };
    }

    public void ImportDTO(PrestigeDTO dto)
    {
        prestigeLevel = Mathf.Max(0, dto.PrestigeLevel);

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


