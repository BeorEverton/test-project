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

        // Retreat rules:
        // - Only offered on non-boss / non-mini-boss waves when all gunners die.
        // - Can only retreat to the previous wave if that previous wave is NOT boss/mini-boss.
        // - Can only be used once until the player completes the retreated-to wave.
        private bool _retreatOfferedThisWave = false;
        private int _retreatDisabledOnWave = -1; // wave index where retreat is disabled (the wave you retreated to)

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

                // New wave => reset retreat offer and hide UI.
                _retreatOfferedThisWave = false;
                if (UIManager.Instance != null)
                    UIManager.Instance.ShowRetreatButton(false);

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
                if (RuntimeBalanceTuningManager.Instance != null)
                {
                    RuntimeBalanceTuningManager.Instance.ApplyToCurrentWaveOnly();
                }

                // Apply JSON formulas AFTER tuning so they remain the final step.
                if (BalanceFormulaManager.Instance != null)
                {
                    BalanceFormulaManager.Instance.ApplyToWave(_currentWaveInstance, _currentWave);
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
                    int completedWaveIndex = _currentWave;

                    if (_autoAdvanceEnabled && _zoneManager != null)
                    {
                        // Let ZoneManager advance zones and global wave index
                        _zoneManager.OnWaveCompleted(playerLost: false);

                        _currentWave = _zoneManager.GlobalWaveIndex;

                        StatsManager.Instance.TotalZonesSecured++;
                        StatsManager.Instance.MaxZone = _currentWave;

                        // If we were on a wave where retreat was disabled (because we retreated to it),
                        // and the player completed it, unlock retreat again for the next attempt.
                        if (completedWaveIndex == _retreatDisabledOnWave)
                            _retreatDisabledOnWave = -1;
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

        public void RebuildCurrentWaveNow()
        {
            // Stop current loop and clear the field
            AbortWaveAndDespawnAll();

            // Critical: ensure nobody can keep using the previous wave instance
            _currentWaveInstance = null;

            // Keep the same global index; just rebuild the Wave (ZoneManager will rebuild fresh clones)
            _autoAdvanceEnabled = true;

            // StartWaveRoutine will call ZoneManager.BuildWaveForCurrentStep() again
            ForceRestartWave();
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

            // Non-boss waves: offer retreat (player choice).
            if (!wave.IsMiniBossWave() && !wave.IsBossWave())
            {
                TryOfferRetreatForNonBossWave();
                return;
            }

            // Boss / mini-boss waves: keep your existing auto-rollback behavior.
            _autoAdvanceEnabled = false;

            int prev = Mathf.Max(1, GetCurrentWaveIndex() - 1);

            if (_enemySpawner != null)
                _enemySpawner.AbortWaveAndDespawnAll();

            LoadWave(prev);      // syncs ZoneManager
            ForceRestartWave();  // restart wave loop

            if (UIManager.Instance != null)
                UIManager.Instance.ShowManualAdvanceButton(true);
        }

        private void TryOfferRetreatForNonBossWave()
        {
            if (_retreatOfferedThisWave)
                return;

            // If we already retreated to this wave, don't offer retreat again until the wave is completed.
            if (_currentWave == _retreatDisabledOnWave)
                return;

            // Must have a previous wave.
            int prev = _currentWave - 1;
            if (prev < 1)
                return;

            // Can't go back to a boss/mini-boss wave.
            if (IsBossOrMiniBossWaveIndex(prev))
                return;

            _retreatOfferedThisWave = true;
            if (UIManager.Instance != null)
                UIManager.Instance.ShowRetreatButton(true);
        }

        public void RetreatToPreviousWave()
        {
            var wave = GetCurrentWave();
            if (wave == null) return;

            // Only meaningful for non-boss waves. Boss waves have their own rollback flow.
            if (wave.IsBossWave() || wave.IsMiniBossWave())
                return;

            if (_currentWave == _retreatDisabledOnWave)
                return;

            int prev = Mathf.Max(1, _currentWave - 1);
            if (IsBossOrMiniBossWaveIndex(prev))
                return;

            _retreatOfferedThisWave = true;
            _retreatDisabledOnWave = prev;

            if (UIManager.Instance != null)
                UIManager.Instance.ShowRetreatButton(false);

            if (_enemySpawner != null)
                _enemySpawner.AbortWaveAndDespawnAll();

            LoadWave(prev);
            ForceRestartWave();
        }

        private bool IsBossOrMiniBossWaveIndex(int waveIndex)
        {
            if (_zoneManager == null) return false;

            int current = _zoneManager.GlobalWaveIndex;
            try
            {
                _zoneManager.SetGlobalWaveIndex(waveIndex);
                Wave w = _zoneManager.BuildWaveForCurrentStep();
                return w != null && (w.IsBossWave() || w.IsMiniBossWave());
            }
            catch
            {
                // If anything goes wrong, fail "open" (treat as not boss) so we don't soft-lock players.
                return false;
            }
            finally
            {
                _zoneManager.SetGlobalWaveIndex(current);
            }
        }
        #endregion

        // Prestige manager needs a way to trigger a rebuild of the current wave with new formulas when the player changes prestige level.
        public void RebuildCurrentWaveInstanceForCurrentStep()
        {
            if (_zoneManager == null) return;

            // Rebuild the wave for the SAME current step (global wave index is held by ZoneManager)
            Wave rebuilt = _zoneManager.BuildWaveForCurrentStep();
            if (rebuilt == null) return;

            _currentWaveInstance = rebuilt;
        }

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
