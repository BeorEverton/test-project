using Assets.Scripts.SO;
using System;
using UnityEditor;

namespace Assets.Editor
{
    [CustomEditor(typeof(TurretInfoSO)), CanEditMultipleObjects]
    public class TurretInfoEditor : UnityEditor.Editor
    {
        private SerializedProperty turretTypeProp;

        // Base Turret
        private SerializedProperty isUnlockedProp;
        private SerializedProperty damageProp;
        private SerializedProperty damageLevelProp;
        private SerializedProperty damageUpgradeAmountProp;
        private SerializedProperty damageUpgradeBaseCostProp;
        private SerializedProperty damageCostExponentialMultiplierProp;
        private SerializedProperty fireRateProp;
        private SerializedProperty fireRateLevelProp;
        private SerializedProperty fireRateUpgradeAmountProp;
        private SerializedProperty fireRateUpgradeBaseCostProp;
        private SerializedProperty fireRateCostExponentialMultiplierProp;
        private SerializedProperty rotationSpeedProp;
        private SerializedProperty angleThresholdProp;

        // Machine Gun Turret
        private SerializedProperty criticalChanceProp;
        private SerializedProperty criticalChanceLevelProp;
        private SerializedProperty criticalChanceUpgradeAmountProp;
        private SerializedProperty criticalChanceUpgradeBaseCostProp;
        private SerializedProperty criticalChanceCostExponentialMultiplierProp;
        private SerializedProperty criticalDamageMultiplierProp;
        private SerializedProperty criticalDamageMultiplierLevelProp;
        private SerializedProperty criticalDamageMultiplierUpgradeAmountProp;
        private SerializedProperty criticalDamageMultiplierUpgradeBaseCostProp;
        private SerializedProperty criticalDamageCostExponentialMultiplierProp;

        // Missile Launcher Turret
        private SerializedProperty explosionRadiusProp;
        private SerializedProperty explosionRadiusLevelProp;
        private SerializedProperty explosionRadiusUpgradeAmountProp;
        private SerializedProperty explosionRadiusUpgradeBaseCostProp;
        private SerializedProperty splashDamageProp;
        private SerializedProperty splashDamageLevelProp;
        private SerializedProperty splashDamageUpgradeAmountProp;
        private SerializedProperty splashDamageUpgradeBaseCostProp;

        // Sniper Turret
        private SerializedProperty pierceChanceProp;
        private SerializedProperty pierceChanceLevelProp;
        private SerializedProperty pierceChanceUpgradeAmountProp;
        private SerializedProperty pierceChanceUpgradeBaseCostProp;
        private SerializedProperty pierceDamageFalloffProp;
        private SerializedProperty pierceDamageFalloffLevelProp;
        private SerializedProperty pierceDamageFalloffUpgradeAmountProp;
        private SerializedProperty pierceDamageFalloffUpgradeBaseCostProp;

        // Shotgun Turret
        private SerializedProperty pelletCountProp;
        private SerializedProperty pelletCountLevelProp;
        private SerializedProperty pelletCountUpgradeAmountProp;
        private SerializedProperty pelletCountUpgradeBaseCostProp;
        private SerializedProperty damageFalloffOverDistanceProp;
        private SerializedProperty damageFalloffOverDistanceLevelProp;
        private SerializedProperty damageFalloffOverDistanceUpgradeAmountProp;
        private SerializedProperty damageFalloffOverDistanceUpgradeBaseCostProp;
        private SerializedProperty knockbackStrengthProp;
        private SerializedProperty knockbackStrengthLevelProp;
        private SerializedProperty knockbackStrengthUpgradeAmountProp;
        private SerializedProperty knockbackStrengthUpgradeBaseCostProp;
        private SerializedProperty knockbackStrengthCostExponentialMultiplierProp;


        // Laser Turret
        private SerializedProperty percentBonusDamagePerSecProp;
        private SerializedProperty percentBonusDamagePerSecLevelProp;
        private SerializedProperty percentBonusDamagePerSecUpgradeAmountProp;
        private SerializedProperty percentBonusDamagePerSecUpgradeBaseCostProp;
        private SerializedProperty slowEffectProp;
        private SerializedProperty slowEffectLevelProp;
        private SerializedProperty slowEffectUpgradeAmountProp;
        private SerializedProperty slowEffectUpgradeBaseCostProp;

