using Assets.Scripts.SO;
using System.Collections.Generic;

namespace Assets.Scripts.WaveSystem
{
    public class Wave
    {
        public WaveConfigSO WaveConfig { get; private set; }
        public int WaveNumber { get; private set; }
        public Dictionary<EnemyClass, EnemyInfoSO> WaveEnemies = new();

        public Wave(WaveConfigSO waveConfig, int waveNumber)
        {
            WaveConfig = waveConfig;
            WaveNumber = waveNumber;
        }

        public void AddEnemyClassToWave(EnemyClass enemyClass, EnemyInfoSO enemyConfig)
        {
            if (WaveEnemies.ContainsKey(enemyClass))
            {
                WaveEnemies.Remove(enemyClass);
            }
            WaveEnemies.Add(enemyClass, enemyConfig);
        }

        public bool IsMiniBossWave() => WaveNumber % 5 == 0 && WaveNumber % 10 != 0; //Every 5th wave is a miniboss wave
        public bool IsBossWave() => WaveNumber % 10 == 0; //Every 10th wave is a boss wave
    }
}