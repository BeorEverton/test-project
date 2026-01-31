using Assets.Scripts.SO;
using Assets.Scripts.Systems;
using Assets.Scripts.Systems.Audio;
using Assets.Scripts.Systems.Save;
using Assets.Scripts.UI;
using System;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.WaveSystem
{
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        public event EventHandler<OnWaveStartedEventArgs> OnWaveStarted;
        public class OnWaveStartedEventArgs : EventArgs
        {
            public int WaveNumber;
        }

        public bool GameRunning = true;

        [SerializeField] private EnemySpawner _enemySpawner;

        [Tooltip("ZoneManager drives zone/wave progression and builds scaled waves.")]
        [SerializeField] private ZoneManager _zoneManager;

        // Global wave index (used for damage bonus, stats, etc.)
        private int _currentWave = 1;

        private bool _waveCompleted = false;
        private bool _waveLost = false;

        // The Wave instance currently being played (built by ZoneManager)
        private Wave _currentWaveInstance;

        // When false, player must click to start the next wave.
        public bool _autoAdvanceEnabled = true;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            if (_zoneManager == null)
            {
                _zoneManager = FindFirstObjectByType<ZoneManager>();
            }

            if (_zoneManager == null)
            {
                Debug.LogError("WaveManager: ZoneManager reference is missing. Please assign it in the inspector.");
                enabled = false;
                return;
            }

            _currentWave = _zoneManager.GlobalWaveIndex;

            StartCoroutine(StartWaveRoutine());

            EnemySpawner.Instance.OnWaveCompleted += EnemySpawner_OnWaveCompleted;
            if (PlayerBaseManager.Instance != null)
                PlayerBaseManager.Instance.OnWaveFailed += PlayerBaseManager_OnWaveFailed;

            // When all equipped gunners die
            if (GunnerManager.Instance != null)
                GunnerManager.Instance.OnAllEquippedGunnersDead += HandleAllEquippedGunnersDead;
        }

        public int GetCurrentWaveIndex() => _currentWave;

        /// <summary>
        /// Returns the Wave instance currently being played (last built by ZoneManager).
        /// </summary>
        public Wave GetCurrentWave() => _currentWaveInstance;

        /// <summary>
        /// Set the current global wave index and sync ZoneManager to that step.
        /// Used by debug tools and rollback.
        /// </summary>
        public void LoadWave(int waveNumber)
        {            
            _currentWave = Mathf.Clamp(waveNumber, 1, int.MaxValue);

            if (_zoneManager != null)
            {         
                _zoneManager.SetGlobalWaveIndex(_currentWave);
            }

            // The actual Wave for this index will be built when StartWaveRoutine
            // calls ZoneManager.BuildWaveForCurrentStep().
            _currentWaveInstance = null;
        }

        private void EnemySpawner_OnWaveCompleted(object sender, EventArgs e)
        {
            _waveCompleted = true;

            // Chatter: wave beaten (praise)
            if (GunnerManager.Instance != null)
            {
                var eq = GunnerManager.Instance.GetAllEquippedGunners();
                if (eq != null && eq.Count > 0)
                    GunnerChatterSystem.TryTrigger(GunnerEvent.WaveEnd, eq[UnityEngine.Random.Range(0, eq.Count)], null, 1f);
            }
        }

        private void PlayerBaseManager_OnWaveFailed(object sender, EventArgs e)
        {
            _waveLost = true;
            GameRunning = false;
            StopAllCoroutines();

            _autoAdvanceEnabled = false;

            // Chatter: wave lost (negative)
            if (GunnerManager.Instance != null)
            {
                var eq = GunnerManager.Instance.GetAllEquippedGunners();
                if (eq != null && eq.Count > 0)
                    GunnerChatterSystem.TryTrigger(GunnerEvent.NearlyDead, eq[UnityEngine.Random.Range(0, eq.Count)], null, 1f);
            }
        }

        private IEnumerator StartWaveRoutine()
        {
            while (GameRunning)
            {                
                // Keep local wave index in sync with ZoneManager.
                _currentWave = _zoneManager != null ? _zoneManager.GlobalWaveIndex : _currentWave;               

                OnWaveStarted?.Invoke(this, new OnWaveStartedEventArgs
                {
                    WaveNumber = _currentWave
                });

                // Chatter at wave start
                if (GunnerManager.Instance != null)
                {
                    var eq = GunnerManager.Instance.GetAllEquippedGunners();
                    if (eq != null && eq.Count > 0)
                        GunnerChatterSystem.TryTrigger(GunnerEvent.WaveStart, eq[UnityEngine.Random.Range(0, eq.Count)]);
                }

                // Prestige manager hook
                if (PrestigeManager.Instance != null)
                    PrestigeManager.Instance.NotifyWaveStarted(_currentWave);

                // Build the wave for this step via ZoneManager
                Wave wave = _zoneManager.BuildWaveForCurrentStep();
                if (wave == null)
                {
                    Debug.LogError("WaveManager.StartWaveRoutine: ZoneManager.BuildWaveForCurrentStep returned null, stopping game loop.");
                    GameRunning = false;
                    yield break;
                }

                _currentWaveInstance = wave;

                // Apply runtime tuning to the wave clones (does not touch assets).
                // Apply runtime tuning to the wave clones (does not touch assets).
                if (RuntimeBalanceTuningManager.Instance != null)
                {
                    RuntimeBalanceTuningManager.Instance.ApplyToCurrentWaveOnly();
                }

                try
                {
                    _enemySpawner.StartWave(wave);
                }
                catch (Exception e)
                {
                    Debug.LogError($"WaveManager.StartWaveRoutine: failed to start wave {_currentWave}. {e.Message}");
                }

                AudioManager.Instance.Play("New Wave");

                // Save game at the start of each round
                Debug.Log("WaveManager: Saving game at start of wave, current wave is " + _currentWave);
                SaveGameManager.Instance.SaveGame();

                yield return new WaitUntil(() => _waveCompleted || _waveLost);

                if (_waveCompleted)
                {
                    if (_autoAdvanceEnabled && _zoneManager != null)
                    {
                        // Let ZoneManager advance zones and global wave index
                        _zoneManager.OnWaveCompleted(playerLost: false);

                        _currentWave = _zoneManager.GlobalWaveIndex;

                        StatsManager.Instance.TotalZonesSecured++;
                        StatsManager.Instance.MaxZone = _currentWave;
                    }
                }

                _waveCompleted = false;
                _waveLost = false;
            }
        }

        /// <summary>
        /// Fully reset wave/zone state but DO NOT start coroutines here.
        /// Caller decides when to restart.
        /// </summary>
        public void ResetWave()
        {
            StopAllCoroutines();
            _waveCompleted = false;
            _waveLost = false;

            _currentWave = 1;
            _currentWaveInstance = null;

            if (_zoneManager != null)
            {
                _zoneManager.ResetProgress();
                _currentWave = _zoneManager.GlobalWaveIndex;
            }

            GameRunning = false;

            if (_enemySpawner != null)
            {
                _enemySpawner.ClearAllPoolsHard();
            }
        }

        public void ForceRestartWave()
        {
            StopAllCoroutines();
            _waveCompleted = false;
            _waveLost = false;

            GameRunning = true;
            StartCoroutine(StartWaveRoutine());
        }

        #region Stop wave progression

        /// <summary>
        /// Abort the current wave, return all active enemies to the pool,
        /// and mark the wave as completed so the loop advances immediately.
        /// </summary>
        public void JumpToNextWaveNow()
        {
            if (_enemySpawner != null)
            {
                _enemySpawner.AbortWaveAndDespawnAll();
            }

            _autoAdvanceEnabled = true;
            _waveCompleted = true;
        }

        public void AbortWaveAndDespawnAll()
        {
            StopAllCoroutines();

            if (_enemySpawner != null)
                _enemySpawner.AbortWaveAndDespawnAll();

            _waveCompleted = false;
            _waveLost = false;
            GameRunning = false;
        }

        public void SetAutoAdvanceEnabled(bool enabled)
        {
            _autoAdvanceEnabled = enabled;
        }

        public bool IsAutoAdvanceEnabled() => _autoAdvanceEnabled;

        private void HandleAllEquippedGunnersDead()
        {
            var wave = GetCurrentWave();
            if (wave == null) return;

            // Only for boss / mini-boss waves (now uses zone-aware flags).
            if (!wave.IsMiniBossWave() && !wave.IsBossWave())
                return;

            _autoAdvanceEnabled = false;

            int prev = Mathf.Max(1, GetCurrentWaveIndex() - 1);

            if (_enemySpawner != null)
                _enemySpawner.AbortWaveAndDespawnAll();

            LoadWave(prev);      // syncs ZoneManager
            ForceRestartWave();  // restart wave loop

            if (UIManager.Instance != null)
                UIManager.Instance.ShowManualAdvanceButton(true);
        }

        #endregion

        #region Debugging

        public void RestartAtWave(int targetWave)
        {
            AbortWaveAndDespawnAll();
            ResetWave();
            LoadWave(targetWave);

            _autoAdvanceEnabled = true;
            ForceRestartWave();
        }

        public void DebugJumpToWave(int targetWave)
        {
            targetWave = Mathf.Max(1, targetWave);
            RestartAtWave(targetWave);
        }

        #endregion
    }
}
