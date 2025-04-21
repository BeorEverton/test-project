using System.IO;
using UnityEngine;

namespace Assets.Scripts.Systems.Save
{
    public static class SaveGameToFile
    {
        private const string FileName = "savegame.json";

        public static void SaveGameDataToFile(GameData gameData)
        {
            string json = JsonUtility.ToJson(gameData, prettyPrint: true);

            string path = Path.Combine(Application.persistentDataPath, FileName);

            File.WriteAllText(path, json);
            Debug.Log($"Game save to: {path}");
        }

        public static GameData LoadGameDataFromFile()
        {
            string path = Path.Combine(Application.persistentDataPath, FileName);
            if (!File.Exists(path))
                return null;

            string json = File.ReadAllText(path);
            GameData gameData = JsonUtility.FromJson<GameData>(json);
            Debug.Log($"Game loaded from: {path}");
            return gameData;
        }

        public static void DeleteSaveGameFile()
        {
            string path = Path.Combine(Application.persistentDataPath, FileName);
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"Game save deleted from: {path}");
            }
            else
            {
                Debug.LogWarning($"No save file found at: {path}");
            }
        }
    }
}