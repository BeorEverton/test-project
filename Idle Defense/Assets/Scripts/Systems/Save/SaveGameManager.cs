using Assets.Scripts.Enemies;
using Assets.Scripts.Systems.Audio;
using Assets.Scripts.Turrets;
using Assets.Scripts.UI;
using Assets.Scripts.WaveSystem;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.Systems.Save
{
    public class SaveGameManager : MonoBehaviour
    {
        public static SaveGameManager Instance { get; private set; }

        [SerializeField] private BaseTurret _machineGunTurret;
        [SerializeField] private BaseTurret _shotgunTurret;
        [SerializeField] private BaseTurret _sniperTurret;
        [SerializeField] private BaseTurret _missileLauncherTurret;
        [SerializeField] private BaseTurret _laserTurret;

        [Header("First-Time Disclaimer")]
        [SerializeField] private GameObject disclaimerPanel;
        private bool _disclaimerShown = true;


        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            LoadGame();
        }

        public void SaveGame()
        {
            int waveNumber = WaveManager.Instance.GetCurrentWaveIndex();

            GameDataDTO gameDataDTO = SaveDataDTOs.CreateGameDataDTO(waveNumber);

            PlayerInfoDTO sessionStatsDTO = SaveDataDTOs.CreatePlayerInfoDTO(PlayerBaseManager.Instance.Stats);
            PlayerInfoDTO permanentStatsDTO = SaveDataDTOs.CreatePlayerInfoDTO(PlayerBaseManager.Instance.PermanentStats);

            // Removed individual turret save calls — inventory handles it all now
            TurretInventoryDTO turretInventory = TurretInventoryManager.Instance.ExportToDTO();
            /*Debug.Log($"[SAVE] Exported {turretInventory.Owned?.Count} owned turrets. " +
                $"Equipped: {turretInventory.EquippedIds?.Count} ids, {turretInventory.EquippedRuntimeStats?.Count} runtime stats.");
            for (int i = 0; i < turretInventory.EquippedIds.Count; i++)
            {
                if (turretInventory.EquippedIds[i] >= 0)
                {
                    Debug.Log($"[SAVE] Slot {i} equipped with turret ID: {turretInventory.EquippedIds[i]}");
                }
            }*/

            StatsDTO statsDTO = SaveDataDTOs.CreateStatsDTO();

            var discoveredEnemies = EnemyLibraryManager.Instance.GetAllEntries()
                .Where(e => e.discovered)
                .Select(e => e.info.Name)
                .ToList();

            var gunnerInventory = GunnerManager.Instance.ExportToDTO();

            var prestigeDTO = SaveDataDTOs.CreatePrestigeDTO();

            GameData gameData = new(
                gameDataDTO,
                sessionStatsDTO,
                permanentStatsDTO,

                statsDTO,
                turretInventory,
                gunnerInventory,
                prestigeDTO
                );

            gameData.DiscoveredEnemyNames = discoveredEnemies;

            SaveGameToFile.SaveGameDataToFile(gameData);
        }

        public void LoadGame()
        {
            GameData gameData = SaveGameToFile.LoadGameDataFromFile();

            if (gameData == null)
            {
                ShowDisclaimerPanel();
                return;
            }

            PlayerBaseManager.Instance.SavedStats = LoadDataDTOs.CreatePlayerBaseSO(gameData.PlayerInfoDTO);
            if (gameData.PermanentPlayerInfoDTO != null)
            {
                PlayerBaseManager.Instance.SetPermanentStats(LoadDataDTOs.CreatePlayerBaseSO(gameData.PermanentPlayerInfoDTO));
            }

            foreach (var entry in gameData.GameDataDTO.Currencies ?? Array.Empty<CurrencyEntry>())
            {
                GameManager.Instance.LoadCurrency(entry.Currency, entry.Amount);
            }
            WaveManager.Instance?.LoadWave(gameData.GameDataDTO.WaveNumber);

            // Apply saved auto-advance mode (inverse of waveFailed).
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.SetAutoAdvanceEnabled(!gameData.GameDataDTO.waveFailed);
            }

            // Show/hide the manual advance button to match the saved state.
            UIManager.Instance?.ShowManualAdvanceButton(gameData.GameDataDTO.waveFailed, persist: false);


            GameTutorialManager.Instance?.LoadGame(gameData.GameDataDTO.TutorialStep);

            StatsManager.Instance?.LoadStats(gameData.StatsDTO);

            TurretInventoryManager.Instance?.ImportFromDTO(gameData.TurretInventory);

            GunnerManager.Instance?.ImportFromDTO(gameData.GunnerInventory);

            SettingsManager.Instance.SetMusicVolume(gameData.GameDataDTO.MusicVolume);
            SettingsManager.Instance.SetSFXVolume(gameData.GameDataDTO.SFXVolume);
            SettingsManager.Instance.Mute = gameData.GameDataDTO.MuteAll;
            SettingsManager.Instance.AllowPopups = gameData.GameDataDTO.PopupsEnabled;
            SettingsManager.Instance.AllowTooltips = gameData.GameDataDTO.TooltipsEnabled;

            AudioManager.Instance.audioMixer.SetFloat("Music", gameData.GameDataDTO.MusicVolume);
            AudioManager.Instance.audioMixer.SetFloat("SFX", gameData.GameDataDTO.SFXVolume);

            foreach (var entry in gameData.GameDataDTO.Currencies)
            {
                GameManager.Instance.LoadCurrency(entry.Currency, entry.Amount);
            }

            if (gameData.DiscoveredEnemyNames != null)
            {
                foreach (string enemyName in gameData.DiscoveredEnemyNames)
                {
                    EnemyLibraryManager.Instance.MarkAsDiscovered(enemyName, true);
                }
            }

            if (gameData.Prestige != null)
            {
                if (PrestigeManager.Instance != null)
                {
                    // Load prestige first so discounts/unlocks are ready
                    PrestigeManager.Instance.ImportDTO(gameData.Prestige);
                }
                else
                {
                    Debug.LogWarning("[SaveGameManager] Prestige data found but no PrestigeManager in scene.");
                }
            }

        }

        public void DeleteSave()
        {
            SaveGameToFile.DeleteSaveGameFile();
            PlayerBaseManager.Instance.ResetPlayerBase();
            GameManager.Instance.ResetGame();
            WaveManager.Instance.ResetWave();
            StatsManager.Instance.ResetStats();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void ShowDisclaimerPanel()
        {
            if (disclaimerPanel != null && !_disclaimerShown)
            {
                disclaimerPanel.SetActive(true);
                Time.timeScale = 0f; // Pause game
                _disclaimerShown = true;
            }
        }

        public void CloseDisclaimerPanel()
        {
            if (disclaimerPanel != null)
            {
                disclaimerPanel.SetActive(false);
            }
        }
    }
}