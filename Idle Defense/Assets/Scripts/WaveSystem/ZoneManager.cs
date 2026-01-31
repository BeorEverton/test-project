using System;
using System.Collections.Generic;
using Assets.Scripts.Enemies;
using Assets.Scripts.SO;
using Assets.Scripts.Systems;
using UnityEngine;

namespace Assets.Scripts.WaveSystem
{
    public enum ZoneLoopMode
    {
        RestartFromFirst,
        PingPong
    }

    /// <summary>
    /// Handles zone progression and builds scaled Wave instances for the current
    /// zone/wave/global step.
    /// </summary>
    public class ZoneManager : MonoBehaviour
    {
        [Header("Zone configuration")]
        [SerializeField] private List<ZoneDefinitionSO> _zones = new List<ZoneDefinitionSO>();

        [Tooltip("How to loop once the player reaches the last zone.")]
        [SerializeField] private ZoneLoopMode _loopMode = ZoneLoopMode.RestartFromFirst;

        [Tooltip("Start zone index (0-based) when a new run begins).")]
        [SerializeField] private int _initialZoneIndex = 0;

        [Tooltip("Start wave index inside the zone (0-based).")]
        [SerializeField] private int _initialWaveIndexInZone = 0;

        [Header("Enemy quantity scaling")]
        [Tooltip("Extra enemies per global wave, added on top of the base NumberOfEnemies for each entry.")]
        [SerializeField] private float _extraEnemiesPerWave = 10f;

        [Tooltip("Hard cap on TOTAL enemies per wave across all entries. 0 = no cap.")]
        [SerializeField] private int _maxEnemiesPerWave = 10000;

        // -------------------------
        // Plateau knobs (editor + runtime tuning)
        // -------------------------
        [SerializeField] private int _bossInterval = 10;

        // how strong the "new tier" jump is (applied once you enter the tier)
        [SerializeField] private float _plateauSpikeBase = 1.25f;

        // per-wave growth inside the tier (0.01 = +1% per wave)
        [SerializeField] private float _plateauStepPerWave = 0.01f;

        // exponent for the tier base wave power (10^1.3 etc.)
        [SerializeField] private float _plateauWavePowerExponent = 1.3f;


        /// <summary>Called when the active zone changes (for visuals, music, etc.).</summary>
        public event Action<ZoneDefinitionSO> OnZoneChanged;

        /// <summary>0-based index into the _zones list.</summary>
        public int CurrentZoneIndex => _currentZoneIndex;

        /// <summary>0-based index inside the current zone's Waves list.</summary>
        public int CurrentWaveIndexInZone => _currentWaveIndexInZone;

        /// <summary>
        /// Global wave index used for scaling and stats. Starts at 1 and increases
        /// every time the player beats a wave, regardless of zone.
        /// </summary>
        public int GlobalWaveIndex => _globalWaveIndex;

        public ZoneDefinitionSO CurrentZone =>
            (_zones != null && _zones.Count > 0 && _currentZoneIndex >= 0 && _currentZoneIndex < _zones.Count)
                ? _zones[_currentZoneIndex]
                : null;

        public ZoneWaveDefinition CurrentZoneWaveDef
        {
            get
            {
                var zone = CurrentZone;
                if (zone == null || zone.Waves == null || zone.Waves.Count == 0) return null;
                if (_currentWaveIndexInZone < 0 || _currentWaveIndexInZone >= zone.Waves.Count) return null;
                return zone.Waves[_currentWaveIndexInZone];
            }
        }

        private int _currentZoneIndex;
        private int _currentWaveIndexInZone;
        private int _globalWaveIndex = 1;

        // For ping-pong mode: true = moving forward through zones, false = backward.
        private bool _forward = true;

        private void Awake()
        {
            ResetProgress();
        }

        /// <summary>
        /// Reset zone/wave/global state back to the initial configuration.
        /// </summary>
        public void ResetProgress()
        {
            _currentZoneIndex = Mathf.Clamp(_initialZoneIndex, 0, Mathf.Max(0, _zones.Count - 1));
            _currentWaveIndexInZone = Mathf.Max(0, _initialWaveIndexInZone);
            _globalWaveIndex = 1;
            _forward = true;

            var zone = CurrentZone;
            if (zone != null)
            {
                OnZoneChanged?.Invoke(zone);
            }
        }