        private void OnEnable()
        {
            // Base
            turretTypeProp = serializedObject.FindProperty("TurretType");
            isUnlockedProp = serializedObject.FindProperty("IsUnlocked");
            damageProp = serializedObject.FindProperty("Damage");
            damageLevelProp = serializedObject.FindProperty("DamageLevel");
            damageUpgradeAmountProp = serializedObject.FindProperty("DamageUpgradeAmount");
            damageUpgradeBaseCostProp = serializedObject.FindProperty("DamageUpgradeBaseCost");
            damageCostExponentialMultiplierProp = serializedObject.FindProperty("DamageCostExponentialMultiplier");
            fireRateProp = serializedObject.FindProperty("FireRate");
            fireRateLevelProp = serializedObject.FindProperty("FireRateLevel");
            fireRateUpgradeAmountProp = serializedObject.FindProperty("FireRateUpgradeAmount");
            fireRateUpgradeBaseCostProp = serializedObject.FindProperty("FireRateUpgradeBaseCost");
            fireRateCostExponentialMultiplierProp = serializedObject.FindProperty("FireRateCostExponentialMultiplier");
            rotationSpeedProp = serializedObject.FindProperty("RotationSpeed");
            angleThresholdProp = serializedObject.FindProperty("AngleThreshold");

            // Machine Gun
            criticalChanceProp = serializedObject.FindProperty("CriticalChance");
            criticalChanceLevelProp = serializedObject.FindProperty("CriticalChanceLevel");
            criticalChanceUpgradeAmountProp = serializedObject.FindProperty("CriticalChanceUpgradeAmount");
            criticalChanceUpgradeBaseCostProp = serializedObject.FindProperty("CriticalChanceUpgradeBaseCost");
            criticalChanceCostExponentialMultiplierProp
                                                  = serializedObject.FindProperty("CriticalChanceCostExponentialMultiplier");
            criticalDamageMultiplierProp = serializedObject.FindProperty("CriticalDamageMultiplier");
            criticalDamageMultiplierLevelProp = serializedObject.FindProperty("CriticalDamageMultiplierLevel");
            criticalDamageMultiplierUpgradeAmountProp
                                                  = serializedObject.FindProperty("CriticalDamageMultiplierUpgradeAmount");
            criticalDamageMultiplierUpgradeBaseCostProp
                                                  = serializedObject.FindProperty("CriticalDamageMultiplierUpgradeBaseCost");
            criticalDamageCostExponentialMultiplierProp
                                                  = serializedObject.FindProperty("CriticalDamageCostExponentialMultiplier");

            // Missile
            explosionRadiusProp = serializedObject.FindProperty("ExplosionRadius");
            explosionRadiusLevelProp = serializedObject.FindProperty("ExplosionRadiusLevel");
            explosionRadiusUpgradeAmountProp = serializedObject.FindProperty("ExplosionRadiusUpgradeAmount");
            explosionRadiusUpgradeBaseCostProp = serializedObject.FindProperty("ExplosionRadiusUpgradeBaseCost");
            splashDamageProp = serializedObject.FindProperty("SplashDamage");
            splashDamageLevelProp = serializedObject.FindProperty("SplashDamageLevel");
            splashDamageUpgradeAmountProp = serializedObject.FindProperty("SplashDamageUpgradeAmount");
            splashDamageUpgradeBaseCostProp = serializedObject.FindProperty("SplashDamageUpgradeBaseCost");

            // Sniper
            pierceChanceProp = serializedObject.FindProperty("PierceChance");
            pierceChanceLevelProp = serializedObject.FindProperty("PierceChanceLevel");
            pierceChanceUpgradeAmountProp = serializedObject.FindProperty("PierceChanceUpgradeAmount");
            pierceChanceUpgradeBaseCostProp = serializedObject.FindProperty("PierceChanceUpgradeBaseCost");
            pierceDamageFalloffProp = serializedObject.FindProperty("PierceDamageFalloff");
            pierceDamageFalloffLevelProp = serializedObject.FindProperty("PierceDamageFalloffLevel");
            pierceDamageFalloffUpgradeAmountProp = serializedObject.FindProperty("PierceDamageFalloffUpgradeAmount");
            pierceDamageFalloffUpgradeBaseCostProp
                                                  = serializedObject.FindProperty("PierceDamageFalloffUpgradeBaseCost");

            // Shotgun
            pelletCountProp = serializedObject.FindProperty("PelletCount");
            pelletCountLevelProp = serializedObject.FindProperty("PelletCountLevel");
            pelletCountUpgradeAmountProp = serializedObject.FindProperty("PelletCountUpgradeAmount");
            pelletCountUpgradeBaseCostProp = serializedObject.FindProperty("PelletCountUpgradeBaseCost");
            damageFalloffOverDistanceProp = serializedObject.FindProperty("DamageFalloffOverDistance");
            damageFalloffOverDistanceLevelProp = serializedObject.FindProperty("DamageFalloffOverDistanceLevel");
            damageFalloffOverDistanceUpgradeAmountProp
                                                  = serializedObject.FindProperty("DamageFalloffOverDistanceUpgradeAmount");
            damageFalloffOverDistanceUpgradeBaseCostProp
                                                  = serializedObject.FindProperty("DamageFalloffOverDistanceUpgradeBaseCost");
            knockbackStrengthProp = serializedObject.FindProperty("KnockbackStrength");
            knockbackStrengthLevelProp = serializedObject.FindProperty("KnockbackStrengthLevel");
            knockbackStrengthUpgradeAmountProp = serializedObject.FindProperty("KnockbackStrengthUpgradeAmount");
            knockbackStrengthUpgradeBaseCostProp = serializedObject.FindProperty("KnockbackStrengthUpgradeBaseCost");
            knockbackStrengthCostExponentialMultiplierProp = serializedObject.FindProperty("KnockbackStrengthCostExponentialMultiplier");


            // Laser
            percentBonusDamagePerSecProp = serializedObject.FindProperty("PercentBonusDamagePerSec");
            percentBonusDamagePerSecLevelProp = serializedObject.FindProperty("PercentBonusDamagePerSecLevel");
            percentBonusDamagePerSecUpgradeAmountProp
                                                  = serializedObject.FindProperty("PercentBonusDamagePerSecUpgradeAmount");
            percentBonusDamagePerSecUpgradeBaseCostProp
                                                  = serializedObject.FindProperty("PercentBonusDamagePerSecUpgradeBaseCost");
            slowEffectProp = serializedObject.FindProperty("SlowEffect");
            slowEffectLevelProp = serializedObject.FindProperty("SlowEffectLevel");
            slowEffectUpgradeAmountProp = serializedObject.FindProperty("SlowEffectUpgradeAmount");
            slowEffectUpgradeBaseCostProp = serializedObject.FindProperty("SlowEffectUpgradeBaseCost");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ShowBaseInfo();

            TurretType type = (TurretType)turretTypeProp.enumValueIndex;

            switch (type)
            {
                case TurretType.MachineGun:
                    ShowMachineGunInfo();
                    break;
                case TurretType.Shotgun:
                    ShowShotgunInfo();
                    break;
                case TurretType.Sniper:
                    ShowSniperInfo();
                    break;
                case TurretType.MissileLauncher:
                    ShowMissileLauncherInfo();
                    break;
                case TurretType.Laser:
                    ShowLaserInfo();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


            serializedObject.ApplyModifiedProperties();
        }

        private void ShowBaseInfo()
        {
            EditorGUILayout.PropertyField(turretTypeProp);
            EditorGUILayout.PropertyField(isUnlockedProp);
            EditorGUILayout.PropertyField(damageProp);
            EditorGUILayout.PropertyField(damageLevelProp);
            EditorGUILayout.PropertyField(damageUpgradeAmountProp);
            EditorGUILayout.PropertyField(damageUpgradeBaseCostProp);
            EditorGUILayout.PropertyField(damageCostExponentialMultiplierProp);
            EditorGUILayout.PropertyField(fireRateProp);
            EditorGUILayout.PropertyField(fireRateLevelProp);
            EditorGUILayout.PropertyField(fireRateUpgradeAmountProp);
            EditorGUILayout.PropertyField(fireRateUpgradeBaseCostProp);
            EditorGUILayout.PropertyField(fireRateCostExponentialMultiplierProp);
            EditorGUILayout.PropertyField(rotationSpeedProp);
            EditorGUILayout.PropertyField(angleThresholdProp);
            EditorGUILayout.Space();
        }

        private void ShowMachineGunInfo()
        {
            EditorGUILayout.PropertyField(criticalChanceProp);
            EditorGUILayout.PropertyField(criticalChanceLevelProp);
            EditorGUILayout.PropertyField(criticalChanceUpgradeAmountProp);
            EditorGUILayout.PropertyField(criticalChanceUpgradeBaseCostProp);
            EditorGUILayout.PropertyField(criticalChanceCostExponentialMultiplierProp);
            EditorGUILayout.PropertyField(criticalDamageMultiplierProp);
            EditorGUILayout.PropertyField(criticalDamageMultiplierLevelProp);
            EditorGUILayout.PropertyField(criticalDamageMultiplierUpgradeAmountProp);
            EditorGUILayout.PropertyField(criticalDamageMultiplierUpgradeBaseCostProp);
            EditorGUILayout.PropertyField(criticalDamageCostExponentialMultiplierProp);
        }

        private void ShowShotgunInfo()
        {
            EditorGUILayout.PropertyField(pelletCountProp);
            EditorGUILayout.PropertyField(pelletCountLevelProp);
            EditorGUILayout.PropertyField(pelletCountUpgradeAmountProp);
            EditorGUILayout.PropertyField(pelletCountUpgradeBaseCostProp);
            EditorGUILayout.PropertyField(damageFalloffOverDistanceProp);
            EditorGUILayout.PropertyField(damageFalloffOverDistanceLevelProp);
            EditorGUILayout.PropertyField(damageFalloffOverDistanceUpgradeAmountProp);
            EditorGUILayout.PropertyField(damageFalloffOverDistanceUpgradeBaseCostProp);
            EditorGUILayout.PropertyField(knockbackStrengthProp);
            EditorGUILayout.PropertyField(knockbackStrengthLevelProp);
            EditorGUILayout.PropertyField(knockbackStrengthUpgradeAmountProp);
            EditorGUILayout.PropertyField(knockbackStrengthUpgradeBaseCostProp);
            EditorGUILayout.PropertyField(knockbackStrengthCostExponentialMultiplierProp);

        }

        private void ShowSniperInfo()
        {
            EditorGUILayout.PropertyField(pierceChanceProp);
            EditorGUILayout.PropertyField(pierceChanceLevelProp);
            EditorGUILayout.PropertyField(pierceChanceUpgradeAmountProp);
            EditorGUILayout.PropertyField(pierceChanceUpgradeBaseCostProp);
            EditorGUILayout.PropertyField(pierceDamageFalloffProp);
            EditorGUILayout.PropertyField(pierceDamageFalloffLevelProp);
            EditorGUILayout.PropertyField(pierceDamageFalloffUpgradeAmountProp);
            EditorGUILayout.PropertyField(pierceDamageFalloffUpgradeBaseCostProp);
        }

        private void ShowMissileLauncherInfo()
        {
            EditorGUILayout.PropertyField(explosionRadiusProp);
            EditorGUILayout.PropertyField(explosionRadiusLevelProp);
            EditorGUILayout.PropertyField(explosionRadiusUpgradeAmountProp);
            EditorGUILayout.PropertyField(explosionRadiusUpgradeBaseCostProp);
            EditorGUILayout.PropertyField(splashDamageProp);
            EditorGUILayout.PropertyField(splashDamageLevelProp);
            EditorGUILayout.PropertyField(splashDamageUpgradeAmountProp);
            EditorGUILayout.PropertyField(splashDamageUpgradeBaseCostProp);
        }

        private void ShowLaserInfo()
        {
            EditorGUILayout.PropertyField(percentBonusDamagePerSecProp);
            EditorGUILayout.PropertyField(percentBonusDamagePerSecLevelProp);
            EditorGUILayout.PropertyField(percentBonusDamagePerSecUpgradeAmountProp);
            EditorGUILayout.PropertyField(percentBonusDamagePerSecUpgradeBaseCostProp);
            EditorGUILayout.PropertyField(slowEffectProp);
            EditorGUILayout.PropertyField(slowEffectLevelProp);
            EditorGUILayout.PropertyField(slowEffectUpgradeAmountProp);
            EditorGUILayout.PropertyField(slowEffectUpgradeBaseCostProp);
        }
    }
}
