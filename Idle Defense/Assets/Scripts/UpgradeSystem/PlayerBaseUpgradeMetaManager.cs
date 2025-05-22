using Assets.Scripts.Helpers;
using Assets.Scripts.Systems;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.UpgradeSystem
{
    public static class PlayerBaseUpgradeMetaManager
    {
        private static Dictionary<string, PlayerBaseUpgradeMeta> _metaDict;

        public static void Load()
        {
            if (_metaDict != null) //Already loaded
                return;

            TextAsset json = Resources.Load<TextAsset>("PlayerBaseUpgradeMeta");
            if (json == null)
            {
                Debug.LogError("Failed to load PlayerBaseUpgradeMeta.json");
                _metaDict = new Dictionary<string, PlayerBaseUpgradeMeta>();
                return;
            }

            PlayerBaseUpgradeMeta[] metas = JsonHelper.FromJson<PlayerBaseUpgradeMeta>(json.text);
            _metaDict = new Dictionary<string, PlayerBaseUpgradeMeta>();
            foreach (PlayerBaseUpgradeMeta meta in metas)
            {
                _metaDict[meta.Type] = meta;
            }
        }

        public static PlayerBaseUpgradeMeta GetMeta(PlayerUpgradeType type)
        {
            Load();
            _metaDict.TryGetValue(type.ToString(), out PlayerBaseUpgradeMeta meta);
            return meta;
        }
    }
}