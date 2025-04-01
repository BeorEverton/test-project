using Assets.Scripts.Enemies;
using Assets.Scripts.SO;
using Assets.Scripts.UI;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
using Random = UnityEngine.Random;

namespace Assets.Scripts.WaveSystem
{
    public class EnemySpawner : MonoBehaviour
    {
        public static EnemySpawner Instance { get; private set; }

        public event EventHandler OnWaveCompleted;
        public event EventHandler OnWaveFailed; // TO-DO, Used to reset the damage bonus
        public event EventHandler<OnWaveCreatedEventArgs> OnWaveCreated;
        public event EventHandler OnEnemyDeath;

        public class OnWaveCreatedEventArgs : EventArgs
        {
            public int EnemyCount;
        }

        public List<GameObject> EnemiesAlive { get; } = new();

        [SerializeField] private float _ySpawnPosition;

        private List<GameObject> _enemiesCurrentWave = new();

        private ObjectPool _objectPool;
        private WaveConfigSO _currentWave;

        private bool _waveSpawned;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            _objectPool = new ObjectPool(transform);
        }

        public void StartWave(WaveConfigSO wave)
        {
            _currentWave = wave;
            _waveSpawned = false;
            CreateWave();
            Shuffle.ShuffleList(_enemiesCurrentWave);
            StartCoroutine(SpawnEnemies());
        }

        private void CreateWave()
        {
            _enemiesCurrentWave.Clear();
            foreach (EnemyWaveEntry entry in _currentWave.EnemyWaveEntries)
            {
                CreateEnemiesFromEntry(entry);
            }
        }

        private void CreateEnemiesFromEntry(EnemyWaveEntry entry)
        {
            for (int i = 0; i < entry.NumberOfEnemies; i++)
            {
                string entryName = entry.EnemyPrefab.GetComponent<Enemy>().Info.Name;
                GameObject tempObj = _objectPool.GetObject(entryName, entry.EnemyPrefab);
                tempObj.transform.position = GetRandomSpawnPosition();
                tempObj.transform.rotation = Quaternion.identity;

                tempObj.SetActive(false);
                _enemiesCurrentWave.Add(tempObj);
            }
            OnWaveCreated?.Invoke(this, new OnWaveCreatedEventArgs { EnemyCount = _enemiesCurrentWave.Count });
        }

        private IEnumerator SpawnEnemies()
        {
            foreach (GameObject enemy in _enemiesCurrentWave)
            {
                enemy.SetActive(true);
                enemy.GetComponent<Enemy>().OnDeath += Enemy_OnEnemyDeath;
                EnemiesAlive.Add(enemy);

                yield return new WaitForSeconds(_currentWave.TimeBetweenSpawns);
            }

            _waveSpawned = true;
        }

        private Vector3 GetRandomSpawnPosition()
        {
            float randomXPosition = Random.Range(-9f, 1.8f);
            return new Vector3(randomXPosition, _ySpawnPosition);
        }

        private void Enemy_OnEnemyDeath(object sender, EventArgs e)
        {
            if (sender is Enemy enemy)
            {
                enemy.OnDeath -= Enemy_OnEnemyDeath;
                EnemiesAlive.Remove(enemy.gameObject);
                _objectPool.ReturnObject(enemy.Info.Name, enemy.gameObject);

                OnEnemyDeath?.Invoke(this, EventArgs.Empty);
            }

            CheckIfWaveCompleted();
        }

        private void CheckIfWaveCompleted()
        {
            if (_waveSpawned && EnemiesAlive.Count <= 0)
                OnWaveCompleted?.Invoke(this, EventArgs.Empty);
        }
    }
}