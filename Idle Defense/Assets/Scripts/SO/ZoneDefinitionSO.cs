using Assets.Scripts.SO;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.WaveSystem
{
    /// <summary>
    /// Data for one zone: which waves it contains, optional boss/miniboss preference,
    /// and (later) zone visuals like ground/sky/particles.
    /// </summary>
    [CreateAssetMenu(menuName = "Wave System/Zone Definition", fileName = "ZoneDefinition")]
    public class ZoneDefinitionSO : ScriptableObject
    {
        [Header("Identification")]
        [Tooltip("Internal id/key for this zone.")]
        public string Id;

        [Tooltip("Name shown in UI, logs, etc.")]
        public string DisplayName;

        [Tooltip("Index in the global zone list (0-based). Mostly for reference.")]
        public int ZoneIndex;

        [Header("Waves inside this zone (order = progression)")]
        public List<ZoneWaveDefinition> Waves = new List<ZoneWaveDefinition>();

        [Header("Boss setup (optional for now)")]
        [Tooltip("Preferred boss prefab for this zone, if you want a special model.")]
        public GameObject BossPrefab;

        [Tooltip("Preferred miniboss prefab for this zone, if you want a special model.")]
        public GameObject MiniBossPrefab;

        [Header("Visuals")]
        public Material GroundMat;        
        public Material SkyMat;
        public Material GroundOverlayMaterial;
        public GameObject ZoneParticlePrefab;
    }

    /// <summary>
    /// One wave entry inside a specific zone.
    /// Each wave has its own WaveConfigSO (enemy composition) and flags for mini/boss.
    /// </summary>
    [Serializable]
    public class ZoneWaveDefinition
    {
        [Tooltip("1-based wave number inside this zone. For readability only.")]
        public int WaveNumberInZone = 1;

        [Tooltip("Base enemy composition for this wave.")]
        public WaveConfigSO WaveConfig;

        [Header("Boss flags")]
        [Tooltip("Treat this as a miniboss wave for this zone.")]
        public bool IsMiniBoss;

        [Tooltip("Treat this as a boss wave for this zone.")]
        public bool IsBoss;

        [Tooltip("Optional override for boss prefab on this specific wave.")]
        public GameObject OverrideBossPrefab;
    }
}
