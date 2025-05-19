using Assets.Scripts.UI;
using TMPro;
using UnityEngine;

public class StatsWindow : MonoBehaviour
{
    [Header("Stat Texts")]
    public TextMeshProUGUI totalDamageText;
    public TextMeshProUGUI maxWaveText;
    public TextMeshProUGUI wavesSecuredText;
    public TextMeshProUGUI hostilesKilledText;
    public TextMeshProUGUI bossesKilledText;
    public TextMeshProUGUI moneyInvestedText;
    public TextMeshProUGUI upgradesMadeText;
    public TextMeshProUGUI damageTakenText;
    public TextMeshProUGUI wallRepairedText;
    public TextMeshProUGUI missionsFailedText;
    public TextMeshProUGUI speedBoostsText;

    [Header("Weapon Damage")]
    public TextMeshProUGUI machineGunDamageText;
    public TextMeshProUGUI shotgunDamageText;
    public TextMeshProUGUI sniperDamageText;
    public TextMeshProUGUI missileLauncherDamageText;
    public TextMeshProUGUI laserDamageText;

    public void UpdateTotalDamage(double value) =>
        totalDamageText.text = UIManager.AbbreviateNumber(value);

    public void UpdateMaxWave(int value) =>
        maxWaveText.text = value.ToString();

    public void UpdateWavesSecured(int value) =>
        wavesSecuredText.text = value.ToString();

    public void UpdateHostilesKilled(int value) =>
        hostilesKilledText.text = value.ToString();

    public void UpdateBossesKilled(int value) =>
        bossesKilledText.text = value.ToString();

    public void UpdateMoneyInvested(double value) =>
        moneyInvestedText.text = UIManager.AbbreviateNumber(value);

    public void UpdateUpgradesMade(int value) =>
        upgradesMadeText.text = value.ToString();

    public void UpdateDamageTaken(double value) =>
        damageTakenText.text = UIManager.AbbreviateNumber(value);

    public void UpdateWallRepaired(double value) =>
        wallRepairedText.text = UIManager.AbbreviateNumber(value);

    public void UpdateMissionsFailed(int value) =>
        missionsFailedText.text = value.ToString();

    public void UpdateSpeedBoosts(int value) =>
        speedBoostsText.text = value.ToString();

    public void UpdateMachineGunDamage(double value) =>
        machineGunDamageText.text = UIManager.AbbreviateNumber(value);

    public void UpdateShotgunDamage(double value) =>
        shotgunDamageText.text = UIManager.AbbreviateNumber(value);

    public void UpdateSniperDamage(double value) =>
        sniperDamageText.text = UIManager.AbbreviateNumber(value);

    public void UpdateMissileLauncherDamage(double value) =>
        missileLauncherDamageText.text = UIManager.AbbreviateNumber(value);

    public void UpdateLaserDamage(double value) =>
        laserDamageText.text = UIManager.AbbreviateNumber(value);
}

