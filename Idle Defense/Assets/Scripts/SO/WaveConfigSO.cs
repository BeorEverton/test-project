using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.SO
{
    [CreateAssetMenu(fileName = "WaveConfig", menuName = "ScriptableObjects/WaveConfig", order = 1)]
    public class WaveConfigSO : ScriptableObject
    {
        [Header("Wave settings")]
        [Tooltip("WaveIndex to set this as base config")]
        public int WaveStartIndex;

        [Tooltip("List of enemy prefabs to spawn for current wave")]
        public List<EnemyWaveEntry> EnemyWaveEntries;

        [Tooltip("Time between each enemy spawn (used as a fallback when TotalSpawnDuration <= 0).")]
        public float TimeBetweenSpawns = 0.2f;

        [Header("Spawn pacing")]
        [Tooltip("Total time (seconds) to spawn ALL enemies in this wave. If <= 0, it will be computed from TimeBetweenSpawns * totalEnemies.")]
        public float TotalSpawnDuration = 0f;

        [Tooltip("Per-wave cumulative spawn curve. X = normalized time (0..1), Y = cumulative fraction of enemies spawned (0..1). If null/empty, spawn linearly.")]
        public AnimationCurve SpawnCurve;

    }

    [System.Serializable]
    public class EnemyWaveEntry
    {
        [Tooltip("Enemy prefab to spawn")]
        public GameObject EnemyPrefab;
        [Tooltip("Number of initial enemies to spawn")]
        public int NumberOfEnemies;
    }
}