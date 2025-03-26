using Assets.Scripts.SO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.WaveSystem
{
    public class WaveManager : MonoBehaviour
    {
        [SerializeField] private List<WaveConfigSO> _waves;
        [SerializeField] private EnemySpawner _enemySpawner;

        [SerializeField] private float _timeBetweenWaves;

        private int _currentWaveIndex = 0;
        private bool _waveInProgress = false;

        private void Start()
        {
            StartCoroutine(StartWaveRoutine());
        }

        private IEnumerator StartWaveRoutine()
        {
            while (_currentWaveIndex < _waves.Count)
            {
                _waveInProgress = true;
                _enemySpawner.StartWave(_waves[_currentWaveIndex]);

                yield return new WaitUntil(() => _enemySpawner.WaveCompleted);

                _waveInProgress = false;
                _currentWaveIndex++;

                yield return new WaitForSeconds(_timeBetweenWaves);
            }
        }
    }
}