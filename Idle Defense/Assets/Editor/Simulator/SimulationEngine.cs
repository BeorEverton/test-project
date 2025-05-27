// Assets/Editor/Simulation/SimulationEngine.cs
// ------------------------------------------------
// Main head-less simulator – per-wave stats export
// ------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

using Assets.Scripts.SO;          // EnemyInfoSO, TurretInfoSO, PlayerBaseSO
using Assets.Scripts.Enemies;     // EnemyClass
using Assets.Scripts.Turrets;     // TurretType
using Assets.Scripts.WaveSystem;
using Assets.Scripts.Systems;  // WaveConfigSO, TurretUnlockTableSO

namespace IdleDefense.Editor.Simulation
{
    // ---------------------------------------------
    // Minimal per-enemy runtime struct
    // ---------------------------------------------
    struct EnemySim
    {
        public EnemyClass Class;
        public float Hp;
        public float Y;
        public float Speed;
        public float Damage;
        public ulong Coin;
        public bool IsBoss;
        public float TimeSinceLastAttack;
        public float AttackRange;
    }

    // ---------------------------------------------
    // Main engine
    // ---------------------------------------------
    public static class SimulationEngine
    {
        // ------------------------------
        // Run one simulation instance
        // ------------------------------
        public static SimStats Run(
            float minutes,
            float clicksPerSec,
            SpendingMode mode)
        {
            //----------------------------------------------------------
            // 0) Load all SO data needed for the sim
            //----------------------------------------------------------
            var enemyInfos = AssetDatabase
                .FindAssets("t:EnemyInfoSO")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(p => AssetDatabase.LoadAssetAtPath<EnemyInfoSO>(p))
                .ToDictionary(e => e.EnemyClass, e => e);

            var turretInfos = AssetDatabase
                .FindAssets("t:TurretInfoSO")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(p => AssetDatabase.LoadAssetAtPath<TurretInfoSO>(p))
                .ToDictionary(t => t.TurretType, t => new TurretBlueprint(t));

            var waves = AssetDatabase
                .FindAssets("t:WaveConfigSO")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(p => AssetDatabase.LoadAssetAtPath<WaveConfigSO>(p))
                .OrderBy(w => w.WaveStartIndex)
                .ToList();

            var unlockTable = AssetDatabase
                .FindAssets("t:TurretUnlockTableSO")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(p => AssetDatabase.LoadAssetAtPath<TurretUnlockTableSO>(p))
                .First();

            var baseSO = AssetDatabase
                .FindAssets("t:PlayerBaseSO")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(p => AssetDatabase.LoadAssetAtPath<PlayerBaseSO>(p))
                .First();

            //----------------------------------------------------------
            // 1) Helper lambdas
            //----------------------------------------------------------
            float MinBaseRegenInterval = 0.5f; // hard floor (same as runtime)

            float CostForBase(PlayerUpgradeType t, int level)
            {
                float cost = float.MaxValue; // Standardize to max value
                if (t == PlayerUpgradeType.MaxHealth)
                {
                    
                    cost = baseSO.MaxHealthUpgradeBaseCost * Mathf.Pow(1.1f, level);
                }
                else if (t == PlayerUpgradeType.RegenAmount)
                {
                    cost = baseSO.RegenAmountUpgradeBaseCost * Mathf.Pow(1.1f, level);
                }
                else if (t == PlayerUpgradeType.RegenInterval)
                {
                    // ensure the interval does not go below the minimum
                    cost = baseSO.RegenIntervalUpgradeBaseCost * Mathf.Pow(1.1f, level);
                    if (baseSO.RegenInterval - baseSO.RegenIntervalUpgradeAmount * level < MinBaseRegenInterval)
                        return float.MaxValue; // too expensive to upgrade below the min interval
                }
                
                return cost;
                
            }

            float GetHybridCost(float baseCost, int level)
            {
                const int hybridThreshold = 50;
                const float quadraticFactor = 0.1f;
                const float exponentialPower = 1.15f;

                if (level < hybridThreshold)
                    return baseCost * (1f + level * level * quadraticFactor);
                else
                    return baseCost * Mathf.Pow(exponentialPower, level);
            }

            float GetExponentialCost(float baseCost, int level, float multiplier)
            {
                return baseCost * Mathf.Pow(multiplier, level);
            }

            float GetExponentialCostPlusLevel(float baseCost, int level, float multiplier)
            {
                // matches TurretUpgradeManager.UpgradeDamage, FireRate…
                return baseCost + Mathf.Pow(multiplier, level) + level;
            }


            //----------------------------------------------------------
            // 2) Global sim state / stats
            //----------------------------------------------------------
            var stats = new SimStats { Waves = new List<WaveStat>() };

            var strategy = SpendingStrategyFactory.Create(mode);

            float simTime = 0f;
            float dt = 0.05f;

            ulong coins = 0;
            int waveIndex = 1;

            // base health / regen
            float baseMaxHealth = baseSO.MaxHealth;
            float baseHealth = baseMaxHealth;
            float regenAmount = baseSO.RegenAmount;
            float regenDelay = baseSO.RegenDelay;
            float regenInterval = baseSO.RegenInterval;
            float regenDelayTimer = 0f, regenTickTimer = 0f;
            int maxHpLvl = 0, regenAmtLvl = 0, regenIntLvl = 0;

            // click speed bonus
            float clickBonus = 0f;
            const float bonusPerClick = 1f, decayPerSec = 1f;

            // slot / turret state
            var slots = new List<TurretBlueprint>();
            var shotTimers = new List<float>();

            foreach (var row in unlockTable.Entries)
                if (row.WaveToUnlock <= 1 && turretInfos.ContainsKey(row.Type))
                {
                    slots.Add(turretInfos[row.Type]);
                    shotTimers.Add(0f);
                }

            // damage by turret type (per-wave)
            double[] perWaveDmg = new double[
                Enum.GetValues(typeof(TurretType)).Length];

            // wave-local accumulators
            WaveStat wStat = new WaveStat();

            // enemy collections
            var pendingSpawns = new Queue<EnemySim>();
            var live = new List<EnemySim>();

            float spawnTimer = 0f;
            float spawnIntervalCurrent = 0f;
            bool bossIncoming = false;
            EnemySim bossTemplate = default;

            // view-port spawn Y (match runtime)
            float spawnY = Camera.main.ViewportToWorldPoint(
                new Vector3(0.5f, 1f, 0f)).y + 1.0f;

            //----------------------------------------------------------
            // 3) Wave initialiser
            //----------------------------------------------------------
            void InitWave()
            {
                var tpl = waves.Last(w => w.WaveStartIndex <= waveIndex);

                //----------------------------------------------------------------
                // spawn queue for every EnemyWaveEntry
                //----------------------------------------------------------------
                pendingSpawns.Clear();
                foreach (var e in tpl.EnemyWaveEntries)
                {
                    var info = enemyInfos[e.EnemyPrefab
                        .GetComponent<Enemy>().Info.EnemyClass];

                    float hp = info.MaxHealth *
                                 Mathf.Pow(info.HealthMultiplierByWaveCount, waveIndex) +
                                 waveIndex;

                    float spd = Random.Range(
                                    info.MovementSpeed - info.MovementSpeedDifference,
                                    info.MovementSpeed + info.MovementSpeedDifference) +
                                 Mathf.Floor((waveIndex - 1) / 5f);

                    float dmg = info.Damage +
                                 info.Damage * ((waveIndex / 100f) * 2f);

                    ulong coin = (ulong)(
                        info.CoinDropAmount *
                        info.CoinDropMultiplierByWaveCount +
                        waveIndex * info.CoinDropMultiplierByWaveCount);

                    int count = e.NumberOfEnemies + waveIndex;
                    for (int k = 0; k < count; k++)
                        pendingSpawns.Enqueue(new EnemySim
                        {
                            Class = info.EnemyClass,
                            Hp = hp,
                            Y = spawnY,
                            Speed = spd,
                            Damage = dmg,
                            Coin = coin,
                            IsBoss = false,
                            TimeSinceLastAttack = 0f,
                            AttackRange = info.AttackRange
                        });
                }

                //----------------------------------------------------------------
                // boss / miniboss template (every 5 / 10 waves)
                //----------------------------------------------------------------
                bossIncoming = false;
                if (waveIndex % 5 == 0)
                {
                    var info = enemyInfos
                        [tpl.EnemyWaveEntries[0]
                          .EnemyPrefab.GetComponent<Enemy>().Info.EnemyClass];

                    var clone = ScriptableObject.Instantiate(info);
                    if (waveIndex % 10 == 0) clone.AttackRange += 0.6f;
                    else clone.AttackRange += 0.2f;

                    float hpMult = (waveIndex % 10 == 0) ? 100f : 30f;
                    float dmgMult = (waveIndex % 10 == 0) ? 40f : 20f;
                    ulong coinMul = (waveIndex % 10 == 0) ? 40u : 20u;

                    bossTemplate = new EnemySim
                    {
                        Class = clone.EnemyClass,
                        Hp = pendingSpawns.Peek().Hp * hpMult,
                        Y = spawnY,
                        Speed = pendingSpawns.Peek().Speed,
                        Damage = pendingSpawns.Peek().Damage * dmgMult,
                        Coin = pendingSpawns.Peek().Coin * coinMul,
                        IsBoss = true,
                        AttackRange = clone.AttackRange
                    };
                    bossIncoming = true;
                }

                spawnIntervalCurrent = tpl.TimeBetweenSpawns;
                spawnTimer = spawnIntervalCurrent;

                // reset wave accumulators
                wStat = new WaveStat
                {
                    Wave = waveIndex,
                    HealthStart = baseHealth
                };
                Array.Clear(perWaveDmg, 0, perWaveDmg.Length);
            }

            //----------------------------------------------------------
            // 4) Simulate!
            //----------------------------------------------------------
            InitWave();
            while (simTime < minutes * 60f)
            {
                NEXT_FRAME:

                //------------------ enemy spawning --------------------
                spawnTimer -= dt;
                if (spawnTimer <= 0f)
                {
                    if (pendingSpawns.Count > 0)
                        live.Add(pendingSpawns.Dequeue());
                    else if (bossIncoming)
                    {
                        live.Add(bossTemplate);
                        bossIncoming = false;
                    }
                    spawnTimer = spawnIntervalCurrent;
                }

                //------------------ click bonus (spdBonus) -----------------------
                // mirror GameManager: initialBoost=5 per click, holdRate=5/sec, decayDelay=1s, decayRate=holdRate*0.8
                const float initialBoost = 5f;
                const float holdRate = 5f;
                const float decayDelay = 1f;
                const float decayRate = holdRate * 0.8f;

                float bonusDelayTimer = 0f;

                if (clicksPerSec > 0f)
                {
                    // each simulated click adds initialBoost
                    clickBonus += initialBoost * clicksPerSec * dt;
                    bonusDelayTimer = 0f;
                }
                else
                {
                    // start decay after delay
                    bonusDelayTimer += dt;
                    if (bonusDelayTimer >= decayDelay)
                        clickBonus = Mathf.Max(0f, clickBonus - decayRate * dt);
                }

                // clamp to the same maxSpdBonus = 100f as runtime
                clickBonus = Mathf.Clamp(clickBonus, 0f, 100f);

                // record the raw spdBonus value for export
                wStat.SpeedBoostClicks = (int)(clicksPerSec * dt);

                //------------------ turret firing (first!) ---------------------
                for (int t = 0; t < slots.Count; t++)
                {
                    var bp = slots[t];
                    shotTimers[t] += dt;
                    float effectiveFireRate = bp.FireRate * (1f + clickBonus);
                    float interval = 1f / effectiveFireRate;

                    while (shotTimers[t] >= interval)
                    {
                        shotTimers[t] -= interval;
                        // find first enemy in range
                        int idx = live.FindIndex(e => e.Y <= bp.Range);
                        if (idx < 0) break;
                        var e = live[idx];

                        // critital hit chance
                        bool crit = Random.value < bp.CritChance * 0.01f;
                        float critMultiplier = 1f + bp.CritDamageMultiplier * 0.01f;  
                        float dmg = bp.Damage * (crit ? critMultiplier : 1f);

                        // deal damage
                        if (dmg > e.Hp) dmg = e.Hp; // to simulate correctly the damage dealt
                        e.Hp -= dmg;
                        wStat.DamageDealt += dmg;
                        stats.TotalDamageDealt += dmg;
                        perWaveDmg[(int)bp.Type] += dmg;

                        if (e.Hp <= 0f)
                        {
                            if (e.IsBoss) { wStat.BossesKilled++; stats.BossesKilled++; }
                            else { wStat.EnemiesKilled++; stats.EnemiesKilled++; }
                            coins += e.Coin;
                            wStat.MoneyEarned += e.Coin;
                            live.RemoveAt(idx);
                        }
                        else
                        {
                            live[idx] = e;
                        }
                    }
                }

                //------------------ enemy movement & base damage ------
                // flag to track if base has taken damage
                bool baseDamaged = false;

                for (int i = live.Count - 1; i >= 0; i--)
                {
                    var e = live[i];
                    e.Y -= e.Speed * dt;

                    if (e.Y <= e.AttackRange)
                    {
                        e.TimeSinceLastAttack += dt;
                        var info = enemyInfos[e.Class];
                        float atkInterval = 1f / info.AttackSpeed;
                        if (e.TimeSinceLastAttack >= atkInterval)
                        {
                            baseHealth = Mathf.Max(0f, baseHealth - e.Damage);
                            wStat.DamageTaken += e.Damage;
                            e.TimeSinceLastAttack = 0f;

                            if (baseHealth <= 0f)
                            {
                                // record a FAILED wave row
                                wStat.HealthEnd = 0f;
                                wStat.WaveBeaten = false;
                                stats.Waves.Add(wStat);
                                stats.MissionsFailed++;
                                waveIndex = Mathf.Max(1, waveIndex - 10);
                                baseHealth = baseMaxHealth;
                                regenDelayTimer = 0f;
                                regenTickTimer = 0f;
                                baseDamaged = true;
                                live.Clear();
                                pendingSpawns.Clear();
                                InitWave();
                                break;  // exit movement loop to next frame
                            }
                        }
                    }

                    live[i] = e;
                }

                //------------------ base regen ------------------------
                if (baseDamaged && baseHealth < baseMaxHealth && live.Count == 0)
                {
                    regenDelayTimer += dt;
                    if (regenDelayTimer >= regenDelay)
                    {
                        regenTickTimer += dt;
                        if (regenTickTimer >= regenInterval)
                        {
                            float healed = Mathf.Min(regenAmount, baseMaxHealth - baseHealth);
                            baseHealth += healed;
                            baseHealth = Mathf.Clamp(baseHealth, 0f, baseMaxHealth);
                            stats.TotalHealthRepaired += healed;
                            wStat.HealthRegen += healed;
                            regenTickTimer = 0f;

                            // Clear damage flag when fully healed
                            if (baseHealth >= baseMaxHealth)
                            {
                                baseDamaged = false;
                            }

                        }
                    }
                }
                else if (live.Count > 0)
                {
                    regenDelayTimer = 0f;
                    regenTickTimer = 0f;
                }                

                // ------------------ spending ---------------------------------
                ulong minCost = ulong.MaxValue;
                bool chooseBase = false;
                PlayerUpgradeType baseChoice = PlayerUpgradeType.MaxHealth;
                TurretUpgradeType chosenTurretStat = TurretUpgradeType.Damage;  // default

                // scan turret upgrades
                // scan turret upgrades with proper scaling
                for (int t = 0; t < slots.Count; t++)
                {
                    #region turret upgrade stats
                    var tp = slots[t];

                    // DAMAGE (exponential + level)
                    {
                        float raw = GetExponentialCostPlusLevel(
                            tp.DamageUpgradeBaseCost,
                            tp.DamageLevel,
                            tp.DamageCostExponentialMultiplier);
                        ulong cost = (ulong)Mathf.Ceil(raw);
                        if (cost > 0 && cost < minCost)
                        {
                            minCost = cost;
                            chooseBase = false;
                            // remember which turret & stat your strategy should pick
                            strategy.SetNextTurretIndex(t);
                            strategy.SetNextUpgradeType(TurretUpgradeType.Damage);
                            chosenTurretStat = TurretUpgradeType.Damage;

                        }
                    }

                    // FIRE RATE (exponential + level)
                    {
                        float raw = GetExponentialCostPlusLevel(
                            tp.FireRateUpgradeBaseCost,
                            tp.FireRateLevel,
                            tp.FireRateCostExponentialMultiplier);
                        ulong cost = (ulong)Mathf.Ceil(raw);
                        if (cost > 0 && cost < minCost)
                        {
                            minCost = cost;
                            chooseBase = false;
                            strategy.SetNextTurretIndex(t);
                            strategy.SetNextUpgradeType(TurretUpgradeType.FireRate);
                            chosenTurretStat = TurretUpgradeType.FireRate;
                        }
                    }

                    // CRITICAL CHANCE (pure exponential)
                    {
                        float raw = GetExponentialCost(
                            tp.CriticalChanceUpgradeBaseCost,
                            tp.CriticalChanceLevel,
                            tp.CriticalChanceCostExponentialMultiplier);
                        ulong cost = (ulong)Mathf.Ceil(raw);
                        if (cost > 0 && cost < minCost)
                        {
                            minCost = cost;
                            chooseBase = false;
                            strategy.SetNextTurretIndex(t);
                            strategy.SetNextUpgradeType(TurretUpgradeType.CriticalChance);
                            chosenTurretStat = TurretUpgradeType.CriticalChance;
                        }
                    }

                    // CRIT DAMAGE MULTIPLIER (pure exponential)
                    {
                        float raw = GetExponentialCost(
                            tp.CriticalDamageMultiplierUpgradeBaseCost,
                            tp.CriticalDamageMultiplierLevel,
                            tp.CriticalDamageCostExponentialMultiplier);
                        ulong cost = (ulong)Mathf.Ceil(raw);
                        if (cost > 0 && cost < minCost)
                        {
                            minCost = cost;
                            chooseBase = false;
                            strategy.SetNextTurretIndex(t);
                            strategy.SetNextUpgradeType(TurretUpgradeType.CriticalDamageMultiplier);
                            chosenTurretStat = TurretUpgradeType.CriticalDamageMultiplier;
                        }
                    }

                    // EXPLOSION RADIUS (hybrid quadratic/exponential)
                    {
                        float raw = GetHybridCost(
                            tp.ExplosionRadiusUpgradeBaseCost,
                            tp.ExplosionRadiusLevel);
                        ulong cost = (ulong)Mathf.Ceil(raw);
                        if (cost > 0 && cost < minCost)
                        {
                            minCost = cost;
                            chooseBase = false;
                            strategy.SetNextTurretIndex(t);
                            strategy.SetNextUpgradeType(TurretUpgradeType.ExplosionRadius);
                            chosenTurretStat = TurretUpgradeType.ExplosionRadius;
                        }
                    }

                    // SPLASH DAMAGE (hybrid)
                    {
                        float raw = GetHybridCost(
                            tp.SplashDamageUpgradeBaseCost,
                            tp.SplashDamageLevel);
                        ulong cost = (ulong)Mathf.Ceil(raw);
                        if (cost > 0 && cost < minCost)
                        {
                            minCost = cost;
                            chooseBase = false;
                            strategy.SetNextTurretIndex(t);
                            strategy.SetNextUpgradeType(TurretUpgradeType.SplashDamage);
                            chosenTurretStat = TurretUpgradeType.SplashDamage;
                        }
                    }

                    // PIERCE CHANCE (hybrid)
                    {
                        float raw = GetHybridCost(
                            tp.PierceChanceUpgradeBaseCost,
                            tp.PierceChanceLevel);
                        ulong cost = (ulong)Mathf.Ceil(raw);
                        if (cost > 0 && cost < minCost)
                        {
                            minCost = cost;
                            chooseBase = false;
                            strategy.SetNextTurretIndex(t);
                            strategy.SetNextUpgradeType(TurretUpgradeType.PierceChance);
                            chosenTurretStat = TurretUpgradeType.PierceChance;
                        }
                    }

                    // PIERCE DAMAGE FALLOFF (hybrid)
                    {
                        float raw = GetHybridCost(
                            tp.PierceDamageFalloffUpgradeBaseCost,
                            tp.PierceDamageFalloffLevel);
                        ulong cost = (ulong)Mathf.Ceil(raw);
                        if (cost > 0 && cost < minCost)
                        {
                            minCost = cost;
                            chooseBase = false;
                            strategy.SetNextTurretIndex(t);
                            strategy.SetNextUpgradeType(TurretUpgradeType.PierceDamageFalloff);
                            chosenTurretStat = TurretUpgradeType.PierceDamageFalloff;
                        }
                    }

                    // PELLET COUNT (hybrid)
                    {
                        float raw = GetHybridCost(
                            tp.PelletCountUpgradeBaseCost,
                            tp.PelletCountLevel);
                        ulong cost = (ulong)Mathf.Ceil(raw);
                        if (cost > 0 && cost < minCost)
                        {
                            minCost = cost;
                            chooseBase = false;
                            strategy.SetNextTurretIndex(t);
                            strategy.SetNextUpgradeType(TurretUpgradeType.PelletCount);
                            chosenTurretStat = TurretUpgradeType.PelletCount;
                        }
                    }

                    // DAMAGE FALLOFF OVER DISTANCE (hybrid)
                    {
                        float raw = GetHybridCost(
                            tp.DamageFalloffOverDistanceUpgradeBaseCost,
                            tp.DamageFalloffOverDistanceLevel);
                        ulong cost = (ulong)Mathf.Ceil(raw);
                        if (cost > 0 && cost < minCost)
                        {
                            minCost = cost;
                            chooseBase = false;
                            strategy.SetNextTurretIndex(t);
                            strategy.SetNextUpgradeType(TurretUpgradeType.DamageFalloffOverDistance);
                            chosenTurretStat = TurretUpgradeType.DamageFalloffOverDistance;
                        }
                    }

                    // PERCENT BONUS DPS (hybrid)
                    {
                        float raw = GetHybridCost(
                            tp.PercentBonusDamagePerSecUpgradeBaseCost,
                            tp.PercentBonusDamagePerSecLevel);
                        ulong cost = (ulong)Mathf.Ceil(raw);
                        if (cost > 0 && cost < minCost)
                        {
                            minCost = cost;
                            chooseBase = false;
                            strategy.SetNextTurretIndex(t);
                            strategy.SetNextUpgradeType(TurretUpgradeType.PercentBonusDamagePerSec);
                            chosenTurretStat = TurretUpgradeType.PercentBonusDamagePerSec;
                        }
                    }

                    // SLOW EFFECT (hybrid)
                    {
                        float raw = GetHybridCost(
                            tp.SlowEffectUpgradeBaseCost,
                            tp.SlowEffectLevel);
                        ulong cost = (ulong)Mathf.Ceil(raw);
                        if (cost > 0 && cost < minCost)
                        {
                            minCost = cost;
                            chooseBase = false;
                            strategy.SetNextTurretIndex(t);
                            strategy.SetNextUpgradeType(TurretUpgradeType.SlowEffect);
                            chosenTurretStat = TurretUpgradeType.SlowEffect;
                        }
                    }

                    // KNOCKBACK STRENGTH (pure exponential)
                    {
                        float raw = GetExponentialCost(
                            tp.KnockbackStrengthUpgradeBaseCost,
                            tp.KnockbackStrengthLevel,
                            tp.KnockbackStrengthCostExponentialMultiplier);
                        ulong cost = (ulong)Mathf.Ceil(raw);
                        if (cost > 0 && cost < minCost)
                        {
                            minCost = cost;
                            chooseBase = false;
                            strategy.SetNextTurretIndex(t);
                            strategy.SetNextUpgradeType(TurretUpgradeType.KnockbackStrength);
                            chosenTurretStat = TurretUpgradeType.KnockbackStrength;
                        }
                    }
                    #endregion
                }

                // allow RANDOM strategy to pick any affordable item
                bool forceRandom = (mode == SpendingMode.Random);

                if (forceRandom)
                {
                    chooseBase = Random.value < 0.5f;          // 50-50
                    if (chooseBase)
                        baseChoice = (PlayerUpgradeType)Random.Range(0, 3);
                }

                if (chooseBase)
                {
                    // if the strategy is random, it must set a new min cost
                    minCost = ulong.MaxValue;
                }

                // scan base upgrades
                foreach (PlayerUpgradeType pu in Enum.GetValues(typeof(PlayerUpgradeType)))
                {
                    int lvl = pu switch
                    {
                        PlayerUpgradeType.MaxHealth => maxHpLvl,
                        PlayerUpgradeType.RegenAmount => regenAmtLvl,
                        _ => regenIntLvl
                    };
                    float rawCost = CostForBase(pu, lvl);
                    ulong cost = (ulong)Mathf.RoundToInt(rawCost);
                    if (cost > 0 && cost < minCost)
                    {
                        minCost = cost;
                        chooseBase = true;
                        baseChoice = pu;
                    }                    
                }

                if (minCost > 0 && coins >= minCost)
                {
                    coins -= minCost;
                    wStat.MoneySpent += minCost;
                    stats.MoneySpent += minCost;

                    if (chooseBase)
                    {
                        switch (baseChoice)
                        {
                            case PlayerUpgradeType.MaxHealth:
                                maxHpLvl++;
                                baseMaxHealth += baseSO.MaxHealthUpgradeAmount;
                                baseHealth = Mathf.Min(baseHealth + baseSO.MaxHealthUpgradeAmount, baseMaxHealth);

                                wStat.MaxHealthLevel = maxHpLvl;
                                wStat.CurrentMaxHealth = baseMaxHealth;
                                break;


                            case PlayerUpgradeType.RegenAmount:
                                regenAmtLvl++;
                                regenAmount += baseSO.RegenAmountUpgradeAmount;
                                wStat.RegenAmountLevel = regenAmtLvl;
                                wStat.CurrentRegenAmount = regenAmount;
                                break;

                            case PlayerUpgradeType.RegenInterval:
                                regenIntLvl++;
                                regenInterval = Mathf.Max(
                                    MinBaseRegenInterval,
                                    regenInterval - baseSO.RegenIntervalUpgradeAmount);
                                wStat.RegenIntervalLevel = regenIntLvl;
                                wStat.CurrentRegenInterval = regenInterval;
                                break;

                        }
                        wStat.BaseUpgrades++;
                    }
                    else
                    {
                        // Keep track of what was upgraded
                        switch (chosenTurretStat)
                        {
                            case TurretUpgradeType.Damage:
                                wStat.DamageUpgrades++; break;
                            case TurretUpgradeType.FireRate:
                                wStat.FireRateUpgrades++; break;
                            case TurretUpgradeType.CriticalChance:
                                wStat.CriticalChanceUpgrades++; break;
                            case TurretUpgradeType.CriticalDamageMultiplier:
                                wStat.CriticalDamageMultiplierUpgrades++; break;
                            case TurretUpgradeType.ExplosionRadius:
                                wStat.ExplosionRadiusUpgrades++; break;
                            case TurretUpgradeType.SplashDamage:
                                wStat.SplashDamageUpgrades++; break;
                            case TurretUpgradeType.PierceChance:
                                wStat.PierceChanceUpgrades++; break;
                            case TurretUpgradeType.PierceDamageFalloff:
                                wStat.PierceDamageFalloffUpgrades++; break;
                            case TurretUpgradeType.PelletCount:
                                wStat.PelletCountUpgrades++; break;
                            case TurretUpgradeType.KnockbackStrength:
                                wStat.KnockbackStrengthUpgrades++; break;
                            case TurretUpgradeType.DamageFalloffOverDistance:
                                wStat.DamageFalloffOverDistanceUpgrades++; break;
                            case TurretUpgradeType.PercentBonusDamagePerSec:
                                wStat.PercentBonusDamagePerSecUpgrades++; break;
                            case TurretUpgradeType.SlowEffect:
                                wStat.SlowEffectUpgrades++; break;
                        }

                        strategy.Tick(ref coins, ref slots, waveIndex);
                        wStat.TurretUpgrades++;                        
                    }
                    // Refresh the minCost for the next round
                    minCost = ulong.MaxValue;
                }

                if (live.Count == 0 && pendingSpawns.Count == 0 && !bossIncoming)
                {
                    wStat.HealthEnd = baseHealth;   // before post wave regen
                    wStat.WaveBeaten = true;
                    regenDelayTimer = 0f;           // block extra regen until next wave
                    regenTickTimer = 0f;

                    // copy per type DPS
                    wStat.MachineGunDamage = perWaveDmg[(int)TurretType.MachineGun];
                    wStat.ShotgunDamage = perWaveDmg[(int)TurretType.Shotgun];
                    wStat.SniperDamage = perWaveDmg[(int)TurretType.Sniper];
                    wStat.MissileLauncherDamage = perWaveDmg[(int)TurretType.MissileLauncher];
                    wStat.LaserDamage = perWaveDmg[(int)TurretType.Laser];

                    stats.Waves.Add(wStat);
                    stats.MaxZone = Mathf.Max(stats.MaxZone, wStat.Wave);
                    stats.WavesBeaten++;
                    waveIndex++;
                    InitWave();
                }


                END_OF_FRAME:
                simTime += dt;
            }

            stats.SimMinutes = minutes;
            return stats;
        }
    }
}
