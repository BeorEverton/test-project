// Assets/Editor/Simulation/SimulationEngine.cs
// ------------------------------------------------
// Main head-less simulator – per-wave stats export
// ------------------------------------------------
// for reference: [^\x00-\x7F]+

/*
namespace IdleDefense.Editor.Simulation
{
    // ---------------------------------------------
    // Minimal per-enemy runtime struct
    // ---------------------------------------------
    struct EnemySim
    {
        public EnemyClass Class;
        public float Hp;
        public float Y;
        public float Speed;
        public float Damage;
        public ulong Coin;
        public bool IsBoss;
        public float TimeSinceLastAttack;
        public float AttackRange;
    }

    // ---------------------------------------------
    // Main engine
    // ---------------------------------------------
    public static class SimulationEngine
    {
        // ------------------------------
        // Run one simulation instance
        // ------------------------------
        public static SimStats Run(
            float minutes,
            float clicksPerSec,
            SpendingMode mode)
        {
            //----------------------------------------------------------
            // 0) Load all SO data needed for the sim
            //----------------------------------------------------------
            var enemyInfos = AssetDatabase
                .FindAssets("t:EnemyInfoSO")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(p => AssetDatabase.LoadAssetAtPath<EnemyInfoSO>(p))
                .ToDictionary(e => e.EnemyClass, e => e);

            var turretInfos = AssetDatabase
                .FindAssets("t:TurretInfoSO")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(p => AssetDatabase.LoadAssetAtPath<TurretInfoSO>(p))
                .ToDictionary(t => t.TurretType, t => new TurretBlueprint(t));

            var waves = AssetDatabase
                .FindAssets("t:WaveConfigSO")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(p => AssetDatabase.LoadAssetAtPath<WaveConfigSO>(p))
                .OrderBy(w => w.WaveStartIndex)
                .ToList();

            var unlockTable = AssetDatabase
                .FindAssets("t:TurretUnlockTableSO")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(p => AssetDatabase.LoadAssetAtPath<TurretUnlockTableSO>(p))
                .First();

            var baseSO = AssetDatabase
                .FindAssets("t:PlayerBaseSO")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(p => AssetDatabase.LoadAssetAtPath<PlayerBaseSO>(p))
                .First();

            //----------------------------------------------------------
            // 1) Helper lambdas
            //----------------------------------------------------------
            float MinBaseRegenInterval = 0.5f; // hard floor (same as runtime)

            float CostForBase(PlayerUpgradeType t, int level)
            {
                float cost = float.MaxValue; // Standardize to max value
                if (t == PlayerUpgradeType.MaxHealth)
                {

                    cost = baseSO.MaxHealthUpgradeBaseCost * Mathf.Pow(1.1f, level);
                }
                else if (t == PlayerUpgradeType.RegenAmount)
                {
                    cost = baseSO.RegenAmountUpgradeBaseCost * Mathf.Pow(1.1f, level);
                }
                else if (t == PlayerUpgradeType.RegenInterval)
                {
                    // ensure the interval does not go below the minimum
                    cost = baseSO.RegenIntervalUpgradeBaseCost * Mathf.Pow(1.1f, level);
                    if (baseSO.RegenInterval - baseSO.RegenIntervalUpgradeAmount * level < MinBaseRegenInterval)
                        return float.MaxValue; // too expensive to upgrade below the min interval
                }

                return cost;

            }

            float GetHybridCost(float baseCost, int level)
            {
                const int hybridThreshold = 50;
                const float quadraticFactor = 0.1f;
                const float exponentialPower = 1.15f;

                if (level < hybridThreshold)
                    return baseCost * (1f + level * level * quadraticFactor);
                else
                    return baseCost * Mathf.Pow(exponentialPower, level);
            }

            float GetExponentialCost(float baseCost, int level, float multiplier)
            {
                return baseCost * Mathf.Pow(multiplier, level);
            }

            float GetExponentialCostPlusLevel(float baseCost, int level, float multiplier)
            {
                // matches TurretUpgradeManager.UpgradeDamage, FireRate
                return baseCost + Mathf.Pow(multiplier, level) + level;
            }


            //----------------------------------------------------------
            // 2) Global sim state / stats
            //----------------------------------------------------------
            var stats = new SimStats { Waves = new List<WaveStat>() };

            var strategy = SpendingStrategyFactory.Create(mode);

            float simTime = 0f;
            float dt = 0.05f;

            ulong coins = 0;
            int waveIndex = 1;

            // base health / regen
            float baseMaxHealth = baseSO.MaxHealth;
            float baseHealth = baseMaxHealth;
            float regenAmount = baseSO.RegenAmount;
            float regenInterval = baseSO.RegenInterval;
            float regenTickTimer = 0f;
            int maxHpLvl = 0, regenAmtLvl = 0, regenIntLvl = 0;
            bool previousWaveFailed = false;

            // click speed bonus
            float clickBonus = 0f;
            float clicksThisWave = 0f;
            // discrete click simulation state
            float nextClickTimer = 0f;
            float bonusDelayTimer = 0f;

            const float bonusPerClick = 1f, decayPerSec = 1f;

            // slot / turret state
            var slots = new List<TurretBlueprint>();
            var shotTimers = new List<float>();
            var turretCounts = new Dictionary<TurretType, int>();
            Dictionary<int, int> laserTargetIndices = new();
            Dictionary<int, float> laserTargetTimers = new();


            // init counts to zero
            foreach (var e in unlockTable.Entries)
                turretCounts[e.Type] = 0;

            // seed your starter turret(s) in slot #1 (wave 1)
            foreach (var e in unlockTable.Entries)
                if (e.WaveToUnlock <= 1 && turretInfos.ContainsKey(e.Type))
                {
                    slots.Add(turretInfos[e.Type]);
                    shotTimers.Add(0f);
                    turretCounts[e.Type]++;
                }

            // slot-purchase thresholds (wave, cost)
            var slotWaves = new[] { 1, 20, 50, 120, 300 };
            var slotCosts = new ulong[] { 0, 5000, 20000, 50000, 250000 };
            int nextSlot = 1;


            // damage by turret type (per-wave)
            double[] perWaveDmg = new double[
                Enum.GetValues(typeof(TurretType)).Length];

            // wave-local accumulators
            WaveStat wStat = new WaveStat();

            // enemy collections
            var pendingSpawns = new Queue<EnemySim>();
            var live = new List<EnemySim>();

            float spawnTimer = 0f;
            float spawnIntervalCurrent = 0f;
            bool bossIncoming = false;
            EnemySim bossTemplate = default;

            // view-port spawn Y (match runtime)
            float spawnY = Camera.main.ViewportToWorldPoint(
                new Vector3(0.5f, 1f, 0f)).y + 1.0f;

            //----------------------------------------------------------
            // 3) Wave initialiser
            //----------------------------------------------------------
            void InitWave()
            {
                var tpl = waves.Last(w => w.WaveStartIndex <= waveIndex);

                if (nextSlot < slotWaves.Length && waveIndex >= slotWaves[nextSlot])
                {
                    if (coins >= slotCosts[nextSlot])
                    {
                        coins -= slotCosts[nextSlot];
                        nextSlot++;
                        Debug.Log($"[Simulation] Wave {waveIndex}: Bought slot #{nextSlot} for {slotCosts[nextSlot - 1]} coins.");
                    }
                }

                // -- buy turrets into any empty slots --
                int emptySlots = nextSlot - slots.Count;
                if (emptySlots > 0)
                {
                    Debug.Log($"[Simulation] Wave {waveIndex}: Buying {emptySlots} turrets with {coins} coins.");
                    // choose cheapest unlocked type you can afford this wave
                    foreach (var entry in unlockTable.Entries
                      .Where(u => u.WaveToUnlock <= waveIndex)
                      .OrderByDescending(u => u.FirstCopyCost))
                    {
                        var type = entry.Type;
                        int owned = turretCounts[type];
                        // cost doubles each copy: FirstCopyCost * 2^(owned-1)
                        ulong cost = owned == 0
                                      ? entry.FirstCopyCost
                                      : entry.FirstCopyCost * (ulong)(1 << (owned - 1));

                        if (cost > 0
                            && cost <= wStat.MoneyEarned
                            && coins >= cost)
                        {
                            coins -= cost;
                            slots.Add(turretInfos[type]);
                            shotTimers.Add(0f);
                            turretCounts[type]++;
                            emptySlots--;
                            Debug.Log($"[Simulation] Wave {waveIndex}: Bought turret {type} for {cost} coins.");
                            if (emptySlots <= 0)
                                break;
                        }
                    }
                }

                //----------------------------------------------------------------
                // spawn queue for every EnemyWaveEntry
                //----------------------------------------------------------------
                pendingSpawns.Clear();
                foreach (var e in tpl.EnemyWaveEntries)
                {
                    var info = enemyInfos[e.EnemyPrefab
                        .GetComponent<Enemy>().Info.EnemyClass];

                    float hp = info.MaxHealth *
                                 Mathf.Pow(info.HealthMultiplierByWaveCount, waveIndex) +
                                 waveIndex;

                    float spd = Random.Range(
                                    info.MovementSpeed - info.MovementSpeedDifference,
                                    info.MovementSpeed + info.MovementSpeedDifference) +
                                 Mathf.Floor((waveIndex - 1) / 5f);

                    float dmg = info.Damage +
                                 info.Damage * ((waveIndex / 100f) * 2f);

                    ulong coin = (ulong)(
                        info.CoinDropAmount *
                        info.CoinDropMultiplierByWaveCount +
                        waveIndex * info.CoinDropMultiplierByWaveCount);

                    int count = e.NumberOfEnemies + waveIndex;
                    for (int k = 0; k < count; k++)
                        pendingSpawns.Enqueue(new EnemySim
                        {
                            Class = info.EnemyClass,
                            Hp = hp,
                            Y = spawnY,
                            Speed = spd,
                            Damage = dmg,
                            Coin = coin,
                            IsBoss = false,
                            TimeSinceLastAttack = 0f,
                            AttackRange = info.AttackRange
                        });
                    //Debug.Log($"[Simulation] Wave {waveIndex}: Spawning {count}x {info.Name} (HP: {hp}, Speed: {spd}, Dmg: {dmg}, Coin: {coin})");

                }

                //----------------------------------------------------------------
                // boss / miniboss template (every 5 / 10 waves)
                //----------------------------------------------------------------
                bossIncoming = false;
                if (waveIndex % 5 == 0)
                {
                    var info = enemyInfos
                        [tpl.EnemyWaveEntries[0]
                          .EnemyPrefab.GetComponent<Enemy>().Info.EnemyClass];

                    var clone = ScriptableObject.Instantiate(info);
                    if (waveIndex % 10 == 0)
                        clone.AttackRange += 0.6f;
                    else
                        clone.AttackRange += 0.2f;

                    float hpMult = (waveIndex % 10 == 0) ? 100f : 30f;
                    float dmgMult = (waveIndex % 10 == 0) ? 40f : 20f;
                    ulong coinMul = (waveIndex % 10 == 0) ? 40u : 20u;

                    bossTemplate = new EnemySim
                    {
                        Class = clone.EnemyClass,
                        Hp = pendingSpawns.Peek().Hp * hpMult,
                        Y = spawnY,
                        Speed = pendingSpawns.Peek().Speed,
                        Damage = pendingSpawns.Peek().Damage * dmgMult,
                        Coin = pendingSpawns.Peek().Coin * coinMul,
                        IsBoss = true,
                        AttackRange = clone.AttackRange
                    };
                    bossIncoming = true;
                }

                spawnIntervalCurrent = tpl.TimeBetweenSpawns;
                spawnTimer = spawnIntervalCurrent;

                // reset wave accumulators
                clicksThisWave = 0f;

                wStat = new WaveStat
                {
                    Wave = waveIndex,
                    HealthStart = baseHealth,
                    MaxHealthLevel = maxHpLvl,
                    RegenAmountLevel = regenAmtLvl,
                    RegenIntervalLevel = regenIntLvl,
                    CurrentMaxHealth = baseMaxHealth,
                    CurrentRegenAmount = regenAmount,
                    CurrentRegenInterval = regenInterval
                };

                wStat.EnemiesSpawned = pendingSpawns.Count; // Add to export the amount of enemies
                Array.Clear(perWaveDmg, 0, perWaveDmg.Length);
            }

            //----------------------------------------------------------
            // 4) Simulate!
            //----------------------------------------------------------
            InitWave();
            while (simTime < minutes * 60f)
            {
            NEXT_FRAME:

                //------------------ enemy spawning --------------------
                spawnTimer -= dt;
                if (spawnTimer <= 0f)
                {
                    if (pendingSpawns.Count > 0)
                        live.Add(pendingSpawns.Dequeue());
                    else if (bossIncoming)
                    {
                        live.Add(bossTemplate);
                        bossIncoming = false;
                    }
                    spawnTimer = spawnIntervalCurrent;

                }


                // ------------------ click bonus (spdBonus) -----------------------
                const float initialBoost = 5f;
                const float decayDelay = 1f;
                const float decayRate = 5f * 0.8f;  // matches holdRate*0.8

                if (clicksPerSec > 0f)
                {
                    // count down to the next discrete click
                    nextClickTimer -= dt;
                    if (nextClickTimer <= 0f)
                    {
                        clickBonus += initialBoost;
                        // reset decay timer on every click
                        bonusDelayTimer = 0f;
                        // schedule next click
                        nextClickTimer += 1f / clicksPerSec;
                    }
                    else
                    {
                        // between clicks: advance decay timer
                        bonusDelayTimer += dt;
                    }
                }
                else
                {
                    // no clicking: no bonus
                    clickBonus = 0f;
                }

                // after holding period, let it decay each frame
                if (bonusDelayTimer >= decayDelay)
                    clickBonus = Mathf.Max(0f, clickBonus - decayRate * dt);

                // enforce in game cap
                clickBonus = Mathf.Clamp(clickBonus, 0f, 100f);
                // ---------------------------------------------------------------

                // record the raw spdBonus value for export
                wStat.SpeedBoostClicks = clickBonus;

                // ENEMY MOVEMENT
                for (int i = 0; i < live.Count; i++) // Iterate carefully if removals happen elsewhere
                {
                    var e = live[i];
                    e.Y -= e.Speed * dt;
                    live[i] = e; // Update position in the list
                }

                // TURRET FIRING
                for (int t = 0; t < slots.Count; t++)
                {
                    var bp = slots[t];
                    shotTimers[t] += dt;
                    // Corrected click bonus application for fire rate:
                    float effectiveFireRate = bp.FireRate * (1f + clickBonus / 100f); // Assuming clickBonus is 0-100
                    float interval = 1f / effectiveFireRate;

                    if (shotTimers[t] >= interval)
                    {
                        shotTimers[t] -= interval; // Reset or subtract cooldown

                        // Targeting logic (e.g., find first enemy in range)
                        int targetEnemyIndex = -1;
                        for (int enemyIdx = 0; enemyIdx < live.Count; enemyIdx++)
                        {
                            if (live[enemyIdx].Y <= bp.Range) // Assuming bp.Range is a Y-coordinate threshold
                            {
                                targetEnemyIndex = enemyIdx;
                                break; // Found first target
                            }
                        }

                        if (targetEnemyIndex != -1)
                        {
                            // --- Sniper ---
                            if (bp.Type == TurretType.Sniper)
                            {
                                // multi-enemy line pierce logic
                                float pierceChance = bp.PierceChance * 0.01f;
                                float damageFalloff = bp.PierceDamageFalloff * 0.01f;
                                float dmg = bp.Damage * (Random.value < bp.CritChance * 0.01f ? 1f + bp.CritDamageMultiplier * 0.01f : 1f);

                                for (int i = 0; i < live.Count && dmg > 0.1f; i++)
                                {
                                    var e = live[i];
                                    if (e.Y <= bp.Range)
                                    {
                                        var enemy = live[i];
                                        enemy.Hp -= dmg;
                                        live[i] = enemy;

                                        wStat.DamageDealt += dmg;
                                        stats.TotalDamageDealt += dmg;
                                        perWaveDmg[(int)bp.Type] += dmg;

                                        if (live[i].Hp <= 0f)
                                        {
                                            coins += e.Coin;
                                            wStat.MoneyEarned += e.Coin;
                                            if (e.IsBoss)
                                            { wStat.BossesKilled++; stats.BossesKilled++; }
                                            else
                                            { wStat.EnemiesKilled++; stats.EnemiesKilled++; }
                                            live.RemoveAt(i);
                                            i--;
                                        }

                                        dmg *= damageFalloff;
                                        if (Random.value > pierceChance)
                                            break;
                                    }
                                }
                            }

                            // --- Missile ---
                            else if (bp.Type == TurretType.MissileLauncher)
                            {
                                var center = live[targetEnemyIndex];
                                float radius = bp.ExplosionRadius;
                                float splashRadius = radius / 3f;

                                for (int i = live.Count - 1; i >= 0; i--)
                                {
                                    var e = live[i];
                                    float dist = Mathf.Abs(e.Y - center.Y);
                                    if (dist > radius)
                                        continue;

                                    float appliedDmg = dist <= splashRadius ? bp.Damage : bp.SplashDamage;
                                    var enemy = live[i];
                                    enemy.Hp -= appliedDmg;
                                    live[i] = enemy;

                                    wStat.DamageDealt += appliedDmg;
                                    stats.TotalDamageDealt += appliedDmg;
                                    perWaveDmg[(int)bp.Type] += appliedDmg;

                                    if (live[i].Hp <= 0f)
                                    {
                                        coins += e.Coin;
                                        wStat.MoneyEarned += e.Coin;
                                        if (e.IsBoss)
                                        { wStat.BossesKilled++; stats.BossesKilled++; }
                                        else
                                        { wStat.EnemiesKilled++; stats.EnemiesKilled++; }
                                        live.RemoveAt(i);
                                    }
                                }
                            }

                            // --- Laser ---
                            else if (bp.Type == TurretType.Laser)
                            {
                                if (!laserTargetIndices.ContainsKey(t))
                                    laserTargetIndices[t] = targetEnemyIndex;
                                if (laserTargetIndices[t] == targetEnemyIndex)
                                    laserTargetTimers[t] += dt;
                                else
                                {
                                    laserTargetIndices[t] = targetEnemyIndex;
                                    laserTargetTimers[t] = 0f;
                                }

                                float rampBonus = bp.Damage * (bp.PercentBonusDamagePerSec / 100f) * laserTargetTimers[t];
                                float totalDmg = bp.Damage + rampBonus;

                                var e = live[targetEnemyIndex];

                                e.Hp -= totalDmg;
                                e.Speed *= 1f - Mathf.Clamp01(bp.SlowEffect);

                                wStat.DamageDealt += totalDmg;
                                stats.TotalDamageDealt += totalDmg;
                                perWaveDmg[(int)bp.Type] += totalDmg;

                                if (e.Hp <= 0f)
                                {
                                    coins += e.Coin;
                                    wStat.MoneyEarned += e.Coin;
                                    if (e.IsBoss)
                                    { wStat.BossesKilled++; stats.BossesKilled++; }
                                    else
                                    { wStat.EnemiesKilled++; stats.EnemiesKilled++; }
                                    live.RemoveAt(targetEnemyIndex);
                                }
                                else
                                {
                                    live[targetEnemyIndex] = e; // reassign the modified struct
                                }

                            }

                            // --- Shotgun ---
                            else if (bp.Type == TurretType.Shotgun)
                            {
                                var e = live[targetEnemyIndex];
                                bool crit = Random.value < bp.CritChance * 0.01f;
                                float critMultiplier = 1f + bp.CritDamageMultiplier * 0.01f;
                                float pelletDmg = bp.Damage * (crit ? critMultiplier : 1f);
                                pelletDmg *= bp.PelletCount;

                                e.Hp -= pelletDmg;
                                wStat.DamageDealt += pelletDmg;
                                stats.TotalDamageDealt += pelletDmg;
                                perWaveDmg[(int)bp.Type] += pelletDmg;

                                e.Y += bp.KnockbackStrength * 0.5f;
                                live[targetEnemyIndex] = e;

                                if (e.Hp <= 0f)
                                {
                                    coins += e.Coin;
                                    wStat.MoneyEarned += e.Coin;
                                    if (e.IsBoss)
                                    { wStat.BossesKilled++; stats.BossesKilled++; }
                                    else
                                    { wStat.EnemiesKilled++; stats.EnemiesKilled++; }
                                    live.RemoveAt(targetEnemyIndex);
                                }
                            }

                            // --- Default (MachineGun) ---
                            else
                            {
                                var e = live[targetEnemyIndex];
                                bool crit = Random.value < bp.CritChance * 0.01f;
                                float critMultiplier = 1f + bp.CritDamageMultiplier * 0.01f;
                                float dmg = bp.Damage * (crit ? critMultiplier : 1f);

                                e.Hp -= dmg;
                                wStat.DamageDealt += dmg;
                                stats.TotalDamageDealt += dmg;
                                perWaveDmg[(int)bp.Type] += dmg;

                                if (e.Hp <= 0f)
                                {
                                    coins += e.Coin;
                                    wStat.MoneyEarned += e.Coin;
                                    if (e.IsBoss)
                                    { wStat.BossesKilled++; stats.BossesKilled++; }
                                    else
                                    { wStat.EnemiesKilled++; stats.EnemiesKilled++; }
                                    live.RemoveAt(targetEnemyIndex);
                                }
                                else
                                {
                                    live[targetEnemyIndex] = e;
                                }
                            }
                        }

                    }
                }

                // ENEMY BASE DAMAGE 
                bool baseDamaged = false;
                for (int i = live.Count - 1; i >= 0; i--) // Iterate backwards for safe removal if base dies
                {
                    var e = live[i];
                    if (e.Y <= e.AttackRange) // Check if enemy is in range to attack base
                    {
                        e.TimeSinceLastAttack += dt;
                        var info = enemyInfos[e.Class]; // To get AttackSpeed
                        float atkInterval = 1f / info.AttackSpeed;
                        if (e.TimeSinceLastAttack >= atkInterval)
                        {
                            baseHealth = Mathf.Max(0f, baseHealth - e.Damage);
                            wStat.DamageTaken += e.Damage;
                            e.TimeSinceLastAttack = 0f; // Reset attack cooldown
                            baseDamaged = true;

                            if (baseHealth <= 0f)
                            {
                                wStat.MachineGunDamage = perWaveDmg[(int)TurretType.MachineGun];
                                wStat.ShotgunDamage = perWaveDmg[(int)TurretType.Shotgun];
                                wStat.SniperDamage = perWaveDmg[(int)TurretType.Sniper];
                                wStat.MissileLauncherDamage = perWaveDmg[(int)TurretType.MissileLauncher];
                                wStat.LaserDamage = perWaveDmg[(int)TurretType.Laser];

                                wStat.HealthEnd = 0f;
                                wStat.WaveBeaten = false;
                                stats.Waves.Add(wStat);
                                stats.MissionsFailed++;
                                waveIndex = Mathf.Max(1, waveIndex - 2); // Roll back 2 waves
                                baseHealth = baseMaxHealth; // Reset base health

                                previousWaveFailed = true;

                                live.Clear(); // Clear all live enemies
                                pendingSpawns.Clear(); // Clear pending spawns
                                InitWave(); // Prepare for the new (potentially earlier) wave
                                goto END_OF_FRAME; // Skip other logic for this dt step
                            }
                            else
                                previousWaveFailed = false;
                        }
                        live[i] = e; // Update enemy state (like TimeSinceLastAttack)
                    }
                }

                // ------------------ base regeneration (single timer) ------------------
                // 1)  If we were hit this frame, reset the regen stopwatch.
                if (baseDamaged)
                    regenTickTimer = 0f;

                // 2)  Every frame we’re below max health, count up the stopwatch.
                if (baseHealth < baseMaxHealth)
                {
                    regenTickTimer += dt;

                    // 3)  Once the stopwatch reaches the interval, heal once and reset it.
                    if (regenTickTimer >= regenInterval)
                    {
                        float healed = Mathf.Min(regenAmount, baseMaxHealth - baseHealth);
                        baseHealth += healed;
                        stats.TotalHealthRepaired += healed;
                        wStat.HealthRegen += healed;

                        regenTickTimer = 0f;                     // restart the same wait period

                        if (baseHealth >= baseMaxHealth)         // fully healed
                            baseDamaged = false;        // clear the flag until next hit
                    }
                }


                // ------------------ spending ---------------------------------
                if (mode == SpendingMode.MostEffective)
                {
                    // 1) Prioritise base health if died last wave
                    float rawHealthCost = CostForBase(PlayerUpgradeType.MaxHealth, maxHpLvl);
                    ulong healthCost = (ulong)Mathf.Ceil(rawHealthCost);
                    if (previousWaveFailed
                        && healthCost > 0
                        && coins >= healthCost)
                    {
                        coins -= healthCost;
                        maxHpLvl++;
                        baseMaxHealth += baseSO.MaxHealthUpgradeAmount;
                        baseHealth = Mathf.Min(baseHealth + baseSO.MaxHealthUpgradeAmount, baseMaxHealth);
                        wStat.BaseUpgrades++;
                        wStat.MaxHealthLevel = maxHpLvl;
                        wStat.CurrentMaxHealth = baseMaxHealth;
                        wStat.MoneySpent += healthCost;
                        stats.MoneySpent += healthCost;
                    }

                    else
                    {
                        // 2) If the base was merely damaged (not lost), invest in regeneration
                        if (wStat.DamageTaken > 0f)
                        {
                            // a) Try upgrading Regen Amount first
                            float rawRegenAmtCost = CostForBase(PlayerUpgradeType.RegenAmount, regenAmtLvl);
                            ulong regenAmtCost = (ulong)Mathf.Ceil(rawRegenAmtCost);
                            if (regenAmtCost > 0 && coins >= regenAmtCost)
                            {
                                coins -= regenAmtCost;
                                regenAmtLvl++;
                                regenAmount += baseSO.RegenAmountUpgradeAmount;
                                wStat.BaseUpgrades++;
                                wStat.RegenAmountLevel = regenAmtLvl;
                                wStat.CurrentRegenAmount = regenAmount;
                                wStat.MoneySpent += regenAmtCost;
                                stats.MoneySpent += regenAmtCost;
                            }
                            else
                            {
                                // b) Otherwise, try upgrading Regen Interval
                                float rawRegenIntCost = CostForBase(PlayerUpgradeType.RegenInterval, regenIntLvl);
                                ulong regenIntCost = (ulong)Mathf.Ceil(rawRegenIntCost);

                                // Only allow interval upgrades if we're not at the floor
                                bool canUpgradeInterval = regenInterval > MinBaseRegenInterval;
                                if (canUpgradeInterval && regenIntCost > 0 && coins >= regenIntCost)
                                {
                                    coins -= regenIntCost;
                                    regenIntLvl++;
                                    regenInterval = Mathf.Max(MinBaseRegenInterval, regenInterval - baseSO.RegenIntervalUpgradeAmount);
                                    wStat.BaseUpgrades++;
                                    wStat.RegenIntervalLevel = regenIntLvl;
                                    wStat.CurrentRegenInterval = regenInterval;
                                    wStat.MoneySpent += regenIntCost;
                                    stats.MoneySpent += regenIntCost;
                                }
                                else
                                {
                                    // c) If regen upgrades are unaffordable or maxed, fall back to turret DPS
                                    ulong before = coins;
                                    var oldSlots = new List<TurretBlueprint>(slots);
                                    strategy.Tick(ref coins, ref slots, waveIndex);
                                    ulong spent = before - coins;

                                    if (spent > 0)
                                    {
                                        wStat.TurretUpgrades++;
                                        // detect WHICH turret stat increased
                                        for (int i = 0; i < oldSlots.Count; i++)
                                        {
                                            var o = oldSlots[i];
                                            var n = slots[i];
                                            if (n.DamageLevel != o.DamageLevel)
                                            { wStat.DamageUpgrades++; break; }
                                            else if (n.FireRateLevel != o.FireRateLevel)
                                            { wStat.FireRateUpgrades++; break; }
                                            else if (n.CriticalChanceLevel != o.CriticalChanceLevel)
                                            { wStat.CriticalChanceUpgrades++; break; }
                                            else if (n.CriticalDamageMultiplierLevel != o.CriticalDamageMultiplierLevel)
                                            { wStat.CriticalDamageMultiplierUpgrades++; break; }
                                            else if (n.ExplosionRadiusLevel != o.ExplosionRadiusLevel)
                                            { wStat.ExplosionRadiusUpgrades++; break; }
                                            else if (n.SplashDamageLevel != o.SplashDamageLevel)
                                            { wStat.SplashDamageUpgrades++; break; }
                                            else if (n.PierceChanceLevel != o.PierceChanceLevel)
                                            { wStat.PierceChanceUpgrades++; break; }
                                            else if (n.PierceDamageFalloffLevel != o.PierceDamageFalloffLevel)
                                            { wStat.PierceDamageFalloffUpgrades++; break; }
                                            else if (n.PelletCountLevel != o.PelletCountLevel)
                                            { wStat.PelletCountUpgrades++; break; }
                                            else if (n.KnockbackStrengthLevel != o.KnockbackStrengthLevel)
                                            { wStat.KnockbackStrengthUpgrades++; break; }
                                            else if (n.DamageFalloffOverDistanceLevel != o.DamageFalloffOverDistanceLevel)
                                            { wStat.DamageFalloffOverDistanceUpgrades++; break; }
                                            else if (n.PercentBonusDamagePerSecLevel != o.PercentBonusDamagePerSecLevel)
                                            { wStat.PercentBonusDamagePerSecUpgrades++; break; }
                                            else if (n.SlowEffectLevel != o.SlowEffectLevel)
                                            { wStat.SlowEffectUpgrades++; break; }
                                        }
                                        wStat.MoneySpent += spent;
                                        stats.MoneySpent += spent;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // 3) No base damage last wave = delegate directly to turret DPS per coin
                            ulong before = coins;
                            var oldSlots = new List<TurretBlueprint>(slots);
                            strategy.Tick(ref coins, ref slots, waveIndex);
                            ulong spent = before - coins;

                            if (spent > 0)
                            {
                                wStat.TurretUpgrades++;
                                for (int i = 0; i < oldSlots.Count; i++)
                                {
                                    var o = oldSlots[i];
                                    var n = slots[i];
                                    if (n.DamageLevel != o.DamageLevel)
                                    { wStat.DamageUpgrades++; break; }
                                    else if (n.FireRateLevel != o.FireRateLevel)
                                    { wStat.FireRateUpgrades++; break; }
                                    else if (n.CriticalChanceLevel != o.CriticalChanceLevel)
                                    { wStat.CriticalChanceUpgrades++; break; }
                                    else if (n.CriticalDamageMultiplierLevel != o.CriticalDamageMultiplierLevel)
                                    { wStat.CriticalDamageMultiplierUpgrades++; break; }
                                    else if (n.ExplosionRadiusLevel != o.ExplosionRadiusLevel)
                                    { wStat.ExplosionRadiusUpgrades++; break; }
                                    else if (n.SplashDamageLevel != o.SplashDamageLevel)
                                    { wStat.SplashDamageUpgrades++; break; }
                                    else if (n.PierceChanceLevel != o.PierceChanceLevel)
                                    { wStat.PierceChanceUpgrades++; break; }
                                    else if (n.PierceDamageFalloffLevel != o.PierceDamageFalloffLevel)
                                    { wStat.PierceDamageFalloffUpgrades++; break; }
                                    else if (n.PelletCountLevel != o.PelletCountLevel)
                                    { wStat.PelletCountUpgrades++; break; }
                                    else if (n.KnockbackStrengthLevel != o.KnockbackStrengthLevel)
                                    { wStat.KnockbackStrengthUpgrades++; break; }
                                    else if (n.DamageFalloffOverDistanceLevel != o.DamageFalloffOverDistanceLevel)
                                    { wStat.DamageFalloffOverDistanceUpgrades++; break; }
                                    else if (n.PercentBonusDamagePerSecLevel != o.PercentBonusDamagePerSecLevel)
                                    { wStat.PercentBonusDamagePerSecUpgrades++; break; }
                                    else if (n.SlowEffectLevel != o.SlowEffectLevel)
                                    { wStat.SlowEffectUpgrades++; break; }
                                }
                                wStat.MoneySpent += spent;
                                stats.MoneySpent += spent;
                            }
                        }
                    }
                }
                else // Cheapest or Random
                {
                    // build all affordable upgrade candidates (turret and base)
                    var candidates = new List<(bool isBase, PlayerUpgradeType baseType, TurretUpgradeType turretType, int slot, ulong cost)>();

                    // 1) turret stat candidates
                    for (int t = 0; t < slots.Count; t++)
                    {
                        var bp = slots[t];
                        // DAMAGE
                        {
                            float raw = GetExponentialCostPlusLevel(
                                bp.DamageUpgradeBaseCost,
                                bp.DamageLevel,
                                bp.DamageCostExponentialMultiplier);
                            ulong c = (ulong)Mathf.Ceil(raw);
                            if (c <= coins)
                                candidates.Add((false, default, TurretUpgradeType.Damage, t, c));
                        }
                        // FIRE RATE
                        {
                            float raw = GetExponentialCostPlusLevel(
                                bp.FireRateUpgradeBaseCost,
                                bp.FireRateLevel,
                                bp.FireRateCostExponentialMultiplier);
                            ulong c = (ulong)Mathf.Ceil(raw);
                            if (c <= coins)
                                candidates.Add((false, default, TurretUpgradeType.FireRate, t, c));
                        }
                        // CRIT CHANCE
                        {
                            float raw = GetExponentialCost(
                                bp.CritChanceUpgradeBaseCost,
                                bp.CriticalChanceLevel,
                                bp.CriticalChanceCostExponentialMultiplier);
                            ulong c = (ulong)Mathf.Ceil(raw);
                            if (c <= coins)
                                candidates.Add((false, default, TurretUpgradeType.CriticalChance, t, c));
                        }
                        // CRIT DAMAGE
                        {
                            float raw = GetExponentialCost(
                                bp.CritDamageUpgradeBaseCost,
                                bp.CriticalDamageMultiplierLevel,
                                bp.CriticalDamageCostExponentialMultiplier);
                            ulong c = (ulong)Mathf.Ceil(raw);
                            if (c <= coins)
                                candidates.Add((false, default, TurretUpgradeType.CriticalDamageMultiplier, t, c));
                        }
                        // EXPLOSION RADIUS
                        {
                            float raw = GetHybridCost(
                                bp.ExplosionRadiusUpgradeBaseCost,
                                bp.ExplosionRadiusLevel);
                            ulong c = (ulong)Mathf.Ceil(raw);
                            if (c <= coins)
                                candidates.Add((false, default, TurretUpgradeType.ExplosionRadius, t, c));
                        }
                        // SPLASH DAMAGE
                        {
                            float raw = GetHybridCost(
                                bp.SplashDamageUpgradeBaseCost,
                                bp.SplashDamageLevel);
                            ulong c = (ulong)Mathf.Ceil(raw);
                            if (c <= coins)
                                candidates.Add((false, default, TurretUpgradeType.SplashDamage, t, c));
                        }
                        // PIERCE CHANCE
                        {
                            float raw = GetHybridCost(
                                bp.PierceChanceUpgradeBaseCost,
                                bp.PierceChanceLevel);
                            ulong c = (ulong)Mathf.Ceil(raw);
                            if (c <= coins)
                                candidates.Add((false, default, TurretUpgradeType.PierceChance, t, c));
                        }
                        // PIERCE FALLOFF
                        {
                            float raw = GetHybridCost(
                                bp.PierceDamageFalloffUpgradeBaseCost,
                                bp.PierceDamageFalloffLevel);
                            ulong c = (ulong)Mathf.Ceil(raw);
                            if (c <= coins)
                                candidates.Add((false, default, TurretUpgradeType.PierceDamageFalloff, t, c));
                        }
                        // PELLET COUNT
                        {
                            float raw = GetHybridCost(
                                bp.PelletCountUpgradeBaseCost,
                                bp.PelletCountLevel);
                            ulong c = (ulong)Mathf.Ceil(raw);
                            if (c <= coins)
                                candidates.Add((false, default, TurretUpgradeType.PelletCount, t, c));
                        }
                        // KNOCKBACK
                        {
                            float raw = GetExponentialCost(
                                bp.KnockbackStrengthUpgradeBaseCost,
                                bp.KnockbackStrengthLevel,
                                bp.KnockbackStrengthCostExponentialMultiplier);
                            ulong c = (ulong)Mathf.Ceil(raw);
                            if (c <= coins)
                                candidates.Add((false, default, TurretUpgradeType.KnockbackStrength, t, c));
                        }
                        // DAMAGE FALLOFF
                        {
                            float raw = GetHybridCost(
                                bp.DamageFalloffOverDistanceUpgradeBaseCost,
                                bp.DamageFalloffOverDistanceLevel);
                            ulong c = (ulong)Mathf.Ceil(raw);
                            if (c <= coins)
                                candidates.Add((false, default, TurretUpgradeType.DamageFalloffOverDistance, t, c));
                        }
                        // % BONUS DPS
                        {
                            float raw = GetHybridCost(
                                bp.PercentBonusDamagePerSecUpgradeBaseCost,
                                bp.PercentBonusDamagePerSecLevel);
                            ulong c = (ulong)Mathf.Ceil(raw);
                            if (c <= coins)
                                candidates.Add((false, default, TurretUpgradeType.PercentBonusDamagePerSec, t, c));
                        }
                        // SLOW EFFECT
                        {
                            float raw = GetHybridCost(
                                bp.SlowEffectUpgradeBaseCost,
                                bp.SlowEffectLevel);
                            ulong c = (ulong)Mathf.Ceil(raw);
                            if (c <= coins)
                                candidates.Add((false, default, TurretUpgradeType.SlowEffect, t, c));
                        }
                    }

                    // 2) base upgrade candidates
                    foreach (PlayerUpgradeType pu in Enum.GetValues(typeof(PlayerUpgradeType)))
                    {
                        int lvl = pu switch
                        {
                            PlayerUpgradeType.MaxHealth => maxHpLvl,
                            PlayerUpgradeType.RegenAmount => regenAmtLvl,
                            _ => regenIntLvl
                        };
                        float raw = CostForBase(pu, lvl);
                        ulong c = (ulong)Mathf.RoundToInt(raw);
                        if (c <= coins)
                            candidates.Add((true, pu, default, -1, c));
                    }

                    // disregard any zero-cost upgrades
                    candidates.RemoveAll(x => x.cost == 0);

                    // 3) pick one
                    if (candidates.Count > 0)
                    {

                        var pick = mode == SpendingMode.Cheapest
                            ? candidates.OrderBy(x => x.cost).First()
                            : candidates[Random.Range(0, candidates.Count)];

                        coins -= pick.cost;
                        wStat.MoneySpent += pick.cost;
                        stats.MoneySpent += pick.cost;

                        if (pick.isBase)
                        {
                            // apply base upgrade
                            switch (pick.baseType)
                            {
                                case PlayerUpgradeType.MaxHealth:
                                    maxHpLvl++;
                                    baseMaxHealth += baseSO.MaxHealthUpgradeAmount;
                                    baseHealth = Mathf.Min(baseHealth + baseSO.MaxHealthUpgradeAmount, baseMaxHealth);
                                    wStat.BaseUpgrades++;
                                    wStat.MaxHealthLevel = maxHpLvl;
                                    wStat.CurrentMaxHealth = baseMaxHealth;
                                    break;
                                case PlayerUpgradeType.RegenAmount:
                                    regenAmtLvl++;
                                    regenAmount += baseSO.RegenAmountUpgradeAmount;
                                    wStat.BaseUpgrades++;
                                    wStat.RegenAmountLevel = regenAmtLvl;
                                    wStat.CurrentRegenAmount = regenAmount;
                                    break;
                                case PlayerUpgradeType.RegenInterval:
                                    regenIntLvl++;
                                    regenInterval = Mathf.Max(MinBaseRegenInterval, regenInterval - baseSO.RegenIntervalUpgradeAmount);
                                    wStat.BaseUpgrades++;
                                    wStat.RegenIntervalLevel = regenIntLvl;
                                    wStat.CurrentRegenInterval = regenInterval;
                                    break;
                            }
                        }
                        else
                        {
                            // apply turret upgrade
                            var bp = slots[pick.slot];
                            switch (pick.turretType)
                            {
                                case TurretUpgradeType.Damage:
                                    slots[pick.slot] = bp.WithDamageUpgraded();
                                    wStat.DamageUpgrades++;
                                    break;
                                case TurretUpgradeType.FireRate:
                                    slots[pick.slot] = bp.WithFireRateUpgraded();
                                    wStat.FireRateUpgrades++;
                                    break;
                                case TurretUpgradeType.CriticalChance:
                                    slots[pick.slot] = bp.WithCritChanceUpgraded();
                                    wStat.CriticalChanceUpgrades++;
                                    break;
                                case TurretUpgradeType.CriticalDamageMultiplier:
                                    slots[pick.slot] = bp.WithCritDamageUpgraded();
                                    wStat.CriticalDamageMultiplierUpgrades++;
                                    break;
                                case TurretUpgradeType.ExplosionRadius:
                                    slots[pick.slot] = bp.WithExplosionRadiusUpgraded();
                                    wStat.ExplosionRadiusUpgrades++;
                                    break;
                                case TurretUpgradeType.SplashDamage:
                                    slots[pick.slot] = bp.WithSplashDamageUpgraded();
                                    wStat.SplashDamageUpgrades++;
                                    break;
                                case TurretUpgradeType.PierceChance:
                                    slots[pick.slot] = bp.WithPierceChanceUpgraded();
                                    wStat.PierceChanceUpgrades++;
                                    break;
                                case TurretUpgradeType.PierceDamageFalloff:
                                    slots[pick.slot] = bp.WithPierceDamageFalloffUpgraded();
                                    wStat.PierceDamageFalloffUpgrades++;
                                    break;
                                case TurretUpgradeType.PelletCount:
                                    slots[pick.slot] = bp.WithPelletCountUpgraded();
                                    wStat.PelletCountUpgrades++;
                                    break;
                                case TurretUpgradeType.KnockbackStrength:
                                    slots[pick.slot] = bp.WithKnockbackStrengthUpgraded();
                                    wStat.KnockbackStrengthUpgrades++;
                                    break;
                                case TurretUpgradeType.DamageFalloffOverDistance:
                                    slots[pick.slot] = bp.WithDamageFalloffOverDistanceUpgraded();
                                    wStat.DamageFalloffOverDistanceUpgrades++;
                                    break;
                                case TurretUpgradeType.PercentBonusDamagePerSec:
                                    slots[pick.slot] = bp.WithPercentBonusDamagePerSecUpgraded();
                                    wStat.PercentBonusDamagePerSecUpgrades++;
                                    break;
                                case TurretUpgradeType.SlowEffect:
                                    slots[pick.slot] = bp.WithSlowEffectUpgraded();
                                    wStat.SlowEffectUpgrades++;
                                    break;
                            }
                        }
                        wStat.TurretUpgrades++;
                    }
                }

                // Write stats for each slot
                for (int i = 0; i < 5; i++)
                {
                    string val;
                    if (i < slots.Count)
                    {
                        // equipped turret: name + total level
                        var bp = slots[i];
                        int totalLevel =
                            bp.DamageLevel + bp.FireRateLevel + bp.CriticalChanceLevel +
                            bp.CriticalDamageMultiplierLevel + bp.ExplosionRadiusLevel +
                            bp.SplashDamageLevel + bp.PierceChanceLevel +
                            bp.PierceDamageFalloffLevel + bp.PelletCountLevel +
                            bp.KnockbackStrengthLevel + bp.DamageFalloffOverDistanceLevel +
                            bp.PercentBonusDamagePerSecLevel + bp.SlowEffectLevel;
                        val = $"{bp.Type}:{totalLevel}";
                    }
                    else if (i < nextSlot)
                    {
                        // slot unlocked but no turret (rare)
                        val = "empty";
                    }
                    else
                    {
                        val = "locked";
                    }

                    switch (i)
                    {
                        case 0:
                            wStat.Slot1 = val;
                            break;
                        case 1:
                            wStat.Slot2 = val;
                            break;
                        case 2:
                            wStat.Slot3 = val;
                            break;
                        case 3:
                            wStat.Slot4 = val;
                            break;
                        case 4:
                            wStat.Slot5 = val;
                            break;
                    }
                }

                // Register stats to export
                if (live.Count == 0 && pendingSpawns.Count == 0 && !bossIncoming)
                {
                    wStat.HealthEnd = baseHealth;   // before post wave regen
                    wStat.WaveBeaten = true;
                    regenTickTimer = 0f;

                    // copy per type DPS
                    wStat.MachineGunDamage = perWaveDmg[(int)TurretType.MachineGun];
                    wStat.ShotgunDamage = perWaveDmg[(int)TurretType.Shotgun];
                    wStat.SniperDamage = perWaveDmg[(int)TurretType.Sniper];
                    wStat.MissileLauncherDamage = perWaveDmg[(int)TurretType.MissileLauncher];
                    wStat.LaserDamage = perWaveDmg[(int)TurretType.Laser];

                    stats.Waves.Add(wStat);
                    stats.MaxZone = Mathf.Max(stats.MaxZone, wStat.Wave);
                    stats.WavesBeaten++;
                    waveIndex++;
                    InitWave();
                }

            END_OF_FRAME:
                simTime += dt;
            }

            stats.SimMinutes = minutes;
            return stats;
        }
    }
}*/
