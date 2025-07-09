using System;
using System.Collections.Generic;

namespace Assets.Scripts.Systems.Save
{
    [Serializable]
    public class GameData
    {
        public GameDataDTO GameDataDTO;
        public PlayerInfoDTO PlayerInfoDTO;
        public PlayerInfoDTO PermanentPlayerInfoDTO;
        public TurretInfoDTO MachineGunTurretInfoDTO;
        public TurretBaseInfoDTO MachineGunTurretBaseInfoDTO;
        public TurretInfoDTO? ShotgunTurretInfoDTO;
        public TurretBaseInfoDTO? ShotgunTurretBaseInfoDTO;
        public TurretInfoDTO? SniperTurretInfoDTO;
        public TurretBaseInfoDTO? SniperTurretBaseInfoDTO;
        public TurretInfoDTO? MissileLauncherTurretInfoDTO;
        public TurretBaseInfoDTO? MissileLauncherTurretBaseInfoDTO;
        public TurretInfoDTO? LaserTurretInfoDTO;
        public TurretBaseInfoDTO? LaserTurretBaseInfoDTO;
        public StatsDTO StatsDTO;
        public TurretInventoryDTO TurretInventory;
        public TurretInfoDTO MachineGunPermanentDTO;
        public TurretBaseInfoDTO MachineGunPermanentBaseDTO;
        public TurretInfoDTO ShotgunPermanentDTO;
        public TurretBaseInfoDTO ShotgunPermanentBaseDTO;
        public TurretInfoDTO SniperPermanentDTO;
        public TurretBaseInfoDTO SniperPermanentBaseDTO;
        public TurretInfoDTO MissileLauncherPermanentDTO;
        public TurretBaseInfoDTO MissileLauncherPermanentBaseDTO;
        public TurretInfoDTO LaserPermanentDTO;
        public TurretBaseInfoDTO LaserPermanentBaseDTO;    

        public List<string> DiscoveredEnemyNames;

        public GameData(GameDataDTO gameData,
            PlayerInfoDTO playerInfo,
            PlayerInfoDTO permanentPlayerInfo,
            TurretInfoDTO machineGunTurretInfoDTO,
            TurretBaseInfoDTO machineGunTurretBaseInfoDTO,
            TurretInfoDTO? shotgunTurretInfoDTO,
            TurretBaseInfoDTO? shotgunTurretBaseInfoDTO,
            TurretInfoDTO? sniperTurretInfoDTO,
            TurretBaseInfoDTO? sniperTurretBaseInfoDTO,
            TurretInfoDTO? missileLauncherTurretInfoDTO,
            TurretBaseInfoDTO? missileLauncherTurretBaseInfoDTO,
            TurretInfoDTO? laserTurretInfoDTO,
            TurretBaseInfoDTO? laserTurretBaseInfoDTO,
            StatsDTO statsDTO,
            TurretInventoryDTO turretInventory,
            TurretInfoDTO machineGunPermanentDTO,
            TurretBaseInfoDTO machineGunPermanentBaseDTO,
            TurretInfoDTO shotgunPermanentDTO,
            TurretBaseInfoDTO shotgunPermanentBaseDTO,
            TurretInfoDTO sniperPermanentDTO,
            TurretBaseInfoDTO sniperPermanentBaseDTO,
            TurretInfoDTO missileLauncherPermanentDTO,
            TurretBaseInfoDTO missileLauncherPermanentBaseDTO,
            TurretInfoDTO laserPermanentDTO,
            TurretBaseInfoDTO laserPermanentBaseDTO)
        {
            GameDataDTO = gameData;
            PlayerInfoDTO = playerInfo;
            PermanentPlayerInfoDTO = permanentPlayerInfo;
            MachineGunTurretInfoDTO = machineGunTurretInfoDTO;
            MachineGunTurretBaseInfoDTO = machineGunTurretBaseInfoDTO;
            ShotgunTurretInfoDTO = shotgunTurretInfoDTO;
            ShotgunTurretBaseInfoDTO = shotgunTurretBaseInfoDTO;
            SniperTurretInfoDTO = sniperTurretInfoDTO;
            SniperTurretBaseInfoDTO = sniperTurretBaseInfoDTO;
            MissileLauncherTurretInfoDTO = missileLauncherTurretInfoDTO;
            MissileLauncherTurretBaseInfoDTO = missileLauncherTurretBaseInfoDTO;
            LaserTurretInfoDTO = laserTurretInfoDTO;
            LaserTurretBaseInfoDTO = laserTurretBaseInfoDTO;            
            StatsDTO = statsDTO;           
            TurretInventory = turretInventory;
            MachineGunPermanentDTO = machineGunPermanentDTO;
            MachineGunPermanentBaseDTO = machineGunPermanentBaseDTO;
            ShotgunPermanentDTO = shotgunPermanentDTO;
            ShotgunPermanentBaseDTO = shotgunPermanentBaseDTO;
            SniperPermanentDTO = sniperPermanentDTO;
            SniperPermanentBaseDTO = sniperPermanentBaseDTO;
            MissileLauncherPermanentDTO = missileLauncherPermanentDTO;
            MissileLauncherPermanentBaseDTO = missileLauncherPermanentBaseDTO;
            LaserPermanentDTO = laserPermanentDTO;
            LaserPermanentBaseDTO = laserPermanentBaseDTO;
        }
    }
}