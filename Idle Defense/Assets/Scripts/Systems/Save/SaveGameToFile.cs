using UnityEngine;

namespace Assets.Scripts.Systems.Save
{
    public static class SaveGameToFile
    {
        private const string FileName = "savegame.json";
        private const string SaveKey = "SaveGameData";

        public static void SaveGameDataToFile(GameData gameData)
        {
            string json = JsonUtility.ToJson(gameData, prettyPrint: true);

            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();

            Debug.Log($"Game save to PlayerPrefs");
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
    }
}