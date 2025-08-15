using Assets.Scripts.Enemies;
using Assets.Scripts.Helpers;
using Assets.Scripts.SO;
using Assets.Scripts.Systems;
using Assets.Scripts.Systems.Audio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;
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

        public Material backgroundMaterial; // used to turn red when a boss is spawned

        public class OnEnemyDeathEventArgs : EventArgs
        {
            public Currency CurrencyType;
            public ulong Amount;
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

        // Used when the player dies
        private bool _suppressWaveComplete = false;

        private Coroutine _spawnEnemiesCoroutine;

        // to ensure we check for wave completion regularly
        private Coroutine _waveCompletionCheckCoroutine;

        public static float spawnXSpread = 15f;

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

        public async void StartWave(Wave wave)
        {
            _currentWaveConfig = wave.WaveConfig;
            _waveSpawned = false;
            OnWaveStarted?.Invoke(this, EventArgs.Empty);

            await CreateWave();
            await Shuffle.ShuffleList(_enemiesCurrentWave);
            await CheckIfBossWave(wave);

            _canSpawnEnemies = true;
            _spawnEnemiesCoroutine = StartCoroutine(SpawnEnemies());

            _waveCompletionCheckCoroutine = StartCoroutine(PeriodicWaveCompletionCheck());

        }

        private async Task CreateWave()
        {
            _enemiesCurrentWave.Clear();
            foreach (EnemyWaveEntry entry in _currentWaveConfig.EnemyWaveEntries)
            {
                await CreateEnemiesFromEntry(entry);
            }
        }

        private Task CheckIfBossWave(Wave wave)
        {
            if (wave.IsMiniBossWave() || wave.IsBossWave())
            {
                Enemy bossEnemy = _enemiesCurrentWave[Random.Range(0, _enemiesCurrentWave.Count)].GetComponent<Enemy>();
                EnemyInfoSO baseInfo = bossEnemy.Info;
                EnemyInfoSO clonedInfo = Instantiate(baseInfo);

                if (wave.IsMiniBossWave())
                {
                    int currentWave = WaveManager.Instance.GetCurrentWaveIndex();
                    float healthMultiplier;
                    float damageMultiplier;
                    float coinMultiplier;
                    if (currentWave == 5) // First mini boss is weaker
                    {
                        healthMultiplier = 30f;
                        damageMultiplier = 15f;
                        coinMultiplier = 20f;
                    }
                    else
                    {
                        healthMultiplier = Mathf.Min(currentWave * 50f, 500f);
                        damageMultiplier = Mathf.Min(currentWave * 10f, 100f);
                        coinMultiplier = Mathf.Min(currentWave * 10f, 100f);
                    }


                    clonedInfo.MaxHealth *= healthMultiplier;
                    clonedInfo.Damage *= damageMultiplier;
                    clonedInfo.CoinDropAmount = (ulong)(clonedInfo.CoinDropAmount * coinMultiplier);

                    clonedInfo.MovementSpeed *= 0.9f;
                    clonedInfo.AttackRange += .2f; // Because the gfx size changes
                }

                if (wave.IsBossWave())
                {
                    int currentWave = WaveManager.Instance.GetCurrentWaveIndex();
                    float healthMultiplier;
                    float damageMultiplier;
                    float coinMultiplier;
                    if (currentWave == 10) // First boss is weaker
                    {
                        healthMultiplier = 60f;
                        damageMultiplier = 25f;
                        coinMultiplier = 45f;
                    }
                    else
                    {
                        healthMultiplier = Mathf.Min(currentWave * 100f, 1000f);
                        damageMultiplier = Mathf.Min(currentWave * 20f, 500f);
                        coinMultiplier = Mathf.Min(currentWave * 20f, 500f);
                    }


                    clonedInfo.MaxHealth *= healthMultiplier;
                    clonedInfo.Damage *= damageMultiplier;
                    clonedInfo.CoinDropAmount = (ulong)(clonedInfo.CoinDropAmount * coinMultiplier);

                    clonedInfo.MovementSpeed *= 0.85f;
                    clonedInfo.AttackRange += .6f;
                }

                AudioManager.Instance.Play("Boss Appear");
                bossEnemy.ApplyBossInfo(clonedInfo, wave.IsMiniBossWave());
            }
            else backgroundMaterial.color = new Color(0.04705883f, 0.0509804f, 0.07843138f, 1f); // Roll back to the regular color if not boss wave

            return Task.CompletedTask;
        }

        private Task CreateEnemiesFromEntry(EnemyWaveEntry entry)
        {
            for (int i = 0; i < entry.NumberOfEnemies; i++)
            {
                string entryName = entry.EnemyPrefab.GetComponent<Enemy>().Info.Name;

                GameObject tempEnemy = GetEnemyFromPool(entryName, entry.EnemyPrefab);
                tempEnemy.SetActive(false);
                _enemiesCurrentWave.Add(tempEnemy);
            }
            OnWaveCreated?.Invoke(this, new OnWaveCreatedEventArgs { EnemyCount = _enemiesCurrentWave.Count });

            return Task.CompletedTask;
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
            foreach (GameObject enemyObj in _enemiesCurrentWave.TakeWhile(enemyObj => _canSpawnEnemies).ToList())
            {                
                Enemy enemy = enemyObj.GetComponent<Enemy>();
                // Center the boss on screen instead of random spawn
                if (enemy.IsBossInstance)
                {
                    enemyObj.transform.position = new Vector3(0f, 0f, 0f).WithDepth(EnemyConfig.EnemySpawnDepth);
                }
                else
                {
                    enemyObj.transform.position = GetRandomSpawnPosition();
                }

                float targetX = Random.Range(-EnemyConfig.BaseXArea, EnemyConfig.BaseXArea);
                Vector3 targetPos = new Vector3(targetX, 0f, 0f);
                enemy.MoveDirection = (targetPos - enemyObj.transform.position).normalized;
                enemyObj.SetActive(true);

                EnemyLibraryManager.Instance.MarkAsDiscovered(enemy.Info.Name);

                if (enemy.IsBossInstance) //Set boss music when boss is spawned
                {
                    AudioManager.Instance.StopAllMusics();
                    AudioManager.Instance.PlayMusic("Boss");
                    backgroundMaterial.color = new Color(0.3f, 0, 0);
                }

                enemy.OnDeath += Enemy_OnEnemyDeath;
                EnemiesAlive.Add(enemyObj);

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
            float randomXPosition = Random.Range(-spawnXSpread * 0.5f, spawnXSpread * 0.5f);
//            Random.Range(_screenLeft, _screenRight);
            return new Vector3(randomXPosition, 0f, EnemyConfig.EnemySpawnDepth);
        }

        private void Enemy_OnEnemyDeath(object sender, EventArgs e)
        {
            if (sender is Enemy enemy)
            {
                StartCoroutine(HandleEnemyDeath(enemy));

                OnEnemyDeath?.Invoke(this, new OnEnemyDeathEventArgs
                {
                    CurrencyType = enemy.Info.CurrencyDropType, 
                    Amount = enemy.Info.CoinDropAmount
                });
            }

            CheckIfWaveCompleted();
        }

        private IEnumerator HandleEnemyDeath(Enemy enemy)
        {
            enemy.OnDeath -= Enemy_OnEnemyDeath;
            if (enemy.IsBossInstance)
            {
                enemy.IsBossInstance = false;
                AudioManager.Instance.PlayMusic("Main");
                backgroundMaterial.color = new Color(0.04705883f, 0.0509804f, 0.07843138f, 1f);
            }
            EnemiesAlive.Remove(enemy.gameObject);

            yield return StartCoroutine(enemy.EnemyDeathEffect.PlayEffectRoutine());

            _objectPool.ReturnObject(enemy.Info.Name, enemy.gameObject);
        }

        private void CheckIfWaveCompleted()
        {
            if (_suppressWaveComplete)
                return;

            if (_waveSpawned && EnemiesAlive.Count <= 0)
                OnWaveCompleted?.Invoke(this, EventArgs.Empty);
        }

        private void PlayerBaseManager_OnWaveFailed(object sender, EventArgs e)
        {
            backgroundMaterial.color = new Color(0.04705883f, 0.0509804f, 0.07843138f, 1f);
            StopCoroutine(_spawnEnemiesCoroutine);
            _enemiesCurrentWave.Clear();
            StartCoroutine(RestartWave());
            if (_waveCompletionCheckCoroutine != null)
                StopCoroutine(_waveCompletionCheckCoroutine);

        }

        private IEnumerator RestartWave()
        {
            _canSpawnEnemies = false;            

            _suppressWaveComplete = true;

            yield return new WaitForSecondsRealtime(.5f); //Wait to secure all managers are done with EnemiesAlive list

            foreach (GameObject enemy in EnemiesAlive.ToList())
            {
                if (enemy == null)
                    continue;

                EnemiesAlive.Remove(enemy.gameObject);
                Enemy enemy_ = enemy.GetComponent<Enemy>();
                enemy_.IsBossInstance = false; // Reset boss state if it was a boss
                enemy_.OnDeath -= Enemy_OnEnemyDeath;
                _objectPool.ReturnObject(enemy.GetComponent<Enemy>().Info.Name, enemy);
            }

            _suppressWaveComplete = false;
        }

        private IEnumerator PeriodicWaveCompletionCheck()
        {
            WaitForSeconds wait = new WaitForSeconds(2f); // adjust as needed for performance
            while (_waveSpawned && !_suppressWaveComplete)
            {
                CheckIfWaveCompleted();
                yield return wait;
            }
        }


    }
}