using Assets.Scripts.Enemies;
using Assets.Scripts.SO;
using Assets.Scripts.WaveSystem;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class BalanceHUD : MonoBehaviour
{
    public int CurrentWaveIndex { get; private set; }
    public int EnemiesSpawnedSoFar { get; private set; }
    public int EnemiesTotalThisWave { get; private set; }
    public int EnemiesAlive { get; private set; }

    public float NextSpawnInSeconds { get; private set; } // NEW

    private EnemySpawner _spawner;
    private float _waveStartTime;

    private void Start()
    {
        _spawner = EnemySpawner.Instance != null ? EnemySpawner.Instance : FindFirstObjectByType<EnemySpawner>();

        if (_spawner != null)
        {
            _spawner.OnWaveStarted += (_, __) => { _waveStartTime = Time.time; };
            _spawner.OnWaveCreated += (_, e) => { EnemiesTotalThisWave = e.EnemyCount; }; // EnemyCount is published by spawner【turn20file7†L91-L93】
        }

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted += (_, e) =>
            {
                CurrentWaveIndex = e.WaveNumber;
                EnemiesSpawnedSoFar = 0;
                NextSpawnInSeconds = 0f;
            };
        }
    }

    private void Update()
    {
        if (_spawner == null) return;

        var alive = _spawner.EnemiesAlive;
        EnemiesAlive = alive != null ? alive.Count : 0;

        // spawnedSoFar = total - remainingBufferCount (buffer shrinks as spawner removes entries to spawn)【turn20file2†L76-L80】
        int remaining = GetEnemiesCurrentWaveCountReflective(_spawner);
        if (EnemiesTotalThisWave > 0)
            EnemiesSpawnedSoFar = Mathf.Clamp(EnemiesTotalThisWave - remaining, 0, EnemiesTotalThisWave);

        NextSpawnInSeconds = ComputeNextSpawnSeconds();
    }

    private int GetEnemiesCurrentWaveCountReflective(EnemySpawner spawner)
    {
        var f = spawner.GetType().GetField("_enemiesCurrentWave", BindingFlags.Instance | BindingFlags.NonPublic);
        if (f == null) return 0;

        var list = f.GetValue(spawner) as System.Collections.ICollection;
        return list != null ? list.Count : 0;
    }

    private float ComputeNextSpawnSeconds()
    {
        WaveConfigSO cfg = GetCurrentWaveConfigReflective(_spawner);
        if (cfg == null || EnemiesTotalThisWave <= 0) return 0f;

        float spawnDuration = GetSpawnDuration(cfg, EnemiesTotalThisWave);
        if (spawnDuration <= 0f) return 0f;

        AnimationCurve curve = GetCurve(cfg);
        float elapsed = Mathf.Max(0f, Time.time - _waveStartTime);
        float tNow = Mathf.Clamp01(elapsed / spawnDuration);

        int spawnedCount = EnemiesSpawnedSoFar;

        // Find the earliest future time where targetSpawned increases by at least 1.
        // This approximates "next burst" because steep curve segments will produce short deltas.
        float bestT = -1f;
        const int steps = 240; // cheap and stable (about 4 seconds granularity at 60fps over whole wave)
        for (int i = 1; i <= steps; i++)
        {
            float t = Mathf.Lerp(tNow, 1f, i / (float)steps);
            float frac = Mathf.Clamp01(curve.Evaluate(t));
            int target = Mathf.Clamp(Mathf.RoundToInt(frac * EnemiesTotalThisWave), 0, EnemiesTotalThisWave);

            if (target > spawnedCount)
            {
                bestT = t;
                break;
            }
        }

        if (bestT < 0f) return 0f;

        float nextElapsed = bestT * spawnDuration;
        return Mathf.Max(0f, nextElapsed - elapsed);
    }

    private WaveConfigSO GetCurrentWaveConfigReflective(EnemySpawner spawner)
    {
        var f = spawner.GetType().GetField("_currentWaveConfig", BindingFlags.Instance | BindingFlags.NonPublic);
        return f != null ? f.GetValue(spawner) as WaveConfigSO : null;
    }

    private float GetSpawnDuration(WaveConfigSO cfg, int totalEnemies)
    {
        float minSpawnDuration = 3f;
        var fMin = _spawner.GetType().GetField("_minSpawnDuration", BindingFlags.Instance | BindingFlags.NonPublic);
        if (fMin != null && fMin.FieldType == typeof(float))
            minSpawnDuration = (float)fMin.GetValue(_spawner);

        if (cfg.TotalSpawnDuration > 0f)
            return Mathf.Max(minSpawnDuration, cfg.TotalSpawnDuration); // matches spawner logic【turn20file2†L17-L20】

        float guess = cfg.TimeBetweenSpawns * totalEnemies; // matches fallback logic【turn20file2†L23-L26】
        return Mathf.Max(minSpawnDuration, guess);
    }

    private AnimationCurve GetCurve(WaveConfigSO cfg)
    {
        if (cfg.SpawnCurve != null && cfg.SpawnCurve.length >= 2)
            return cfg.SpawnCurve;

        return AnimationCurve.Linear(0f, 0f, 1f, 1f); // matches spawner fallback【turn20file2†L37-L41】
    }
}
