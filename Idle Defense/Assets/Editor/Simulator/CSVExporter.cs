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

        public static void Append(SimStats s, SpendingMode mode)
        {
            if (!Directory.Exists(Folder))
                Directory.CreateDirectory(Folder);

            string path = Path.Combine(Folder, FileName);
            bool writeHeader = !File.Exists(path);

            using var sw = new StreamWriter(path, append: true);
            if (writeHeader)
            {
                sw.WriteLine(
                    "SpendingMode," +
                    "WavesBeaten,EnemiesKilled,BossesKilled,TotalDamageDealt," +
                    "MaxZone,MoneySpent,UpgradeAmount," +
                    // new cols:
                    "UpgradeHistory,TurretStats,BaseStats," +
                    "TotalDamageTaken,TotalHealthRepaired,MissionsFailed," +
                    "SpeedBoostClicks,MachineGunDamage,ShotgunDamage," +
                    "SniperDamage,MissileLauncherDamage,LaserDamage,SimMinutes"
                );

            }

            string history = string.Join("|", s.UpgradeHistory);
            string turretSS = string.Join("|", s.TurretSnapshots);
            string baseSS = string.Join("|", s.BaseSnapshots);


            sw.WriteLine(
                $"{mode}," +
                $"{s.WavesBeaten}," +
                $"{s.EnemiesKilled}," +
                $"{s.BossesKilled}," +
                $"{s.TotalDamageDealt:F2}," +
                $"{s.MaxZone}," +
                $"{s.MoneySpent:F0}," +
                $"{s.UpgradeAmount}," +
                // new fields:
                $"\"{history}\"," +
                $"\"{turretSS}\"," +
                $"\"{baseSS}\"," +
                $"{s.TotalDamageTaken:F2}," +
                $"{s.TotalHealthRepaired:F2}," +
                $"{s.MissionsFailed}," +
                $"{s.SpeedBoostClicks}," +
                $"{s.MachineGunDamage:F2}," +
                $"{s.ShotgunDamage:F2}," +
                $"{s.SniperDamage:F2}," +
                $"{s.MissileLauncherDamage:F2}," +
                $"{s.LaserDamage:F2}," +
                $"{s.SimMinutes:F2}"
    );
            sw.Flush();

            if (!_refreshScheduled)
            {
                _refreshScheduled = true;
                EditorApplication.delayCall += () =>
                {
                    AssetDatabase.Refresh();
                    _refreshScheduled = false;
                };
            }
        }

    }
}
