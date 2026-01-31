using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class BalanceTuningProfile
{
    [Header("Meta")]
    public string profileName = "Session";
    public string testerName = "";
    public string notes = "";
    public string createdUtc = "";

    [Header("Enemy global multipliers (applied to base prefab EnemyInfoSO before ZoneManager clones)")]
    [Range(0.1f, 3.0f)] public float enemyBaseHealthMul = 1.0f;
    [Range(0.1f, 3.0f)] public float enemyDamageMul = 1.0f;
    [Range(0.1f, 3.0f)] public float enemyMoveSpeedMul = 1.0f;
    [Range(0.1f, 3.0f)] public float enemyCoinDropMul = 1.0f;

    [Range(0.0f, 3.0f)] public float enemyArmorMul = 1.0f;     // armor is 0..0.9 in SO, we clamp
    [Range(0.0f, 3.0f)] public float enemyDodgeMul = 1.0f;     // dodge is 0..0.9 in SO, we clamp

    [Header("Zone scaling knobs (ZoneManager private fields via reflection)")]
    [Tooltip("ZoneManager: _extraEnemiesPerWave")]
    public float extraEnemiesPerWave = 10f;

    [Tooltip("ZoneManager: _maxEnemiesPerWave (0 = no cap)")]
    public int maxEnemiesPerWave = 10000;

    [Header("Spawner knobs (EnemySpawner private fields via reflection)")]
    [Tooltip("EnemySpawner: _maxConcurrentEnemies (0 = no cap)")]
    public int maxConcurrentEnemies = 500;

    [Tooltip("EnemySpawner: _minSpawnDuration")]
    public float minSpawnDuration = 3f;

    [Header("Per-enemy overrides (optional)")]
    public List<EnemyOverride> perEnemy = new List<EnemyOverride>();

    [Serializable]
    public sealed class EnemyOverride
    {
        public int enemyId = -1;
        public string enemyName = "";

        [Range(0.1f, 3.0f)] public float baseHealthMul = 1.0f;
        [Range(0.1f, 3.0f)] public float damageMul = 1.0f;
        [Range(0.1f, 3.0f)] public float moveSpeedMul = 1.0f;
        [Range(0.1f, 3.0f)] public float coinDropMul = 1.0f;

        public bool overrideShieldCharges = false;
        public int shieldCharges = 0;
    }

    public static BalanceTuningProfile CreateDefault()
    {
        return new BalanceTuningProfile
        {
            createdUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
    }
}
