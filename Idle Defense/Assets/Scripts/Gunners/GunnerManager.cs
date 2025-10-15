using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using Assets.Scripts.Systems.Save;
using Assets.Scripts.Turrets;
using Assets.Scripts.UI;
using Assets.Scripts.WaveSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Assets.Scripts.Enemies.Enemy;

public class GunnerManager : MonoBehaviour
{
    public static GunnerManager Instance { get; private set; }

    [Header("Authoring")]
    [SerializeField] private GunnerUnlockTableSO unlockTable;

    // Built from unlockTable at runtime for fast access
    private readonly List<GunnerSO> allGunners = new();
    private readonly Dictionary<string, GunnerSO> soById = new();

    [SerializeField] private GameObject gunnerBillboardPrefab; // simple SpriteRenderer child
    [SerializeField] private Vector3 onTurretLocalOffset = new Vector3(0f, 0.6f, 0f);

    [Header("Starter")]
    [SerializeField, Tooltip("Preferred starter gunner kept across prestiges (drag a GunnerSO here or set once via code).")]
    private GunnerSO preferredStarterGunner;

    // Player state
    private readonly Dictionary<string, GunnerRuntime> runtimes = new();
    // slotIndex -> GunnerId
    private readonly Dictionary<int, string> slotToGunner = new();

    // cache: GunnerId -> spawned visual (under turret)
    private readonly Dictionary<string, GameObject> visuals = new();

    // slot -> turret anchor (used to get world X of the gunner)
    private readonly Dictionary<int, Transform> slotAnchors = new();

    // bars by gunner id
    private readonly Dictionary<string, DualPhaseBarUI> healthBarByGunner = new();
    private readonly Dictionary<string, DualPhaseBarUI> limitBarByGunner = new();
    private readonly Dictionary<string, GunnerBillboardBinding> billboardByGunner = new();

    // Ownership (first copy)
    private readonly HashSet<string> ownedGunners = new();

    // External listeners (UI, etc.)
    public event System.Action OnRosterChanged;

    // event if all equipped gunners die like OnWaveFailed
    public event Action OnAllEquippedGunnersDead;

    public event Action<int> OnSlotGunnerChanged;
    public event Action<int> OnSlotGunnerStatsChanged;

    // Helpers for the Limit Break
    public GunnerSO GetSO(string gunnerId) => (soById.TryGetValue(gunnerId, out var so) ? so : null);

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Init or load
        // Build registry from unlock table so authoring is centralized
        allGunners.Clear();
        soById.Clear();

