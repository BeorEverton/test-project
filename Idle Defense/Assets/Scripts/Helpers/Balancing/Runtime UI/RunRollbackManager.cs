using Assets.Scripts.Systems.Save;
using Assets.Scripts.WaveSystem;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

public class RunRollbackManager : MonoBehaviour
{
    private string _backupPath;

    // Call this at the start of a test run
    public void SnapshotPreRun()
    {
        // Try reflective SaveGameToFile first (if your project has it)
        // Otherwise, you can hardcode/assign your save path here.
        string savePath = TryGetSavePathReflective();
        if (string.IsNullOrEmpty(savePath) || !File.Exists(savePath))
        {
            Debug.LogWarning("[Rollback] Could not locate save file path. Snapshot skipped.");
            return;
        }

        _backupPath = savePath + ".balance_backup";
        File.Copy(savePath, _backupPath, true);
        Debug.Log("[Rollback] Snapshot created: " + _backupPath);
    }

    public void RestorePreRunAndReload()
    {
        string savePath = TryGetSavePathReflective();
        if (string.IsNullOrEmpty(savePath) || string.IsNullOrEmpty(_backupPath))
        {
            Debug.LogWarning("[Rollback] Missing paths; cannot restore.");
            return;
        }

        if (!File.Exists(_backupPath))
        {
            Debug.LogWarning("[Rollback] Backup not found: " + _backupPath);
            return;
        }

        File.Copy(_backupPath, savePath, true);
        Debug.Log("[Rollback] Restored backup -> save file. Now reloading...");

        // Prefer calling your SaveGameManager load flow if it exists.
        // If not, fallback to scene reload (you can swap this for your real boot flow).
        var sgm = SaveGameManager.Instance;
        if (sgm != null)
        {
            // Try common method names without requiring changes in SaveGameManager
            if (InvokeIfExists(sgm, "LoadGame")) return;
            if (InvokeIfExists(sgm, "Load")) return;
            if (InvokeIfExists(sgm, "LoadFromDisk")) return;
        }

        // Fallback: restart current wave (won’t reset money/upgrades if those are in-memory only).
        if (WaveManager.Instance != null)
        {
            int w = WaveManager.Instance.GetCurrentWaveIndex();
            WaveManager.Instance.RestartAtWave(w);
        }
    }

    private bool InvokeIfExists(object target, string methodName)
    {
        var m = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (m == null) return false;
        m.Invoke(target, null);
        return true;
    }

    private string TryGetSavePathReflective()
    {
        // Look for a SaveGameToFile type and common static path fields/properties.
        // If your save system differs, easiest is to replace this with your known path.
        var asm = typeof(SaveGameManager).Assembly;
        var t = asm.GetType("Assets.Scripts.Systems.Save.SaveGameToFile");
        if (t == null) return null;

        string[] candidates = { "SavePath", "SAVE_PATH", "FilePath", "PATH", "SaveFilePath" };

        foreach (var name in candidates)
        {
            var f = t.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(string))
                return (string)f.GetValue(null);

            var p = t.GetProperty(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.PropertyType == typeof(string))
                return (string)p.GetValue(null);
        }

        return null;
    }
}
