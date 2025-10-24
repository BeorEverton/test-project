using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.Systems.Save
{
    public static class SaveGameToFile
    {
        private const string SaveKey = "SaveGameData";

        public static void SaveGameDataToFile(GameData gameData)
        {
            string json = JsonUtility.ToJson(gameData, prettyPrint: true);

            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();


        }

        public static GameData LoadGameDataFromFile()
        {
            if (!PlayerPrefs.HasKey(SaveKey))
                return null;

            string json = PlayerPrefs.GetString(SaveKey);
            GameData gameData = JsonUtility.FromJson<GameData>(json);

            Debug.Log($"Game loaded from PlayerPrefs");
            return gameData;
        }

        public static void DeleteSaveGameFile()
        {
            if (PlayerPrefs.HasKey(SaveKey))
            {
                PlayerPrefs.DeleteKey(SaveKey);
                Debug.Log("Game save deleted from PlayerPrefs");
            }
            else
                Debug.LogWarning("No save file found in PlayerPrefs");
        }


        // Returns the raw JSON currently stored (or null if none)
        public static string GetRawJson()
        {
            const string SaveKey = "SaveGameData"; // keep in sync with your existing const
            return PlayerPrefs.HasKey(SaveKey) ? PlayerPrefs.GetString(SaveKey) : null;
        }

        // Validates and writes JSON into PlayerPrefs, then PlayerPrefs.Save().
        public static bool TryImportRawJson(string rawJson, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                error = "Empty JSON.";
                return false;
            }

            try
            {
                var parsed = JsonUtility.FromJson<GameData>(rawJson);
                if (parsed == null)
                {
                    error = "JSON parsed to null GameData.";
                    return false;
                }
                if (parsed.GameDataDTO == null)
                {
                    error = "Missing GameDataDTO.";
                    return false;
                }

                const string SaveKey = "SaveGameData";
                PlayerPrefs.SetString(SaveKey, rawJson);
                PlayerPrefs.Save();
                return true;
            }
            catch (Exception ex)
            {
                error = $"Invalid or incompatible JSON: {ex.Message}";
                return false;
            }
        }

        // ----- Optional package support -----
        [Serializable] private class ExportPackage { public int version; public string exportedAt; public string checksum; public string data; }

        public static string WrapAsPackage(string plainJson)
        {
            var pkg = new ExportPackage
            {
                version = 1,
                exportedAt = DateTime.UtcNow.ToString("o"),
                checksum = ComputeSha256(plainJson),
                data = plainJson
            };
            return JsonUtility.ToJson(pkg, true);
        }

        private static string TryExtractPackage(string maybePackageJson, out string checksum)
        {
            checksum = null;
            try
            {
                var pkg = JsonUtility.FromJson<ExportPackage>(maybePackageJson);
                if (pkg != null && !string.IsNullOrEmpty(pkg.data))
                {
                    // (Optional) verify checksum
                    var computed = ComputeSha256(pkg.data);
                    checksum = computed;
                    // We don't hard-fail if checksum mismatch; you can add strictness here.
                    return pkg.data;
                }
            }
            catch { /* not a package */ }
            return null;
        }

        private static string ComputeSha256(string s)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

    }
}