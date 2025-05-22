using UnityEditor;
using UnityEngine;

namespace IdleDefense.Editor.Simulation
{
    public class SimulationRunnerWindow : EditorWindow
    {
        private float minutes = 10f;
        private float clicksPerSec = 3f;
        private int iterations = 50;
        private SpendingMode mode = SpendingMode.MostEffective;

        [MenuItem("Idle Defense/Run Simulation…")]
        static void Open() => GetWindow<SimulationRunnerWindow>("Simulator");

        void OnGUI()
        {
            GUILayout.Label("Simulation Settings", EditorStyles.boldLabel);
            minutes = EditorGUILayout.FloatField("Sim Minutes", minutes);
            clicksPerSec = EditorGUILayout.FloatField("Clicks/sec", clicksPerSec);
            iterations = EditorGUILayout.IntField("Iterations", iterations);
            mode = (SpendingMode)EditorGUILayout.EnumPopup("Spending Mode", mode);

            if (GUILayout.Button("Run"))
            {
                for (int i = 0; i < iterations; i++)
                {
                    var stats = SimulationEngine.Run(minutes, clicksPerSec, mode);
                    CsvExporter.Append(stats);
                }
                Debug.Log($" Simulation done ({iterations} runs) - Assets/SimResults/results.csv");
            }
        }
    }
}
