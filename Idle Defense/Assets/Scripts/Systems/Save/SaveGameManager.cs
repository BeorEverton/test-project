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
            // 1) Wipe the persisted save
            SaveGameToFile.DeleteSaveGameFile();

            // 2) Clear runtime systems to pristine state
            // Player base (session + visuals)
            PlayerBaseManager.Instance?.ResetPlayerBase();
            PlayerBaseManager.Instance?.InitializeGame(usePermanentStats: true);

            // Core game state & currencies
            GameManager.Instance?.ResetGame();

            // Waves
            WaveManager.Instance?.ResetWave();

            // Stats
            StatsManager.Instance?.ResetStats();

            TurretSlotManager.Instance?.UnequipAll(autoEquipStarter: false);
            // Turrets: wipe ownership & upgrades and DO NOT auto-equip (let inventory seed starter where it wants)
            if (TurretInventoryManager.Instance != null)
            {
                TurretInventoryManager.Instance.ResetAll(wipeOwnership: true, wipeUpgrades: true);
            }

            // Gunners: clear preferred starter first so nothing is auto-kept/equipped
            if (GunnerManager.Instance != null)
            {
                GunnerManager.Instance.SetPreferredStarter(null);
                GunnerManager.Instance.ResetAll(
                    wipeOwnership: true,
                    wipeUpgrades: true,
                    resetLevels: true
                );
            }

            // Prestige: wipe meta
            if (PrestigeManager.Instance != null)
            {
                var empty = new PrestigeDTO
                {
                    PrestigeLevel = 0,                    
                    OwnedNodeIds = new System.Collections.Generic.List<string>()
                };
                PrestigeManager.Instance.ImportDTO(empty);
            }

            // Enemy Library (discovered list)
            if (EnemyLibraryManager.Instance != null)
            {
                foreach (var e in EnemyLibraryManager.Instance.GetAllEntries())
                    EnemyLibraryManager.Instance.MarkAsDiscovered(e.info.Name, false);
            }

            // 3) Clear PlayerPrefs toggles that skip onboarding/selection
            // - Starter selection UI flag
            PlayerPrefs.DeleteKey("StarterSelectionDone");
            // - Preferred starter id (legacy cache; export/import handles the real value)
            PlayerPrefs.DeleteKey("PreferredStarterGunnerId");
            PlayerPrefs.Save();

            // 4) Safety: ensure we come back unpaused
            Time.timeScale = 1f;

            // 5) Reload the active scene
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
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