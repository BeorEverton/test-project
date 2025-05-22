using Assets.Scripts.Helpers;
using Assets.Scripts.Systems;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.UpgradeSystem
{
    public static class TurretUpgradeMetaManager
    {
        private static Dictionary<string, TurretUpgradeMeta> _metaDict;

        public static void Load()
        {
            if (_metaDict != null) //Already loaded
                return;

            TextAsset json = Resources.Load<TextAsset>("TurretUpgradeMeta");
            if (json == null)
            {
                Debug.LogError("Failed to load TurretUpgradeMeta.json");
                _metaDict = new Dictionary<string, TurretUpgradeMeta>();
                return;
            }

            TurretUpgradeMeta[] metas = JsonHelper.FromJson<TurretUpgradeMeta>(json.text);
            _metaDict = new Dictionary<string, TurretUpgradeMeta>();
            foreach (TurretUpgradeMeta meta in metas)
            {
                _metaDict[meta.Type] = meta;
            }
        }

        public static TurretUpgradeMeta GetMeta(TurretUpgradeType type)
        {
            Load();
            _metaDict.TryGetValue(type.ToString(), out TurretUpgradeMeta meta);
            return meta;
        }
    }
}