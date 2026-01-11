using Assets.Scripts.SO;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.WaveSystem
{
    /// <summary>
    /// Runtime wave instance. Holds the WaveConfig used by the spawner plus
    /// metadata about its zone and any boss overrides.
    /// </summary>
    public class Wave
    {
        public WaveConfigSO WaveConfig { get; private set; }

        /// <summary>
        /// Global wave index (1..N) used for progression, damage bonus, etc.
        /// </summary>
        public int WaveNumber { get; private set; }

        /// <summary>
        /// Per-wave EnemyInfo clones, scaled for this wave. Keyed by EnemyClass.
        /// </summary>
        public Dictionary<int, EnemyInfoSO> WaveEnemies = new();

        // --- Zone meta data ---

        /// <summary>The zone this wave belongs to, if using the zone system.</summary>
        public ZoneDefinitionSO Zone { get; private set; }

        /// <summary>Wave index inside the current zone (1..ZoneLength).</summary>
        public int WaveNumberInZone { get; private set; }

        /// <summary>Flags coming from ZoneWaveDefinition for boss/miniboss waves.</summary>
        public bool IsMiniBossConfigured { get; private set; }
        public bool IsBossConfigured { get; private set; }

        /// <summary>
        /// Optional per-zone boss prefabs. If null, we fall back to the old
        /// behaviour (pick any enemy from the wave and promote it to boss).
        /// </summary>
        public GameObject MiniBossPrefab { get; private set; }
        public GameObject BossPrefab { get; private set; }

        public Wave(WaveConfigSO waveConfig, int waveNumber)
        {
            WaveConfig = waveConfig;
            WaveNumber = waveNumber;
        }

        /// <summary>
        /// Called by ZoneManager when the wave is built so we know which zone
        /// and which boss overrides apply to this wave.
        /// </summary>
        public void SetZoneData(
            ZoneDefinitionSO zone,
            int waveNumberInZone,
            bool isMiniBoss,
            bool isBoss,
            GameObject miniBossPrefab,
            GameObject bossPrefab)
        {
            Zone = zone;
            WaveNumberInZone = waveNumberInZone;
            IsMiniBossConfigured = isMiniBoss;
            IsBossConfigured = isBoss;
            MiniBossPrefab = miniBossPrefab;
            BossPrefab = bossPrefab;
        }

        public void AddEnemyClassToWave(int enemyId, EnemyInfoSO enemyConfig)
        {
            if (WaveEnemies.ContainsKey(enemyId))
            {
                WaveEnemies.Remove(enemyId);
            }
            WaveEnemies.Add(enemyId, enemyConfig);
        }

        /// <summary>
        /// Zone-aware miniboss flag. If the wave comes from a ZoneDefinition we
        /// trust the ZoneWaveDefinition; otherwise we fall back to the old
        /// modulo-based rule so legacy content still works.
        /// </summary>
        public bool IsMiniBossWave()
        {
            if (Zone != null)
                return IsMiniBossConfigured;

            // Legacy behaviour
            return WaveNumber % 5 == 0 && WaveNumber % 10 != 0;
        }

        /// <summary>
        /// Zone-aware boss flag. If the wave comes from a ZoneDefinition we
        /// trust the ZoneWaveDefinition; otherwise we fall back to the old
        /// modulo-based rule.
        /// </summary>
        public bool IsBossWave()
        {
            if (Zone != null)
                return IsBossConfigured;

            // Legacy behaviour
            return WaveNumber % 10 == 0;
        }
    }
}
