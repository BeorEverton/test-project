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

#if UNITY_EDITOR
        // --- editor-time safety ---
        private const float _curveOneEpsilon = 0.0005f;
        [System.NonSerialized] private bool _queuedCurveValidation;
        [System.NonSerialized] private int _lastCurveSignature;

        private void OnValidate()
        {
            // We only care about curve validation in the editor. Runtime safety is handled in EnemySpawner.
            if (SpawnCurve == null || SpawnCurve.length == 0)
                return;

            int sig = ComputeCurveSignature(SpawnCurve);
            if (sig == _lastCurveSignature)
                return;

            _lastCurveSignature = sig;

            // Delay so we validate once after the inspector finishes applying curve edits.
            if (_queuedCurveValidation)
                return;

            _queuedCurveValidation = true;
            UnityEditor.EditorApplication.delayCall += ValidateAndAutoFixCurveDelayed;
        }

        private void ValidateAndAutoFixCurveDelayed()
        {
            _queuedCurveValidation = false;
            if (this == null) return;

            if (SpawnCurve == null || SpawnCurve.length == 0)
                return;

            float end = Mathf.Clamp01(SpawnCurve.Evaluate(1f));
            if (end >= 1f - _curveOneEpsilon)
                return;

            Debug.LogError(
                $"[WaveConfigSO] '{name}' SpawnCurve does NOT reach 1 at t=1 (Evaluate(1)={end:0.###}). " +
                "This would leave enemies unspawned (and can stall the wave). Auto-fixing by forcing a key at (1,1).",
                this);

            ForceCurveToEndAtOne();
        }

        private void ForceCurveToEndAtOne()
        {
            if (SpawnCurve == null)
                SpawnCurve = new AnimationCurve();

            // If there's already a key at t=1, set its value to 1.
            // Otherwise, add a new key at t=1.
            int idxAtOne = -1;
            for (int i = 0; i < SpawnCurve.length; i++)
            {
                if (Mathf.Abs(SpawnCurve.keys[i].time - 1f) <= _curveOneEpsilon)
                {
                    idxAtOne = i;
                    break;
                }
            }

            if (idxAtOne >= 0)
            {
                var k = SpawnCurve.keys[idxAtOne];
                k.time = 1f;
                k.value = 1f;
                SpawnCurve.MoveKey(idxAtOne, k);
            }
            else
            {
                SpawnCurve.AddKey(new Keyframe(1f, 1f));
            }

            // Keep the curve predictable.
            SpawnCurve.postWrapMode = WrapMode.ClampForever;
            SpawnCurve.preWrapMode = WrapMode.ClampForever;

            UnityEditor.EditorUtility.SetDirty(this);
        }

        private static int ComputeCurveSignature(AnimationCurve curve)
        {
            unchecked
            {
                int h = 17;
                var keys = curve.keys;
                h = (h * 31) ^ keys.Length;
                for (int i = 0; i < keys.Length; i++)
                {
                    // Quantize to reduce noise from float precision jitter during editing.
                    int t = Mathf.RoundToInt(keys[i].time * 10000f);
                    int v = Mathf.RoundToInt(keys[i].value * 10000f);
                    h = (h * 31) ^ t;
                    h = (h * 31) ^ v;
                }
                return h;
            }
        }
#endif

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