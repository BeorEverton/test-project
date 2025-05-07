using Assets.Scripts.Enemies;
using Assets.Scripts.SO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyInfoPanel : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText, hpText, dmgText, spdText;
    [SerializeField] private Sprite unknownSprite;

    public void DisplayEnemyInfo(EnemyInfoSO enemyInfo)
    {
        if (!EnemyLibraryManager.Instance) return;

        var entry = EnemyLibraryManager.Instance.GetAllEntries()
                      .FirstOrDefault(e => e.info == enemyInfo);

        if (entry.info == null) return;

        if (!entry.discovered)
        {
            icon.sprite = unknownSprite;
            nameText.text = "???";
            hpText.text = dmgText.text = spdText.text = "?";
            return;
        }

        icon.sprite = enemyInfo.Icon;
        nameText.text = enemyInfo.Name;

        var (hp, dmg, spd) = EnemyLibraryManager.Instance.GetEnemyTiers(enemyInfo);
        hpText.text = $"HP: {hp}";
        dmgText.text = $"DMG: {dmg}";
        spdText.text = $"SPD: {spd}";
    }
}
