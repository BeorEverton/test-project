using Assets.Scripts.SO;
using Assets.Scripts.Turrets;
using Assets.Scripts.WaveSystem;
using UnityEngine;

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
            TurretInfoDTO shotgunTurretDTO = SaveDataDTOs.CreateTurretInfoDTO(_shotgunTurret.GetStats());
            TurretInfoDTO sniperTurretDTO = SaveDataDTOs.CreateTurretInfoDTO(_sniperTurret.GetStats());
            TurretInfoDTO missileLauncherTurretDTO = SaveDataDTOs.CreateTurretInfoDTO(_missileLauncherTurret.GetStats());
            TurretInfoDTO laserTurretDTO = SaveDataDTOs.CreateTurretInfoDTO(_laserTurret.GetStats());

            GameData gameData = new(gameDataDTO, playerInfoDTO, machineGunTurretDTO, shotgunTurretDTO, sniperTurretDTO, missileLauncherTurretDTO, laserTurretDTO);

            SaveGameToFile.SaveGameDataToFile(gameData);
        }

        public void LoadGame()
        {
            GameData gameData = SaveGameToFile.LoadGameDataFromFile();

            if (gameData == null)
                return;

            _machineGunTurret.SavedStats = LoadDataDTOs.CreateTurretStatsInstance(gameData.MachineGunTurretInfoDTO);
            _shotgunTurret.SavedStats = LoadDataDTOs.CreateTurretStatsInstance(gameData.ShotgunTurretInfoDTO);
            _sniperTurret.SavedStats = LoadDataDTOs.CreateTurretStatsInstance(gameData.SniperTurretInfoDTO);
            _missileLauncherTurret.SavedStats = LoadDataDTOs.CreateTurretStatsInstance(gameData.MissileLauncherTurretInfoDTO);
            _laserTurret.SavedStats = LoadDataDTOs.CreateTurretStatsInstance(gameData.LaserTurretInfoDTO);

            PlayerBaseManager.Instance.LoadPlayerBase(LoadDataDTOs.CreatePlayerBaseSO(gameData.PlayerInfoDTO));

            GameManager.Instance.LoadMoney(gameData.GameDataDTO.Money);
            WaveManager.Instance.LoadWave(gameData.GameDataDTO.WaveNumber);
        }
    }
}