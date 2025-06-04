// Assets/Editor/Simulation/SpendingStrategies.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Turrets;
using Assets.Scripts.Systems;  // for TurretType

namespace IdleDefense.Editor.Simulation
{
    public enum SpendingMode { Cheapest, Random, MostEffective }

    public interface ISpendingStrategy
    {
        // Called by SimulationEngine after coins have been deducted.
        void Tick(ref ulong coins, ref List<TurretBlueprint> slots, int currentWave);

        // Set by the engine to tell the strategy *which* turret slot to upgrade.
        void SetNextTurretIndex(int index);

        // Set by the engine to tell the strategy *which* stat to upgrade.
        void SetNextUpgradeType(TurretUpgradeType upgrade);
    }

    public static class SpendingStrategyFactory
    {
        public static ISpendingStrategy Create(SpendingMode mode)
        {
            return mode switch
            {
                SpendingMode.Cheapest => new CheapestStrategy(),
                SpendingMode.Random => new RandomStrategy(),
                SpendingMode.MostEffective => new MostEffectiveStrategy(),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }
    }

    // ---------------------------------------------------------
    // Base class to share the apply upgrade code
    // ---------------------------------------------------------
    abstract class BaseStrategy : ISpendingStrategy
    {
        protected int nextTurretIndex = -1;
        protected TurretUpgradeType nextUpgradeType;

        public void SetNextTurretIndex(int index)
            => nextTurretIndex = index;

        public void SetNextUpgradeType(TurretUpgradeType upgrade)
            => nextUpgradeType = upgrade;

        public virtual void Tick(ref ulong coins, ref List<TurretBlueprint> slots, int currentWave)
        {
            if (nextTurretIndex < 0 || nextTurretIndex >= slots.Count)
                return;

            var bp = slots[nextTurretIndex];
            switch (nextUpgradeType)
            {
                case TurretUpgradeType.Damage:
                    slots[nextTurretIndex] = bp.WithDamageUpgraded(); break;
                case TurretUpgradeType.FireRate:
                    slots[nextTurretIndex] = bp.WithFireRateUpgraded(); break;
                case TurretUpgradeType.CriticalChance:
                    slots[nextTurretIndex] = bp.WithCritChanceUpgraded(); break;
                case TurretUpgradeType.CriticalDamageMultiplier:
                    slots[nextTurretIndex] = bp.WithCritDamageUpgraded(); break;
                case TurretUpgradeType.ExplosionRadius:
                    slots[nextTurretIndex] = bp.WithExplosionRadiusUpgraded(); break;
                case TurretUpgradeType.SplashDamage:
                    slots[nextTurretIndex] = bp.WithSplashDamageUpgraded(); break;
                case TurretUpgradeType.PierceChance:
                    slots[nextTurretIndex] = bp.WithPierceChanceUpgraded(); break;
                case TurretUpgradeType.PierceDamageFalloff:
                    slots[nextTurretIndex] = bp.WithPierceDamageFalloffUpgraded(); break;
                case TurretUpgradeType.PelletCount:
                    slots[nextTurretIndex] = bp.WithPelletCountUpgraded(); break;
                case TurretUpgradeType.DamageFalloffOverDistance:
                    slots[nextTurretIndex] = bp.WithDamageFalloffOverDistanceUpgraded(); break;
                case TurretUpgradeType.PercentBonusDamagePerSec:
                    slots[nextTurretIndex] = bp.WithPercentBonusDamagePerSecUpgraded(); break;
                case TurretUpgradeType.SlowEffect:
                    slots[nextTurretIndex] = bp.WithSlowEffectUpgraded(); break;
                case TurretUpgradeType.KnockbackStrength:
                    slots[nextTurretIndex] = bp.WithKnockbackStrengthUpgraded(); break;
            }

            // reset for next decision
            nextTurretIndex = -1;
        }
    }

