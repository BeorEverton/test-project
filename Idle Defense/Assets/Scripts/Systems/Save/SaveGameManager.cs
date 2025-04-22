using Assets.Scripts.SO;
using Assets.Scripts.Turrets;
using Assets.Scripts.WaveSystem;
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

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            LoadGame();
        }

        public void SaveGame()
        {
            int waveNumber = WaveManager.Instance.GetCurrentWaveIndex();
            ulong money = GameManager.Instance.Money;
            GameDataDTO gameDataDTO = SaveDataDTOs.CreateGameDataDTO(waveNumber, money);

            PlayerInfoDTO playerInfoDTO = SaveDataDTOs.CreatePlayerInfoDTO(PlayerBaseManager.Instance.Info);

            TurretInfoDTO machineGunTurretDTO = SaveDataDTOs.CreateTurretInfoDTO(_machineGunTurret.GetStats());
            TurretBaseInfoDTO machineGunTurretBaseDTO = SaveDataDTOs.CreateTurretBaseInfoDTO(_machineGunTurret.GetStats());
            TurretInfoDTO shotgunTurretDTO = SaveDataDTOs.CreateTurretInfoDTO(_shotgunTurret.GetStats());
            TurretBaseInfoDTO shotgunTurretBaseDTO = SaveDataDTOs.CreateTurretBaseInfoDTO(_machineGunTurret.GetStats());
            TurretInfoDTO sniperTurretDTO = SaveDataDTOs.CreateTurretInfoDTO(_sniperTurret.GetStats());
            TurretBaseInfoDTO sniperTurretBaseDTO = SaveDataDTOs.CreateTurretBaseInfoDTO(_machineGunTurret.GetStats());
            TurretInfoDTO missileLauncherTurretDTO = SaveDataDTOs.CreateTurretInfoDTO(_missileLauncherTurret.GetStats());
            TurretBaseInfoDTO missileLauncherTurretBaseDTO = SaveDataDTOs.CreateTurretBaseInfoDTO(_machineGunTurret.GetStats());
            TurretInfoDTO laserTurretDTO = SaveDataDTOs.CreateTurretInfoDTO(_laserTurret.GetStats());
            TurretBaseInfoDTO laserTurretBaseDTO = SaveDataDTOs.CreateTurretBaseInfoDTO(_machineGunTurret.GetStats());

            GameData gameData = new(gameDataDTO,
                playerInfoDTO,
                machineGunTurretDTO,
                machineGunTurretBaseDTO,
                shotgunTurretDTO,
                shotgunTurretBaseDTO,
                sniperTurretDTO,
                sniperTurretBaseDTO,
                missileLauncherTurretDTO,
                missileLauncherTurretBaseDTO,
                laserTurretDTO,
                laserTurretBaseDTO);

            SaveGameToFile.SaveGameDataToFile(gameData);
        }

        public void LoadGame()
        {
            GameData gameData = SaveGameToFile.LoadGameDataFromFile();

            if (gameData == null)
                return;

            _machineGunTurret.SavedStats = LoadDataDTOs.CreateTurretStatsInstance(gameData.MachineGunTurretInfoDTO, gameData.MachineGunTurretBaseInfoDTO);
            _shotgunTurret.SavedStats = LoadDataDTOs.CreateTurretStatsInstance(gameData.ShotgunTurretInfoDTO, gameData.ShotgunTurretBaseInfoDTO);
            _sniperTurret.SavedStats = LoadDataDTOs.CreateTurretStatsInstance(gameData.SniperTurretInfoDTO, gameData.SniperTurretBaseInfoDTO);
            _missileLauncherTurret.SavedStats = LoadDataDTOs.CreateTurretStatsInstance(gameData.MissileLauncherTurretInfoDTO, gameData.MissileLauncherTurretBaseInfoDTO);
            _laserTurret.SavedStats = LoadDataDTOs.CreateTurretStatsInstance(gameData.LaserTurretInfoDTO, gameData.LaserTurretBaseInfoDTO);

            PlayerBaseManager.Instance.LoadPlayerBase(LoadDataDTOs.CreatePlayerBaseSO(gameData.PlayerInfoDTO));

            GameManager.Instance.LoadMoney(gameData.GameDataDTO.Money);
            WaveManager.Instance.LoadWave(gameData.GameDataDTO.WaveNumber);
        }

        public void DeleteSave()
        {
            SaveGameToFile.DeleteSaveGameFile();
            PlayerBaseManager.Instance.ResetPlayerBase();
            GameManager.Instance.ResetGame();
            WaveManager.Instance.ResetWave();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}