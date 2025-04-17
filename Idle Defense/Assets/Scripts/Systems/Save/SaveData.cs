using System;

namespace Assets.Scripts.Systems.Save
{
    [Serializable]
    public class GameData
    {
        public GameDataDTO GameDataDTO;
        public PlayerInfoDTO PlayerInfoDTO;
        public TurretInfoDTO MachineGunTurretInfoDTO;
        public TurretInfoDTO? ShotgunTurretInfoDTO;
        public TurretInfoDTO? SniperTurretInfoDTO;
        public TurretInfoDTO? MissileLauncherTurretInfoDTO;
        public TurretInfoDTO? LaserTurretInfoDTO;

        public GameData(GameDataDTO gameData, PlayerInfoDTO playerInfo, TurretInfoDTO machineGunTurret, TurretInfoDTO? shotgunTurret, TurretInfoDTO? sniperTurret, TurretInfoDTO? missileLauncherTurret, TurretInfoDTO? laserTurret)
        {
            GameDataDTO = gameData;
            PlayerInfoDTO = playerInfo;
            MachineGunTurretInfoDTO = machineGunTurret;
            ShotgunTurretInfoDTO = shotgunTurret;
            SniperTurretInfoDTO = sniperTurret;
            MissileLauncherTurretInfoDTO = missileLauncherTurret;
            LaserTurretInfoDTO = laserTurret;
        }
    }
}