    // ---------------------------------------------------------
    // Cheapest: finds the single cheapest next upgrade across all slots and stats
    // ---------------------------------------------------------
    class CheapestStrategy : BaseStrategy
    {
        public override void Tick(ref ulong coins, ref List<TurretBlueprint> slots, int currentWave)
        {
            ulong bestCost = ulong.MaxValue;
            int bestSlot = -1;
            int bestUpgrade = -1; // index into our list of upgrade types

            // for each slot, check every upgrade cost
            for (int i = 0; i < slots.Count; i++)
            {
                var t = slots[i];
                var costs = new ulong[]
                {
                    (ulong)t.DamageUpgradeBaseCost,
                    (ulong)t.FireRateUpgradeBaseCost,
                    (ulong)t.CritChanceUpgradeBaseCost,
                    (ulong)t.CritDamageUpgradeBaseCost,
                    (ulong)t.ExplosionRadiusUpgradeBaseCost,
                    (ulong)t.SplashDamageUpgradeBaseCost,
                    (ulong)t.PierceChanceUpgradeBaseCost,
                    (ulong)t.PierceDamageFalloffUpgradeBaseCost,
                    (ulong)t.PelletCountUpgradeBaseCost,
                    (ulong)t.KnockbackStrengthUpgradeBaseCost,
                    (ulong)t.DamageFalloffOverDistanceUpgradeBaseCost,
                    (ulong)t.PercentBonusDamagePerSecUpgradeBaseCost,
                    (ulong)t.SlowEffectUpgradeBaseCost
                };

                for (int u = 0; u < costs.Length; u++)
                {
                    var c = costs[u];
                    if (c <= coins && c < bestCost)
                    {
                        bestCost = c;
                        bestSlot = i;
                        bestUpgrade = u;
                    }
                }
            }

            if (bestSlot >= 0)
            {
                coins -= bestCost;
                var bp = slots[bestSlot];
                // apply the chosen upgrade
                switch (bestUpgrade)
                {
                    case 0: slots[bestSlot] = bp.WithDamageUpgraded(); break;
                    case 1: slots[bestSlot] = bp.WithFireRateUpgraded(); break;
                    case 2: slots[bestSlot] = bp.WithCritChanceUpgraded(); break;
                    case 3: slots[bestSlot] = bp.WithCritDamageUpgraded(); break;
                    case 4: slots[bestSlot] = bp.WithExplosionRadiusUpgraded(); break;
                    case 5: slots[bestSlot] = bp.WithSplashDamageUpgraded(); break;
                    case 6: slots[bestSlot] = bp.WithPierceChanceUpgraded(); break;
                    case 7: slots[bestSlot] = bp.WithPierceDamageFalloffUpgraded(); break;
                    case 8: slots[bestSlot] = bp.WithPelletCountUpgraded(); break;
                    case 9: slots[bestSlot] = bp.WithKnockbackStrengthUpgraded(); break;
                    case 10: slots[bestSlot] = bp.WithDamageFalloffOverDistanceUpgraded(); break;
                    case 11: slots[bestSlot] = bp.WithPercentBonusDamagePerSecUpgraded(); break;
                    case 12: slots[bestSlot] = bp.WithSlowEffectUpgraded(); break;
                }
            }
        }
    }

    // ---------------------------------------------------------
    // Random: picks a random upgrade among all affordable options
    // ---------------------------------------------------------
    class RandomStrategy : BaseStrategy
    {
        private readonly System.Random rng = new System.Random();

