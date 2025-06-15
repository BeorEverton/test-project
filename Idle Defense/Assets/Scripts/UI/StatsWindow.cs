using Assets.Scripts.Systems;
using System;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI
{
    public class StatsWindow : MonoBehaviour
    {
        private void OnEnable()
        {
            StatsManager.Instance.OnStatChanged += UpdateStatText;
            RefreshAll();
        }

        private void OnDisable()
        {
            StatsManager.Instance.OnStatChanged -= UpdateStatText;
        }

        private void RefreshAll()
        {
            StatsManager sm = StatsManager.Instance;

            UpdateGameTime(sm.GameTime);
            UpdateTotalDamage(sm.TotalDamage);
            UpdateMaxWave(sm.MaxZone);
            UpdateWavesSecured(sm.TotalZonesSecured);
            UpdateHostilesKilled(sm.EnemiesKilled);
            UpdateBossesKilled(sm.BossesKilled);
            UpdateMoneyInvested(sm.MoneySpent);
            UpdateUpgradesMade(sm.UpgradeAmount);
            UpdateDamageTaken(sm.TotalDamageTaken);
            UpdateWallRepaired(sm.TotalHealthRepaired);
            UpdateMissionsFailed(sm.MissionsFailed);
            UpdateSpeedBoosts(sm.SpeedBoostClicks);
            UpdateMachineGunDamage(sm.MachineGunDamage);
            UpdateShotgunDamage(sm.ShotgunDamage);
            UpdateSniperDamage(sm.SniperDamage);
            UpdateMissileLauncherDamage(sm.MissileLauncherDamage);
            UpdateLaserDamage(sm.LaserDamage);
        }

        private void UpdateStatText(string statName, object newValue)
        {
            switch (statName)
            {
                case nameof(StatsManager.GameTime):
                    UpdateGameTime((double)newValue);
                    break;
                case nameof(StatsManager.TotalDamage):
                    UpdateTotalDamage((double)newValue);
                    break;
                case nameof(StatsManager.MaxZone):
                    UpdateMaxWave((int)newValue);
                    break;
                case nameof(StatsManager.TotalZonesSecured):
                    UpdateWavesSecured((int)newValue);
                    break;
                case nameof(StatsManager.EnemiesKilled):
                    UpdateHostilesKilled((int)newValue);
                    break;
                case nameof(StatsManager.BossesKilled):
                    UpdateBossesKilled((int)newValue);
                    break;
                case nameof(StatsManager.MoneySpent):
                    UpdateMoneyInvested((double)newValue);
                    break;
                case nameof(StatsManager.UpgradeAmount):
                    UpdateUpgradesMade((int)newValue);
                    break;
                case nameof(StatsManager.TotalDamageTaken):
                    UpdateDamageTaken((double)newValue);
                    break;
                case nameof(StatsManager.TotalHealthRepaired):
                    UpdateWallRepaired((double)newValue);
                    break;
                case nameof(StatsManager.MissionsFailed):
                    UpdateMissionsFailed((int)newValue);
                    break;
                case nameof(StatsManager.SpeedBoostClicks):
                    UpdateSpeedBoosts((int)newValue);
                    break;
                case nameof(StatsManager.MachineGunDamage):
                    UpdateMachineGunDamage((double)newValue);
                    break;
                case nameof(StatsManager.ShotgunDamage):
                    UpdateShotgunDamage((double)newValue);
                    break;
                case nameof(StatsManager.SniperDamage):
                    UpdateSniperDamage((double)newValue);
                    break;
                case nameof(StatsManager.MissileLauncherDamage):
                    UpdateMissileLauncherDamage((double)newValue);
                    break;
                case nameof(StatsManager.LaserDamage):
                    UpdateLaserDamage((double)newValue);
                    break;
            }
        }

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
        public TextMeshProUGUI gameTimeText;

        [Header("Weapon Damage")]
        public TextMeshProUGUI machineGunDamageText;
        public TextMeshProUGUI shotgunDamageText;
        public TextMeshProUGUI sniperDamageText;
        public TextMeshProUGUI missileLauncherDamageText;
        public TextMeshProUGUI laserDamageText;

        public void UpdateGameTime(double value) =>
            gameTimeText.text = " " + UIManager.FormatTime(TimeSpan.FromSeconds(value));

        public void UpdateTotalDamage(double value) =>
            totalDamageText.text = " " + UIManager.AbbreviateNumber(value);

        public void UpdateMaxWave(int value) =>
            maxWaveText.text = " " + value.ToString();

        public void UpdateWavesSecured(int value) =>
            wavesSecuredText.text = " " + value.ToString();

        public void UpdateHostilesKilled(int value) =>
            hostilesKilledText.text = " " + value.ToString();

        public void UpdateBossesKilled(int value) =>
            bossesKilledText.text = " " + value.ToString();

        public void UpdateMoneyInvested(double value) =>
            moneyInvestedText.text = " " + UIManager.AbbreviateNumber(value);

        public void UpdateUpgradesMade(int value) =>
            upgradesMadeText.text = " " + value.ToString();

        public void UpdateDamageTaken(double value) =>
            damageTakenText.text = " " + UIManager.AbbreviateNumber(value);

        public void UpdateWallRepaired(double value) =>
            wallRepairedText.text = " " + UIManager.AbbreviateNumber(value);

        public void UpdateMissionsFailed(int value) =>
            missionsFailedText.text = " " + value.ToString();

        public void UpdateSpeedBoosts(int value) =>
            speedBoostsText.text = " " + value.ToString();

        public void UpdateMachineGunDamage(double value) =>
            machineGunDamageText.text = " " + UIManager.AbbreviateNumber(value);

        public void UpdateShotgunDamage(double value) =>
            shotgunDamageText.text = " " + UIManager.AbbreviateNumber(value);

        public void UpdateSniperDamage(double value) =>
            sniperDamageText.text = " " + UIManager.AbbreviateNumber(value);

        public void UpdateMissileLauncherDamage(double value) =>
            missileLauncherDamageText.text = " " + UIManager.AbbreviateNumber(value);

        public void UpdateLaserDamage(double value) =>
            laserDamageText.text = " " + UIManager.AbbreviateNumber(value);
    }
}