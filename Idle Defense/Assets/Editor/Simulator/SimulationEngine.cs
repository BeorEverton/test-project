// Assets/Editor/Simulation/SimulationEngine.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Assets.Scripts.SO;            // your SO namespace
using Assets.Scripts.Turrets;       // for TurretInfoSO & TurretType
using Assets.Scripts.WaveSystem;    // for WaveConfigSO
using Assets.Scripts.Enemies;       // for Enemy & EnemyInfoSO

namespace IdleDefense.Editor.Simulation
{
    public static class SimulationEngine
    {
        public static SimStats Run(
            float minutes,
            float clicksPerSec,
            SpendingMode mode)
        {
            //  Load & prepare blueprints 
            // Waves, ordered by their start index
            var baseWaves = AssetDatabase
              .FindAssets("t:WaveConfigSO")
              .Select(AssetDatabase.GUIDToAssetPath)
              .Select(path => AssetDatabase.LoadAssetAtPath<WaveConfigSO>(path))
              .OrderBy(so => so.WaveStartIndex)
              .ToList();

            // Turret SOs as blueprints
            var turretDict = AssetDatabase
              .FindAssets("t:TurretInfoSO")
              .Select(AssetDatabase.GUIDToAssetPath)
              .Select(path => AssetDatabase.LoadAssetAtPath<TurretInfoSO>(path))
              .ToDictionary(so => so.TurretType, so => new TurretBlueprint(so));

            // PlayerBaseSO (if needed)
            var playerBaseSO = AssetDatabase
              .FindAssets("t:PlayerBaseSO")
              .Select(AssetDatabase.GUIDToAssetPath)
              .Select(path => AssetDatabase.LoadAssetAtPath<PlayerBaseSO>(path))
              .First();

            //  Initialize simulation state 
            float dt = 0.05f;
            float simTime = 0f;
            float clickAcc = 0f;
            int waveIndex = 1;  // start at wave 1
            ulong coins = 0;

            // Live turret slots: start with the first type available
            var slotList = new List<TurretBlueprint> { turretDict.Values.First() };

            // Spending strategy
            var strategy = SpendingStrategyFactory.Create(mode);

            // Stats recorder
            var stats = new SimStats();

            // State per wave
            int enemiesToSpawn = 0;
            float spawnTimer = 0f;
            float enemyHp = 0f;
            ulong coinPerKill = 0;
            ulong waveClearBonus = 0;
            var liveEnemiesHp = new List<float>();

            // Helper to (re)initialize a wave
            Action initWave = () =>
            {
                // pick the right template for this waveIndex
                var tpl = baseWaves.Last(w => w.WaveStartIndex <= waveIndex);

                // we ignore multiple EnemyWaveEntries and merge them:
                // count = sum(entry.NumberOfEnemies + waveIndex)
                enemiesToSpawn = tpl.EnemyWaveEntries.Sum(e => e.NumberOfEnemies + waveIndex);
                spawnTimer = tpl.TimeBetweenSpawns;

                // all enemies share the same scaled stats (mirror your WaveManager.Clone logic)
                // here we pick the first entry’s prefab to fetch its SO
                var so = tpl.EnemyWaveEntries[0].EnemyPrefab.GetComponent<Enemy>().Info;
                enemyHp = so.MaxHealth * Mathf.Pow(so.HealthMultiplierByWaveCount, waveIndex)
                               + waveIndex;
                coinPerKill = (ulong)(so.CoinDropAmount * so.CoinDropMultiplierByWaveCount
                               + waveIndex * so.CoinDropMultiplierByWaveCount);

                waveClearBonus = (ulong)(5 + waveIndex * 2);  // your clear bonus formula

                liveEnemiesHp.Clear();
                stats.TimesDefeated = stats.TimesDefeated; // no change
            };

            // Kick off wave 1
            initWave();

            //  Simulation loop 
            while (simTime < minutes * 60f)
            {
                //Spawn new enemies
                if (enemiesToSpawn > 0)
                {
                    spawnTimer -= dt;
                    if (spawnTimer <= 0f)
                    {
                        liveEnemiesHp.Add(enemyHp);
                        enemiesToSpawn--;
                        spawnTimer = baseWaves
                            .Last(w => w.WaveStartIndex <= waveIndex)
                            .TimeBetweenSpawns;
                    }
                }

                //Turret DPS & clicks
                clickAcc += clicksPerSec * dt;
                float clickBonus = clickAcc;
                clickAcc = 0f;

                float totalDps = slotList.Sum(t => t.DamagePerSecond(clickBonus));
                float dmgAvail = totalDps * dt;

                //apply damage to the first enemy in line
                if (liveEnemiesHp.Count > 0)
                {
                    liveEnemiesHp[0] -= dmgAvail;
                    if (liveEnemiesHp[0] <= 0f)
                    {
                        // kill
                        stats.EnemiesKilled++;
                        coins += coinPerKill;
                        liveEnemiesHp.RemoveAt(0);
                    }
                }

                //Check wave clear
                if (enemiesToSpawn == 0 && liveEnemiesHp.Count == 0)
                {
                    stats.WavesBeaten++;
                    coins += waveClearBonus;
                    waveIndex++;
                    initWave();
                }

                //Spending
                strategy.Tick(ref coins, ref slotList, waveIndex);

                //Advance time
                simTime += dt;
            }

            stats.SimMinutes = minutes;
            return stats;
        }
    }
}
