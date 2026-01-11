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

        private readonly Dictionary<int, int> _bossLockedSlot = new Dictionary<int, int>();

        private readonly List<GameObject> _pendingRemovals = new List<GameObject>();

        // Advanced movement
        // Start steering after this progress from spawn depth to bottom (0.5 = midpoint)
        [SerializeField, Range(0f, 1f)] private float steerStartProgress = 0.50f;
        // Where steering reaches full strength (ease-in between start and full)
        [SerializeField, Range(0f, 1f)] private float steerFullProgress = 0.9f;
        // Fraction of forward speed used for sideways drift while steering
        [SerializeField] private float lateralSpeedMultiplier = 0.30f;

        // Healer batching: process only ~1/N healers per frame to spread work.
        private const int HealerBatches = 6; 


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

                // Summoner: tick from spawn time so FirstDelay is real time
                if (enemyComponent.Info.SummonerEnabled)
                {
                    if (enemyComponent.SummonerReady(Time.deltaTime))
                        enemyComponent.DoSummon();   // cheap; batching not needed here
                }

                // Knockback handling
                if (enemyComponent.KnockbackTime > 0f)
                {
                    if (enemyComponent.IsBossInstance)
                    {
                        enemyComponent.KnockbackVelocity /= 2f;
                        enemyComponent.KnockbackTime /= 2f;
                    }

                    // Strictly depth-only knockback: no X drift, never touch Y
                    enemyComponent.KnockbackVelocity.x = 0f;

                    float stepMag = Mathf.Abs(enemyComponent.KnockbackVelocity.y) * Time.deltaTime;
                    Vector3 p = enemyComponent.transform.position;

                    // Choose the Z direction that INCREASES Depth()
                    float depthNow = p.Depth();
                    float depthPlus = new Vector3(p.x, p.y, p.z + stepMag).Depth();
                    float depthMinus = new Vector3(p.x, p.y, p.z - stepMag).Depth();

                    float signedStepZ = (depthPlus > depthMinus) ? (+stepMag) : (-stepMag);
                    enemyComponent.transform.position = new Vector3(p.x, p.y, p.z + signedStepZ);

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
                    // Healer: manager-batched
                    if (enemyComponent.Info.HealerEnabled)
                    {
                        int batch = Time.frameCount % HealerBatches;
                        if ((enemyComponent.GetInstanceID() & int.MaxValue) % HealerBatches == batch)
                        {
                            if (enemyComponent.HealerReady(Time.deltaTime * HealerBatches))
                                DoHealerBurst(enemyComponent);
                        }
                    }                    

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

            // Kamikaze behavior: if enabled, explode instead of a normal attack(no rewards/ XP).
            if (enemy.Info.KamikazeOnReach)
            {
                enemy.TriggerKamikazeExplosion();
                enemy.TimeSinceLastAttack = 0f;
                return;
            }

            int mainSlot = -1;
            int id = enemy.GetInstanceID();

            if (enemy.IsBossInstance)
            {
                // Ensure we have a locked slot for this boss:
                bool needLock = !_bossLockedSlot.TryGetValue(id, out mainSlot);

                // If we had a lock, verify it's still alive; if dead, re-lock.
                if (!needLock)
                {
                    // Probe with 0 damage: returns false if no living gunner in that slot.
                    if (!GunnerManager.Instance.ApplyDamageOnSlot(mainSlot, 0f))
                        needLock = true;
                }

                if (needLock)
                {
                    // Prefer slot 2 if alive; otherwise nearest alive by X.
                    mainSlot = 2;
                    bool midAlive = GunnerManager.Instance.ApplyDamageOnSlot(2, 0f);
                    if (!midAlive)
                        mainSlot = GunnerManager.Instance.GetNearestAliveSlotByX(enemy.transform.position.x);

                    _bossLockedSlot[id] = mainSlot; // may be -1 if none found; handled below
                }
            }
            else
            {
                // Normal enemies: nearest living gunner by X
                mainSlot = GunnerManager.Instance.GetNearestAliveSlotByX(enemy.transform.position.x);
            }

            if (mainSlot >= 0)
            {
                int[] slotsToHit = GetSweepSlots(mainSlot, enemy.Info.SweepTargets);
                for (int i = 0; i < slotsToHit.Length; i++)
                    GunnerManager.Instance.ApplyDamageOnSlot(slotsToHit[i], enemy.Info.Damage);
            }

            enemy.TimeSinceLastAttack = 0f;
        }

        private void Attack(float damage)
        {
            PlayerBaseManager.Instance.TakeDamage(damage);
        }

        /// <summary>
        /// Heals up to MaxTargets nearest allies within radius around 'e'.
        /// Uses grid broad-phase + squared-distance narrow phase. No allocations, no sorting.
        /// </summary>
        private void DoHealerBurst(Enemy e)
        {
            float radius = Mathf.Max(0f, e.Info.HealerRadius);
            int maxTargets = Mathf.Max(1, e.Info.HealerMaxTargets);
            float healPct = Mathf.Clamp01(e.Info.HealerHealPctOfMaxHP);
            if (radius <= 0f || healPct <= 0f) return;

            // Broad phase: your grid query (same as boss explosion approach)
            var nearby = GridManager.Instance.GetEnemiesInRange(e.transform.position, Mathf.CeilToInt(radius));
            if (nearby == null || nearby.Count == 0) return; // nothing nearby
                                                             // (Your GridManager API is already used for boss explosion and traps.) :contentReference[oaicite:5]{index=5}

            // Narrow phase: pick up to K nearest by squared distance (no allocations).
            float r2 = radius * radius;

            // Fixed small buffers (K <= 5 typically)
            Enemy a0 = null, a1 = null, a2 = null, a3 = null, a4 = null;
            float d0 = float.MaxValue, d1 = float.MaxValue, d2 = float.MaxValue, d3 = float.MaxValue, d4 = float.MaxValue;

            Vector3 epos = e.transform.position;

            for (int i = 0; i < nearby.Count; i++)
            {
                var cand = nearby[i];
                if (cand == null || cand == e || !cand.IsAlive) continue;

                Vector3 cpos = cand.transform.position;
                float dx = cpos.x - epos.x;
                float dz = cpos.z - epos.z;
                float d2s = dx * dx + dz * dz;
                if (d2s > r2) continue;

                // Insert into the small "sorted" buffer (K<=5) with manual shifts.
                if (d2s < d0) { a4 = a3; d4 = d3; a3 = a2; d3 = d2; a2 = a1; d2 = d1; a1 = a0; d1 = d0; a0 = cand; d0 = d2s; }
                else if (d2s < d1) { a4 = a3; d4 = d3; a3 = a2; d3 = d2; a2 = a1; d2 = d1; a1 = cand; d1 = d2s; }
                else if (d2s < d2) { a4 = a3; d4 = d3; a3 = a2; d3 = d2; a2 = cand; d2 = d2s; }
                else if (d2s < d3) { a4 = a3; d4 = d3; a3 = cand; d3 = d2s; }
                else if (d2s < d4) { a4 = cand; d4 = d2s; }
            }

            // Heal up to maxTargets; each ally gets % of its own MaxHealth
            int healed = 0;
            if (a0 != null && healed < maxTargets) { a0.Heal(a0.MaxHealth * healPct); healed++; }
            if (a1 != null && healed < maxTargets) { a1.Heal(a1.MaxHealth * healPct); healed++; }
            if (a2 != null && healed < maxTargets) { a2.Heal(a2.MaxHealth * healPct); healed++; }
            if (a3 != null && healed < maxTargets) { a3.Heal(a3.MaxHealth * healPct); healed++; }
            if (a4 != null && healed < maxTargets) { a4.Heal(a4.MaxHealth * healPct); healed++; }
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

        private int[] GetSweepSlots(int mainSlot, int sweepTargets)
        {
            List<int> hits = new List<int> { mainSlot };
            sweepTargets = Mathf.Clamp(sweepTargets, 1, 5);

            if (sweepTargets > 1)
            {
                if (mainSlot == 0) hits.Add(1);
                else if (mainSlot == 4) hits.Add(3);
                else
                {
                    // Deterministic side: for 1 -> hit left (0); for 3 -> hit right (4); for 2 -> prefer right (3).
                    int neighbor =
                        (mainSlot <= 1) ? (mainSlot - 1) :               // 1 -> 0
                        (mainSlot >= 3) ? (mainSlot + 1) :               // 3 -> 4
                                           (mainSlot + 1);               // 2 -> 3 (prefer right)
                    neighbor = Mathf.Clamp(neighbor, 0, 4);
                    hits.Add(neighbor);
                }

                // If in the future you allow 3–5 targets, extend here by adding the opposite neighbor, then expanding outward.
            }

            return hits.ToArray();
        }

    }
}