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
        public List<EnemyWaveEntry> EnemyPrefabs;
        [Tooltip("Time between each enemy spawn")]
        public float TimeBetweenSpawns;
    }

    [System.Serializable]
    public class EnemyWaveEntry
    {
        public GameObject enemyPrefab;
        public int numberOfEnemies;
    }
}