        public override void Tick(ref ulong coins, ref List<TurretBlueprint> slots, int currentWave)
        {
            var candidates = new List<(int slot, int upgrade, ulong cost)>();

            for (int i = 0; i < slots.Count; i++)
            {
                var t = slots[i];
                var costs = new ulong[]
                {
                    (ulong)t.DamageUpgradeBaseCost,
                    (ulong)t.FireRateUpgradeBaseCost,
                    (ulong)t.CritChanceUpgradeBaseCost,
                    (ulong)t.CritDamageUpgradeBaseCost,
                    (ulong)t.ExplosionRadiusUpgradeBaseCost,
                    (ulong)t.SplashDamageUpgradeBaseCost,
                    (ulong)t.PierceChanceUpgradeBaseCost,
                    (ulong)t.PierceDamageFalloffUpgradeBaseCost,
                    (ulong)t.PelletCountUpgradeBaseCost,
                    (ulong)t.KnockbackStrengthUpgradeBaseCost,
                    (ulong)t.DamageFalloffOverDistanceUpgradeBaseCost,
                    (ulong)t.PercentBonusDamagePerSecUpgradeBaseCost,
                    (ulong)t.SlowEffectUpgradeBaseCost
                };

                for (int u = 0; u < costs.Length; u++)
                    if (costs[u] <= coins)
                        candidates.Add((i, u, costs[u]));
            }

            if (candidates.Count == 0) return;

            var pick = candidates[rng.Next(candidates.Count)];
            coins -= pick.cost;
            var bp = slots[pick.slot];
            switch (pick.upgrade)
            {
                case 0: slots[pick.slot] = bp.WithDamageUpgraded(); break;
                case 1: slots[pick.slot] = bp.WithFireRateUpgraded(); break;
                case 2: slots[pick.slot] = bp.WithCritChanceUpgraded(); break;
                case 3: slots[pick.slot] = bp.WithCritDamageUpgraded(); break;
                case 4: slots[pick.slot] = bp.WithExplosionRadiusUpgraded(); break;
                case 5: slots[pick.slot] = bp.WithSplashDamageUpgraded(); break;
                case 6: slots[pick.slot] = bp.WithPierceChanceUpgraded(); break;
                case 7: slots[pick.slot] = bp.WithPierceDamageFalloffUpgraded(); break;
                case 8: slots[pick.slot] = bp.WithPelletCountUpgraded(); break;
                case 9: slots[pick.slot] = bp.WithKnockbackStrengthUpgraded(); break;
                case 10: slots[pick.slot] = bp.WithDamageFalloffOverDistanceUpgraded(); break;
                case 11: slots[pick.slot] = bp.WithPercentBonusDamagePerSecUpgraded(); break;
                case 12: slots[pick.slot] = bp.WithSlowEffectUpgraded(); break;
            }
        }
    }

    // ---------------------------------------------------------
    // MostEffective: picks the upgrade with highest DPS gain per coin
    // ---------------------------------------------------------
    class MostEffectiveStrategy : BaseStrategy
    {
        // helper to mirror SimulationEngine’s exponential+level cost
        static float CostPlusLevel(float baseCost, int level, float multiplier)
        {
            return baseCost + Mathf.Pow(multiplier, level) + level;
        }