        /// <summary>
        /// Build a fully scaled Wave instance for the current zone/wave/global step.
        /// This is what WaveManager passes to EnemySpawner.StartWave().
        /// </summary>
        public Wave BuildWaveForCurrentStep()
        {
            ZoneDefinitionSO zone = CurrentZone;
            ZoneWaveDefinition def = CurrentZoneWaveDef;

            if (zone == null)
            {
                Debug.LogError("ZoneManager.BuildWaveForCurrentStep: no current zone configured.");
                return null;
            }

            if (def == null || def.WaveConfig == null)
            {
                Debug.LogError($"ZoneManager.BuildWaveForCurrentStep: zone '{zone.DisplayName}' has a null wave definition or WaveConfig.");
                return null;
            }

            WaveConfigSO baseWave = def.WaveConfig;

            // 1) Create a temporary WaveConfig with scaled counts for this global step.
            List<EnemyInfoSO> scaledInfos;
            WaveConfigSO tempWaveConfig;
            CreateTempWaveConfig(baseWave, _globalWaveIndex, out scaledInfos, out tempWaveConfig);

            // 2) Build the runtime Wave wrapper.
            Wave wave = new Wave(tempWaveConfig, _globalWaveIndex);

            int waveNumberInZone = def.WaveNumberInZone > 0
                ? def.WaveNumberInZone
                : _currentWaveIndexInZone + 1;

            // Decide which boss / miniboss prefab to use for this wave.
            GameObject miniBossPrefab = null;
            GameObject bossPrefab = null;

            if (def.IsBoss)
            {
                // If this wave wants to be a full boss, prefer its override, then the zone default.
                bossPrefab = def.OverrideBossPrefab != null ? def.OverrideBossPrefab : zone.BossPrefab;
            }

            if (def.IsMiniBoss)
            {
                // If this wave wants to be a mini-boss, prefer its override, then the zone default.
                miniBossPrefab = def.OverrideBossPrefab != null ? def.OverrideBossPrefab : zone.MiniBossPrefab;
            }

            wave.SetZoneData(
                zone,
                waveNumberInZone,
                def.IsMiniBoss,
                def.IsBoss,
                miniBossPrefab,
                bossPrefab);

            // 3) Fill the WaveEnemies dictionary so Enemy.ResetEnemy can restore stats.
            foreach (EnemyInfoSO info in scaledInfos)
            {
                wave.AddEnemyClassToWave(info.EnemyId, info);
            }


            return wave;
        }


        /// <summary>
        /// Called by WaveManager when a wave is successfully completed.
        /// This advances zone and wave indexes and increments the global step.
        /// </summary>
        public void OnWaveCompleted(bool playerLost)
        {
            if (playerLost)
            {
                // We do not advance zones on loss. WaveManager already handles restarts.
                return;
            }

            // Player beat the wave -> next global step.
            _globalWaveIndex = Mathf.Max(1, _globalWaveIndex + 1);
            Debug.Log($"ZoneManager: advancing to global wave {_globalWaveIndex}.");

            // Step to next wave/zone according to the loop mode.
            StepToNextWave(raiseZoneChanged: true);
        }

        /// <summary>
        /// Force the internal state to a specific global wave index.
        /// Used by debug tools / rollbacks.
        /// </summary>
        public void SetGlobalWaveIndex(int globalWaveIndex)
        {            
            globalWaveIndex = Mathf.Max(1, globalWaveIndex);

            // Re-simulate the path from the beginning to that step.
            ResetProgress();
            
            // We start at global 1; we need to advance (globalWaveIndex - 1) times.
            for (int i = 1; i < globalWaveIndex; i++)
            {
                StepToNextWave(raiseZoneChanged: false);
            }

            _globalWaveIndex = globalWaveIndex;            

            var zone = CurrentZone;
            if (zone != null)
            {
                OnZoneChanged?.Invoke(zone);
            }
        }

