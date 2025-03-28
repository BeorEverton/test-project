using Assets.Scripts.Enemies;
using Assets.Scripts.SO;
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
                tempObj.SetActive(false);
                tempObj.transform.position = GetRandomSpawnPosition();
                tempObj.transform.rotation = Quaternion.identity;

                tempObj.GetComponent<Enemy>().OnDeath += OnEnemyDeath;
                _enemiesCurrentWave.Add(tempObj);
            }
        }

        private IEnumerator SpawnEnemies()
        {
            foreach (GameObject enemy in _enemiesCurrentWave)
            {
                enemy.SetActive(true);
                EnemiesAlive.Add(enemy);

                yield return new WaitForSeconds(_currentWave.TimeBetweenSpawns);
            }

            _waveSpawned = true;
        }

        private Vector3 GetRandomSpawnPosition()
        {
            float randomXPosition = Random.Range(-9f, 9f);
            return new Vector3(randomXPosition, _ySpawnPosition);
        }

        private void OnEnemyDeath(object sender, EventArgs e)
        {
            if (sender is Enemy enemy)
            {
                enemy.OnDeath -= OnEnemyDeath;
                EnemiesAlive.Remove(enemy.gameObject);
                _objectPool.ReturnObject(enemy.Info.Name, enemy.gameObject);
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