// Assets/Editor/Simulation/CsvExporter.cs
using System.IO;
using UnityEditor;

namespace IdleDefense.Editor.Simulation
{
    static class CsvExporter
    {
        const string Folder = "Assets/SimResults";
        const string FileName = "results.csv";

        // Flag to ensure we only schedule one refresh per batch
        static bool _refreshScheduled = false;

        public static void Append(SimStats stats)
        {
            // 1) Ensure folder exists
            if (!Directory.Exists(Folder))
                Directory.CreateDirectory(Folder);

            // 2) Determine path
            string path = Path.Combine(Folder, FileName);

            // 3) Append header if new file
            bool writeHeader = !File.Exists(path);
            using (var sw = new StreamWriter(path, append: true))
            {
                if (writeHeader)
                    sw.WriteLine("WavesBeaten,EnemiesKilled,TimesDefeated,SimMinutes");

                sw.WriteLine(
                    $"{stats.WavesBeaten}," +
                    $"{stats.EnemiesKilled}," +
                    $"{stats.TimesDefeated}," +
                    $"{stats.SimMinutes}"
                );
            }

            // 4) Schedule a single, deferred AssetDatabase.Refresh()
            if (!_refreshScheduled)
            {
                _refreshScheduled = true;
                EditorApplication.delayCall += DoRefresh;
            }
        }

        private static void DoRefresh()
        {
            AssetDatabase.Refresh();
            _refreshScheduled = false;
            EditorApplication.delayCall -= DoRefresh;
        }
    }
}