        /// <summary>
        /// Single step through the zone/wave structure (used both for normal completion and recomputation).
        /// </summary>
        private void StepToNextWave(bool raiseZoneChanged)
        {
            var zone = CurrentZone;
            if (zone == null || zone.Waves == null || zone.Waves.Count == 0)
            {
                return;
            }

            _currentWaveIndexInZone++;

            // Finished all waves inside this zone -> move to the next zone.
            if (_currentWaveIndexInZone >= zone.Waves.Count)
            {
                _currentWaveIndexInZone = 0;

                if (_loopMode == ZoneLoopMode.RestartFromFirst)
                {
                    _currentZoneIndex++;

                    if (_currentZoneIndex >= _zones.Count)
                    {
                        // Wrap back to first zone but keep difficulty scaling.
                        _currentZoneIndex = 0;
                    }
                }
                else // PingPong
                {
                    if (_forward)
                    {
                        if (_currentZoneIndex >= _zones.Count - 1)
                        {
                            _forward = false;
                            _currentZoneIndex = Mathf.Max(0, _currentZoneIndex - 1);
                        }
                        else
                        {
                            _currentZoneIndex++;
                        }
                    }
                    else
                    {
                        if (_currentZoneIndex <= 0)
                        {
                            _forward = true;
                            _currentZoneIndex = Mathf.Min(_zones.Count - 1, _currentZoneIndex + 1);
                        }
                        else
                        {
                            _currentZoneIndex--;
                        }
                    }
                }

                if (raiseZoneChanged)
                {
                    var newZone = CurrentZone;
                    if (newZone != null)
                    {
                        OnZoneChanged?.Invoke(newZone);
                    }
                }
            }
        }

        #region Enemy scaling helpers (copied from previous WaveManager logic)

        private void CreateTempWaveConfig(
            WaveConfigSO baseWave,
            int globalIndex,
            out List<EnemyInfoSO> enemyWaveEntries,
            out WaveConfigSO tempWaveConfig)
        {
            enemyWaveEntries = new List<EnemyInfoSO>();
            tempWaveConfig = ScriptableObject.CreateInstance<WaveConfigSO>();

            // Copy authored pacing data (otherwise spawner falls back to linear/default behaviour).
            tempWaveConfig.WaveStartIndex = baseWave.WaveStartIndex;
            tempWaveConfig.TimeBetweenSpawns = baseWave.TimeBetweenSpawns;
            tempWaveConfig.TotalSpawnDuration = baseWave.TotalSpawnDuration;

            // Important: clone the curve so runtime edits can't mutate the asset.
            tempWaveConfig.SpawnCurve = (baseWave.SpawnCurve != null && baseWave.SpawnCurve.length > 0)
                ? new AnimationCurve(baseWave.SpawnCurve.keys)
                : null;

            tempWaveConfig.EnemyWaveEntries = new List<EnemyWaveEntry>();

            int totalCount = 0;
            int cap = _maxEnemiesPerWave > 0 ? _maxEnemiesPerWave : int.MaxValue;


            foreach (EnemyWaveEntry entry in baseWave.EnemyWaveEntries)
            {
                EnemyWaveEntry newEntry = CreateNewEntry(entry, globalIndex);

                if (newEntry.EnemyPrefab == null || newEntry.NumberOfEnemies <= 0)
                    continue;

                int remaining = cap - totalCount;
                if (remaining <= 0)
                    break; // hit global cap for this wave

                if (newEntry.NumberOfEnemies > remaining)
                    newEntry.NumberOfEnemies = remaining;

                if (newEntry.NumberOfEnemies <= 0)
                    continue;

                totalCount += newEntry.NumberOfEnemies;

                if (newEntry.EnemyPrefab.TryGetComponent(out Enemy enemy))
                {
                    EnemyInfoSO clonedInfo = CloneEnemyInfoWithScale(enemy, globalIndex);
                    enemyWaveEntries.Add(clonedInfo);
                }

                tempWaveConfig.EnemyWaveEntries.Add(newEntry);
            }
        }


        /// <summary>
        /// Scale number of enemies based on the global index and prestige settings.
        /// </summary>
        private EnemyWaveEntry CreateNewEntry(EnemyWaveEntry baseEntry, int globalIndex)
        {
            // Base authored value
            int count = Mathf.Max(0, baseEntry.NumberOfEnemies);

            // Extra enemies per global wave (globalIndex is 1-based).
            if (_extraEnemiesPerWave > 0f && globalIndex > 1)
            {
                int extra = Mathf.RoundToInt((globalIndex - 1) * _extraEnemiesPerWave);
                count += Mathf.Max(0, extra);
            }

            // Prestige multiplier
            var pm = PrestigeManager.Instance;
            if (pm != null)
            {
                float countMul = pm.GetEnemyCountMultiplier();
                count = Mathf.Max(1, Mathf.RoundToInt(count * countMul));
            }

            return new EnemyWaveEntry
            {
                EnemyPrefab = baseEntry.EnemyPrefab,
                NumberOfEnemies = Mathf.Max(0, count)
            };
        }


