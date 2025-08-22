using Assets.Scripts.Enemies;
using Assets.Scripts.SO;
using Assets.Scripts.Systems;
using Assets.Scripts.Systems.Audio;
using Assets.Scripts.Systems.Save;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

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

        private bool _autoAdvanceEnabled = true;     // when false, player must click to start the next wave

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
            PlayerBaseManager.Instance.OnWaveFailed += PlayerBaseManager_OnWaveFailed;
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
        }

        private void PlayerBaseManager_OnWaveFailed(object sender, EventArgs e)
        {
            _waveLost = true;
            GameRunning = false;
            StopAllCoroutines();

            _autoAdvanceEnabled = false;
        }

        private IEnumerator StartWaveRoutine()
        {
            while (GameRunning)
            {                
                OnWaveStarted?.Invoke(this, new OnWaveStartedEventArgs
                {
                    WaveNumber = _currentWave
                });

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
            return new EnemyWaveEntry
            {
                EnemyPrefab = baseEntry.EnemyPrefab,
                NumberOfEnemies = baseEntry.NumberOfEnemies + waveNumber
            };
        }

        // NEW METHOD WITH PLATEAUED SCALING
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
            StopAllCoroutines();              // stop spawning
            _enemySpawner.DespawnAllActiveEnemies();        // loop active list and return to pool
        }

        /// <summary>Called by the UI “Advance” button when manual gating is active.</summary>


        /// <summary>Optional UI toggle for auto-advance (e.g., a settings checkbox).</summary>
        public void SetAutoAdvanceEnabled(bool enabled)
        {
            _autoAdvanceEnabled = enabled;
        }
        public bool IsAutoAdvanceEnabled() => _autoAdvanceEnabled;

        #endregion
    }
}