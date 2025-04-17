using Assets.Scripts.Enemies;
using Assets.Scripts.SO;
using Assets.Scripts.Systems;
using Assets.Scripts.Systems.Save;
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

        [Header("Enemy Wave Bonus")]
        [Tooltip("Maxhealth += waveCount * healthMultiplier (Default = 2)")]
        [SerializeField] private float _healthMultiplierByWaveCount = 2f;
        [Tooltip("CoinDrop = Ceil(CoinDrop * coinDropmultiplier) (Default = 1.05, 5% increase per wave)")]
        [SerializeField] private float _coinDropMultiplierByWaveCount = 1.05f;

        private Dictionary<int, Wave> _waves = new(); //Dictionary of all waves, with wave number as key
        private int _maxWaves = 0; //Amount of waves in dictionary
        private int _currentWave = 1; //Overall wave index
        private bool _waveCompleted = false;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private async void Start()
        {
            await GenerateWavesAsync(startWaveNumber: 1, amountToGenerate: 100); //Generate first 100 waves, starting from wave 1

            StartCoroutine(StartWaveRoutine());

            EnemySpawner.Instance.OnWaveCompleted += OnWaveCompleted;
            PlayerBaseManager.Instance.OnWaveFailed += PlayerBaseManager_OnWaveFailed;
        }

        public int GetCurrentWaveIndex() => _currentWave;
        public Wave GetCurrentWave() => _waves[_currentWave];
        public void LoadWave(int waveNumber) => _currentWave = waveNumber; //Load previous wave, since StartWaveRoutine count +1 before initializing

        private void OnWaveCompleted(object sender, EventArgs e)
        {
            _waveCompleted = true;
        }

        private void PlayerBaseManager_OnWaveFailed(object sender, EventArgs e)
        {
            _currentWave -= 10;
            if (_currentWave < 1)
                _currentWave = 0;
        }

        private IEnumerator StartWaveRoutine()
        {
            while (GameRunning)
            {
                OnWaveStarted?.Invoke(this, new OnWaveStartedEventArgs
                {
                    WaveNumber = _currentWave
                });

                try
                {
                    if (_waves.TryGetValue(_currentWave, out Wave wave))
                    {
                        _enemySpawner.StartWave(wave.WaveConfig);

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

                if (_maxWaves < _currentWave + 10) //Generate new waves when only 10 left
                {
                    GenerateWaves(startWaveNumber: _maxWaves, amountToGenerate: 100);
                }

                yield return new WaitUntil(() => _waveCompleted);

                _currentWave++;

                SaveGameManager.Instance.SaveGame(); //Save game at the start of each round

                _waveCompleted = false;
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

        private EnemyInfoSO CloneEnemyInfoWithScale(Enemy enemy, int waveIndex)
        {
            EnemyInfoSO clonedInfo = Instantiate(enemy.Info);
            clonedInfo.MaxHealth += (waveIndex * _healthMultiplierByWaveCount);
            clonedInfo.CoinDropAmount =
                (ulong)Mathf.CeilToInt(clonedInfo.CoinDropAmount * (ulong)waveIndex * _coinDropMultiplierByWaveCount);
            return clonedInfo;
        }

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
                WaveConfigSO tempWaveConfig = ScriptableObject.CreateInstance<WaveConfigSO>();
                List<EnemyInfoSO> enemyWaveEntries = new();

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

                tempWaveConfig.TimeBetweenSpawns = baseWave.TimeBetweenSpawns;

                Wave wave = new(tempWaveConfig, waveNumber);
                enemyWaveEntries.ForEach(entry => wave.AddEnemyClassToWave(entry.EnemyClass, entry));

                _waves.Add(waveNumber, wave);
                _maxWaves++;
                waveNumber++;
            }

            return Task.CompletedTask;
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
    }
}