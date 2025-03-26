using Assets.Scripts.Enemies;
using Assets.Scripts.SO;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts.WaveSystem
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private float _ySpawnPosition;

        private WaveConfigSO _currentWave;
        private List<GameObject> _enemiesAlive = new();

        public void StartWave(WaveConfigSO wave)
        {
            _currentWave = wave;
            StartCoroutine(SpawnEnemies());
        }

        public bool WaveCompleted => _enemiesAlive.Count == 0;

        private IEnumerator SpawnEnemies()
        {
            foreach (EnemyWaveEntry entry in _currentWave.EnemyPrefabs)
            {
                for (int i = 0; i < entry.numberOfEnemies; i++)
                {
                    GameObject tempEnemy = Instantiate(entry.enemyPrefab, GetRandomSpawnPosition(), Quaternion.identity);

                    tempEnemy.GetComponent<Enemy>().OnDeath += OnEnemyDeath;
                    _enemiesAlive.Add(tempEnemy);

                    yield return new WaitForSeconds(_currentWave.TimeBetweenSpawns);
                }
            }
        }

        private void OnEnemyDeath(object sender, EventArgs e)
        {
            if (sender is Enemy enemy)
            {
                enemy.OnDeath -= OnEnemyDeath;
                _enemiesAlive.Remove(enemy.gameObject);
            }
        }

        private Vector3 GetRandomSpawnPosition()
        {
            float randomXPosition = Random.Range(-9f, 9f);
            return new Vector3(randomXPosition, _ySpawnPosition);
        }
    }
}