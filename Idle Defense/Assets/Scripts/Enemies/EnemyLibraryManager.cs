using Assets.Scripts.SO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private GameObject enemyDiscoveredPopUp;
        [SerializeField] private Image enemyDiscoveredIcon;
        [SerializeField] private Slider popUpTimer;

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

        public void MarkAsDiscovered(string enemyName, bool load = false)
        {
            if (entryLookup.TryGetValue(enemyName, out var entry))
            {
                if (!entry.discovered && !load)
                    StartCoroutine(ShowEnemyDiscoveredPopUp(entry.info.Icon));
                entry.discovered = true;
            }
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

        public (string hp, string dmg, string speed, string attackRange) GetEnemyTiers(EnemyInfoSO target)
        {
            List<float> allHPs = enemyEntries.Select(e => e.info.MaxHealth).ToList();
            List<float> allDMGs = enemyEntries.Select(e => e.info.Damage).ToList();
            List<float> allSpeeds = enemyEntries.Select(e => e.info.MovementSpeed).ToList();
            List<float> allRanges = enemyEntries.Select(e => e.info.AttackRange).ToList();

            string hpTier = GetStatTier(target.MaxHealth, allHPs);
            string dmgTier = GetStatTier(target.Damage, allDMGs);
            string spdTier = GetStatTier(target.MovementSpeed, allSpeeds);
            string rangeTier = GetStatTier(target.AttackRange, allRanges);

            return (hpTier, dmgTier, spdTier, rangeTier);
        }

        IEnumerator ShowEnemyDiscoveredPopUp(Sprite icon)
        {
            yield return new WaitForSeconds(.5f); // To prevent disappearing when the time is paused
            enemyDiscoveredPopUp.SetActive(true);
            enemyDiscoveredIcon.sprite = icon;
            float duration = 5f;
            popUpTimer.maxValue = duration;
            popUpTimer.value = duration;
            for (float t = 5f; t >= 0; t -= Time.unscaledDeltaTime)
            {
                popUpTimer.value = t;
                yield return null;
            }
            enemyDiscoveredPopUp.SetActive(false);
        }

        public void StopTimer()
        {
            StopAllCoroutines();
            enemyDiscoveredPopUp.SetActive(false);
        }
    }
}
