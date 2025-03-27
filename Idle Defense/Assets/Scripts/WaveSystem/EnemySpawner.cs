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

        [SerializeField] private float _ySpawnPosition;

        private WaveConfigSO _currentWave;
        private readonly List<GameObject> _enemiesCurrentWave = new();

        private ObjectPool _objectPool;

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
            CreateWave();
            Shuffle.ShuffleList(_enemiesCurrentWave);
            StartCoroutine(SpawnEnemies());
        }

        private void CreateWave()
        {
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

                yield return new WaitForSeconds(_currentWave.TimeBetweenSpawns);
            }
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
                _enemiesCurrentWave.Remove(enemy.gameObject);
                _objectPool.ReturnObject(enemy.Info.Name, enemy.gameObject);

                // Reduce enemy count in UI
                UIManager.Instance.EnemyDied(_enemiesCurrentWave.Count);
            }

            CheckIfWaveCompleted();
        }

        private void CheckIfWaveCompleted()
        {
            if (_enemiesCurrentWave.Count <= 0)
                OnWaveCompleted?.Invoke(this, EventArgs.Empty);
        }
    }
}