using System;

namespace Assets.Scripts.Systems.Save
{
    [Serializable]
    public class GameData
    {
        public GameDataDTO GameDataDTO;
        public PlayerInfoDTO PlayerInfoDTO;
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
        public TurretInventoryDTO TurretInventory;


        public GameData(GameDataDTO gameData,
            PlayerInfoDTO playerInfo,
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
            TurretInventoryDTO turretInventory)
        {
            GameDataDTO = gameData;
            PlayerInfoDTO = playerInfo;
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
            TurretInventory = turretInventory;
        }
    }
}