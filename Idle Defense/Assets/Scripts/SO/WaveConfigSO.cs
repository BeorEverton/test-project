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
        [Tooltip("Time between each enemy spawn")]
        public float TimeBetweenSpawns;
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