        if (unlockTable != null)
        {
            foreach (var e in unlockTable.Entries)
            {
                if (!e.Gunner) continue;
                var so = e.Gunner;
                if (soById.ContainsKey(so.GunnerId)) continue; // avoid dupes

                soById[so.GunnerId] = so;
                allGunners.Add(so);

                if (!runtimes.ContainsKey(so.GunnerId))
                    runtimes[so.GunnerId] = new GunnerRuntime(so);
            }
        }
        else
        {
            Debug.LogWarning("[GunnerManager] No unlock table assigned; manager will be empty.");
        }
    }

    private void Update()
    {
        // resolve quest timers → free up on completion
        foreach (var rt in runtimes.Values)
            rt.IsAvailableNow();
    }

    private void Start()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveStarted += Wave_OnWaveStarted;
    }

    private void OnDisable()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveStarted -= Wave_OnWaveStarted;
    }

    private void Wave_OnWaveStarted(object sender, WaveManager.OnWaveStartedEventArgs e)
    {        
        // Heal all gunners at the start of each wave
        HealAllGunners(resetLimitBreak: false);
    }

    /* ===================== NEW GAME ===================== */

    /// <summary>Change the preferred starter gunner at runtime (e.g., from a selection UI).</summary>
    public void SetPreferredStarter(GunnerSO so)
    {
        preferredStarterGunner = so;
        if (so != null && !IsOwned(so.GunnerId))
            ownedGunners.Add(so.GunnerId); // allow immediate use in the current run if you want
        Save();
        OnRosterChanged?.Invoke();
    }

    /* ===================== EQUIP / UNEQUIP ===================== */

    public bool IsEquipped(string gunnerId) => runtimes.TryGetValue(gunnerId, out var rt) && rt.EquippedSlot >= 0;

    public bool IsAvailable(string gunnerId)
    {
        if (!runtimes.TryGetValue(gunnerId, out var rt)) return false;
        return !IsEquipped(gunnerId) && rt.IsAvailableNow();
    }

    public bool EquipToSlot(string gunnerId, int slotIndex, Transform turretAnchor)
    {
        if (!runtimes.TryGetValue(gunnerId, out var rt)) return false;
        if (!rt.IsAvailableNow()) return false;

        // one per slot
        if (slotToGunner.TryGetValue(slotIndex, out var existing))
            UnequipFromSlot(slotIndex);

        // clear previous slot if any
        if (rt.EquippedSlot >= 0)
            slotToGunner.Remove(rt.EquippedSlot);

        slotToGunner[slotIndex] = gunnerId;
        rt.EquippedSlot = slotIndex;

        AttachEquippedGunnerVisual_Internal(gunnerId, turretAnchor, slotIndex);

        NotifyTurretOfChange(slotIndex);

        Save();
        OnSlotGunnerChanged?.Invoke(slotIndex);
        return true;
    }

    public void UnequipFromSlot(int slotIndex)
    {
        if (!slotToGunner.TryGetValue(slotIndex, out var gunnerId)) return;

        slotToGunner.Remove(slotIndex);
        if (runtimes.TryGetValue(gunnerId, out var rt))
            rt.EquippedSlot = -1;

        // destroy visual
        // destroy visual + forget bars/anchor
        if (visuals.TryGetValue(gunnerId, out var go) && go != null)
        {
            Destroy(go);
            visuals.Remove(gunnerId);
        }
        healthBarByGunner.Remove(gunnerId);
        limitBarByGunner.Remove(gunnerId);
        slotAnchors.Remove(slotIndex);
        billboardByGunner.Remove(gunnerId);

        NotifyTurretOfChange(slotIndex);
        Save();
        OnSlotGunnerChanged?.Invoke(slotIndex);

    }

    public string GetEquippedGunnerId(int slotIndex)
        => slotToGunner.TryGetValue(slotIndex, out var id) ? id : null;

    public GunnerRuntime GetRuntime(string gunnerId)
        => runtimes.TryGetValue(gunnerId, out var rt) ? rt : null;

    public void AttachEquippedGunnerVisual(int slotIndex, Transform turretAnchor)
    {
        var id = GetEquippedGunnerId(slotIndex);
        if (string.IsNullOrEmpty(id)) return;
        AttachEquippedGunnerVisual_Internal(id, turretAnchor, slotIndex);

    }

    private void AttachEquippedGunnerVisual_Internal(string gunnerId, Transform turretAnchor, int slotIndex)
    {
        if (turretAnchor == null) return;
        var so = allGunners.FirstOrDefault(g => g.GunnerId == gunnerId);
        if (so == null) return;

        var lbIcon = GetSkillIcon(so);

        slotAnchors[slotIndex] = turretAnchor;

        if (visuals.TryGetValue(gunnerId, out var existing) && existing != null)
        {
            existing.transform.SetParent(turretAnchor, false);
            existing.transform.localPosition = onTurretLocalOffset;
            existing.SetActive(true);
            ApplySprite(existing, so.OnTurretSprite);

            // Rebind bars if present on this instance
            var binding = existing.GetComponent<GunnerBillboardBinding>();
            if (binding != null && runtimes.TryGetValue(gunnerId, out var rt0))
            {
                binding.InitializeFromRuntime(rt0, lbIcon);
                if (binding.HealthBar) healthBarByGunner[gunnerId] = binding.HealthBar;
                if (binding.LimitBreakBar) limitBarByGunner[gunnerId] = binding.LimitBreakBar;
                billboardByGunner[gunnerId] = binding;
            }
            return;
        }

        GameObject card = gunnerBillboardPrefab != null
            ? Instantiate(gunnerBillboardPrefab, turretAnchor, false)
            : new GameObject("GunnerBillboard_" + gunnerId);

        card.transform.localPosition = onTurretLocalOffset;
        var sr = card.GetComponent<SpriteRenderer>();
        if (sr == null) sr = card.AddComponent<SpriteRenderer>();
        sr.sprite = so.OnTurretSprite;

        // Bars binding (optional)
        var bindingNew = card.GetComponent<GunnerBillboardBinding>();
        if (bindingNew != null && runtimes.TryGetValue(gunnerId, out var rt))
        {
            bindingNew.InitializeFromRuntime(rt, lbIcon);
            if (bindingNew.HealthBar) healthBarByGunner[gunnerId] = bindingNew.HealthBar;
            if (bindingNew.LimitBreakBar) limitBarByGunner[gunnerId] = bindingNew.LimitBreakBar;
            billboardByGunner[gunnerId] = bindingNew;
        }

        visuals[gunnerId] = card;
    }

    private static void ApplySprite(GameObject go, Sprite sprite)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
    }

    private void NotifyTurretOfChange(int slotIndex)
    {
        // If your SlotWorldButton/scene structure keeps the turret under a known anchor,
        // you can find the BaseTurret and nudge it to rebuild effects if needed.
        // For now, turrets will query bonuses every Update and stay correct.
    }

    /* ===================== BUY / UNLOCK ===================== */
    private bool TryGetEntry(string id, out GunnerUnlockTableSO.Entry e)
    {
        if (unlockTable != null && unlockTable.TryGet(id, out e))
            return true;

        e = default;
        return false;
    }

    public bool RequiresPrestigeUnlock(string id)
        => TryGetEntry(id, out var e) && e.RequirePrestigeUnlock;

    public bool IsOwned(string id) => ownedGunners.Contains(id);
    public ulong GetFirstCopyCost(string id)
    {
        return TryGetEntry(id, out var e) ? e.unlockCost : 0UL;
    }

    public  Currency GetPurchaseCurrency(string id)
    {
        if (TryGetEntry(id, out var e)) return e.unlockCurrency;
        return Currency.Scraps; // safe default
    }

    public bool TryPurchaseGunner(string id)
    {
        if (IsOwned(id)) return false;                 // already own
        if (!IsPurchasableNow(id)) return false;       // wave/prestige gates

        ulong cost = GetFirstCopyCost(id);
        if (cost > 0)
        {
            var currency = GetPurchaseCurrency(id);
            if (!GameManager.Instance.TrySpendCurrency(currency, cost))
                return false;
        }

        ownedGunners.Add(id);
        Save();
        OnRosterChanged?.Invoke();
        return true;
    }


    public bool IsPurchasableNow(string id)
    {
        if (!TryGetEntry(id, out var e)) return true; // default permissive

        // wave gate
        int curWave = WaveManager.Instance != null ? WaveManager.Instance.GetCurrentWaveIndex() : 0;
        if (curWave < e.WaveToUnlock) return false;

        // prestige gate
        if (e.RequirePrestigeUnlock && (PrestigeManager.Instance == null || !PrestigeManager.Instance.IsGunnerUnlocked(id)))
            return false;

        return true;
    }


    public void NotifyExternalUnlocksChanged()
    {
        OnRosterChanged?.Invoke();
    }


    /* ===================== XP / LEVEL ===================== */

    public void GrantXpToAll(float xp)
    {
        foreach (var so in allGunners)
        {
            var rt = runtimes[so.GunnerId];
            rt.CurrentXp += Mathf.Max(0, xp);
            TryLevelUp(so, rt);
        }
        Save();
    }

    // New: grant XP only to currently equipped gunners (unique per slot)
    public void GrantXpToEquipped(float xp)
    {
        var given = new HashSet<string>();
        foreach (var kv in slotToGunner)
        {
            var gid = kv.Value;
            if (!given.Add(gid)) continue; // a gunner can’t be in two slots, but guard anyway

            if (!runtimes.TryGetValue(gid, out var rt)) continue;
            var so = allGunners.FirstOrDefault(g => g.GunnerId == gid);
            if (so == null) continue;

            rt.CurrentXp += Mathf.Max(0, xp);
            TryLevelUp(so, rt);
        }
        Save();
    }


    private void TryLevelUp(GunnerSO so, GunnerRuntime rt)
    {
        bool leveled = false;

        // Loop in case we cross multiple levels at once
        while (rt.CurrentXp >= so.XpRequiredForLevel(rt.Level))
        {
            float need = so.XpRequiredForLevel(rt.Level);
            rt.CurrentXp -= need;
            rt.Level++;
            rt.UnspentSkillPoints += so.SkillPointsPerLevel;
            leveled = true;

            // Apply level-based unlocks
            for (int i = 0; i < so.LevelUnlocks.Count; i++)
            {
                var group = so.LevelUnlocks[i];
                if (group.Level == rt.Level)
                {
                    for (int k = 0; k < group.Unlocks.Count; k++)
                        rt.Unlocked.Add(group.Unlocks[k]);
                }
            }
        }

        if (leveled)
        {
            Save();
            OnSlotGunnerStatsChanged?.Invoke(rt.EquippedSlot);

        }
    }


    /* ===================== QUESTS ===================== */

    public bool SendOnQuest(string gunnerId, TimeSpan duration)
    {
        if (!runtimes.TryGetValue(gunnerId, out var rt)) return false;
        if (IsEquipped(gunnerId)) return false;
        if (!rt.IsAvailableNow()) return false;

        rt.IsOnQuest = true;
        rt.QuestEndUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (long)duration.TotalSeconds;
        Save();
        return true;
    }

    /* ===================== EFFECTIVE STATS ===================== */

    public void NotifySlotStatsChanged(int slotIndex)
    {
        if (slotIndex < 0) return;
        OnSlotGunnerStatsChanged?.Invoke(slotIndex);
    }

    public GunnerBonus ComputeBonusForSlot(int slotIndex)
    {
        var bonus = new GunnerBonus();
        if (!slotToGunner.TryGetValue(slotIndex, out var id)) return bonus;
        if (!runtimes.TryGetValue(id, out var rt)) return bonus;
        if (rt.IsDead || rt.IsOnQuest) return bonus;

        var so = allGunners.FirstOrDefault(g => g.GunnerId == id);
        if (so == null) return bonus;

        // Use upgraded values from GunnerUpgradeManager (base + growth)
        // Use upgraded values if the manager exists; otherwise fall back to zeros to avoid NRE
        var up = GunnerUpgradeManager.Instance;
        float Get(GunnerStatKey k) => (up != null) ? up.GetEffectiveStatValue(so, rt, k) : 0f;

        float dmg = Get(GunnerStatKey.Damage);
        float fr = Get(GunnerStatKey.FireRate);
        float rng = Get(GunnerStatKey.Range);
        float ramp = Get(GunnerStatKey.PercentBonusDamagePerSec);
        float slow = Get(GunnerStatKey.SlowEffect);
        float cc = Get(GunnerStatKey.CriticalChance);
        float cd = Get(GunnerStatKey.CriticalDamageMultiplier);
        float kb = Get(GunnerStatKey.KnockbackStrength);
        float sp = Get(GunnerStatKey.SplashDamage);
        float pc = Get(GunnerStatKey.PierceChance);
        float pf = Get(GunnerStatKey.PierceDamageFalloff);
        float ap = Get(GunnerStatKey.ArmorPenetration);

        // Only contribute stats that are unlocked for this gunner
        bool Unl(GunnerStatKey k) => rt.Unlocked != null && rt.Unlocked.Contains(k);

        // assign into bonus (additive to turret)
        if (Unl(GunnerStatKey.Damage)) bonus.Damage += dmg;
        if (Unl(GunnerStatKey.FireRate)) bonus.FireRate += fr;
        if (Unl(GunnerStatKey.Range)) bonus.Range += rng;
        if (Unl(GunnerStatKey.PercentBonusDamagePerSec)) bonus.PercentBonusDamagePerSec += ramp;
        if (Unl(GunnerStatKey.SlowEffect)) bonus.SlowEffect += slow;
        if (Unl(GunnerStatKey.CriticalChance)) bonus.CriticalChance += cc;
        if (Unl(GunnerStatKey.CriticalDamageMultiplier)) bonus.CriticalDamageMultiplier += cd;
        if (Unl(GunnerStatKey.KnockbackStrength)) bonus.KnockbackStrength += kb;
        if (Unl(GunnerStatKey.SplashDamage)) bonus.SplashDamage += sp;
        if (Unl(GunnerStatKey.PierceChance)) bonus.PierceChance += pc;
        if (Unl(GunnerStatKey.PierceDamageFalloff)) bonus.PierceDamageFalloff += pf;
        if (Unl(GunnerStatKey.ArmorPenetration)) bonus.ArmorPenetration += ap;

        return bonus;

    }

    /* Convenience used by BaseTurret */
    public void ApplyTo(TurretStatsInstance baseStats, int slotIndex, TurretStatsInstance intoScratch)
    {
        // 1) copy
        CopyStats(baseStats, intoScratch);

        // If no gunner in this slot, we are done
        if (!slotToGunner.TryGetValue(slotIndex, out var gunnerId) || !runtimes.ContainsKey(gunnerId))
            return;

        if (runtimes[gunnerId].IsDead)
            return; // dead gunners give no bonuses

        // 2) add bonuses
        var b = ComputeBonusForSlot(slotIndex);
        intoScratch.Damage += b.Damage;
        intoScratch.FireRate += b.FireRate;        
        intoScratch.Range += b.Range;
        intoScratch.PercentBonusDamagePerSec += b.PercentBonusDamagePerSec;
        intoScratch.SlowEffect += b.SlowEffect;
        intoScratch.CriticalChance += b.CriticalChance;
        intoScratch.CriticalDamageMultiplier += b.CriticalDamageMultiplier;
        intoScratch.KnockbackStrength += b.KnockbackStrength;
        intoScratch.SplashDamage += b.SplashDamage;
        intoScratch.PierceChance += b.PierceChance;
        intoScratch.PierceDamageFalloff += b.PierceDamageFalloff;
        intoScratch.ArmorPenetration += b.ArmorPenetration;
    }

    private static void CopyStats(TurretStatsInstance src, TurretStatsInstance dst)
    {
        // shallow copy every runtime field you actually read during combat.
        // For brevity, we copy the ones your BaseTurret uses frequently.
        dst.IsUnlocked = src.IsUnlocked;
        dst.TurretType = src.TurretType;

        dst.Damage = src.Damage;
        dst.FireRate = src.FireRate;
        dst.Range = src.Range;
        dst.RotationSpeed = src.RotationSpeed;
        dst.AngleThreshold = src.AngleThreshold;

        dst.CriticalChance = src.CriticalChance;
        dst.CriticalDamageMultiplier = src.CriticalDamageMultiplier;

        dst.KnockbackStrength = src.KnockbackStrength;
        dst.SplashDamage = src.SplashDamage;
        dst.PierceChance = src.PierceChance;
        dst.PierceDamageFalloff = src.PierceDamageFalloff;

        dst.PercentBonusDamagePerSec = src.PercentBonusDamagePerSec;
        dst.SlowEffect = src.SlowEffect;
        dst.CanHitFlying = src.CanHitFlying;
        dst.ArmorPenetration = src.ArmorPenetration;

        // patterns that matter to ExecuteAttack can also be mirrored if needed
        dst.BounceCount = src.BounceCount;
        dst.BounceRange = src.BounceRange;
        dst.BounceDelay = src.BounceDelay;
        dst.BounceDamagePct = src.BounceDamagePct;

        dst.ConeAngle = src.ConeAngle;
        dst.ExplosionDelay = src.ExplosionDelay;
    }

    /* ===================== ENEMY XP HOOK ===================== */

    public void OnEnemyKilled(float xpReward = 1f)
    {
        GrantXpToEquipped(xpReward);
    }

    public void OnEnemySpawned(Enemy enemy)
    {
        enemy.OnDeath += HandleEnemyDeathForXp;
    }

    private void HandleEnemyDeathForXp(object sender, System.EventArgs e)
    {
        if (e is OnDeathEventArgs death)
            OnEnemyKilled(death.XPDropAmount);
    }

    /* ===================== ENEMY HELPER ===================== */
    public bool ApplyDamageOnSlot(int slotIndex, float damage)
    {
        if (!slotToGunner.TryGetValue(slotIndex, out var gid)) return false;
        if (!runtimes.TryGetValue(gid, out var rt)) return false;
        if (rt.IsDead) return false;

        bool diedNow = rt.TakeDamage(damage, out float actual);

        // Tell the equipped turret (for this slot) to rebuild its effective stats immediately
        OnSlotGunnerStatsChanged?.Invoke(slotIndex);

        // Update bars if we have them
        if (healthBarByGunner.TryGetValue(gid, out var hb) && hb != null)
            hb.SetValue(rt.CurrentHealth);

        if (limitBarByGunner.TryGetValue(gid, out var lb) && lb != null)
        {
            // ensure max is correct in case MaxHealth changed elsewhere
            lb.SetMax(rt.LimitBreakMax);
            lb.SetValue(rt.LimitBreakCurrent);
        }

        // Also notify the billboard so it can toggle the Limit Break button/glow
        if (billboardByGunner.TryGetValue(gid, out var bb) && bb != null)
        {
            bb.RefreshLimitBreak(rt.LimitBreakCurrent);
        }

        // If all equipped are dead, signal once (consumer can fail wave)
        bool anyAlive = false;
        foreach (var kvp in slotToGunner)
        {
            var g = kvp.Value;
            if (runtimes.TryGetValue(g, out var rtx) && !rtx.IsDead) { anyAlive = true; break; }
        }
        if (!anyAlive) OnAllEquippedGunnersDead?.Invoke();

        Save(); // persist HP changes with your central save
        return true;
    }

    public bool TryApplyDamageForEnemy(Enemy enemy, float damage)
    {
        if (enemy == null) return false;

        // choose nearest alive target by X at the moment of attack
        int slot = GetNearestAliveSlotByX(enemy.transform.position.x);
        if (slot < 0) return false;

        return ApplyDamageOnSlot(slot, damage);
    }


    public int GetNearestAliveSlotByX(float worldX)
    {
        int bestSlot = -1;
        float bestDist = float.MaxValue;

        foreach (var kvp in slotToGunner)
        {
            int slot = kvp.Key;
            string gid = kvp.Value;

            if (!runtimes.TryGetValue(gid, out var rt) || rt.IsDead) continue;
            if (!slotAnchors.TryGetValue(slot, out var anchor) || anchor == null) continue;

            float dx = Mathf.Abs(anchor.position.x - worldX);
            if (dx < bestDist)
            {
                bestDist = dx;
                bestSlot = slot;
            }
        }
        return bestSlot;
    }

    public float GetSlotAnchorX(int slotIndex)
    {
        return slotAnchors.TryGetValue(slotIndex, out var t) && t != null ? t.position.x : 0f;
    }

    /* ===================== HEAL / LIMIT BREAK ===================== */
    /// <summary>
    /// Heal all gunners to full HP. Optionally reset Limit Break gauge.
    /// Updates billboard bars for equipped gunners. No save here; WaveManager saves after starting the wave.
    /// </summary>
    public void HealAllGunners(bool resetLimitBreak = false)
    {
        // Heal every known gunner (equipped or not)
        foreach (var kv in runtimes)
        {
            var rt = kv.Value;
            rt.Heal(rt.MaxHealth);            
            if (resetLimitBreak) rt.ResetLimitBreak();
        }

        // Refresh bars only for equipped (those have billboards)
        foreach (var map in slotToGunner)
        {
            string gid = map.Value;
            if (!runtimes.TryGetValue(gid, out var rt)) continue;

            if (healthBarByGunner.TryGetValue(gid, out var hb) && hb != null)
            {
                hb.SetMax(rt.MaxHealth);
                hb.SetValue(rt.CurrentHealth);
            }
            if (limitBarByGunner.TryGetValue(gid, out var lb) && lb != null)
            {
                lb.SetMax(rt.LimitBreakMax);
                lb.SetValue(rt.LimitBreakCurrent);
            }
        }

        // Ping turrets so they re-merge (important if a dead gunner was just healed)
        if (OnSlotGunnerStatsChanged != null)
        {
            foreach (var s in slotToGunner.Keys)
                OnSlotGunnerStatsChanged.Invoke(s);
        }
    }

    // Delegate to the new manager
    public bool TryActivateLimitBreak(string gunnerId)
    {
        return LimitBreakManager.Instance != null
            ? LimitBreakManager.Instance.TryActivate(gunnerId)
            : false;
    }

    public void NotifyLimitBreakChanged(string gunnerId)
    {
        if (!runtimes.TryGetValue(gunnerId, out var rt)) return;
        if (limitBarByGunner.TryGetValue(gunnerId, out var lb) && lb) { lb.SetMax(rt.LimitBreakMax); lb.SetValue(rt.LimitBreakCurrent); }
        if (billboardByGunner.TryGetValue(gunnerId, out var bb) && bb) { bb.RefreshLimitBreak(rt.LimitBreakCurrent); }
    }

    public LimitBreakSkillSO ResolveLimitBreakFor(GunnerSO so)
    {
        // Delegate to registry for a single source of truth
        return (LimitBreakManager.Instance != null)
            ? LimitBreakManager.Instance.ResolveFor(so)
            : so?.LimitBreakSkill;
    }

    public static Sprite GetSkillIcon(GunnerSO so)
    {
        if (so == null) return null;
        return (LimitBreakManager.Instance != null)
            ? LimitBreakManager.Instance.GetIconFor(so)
            : (so.LimitBreakSkill != null ? so.LimitBreakSkill.Icon : null);
    }

    /* ===================== SAVE / LOAD ===================== */
    private void Save()
    {
        if (SaveGameManager.Instance != null)
            SaveGameManager.Instance.SaveGame();
    }

    public GunnerInventoryDTO ExportToDTO()
    {
        var dto = new GunnerInventoryDTO();
                
        // runtimes
        foreach (var kvp in runtimes)
        {
            var rt = kvp.Value;

            // NEW: flatten UpgradeLevels into arrays
            int upCount = rt.UpgradeLevels?.Count ?? 0;
            int[] upKeys = new int[upCount];
            int[] upLvls = new int[upCount];
            for (int i = 0; i < upCount; i++)
            {
                upKeys[i] = (int)rt.UpgradeLevels[i].Key;
                upLvls[i] = rt.UpgradeLevels[i].Level;
            }

            dto.Runtimes.Add(new GunnerRuntimeDTO
            {
                Id = kvp.Key,
                CurHP = rt.CurrentHealth,
                MaxHP = rt.MaxHealth,
                Level = rt.Level,
                Xp = rt.CurrentXp,
                Points = rt.UnspentSkillPoints,
                Unlocked = rt.Unlocked.Select(u => (int)u).ToArray(),

                // NEW
                UpKeys = upKeys,
                UpLevels = upLvls,

                OnQuest = rt.IsOnQuest,
                QuestEnd = rt.QuestEndUnixTime,
                EquippedSlot = rt.EquippedSlot
            });
        }


        // slot map
        foreach (var kvp in slotToGunner)
            dto.SlotMap.Add(new SlotGunnerDTO { SlotIndex = kvp.Key, GunnerId = kvp.Value });

        return dto;
    }

    public void ImportFromDTO(GunnerInventoryDTO dto)
    {
        if (dto == null) return;

        // restore runtimes
        foreach (var r in dto.Runtimes)
        {
            if (!runtimes.TryGetValue(r.Id, out var rt))
            {
                var so = allGunners.FirstOrDefault(g => g.GunnerId == r.Id);
                if (so == null) continue;
                rt = new GunnerRuntime(so);
                runtimes[r.Id] = rt;
            }

            rt.CurrentHealth = r.CurHP;
            rt.MaxHealth = r.MaxHP;
            rt.Level = r.Level;
            rt.CurrentXp = r.Xp;
            rt.UnspentSkillPoints = r.Points;
            rt.Unlocked = new HashSet<GunnerStatKey>(r.Unlocked.Select(i => (GunnerStatKey)i));
            rt.IsOnQuest = r.OnQuest;
            rt.QuestEndUnixTime = r.QuestEnd;
            rt.EquippedSlot = r.EquippedSlot;

            // rebuild per-stat upgrade levels
            rt.UpgradeLevels.Clear();
            if (r.UpKeys != null && r.UpLevels != null)
            {
                int n = Mathf.Min(r.UpKeys.Length, r.UpLevels.Length);
                for (int i = 0; i < n; i++)
                {
                    rt.UpgradeLevels.Add(new GunnerRuntime.GunnerStatLevel
                    {
                        Key = (GunnerStatKey)r.UpKeys[i],
                        Level = r.UpLevels[i]
                    });
                }
            }

            Transform attachAnchor = null;
            foreach (var slot in FindObjectsByType<SlotWorldButton>(FindObjectsSortMode.None))
            {
                if (slot.slotIndex == rt.EquippedSlot)
                {
                    attachAnchor = slot.barrelAnchor;
                    break;
                }
            }
            AttachEquippedGunnerVisual_Internal(rt.GunnerId, attachAnchor, rt.EquippedSlot);

        }

        slotToGunner.Clear();
        for (int i = 0; i < dto.SlotMap.Count; i++)
        {
            var s = dto.SlotMap[i];
            slotToGunner[s.SlotIndex] = s.GunnerId;
        }
    }

    /* ===================== PRESTIGE RESET ===================== */
    /// <summary>
    /// Reset gunners for prestige. If wipeOwnership = true, removes ownership
    /// and unequips everyone. If only wipeUpgrades = true, keeps ownership but
    /// resets level, points, learned stats and per-stat upgrade levels.
    /// </summary>
    public void ResetAll(bool wipeOwnership, bool wipeUpgrades, bool resetLevels)
    {
        DebugDumpAll("PRE");

        // Always unequip visuals/slots first
        var slots = new List<int>(slotToGunner.Keys);
        for (int i = 0; i < slots.Count; i++)
            UnequipFromSlot(slots[i]);

        if (wipeOwnership)
        {
            Debug.Log("[GunnerReset] WipeOwnership=TRUE -> clearing owned set; resetting runtimes; keeping preferred starter (if any).");

            ownedGunners.Clear();

            // Reset EVERY runtime to a fresh baseline so no old levels/stats leak through
            foreach (var kv in runtimes)
                ResetRuntimeToFresh(kv.Key, kv.Value);

            // Keep preferred starter and auto-equip to slot 0 (fresh runtime)
            if (preferredStarterGunner != null)
            {
                var pid = preferredStarterGunner.GunnerId;
                ownedGunners.Add(pid);

                if (!runtimes.TryGetValue(pid, out var rt))
                {
                    rt = new GunnerRuntime(preferredStarterGunner);
                    runtimes[pid] = rt;
                }
                ResetRuntimeToFresh(pid, rt);

                EquipToFirstFreeSlot(pid, preferSlotIndex: 0);
                Debug.Log($"[GunnerReset] Kept preferred starter: {pid} (fresh stats) and auto-equipped.");
            }

            Save();
            OnRosterChanged?.Invoke();
            DebugDumpAll("POST (wipe ownership)");
            return;
        }


        Debug.Log($"[GunnerReset] Applying resets: resetLevels={resetLevels}, wipeUpgrades={wipeUpgrades}");

        foreach (var kv in runtimes)
        {
            string gid = kv.Key;
            var rt = kv.Value;

            DebugDumpRuntime("BEFORE", gid, rt);

            if (resetLevels)
            {
                rt.Level = 1;
                rt.CurrentXp = 0f;
                rt.UnspentSkillPoints = 0;

                // Revert to StartingUnlocked only (remove level-milestone unlocks)
                var so = GetSO(gid);
                if (so != null)
                    rt.Unlocked = new HashSet<GunnerStatKey>(so.StartingUnlocked);
            }

            if (wipeUpgrades)
            {
                // Wipe per-stat investment completely
                rt.UpgradeLevels?.Clear();

                // Optional: if wiping upgrades should also wipe *all* unlocks (not just level-based), keep this line:
                // rt.Unlocked?.Clear();
                // Otherwise we rely on the resetLevels step to rebuild Unlocked to StartingUnlocked when requested.
            }

            // Always recompute derived HP from current state (after any changes above)
            RecomputeDerivedStats(gid, rt);

            // Clear any ongoing quest state on prestige
            rt.IsOnQuest = false;
            rt.QuestEndUnixTime = 0;

            DebugDumpRuntime("AFTER", gid, rt);
        }

        Save();
        OnRosterChanged?.Invoke();
        DebugDumpAll("POST");
    }
    /// <summary>Computes base value for a stat using level 0 upgrades.</summary>
    private float GetEffectiveStatValueForReset(string gunnerId, GunnerStatKey key)
    {
        var so = GetSO(gunnerId);
        if (so == null) return 0f;
        return GunnerUpgradeManager.Instance != null
            ? GunnerUpgradeManager.Instance.GetEffectiveStatValueAtLevel(so, key, 0)
            : 0f;
    }

    /// <summary>Equip the given gunner to the preferred slot (0 by default).
    /// If the anchor dictionary is empty (common right after resets), we rebuild it
    /// from SlotWorldButton in the scene. If still missing, we set the mapping now
    /// and defer the visual attach until anchors are available.</summary>
    public void EquipToFirstFreeSlot(string gunnerId, int preferSlotIndex = 0)
    {
        if (string.IsNullOrEmpty(gunnerId)) return;

        // Resolve anchor safely
        if (!slotAnchors.TryGetValue(preferSlotIndex, out var slotBarrel) || slotBarrel == null)
        {
            var slots = FindObjectsByType<SlotWorldButton>(FindObjectsSortMode.None);
            for (int i = 0; i < slots.Length; i++)
                slotAnchors[slots[i].slotIndex] = slots[i].barrelAnchor;

            slotBarrel = slotAnchors.TryGetValue(preferSlotIndex, out var t) ? t : null;
        }

        if (slotBarrel == null)
        {
            Debug.LogWarning($"[GunnerManager] No anchor for slot {preferSlotIndex}. Deferring visual; mapping only.");
            // set slot mapping now so gameplay continues
            slotToGunner[preferSlotIndex] = gunnerId;
            if (runtimes.TryGetValue(gunnerId, out var rt)) rt.EquippedSlot = preferSlotIndex;
            OnSlotGunnerChanged?.Invoke(preferSlotIndex);
            return;
        }

        if (!EquipToSlot(gunnerId, preferSlotIndex, slotBarrel))
            Debug.LogWarning($"[GunnerManager] Could not equip gunner {gunnerId} to preferred slot {preferSlotIndex} (anchor ok).");
    }


    /// <summary>Recompute derived runtime stats from current UpgradeLevels/unlocks.</summary>
    private void RecomputeDerivedStats(string gunnerId, GunnerRuntime rt)
    {
        // Health is the only one we persist directly on the runtime (others are merged into turrets on demand).
        rt.MaxHealth = GetEffectiveStatValueForReset(gunnerId, GunnerStatKey.Health);
        rt.MaxHealth = Mathf.Max(1f, rt.MaxHealth);
        rt.CurrentHealth = Mathf.Min(rt.CurrentHealth, rt.MaxHealth);
    }

    /// <summary>Hard reset a runtime to a fresh state based on its SO.</summary>
    private void ResetRuntimeToFresh(string gid, GunnerRuntime rt)
    {
        var so = GetSO(gid);
        rt.Level = 1;
        rt.CurrentXp = 0f;
        rt.UnspentSkillPoints = 0;
        rt.Unlocked = (so != null)
            ? new HashSet<GunnerStatKey>(so.StartingUnlocked)
            : new HashSet<GunnerStatKey>();
        rt.UpgradeLevels?.Clear();
        rt.IsOnQuest = false;
        rt.QuestEndUnixTime = 0;
        RecomputeDerivedStats(gid, rt);
        Debug.Log($"[GunnerReset] Fresh runtime applied -> id={gid} Lvl={rt.Level} HP={rt.MaxHealth} Unlocked={rt.Unlocked.Count} Upgrades={rt.UpgradeLevels.Count}");
    }


    #region DEBUG
    private void DebugDumpRuntime(string title, string gid, GunnerRuntime rt)
    {
        var upCount = rt.UpgradeLevels?.Count ?? 0;
        var unCount = rt.Unlocked?.Count ?? 0;
        Debug.Log($"[GunnerReset] {title} id={gid} " +
                  $"Lvl={rt.Level} XP={rt.CurrentXp} Pts={rt.UnspentSkillPoints} " +
                  $"MaxHP={rt.MaxHealth} CurHP={rt.CurrentHealth} " +
                  $"Upgrades={upCount} Unlocked={unCount} " +
                  $"Quest={(rt.IsOnQuest ? "Y" : "N")} Slot={rt.EquippedSlot}");
    }

    private void DebugDumpAll(string title)
    {
        Debug.Log($"[GunnerReset] ---- {title} ---- runtimes={runtimes.Count} owned={ownedGunners.Count}");
        foreach (var kv in runtimes)
            DebugDumpRuntime(title, kv.Key, kv.Value);
    }
    #endregion

}
