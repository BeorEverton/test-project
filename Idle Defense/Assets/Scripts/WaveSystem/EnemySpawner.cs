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

        public static float spawnXSpread = 22f;

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
            _spawnEnemiesCoroutine = StartCoroutine(SpawnEnemiesDelayed());

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

                // Chatter: boss appeared
                if (GunnerManager.Instance != null)
                {
                    var equipped = GunnerManager.Instance.GetAllEquippedGunners();
                    if (equipped != null && equipped.Count > 0)
                    {
                        var a = equipped[Random.Range(0, equipped.Count)];
                        GunnerChatterSystem.TryTrigger(GunnerEvent.BossAppeared, a);
                    }
                }

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

        private IEnumerator SpawnEnemiesDelayed()
        {
            yield return null; // wait 1 frame to ensure setup
            yield return StartCoroutine(SpawnEnemies());
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

                EnemyLibraryManager.Instance.MarkAsDiscovered(enemy.Info.Name);

                if (enemy.IsBossInstance) //Set boss music when boss is spawned
                {
                    AudioManager.Instance.StopAllMusics();
                    AudioManager.Instance.PlayMusic("Boss");
                    backgroundMaterial.color = new Color(0.3f, 0, 0);
                    targetX = 0;
                }

                Vector3 targetPos = new Vector3(targetX, 0f, 0f);
                enemy.MoveDirection = (targetPos - enemyObj.transform.position).normalized;
                enemyObj.SetActive(true);

                // Gunner XP
                GunnerManager.Instance.OnEnemySpawned(enemy);


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
            float randomXPosition = Random.Range(-spawnXSpread, spawnXSpread);

            return new Vector3(randomXPosition, 0f, EnemyConfig.EnemySpawnDepth);
        }

        private void Enemy_OnEnemyDeath(object sender, EventArgs e)
        {
            if (sender is Enemy enemy)
            {
                // Chatter: boss killed (check flag BEFORE it's reset in HandleEnemyDeath)
                if (enemy.IsBossInstance && GunnerManager.Instance != null)
                {
                    var eq = GunnerManager.Instance.GetAllEquippedGunners();
                    if (eq != null && eq.Count > 0)
                    {
                        var a = eq[Random.Range(0, eq.Count)];
                        // Let the first speak; quick reply is handled by the chatter system
                        GunnerChatterSystem.TryTrigger(GunnerEvent.BossKilled, a);
                    }
                }

                StartCoroutine(HandleEnemyDeath(enemy));

                // Apply prestige currency multiplier before notifying listeners
                ulong baseAmount = enemy.Info.CoinDropAmount;
                float mult = 1f;

                var pm = PrestigeManager.Instance;
                if (pm != null)
                {
                    mult = (enemy.Info.CurrencyDropType == Currency.BlackSteel)
                        ? pm.GetBlackSteelGainMultiplier()
                        : pm.GetScrapsGainMultiplier();
                }

                // round and ensure at least 1 if there was any base drop
                ulong boosted = baseAmount == 0 ? 0 : (ulong)Mathf.Max(1, Mathf.RoundToInt(baseAmount * mult));

                OnEnemyDeath?.Invoke(this, new OnEnemyDeathEventArgs
                {
                    CurrencyType = enemy.Info.CurrencyDropType,
                    Amount = boosted
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
            {
                OnWaveCompleted?.Invoke(this, EventArgs.Empty);
            }
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


        #region Wave stop and manual advance
        /// <summary>
        /// Stop spawning, suppress completion checks, and despawn every active enemy.
        /// Used by WaveManager to skip mid-wave without playing death FX or awarding drops.
        /// </summary>
        public void AbortWaveAndDespawnAll()
        {
            // stop any ongoing spawning/check loops
            _canSpawnEnemies = false;
            if (_spawnEnemiesCoroutine != null) { StopCoroutine(_spawnEnemiesCoroutine); _spawnEnemiesCoroutine = null; }
            if (_waveCompletionCheckCoroutine != null) { StopCoroutine(_waveCompletionCheckCoroutine); _waveCompletionCheckCoroutine = null; }

            // prevent OnWaveCompleted from firing while we clear the field
            _suppressWaveComplete = true;

            DespawnAllActiveEnemies();

            // allow normal completion checks again
            _suppressWaveComplete = false;
        }

        /// <summary>
        /// Return all alive enemies to the pool instantly (no FX), clear wave buffers,
        /// and restore presentation (music/background) if a boss was present.
        /// </summary>
        public void DespawnAllActiveEnemies()
        {
            bool hadBoss = false;

            // copy to avoid modifying while iterating
            foreach (var enemyObj in EnemiesAlive.ToList())
            {
                if (enemyObj == null) { continue; }

                var enemy = enemyObj.GetComponent<Enemy>();
                if (enemy == null) { continue; }

                // detach death callback; do NOT trigger death sequence or rewards
                enemy.OnDeath -= Enemy_OnEnemyDeath;

                if (enemy.IsBossInstance) { hadBoss = true; enemy.IsBossInstance = false; }

                // return to pool immediately
                _objectPool.ReturnObject(enemy.Info.Name, enemyObj);
                EnemiesAlive.Remove(enemyObj);
            }

            // clear any pre-created-but-not-spawned entries for this wave
            _enemiesCurrentWave.Clear();
            _waveSpawned = false;

            // restore presentation
            if (hadBoss)
            {
                AudioManager.Instance.PlayMusic("Main");
            }
            backgroundMaterial.color = new Color(0.04705883f, 0.0509804f, 0.07843138f, 1f);
        }


        #endregion

    }
}