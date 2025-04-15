using Assets.Scripts.SO;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
    public class CSVtoSO
    {
        private static string _enemyCsvPath = "/Editor/EnemyInfoCSV.csv";

        [MenuItem("Utilities/Generate EnemyInfoSo")]
        public static void GenerateEnemies()
        {
            string[] allLines = File.ReadAllLines(Application.dataPath + _enemyCsvPath);

            foreach (string line in allLines.Skip(1))
            {
                string[] lines = line.Split(',');

                if (lines[0] == "FALSE") // Skip if enemy not marked as ingame
                    continue;
                Enum.TryParse(lines[2], out EnemyClass enemyClass);

                EnemyInfoSO enemyInfo = ScriptableObject.CreateInstance<EnemyInfoSO>();
                enemyInfo.name = lines[1];
                enemyInfo.Name = lines[1];
                enemyInfo.EnemyClass = enemyClass;
                enemyInfo.MaxHealth = float.Parse(lines[3]);
                enemyInfo.MovementSpeed = float.Parse(lines[4]);
                enemyInfo.MovementSpeedDifference = float.Parse(lines[5]);
                enemyInfo.CoinDropAmount = ulong.Parse(lines[6]);
                enemyInfo.Damage = float.Parse(lines[7]);
                enemyInfo.AttackRange = float.Parse(lines[8]);
                enemyInfo.AttackSpeed = float.Parse(lines[9]);

                AssetDatabase.CreateAsset(enemyInfo, $"Assets/Scriptable Objects/Enemies/{enemyInfo.name}.asset");
            }

            AssetDatabase.SaveAssets();
        }
    }
}