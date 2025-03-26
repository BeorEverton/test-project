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
        public bool GameInRunning = true;

        [SerializeField] private List<WaveConfigSO> _baseWaves;
        [SerializeField] private EnemySpawner _enemySpawner;

        [SerializeField] private float _timeBetweenWaves;

        private WaveConfigSO _currentBaseWave;
        private int _currentBaseWaveIndex = 0;
        private int _currentWaveIndex = 0;
        private int _waveIndexOfCurrentBaseWave = 0;

        private bool _waveCompleted = false;

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
            while (GameInRunning)
            {
                _currentWaveIndex++;
                _waveIndexOfCurrentBaseWave++;

                UpdateCurrentBasicWave();

                WaveConfigSO dynamicWave = GenerateDynamicWaveConfig(_currentBaseWave, _currentWaveIndex);
                _enemySpawner.StartWave(dynamicWave);

                yield return new WaitUntil(() => _waveCompleted);

                _waveCompleted = false;
                yield return new WaitForSeconds(_timeBetweenWaves);
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

            newWaveConfig.EnemyPrefabs = new List<EnemyWaveEntry>();
            foreach (EnemyWaveEntry entry in baseConfig.EnemyPrefabs)
            {
                EnemyWaveEntry newEntry = new()
                {
                    enemyPrefab = entry.enemyPrefab,
                    numberOfEnemies = entry.numberOfEnemies
                };

                newEntry.numberOfEnemies += _waveIndexOfCurrentBaseWave + _currentBaseWaveIndex;

                newEntry.enemyPrefab.GetComponent<Enemy>().Stats.AddMaxHealth = (waveIndex * 2f);

                newWaveConfig.EnemyPrefabs.Add(newEntry);
            }

            newWaveConfig.TimeBetweenSpawns = baseConfig.TimeBetweenSpawns;

            return newWaveConfig;
        }
    }
}