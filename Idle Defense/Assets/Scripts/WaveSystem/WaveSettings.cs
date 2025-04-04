using Assets.Scripts.SO;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.WaveSystem
{
    public class WaveSettings : MonoBehaviour
    {
        public static WaveSettings Instance { get; private set; }

        private Dictionary<EnemyClass, EnemyInfoSO> _waveConfig = new();
        public Dictionary<EnemyClass, EnemyInfoSO> WaveConfig => _waveConfig;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public void AddEnemyConfig(EnemyClass enemyClass, EnemyInfoSO enemyConfig)
        {
            if (_waveConfig.ContainsKey(enemyClass))
            {
                _waveConfig.Remove(enemyClass);
            }
            _waveConfig.Add(enemyClass, enemyConfig);
        }
    }
}