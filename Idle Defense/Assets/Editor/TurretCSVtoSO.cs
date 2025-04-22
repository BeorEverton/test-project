using Assets.Scripts.SO;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
    public class TurretCSVtoSO
    {
        private static string _turretCsvPath = "/Editor/CSVs/TurretInfoCSV.csv";

        [MenuItem("Utilities/Generate TurretSo")]
        public static void GenerateTurrets()
        {
            string[] allLines = File.ReadAllLines(Application.dataPath + _turretCsvPath);

            foreach (string line in allLines)
            {
                string[] lines = line.Split(',');

                if (!Enum.TryParse(lines[0], out TurretType turretType))
                    continue;

                TurretInfoSO turretInfo =
                    AssetDatabase.LoadAssetAtPath<TurretInfoSO>(
                        $"Assets/Scriptable Objects/Turrets/{lines[0]}Turret.asset");

                if (turretInfo == null)
                {
                    turretInfo = ScriptableObject.CreateInstance<TurretInfoSO>();
                    AssetDatabase.CreateAsset(turretInfo, $"Assets/Scriptable Objects/Enemies/{lines[0]}Turret.asset");

                }

                //Update asset's data
                turretInfo.name = $"{lines[0]}Turret";
                turretInfo.TurretType = turretType;

                SetBaseInfo(turretInfo, lines);
                switch (turretType)
                {
                    case TurretType.MachineGun:
                        SetMachineGunInfo(turretInfo, lines);
                        break;
                    case TurretType.Shotgun:
                        SetShotgunInfo(turretInfo, lines);
                        break;
                    case TurretType.Sniper:
                        SetSniperInfo(turretInfo, lines);
                        break;
                    case TurretType.MissileLauncher:
                        SetMissileLauncherInfo(turretInfo, lines);
                        break;
                    case TurretType.Laser:
                        SetLaserInfo(turretInfo, lines);
                        break;
                    default:
                        Debug.LogError($"Turret type {turretType} not implemented.");
                        break;
                }

                //Mark asset dirty so that the changes are saved
                EditorUtility.SetDirty(turretInfo);
            }

            AssetDatabase.SaveAssets();
        }

        private static void SetBaseInfo(TurretInfoSO turretInfo, string[] lines)
        {
            turretInfo.IsUnlocked = bool.Parse(lines[1]);

            turretInfo.Damage = float.Parse(lines[2], CultureInfo.InvariantCulture);
            turretInfo.DamageLevel = 0;
            turretInfo.DamageUpgradeAmount = float.Parse(lines[3], CultureInfo.InvariantCulture);
            turretInfo.DamageUpgradeBaseCost = float.Parse(lines[4], CultureInfo.InvariantCulture);
            turretInfo.DamageCostExponentialMultiplier = float.Parse(lines[5], CultureInfo.InvariantCulture);

            turretInfo.FireRate = float.Parse(lines[6], CultureInfo.InvariantCulture);
            turretInfo.FireRateLevel = 0;
            turretInfo.FireRateUpgradeAmount = float.Parse(lines[7], CultureInfo.InvariantCulture);
            turretInfo.FireRateUpgradeBaseCost = float.Parse(lines[8], CultureInfo.InvariantCulture);
            turretInfo.FireRateCostExponentialMultiplier = float.Parse(lines[9], CultureInfo.InvariantCulture);

            turretInfo.RotationSpeed = float.Parse(lines[18], CultureInfo.InvariantCulture);

            turretInfo.AngleThreshold = float.Parse(lines[19], CultureInfo.InvariantCulture);
        }

        private static void SetMachineGunInfo(TurretInfoSO turretInfo, string[] lines)
        {
            turretInfo.CriticalChance = float.Parse(lines[10], CultureInfo.InvariantCulture);
            turretInfo.CriticalChanceLevel = 0;
            turretInfo.CriticalChanceUpgradeAmount = float.Parse(lines[11], CultureInfo.InvariantCulture);
            turretInfo.CriticalChanceUpgradeBaseCost = float.Parse(lines[12], CultureInfo.InvariantCulture);
            turretInfo.CriticalChanceCostExponentialMultiplier = float.Parse(lines[13], CultureInfo.InvariantCulture);

            turretInfo.CriticalDamageMultiplier = float.Parse(lines[14], CultureInfo.InvariantCulture);
            turretInfo.CriticalDamageMultiplierLevel = 0;
            turretInfo.CriticalDamageMultiplierUpgradeAmount = float.Parse(lines[15], CultureInfo.InvariantCulture);
            turretInfo.CriticalDamageMultiplierUpgradeBaseCost = float.Parse(lines[16], CultureInfo.InvariantCulture);
            turretInfo.CriticalDamageCostExponentialMultiplier = float.Parse(lines[17], CultureInfo.InvariantCulture);
        }

        private static void SetShotgunInfo(TurretInfoSO turretInfo, string[] lines)
        {
            turretInfo.PelletCount = int.Parse(lines[10]);
            turretInfo.PelletCountLevel = 0;
            turretInfo.PelletCountUpgradeAmount = int.Parse(lines[11]);
            turretInfo.PelletCountUpgradeBaseCost = float.Parse(lines[12], CultureInfo.InvariantCulture);
            turretInfo.PelletCountCostExponentialMultiplier = float.Parse(lines[13], CultureInfo.InvariantCulture);

            turretInfo.DamageFalloffOverDistance = float.Parse(lines[14], CultureInfo.InvariantCulture);
            turretInfo.DamageFalloffOverDistanceLevel = 0;
            turretInfo.DamageFalloffOverDistanceUpgradeAmount = float.Parse(lines[15], CultureInfo.InvariantCulture);
            turretInfo.DamageFalloffOverDistanceUpgradeBaseCost = float.Parse(lines[16], CultureInfo.InvariantCulture);
            turretInfo.DamageFalloffOverDistanceCostExponentialMultiplier = float.Parse(lines[17], CultureInfo.InvariantCulture);
        }

        private static void SetSniperInfo(TurretInfoSO turretInfo, string[] lines)
        {
            turretInfo.PierceChance = float.Parse(lines[10], CultureInfo.InvariantCulture);
            turretInfo.PierceChanceLevel = 0;
            turretInfo.PierceChanceUpgradeAmount = float.Parse(lines[11], CultureInfo.InvariantCulture);
            turretInfo.PierceChanceUpgradeBaseCost = float.Parse(lines[12], CultureInfo.InvariantCulture);
            turretInfo.PierceChanceCostExponentialMultiplier = float.Parse(lines[13], CultureInfo.InvariantCulture);

            turretInfo.PierceDamageFalloff = float.Parse(lines[14], CultureInfo.InvariantCulture);
            turretInfo.PierceDamageFalloffLevel = 0;
            turretInfo.PierceDamageFalloffUpgradeAmount = float.Parse(lines[15], CultureInfo.InvariantCulture);
            turretInfo.PierceDamageFalloffUpgradeBaseCost = float.Parse(lines[16], CultureInfo.InvariantCulture);
            turretInfo.PierceDamageFalloffCostExponentialMultiplier = float.Parse(lines[17], CultureInfo.InvariantCulture);
        }

        private static void SetMissileLauncherInfo(TurretInfoSO turretInfo, string[] lines)
        {
            turretInfo.ExplosionRadius = float.Parse(lines[10], CultureInfo.InvariantCulture);
            turretInfo.ExplosionRadiusLevel = 0;
            turretInfo.ExplosionRadiusUpgradeAmount = float.Parse(lines[11], CultureInfo.InvariantCulture);
            turretInfo.ExplosionRadiusUpgradeBaseCost = float.Parse(lines[12], CultureInfo.InvariantCulture);
            turretInfo.ExplosionRadiusCostExponentialMultiplier = float.Parse(lines[13], CultureInfo.InvariantCulture);

            turretInfo.SplashDamage = float.Parse(lines[14], CultureInfo.InvariantCulture);
            turretInfo.SplashDamageLevel = 0;
            turretInfo.SplashDamageUpgradeAmount = float.Parse(lines[15], CultureInfo.InvariantCulture);
            turretInfo.SplashDamageUpgradeBaseCost = float.Parse(lines[16], CultureInfo.InvariantCulture);
            turretInfo.SplashDamageCostExponentialMultiplier = float.Parse(lines[17], CultureInfo.InvariantCulture);
        }

        private static void SetLaserInfo(TurretInfoSO turretInfo, string[] lines)
        {
            turretInfo.PercentBonusDamagePerSec = float.Parse(lines[10], CultureInfo.InvariantCulture);
            turretInfo.PercentBonusDamagePerSecLevel = 0;
            turretInfo.PercentBonusDamagePerSecUpgradeAmount = float.Parse(lines[11], CultureInfo.InvariantCulture);
            turretInfo.PercentBonusDamagePerSecUpgradeBaseCost = float.Parse(lines[12], CultureInfo.InvariantCulture);
            turretInfo.PercentBonusDamagePerSecCostExponentialMultiplier = float.Parse(lines[13], CultureInfo.InvariantCulture);

            turretInfo.SlowEffect = float.Parse(lines[14], CultureInfo.InvariantCulture);
            turretInfo.SlowEffectLevel = 0;
            turretInfo.SlowEffectUpgradeAmount = float.Parse(lines[15], CultureInfo.InvariantCulture);
            turretInfo.SlowEffectUpgradeBaseCost = float.Parse(lines[16], CultureInfo.InvariantCulture);
            turretInfo.SlowEffectCostExponentialMultiplier = float.Parse(lines[17], CultureInfo.InvariantCulture);
        }
    }
}