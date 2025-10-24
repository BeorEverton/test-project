using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.WaveSystem
{
    public class EnemyManager : MonoBehaviour
    {
        private bool _gameRunning;

        private readonly List<GameObject> _pendingRemovals = new List<GameObject>();

        // Advanced movement
        // Start steering after this progress from spawn depth to bottom (0.5 = midpoint)
        [SerializeField, Range(0f, 1f)] private float steerStartProgress = 0.50f;
        // Where steering reaches full strength (ease-in between start and full)
        [SerializeField, Range(0f, 1f)] private float steerFullProgress = 0.9f;
        // Fraction of forward speed used for sideways drift while steering
        [SerializeField] private float lateralSpeedMultiplier = 0.30f;

        private void Start()
        {
            EnemySpawner.Instance.OnWaveStarted += OnWaveStarted;
            EnemySpawner.Instance.OnWaveCompleted += OnWaveStopped;
            PlayerBaseManager.Instance.OnWaveFailed += OnWaveStopped;

        }

        private void OnDisable()
        {
            EnemySpawner.Instance.OnWaveStarted -= OnWaveStarted;
            EnemySpawner.Instance.OnWaveCompleted -= OnWaveStopped;
            PlayerBaseManager.Instance.OnWaveFailed -= OnWaveStopped;
        }

        private void OnWaveStopped(object sender, EventArgs e)
        {
            _gameRunning = false;
        }

        private void OnWaveStarted(object sender, EventArgs e)
        {
            _gameRunning = true;
        }

        private void Update()
        {
            if (!_gameRunning)
                return;

            HandleEnemiesAlive();
        }

        private void HandleEnemiesAlive()
        {
            var enemies = EnemySpawner.Instance.EnemiesAlive;

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemyGO = enemies[i];
                if (enemyGO == null)
                {
                    _pendingRemovals.Add(enemyGO);
                    continue;
                }

                Enemy enemyComponent = enemyGO.GetComponent<Enemy>();
                if (enemyComponent == null || !enemyComponent.IsAlive)
                {
                    _pendingRemovals.Add(enemyGO);
                    continue;
                }

                // Knockback handling
                if (enemyComponent.KnockbackTime > 0f)
                {
                    if (enemyComponent.IsBossInstance)
                    {
                        enemyComponent.KnockbackVelocity /= 2f;
                        enemyComponent.KnockbackTime /= 2f;
                    }

                    enemyComponent.KnockbackVelocity.x = 0f; // Ensure no lateral movement
                    enemyComponent.transform.position +=
                        (Vector3)(enemyComponent.KnockbackVelocity * Time.deltaTime);

                    enemyComponent.KnockbackTime -= Time.deltaTime;
                    continue; // Skip movement/attack while being pushed
                }

                // Movement & grid position
                if (!enemyComponent.CanAttack || enemyComponent.KnockbackTime > 0f)
                {
                    MoveEnemy(enemyComponent);
                    HandleGridPosition(enemyGO, enemyComponent);
                }
                else
                {
                    // Within attack range
                    if (enemyComponent.transform.position.Depth() <= enemyComponent.attackRange)
                    {
                        // Check for trap in current cell
                        if (TrapPoolManager.Instance.hasAnyTrapActive)
                        {
                            Trap trap = TrapPoolManager.Instance.GetTrapAtCell(enemyComponent.LastGridPos);
                            if (trap != null)
                                StartCoroutine(TriggerTrapCoroutine(trap, enemyComponent));
                        }

                        TryAttack(enemyComponent);
                    }
                    else
                    {
                        // Cancel attack state if pushed out of range
                        enemyComponent.CanAttack = false;
                    }
                }
            }

            // Remove any dead/null enemies in one pass
            if (_pendingRemovals.Count > 0)
            {
                for (int r = 0; r < _pendingRemovals.Count; r++)
                {
                    enemies.Remove(_pendingRemovals[r]);
                }
                _pendingRemovals.Clear();
            }
        }
        private void MoveEnemy(Enemy enemy)
        {
            var pos = enemy.transform.position;
            float depthNow = pos.Depth();

            // Attack gate doubles as the "do not change lateral anymore" threshold
            bool atBottom = depthNow <= enemy.attackRange;

            // 0..1 progress from spawn depth to bottom (based on EnemyConfig.EnemySpawnDepth)
            float prog = DepthProgress(depthNow);

            if (prog < steerStartProgress)
            {
                // BEFORE MIDPOINT: follow the original spawn path, but ONLY on depth.
                enemy.transform.position += enemy.MoveDirection * enemy.MovementSpeed * Time.deltaTime;
            }
            else
            {
                // AFTER MIDPOINT:
                if (!atBottom)
                {
                    // If a gunner is alive, drift toward the nearest gunner X; else keep original diagonal path.
                    int slot = GunnerManager.Instance.GetNearestAliveSlotByX(pos.x);
                    bool hasGunner = slot >= 0;

                    if (hasGunner)
                    {
                        // Smoothly ramp lateral strength from start -> full as we approach bottom
                        float t = Mathf.InverseLerp(steerStartProgress, steerFullProgress, prog);
                        float weight = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

                        float targetX = GunnerManager.Instance.GetSlotAnchorX(slot);
                        targetX += StableOffsetForEnemy(enemy, 1.3f); // stable per-enemy offset, no jitter

                        float stepX = Mathf.Max(0.001f, enemy.MovementSpeed * lateralSpeedMultiplier * weight) * Time.deltaTime;
                        pos.x = Mathf.MoveTowards(pos.x, targetX, stepX);

                        // Keep forward speed consistent with the spawn path timing
                        Vector3 forwardOnly = new Vector3(0f, enemy.MoveDirection.y, enemy.MoveDirection.z);
                        enemy.transform.position = pos + forwardOnly * enemy.MovementSpeed * Time.deltaTime;
                    }
                    else
                    {
                        // No gunners alive: continue on the original spawn path (includes its random bottom-X)
                        enemy.transform.position += enemy.MoveDirection * enemy.MovementSpeed * Time.deltaTime;
                    }
                }
                else
                {
                    // Already at bottom zone: keep the spawn path; do not change lateral intent
                    enemy.transform.position += enemy.MoveDirection * enemy.MovementSpeed * Time.deltaTime;
                }
            }

            if (enemy.gameObject.transform.position.Depth() <= enemy.attackRange)
                enemy.CanAttack = true;
        }



        private void HandleGridPosition(GameObject enemy, Enemy enemyComponent)
        {
            Vector2Int currentGridPos = GridManager.Instance.GetGridPosition(enemy.transform.position);

            if (currentGridPos == enemyComponent.LastGridPos)
                return;

            GridManager.Instance.RemoveEnemy(enemyComponent, enemyComponent.LastGridPos);
            GridManager.Instance.AddEnemy(enemyComponent);
            enemyComponent.LastGridPos = currentGridPos;

            if (TrapPoolManager.Instance.hasAnyTrapActive)
            {
                Trap trap = TrapPoolManager.Instance.GetTrapAtCell(currentGridPos);
                if (trap != null)
                    StartCoroutine(TriggerTrapCoroutine(trap, enemyComponent));
            }
        }

        private IEnumerator TriggerTrapCoroutine(Trap trap, Enemy enemyComponent)
        {
            if (trap.delay > 0f)
                yield return new WaitForSeconds(trap.delay);

            if (trap.radius <= 0f)
            {
                // Single target
                trap.Trigger(enemyComponent);

            }
            else
            {
                // AOE
                var enemies = GridManager.Instance.GetEnemiesInRange(
                    GridManager.Instance.GetWorldPosition(trap.cell, trap.worldY),
                    Mathf.CeilToInt(trap.radius));
                Vector3 trapWorldPos = GridManager.Instance.GetWorldPosition(trap.cell, trap.worldY);

                foreach (var e in enemies)
                {
                    if (e != null && Vector3.Distance(e.transform.position, trapWorldPos) <= trap.radius)
                        trap.Trigger(e);
                }
            }
        }

        private void TryAttack(Enemy enemy)
        {
            if (enemy.KnockbackTime > 0f)
                return;

            enemy.TimeSinceLastAttack += Time.deltaTime;
            if (enemy.TimeSinceLastAttack < 1 / enemy.Info.AttackSpeed)
                return;

            enemy.AnimateAttack();
            //Attack(enemy.Info.Damage);
            bool hitGunner = GunnerManager.Instance.TryApplyDamageForEnemy(enemy, enemy.Info.Damage);
            //if (!hitGunner)
            //  Debug.Log("All gunners are dead, ignoring attack");

            enemy.TimeSinceLastAttack = 0f;
        }

        private void Attack(float damage)
        {
            PlayerBaseManager.Instance.TakeDamage(damage);
        }

        // Progress helper: 0 at spawn depth, 1 at bottom. Uses EnemyConfig.EnemySpawnDepth.
        private static float DepthProgress(float currentDepth)
        {
            return Mathf.Clamp01(1f - (currentDepth / Mathf.Max(0.001f, EnemyConfig.EnemySpawnDepth)));
        }

        // Deterministic pseudo-random in [-range, +range] based on the instance id 
        private static float StableOffsetForEnemy(Enemy e, float range)
        {
            // LCG hash on the instance id → [0,1)
            unchecked
            {
                uint h = (uint)e.GetInstanceID();
                h = h * 1664525u + 1013904223u;
                float t = (h & 0x00FFFFFF) / 16777216f; // 0..1 (24-bit mantissa)
                return (t * 2f - 1f) * range;           // map to [-range, +range]
            }
        }

    }
}