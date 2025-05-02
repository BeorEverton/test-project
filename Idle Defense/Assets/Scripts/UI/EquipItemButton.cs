using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Turrets;
using Assets.Scripts.UI;

public class EquipItemButton : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private TMP_Text dpsLabel;     // e.g. "41.2 K DPS"
    [SerializeField] private TMP_Text levelLabel;   // e.g. "Lv 87"
    [SerializeField] private Image[] stars;        // 4 star images
    [SerializeField] private Button btn;
    [SerializeField] private Image iconImage;

    private TurretStatsInstance data;

    // Called by EquipPanelUI
    public void Init(TurretStatsInstance inst, System.Action onClick, Sprite icon)
    {
        data = inst;

        // ----- DPS --------------------------------------------------------
        double dps = ComputeDps(inst);
        dpsLabel.text = UIManager.AbbreviateNumber(dps) + " DPS";

        // ----- Level & stars ---------------------------------------------
        int totalLvl = TotalLevel(inst);
        levelLabel.text = "Lv " + totalLvl;

        int starCount = totalLvl >= 500 ? 4 :
                        totalLvl >= 250 ? 3 :
                        totalLvl >= 50 ? 2 : 1;

        for (int i = 0; i < stars.Length; i++)
            stars[i].enabled = i < starCount;

        iconImage.sprite = icon;

        // ----- Click ------------------------------------------------------
        if (!btn) btn = GetComponent<Button>();

        btn.onClick.RemoveAllListeners();

        if (onClick != null)
        {
            btn.onClick.AddListener(() => onClick.Invoke());
            btn.interactable = true;            // enable
        }
        else
        {
            Debug.LogWarning($"{name}: onClick delegate was null.");
            btn.interactable = false;           // keep it disabled
        }

    }

    private static double ComputeDps(TurretStatsInstance s)
    {
        // Expected crit bonus
        double critBonus = (s.CriticalChance / 100.0) *
                           (s.CriticalDamageMultiplier / 100.0 - 1.0);

        double dmgPerProj = s.Damage * (1.0 + critBonus);

        int pellets = Mathf.Max(1, s.PelletCount);

        // For lasers FireRate is 0, treat as 1 shot/sec (damage already per sec)
        double shotsPerSec = s.FireRate > 0.01f ? s.FireRate : 1.0;

        return dmgPerProj * pellets * shotsPerSec;
    }

    private static int TotalLevel(TurretStatsInstance s)
    {
        return Mathf.FloorToInt(
              s.DamageLevel
            + s.FireRateLevel
            + s.CriticalChanceLevel
            + s.CriticalDamageMultiplierLevel
            + s.ExplosionRadiusLevel
            + s.SplashDamageLevel
            + s.PierceChanceLevel
            + s.PierceDamageFalloffLevel
            + s.PelletCountLevel
            + s.DamageFalloffOverDistanceLevel
            + s.PercentBonusDamagePerSecLevel
            + s.SlowEffectLevel);
    }
}
