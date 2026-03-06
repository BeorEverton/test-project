using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.UI;
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

            //Debug.Log($"EnemyManager: Setting pending boss skill {skill} for {enemy.name} with params a={a}, b={b}, i={i}.");
            _pendingSkills[enemy.GetInstanceID()] = new PendingSkill
            {
                Type = PendingSkillType.BossSkill,
                HealAmount = 0f,
                SummonCount = 0,
                BossSkill = skill,
                ParamA = a,
                ParamB = b,
                ParamI = i,

            };
        }

        public bool HasPendingSkill(Enemy enemy)
        {
            if (enemy == null) return false;

            int key = enemy.GetInstanceID();
            if (!_pendingSkills.TryGetValue(key, out PendingSkill p)) return false;

            return p.Type == PendingSkillType.BossSkill || p.Type == PendingSkillType.Heal || p.Type == PendingSkillType.Summon;
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
                    // Boss resistance WITHOUT mutating stored knockback every frame
                    float bossMult = enemyComponent.IsBossInstance ? 0.5f : 1f;

                    float stepMag = Mathf.Abs(enemyComponent.KnockbackVelocity.y) * bossMult * Time.deltaTime;
                    Vector3 p = enemyComponent.transform.position;

                    // Choose the Z direction that INCREASES Depth()
                    float depthPlus = new Vector3(p.x, p.y, p.z + stepMag).Depth();
                    float depthMinus = new Vector3(p.x, p.y, p.z - stepMag).Depth();

                    float signedStepZ = (depthPlus > depthMinus) ? (+stepMag) : (-stepMag);
                    p.z += signedStepZ;

                    // IMPORTANT: keep steering while knocked back (X-only), so they still converge to center.
                    float prog = DepthProgress(p.Depth());
                    if (prog >= steerStartProgress)
                    {
                        int slot = GunnerManager.Instance.GetNearestAliveSlotByX(p.x);
                        if (slot >= 0)
                        {
                            float t = Mathf.InverseLerp(steerStartProgress, steerFullProgress, prog);
                            float weight = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

                            float targetX = GunnerManager.Instance.GetSlotAnchorX(slot);
                            targetX += StableOffsetForEnemy(enemyComponent, 1.3f);

                            float stepX = Mathf.Max(0.001f, enemyComponent.MovementSpeed * lateralSpeedMultiplier * weight) * Time.deltaTime;
                            p.x = Mathf.MoveTowards(p.x, targetX, stepX);
                        }
                    }

                    enemyComponent.transform.position = p;
                    enemyComponent.SetAnimIdle(false);

                    enemyComponent.KnockbackTime -= Time.deltaTime;
                    continue; // Still skip normal movement/attack while being pushed
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
                                trap.Trigger(enemyComponent);
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

            // Final safety clamp into the funnel
            var pFinal = enemy.transform.position;
            float progFinal = DepthProgress(pFinal.Depth());
            float halfFinal = FunnelHalfWidth(progFinal);
            if (pFinal.x < -halfFinal) pFinal.x = -halfFinal;
            else if (pFinal.x > halfFinal) pFinal.x = halfFinal;
            enemy.transform.position = pFinal;
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
                    Mathf.CeilToInt(trap.radius),
                    trap.ownerTurret.RuntimeStats.CanHitFlying);
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

            if (enemy.IsBossInstance)
            {
                BossBrain brain = enemy.GetComponent<BossBrain>();
                if (brain != null && brain.BlocksBasicAttack)
                    return;

                if (HasPendingSkill(enemy))
                    return;
            }

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

            //Debug.Log($"EnemyManager: Attempting to execute pending skill for {enemy.name}.");

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
            var nearby = GridManager.Instance.GetEnemiesInRange(healer.transform.position, Mathf.CeilToInt(radius), true);
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
            Debug.Log("Calling boss skillnow with " + pending.BossSkill);
            Debug.Log("Boss null or dead? " + boss + boss.IsAlive);
            if (boss == null || !boss.IsAlive)
                return;

            switch (pending.BossSkill)
            {
                case BossSkillId.ShieldGain:
                    Debug.Log("Getting shield");
                    boss.AddShieldChargesRT(pending.ParamI);
                    break;

                case BossSkillId.HealPulse:
                    // ParamA = heal pct of max HP per target (0.10 = 10%)
                    // ParamB = radius
                    // ParamI = max targets
                    ExecuteHealPulseNow(boss, pending.ParamA, pending.ParamB, pending.ParamI);
                    break;

                case BossSkillId.SummonWave:
                    boss.DoSummon();
                    break;

                case BossSkillId.BuffSelf:
                    // ParamA = armor bonus as pct of base armor (0.25 = +25% of boss.Info.Armor)
                    // ParamB = speed multiplier (1.25 = 125% speed)
                    // ParamI = duration seconds
                    boss.ApplyTimedBuff(
                        speedMult: (pending.ParamB <= 0f) ? 1f : pending.ParamB,
                        damageMult: 1f,
                        armorDelta: ResolveBossArmorDeltaFromPct(boss, pending.ParamA),
                        dodgeDelta: 0f,
                        durationSeconds: Mathf.Max(0.01f, pending.ParamI)
                    );
                    break;

                case BossSkillId.DebuffSelf:
                    break;

                case BossSkillId.JumpDepth:
                    boss.LockMovement(0.25f);
                    boss.JumpDepth(pending.ParamA);
                    break;

                case BossSkillId.SpecialGunnerHit:
                    {
                        // ParamA = multiplier of regular attack damage (1.5 = 150%)
                        // ParamB = radius
                        // ParamI = max targets
                        float finalDamage = ResolveBossAttackDamageFromMultiplier(boss, pending.ParamA);
                        boss.SpecialHitGunners(finalDamage, pending.ParamB, pending.ParamI);
                        break;
                    }

                case BossSkillId.DeathExplosionMode:
                    boss.SetBossDeathExplosionMode((BossDeathExplosionMode)pending.ParamI);
                    break;
            }
        }
        private float ResolveBossAttackDamageFromMultiplier(Enemy boss, float attackDamageMultiplier)
        {
            if (boss == null || boss.Info == null)
                return 0f;

            float mult = Mathf.Max(0f, attackDamageMultiplier);
            return Mathf.Max(0f, boss.Info.Damage * boss.DamageMultRT * mult);
        }

        private float ResolveBossArmorDeltaFromPct(Enemy boss, float armorPctOfBase)
        {
            if (boss == null || boss.Info == null)
                return 0f;

            float pct = Mathf.Max(0f, armorPctOfBase);
            return Mathf.Max(0f, boss.Info.Armor * pct);
        }

        private void ExecuteHealPulseNow(Enemy healer, float healPctOfMaxHp, float radius, int maxTargets)
        {
            if (healer == null || !healer.IsAlive)
                return;

            float finalRadius = Mathf.Max(0f, radius);
            int finalMaxTargets = Mathf.Clamp(maxTargets, 1, 5);
            float finalHealPct = Mathf.Clamp01(healPctOfMaxHp);

            if (finalRadius <= 0f || finalHealPct <= 0f)
                return;

            var nearby = GridManager.Instance.GetEnemiesInRange(healer.transform.position, Mathf.CeilToInt(finalRadius), true);
            if (nearby == null || nearby.Count == 0)
                return;

            float r2 = finalRadius * finalRadius;

            Enemy a0 = null, a1 = null, a2 = null, a3 = null, a4 = null;
            float d0 = float.MaxValue, d1 = float.MaxValue, d2 = float.MaxValue, d3 = float.MaxValue, d4 = float.MaxValue;

            Vector3 epos = healer.transform.position;

            for (int i = 0; i < nearby.Count; i++)
            {
                Enemy cand = nearby[i];
                if (cand == null || cand == healer || !cand.IsAlive)
                    continue;

                Vector3 cpos = cand.transform.position;
                float dx = cpos.x - epos.x;
                float dz = cpos.z - epos.z;
                float d2s = dx * dx + dz * dz;
                if (d2s > r2)
                    continue;

                if (d2s < d0) { a4 = a3; d4 = d3; a3 = a2; d3 = d2; a2 = a1; d2 = d1; a1 = a0; d1 = d0; a0 = cand; d0 = d2s; }
                else if (d2s < d1) { a4 = a3; d4 = d3; a3 = a2; d3 = d2; a2 = a1; d2 = d1; a1 = cand; d1 = d2s; }
                else if (d2s < d2) { a4 = a3; d4 = d3; a3 = a2; d3 = d2; a2 = cand; d2 = d2s; }
                else if (d2s < d3) { a4 = a3; d4 = d3; a3 = cand; d3 = d2s; }
                else if (d2s < d4) { a4 = cand; d4 = d2s; }
            }

            int healed = 0;
            if (a0 != null && healed < finalMaxTargets) { a0.Heal(a0.MaxHealth * finalHealPct); healed++; }
            if (a1 != null && healed < finalMaxTargets) { a1.Heal(a1.MaxHealth * finalHealPct); healed++; }
            if (a2 != null && healed < finalMaxTargets) { a2.Heal(a2.MaxHealth * finalHealPct); healed++; }
            if (a3 != null && healed < finalMaxTargets) { a3.Heal(a3.MaxHealth * finalHealPct); healed++; }
            if (a4 != null && healed < finalMaxTargets) { a4.Heal(a4.MaxHealth * finalHealPct); healed++; }
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
            //Debug.Log($"EnemyManager: Executed attack from {enemy.name} on slots {pending.SlotA} and {pending.SlotB} for {pending.Damage} damage. BossBrain present: {brain != null}");
            if (brain != null) brain.NotifyAttackExecuted();
        }

        // Used to prevent enemies steering away from center
        private static float FunnelHalfWidth(float prog01)
        {
            // prog01: 0 at spawn, 1 at base
            // At horizon: +/-spawnXSpread, at base: +/-BaseXArea
            return Mathf.Lerp(EnemyConfig.spawnXSpread, EnemyConfig.BaseXArea, Mathf.Clamp01(prog01));
        }
    }
}