using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Assets.Scripts.Enemies.Enemy;

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

        public static EnemyManager Instance { get; private set; }

        private enum PendingSkillType
        {
            None,
            Heal,
            Summon,
            Kamikaze,
            BossSkill
        }

        private struct PendingSkill
        {
            public PendingSkillType Type;
            public float HealAmount;
            public int SummonCount;

            // Boss specific
            public BossSkillId BossSkill;
            public float ParamA;
            public float ParamB;
            public int ParamI;
        }

        public enum BossSkillId
        {
            ShieldGain,
            HealPulse,
            SummonWave,
            BuffSelf,
            DebuffSelf,      
            JumpDepth,       
            SpecialGunnerHit,
            DeathExplosionMode // sets how death explosion behaves
        }

        public void SetPendingBossSkill(Enemy enemy, BossSkillId skill, float a = 0f, float b = 0f, int i = 0)
        {
            if (enemy == null) return;
            _pendingSkills[enemy.GetInstanceID()] = new PendingSkill
            {
                Type = PendingSkillType.BossSkill,
                HealAmount = 0f,
                SummonCount = 0,
                BossSkill = skill,
                ParamA = a,
                ParamB = b,
                ParamI = i
            };
        }

        private readonly Dictionary<int, PendingSkill> _pendingSkills = new Dictionary<int, PendingSkill>();

        private struct PendingAttack
        {
            public int SlotA;
            public int SlotB;
            public float Damage;
        }

        private readonly Dictionary<int, PendingAttack> _pendingAttacks = new Dictionary<int, PendingAttack>();

        private void Awake()
        {
            Instance = this;
        }

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

                enemyComponent.TickRuntimeBuffs(Time.deltaTime);
                enemyComponent.TickMovementLock(Time.deltaTime);

                // Summoner: tick from spawn time so FirstDelay is real time
                if (enemyComponent.Info.SummonerEnabled)
                {
                    if (enemyComponent.SummonerReady(Time.deltaTime))
                    {
                        // Skill animation for Summon
                        SetPendingSummon(enemyComponent, Mathf.Max(1, enemyComponent.Info.SummonCount));
                        enemyComponent.TriggerAnimSkill();
                    }
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

                    enemyComponent.SetAnimIdle(false);

                    enemyComponent.KnockbackTime -= Time.deltaTime;
                    continue; // Skip movement/attack while being pushed
                }

                // Movement & grid position
                if (!enemyComponent.CanAttack || enemyComponent.KnockbackTime > 0f)
                {
                    enemyComponent.SetAnimIdle(false);

                    MoveEnemy(enemyComponent);
                    HandleGridPosition(enemyGO, enemyComponent);
                }
                else
                {
                    enemyComponent.SetAnimIdle(true);

                    // Healer: manager-batched
                    if (enemyComponent.Info.HealerEnabled)
                    {
                        int batch = Time.frameCount % HealerBatches;
                        if ((enemyComponent.GetInstanceID() & int.MaxValue) % HealerBatches == batch)
                        {
                            if (enemyComponent.HealerReady(Time.deltaTime * HealerBatches))
                            {
                                // Drive heal via animation event
                                SetPendingHeal(enemyComponent, 0f); // amount computed inside ExecuteHealNow currently
                                enemyComponent.TriggerAnimSkill();
                            }
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
            if (enemy.IsMovementLocked)
                return;

            var pos = enemy.transform.position;
            float depthNow = pos.Depth();

            // If in attack range, STOP movement immediately (prevents "moving in place")
            if (depthNow <= enemy.attackRange)
            {
                if (!enemy.CanAttack)
                {
                    enemy.CanAttack = true;

                    // Allow instant first attack on arrival
                    float atkSpd = Mathf.Max(0.001f, enemy.Info.AttackSpeed);
                    enemy.TimeSinceLastAttack = 1f / atkSpd;
                }

                enemy.SetAnimIdle(true);
                return;
            }

            enemy.SetAnimIdle(false);

            // 0..1 progress from spawn depth to bottom
            float prog = DepthProgress(depthNow);

            if (prog < steerStartProgress)
            {
                float moveSpeed = enemy.MovementSpeed * enemy.SpeedMultRT;
                enemy.transform.position += enemy.MoveDirection * moveSpeed * Time.deltaTime;

                return;
            }

            // AFTER MIDPOINT: steer toward nearest alive gunner X
            int slot = GunnerManager.Instance.GetNearestAliveSlotByX(pos.x);
            bool hasGunner = slot >= 0;

            if (hasGunner)
            {
                float t = Mathf.InverseLerp(steerStartProgress, steerFullProgress, prog);
                float weight = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

                float targetX = GunnerManager.Instance.GetSlotAnchorX(slot);
                targetX += StableOffsetForEnemy(enemy, 1.3f);

                float stepX = Mathf.Max(0.001f, enemy.MovementSpeed * lateralSpeedMultiplier * weight) * Time.deltaTime;
                pos.x = Mathf.MoveTowards(pos.x, targetX, stepX);

                Vector3 forwardOnly = new Vector3(0f, enemy.MoveDirection.y, enemy.MoveDirection.z);
                float moveSpeed = enemy.MovementSpeed * enemy.SpeedMultRT;
                enemy.transform.position += enemy.MoveDirection * moveSpeed * Time.deltaTime;
            }
            else
            {
                float moveSpeed = enemy.MovementSpeed * enemy.SpeedMultRT;
                enemy.transform.position += enemy.MoveDirection * moveSpeed * Time.deltaTime;
            }
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
            float atkSpd = Mathf.Max(0.001f, enemy.Info.AttackSpeed);
            if (enemy.TimeSinceLastAttack < 1f / atkSpd)
                return;

            // Kamikaze is a SKILL (handled via skill event system, not attack event)
            if (enemy.Info.KamikazeOnReach)
            {
                SetPendingKamikaze(enemy);
                enemy.TriggerAnimSkill();                
                enemy.TimeSinceLastAttack = 0f;
                return;
            }

            // Compute target slots NOW (no allocations)
            int mainSlot = -1;
            int id = enemy.GetInstanceID();

            if (enemy.IsBossInstance)
            {
                bool needLock = !_bossLockedSlot.TryGetValue(id, out mainSlot);

                if (!needLock)
                {
                    if (!GunnerManager.Instance.ApplyDamageOnSlot(mainSlot, 0f))
                        needLock = true;
                }

                if (needLock)
                {
                    mainSlot = 2;
                    bool midAlive = GunnerManager.Instance.ApplyDamageOnSlot(2, 0f);
                    if (!midAlive)
                        mainSlot = GunnerManager.Instance.GetNearestAliveSlotByX(enemy.transform.position.x);

                    _bossLockedSlot[id] = mainSlot;
                }
            }
            else
            {
                mainSlot = GunnerManager.Instance.GetNearestAliveSlotByX(enemy.transform.position.x);
            }

            int slotA = mainSlot;
            int slotB = -1;

            int sweepTargets = Mathf.Clamp(enemy.Info.SweepTargets, 1, 5);
            if (sweepTargets > 1 && mainSlot >= 0)
            {
                if (mainSlot == 0) slotB = 1;
                else if (mainSlot == 4) slotB = 3;
                else
                {
                    // mirror your previous deterministic preference
                    int neighbor =
                        (mainSlot <= 1) ? (mainSlot - 1) :
                        (mainSlot >= 3) ? (mainSlot + 1) :
                                          (mainSlot + 1);
                    slotB = Mathf.Clamp(neighbor, 0, 4);
                }
            }

            // Store pending damage; animation event will apply it
            _pendingAttacks[enemy.GetInstanceID()] = new PendingAttack
            {
                SlotA = slotA,
                SlotB = slotB,
                Damage = enemy.Info.Damage * enemy.DamageMultRT
            };

            enemy.AnimateAttack();

            // Fallback if the attack clip event isn't wired
            StartCoroutine(ExecutePendingAttackFallback(enemy, 0.06f));

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

        // Animator events
        public void SetPendingHeal(Enemy enemy, float healAmount)
        {
            if (enemy == null) return;
            _pendingSkills[enemy.GetInstanceID()] = new PendingSkill
            {
                Type = PendingSkillType.Heal,
                HealAmount = healAmount,
                SummonCount = 0
            };
        }

        public void SetPendingSummon(Enemy enemy, int count)
        {
            if (enemy == null) return;
            _pendingSkills[enemy.GetInstanceID()] = new PendingSkill
            {
                Type = PendingSkillType.Summon,
                HealAmount = 0f,
                SummonCount = count
            };
        }

        public void SetPendingKamikaze(Enemy enemy)
        {
            if (enemy == null) return;
            _pendingSkills[enemy.GetInstanceID()] = new PendingSkill
            {
                Type = PendingSkillType.Kamikaze,
                HealAmount = 0f,
                SummonCount = 0
            };
        }

        public void ClearPendingSkill(Enemy enemy)
        {
            if (enemy == null) return;
            _pendingSkills.Remove(enemy.GetInstanceID());
        }

        public void ExecutePendingSkill(Enemy enemy)
        {
            if (enemy == null) return;

            int id = enemy.GetInstanceID();
            if (!_pendingSkills.TryGetValue(id, out PendingSkill pending))
                return;

            // Consume first to avoid double-fire if multiple events happen.
            _pendingSkills.Remove(id);

            switch (pending.Type)
            {
                case PendingSkillType.Heal:
                    ExecuteHealNow(enemy, pending.HealAmount);
                    break;

                case PendingSkillType.Summon:
                    ExecuteSummonNow(enemy, pending.SummonCount);
                    break;

                case PendingSkillType.Kamikaze:
                    ExecuteKamikazeNow(enemy);
                    break;

                case PendingSkillType.BossSkill:
                    ExecuteBossSkillNow(enemy, pending);
                    break;
            }

        }

        private void ExecuteHealNow(Enemy healer, float healAmount)
        {
            float radius = Mathf.Max(0f, healer.Info.HealerRadius);
            int maxTargets = Mathf.Max(1, healer.Info.HealerMaxTargets);
            float healPct = Mathf.Clamp01(healer.Info.HealerHealPctOfMaxHP);
            if (radius <= 0f || healPct <= 0f) return;

            // Broad phase: your grid query (same as boss explosion approach)
            var nearby = GridManager.Instance.GetEnemiesInRange(healer.transform.position, Mathf.CeilToInt(radius));
            if (nearby == null || nearby.Count == 0) return; // nothing nearby
                                                             // (Your GridManager API is already used for boss explosion and traps.) :contentReference[oaicite:5]{index=5}

            // Narrow phase: pick up to K nearest by squared distance (no allocations).
            float r2 = radius * radius;

            // Fixed small buffers (K <= 5 typically)
            Enemy a0 = null, a1 = null, a2 = null, a3 = null, a4 = null;
            float d0 = float.MaxValue, d1 = float.MaxValue, d2 = float.MaxValue, d3 = float.MaxValue, d4 = float.MaxValue;

            Vector3 epos = healer.transform.position;

            for (int i = 0; i < nearby.Count; i++)
            {
                var cand = nearby[i];
                if (cand == null || cand == healer || !cand.IsAlive) continue;

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

        private void ExecuteSummonNow(Enemy summoner, int count)
        {
            // Call the exact existing summon logic you already use (spawn count, prefab, etc).
            // If your Enemy.DoSummon() already encapsulates everything, just call it:
            summoner.DoSummon();
        }

        private void ExecuteKamikazeNow(Enemy enemy)
        {
            // Call your existing kamikaze behavior:
            enemy.TriggerKamikazeExplosion();
        }

        private void ExecuteBossSkillNow(Enemy boss, PendingSkill pending)
        {
            if (boss == null || !boss.IsAlive) return;

            switch (pending.BossSkill)
            {
                case BossSkillId.ShieldGain:
                    boss.AddShieldChargesRT(pending.ParamI);
                    break;

                case BossSkillId.HealPulse:
                    // Reuse existing healer execution for “heal allies” pulse
                    ExecuteHealNow(boss, 0f);
                    break;

                case BossSkillId.SummonWave:
                    boss.DoSummon();
                    break;

                case BossSkillId.BuffSelf:
                    // ParamA: armor delta, ParamB: speed delta, ParamI: time
                    boss.ApplyTimedBuff(
                        armorDelta: pending.ParamA,
                        dodgeDelta: 0f,
                        speedMult: pending.ParamB,
                        damageMult: 0f,
                        durationSeconds: Mathf.Max(0.01f, pending.ParamI)
                    );
                    break;

                case BossSkillId.JumpDepth:
                    // ParamA: depth delta (z)
                    boss.LockMovement(0.25f);
                    boss.JumpDepth(pending.ParamA);
                    break;

                case BossSkillId.SpecialGunnerHit:
                    // ParamA: damage, ParamI: targets count, ParamB: radius
                    boss.SpecialHitGunners(pending.ParamA, pending.ParamB, pending.ParamI);
                    break;

                case BossSkillId.DeathExplosionMode:
                    boss.SetBossDeathExplosionMode((BossDeathExplosionMode)pending.ParamI);
                    break;
            }
        }

        public void ClearPendingAttack(Enemy enemy)
        {
            if (enemy == null) return;
            _pendingAttacks.Remove(enemy.GetInstanceID());
        }

        public void ExecutePendingAttack(Enemy enemy)
        {
            if (enemy == null) return;

            int id = enemy.GetInstanceID();
            if (!_pendingAttacks.TryGetValue(id, out PendingAttack pending))
                return;

            _pendingAttacks.Remove(id);

            if (pending.SlotA >= 0)
                GunnerManager.Instance.ApplyDamageOnSlot(pending.SlotA, pending.Damage);
            if (pending.SlotB >= 0)
                GunnerManager.Instance.ApplyDamageOnSlot(pending.SlotB, pending.Damage);

            // Notify boss brain (only if present)
            BossBrain brain = enemy.GetComponent<BossBrain>();
            if (brain != null) brain.NotifyAttackExecuted();
        }

        private IEnumerator ExecutePendingAttackFallback(Enemy enemy, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (enemy == null || !enemy.IsAlive) yield break;
            ExecutePendingAttack(enemy);
        }
    }
}