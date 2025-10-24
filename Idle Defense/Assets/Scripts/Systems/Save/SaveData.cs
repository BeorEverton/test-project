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

        public StatsDTO StatsDTO;
        public TurretInventoryDTO TurretInventory;

        public List<string> DiscoveredEnemyNames;

        public GunnerInventoryDTO GunnerInventory;

        public PrestigeDTO Prestige;

        public GameData(GameDataDTO gameData,
            PlayerInfoDTO playerInfo,
            PlayerInfoDTO permanentPlayerInfo,
            StatsDTO statsDTO,
            TurretInventoryDTO turretInventory,
            GunnerInventoryDTO gunnerInventory,
            PrestigeDTO prestige
            )
        {
            GameDataDTO = gameData;
            PlayerInfoDTO = playerInfo;
            PermanentPlayerInfoDTO = permanentPlayerInfo;

            StatsDTO = statsDTO;
            TurretInventory = turretInventory;

            GunnerInventory = gunnerInventory;

            Prestige = prestige;
        }
    }
}