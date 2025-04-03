using Assets.Scripts.Enemies;
using Assets.Scripts.SO;
using Assets.Scripts.UI;
using System;
using System.Collections;
using System.Collections.Generic;
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

        [SerializeField] private List<WaveConfigSO> _baseWaves;
        [SerializeField] private EnemySpawner _enemySpawner;

        [Header("Enemy Wave Bonus")]
        [Tooltip("Maxhealth += waveCount * healthMultiplier (Default = 2)")]
        [SerializeField] private float _healthMultiplierByWaveCount = 2f;
        [Tooltip("CoinDrop = Ceil(CoinDrop * coinDropmultiplier) (Default = 1.05, 5% increase per wave)")]
        [SerializeField] private float _coinDropMultiplierByWaveCount = 1.05f;

        private WaveConfigSO _currentBaseWave;
        private int _currentBaseWaveIndex = 0;
        private int _currentWaveIndex = 0;
        private int _waveIndexOfCurrentBaseWave = 0;

        private bool _waveCompleted = false;

        private bool forcedWave = false; // Used by the UI to force the next wave

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            if (_baseWaves != null && _baseWaves.Count > 0)
            {
                _currentBaseWave = _baseWaves[0];
            }

            StartCoroutine(StartWaveRoutine());

            EnemySpawner.Instance.OnWaveCompleted += OnWaveCompleted;
        }

        private void OnWaveCompleted(object sender, EventArgs e)
        {
            _waveCompleted = true;
        }

        private IEnumerator StartWaveRoutine()
        {
            while (GameRunning)
            {
                if (forcedWave)
                    forcedWave = false;
                else
                {
                    _currentWaveIndex++;
                    _waveIndexOfCurrentBaseWave++;
                }

                OnWaveStarted?.Invoke(this, new OnWaveStartedEventArgs
                {
                    WaveNumber = _currentWaveIndex
                });

                UpdateCurrentBasicWave();

                WaveConfigSO dynamicWave = GenerateDynamicWaveConfig(_currentBaseWave, _currentWaveIndex);
                _enemySpawner.StartWave(dynamicWave);

                yield return new WaitUntil(() => _waveCompleted);

                _waveCompleted = false;
            }
        }

        private void UpdateCurrentBasicWave()
        {
            if (_currentBaseWaveIndex >= _baseWaves.Count - 1)
                return;

            WaveConfigSO nextBaseWave = _baseWaves[_currentBaseWaveIndex + 1];

            if (_currentWaveIndex < nextBaseWave.WaveStartIndex)
                return;

            _currentBaseWaveIndex++;
            _waveIndexOfCurrentBaseWave = 0;
            _currentBaseWave = _baseWaves[_currentBaseWaveIndex];
        }

        private WaveConfigSO GenerateDynamicWaveConfig(WaveConfigSO baseConfig, int waveIndex)
        {
            WaveConfigSO newWaveConfig = ScriptableObject.CreateInstance<WaveConfigSO>();
            newWaveConfig.EnemyWaveEntries = new List<EnemyWaveEntry>();

            foreach (EnemyWaveEntry entry in baseConfig.EnemyWaveEntries)
            {
                EnemyWaveEntry newEntry = new()
                {
                    EnemyPrefab = Instantiate(entry.EnemyPrefab),
                    NumberOfEnemies = entry.NumberOfEnemies + _waveIndexOfCurrentBaseWave + _currentBaseWaveIndex
                };

                if (newEntry.EnemyPrefab.TryGetComponent(out Enemy enemy))
                {
                    EnemyInfoSO clonedInfo = Instantiate(enemy.Info);
                    clonedInfo.MaxHealth += (waveIndex * _healthMultiplierByWaveCount);
                    clonedInfo.CoinDropAmount = 
                        (ulong)Mathf.CeilToInt(clonedInfo.CoinDropAmount * (ulong)_currentWaveIndex * _coinDropMultiplierByWaveCount);

                    enemy.SetNewStats(clonedInfo);
                }

                newWaveConfig.EnemyWaveEntries.Add(newEntry);
            }

            newWaveConfig.TimeBetweenSpawns = baseConfig.TimeBetweenSpawns;

            return newWaveConfig;
        }

        public int GetCurrentWaveIndex()
        {
            return _currentWaveIndex;
        }

        public void SetWave(int waveIndex)
        {
            _currentWaveIndex = waveIndex;
            forcedWave = true;
        }
    }
}