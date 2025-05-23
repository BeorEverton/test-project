// Assets/Editor/Simulation/SpendingStrategies.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Turrets;  // for TurretType

namespace IdleDefense.Editor.Simulation
{
    public enum SpendingMode { Cheapest, Random, MostEffective }

    public interface ISpendingStrategy
    {
        void Tick(ref ulong coins,
                  ref List<TurretBlueprint> slots,
                  int currentWave);
    }

    public static class SpendingStrategyFactory
    {
        public static ISpendingStrategy Create(SpendingMode mode)
        {
            switch (mode)
            {
                case SpendingMode.Cheapest: return new CheapestStrategy();
                case SpendingMode.Random: return new RandomStrategy();
                case SpendingMode.MostEffective: return new MostEffectiveStrategy();
                default: throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }
    }

    // ---------------------------------------------------------
    // Cheapest: finds the single cheapest next upgrade across all slots and stats
    // ---------------------------------------------------------
    class CheapestStrategy : ISpendingStrategy
    {
        public void Tick(ref ulong coins, ref List<TurretBlueprint> slots, int currentWave)
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
    class RandomStrategy : ISpendingStrategy
    {
        private readonly System.Random rng = new System.Random();

        public void Tick(ref ulong coins, ref List<TurretBlueprint> slots, int currentWave)
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
    class MostEffectiveStrategy : ISpendingStrategy
    {
        public void Tick(ref ulong coins, ref List<TurretBlueprint> slots, int currentWave)
        {
            float bestScore = 0f;
            int bestSlot = -1;
            int bestUpgrade = -1;
            ulong bestCost = 0;

            for (int i = 0; i < slots.Count; i++)
            {
                var t = slots[i];
                float baseDps = t.DamagePerSecond();

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
                    var cost = costs[u];
                    if (cost > coins) continue;

                    // simulate applying the u-th upgrade
                    TurretBlueprint up = t;
                    switch (u)
                    {
                        case 0: up = t.WithDamageUpgraded(); break;
                        case 1: up = t.WithFireRateUpgraded(); break;
                        case 2: up = t.WithCritChanceUpgraded(); break;
                        case 3: up = t.WithCritDamageUpgraded(); break;
                        case 4: up = t.WithExplosionRadiusUpgraded(); break;
                        case 5: up = t.WithSplashDamageUpgraded(); break;
                        case 6: up = t.WithPierceChanceUpgraded(); break;
                        case 7: up = t.WithPierceDamageFalloffUpgraded(); break;
                        case 8: up = t.WithPelletCountUpgraded(); break;
                        case 9: up = t.WithKnockbackStrengthUpgraded(); break;
                        case 10: up = t.WithDamageFalloffOverDistanceUpgraded(); break;
                        case 11: up = t.WithPercentBonusDamagePerSecUpgraded(); break;
                        case 12: up = t.WithSlowEffectUpgraded(); break;
                    }

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
