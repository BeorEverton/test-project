// SaveExportImport.cs
using System;
using System.IO;
using UnityEngine;
using Assets.Scripts.Systems.Save;
using SFB; // StandaloneFileBrowser

public class SaveExportImport : MonoBehaviour
{
    [Tooltip("Default file name when exporting to folder")]
    [SerializeField] private string defaultFileName = "Idle_Resistance";

    // --- EXPORT: Save As (file dialog) ---
    public void Export_SaveAs()
    {
        EnsureUpToDateSave();

        string json = SaveGameToFile.GetRawJson();
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("No save found to export.");
            return;
        }

        string path = StandaloneFileBrowser.SaveFilePanel(
            "Export Save",              // title
            "",                         // initial directory
            $"{defaultFileName}_{DateTime.Now:yyyy-MM-dd_HHmmss}",
            "json"                      // extension filter
        );

        if (string.IsNullOrEmpty(path)) return; // user canceled

        try
        {
            File.WriteAllText(path, json);
            Debug.Log($"[Export] Wrote save to: {path}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Export] Failed to write file: {ex.Message}");
        }
    }

    // --- EXPORT: Choose Folder (auto filename) ---
    public void Export_ToFolder()
    {
        EnsureUpToDateSave();

        string json = SaveGameToFile.GetRawJson();
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("No save found to export.");
            return;
        }

        var folders = StandaloneFileBrowser.OpenFolderPanel(
            "Exporting data of Idle Resistance, choose where to save it.", "", false
        );
        if (folders == null || folders.Length == 0) return; // canceled

        string dir = folders[0];
        string file = Path.Combine(dir, $"{defaultFileName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json");

        try
        {
            File.WriteAllText(file, json);
            Debug.Log($"[Export] Wrote save to: {file}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Export] Failed to write file: {ex.Message}");
        }
    }

    // --- IMPORT: Open File (file dialog) ---
    public void Import_OpenFile()
    {
        var paths = StandaloneFileBrowser.OpenFilePanel(
            "Imporing data for Idle Resistance, choose the save file.", "", "json", false
        );
        if (paths == null || paths.Length == 0) return; // canceled

        string path = paths[0];
        if (!File.Exists(path))
        {
            Debug.LogError("[Import] File not found.");
            return;
        }

        try
        {
            string raw = File.ReadAllText(path);
            if (SaveGameToFile.TryImportRawJson(raw, out string err))
            {
                Debug.Log($"[Import] Imported from: {path}");
                ReloadGame();
            }
            else
            {
                Debug.LogError($"[Import] Failed: {err}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Import] Failed to read or parse: {ex.Message}");
        }
    }

    // --- Helpers ---
    private void EnsureUpToDateSave()
    {
        if (SaveGameManager.Instance != null)
            SaveGameManager.Instance.SaveGame();
    }

    private void ReloadGame()
    {   
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        Time.timeScale = 1f;
    }
}
