// Assets/Editor/Simulation/SimulationEngine.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Assets.Scripts.SO;          // EnemyInfoSO, TurretInfoSO, PlayerBaseSO
using Assets.Scripts.Turrets;     // TurretType
using Assets.Scripts.WaveSystem;  // WaveConfigSO, TurretUnlockTableSO
using Random = UnityEngine.Random;
using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;

namespace IdleDefense.Editor.Simulation
{
    // --- simplified per-enemy state ---
    struct EnemySim
    {
        public EnemyClass Class;
        public float Hp;
        public float Y;
        public float Speed;
        public float Damage;
        public ulong Coin;
        public bool IsBoss;
        public float timeSinceLastAttack; // for AttackSpeed
    }

    public static class SimulationEngine
    {
        public static SimStats Run(
            float minutes,
            float clicksPerSec,
            SpendingMode mode)
        {
            // --- 0) seed RNG to match in-game randomness ---
            Random.InitState(Guid.NewGuid().GetHashCode());

            // --- 1) compute spawn Y exactly as EnemySpawner does ---
            float screenTop = Camera.main.ViewportToWorldPoint(
                new Vector3(0.5f, 1f, 0f)).y + 1f;
            float spawnY = screenTop;

            // --- 2) load enemy templates by class ---
            var enemyDict = AssetDatabase
              .FindAssets("t:EnemyInfoSO")
              .Select(AssetDatabase.GUIDToAssetPath)
              .Select(path => AssetDatabase.LoadAssetAtPath<EnemyInfoSO>(path))
              .ToDictionary(so => so.EnemyClass, so => so);

            // --- 3) load wave templates ---
            var baseWaves = AssetDatabase
              .FindAssets("t:WaveConfigSO")
              .Select(AssetDatabase.GUIDToAssetPath)
              .Select(path => AssetDatabase.LoadAssetAtPath<WaveConfigSO>(path))
              .OrderBy(so => so.WaveStartIndex)
              .ToList();

            // --- 4) turret blueprints & unlock table ---
            var turretDict = AssetDatabase
              .FindAssets("t:TurretInfoSO")
              .Select(AssetDatabase.GUIDToAssetPath)
              .Select(path => AssetDatabase.LoadAssetAtPath<TurretInfoSO>(path))
              .ToDictionary(so => so.TurretType, so => new TurretBlueprint(so));

            var unlockTable = AssetDatabase
              .FindAssets("t:TurretUnlockTableSO")
              .Select(AssetDatabase.GUIDToAssetPath)
              .Select(path => AssetDatabase.LoadAssetAtPath<TurretUnlockTableSO>(path))
              .First();
            var unlockRows = unlockTable.Entries;

            // --- 5) player base stats for regen/fail ---
            var baseSO = AssetDatabase
              .FindAssets("t:PlayerBaseSO")
              .Select(AssetDatabase.GUIDToAssetPath)
              .Select(path => AssetDatabase.LoadAssetAtPath<PlayerBaseSO>(path))
              .First();
            float baseMaxHealth = baseSO.MaxHealth;
            float regenAmount = baseSO.RegenAmount;
            float regenDelay = baseSO.RegenDelay;
            float regenInterval = baseSO.RegenInterval;

            // simulate base upgrade levels
            int maxHealthLvl = 0;
            int regenAmountLvl = 0;
            int regenIntervalLvl = 0;

            // helper to compute base upgrade cost
            float BaseCost(PlayerUpgradeType type)
            {
                switch (type)
                {
                    case PlayerUpgradeType.MaxHealth:
                        return baseSO.MaxHealthUpgradeBaseCost
                             * Mathf.Pow(1.1f, maxHealthLvl);
                    case PlayerUpgradeType.RegenAmount:
                        return baseSO.RegenAmountUpgradeBaseCost
                             * Mathf.Pow(1.1f, regenAmountLvl);
                    case PlayerUpgradeType.RegenInterval:
                        return baseSO.RegenIntervalUpgradeBaseCost
                             * Mathf.Pow(1.1f, regenIntervalLvl);
                    default: throw new ArgumentOutOfRangeException();
                }
            }


            // --- 6) sim state & stats ---
            float dt = 0.05f;
            float simTime = 0f;
            ulong coins = 0;
            int waveIndex = 1;

            float baseHealth = baseMaxHealth;
            float delayTimer = 0f;
            float tickTimer = 0f;

            // initial turret slots
            var slots = new List<TurretBlueprint>();
            foreach (var row in unlockRows)
                    if (row.WaveToUnlock <= 1 && turretDict.ContainsKey(row.Type))
            slots.Add(turretDict[row.Type]);

            var shotTimers = new List<float>(slots.Count);
            for (int i = 0; i < slots.Count; i++)
            shotTimers.Add(0f);


            // click bonus
            float spdBonus = 0f;
            const float bonusPerClick = 1f, decayPerSec = 1f;

            var strategy = SpendingStrategyFactory.Create(mode);
            var stats = new SimStats
            {
                UpgradeHistory = new List<string>(),
                TurretSnapshots = new List<string>(),
                BaseSnapshots = new List<string>()
            };


            // damage by type
            var dmgByType = new Dictionary<TurretType, double>();
            foreach (TurretType t in Enum.GetValues(typeof(TurretType)))
                dmgByType[t] = 0.0;

            // wave/enemy state
            int enemiesToSpawn = 0;
            float spawnTimer = 0f;
            ulong coinPerKill = 0;
            float enemySpeed = 0f;
            ulong waveClearBonus = 0;
            var enemies = new List<EnemySim>();

            // boss flags
            bool hasBoss = false;
            bool bossSpawned = false;
            float bossHp = 0f;
            float bossDamage = 0f;
            ulong bossCoin = 0;

            // turret vertical position (world y)
            const float turretY = -0.7f;

            // --- InitWave helper ---
            void InitWave()
            {
                // pick template
                var tpl = baseWaves.Last(w => w.WaveStartIndex <= waveIndex);

                // total count
                enemiesToSpawn = tpl.EnemyWaveEntries
                    .Sum(e => e.NumberOfEnemies + waveIndex);
                spawnTimer = tpl.TimeBetweenSpawns;

                // scale base enemy from SO
                var baseEntry = tpl.EnemyWaveEntries[0];
                var infoSO = enemyDict[baseEntry.EnemyPrefab
                                   .GetComponent<Enemy>().Info.EnemyClass];

                // stats from SO + waveIndex
                float hp = infoSO.MaxHealth
                           * Mathf.Pow(infoSO.HealthMultiplierByWaveCount, waveIndex)
                           + waveIndex;
                enemySpeed = infoSO.MovementSpeed
                           + Mathf.Floor((waveIndex - 1) / 5f);
                float dmg = infoSO.Damage
                           + infoSO.Damage * ((waveIndex / 100f) * 2f);
                coinPerKill = (ulong)(infoSO.CoinDropAmount
                           * infoSO.CoinDropMultiplierByWaveCount
                           + waveIndex * infoSO.CoinDropMultiplierByWaveCount);

                // clear bonus
                waveClearBonus = (ulong)(5 + waveIndex * 2);

                // set up normal spawns
                enemies.Clear();
                hasBoss = false;
                bossSpawned = false;

                // boss/miniboss?
                bool mini = (waveIndex % 5 == 0 && waveIndex % 10 != 0);
                bool boss = (waveIndex % 10 == 0);
                if (mini || boss)
                {
                    hasBoss = true;
                    bossHp = hp * (mini ? 30f : 100f);
                    bossDamage = dmg * (mini ? 20f : 40f);
                    bossCoin = (ulong)(coinPerKill * (mini ? 20u : 40u));
                }

                // preload normals
                for (int i = 0; i < tpl.EnemyWaveEntries.Count; i++)
                {
                    var entry = tpl.EnemyWaveEntries[i];
                    int cnt = entry.NumberOfEnemies + waveIndex;
                    for (int k = 0; k < cnt; k++)
                    {
                        enemies.Add(new EnemySim
                        {
                            Class = entry.EnemyPrefab
                                                      .GetComponent<Enemy>().Info.EnemyClass,
                            Hp = hp,
                            Y = spawnY,
                            Speed = enemySpeed,
                            Damage = dmg,
                            Coin = coinPerKill,
                            IsBoss = false,
                            timeSinceLastAttack = 0f
                        });
                    }
                }
            }

            InitWave();
            Debug.Log("[SIM START] CPS=" + clicksPerSec + " Mode=" + mode);

            // --- main loop ---
            while (simTime < minutes * 60f)
            {
                // 1) spawn boss when normals done
                if (enemiesToSpawn <= 0 && hasBoss && !bossSpawned)
                {
                    spawnTimer -= dt;
                    if (spawnTimer <= 0f)
                    {
                        enemies.Add(new EnemySim
                        {
                            Class = enemies[0].Class,
                            Hp = bossHp,
                            Y = spawnY,
                            Speed = enemySpeed,
                            Damage = bossDamage,
                            Coin = bossCoin,
                            IsBoss = true,
                            timeSinceLastAttack = 0f
                        });
                        bossSpawned = true;
                        Debug.Log("[SIM] Boss spawn wave " + waveIndex);
                    }
                }

                // 2) move + attack base when in range
                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    var e = enemies[i];
                    // move down
                    e.Y -= e.Speed * dt;

                    // in attack range?
                    var info = enemyDict[e.Class];
                    if (e.Y <= info.AttackRange)
                    {
                        e.timeSinceLastAttack += dt;
                        float atkInterval = 1f / info.AttackSpeed;
                        if (e.timeSinceLastAttack >= atkInterval)
                        {
                            // damage to base
                            stats.TotalDamageTaken += e.Damage;
                            baseHealth = Mathf.Max(0f, baseHealth - e.Damage);
                            e.timeSinceLastAttack = 0f;

                            // mission fail?
                            if (baseHealth <= 0f)
                            {
                                stats.MissionsFailed++;
                                baseHealth = baseMaxHealth;
                                delayTimer = 0f;
                                tickTimer = 0f;
                                Debug.Log("[SIM] Mission failed wave " + waveIndex);
                            }
                        }
                    }

                    enemies[i] = e;
                }

                // 3) base regen
                if (baseHealth < baseMaxHealth)
                {
                    delayTimer += dt;
                    if (delayTimer >= regenDelay)
                    {
                        tickTimer += dt;
                        if (tickTimer >= regenInterval)
                        {
                            baseHealth = Mathf.Min(
                                baseMaxHealth, baseHealth + regenAmount);
                            stats.TotalHealthRepaired += regenAmount;
                            tickTimer = 0f;
                        }
                    }
                }

                // 4) click build/decay
                spdBonus += bonusPerClick * clicksPerSec * dt;
                spdBonus = Mathf.Max(0f, spdBonus - decayPerSec * dt);
                stats.SpeedBoostClicks += (int)Math.Floor(clicksPerSec * dt);

                // 5) turret DPS per shot with crit/pellet randomness
                for (int ti = 0; ti < slots.Count; ti++)
                {
                    var bp = slots[ti];

                    // accumulate time
                    shotTimers[ti] += dt;
                    float interval = 1f / bp.FireRate;  // seconds per shot

                    // fire as many shots as the timer allows
                    while (shotTimers[ti] >= interval)
                    {
                        shotTimers[ti] -= interval;

                        // pick the first enemy
                        if (enemies.Count == 0) break;
                        int eidx = 0;
                        EnemySim e = enemies[eidx];

                        // crit roll
                        bool isCrit = Random.Range(0, 100) < bp.CritChance * 100;
                        float dmg = bp.Damage * (isCrit ? 1f + bp.CritDamageMultiplier : 1f);

                        stats.TotalDamageDealt += dmg;
                        dmgByType[bp.Type] += dmg;

                        e.Hp -= dmg;
                        if (e.Hp <= 0f)
                        {
                            if (e.IsBoss) stats.BossesKilled++;
                            else stats.EnemiesKilled++;

                            coins += e.Coin;
                            enemies.RemoveAt(eidx);
                            Debug.Log($"[SIM] {bp.Type} killed {(e.IsBoss ? "boss" : "enemy")} on wave {waveIndex}");
                            break;  // move to next turret
                        }
                        else
                        {
                            enemies[eidx] = e;  // write back the reduced HP
                        }
                    }
                }


                // 6) wave clear?
                if ((!hasBoss && enemies.Count == 0)
                 || (hasBoss && bossSpawned && enemies.Count == 0))
                {
                    stats.WavesBeaten++;
                    var ts = slots.Select(bp =>
                        $"{bp.Type}|D{bp.Damage:F1}|R{bp.FireRate:F2}|C{bp.CritChance:F1}"
                    );
                                        stats.TurretSnapshots.Add(string.Join(";", ts));

                    // 6e) capture base stats
                    stats.BaseSnapshots.Add(
                        $"Health{baseHealth:F1}/{baseMaxHealth:F1}:Regen{regenAmount:F2}@" +
                        $"{regenInterval:F2}"
                    );

                    stats.MaxZone = Math.Max(stats.MaxZone, waveIndex);
                    stats.TotalZonesSecured++;
                    coins += waveClearBonus;
                    Debug.Log("[SIM] Wave " + waveIndex + " clear. Coins=" + coins);
                    waveIndex++;
                    InitWave();
                }

                // 7) unlock new turrets
                foreach (var row in unlockRows)
                {
                    if (row.WaveToUnlock == waveIndex
                        && !slots.Any(x => x.Type == row.Type)
                        && coins >= row.FirstCopyCost)
                    {
                            coins -= row.FirstCopyCost;
                            slots.Add(turretDict[row.Type]);
                            shotTimers.Add(0f);   // keep timers in sync
                            Debug.Log("[SIM] Unlocked " + row.Type
                                        +" wave " + waveIndex);       
                    }
                }

                // 8) spending (including base upgrades)
                ulong minCost = ulong.MaxValue;
                bool isBase = false;
                PlayerUpgradeType baseChoice = PlayerUpgradeType.MaxHealth;

                //  8a) gather cheapest turret upgrade
                foreach (var bp in slots)
                {
                    var costs = new ulong[]
                    {
                        (ulong)bp.DamageUpgradeBaseCost,
                        (ulong)bp.FireRateUpgradeBaseCost,
                        (ulong)bp.CritChanceUpgradeBaseCost,
                        (ulong)bp.CritDamageUpgradeBaseCost,
                        (ulong)bp.ExplosionRadiusUpgradeBaseCost,
                        (ulong)bp.SplashDamageUpgradeBaseCost,
                        (ulong)bp.PierceChanceUpgradeBaseCost,
                        (ulong)bp.PierceDamageFalloffUpgradeBaseCost,
                        (ulong)bp.PelletCountUpgradeBaseCost,
                        (ulong)bp.KnockbackStrengthUpgradeBaseCost,
                        (ulong)bp.DamageFalloffOverDistanceUpgradeBaseCost,
                        (ulong)bp.PercentBonusDamagePerSecUpgradeBaseCost,
                        (ulong)bp.SlowEffectUpgradeBaseCost
                    };

                    foreach (var c in costs)
                        if (c < minCost)
                        {
                            minCost = c;
                            isBase = false;
                        }
                }

                //  8b) gather cheapest base upgrade
                foreach (PlayerUpgradeType t in Enum.GetValues(typeof(PlayerUpgradeType)))
                {
                    float costF = BaseCost(t);
                    ulong cost = (ulong)Mathf.Ceil(costF);
                    if (cost < minCost)
                    {
                        minCost = cost;
                        isBase = true;
                        baseChoice = t;
                    }
                }

                //  8c) if we can afford something, buy the cheapest
                if (coins >= minCost)
                {
                    ulong before = coins;
                    if (isBase)
                    {
                        // simulate the base upgrade
                        coins -= minCost;
                        switch (baseChoice)
                        {
                            case PlayerUpgradeType.MaxHealth:
                                maxHealthLvl++;
                                // bump your simulated baseHealth  maxHealth here
                                baseMaxHealth += baseSO.MaxHealthUpgradeAmount;
                                baseHealth += baseSO.MaxHealthUpgradeAmount;
                                break;
                            case PlayerUpgradeType.RegenAmount:
                                regenAmountLvl++;
                                // bump regenAmount:
                                regenAmount += baseSO.RegenAmountUpgradeAmount;
                                break;
                            case PlayerUpgradeType.RegenInterval:
                                regenIntervalLvl++;
                                // reduce regenInterval
                                regenInterval = Mathf.Max(
                                0.5f,
                                regenInterval - baseSO.RegenIntervalUpgradeAmount
                                );
                                break;
                        }
                        stats.UpgradeHistory.Add(
                          $"Wave{waveIndex}:Base:{baseChoice}:{minCost}"
                        );


                    }
                    else
                    {
                        // still call your turret strategy for non base purchases
                        strategy.Tick(ref coins, ref slots, waveIndex);
                    }

                    ulong spent = before - coins;
                    if (spent > 0)
                    {
                        stats.MoneySpent += spent;
                        stats.UpgradeAmount += 1;
                    }
                }


                simTime += dt;
            }

            // assign per type DPS
            foreach (var kv in dmgByType)
            {
                switch (kv.Key)
                {
                    case TurretType.MachineGun:
                        stats.MachineGunDamage = kv.Value; break;
                    case TurretType.Shotgun:
                        stats.ShotgunDamage = kv.Value; break;
                    case TurretType.Sniper:
                        stats.SniperDamage = kv.Value; break;
                    case TurretType.MissileLauncher:
                        stats.MissileLauncherDamage = kv.Value; break;
                    case TurretType.Laser:
                        stats.LaserDamage = kv.Value; break;
                }
            }

            stats.SimMinutes = minutes;
            Debug.Log("[SIM END] W=" + stats.WavesBeaten
                     + " K=" + stats.EnemiesKilled
                     + " B=" + stats.BossesKilled
                     + " DT=" + stats.TotalDamageTaken
                     + " R=" + stats.TotalHealthRepaired
                     + " F=" + stats.MissionsFailed);
            return stats;
        }
    }
}