        /// <summary>
        /// Clone an EnemyInfoSO and apply plateaued health/damage scaling + prestige multipliers.
        /// </summary>
        private EnemyInfoSO CloneEnemyInfoWithScale(Enemy enemy, int globalIndex)
        {
            EnemyInfoSO clonedInfo = UnityEngine.Object.Instantiate(enemy.Info);

            // -------------------------
            // 1) Base scaling using EnemyInfoSO wave multipliers
            // -------------------------
            int wave = Mathf.Max(1, globalIndex);

            // Health (additive)
            float baseHealth = clonedInfo.MaxHealth;
            float healthAddPerWave = Mathf.Max(0f, clonedInfo.HealthMultiplierByWaveCount);
            clonedInfo.MaxHealth = baseHealth + (wave * healthAddPerWave);

            // Damage (additive)
            float baseDamage = clonedInfo.Damage;
            float dmgAddPerWave = Mathf.Max(0f, clonedInfo.DamageMultiplierByWaveCount);
            clonedInfo.Damage = baseDamage + (wave * dmgAddPerWave);

            // Coins (compounding)
            ulong baseCoin = clonedInfo.CoinDropAmount;
            float coinMul = Mathf.Max(0f, clonedInfo.CoinDropMultiplierByWaveCount);
            if (coinMul <= 0f) coinMul = 1f;

            double coinScaledDouble = baseCoin * System.Math.Pow(coinMul, wave - 1);
            if (coinScaledDouble < 0) coinScaledDouble = 0;
            if (coinScaledDouble > ulong.MaxValue) coinScaledDouble = ulong.MaxValue;
            clonedInfo.CoinDropAmount = (ulong)System.Math.Ceiling(coinScaledDouble);


            // -------------------------
            // 2) Plateau tier scaling (starts AFTER boss wave)
            //    tier = (wave-1)/bossInterval => wave 11 enters tier 1
            // -------------------------
            int bossInterval = Mathf.Max(1, _bossInterval);
            int tier = (wave - 1) / bossInterval;

            if (tier > 0)
            {
                int tierBaseWave = tier * bossInterval;           // 10, 20, 30...
                int intra = wave - tierBaseWave;                  // 1..bossInterval (wave 11 => intra=1)

                float spikeMultiplier = Mathf.Pow(_plateauSpikeBase, tier);
                float plateauMultiplier = 1f + ((intra - 1) * _plateauStepPerWave);
                float wavePower = Mathf.Pow(tierBaseWave, _plateauWavePowerExponent);

                float plateauFactor = wavePower * spikeMultiplier * plateauMultiplier;

                clonedInfo.MaxHealth *= plateauFactor;
                clonedInfo.Damage *= plateauFactor;

                double c2 = clonedInfo.CoinDropAmount * (double)(wavePower * spikeMultiplier * plateauMultiplier);
                if (c2 > ulong.MaxValue) c2 = ulong.MaxValue;
                    clonedInfo.CoinDropAmount = (ulong)System.Math.Ceiling(c2);
            }

            // Prestige enemy health multiplier (existing behavior)
            var pm = PrestigeManager.Instance;
            if (pm != null)
            {
                clonedInfo.MaxHealth *= pm.GetEnemyHealthMultiplier();
            }

            // Global formula multipliers (JSON). Applied on top of per-enemy scaling + plateau.            
            var fm = BalanceFormulaManager.Instance;
            if (fm != null)
            {
                float hMul = fm.GetEnemyHealthMultiplier(globalIndex);
                float dMul = fm.GetEnemyDamageMultiplier(globalIndex);
                float cMul = fm.GetEnemyCoinMultiplier(globalIndex);

                clonedInfo.MaxHealth *= hMul;
                clonedInfo.Damage *= dMul;

                double coins = clonedInfo.CoinDropAmount * (double)cMul;
                if (coins > ulong.MaxValue) coins = ulong.MaxValue;
                if (coins < 0) coins = 0;
                clonedInfo.CoinDropAmount = (ulong)System.Math.Ceiling(coins);
            }


            return clonedInfo;
        }


        #endregion
    }
}
