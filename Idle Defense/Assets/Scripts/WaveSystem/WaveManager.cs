using Assets.Scripts.Enemies;
using Assets.Scripts.SO;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.WaveSystem
{
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        public bool GameRunning = true;

        [SerializeField] private List<WaveConfigSO> _baseWaves;
        [SerializeField] private EnemySpawner _enemySpawner;

        private WaveConfigSO _currentBaseWave;
        private int _currentBaseWaveIndex = 0;
        private int _currentWaveIndex = 0;
        private int _waveIndexOfCurrentBaseWave = 0;

        private bool _waveCompleted = false;

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
                _currentWaveIndex++;
                _waveIndexOfCurrentBaseWave++;

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
                    EnemyPrefab = entry.EnemyPrefab,
                    NumberOfEnemies = entry.NumberOfEnemies
                };

                newEntry.NumberOfEnemies += _waveIndexOfCurrentBaseWave + _currentBaseWaveIndex;

                if (newEntry.EnemyPrefab.TryGetComponent(out Enemy enemy))
                    enemy.Info.AddMaxHealth = (waveIndex * 2f);

                newWaveConfig.EnemyWaveEntries.Add(newEntry);
            }

            newWaveConfig.TimeBetweenSpawns = baseConfig.TimeBetweenSpawns;

            return newWaveConfig;
        }
    }
}