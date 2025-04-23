using Assets.Scripts.Enemies;
using Assets.Scripts.Helpers;
using Assets.Scripts.SO;
using Assets.Scripts.Systems;
using Assets.Scripts.Systems.Audio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts.WaveSystem
{
    public class EnemySpawner : MonoBehaviour
    {
        public static EnemySpawner Instance { get; private set; }

        public List<GameObject> EnemiesAlive { get; } = new();

        public event EventHandler OnWaveCompleted;
        public event EventHandler OnWaveStarted;
        public event EventHandler<OnWaveCreatedEventArgs> OnWaveCreated;
        public event EventHandler<OnEnemyDeathEventArgs> OnEnemyDeath;
        public class OnEnemyDeathEventArgs : EventArgs
        {
            public ulong CoinDropAmount;
        }

        public class OnWaveCreatedEventArgs : EventArgs
        {
            public int EnemyCount;
        }

        // Screen size control for adjustable x spawn position
        private float _screenLeft;
        private float _screenRight;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private const float _spawnMargin = 0.5f;

        private float _screenTop;

        private List<GameObject> _enemiesCurrentWave = new();
        private ObjectPool _objectPool;
        private WaveConfigSO _currentWaveConfig;

        private bool _waveSpawned;
        private bool _canSpawnEnemies;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            _objectPool = new ObjectPool(transform);
        }

        private void Start()
        {
            PlayerBaseManager.Instance.OnWaveFailed += PlayerBaseManager_OnWaveFailed;
        }

        public void StartWave(Wave wave)
        {
            _currentWaveConfig = wave.WaveConfig;
            _waveSpawned = false;
            OnWaveStarted?.Invoke(this, EventArgs.Empty);

            CreateWave();
            Shuffle.ShuffleList(_enemiesCurrentWave);
            CheckIfBossWave(wave);

            _canSpawnEnemies = true;
            StartCoroutine(SpawnEnemies());
        }

        private void CreateWave()
        {
            _enemiesCurrentWave.Clear();
            foreach (EnemyWaveEntry entry in _currentWaveConfig.EnemyWaveEntries)
            {
                CreateEnemiesFromEntry(entry);
            }
        }

        private void CheckIfBossWave(Wave wave)
        {
            if (wave.IsMiniBossWave() || wave.IsBossWave())
            {
                Enemy bossEnemy = _enemiesCurrentWave[Random.Range(0, _enemiesCurrentWave.Count)].GetComponent<Enemy>();
                EnemyInfoSO baseInfo = bossEnemy.Info;
                EnemyInfoSO clonedInfo = Instantiate(baseInfo);

                if (wave.IsMiniBossWave())
                {
                    clonedInfo.Damage *= 3f;
                    clonedInfo.MaxHealth *= 10f;
                    clonedInfo.CoinDropAmount *= 10;
                    clonedInfo.MovementSpeed *= 0.8f;
                    clonedInfo.AttackRange += .2f; // Because the gfx size changes
                }

                if (wave.IsBossWave())
                {
                    clonedInfo.Damage *= 5f;
                    clonedInfo.MaxHealth *= 20f;
                    clonedInfo.CoinDropAmount *= 20;
                    clonedInfo.MovementSpeed *= 0.6f;
                    clonedInfo.AttackRange += .6f;
                    AudioManager.Instance.PlayMusic("Boss");
                }

                AudioManager.Instance.Play("Boss Appear");
                bossEnemy.ApplyBossInfo(clonedInfo, wave.IsMiniBossWave());
            }
            else if (Camera.main != null)
                Camera.main.backgroundColor = new Color(.14f, .14f, .14f, 1f);
        }


        private EnemyInfoSO GetRandomEnemyFromWave()
        {
            int randomIndex = Random.Range(0, _currentWaveConfig.EnemyWaveEntries.Count);
            return _enemiesCurrentWave[randomIndex].GetComponent<Enemy>().Info;
        }

        private void CreateEnemiesFromEntry(EnemyWaveEntry entry)
        {
            for (int i = 0; i < entry.NumberOfEnemies; i++)
            {
                string entryName = entry.EnemyPrefab.GetComponent<Enemy>().Info.Name;

                GameObject tempEnemy = GetEnemyFromPool(entryName, entry.EnemyPrefab);
                tempEnemy.SetActive(false);
                _enemiesCurrentWave.Add(tempEnemy);
            }
            OnWaveCreated?.Invoke(this, new OnWaveCreatedEventArgs { EnemyCount = _enemiesCurrentWave.Count });
        }

        private GameObject GetEnemyFromPool(string enemyName, GameObject enemyPrefab)
        {
            GameObject enemy = _objectPool.GetObject(enemyName, enemyPrefab);
            enemy.transform.position = GetRandomSpawnPosition();
            enemy.transform.rotation = Quaternion.identity;
            return enemy;
        }

        private IEnumerator SpawnEnemies()
        {
            foreach (GameObject enemy in _enemiesCurrentWave)
            {
                if (!_canSpawnEnemies)
                    break;

                enemy.SetActive(true);
                enemy.GetComponent<Enemy>().OnDeath += Enemy_OnEnemyDeath;
                EnemiesAlive.Add(enemy);

                yield return new WaitForSeconds(_currentWaveConfig.TimeBetweenSpawns);
            }

            _waveSpawned = true;
        }

        /// <summary>
        /// Updates the bounds of the screen to know where enemies should respawn
        /// </summary>
        private void UpdateScreenBoundsIfNeeded()
        {
            if (Screen.width == _lastScreenWidth && Screen.height == _lastScreenHeight)
                return;

            Camera cam = Camera.main;
            _screenLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, 0)).x + _spawnMargin;
            _screenRight = cam.ViewportToWorldPoint(new Vector3(1, 0, 0)).x - _spawnMargin;

            _screenTop = cam.ViewportToWorldPoint(new Vector3(0.5f, 1f, 0)).y + 1f; // 1 unit above visible top
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
        }

        private Vector3 GetRandomSpawnPosition()
        {
            UpdateScreenBoundsIfNeeded();
            float randomXPosition = Random.Range(_screenLeft, _screenRight);
            return new Vector3(randomXPosition, _screenTop);
        }

        private void Enemy_OnEnemyDeath(object sender, EventArgs e)
        {
            if (sender is Enemy enemy)
            {
                StartCoroutine(HandleEnemyDeath(enemy));

                OnEnemyDeath?.Invoke(this, new OnEnemyDeathEventArgs
                {
                    CoinDropAmount = enemy.Info.CoinDropAmount
                });
            }

            CheckIfWaveCompleted();
        }

        private IEnumerator HandleEnemyDeath(Enemy enemy)
        {
            enemy.OnDeath -= Enemy_OnEnemyDeath;
            EnemiesAlive.Remove(enemy.gameObject);

            yield return StartCoroutine(enemy.EnemyDeathEffect.PlayEffectRoutine());

            _objectPool.ReturnObject(enemy.Info.Name, enemy.gameObject);
        }

        private void CheckIfWaveCompleted()
        {
            if (_waveSpawned && EnemiesAlive.Count <= 0)
                OnWaveCompleted?.Invoke(this, EventArgs.Empty);
        }

        private void PlayerBaseManager_OnWaveFailed(object sender, EventArgs e)
        {
            StartCoroutine(RestartWave());
        }

        private IEnumerator RestartWave()
        {
            _canSpawnEnemies = false;
            StopCoroutine(SpawnEnemies());

            yield return new WaitForSeconds(.5f); //Wait to secure all managers are done with EnemiesAlive list

            foreach (GameObject enemy in EnemiesAlive.ToList())
            {
                if (enemy == null)
                    continue;

                EnemiesAlive.Remove(enemy.gameObject);
                enemy.GetComponent<Enemy>().OnDeath -= Enemy_OnEnemyDeath;
                _objectPool.ReturnObject(enemy.GetComponent<Enemy>().Info.Name, enemy);
            }

            OnWaveCompleted?.Invoke(this, EventArgs.Empty);
        }

    }
}