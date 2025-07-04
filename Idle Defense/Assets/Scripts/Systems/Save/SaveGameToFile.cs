using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

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

            if (CrazyGames.CrazySDK.IsInitialized)
            {
                CrazyGames.CrazySDK.Data.SetString(SaveKey, json);
            }

            Debug.Log($"Game save to PlayerPrefs");
        }

        public static GameData LoadGameDataFromFile()
        {
            string json = "";
            GameData gameData;
            if (CrazyGames.CrazySDK.IsInitialized)
            {
                if (CrazyGames.CrazySDK.Data.HasKey(SaveKey))
                {
                    json = CrazyGames.CrazySDK.Data.GetString(SaveKey);
                    gameData = JsonUtility.FromJson<GameData>(json);
                    return gameData;
                }
            }
            if (!PlayerPrefs.HasKey(SaveKey))
                return null;

            json = PlayerPrefs.GetString(SaveKey);
            gameData = JsonUtility.FromJson<GameData>(json);

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