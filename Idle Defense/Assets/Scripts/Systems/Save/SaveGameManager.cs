using Assets.Scripts.Enemies;
using Assets.Scripts.SO;
using Assets.Scripts.Systems.Audio;
using Assets.Scripts.Turrets;
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

            // TEMPORARY STATS (Scrap upgrades)
            var machineGunRuntime = TurretSlotManager.Instance.GetRuntimeStatsOfType(TurretType.MachineGun);
            Debug.Log($"Machine Gun Runtime Stats: {machineGunRuntime?.Damage}");
            TurretInfoDTO machineGunTurretDTO = SaveDataDTOs.CreateTurretInfoDTO(machineGunRuntime);
            TurretBaseInfoDTO machineGunTurretBaseDTO = SaveDataDTOs.CreateTurretBaseInfoDTO(machineGunRuntime);

            Debug.Log("Saved machine gun with damage: " + machineGunTurretDTO.Damage);

            TurretInfoDTO shotgunTurretDTO = SaveDataDTOs.CreateTurretInfoDTO(_shotgunTurret.RuntimeStats);
            TurretBaseInfoDTO shotgunTurretBaseDTO = SaveDataDTOs.CreateTurretBaseInfoDTO(_shotgunTurret.RuntimeStats);

            TurretInfoDTO sniperTurretDTO = SaveDataDTOs.CreateTurretInfoDTO(_sniperTurret.RuntimeStats);
            TurretBaseInfoDTO sniperTurretBaseDTO = SaveDataDTOs.CreateTurretBaseInfoDTO(_sniperTurret.RuntimeStats);

            TurretInfoDTO missileLauncherTurretDTO = SaveDataDTOs.CreateTurretInfoDTO(_missileLauncherTurret.RuntimeStats);
            TurretBaseInfoDTO missileLauncherTurretBaseDTO = SaveDataDTOs.CreateTurretBaseInfoDTO(_missileLauncherTurret.RuntimeStats);

            TurretInfoDTO laserTurretDTO = SaveDataDTOs.CreateTurretInfoDTO(_laserTurret.RuntimeStats);
            TurretBaseInfoDTO laserTurretBaseDTO = SaveDataDTOs.CreateTurretBaseInfoDTO(_laserTurret.RuntimeStats);

            // PERMANENT STATS (BlackSteel upgrades)
            TurretInfoDTO machineGunPermanentDTO = SaveDataDTOs.CreateTurretInfoDTO(_machineGunTurret.PermanentStats);
            TurretBaseInfoDTO machineGunPermanentBaseDTO = SaveDataDTOs.CreateTurretBaseInfoDTO(_machineGunTurret.PermanentStats);

            TurretInfoDTO shotgunPermanentDTO = SaveDataDTOs.CreateTurretInfoDTO(_shotgunTurret.PermanentStats);
            TurretBaseInfoDTO shotgunPermanentBaseDTO = SaveDataDTOs.CreateTurretBaseInfoDTO(_shotgunTurret.PermanentStats);

            TurretInfoDTO sniperPermanentDTO = SaveDataDTOs.CreateTurretInfoDTO(_sniperTurret.PermanentStats);
            TurretBaseInfoDTO sniperPermanentBaseDTO = SaveDataDTOs.CreateTurretBaseInfoDTO(_sniperTurret.PermanentStats);

            TurretInfoDTO missileLauncherPermanentDTO = SaveDataDTOs.CreateTurretInfoDTO(_missileLauncherTurret.PermanentStats);
            TurretBaseInfoDTO missileLauncherPermanentBaseDTO = SaveDataDTOs.CreateTurretBaseInfoDTO(_missileLauncherTurret.PermanentStats);

            TurretInfoDTO laserPermanentDTO = SaveDataDTOs.CreateTurretInfoDTO(_laserTurret.PermanentStats);
            TurretBaseInfoDTO laserPermanentBaseDTO = SaveDataDTOs.CreateTurretBaseInfoDTO(_laserTurret.PermanentStats);

            StatsDTO statsDTO = SaveDataDTOs.CreateStatsDTO();            

            TurretInventoryDTO turretInventory = TurretInventoryManager.Instance.ExportToDTO();

            var discoveredEnemies = EnemyLibraryManager.Instance.GetAllEntries()
                .Where(e => e.discovered)
                .Select(e => e.info.Name)
                .ToList();

            GameData gameData = new(
                gameDataDTO,
                sessionStatsDTO,
                permanentStatsDTO,
                machineGunTurretDTO,
                machineGunTurretBaseDTO,
                shotgunTurretDTO,
                shotgunTurretBaseDTO,
                sniperTurretDTO,
                sniperTurretBaseDTO,
                missileLauncherTurretDTO,
                missileLauncherTurretBaseDTO,
                laserTurretDTO,
                laserTurretBaseDTO,
                statsDTO,
                turretInventory,                
                machineGunPermanentDTO,
                machineGunPermanentBaseDTO,
                shotgunPermanentDTO,
                shotgunPermanentBaseDTO,
                sniperPermanentDTO,
                sniperPermanentBaseDTO,
                missileLauncherPermanentDTO,
                missileLauncherPermanentBaseDTO,
                laserPermanentDTO,
                laserPermanentBaseDTO);

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

            if (gameData.MachineGunTurretInfoDTO != null)
            {
                _machineGunTurret.RuntimeStats = LoadDataDTOs.CreateTurretStatsInstance(gameData.MachineGunTurretInfoDTO, gameData.MachineGunTurretBaseInfoDTO);
                Debug.Log($"Loaded Machine Gun Turret: {_machineGunTurret.RuntimeStats.Damage}");
            }
            if (gameData.ShotgunTurretInfoDTO != null)
                _shotgunTurret.RuntimeStats = LoadDataDTOs.CreateTurretStatsInstance(gameData.ShotgunTurretInfoDTO, gameData.ShotgunTurretBaseInfoDTO);
            if (gameData.SniperTurretInfoDTO != null)
                _sniperTurret.RuntimeStats = LoadDataDTOs.CreateTurretStatsInstance(gameData.SniperTurretInfoDTO, gameData.SniperTurretBaseInfoDTO);
            if (gameData.MissileLauncherTurretInfoDTO != null)
                _missileLauncherTurret.RuntimeStats = LoadDataDTOs.CreateTurretStatsInstance(gameData.MissileLauncherTurretInfoDTO, gameData.MissileLauncherTurretBaseInfoDTO);
            if (gameData.LaserTurretInfoDTO != null)
                _laserTurret.RuntimeStats = LoadDataDTOs.CreateTurretStatsInstance(gameData.LaserTurretInfoDTO, gameData.LaserTurretBaseInfoDTO);

            if (gameData.MachineGunPermanentDTO != null)
                _machineGunTurret.PermanentStats = LoadDataDTOs.CreateTurretStatsInstance(gameData.MachineGunPermanentDTO, gameData.MachineGunPermanentBaseDTO);
            if (gameData.ShotgunPermanentDTO != null)
                _shotgunTurret.PermanentStats = LoadDataDTOs.CreateTurretStatsInstance(gameData.ShotgunPermanentDTO, gameData.ShotgunPermanentBaseDTO);
            if (gameData.SniperPermanentDTO != null)
                _sniperTurret.PermanentStats = LoadDataDTOs.CreateTurretStatsInstance(gameData.SniperPermanentDTO, gameData.SniperPermanentBaseDTO);
            if (gameData.MissileLauncherPermanentDTO != null)
                _missileLauncherTurret.PermanentStats = LoadDataDTOs.CreateTurretStatsInstance(gameData.MissileLauncherPermanentDTO, gameData.MissileLauncherPermanentBaseDTO);
            if (gameData.LaserPermanentDTO != null)
                _laserTurret.SetPermanentStats(LoadDataDTOs.CreateTurretStatsInstance(gameData.LaserPermanentDTO, gameData.LaserPermanentBaseDTO));

            PlayerBaseManager.Instance.SavedStats = LoadDataDTOs.CreatePlayerBaseSO(gameData.PlayerInfoDTO);
            if (gameData.PermanentPlayerInfoDTO != null)
            {
                PlayerBaseManager.Instance.SetPermanentStats(LoadDataDTOs.CreatePlayerBaseSO(gameData.PermanentPlayerInfoDTO));
            }

            foreach (var entry in gameData.GameDataDTO.Currencies ?? Array.Empty<CurrencyEntry>())
            {
                GameManager.Instance.LoadCurrency(entry.Currency, entry.Amount);
            }


            WaveManager.Instance.LoadWave(gameData.GameDataDTO.WaveNumber);
            GameTutorialManager.Instance.LoadGame(gameData.GameDataDTO.TutorialStep);

            StatsManager.Instance.LoadStats(gameData.StatsDTO);

            TurretInventoryManager.Instance.ImportFromDTO(gameData.TurretInventory);

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