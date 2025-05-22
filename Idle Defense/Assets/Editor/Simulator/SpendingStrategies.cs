// Assets/Editor/Simulation/SpendingStrategies.cs
using System;
using System.Collections.Generic;
using Assets.Scripts.Turrets;  // for TurretType
using UnityEngine;

namespace IdleDefense.Editor.Simulation
{
    public enum SpendingMode { Cheapest, Random, MostEffective }

    public interface ISpendingStrategy
    {
        /// <summary>
        /// Called every tick. You get a ref to coins (so you can spend),
        /// a ref to the list of turret slots (so you can replace entries),
        /// and the current wave index if you need it.
        /// </summary>
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
    // Cheapest: finds the single cheapest upgrade across all slots
    // ---------------------------------------------------------
    class CheapestStrategy : ISpendingStrategy
    {
        public void Tick(ref ulong coins, ref List<TurretBlueprint> slots, int currentWave)
        {
            // copy coin balance to a local so loops can use it
            ulong available = coins;
            int bestSlot = -1;
            bool bestIsDamage = false;
            ulong bestCost = ulong.MaxValue;

            for (int i = 0; i < slots.Count; i++)
            {
                TurretBlueprint t = slots[i];

                // damage upgrade cost
                ulong costD = t.CostPerDamageUp;
                if (costD <= available && costD < bestCost)
                {
                    bestCost = costD;
                    bestSlot = i;
                    bestIsDamage = true;
                }

                // fire rate upgrade cost
                ulong costF = t.CostPerFireRateUp;
                if (costF <= available && costF < bestCost)
                {
                    bestCost = costF;
                    bestSlot = i;
                    bestIsDamage = false;
                }
            }

            if (bestSlot >= 0)
            {
                coins -= bestCost;
                TurretBlueprint bp = slots[bestSlot];
                slots[bestSlot] = bestIsDamage
                    ? bp.WithDamageUpgraded()
                    : bp.WithFireRateUpgraded();
            }
        }
    }

    // ---------------------------------------------------------
    // Random: picks one affordable upgrade at random each tick
    // ---------------------------------------------------------
    class RandomStrategy : ISpendingStrategy
    {
        private readonly System.Random rng = new System.Random();

        public void Tick(ref ulong coins, ref List<TurretBlueprint> slots, int currentWave)
        {
            ulong available = coins;
            var candidates = new List<(int idx, bool isDamage, ulong cost)>(slots.Count * 2);

            for (int i = 0; i < slots.Count; i++)
            {
                TurretBlueprint t = slots[i];
                ulong costD = t.CostPerDamageUp;
                if (costD <= available)
                    candidates.Add((i, true, costD));

                ulong costF = t.CostPerFireRateUp;
                if (costF <= available)
                    candidates.Add((i, false, costF));
            }

            if (candidates.Count == 0)
                return;

            var pick = candidates[rng.Next(candidates.Count)];
            coins -= pick.cost;

            TurretBlueprint bp = slots[pick.idx];
            slots[pick.idx] = pick.isDamage
                ? bp.WithDamageUpgraded()
                : bp.WithFireRateUpgraded();
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
            bool bestIsDamage = false;
            ulong bestCost = 0;

            // evaluate every possible upgrade
            for (int i = 0; i < slots.Count; i++)
            {
                TurretBlueprint t = slots[i];
                float baseDps = t.DamagePerSecond();

                // damage upgrade
                {
                    ulong cost = t.CostPerDamageUp;
                    if (cost <= coins)
                    {
                        var up = t.WithDamageUpgraded();
                        float delta = up.DamagePerSecond() - baseDps;
                        float score = delta / cost;
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestSlot = i;
                            bestIsDamage = true;
                            bestCost = cost;
                        }
                    }
                }

                // fire rate upgrade
                {
                    ulong cost = t.CostPerFireRateUp;
                    if (cost <= coins)
                    {
                        var up = t.WithFireRateUpgraded();
                        float delta = up.DamagePerSecond() - baseDps;
                        float score = delta / cost;
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestSlot = i;
                            bestIsDamage = false;
                            bestCost = cost;
                        }
                    }
                }
            }

            if (bestSlot >= 0)
            {
                coins -= bestCost;
                TurretBlueprint bp = slots[bestSlot];
                slots[bestSlot] = bestIsDamage
                    ? bp.WithDamageUpgraded()
                    : bp.WithFireRateUpgraded();
            }
        }
    }
}
