using Assets.Scripts.SO;
using System;
using System.Globalization;
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

                EnemyInfoSO enemyInfo = AssetDatabase.LoadAssetAtPath<EnemyInfoSO>($"Assets/Scriptable Objects/Enemies/{lines[1]}.asset");

                if (enemyInfo == null)
                {
                    enemyInfo = ScriptableObject.CreateInstance<EnemyInfoSO>();
                    AssetDatabase.CreateAsset(enemyInfo, $"Assets/Scriptable Objects/Enemies/{lines[1]}.asset");

                }

                //Update asset's data
                enemyInfo.name = lines[1];
                enemyInfo.Name = lines[1];
                enemyInfo.EnemyClass = enemyClass;
                enemyInfo.MaxHealth = float.Parse(lines[3], CultureInfo.InvariantCulture);
                enemyInfo.MovementSpeed = float.Parse(lines[4], CultureInfo.InvariantCulture);
                enemyInfo.MovementSpeedDifference = float.Parse(lines[5], CultureInfo.InvariantCulture);
                enemyInfo.CoinDropAmount = ulong.Parse(lines[6]);
                enemyInfo.Damage = float.Parse(lines[7], CultureInfo.InvariantCulture);
                enemyInfo.AttackRange = float.Parse(lines[8], CultureInfo.InvariantCulture);
                enemyInfo.AttackSpeed = float.Parse(lines[9], CultureInfo.InvariantCulture);

                //Mark asset dirty so that the changes are saved
                EditorUtility.SetDirty(enemyInfo);
            }

            AssetDatabase.SaveAssets();
        }
    }
}