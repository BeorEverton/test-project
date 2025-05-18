using Assets.Scripts.Enemies;
using Assets.Scripts.SO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyEntryButtonUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image iconImage;
    private EnemyInfoSO info;
    private EnemyInfoPanel infoPanel;

    public void Setup(EnemyInfoSO enemyInfo, bool discovered, EnemyInfoPanel panel)
    {
        info = enemyInfo;
        infoPanel = panel;

        nameText.text = discovered ? info.Name : "???";
        iconImage.sprite = discovered ? info.Icon : EnemyLibraryManager.Instance.UnknownSprite;

        GetComponent<Button>().onClick.AddListener(() => infoPanel.gameObject.SetActive(true));
        GetComponent<Button>().onClick.AddListener(() => infoPanel.DisplayEnemyInfo(info));
    }
}
