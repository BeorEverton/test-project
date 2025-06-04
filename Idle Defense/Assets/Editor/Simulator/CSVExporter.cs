// Assets/Editor/Simulation/CsvExporter.cs
using System.IO;
using UnityEditor;

namespace IdleDefense.Editor.Simulation
{
    static class CsvExporter
    {
        const string Folder = "Assets/SimResults";
        const string FileName = "results.csv";
        static bool _refreshScheduled = false;

        public static void Append(SimStats s, SpendingMode mode, int simIndex)
        {
            if (!Directory.Exists(Folder)) Directory.CreateDirectory(Folder);
            string path = Path.Combine(Folder, FileName);
            bool writeHeader = !File.Exists(path);

            using var sw = new StreamWriter(path, append: true);
            if (writeHeader)
                sw.WriteLine("SimIndex,SpendingMode,Wave,EnemiesSpawned,EnemiesKilled,BossesKilled,DamageDealt,MaxZone," +
                    "MoneyEarned,MoneySpent,TurretUps,BaseUps,DamageTaken,HealthStart," +
                    "HealthEnd,HealthRegen,WaveBeaten,SpeedClicks," +
                    "MaxHpLvl,RegenAmtLvl,RegenIntLvl,CurrentMaxHp,CurrentRegenAmt,CurrentRegenInt," +
                    "MG,SG,SN,ML,LA," +
                    "DamageUpgrades,FireRateUpgrades,CriticalChanceUpgrades," +
                    "CriticalDamageMultiplierUpgrades,ExplosionRadiusUpgrades," +
                    "SplashDamageUpgrades,PierceChanceUpgrades," +
                    "PierceDamageFalloffUpgrades,PelletCountUpgrades," +
                    "KnockbackStrengthUpgrades,DamageFalloffOverDistanceUpgrades," +
                    "PercentBonusDamagePerSecUpgrades,SlowEffectUpgrades," +
                    "TurretMoneySpent,Minutes,Slot1,Slot2,Slot3,Slot4,Slot5");



            // header row for this sim
            sw.WriteLine($"Simulation {simIndex}");

            // one row per wave
            foreach (var w in s.Waves)
            {
                sw.WriteLine($"{simIndex},{mode},{w.Wave},{w.EnemiesSpawned},{w.EnemiesKilled},{w.BossesKilled}," +
                    $"{w.DamageDealt:F2},{s.MaxZone},{w.MoneyEarned},{w.MoneySpent}," +
                    $"{w.TurretUpgrades},{w.BaseUpgrades},{w.DamageTaken},{w.HealthStart}," +
                    $"{w.HealthEnd},{w.HealthRegen},{(w.WaveBeaten ? 1 : 0)},{w.SpeedBoostClicks}," +
                    $"{w.MaxHealthLevel},{w.RegenAmountLevel},{w.RegenIntervalLevel}," +
                    $"{w.CurrentMaxHealth:F1},{w.CurrentRegenAmount:F2},{w.CurrentRegenInterval:F2}," +
                    $"{w.MachineGunDamage:F2},{w.ShotgunDamage:F2},{w.SniperDamage:F2}," +
                    $"{w.MissileLauncherDamage:F2},{w.LaserDamage:F2}," +
                    $"{w.DamageUpgrades},{w.FireRateUpgrades}," +
                    $"{w.CriticalChanceUpgrades},{w.CriticalDamageMultiplierUpgrades}," +
                    $"{w.ExplosionRadiusUpgrades},{w.SplashDamageUpgrades}," +
                    $"{w.PierceChanceUpgrades},{w.PierceDamageFalloffUpgrades}," +
                    $"{w.PelletCountUpgrades},{w.KnockbackStrengthUpgrades}," +
                    $"{w.DamageFalloffOverDistanceUpgrades}," +
                    $"{w.PercentBonusDamagePerSecUpgrades},{w.SlowEffectUpgrades}," +
                    $"{w.TurretMoneySpent},{s.SimMinutes}, " +
                    $"{w.Slot1},{w.Slot2},{w.Slot3},{w.Slot4},{w.Slot5}");

            }

            sw.Flush();
            if (!_refreshScheduled)
            {
                _refreshScheduled = true;
                EditorApplication.delayCall += () => { AssetDatabase.Refresh(); _refreshScheduled = false; };
            }
        }

    }
}
