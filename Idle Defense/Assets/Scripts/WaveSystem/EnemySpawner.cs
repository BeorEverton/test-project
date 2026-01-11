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
            public Vector3 WorldPosition; // death position of the enemy
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

        [Header("Spawn pacing / performance")]
        [Tooltip("Maximum number of enemies active at the same time. 0 = no limit.")]
        [SerializeField] private int _maxConcurrentEnemies = 500;

        [Tooltip("Minimum duration (seconds) to spawn the whole wave, even if TotalSpawnDuration is smaller.")]
        [SerializeField] private float _minSpawnDuration = 3f;

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

        // Reused per-despawn to avoid allocations
        private readonly HashSet<GameObject> _tmpReturned = new HashSet<GameObject>(256);


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

        public void ClearAllPoolsHard()
        {
            // Stop any activity and clear live lists
            AbortWaveAndDespawnAll();

            // Forget any cached config/buffers so we can't resume the old wave by mistake
            _currentWaveConfig = null;
            _enemiesCurrentWave.Clear();
            EnemiesAlive.Clear();
            _waveSpawned = false;
            _canSpawnEnemies = false;

            StopAllCoroutines();

            // Destroy every pooled instance to release memory
            _objectPool.ClearAll();
        }

        // ---  free pools for enemy types not present in the upcoming wave ---
        private void PurgePoolsNotIn(Wave wave)
        {
            if (wave == null || wave.WaveConfig == null) return;

            // Build the set of keys to keep. Keys are Info.Name used by ObjectPool.
            HashSet<string> keep = new HashSet<string>();
            foreach (var entry in wave.WaveConfig.EnemyWaveEntries)
            {
                if (entry == null || entry.EnemyPrefab == null) continue;
                var enemy = entry.EnemyPrefab.GetComponent<Enemy>();
                if (enemy == null || enemy.Info == null) continue;
                keep.Add(enemy.Info.Name);
            }

            _objectPool.ClearAllExcept(keep);
        }

        public async void StartWave(Wave wave)
        {            
            // Defensive: fully stop any previous spawning/check loops
            AbortWaveAndDespawnAll();

            // Purge unused enemy types before building this wave (saves memory long-term)
            PurgePoolsNotIn(wave);

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
            // Default background when not a boss wave
            Color defaultBgColor = new Color(0.04705883f, 0.0509804f, 0.07843138f, 1f);

            if (wave == null)
            {
                backgroundMaterial.color = defaultBgColor;
                return Task.CompletedTask;
            }

            bool isMini = wave.IsMiniBossWave();
            bool isBoss = wave.IsBossWave();

            if (!isMini && !isBoss)
            {
                backgroundMaterial.color = defaultBgColor;
                return Task.CompletedTask;
            }

            Enemy bossEnemy = null;

            // --- 1) Try to use zone/boss-specific prefab if configured ---
            GameObject overridePrefab = null;

            if (isBoss && wave.BossPrefab != null)
                overridePrefab = wave.BossPrefab;
            else if (isMini && wave.MiniBossPrefab != null)
                overridePrefab = wave.MiniBossPrefab;

            if (overridePrefab != null)
            {
                Enemy prefabEnemy = overridePrefab.GetComponent<Enemy>();
                if (prefabEnemy == null || prefabEnemy.Info == null)
                {
                    Debug.LogWarning("EnemySpawner.CheckIfBossWave: override boss prefab is missing Enemy/Info; falling back to random enemy.");
                }
                else
                {
                    string key = prefabEnemy.Info.Name;

                    GameObject bossGO = GetEnemyFromPool(key, overridePrefab);
                    bossGO.SetActive(false); // will be activated later in SpawnEnemies()

                    _enemiesCurrentWave.Add(bossGO);

                    // Update UI with new total enemy count
                    OnWaveCreated?.Invoke(this, new OnWaveCreatedEventArgs { EnemyCount = _enemiesCurrentWave.Count });

                    bossEnemy = bossGO.GetComponent<Enemy>();
                }
            }

            // 2) Fallback: pick any existing enemy and promote it to boss
            if (bossEnemy == null)
            {
                if (_enemiesCurrentWave.Count == 0)
                {
                    Debug.LogWarning("EnemySpawner.CheckIfBossWave: boss/miniboss wave requested but _enemiesCurrentWave is empty.");
                    backgroundMaterial.color = defaultBgColor;
                    return Task.CompletedTask;
                }

                bossEnemy = _enemiesCurrentWave[Random.Range(0, _enemiesCurrentWave.Count)].GetComponent<Enemy>();
            }

            if (bossEnemy == null || bossEnemy.Info == null)
            {
                Debug.LogWarning("EnemySpawner.CheckIfBossWave: could not obtain a valid Enemy to promote to boss.");
                backgroundMaterial.color = defaultBgColor;
                return Task.CompletedTask;
            }

            EnemyInfoSO baseInfo = bossEnemy.Info;

            // If ZoneManager built a scaled clone for this enemy id, use that as the baseline.
            // This ensures bosses scale with the current wave difficulty BEFORE the boss multipliers.
            if (wave != null && wave.WaveEnemies != null)
            {
                int id = baseInfo != null ? baseInfo.EnemyId : 0;
                if (id > 0 && wave.WaveEnemies.TryGetValue(id, out var scaled))
                    baseInfo = scaled;
            }

            EnemyInfoSO clonedInfo = Instantiate(baseInfo);

            if (isMini)
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

            if (isBoss)
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

            bossEnemy.ApplyBossInfo(clonedInfo, isMini);

            // Boss waves tint the background; non-boss fallback handled earlier
            if (backgroundMaterial != null)
                backgroundMaterial.color = new Color(0.3f, 0, 0);

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
        /// <summary>
        /// Spawn enemies over time using the per-wave spawn curve defined in WaveConfigSO.
        /// The curve controls cumulative fraction of enemies spawned from t=0..1.
        /// We also enforce a max concurrent enemy count for performance.
        /// </summary>
        private IEnumerator SpawnEnemies()
        {
            int totalEnemies = _enemiesCurrentWave.Count;
            if (totalEnemies <= 0)
            {
                _waveSpawned = true;
                CheckIfWaveCompleted();
                yield break;
            }

            // --- Determine spawn duration for THIS wave ---
            float spawnDuration = _minSpawnDuration;

            if (_currentWaveConfig != null)
            {
                if (_currentWaveConfig.TotalSpawnDuration > 0f)
                {
                    spawnDuration = Mathf.Max(_minSpawnDuration, _currentWaveConfig.TotalSpawnDuration);
                }
                else
                {
                    // Fallback: original behaviour scaled by enemy count.
                    float guess = _currentWaveConfig.TimeBetweenSpawns * totalEnemies;
                    spawnDuration = Mathf.Max(_minSpawnDuration, guess);
                }
            }

            // --- Determine curve for THIS wave ---
            AnimationCurve curve;
            if (_currentWaveConfig != null &&
                _currentWaveConfig.SpawnCurve != null &&
                _currentWaveConfig.SpawnCurve.length >= 2)
            {
                curve = _currentWaveConfig.SpawnCurve;
            }
            else
            {
                // Linear fallback if no curve assigned
                curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            }

            float elapsed = 0f;
            int spawnedCount = 0;
            int maxConcurrent = _maxConcurrentEnemies;

            while (_canSpawnEnemies && spawnedCount < totalEnemies)
            {
                elapsed += Time.deltaTime;
                float t = spawnDuration > 0f ? Mathf.Clamp01(elapsed / spawnDuration) : 1f;

                // Curve gives us cumulative fraction that *should* be spawned now.
                float fraction = Mathf.Clamp01(curve.Evaluate(t));
                int targetSpawned = Mathf.Clamp(
                    Mathf.RoundToInt(fraction * totalEnemies),
                    0,
                    totalEnemies);

#if UNITY_EDITOR
                if (Time.frameCount % 15 == 0) // not every frame
                {
                    //Debug.Log($"[SpawnCurve] t={t:0.00} frac={fraction:0.00} target={targetSpawned}/{totalEnemies} spawned={spawnedCount} alive={EnemiesAlive.Count}");
                }
#endif


                // Spawn towards target, respecting concurrent cap.                
                while (_canSpawnEnemies &&
                       spawnedCount < targetSpawned &&
                       (maxConcurrent <= 0 || EnemiesAlive.Count < maxConcurrent))
                {
                    // If something aborted the wave and cleared the buffer, bail.
                    if (_enemiesCurrentWave.Count == 0)
                        break;

                    // Take one enemy from the end of the buffer and remove it.
                    int lastIndex = _enemiesCurrentWave.Count - 1;
                    GameObject enemyObj = _enemiesCurrentWave[lastIndex];
                    _enemiesCurrentWave.RemoveAt(lastIndex);
                    spawnedCount++;

                    Enemy enemy = enemyObj.GetComponent<Enemy>();

                    // Hook death tracking 
                    enemy.OnDeath += Enemy_OnEnemyDeath;
                    if (!EnemiesAlive.Contains(enemyObj))
                        EnemiesAlive.Add(enemyObj);

                    // Center the boss on screen instead of random spawn
                    if (enemy.IsBossInstance)
                    {
                        enemyObj.transform.position =
                            new Vector3(0f, 0f, 0f).WithDepth(EnemyConfig.EnemySpawnDepth);
                    }
                    else
                    {
                        enemyObj.transform.position = GetRandomSpawnPosition();
                    }

                    float targetX = Random.Range(-EnemyConfig.BaseXArea, EnemyConfig.BaseXArea);

                    EnemyLibraryManager.Instance.MarkAsDiscovered(enemy.Info.Name);

                    if (enemy.IsBossInstance) // Set boss music when boss is spawned
                    {
                        AudioManager.Instance.StopAllMusics();
                        AudioManager.Instance.PlayMusic("Boss");
                        backgroundMaterial.color = new Color(0.3f, 0, 0);
                        targetX = 0;
                    }

                    Vector3 targetPos = new Vector3(targetX, 0f, 0f);
                    enemy.MoveDirection = (targetPos - enemyObj.transform.position).normalized;

                    // all listeners ready
                    enemyObj.SetActive(true);

                    // Gunner XP
                    GunnerManager.Instance.OnEnemySpawned(enemy);
                }


                // Next frame: re-evaluate curve and concurrent cap.
                yield return null;
            }

            _waveSpawned = true;

            // In case all enemies were killed during spawn duration
            CheckIfWaveCompleted();
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
                // Chatter boss killed
                if (enemy.IsBossInstance && GunnerManager.Instance != null)
                {
                    var eq = GunnerManager.Instance.GetAllEquippedGunners();
                    if (eq != null && eq.Count > 0)
                    {
                        var a = eq[Random.Range(0, eq.Count)];
                        // Let the first speak, quick reply is handled by the chatter system
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
                    Amount = boosted,
                    WorldPosition = enemy.transform.position
                });
            }

            CheckIfWaveCompleted();
        }

        private IEnumerator HandleEnemyDeath(Enemy enemy)
        {
            // Stop listening to this instance
            enemy.OnDeath -= Enemy_OnEnemyDeath;

            // If it was a boss, reset presentation
            if (enemy.IsBossInstance)
            {
                enemy.IsBossInstance = false;
                AudioManager.Instance.PlayMusic("Main");
                backgroundMaterial.color = new Color(0.04705883f, 0.0509804f, 0.07843138f, 1f);
            }

            // Remove from live list
            EnemiesAlive.Remove(enemy.gameObject);

            // Play death FX before returning to the pool
            yield return StartCoroutine(enemy.EnemyDeathEffect.PlayEffectRoutine());

            // Return once to the pool for reuse in future waves
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
            WaitForSeconds wait = new WaitForSeconds(2f); // for performance
            
            // wait until the wave has finished spawning
            while (!_waveSpawned && !_suppressWaveComplete)
                yield return null;

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

            // Track what we returned in pass A to avoid double-enqueue in pass B
            _tmpReturned.Clear();

            // --- Pass A: return every active enemy now ---
            foreach (var enemyObj in EnemiesAlive.ToList())
            {
                if (enemyObj == null) { continue; }

                var enemy = enemyObj.GetComponent<Enemy>();
                if (enemy == null) { continue; }

                // detach death callback; do NOT trigger death sequence or rewards
                enemy.OnDeath -= Enemy_OnEnemyDeath;

                if (enemy.IsBossInstance) { hadBoss = true; enemy.IsBossInstance = false; }

                // return to pool immediately (will SetActive(false) inside)
                _objectPool.ReturnObject(enemy.Info.Name, enemyObj);
                _tmpReturned.Add(enemyObj);
                EnemiesAlive.Remove(enemyObj);
            }

            // --- Pass B: return any pre-created-but-not-spawned entries ---
            // These were SetActive(false) in CreateEnemiesFromEntry and never added to EnemiesAlive.
            // We skip those already handled above (in case some were spawned earlier).
            for (int i = 0; i < _enemiesCurrentWave.Count; i++)
            {
                var enemyObj = _enemiesCurrentWave[i];
                if (enemyObj == null) continue;
                if (_tmpReturned.Contains(enemyObj)) continue; // already returned in Pass A

                var enemy = enemyObj.GetComponent<Enemy>();
                if (enemy == null) continue;

                // Return to pool (safe even if already inactive)
                _objectPool.ReturnObject(enemy.Info.Name, enemyObj);
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

        #region Enemy Spawn Enemy
        /// <summary>
        /// Ensure the pool has 'count' ready instances for the given prefab key.
        /// </summary>
        public void PrewarmPrefab(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0) return;
            string key = prefab.GetComponent<Enemy>().Info.Name;

            // Pull 'count' objects and immediately return them to the pool inactive.
            for (int i = 0; i < count; i++)
            {
                GameObject obj = _objectPool.GetObject(key, prefab);
                // Make sure we don't accidentally show it on screen.
                obj.transform.position = new Vector3(9999, 9999, 9999);
                _objectPool.ReturnObject(key, obj);
            }
        }

        /// <summary>
        /// Spawn one enemy from the pool at the specified world position
        /// Automatically hooks into EnemiesAlive and death events
        /// </summary>
        public Enemy SpawnSummonedEnemy(GameObject prefab, Vector3 worldPos)
        {
            if (prefab == null) return null;

            string key = prefab.GetComponent<Enemy>().Info.Name;
            GameObject eObj = _objectPool.GetObject(key, prefab);
            eObj.transform.position = worldPos;
            eObj.transform.rotation = Quaternion.identity;

            Enemy e = eObj.GetComponent<Enemy>();

            // Hook death and tracking 
            e.OnDeath += Enemy_OnEnemyDeath;
            if (!EnemiesAlive.Contains(eObj))
                EnemiesAlive.Add(eObj);

            // Set initial movement like regular spawns
            float targetX = Random.Range(-EnemyConfig.BaseXArea, EnemyConfig.BaseXArea);
            Vector3 targetPos = new Vector3(targetX, 0f, 0f);
            e.MoveDirection = (targetPos - eObj.transform.position).normalized;

            // Activate and integrate with current wave/presentation
            eObj.SetActive(true);

            // Gunner XP
            GunnerManager.Instance.OnEnemySpawned(e);

            return e;
        }

        #endregion


    }
}