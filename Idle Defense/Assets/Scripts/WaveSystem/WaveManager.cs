using Assets.Scripts.Enemies;
using Assets.Scripts.SO;
using Assets.Scripts.Systems;
using Assets.Scripts.Systems.Audio;
using Assets.Scripts.Systems.Save;
using Assets.Scripts.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.WaveSystem
{
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        public event EventHandler<OnWaveStartedEventArgs> OnWaveStarted;
        public class OnWaveStartedEventArgs : EventArgs
        {
            public int WaveNumber;
        }

        public bool GameRunning = true;

        [SerializeField] private List<WaveConfigSO> _baseWaveSOs;
        [SerializeField] private EnemySpawner _enemySpawner;

        private Dictionary<int, Wave> _waves = new(); //Dictionary of all waves, with wave number as key
        private int _maxWaves = 0; //Amount of waves in dictionary
        private int _currentWave = 1; //Overall wave index
        private bool _waveCompleted = false;
        private bool _waveLost = false;
                
        public bool _autoAdvanceEnabled = true;     // when false, player must click to start the next wave

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private async void Start()
        {
            await GenerateWavesAsync(startWaveNumber: 1, amountToGenerate: 25); //Generate first 100 waves, starting from wave 1

            StartCoroutine(StartWaveRoutine());

            EnemySpawner.Instance.OnWaveCompleted += EnemySpawner_OnWaveCompleted;
            if (PlayerBaseManager.Instance != null)
                PlayerBaseManager.Instance.OnWaveFailed += PlayerBaseManager_OnWaveFailed;

            // When all equipped gunners die
            if (GunnerManager.Instance != null)
                GunnerManager.Instance.OnAllEquippedGunnersDead += HandleAllEquippedGunnersDead;

        }

        public int GetCurrentWaveIndex() => _currentWave;
        public Wave GetCurrentWave() => _waves[_currentWave];

        public void LoadWave(int waveNumber)
        {
            _currentWave = Mathf.Clamp(waveNumber, 1, int.MaxValue);
        }

        private void EnemySpawner_OnWaveCompleted(object sender, EventArgs e)
        {
            _waveCompleted = true;

            // Chatter: wave beaten (praise)
            if (GunnerManager.Instance != null)
            {
                var eq = GunnerManager.Instance.GetAllEquippedGunners();
                if (eq != null && eq.Count > 0)
                    GunnerChatterSystem.TryTrigger(GunnerEvent.WaveEnd, eq[UnityEngine.Random.Range(0, eq.Count)], null, 1f);
            }
        }

        private void PlayerBaseManager_OnWaveFailed(object sender, EventArgs e)
        {
            _waveLost = true;
            GameRunning = false;
            StopAllCoroutines();

            _autoAdvanceEnabled = false;

            // Chatter: wave lost (negative)
            if (GunnerManager.Instance != null)
            {
                var eq = GunnerManager.Instance.GetAllEquippedGunners();
                if (eq != null && eq.Count > 0)
                    GunnerChatterSystem.TryTrigger(GunnerEvent.NearlyDead, eq[UnityEngine.Random.Range(0, eq.Count)], null, 1f);
            }
        }

        private IEnumerator StartWaveRoutine()
        {
            while (GameRunning)
            {
                OnWaveStarted?.Invoke(this, new OnWaveStartedEventArgs
                {
                    WaveNumber = _currentWave
                });

                // Gunner chatter at start of wave
                if (GunnerManager.Instance != null)
                {
                    var eq = GunnerManager.Instance.GetAllEquippedGunners();
                    if (eq != null && eq.Count > 0)
                        GunnerChatterSystem.TryTrigger(GunnerEvent.WaveStart, eq[UnityEngine.Random.Range(0, eq.Count)]);
                }

                // Tell PrestigeManager a new wave began so it can update eligibility
                if (PrestigeManager.Instance != null)
                    PrestigeManager.Instance.NotifyWaveStarted(_currentWave);

                while (_maxWaves < _currentWave + 10) //Generate new waves when only 10 left
                {
                    GenerateWaves(startWaveNumber: _maxWaves + 1, amountToGenerate: 25);
                }

                try
                {
                    if (_waves.TryGetValue(_currentWave, out Wave wave))
                    {

                        _enemySpawner.StartWave(wave);
                    }
                    else
                    {
                        throw new Exception($"Wave {_currentWave} not found in dictionary");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                }

                AudioManager.Instance.Play("New Wave");

                SaveGameManager.Instance.SaveGame(); //Save game at the start of each round

                yield return new WaitUntil(() => _waveCompleted || _waveLost);

                if (_waveCompleted)
                {
                    if (_autoAdvanceEnabled)
                    {
                        _currentWave++;
                        StatsManager.Instance.TotalZonesSecured++;
                        StatsManager.Instance.MaxZone = _currentWave;
                    }
                }

                _waveCompleted = false;
                _waveLost = false;
            }
        }

        private EnemyWaveEntry CreateNewEntry(EnemyWaveEntry baseEntry, int waveNumber)
        {
            int baseCount = baseEntry.NumberOfEnemies + waveNumber;

            var pm = PrestigeManager.Instance;
            if (pm != null)
            {
                float countMul = pm.GetEnemyCountMultiplier();
                baseCount = Mathf.Max(1, Mathf.RoundToInt(baseCount * countMul));
            }

            return new EnemyWaveEntry
            {
                EnemyPrefab = baseEntry.EnemyPrefab,
                NumberOfEnemies = baseCount
            };

        }

        private EnemyInfoSO CloneEnemyInfoWithScale(Enemy enemy, int waveIndex)
        {
            EnemyInfoSO clonedInfo = Instantiate(enemy.Info);

            // --- Health Plateaued Scaling ---
            float baseHealth = clonedInfo.MaxHealth;
            int bossInterval = 10;
            int bossIndex = waveIndex / bossInterval;
            int plateauBaseWave = bossIndex * bossInterval;

            float spikeMultiplier = Mathf.Pow(1.25f, bossIndex);
            float plateauMultiplier = 1f + ((waveIndex - plateauBaseWave) * 0.01f);
            float wavePower = Mathf.Pow(plateauBaseWave > 0 ? plateauBaseWave : 1f, 1.3f);

            clonedInfo.MaxHealth = baseHealth * wavePower * spikeMultiplier * plateauMultiplier;

            // Apply prestige enemy health multiplier (single place)
            var pm = PrestigeManager.Instance;
            if (pm != null)
            {
                clonedInfo.MaxHealth *= pm.GetEnemyHealthMultiplier();
            }

            // --- Damage Scaling (mild exponential) ---
            clonedInfo.Damage += clonedInfo.Damage * (bossIndex * 0.2f); // 20% more per 10 waves

            // --- Coin Drop Scaling ---
            float coinBase = clonedInfo.CoinDropAmount * clonedInfo.CoinDropMultiplierByWaveCount;
            float coinBonus = waveIndex * clonedInfo.CoinDropMultiplierByWaveCount;
            clonedInfo.CoinDropAmount = (ulong)(coinBase + coinBonus);

            return clonedInfo;
        }

        /*PREVIOUS METHOD OF SCALING ENEMIES
         * private EnemyInfoSO CloneEnemyInfoWithScale(Enemy enemy, int waveIndex)
        {
            EnemyInfoSO clonedInfo = Instantiate(enemy.Info);
            clonedInfo.MaxHealth *= clonedInfo.HealthMultiplierByWaveCount * waveIndex;
            clonedInfo.Damage += clonedInfo.Damage * (waveIndex / 100 * 2);
            clonedInfo.CoinDropAmount = (ulong)(clonedInfo.CoinDropAmount * clonedInfo.CoinDropMultiplierByWaveCount +
                                                waveIndex * clonedInfo.CoinDropMultiplierByWaveCount);
            return clonedInfo;
        }*/

        private async void GenerateWaves(int startWaveNumber = 1, int amountToGenerate = 100)
        {
            await GenerateWavesAsync(startWaveNumber, amountToGenerate);
        }

        private Task GenerateWavesAsync(int startWaveNumber = 1, int amountToGenerate = 100)
        {
            int waveNumber = startWaveNumber;
            for (int i = 0; i < amountToGenerate; i++)
            {
                WaveConfigSO baseWave = GetBasicWaveConfigSo(waveNumber);

                CreateTempWaveConfig(baseWave, waveNumber, out List<EnemyInfoSO> enemyWaveEntries, out WaveConfigSO tempWaveConfig);

                tempWaveConfig.TimeBetweenSpawns = baseWave.TimeBetweenSpawns;

                Wave wave = new(tempWaveConfig, waveNumber);
                enemyWaveEntries.ForEach(entry => wave.AddEnemyClassToWave(entry.EnemyClass, entry));

                _waves.Add(waveNumber, wave);
                _maxWaves++;
                waveNumber++;
            }

            return Task.CompletedTask;
        }

        private void CreateTempWaveConfig(WaveConfigSO baseWave, int waveNumber, out List<EnemyInfoSO> enemyWaveEntries, out WaveConfigSO tempWaveConfig)
        {
            enemyWaveEntries = new List<EnemyInfoSO>();
            tempWaveConfig = ScriptableObject.CreateInstance<WaveConfigSO>();
            tempWaveConfig.EnemyWaveEntries = new List<EnemyWaveEntry>();

            foreach (EnemyWaveEntry entry in baseWave.EnemyWaveEntries)
            {
                EnemyWaveEntry newEntry = CreateNewEntry(entry, waveNumber);

                if (newEntry.EnemyPrefab.TryGetComponent(out Enemy enemy))
                {
                    EnemyInfoSO clonedInfo = CloneEnemyInfoWithScale(enemy, waveNumber);

                    enemyWaveEntries.Add(clonedInfo);
                }

                tempWaveConfig.EnemyWaveEntries.Add(newEntry);
            }
        }

        private WaveConfigSO GetBasicWaveConfigSo(int waveNumber)
        {
            WaveConfigSO tempWaveConfig = _baseWaveSOs[0];

            foreach (WaveConfigSO wave in _baseWaveSOs)
            {
                if (wave.WaveStartIndex == tempWaveConfig.WaveStartIndex)
                    continue;

                if (wave.WaveStartIndex <= waveNumber)
                {
                    tempWaveConfig = wave;
                }
                else
                    break;
            }
            return tempWaveConfig;
        }

        public void ResetWave()
        {
            _currentWave = 1;
            _maxWaves = 0;
            _waves.Clear();
            GameRunning = true;
        }

        public void ForceRestartWave()
        {
            StopAllCoroutines();
            _waveCompleted = false;
            _waveLost = false;

            GameRunning = true;
            StartCoroutine(StartWaveRoutine());
        }

        #region Stop wave progression
        /// <summary>
        /// Abort the current wave, return all active enemies to the pool,
        /// and mark the wave as completed so the loop advances immediately.
        /// </summary>
        public void JumpToNextWaveNow()
        {
            if (_enemySpawner != null)
            {
                _enemySpawner.AbortWaveAndDespawnAll();
            }
            // Force an advance at wave end, but keep whatever mode we’re in
            _autoAdvanceEnabled = true;
            _waveCompleted = true;
        }

        public void AbortWaveAndDespawnAll()
        {
            // Stop our own loops first
            StopAllCoroutines();

            if (_enemySpawner != null)
                _enemySpawner.AbortWaveAndDespawnAll();

            // Ensure local flags are clean
            _waveCompleted = false;
            _waveLost = false;
            GameRunning = false;
        }

        /// <summary>Optional UI toggle for auto-advance (e.g., a settings checkbox).</summary>
        public void SetAutoAdvanceEnabled(bool enabled)
        {
            _autoAdvanceEnabled = enabled;
        }
        public bool IsAutoAdvanceEnabled() => _autoAdvanceEnabled;

        private void HandleAllEquippedGunnersDead()
        {
            var wave = GetCurrentWave();                 // Wave has boss/miniboss helpers
            if (wave == null) return;

            // Only for boss / mini-boss waves
            if (!(wave.IsBossWave() || wave.IsMiniBossWave()))
                return;                                  // ignore normal waves
                                                         // (EnemySpawner also uses these helpers)
                                                         // :contentReference[oaicite:6]{index=6}

            // We want manual control for the next increment
            _autoAdvanceEnabled = false;                 // finishing the rollback wave will NOT advance. :contentReference[oaicite:7]{index=7}

            // Roll back exactly one wave (clamped to 1)
            int prev = Mathf.Max(1, GetCurrentWaveIndex() - 1);  // :contentReference[oaicite:8]{index=8}

            // Clear current wave immediately (no rewards/FX)
            if (_enemySpawner != null)
                _enemySpawner.AbortWaveAndDespawnAll();          // :contentReference[oaicite:9]{index=9}

            LoadWave(prev);                                      // set wave index (clamped) :contentReference[oaicite:10]{index=10}
            ForceRestartWave();                                  // restart loop cleanly        :contentReference[oaicite:11]{index=11}

            // Reveal the manual-advance button
            if (UIManager.Instance != null)
                UIManager.Instance.ShowManualAdvanceButton(true); // method added below
        }


        #endregion

    }
}