using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Assets.Scripts.WaveSystem;
using Assets.Scripts.SO;
using Assets.Scripts.Enemies;

public sealed class RuntimeBalanceTuningManager : MonoBehaviour
{
    public static RuntimeBalanceTuningManager Instance { get; private set; }

    [Header("Active profile (your debug UI edits this)")]
    public BalanceTuningProfile active = BalanceTuningProfile.CreateDefault();

    [Header("Safety")]
    [Tooltip("If true, reload current wave after Apply()")]
    public bool autoReloadWaveOnApply = true;

    private readonly Dictionary<int, EnemyInfoSnapshot> _enemyOriginalById = new Dictionary<int, EnemyInfoSnapshot>(256);
    private readonly Dictionary<FieldKey, ObjectFieldSnapshot> _fieldSnapshots = new Dictionary<FieldKey, ObjectFieldSnapshot>(32);



    private List<EnemyInfoSO> _enemyInfosInUse = new List<EnemyInfoSO>();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (string.IsNullOrEmpty(active.createdUtc))
            active.createdUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }

    // -------------------------
    // Public API for your debug UI
    // -------------------------

    public void DiscoverAndSnapshotIfNeeded()
    {
        _enemyInfosInUse = DiscoverEnemyInfosFromZones();
        SnapshotEnemyInfosOnce(_enemyInfosInUse);

        SnapshotPrivateFieldOnce(FindFirstObjectByType<ZoneManager>(), "_extraEnemiesPerWave");
        SnapshotPrivateFieldOnce(FindFirstObjectByType<ZoneManager>(), "_maxEnemiesPerWave");

        SnapshotPrivateFieldOnce(FindFirstObjectByType<EnemySpawner>(), "_maxConcurrentEnemies");
        SnapshotPrivateFieldOnce(FindFirstObjectByType<EnemySpawner>(), "_minSpawnDuration");
    }

    public void Apply()
    {
        DiscoverAndSnapshotIfNeeded();

        ApplyZoneKnobs(active);
        ApplySpawnerKnobs(active);
        ApplyEnemyMultipliers(active);

        if (autoReloadWaveOnApply)
            ReloadCurrentWave();
    }

    public void RevertAll()
    {
        // Revert enemy infos
        foreach (var info in _enemyInfosInUse)
        {
            if (info == null) continue;
            if (_enemyOriginalById.TryGetValue(info.EnemyId, out var snap))
                snap.ApplyTo(info);
        }

        // Revert private fields
        foreach (var kv in _fieldSnapshots)
        {
            // kv.Key stores instance id + field name; snapshot stores field name.
            // We only need the actual object reference, so store it inside snapshot.
            kv.Value.Revert();
        }


        ReloadCurrentWave();
    }

    public void ReloadCurrentWave()
    {
        DiscoverAndSnapshotIfNeeded();

        var wm = WaveManager.Instance;
        if (wm == null) return;

        int cur = wm.GetCurrentWaveIndex();

        // Hard stop the current wave content (enemies, spawns, coroutines).
        wm.AbortWaveAndDespawnAll();

        // Re-sync the wave index and rebuild via ZoneManager on restart.
        wm.LoadWave(cur);
        wm.ForceRestartWave();
    }


    public string ExportSessionReportToJson()
    {
        // Add some runtime metadata (wave, zone, etc.)
        int wave = (WaveManager.Instance != null) ? WaveManager.Instance.GetCurrentWaveIndex() : -1;
        int globalWave = (FindFirstObjectByType<ZoneManager>() != null) ? FindFirstObjectByType<ZoneManager>().GlobalWaveIndex : -1;

        var report = new BalanceSessionReport
        {
            createdUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            unityVersion = Application.unityVersion,
            platform = Application.platform.ToString(),
            currentWave = wave,
            zoneGlobalWave = globalWave,
            profile = active
        };

        return JsonUtility.ToJson(report, true);
    }

    public void SaveReportToFile()
    {
        string json = ExportSessionReportToJson();
        string path = GetReportPath();
        File.WriteAllText(path, json);
        Debug.Log("[BalanceTuning] Saved report: " + path);
        Debug.Log(json);
    }

    public void SaveProfileToFile()
    {
        string path = GetProfilePath();
        File.WriteAllText(path, JsonUtility.ToJson(active, true));
        Debug.Log("[BalanceTuning] Saved profile: " + path);
    }

    public void LoadProfileFromFileAndApply()
    {
        string path = GetProfilePath();
        if (!File.Exists(path))
        {
            Debug.LogWarning("[BalanceTuning] No profile file found at: " + path);
            return;
        }

        string json = File.ReadAllText(path);
        active = JsonUtility.FromJson<BalanceTuningProfile>(json);
        Debug.Log("[BalanceTuning] Loaded profile: " + path);

        Apply();
    }

    // -------------------------
    // Internals
    // -------------------------

    private void ApplyZoneKnobs(BalanceTuningProfile p)
    {
        var zm = FindFirstObjectByType<ZoneManager>();
        if (zm == null) return;

        SetPrivateField(zm, "_extraEnemiesPerWave", Mathf.RoundToInt(p.extraEnemiesPerWave));

        SetPrivateField(zm, "_maxEnemiesPerWave", p.maxEnemiesPerWave);
        // ZoneManager uses these fields during CreateNewEntry and cap logic.
    }

    private void ApplySpawnerKnobs(BalanceTuningProfile p)
    {
        var sp = FindFirstObjectByType<EnemySpawner>();
        if (sp == null) return;

        SetPrivateField(sp, "_maxConcurrentEnemies", p.maxConcurrentEnemies);
        SetPrivateField(sp, "_minSpawnDuration", p.minSpawnDuration);
        // Spawn concurrency and pacing are applied in SpawnEnemies(). :contentReference[oaicite:5]{index=5}
    }

    private void ApplyEnemyMultipliers(BalanceTuningProfile p)
    {
        // We do not touch prefab EnemyInfoSO assets.
        // We apply tuning only to the per-wave clones stored in Wave.WaveEnemies.
        var wm = WaveManager.Instance;
        if (wm == null) return;

        var wave = wm.GetCurrentWave();
        if (wave == null || wave.WaveEnemies == null || wave.WaveEnemies.Count == 0) return;

        // Build quick override lookup
        var ovById = new Dictionary<int, BalanceTuningProfile.EnemyOverride>(p.perEnemy.Count);
        for (int i = 0; i < p.perEnemy.Count; i++)
        {
            var ov = p.perEnemy[i];
            if (ov != null && ov.enemyId >= 0)
                ovById[ov.enemyId] = ov;
        }

        foreach (var kv in wave.WaveEnemies)
        {
            int enemyId = kv.Key;
            var info = kv.Value;
            if (info == null) continue;


            // Key includes the wave number so each wave gets the correct "wave-scaled baseline" snapshot.
            // Otherwise wave 2 might reuse wave 1 baseline and the slider effect looks wrong/tiny.
            int waveNumber = 1;
            if (wm != null) waveNumber = Mathf.Max(1, wm.GetCurrentWaveIndex());

            // Make a stable unique negative key for (waveNumber, enemyId).
            // 100000 is just a safe stride; keep it bigger than your max enemyId.
            int waveKey = -((waveNumber * 100000) + enemyId);

            if (!_enemyOriginalById.ContainsKey(waveKey))
                _enemyOriginalById[waveKey] = EnemyInfoSnapshot.From(info);

            var original = _enemyOriginalById[waveKey];


            float healthMul = p.enemyBaseHealthMul;
            float dmgMul = p.enemyDamageMul;
            float spdMul = p.enemyMoveSpeedMul;
            float coinMul = p.enemyCoinDropMul;

            if (ovById.TryGetValue(enemyId, out var ov2))
            {
                healthMul *= ov2.baseHealthMul;
                dmgMul *= ov2.damageMul;
                spdMul *= ov2.moveSpeedMul;
                coinMul *= ov2.coinDropMul;

                if (ov2.overrideShieldCharges)
                    info.ShieldCharges = Mathf.Max(0, ov2.shieldCharges);
                else
                    info.ShieldCharges = original.ShieldCharges;
            }
            else
            {
                info.ShieldCharges = original.ShieldCharges;
            }

            // IMPORTANT: apply on the WAVE-SCALED baseline (original snapshot),
            // which matches your "bonus scaled" mental model.
            info.MaxHealth = original.MaxHealth * healthMul;
            info.Damage = original.Damage * dmgMul;
            info.MovementSpeed = original.MovementSpeed * spdMul;

            info.Armor = Mathf.Clamp(original.Armor * p.enemyArmorMul, 0f, 0.9f);
            info.DodgeChance = Mathf.Clamp(original.DodgeChance * p.enemyDodgeMul, 0f, 0.9f);

            ulong baseCoin = original.CoinDropAmount;
            double scaled = Math.Floor(baseCoin * (double)coinMul);
            if (scaled < 0) scaled = 0;
            if (scaled > ulong.MaxValue) scaled = ulong.MaxValue;
            info.CoinDropAmount = (ulong)scaled;
        }
    }

    public void ApplyToCurrentWaveOnly()
    {
        DiscoverAndSnapshotIfNeeded();

        // apply knobs that affect wave construction is NOT needed here (wave already built)
        // so we only apply enemy multipliers to wave clones
        ApplyEnemyMultipliers(active);
    }

    private List<EnemyInfoSO> DiscoverEnemyInfosFromZones()
    {
        var zm = FindFirstObjectByType<ZoneManager>();
        var results = new List<EnemyInfoSO>(128);
        if (zm == null) return results;

        // ZoneManager has private _zones. :contentReference[oaicite:7]{index=7}
        var zonesObj = GetPrivateField(zm, "_zones") as System.Collections.IList;
        if (zonesObj == null) return results;

        var set = new HashSet<int>();

        for (int zi = 0; zi < zonesObj.Count; zi++)
        {
            var zone = zonesObj[zi] as ZoneDefinitionSO;
            if (zone == null || zone.Waves == null) continue;

            for (int wi = 0; wi < zone.Waves.Count; wi++)
            {
                var waveDef = zone.Waves[wi];
                if (waveDef == null || waveDef.WaveConfig == null || waveDef.WaveConfig.EnemyWaveEntries == null)
                    continue;

                var entries = waveDef.WaveConfig.EnemyWaveEntries;
                for (int ei = 0; ei < entries.Count; ei++)
                {
                    var entry = entries[ei];
                    if (entry == null || entry.EnemyPrefab == null) continue;

                    var enemy = entry.EnemyPrefab.GetComponent<Enemy>();
                    if (enemy == null || enemy.Info == null) continue;

                    var info = enemy.Info;
                    if (info != null && set.Add(info.EnemyId))
                        results.Add(info);
                }
            }
        }

        return results;
    }

    private void SnapshotEnemyInfosOnce(List<EnemyInfoSO> infos)
    {
        for (int i = 0; i < infos.Count; i++)
        {
            var info = infos[i];
            if (info == null) continue;
            if (_enemyOriginalById.ContainsKey(info.EnemyId)) continue;

            _enemyOriginalById[info.EnemyId] = EnemyInfoSnapshot.From(info);
        }
    }

    private void SnapshotPrivateFieldOnce(UnityEngine.Object obj, string fieldName)
    {
        if (obj == null) return;

        var key = new FieldKey(obj, fieldName);
        if (_fieldSnapshots.ContainsKey(key)) return;

        _fieldSnapshots[key] = ObjectFieldSnapshot.Capture(obj, fieldName);
    }

    private readonly struct FieldKey
    {
        private readonly int _objId;
        private readonly string _fieldName;

        public FieldKey(UnityEngine.Object obj, string fieldName)
        {
            _objId = obj != null ? obj.GetInstanceID() : 0;
            _fieldName = fieldName ?? "";
        }

        public override int GetHashCode()
        {
            unchecked { return (_objId * 397) ^ _fieldName.GetHashCode(); }
        }

        public override bool Equals(object other)
        {
            if (other is not FieldKey o) return false;
            return _objId == o._objId && _fieldName == o._fieldName;
        }
    }


    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        if (obj == null) return;
        var fi = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (fi == null)
        {
            Debug.LogWarning("[BalanceTuning] Field not found: " + obj.GetType().Name + "." + fieldName);
            return;
        }
        fi.SetValue(obj, value);
    }

    private static object GetPrivateField(object obj, string fieldName)
    {
        if (obj == null) return null;
        var fi = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        return (fi != null) ? fi.GetValue(obj) : null;
    }

    private static string GetProfilePath()
    {
        return Path.Combine(Application.persistentDataPath, "balance_profile.json");
    }

    private static string GetReportPath()
    {
        string stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        return Path.Combine(Application.persistentDataPath, "balance_report_" + stamp + ".json");
    }

    [Serializable]
    private sealed class BalanceSessionReport
    {
        public string createdUtc;
        public string unityVersion;
        public string platform;
        public int currentWave;
        public int zoneGlobalWave;
        public BalanceTuningProfile profile;
    }

    private struct EnemyInfoSnapshot
    {
        public int EnemyId;
        public string Name;
        public float MaxHealth;
        public float MovementSpeed;
        public ulong CoinDropAmount;

        public float Damage;
        public float AttackRange;
        public float AttackSpeed;
        public int SweepTargets;

        public float Armor;
        public float DodgeChance;

        public int ShieldCharges;

        public static EnemyInfoSnapshot From(EnemyInfoSO so)
        {
            return new EnemyInfoSnapshot
            {
                EnemyId = so.EnemyId,
                Name = so.Name,
                MaxHealth = so.MaxHealth,
                MovementSpeed = so.MovementSpeed,
                CoinDropAmount = so.CoinDropAmount,
                Damage = so.Damage,
                AttackRange = so.AttackRange,
                AttackSpeed = so.AttackSpeed,
                SweepTargets = so.SweepTargets,
                Armor = so.Armor,
                DodgeChance = so.DodgeChance,
                ShieldCharges = so.ShieldCharges
            };
        }

        public void ApplyTo(EnemyInfoSO so)
        {
            so.MaxHealth = MaxHealth;
            so.MovementSpeed = MovementSpeed;
            so.CoinDropAmount = CoinDropAmount;

            so.Damage = Damage;
            so.AttackRange = AttackRange;
            so.AttackSpeed = AttackSpeed;
            so.SweepTargets = SweepTargets;

            so.Armor = Armor;
            so.DodgeChance = DodgeChance;

            so.ShieldCharges = ShieldCharges;
        }
    }

    private sealed class ObjectFieldSnapshot
    {
        private readonly UnityEngine.Object _obj;
        private readonly string _fieldName;
        private readonly object _value;

        private ObjectFieldSnapshot(UnityEngine.Object obj, string fieldName, object value)
        {
            _obj = obj;
            _fieldName = fieldName;
            _value = value;
        }

        public static ObjectFieldSnapshot Capture(UnityEngine.Object obj, string fieldName)
        {
            if (obj == null) return new ObjectFieldSnapshot(null, fieldName, null);

            var fi = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (fi == null)
                return new ObjectFieldSnapshot(obj, fieldName, null);

            return new ObjectFieldSnapshot(obj, fieldName, fi.GetValue(obj));
        }

        public void Revert()
        {
            if (_obj == null) return;
            var fi = _obj.GetType().GetField(_fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (fi == null) return;
            fi.SetValue(_obj, _value);
        }
    }


}
