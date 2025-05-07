using Assets.Scripts.Enemies;
using UnityEngine;

public class EnemyLibraryUI : MonoBehaviour
{
    [SerializeField] private Transform contentRoot; // container in ScrollView
    [SerializeField] private GameObject enemyButtonPrefab;
    [SerializeField] private EnemyInfoPanel infoPanel;

    private void Start()
    {
        foreach (var entry in EnemyLibraryManager.Instance.GetAllEntries())
        {
            var go = Instantiate(enemyButtonPrefab, contentRoot);
            var btn = go.GetComponent<EnemyEntryButtonUI>();
            btn.Setup(entry.info, entry.discovered, infoPanel);
        }
    }
}
