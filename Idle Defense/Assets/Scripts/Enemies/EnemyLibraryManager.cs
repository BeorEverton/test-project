using Assets.Scripts.SO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Enemies
{
    [System.Serializable]
    public class EnemyLibraryEntry
    {
        public EnemyInfoSO info;
        public bool discovered;
    }

    public class EnemyLibraryManager : MonoBehaviour
    {
        public static EnemyLibraryManager Instance { get; private set; }

        [SerializeField] private List<EnemyLibraryEntry> enemyEntries = new();
        private Dictionary<string, EnemyLibraryEntry> entryLookup;
        [SerializeField] private Sprite unknownSprite;
        public Sprite UnknownSprite => unknownSprite;

        [SerializeField] private Transform contentParent; // Your ScrollView's Content
        [SerializeField] private GameObject enemyEntryPrefab; // Your entry prefab

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            LoadAllEnemies();
        }

        private void LoadAllEnemies()
        {
            EnemyInfoSO[] allEnemies = Resources.LoadAll<EnemyInfoSO>("Scriptable Objects/Enemies");
            enemyEntries.Clear();

            foreach (var enemy in allEnemies)
            {
                enemyEntries.Add(new EnemyLibraryEntry
                {
                    info = enemy,
                    discovered = false
                });
            }

            entryLookup = enemyEntries.ToDictionary(e => e.info.Name, e => e);
        }

        public void MarkAsDiscovered(string enemyName)
        {
            if (entryLookup.TryGetValue(enemyName, out var entry))
                entry.discovered = true;
        }

        public List<EnemyLibraryEntry> GetAllEntries() => enemyEntries;

        public string GetStatTier(float value, List<float> allValues)
        {
            if (allValues.Count == 0) return "Unknown";

            allValues.Sort();
            int index = allValues.FindIndex(v => value <= v);
            float percentile = index / (float)allValues.Count;

            if (percentile <= 0.25f) return "Low";
            if (percentile <= 0.5f) return "Med";
            if (percentile <= 0.75f) return "High";
            return "Max";
        }

        public (string hp, string dmg, string speed) GetEnemyTiers(EnemyInfoSO target)
        {
            List<float> allHPs = enemyEntries.Select(e => e.info.MaxHealth).ToList();
            List<float> allDMGs = enemyEntries.Select(e => e.info.Damage).ToList();
            List<float> allSpeeds = enemyEntries.Select(e => e.info.MovementSpeed).ToList();

            string hpTier = GetStatTier(target.MaxHealth, allHPs);
            string dmgTier = GetStatTier(target.Damage, allDMGs);
            string spdTier = GetStatTier(target.MovementSpeed, allSpeeds);

            return (hpTier, dmgTier, spdTier);
        }


    }
}
