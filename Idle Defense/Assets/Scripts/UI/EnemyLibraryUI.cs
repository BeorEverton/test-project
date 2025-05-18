using Assets.Scripts.Enemies;
using System.Collections.Generic;
using UnityEngine;

public class EnemyLibraryUI : MonoBehaviour
{
    [SerializeField] private Transform contentRoot;
    [SerializeField] private GameObject enemyButtonPrefab;
    [SerializeField] private EnemyInfoPanel infoPanel;

    private List<GameObject> enemyButtons = new List<GameObject>();

    private void OnEnable()
    {
        var entries = EnemyLibraryManager.Instance.GetAllEntries();
        entries.Sort((a, b) =>
        {
            int discoveredCompare = b.discovered.CompareTo(a.discovered); // true = 1, false = 0
            if (discoveredCompare != 0)
                return discoveredCompare;

            // Optional: further sort by enemy name if needed
            return a.info.Name.CompareTo(b.info.Name);
        });


        // Ensure we have enough buttons (instantiate more if needed)
        for (int i = enemyButtons.Count; i < entries.Count; i++)
        {
            var newButton = Instantiate(enemyButtonPrefab, contentRoot);
            enemyButtons.Add(newButton);
        }

        // Set up all buttons
        for (int i = 0; i < entries.Count; i++)
        {
            enemyButtons[i].SetActive(true);
            var btn = enemyButtons[i].GetComponent<EnemyEntryButtonUI>();
            btn.Setup(entries[i].info, entries[i].discovered, infoPanel);
        }

        // Hide any extra buttons
        for (int i = entries.Count; i < enemyButtons.Count; i++)
        {
            enemyButtons[i].SetActive(false);
        }
    }
}