        public override void Tick(ref ulong coins, ref List<TurretBlueprint> slots, int currentWave)
        {
            float bestScore = 0f;
            int bestSlot = -1;
            int bestUpgrade = -1;
            ulong bestCost = 0;

            float expectedEnemyHp = 100f * Mathf.Pow(1.05f, currentWave) + currentWave;

            for (int i = 0; i < slots.Count; i++)
            {
                var t = slots[i];
                float baseDps = t.DamagePerSecond();
                float baseShotDamage = t.Damage * (1f + Mathf.Clamp01(t.CritChance / 100f) * ((t.CritDamageMultiplier / 100f) - 1f));

                for (int u = 0; u < 13; u++)
                {
                    if (u == 0 && baseShotDamage >= expectedEnemyHp)
                        continue; // skip damage upgrades if already 1-shotting

                    float rawCost;
                    TurretBlueprint up = t;

                    switch (u)
                    {
                        case 0:
                            rawCost = CostPlusLevel(t.DamageUpgradeBaseCost, t.DamageLevel, t.DamageCostExponentialMultiplier);
                            up = t.WithDamageUpgraded(); break;
                        case 1:
                            rawCost = CostPlusLevel(t.FireRateUpgradeBaseCost, t.FireRateLevel, t.FireRateCostExponentialMultiplier);
                            up = t.WithFireRateUpgraded(); break;
                        case 2:
                            rawCost = CostPlusLevel(t.CritChanceUpgradeBaseCost, t.CriticalChanceLevel, t.CriticalChanceCostExponentialMultiplier);
                            up = t.WithCritChanceUpgraded(); break;
                        case 3:
                            rawCost = CostPlusLevel(t.CritDamageUpgradeBaseCost, t.CriticalDamageMultiplierLevel, t.CriticalDamageCostExponentialMultiplier);
                            up = t.WithCritDamageUpgraded(); break;
                        case 4:
                            rawCost = CostPlusLevel(t.ExplosionRadiusUpgradeBaseCost, t.ExplosionRadiusLevel, t.ExplosionRadiusCostExponentialMultiplier);
                            up = t.WithExplosionRadiusUpgraded(); break;
                        case 5:
                            rawCost = CostPlusLevel(t.SplashDamageUpgradeBaseCost, t.SplashDamageLevel, t.SplashDamageCostExponentialMultiplier);
                            up = t.WithSplashDamageUpgraded(); break;
                        case 6:
                            rawCost = CostPlusLevel(t.PierceChanceUpgradeBaseCost, t.PierceChanceLevel, t.PierceChanceCostExponentialMultiplier);
                            up = t.WithPierceChanceUpgraded(); break;
                        case 7:
                            rawCost = CostPlusLevel(t.PierceDamageFalloffUpgradeBaseCost, t.PierceDamageFalloffLevel, t.PierceDamageFalloffCostExponentialMultiplier);
                            up = t.WithPierceDamageFalloffUpgraded(); break;
                        case 8:
                            rawCost = CostPlusLevel(t.PelletCountUpgradeBaseCost, t.PelletCountLevel, t.PelletCountCostExponentialMultiplier);
                            up = t.WithPelletCountUpgraded(); break;
                        case 9:
                            rawCost = CostPlusLevel(t.KnockbackStrengthUpgradeBaseCost, t.KnockbackStrengthLevel, t.KnockbackStrengthCostExponentialMultiplier);
                            up = t.WithKnockbackStrengthUpgraded(); break;
                        case 10:
                            rawCost = CostPlusLevel(t.DamageFalloffOverDistanceUpgradeBaseCost, t.DamageFalloffOverDistanceLevel, t.DamageFalloffOverDistanceCostExponentialMultiplier);
                            up = t.WithDamageFalloffOverDistanceUpgraded(); break;
                        case 11:
                            rawCost = CostPlusLevel(t.PercentBonusDamagePerSecUpgradeBaseCost, t.PercentBonusDamagePerSecLevel, t.PercentBonusDamagePerSecCostExponentialMultiplier);
                            up = t.WithPercentBonusDamagePerSecUpgraded(); break;
                        case 12:
                            rawCost = CostPlusLevel(t.SlowEffectUpgradeBaseCost, t.SlowEffectLevel, t.SlowEffectCostExponentialMultiplier);
                            up = t.WithSlowEffectUpgraded(); break;
                        default:
                            continue;
                    }

                    ulong cost = (ulong)Mathf.Ceil(rawCost);
                    if (cost > coins) continue;

                    float delta = up.DamagePerSecond() - baseDps;
                    float score = delta / cost;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestSlot = i;
                        bestUpgrade = u;
                        bestCost = cost;
                    }
                }
            }

            if (bestSlot >= 0)
            {
                coins -= bestCost;
                var bp = slots[bestSlot];
                switch (bestUpgrade)
                {
                    case 0: slots[bestSlot] = bp.WithDamageUpgraded(); break;
                    case 1: slots[bestSlot] = bp.WithFireRateUpgraded(); break;
                    case 2: slots[bestSlot] = bp.WithCritChanceUpgraded(); break;
                    case 3: slots[bestSlot] = bp.WithCritDamageUpgraded(); break;
                    case 4: slots[bestSlot] = bp.WithExplosionRadiusUpgraded(); break;
                    case 5: slots[bestSlot] = bp.WithSplashDamageUpgraded(); break;
                    case 6: slots[bestSlot] = bp.WithPierceChanceUpgraded(); break;
                    case 7: slots[bestSlot] = bp.WithPierceDamageFalloffUpgraded(); break;
                    case 8: slots[bestSlot] = bp.WithPelletCountUpgraded(); break;
                    case 9: slots[bestSlot] = bp.WithKnockbackStrengthUpgraded(); break;
                    case 10: slots[bestSlot] = bp.WithDamageFalloffOverDistanceUpgraded(); break;
                    case 11: slots[bestSlot] = bp.WithPercentBonusDamagePerSecUpgraded(); break;
                    case 12: slots[bestSlot] = bp.WithSlowEffectUpgraded(); break;
                }
            }
        }


    }
}
