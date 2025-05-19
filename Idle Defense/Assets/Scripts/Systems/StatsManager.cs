using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    public class StatsManager : MonoBehaviour
    {
        public static StatsManager Instance { get; private set; }

        public event Action<string, object> OnStatChanged;

        private double _totalDamage;
        private int _maxZone;
        private int _enemiesKilled;
        private int _bossesKilled;
        private double _moneySpent;
        private int _upgradeAmount;
        private double _damageTaken;
        private double _healthRepaired;
        private int _missionsFailed;

        private double _machinegunDamage;
        private double _shotgunDamage;
        private double _sniperDamage;
        private double _missileLauncherDamage;
        private double _laserDamage;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private bool SetField<T>(ref T field, T newValue, string statName)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
                return false;
            field = newValue;
            OnStatChanged?.Invoke(statName, newValue);
            return true;
        }

        public double TotalDamage
        {
            get => _totalDamage;
            set => SetField(ref _totalDamage, value, nameof(TotalDamage));
        }
        public int MaxZone
        {
            get => _maxZone;
            set => SetField(ref _maxZone, value, nameof(MaxZone));
        }
        public int EnemiesKilled
        {
            get => _enemiesKilled;
            set => SetField(ref _enemiesKilled, value, nameof(EnemiesKilled));
        }
        public int BossesKilled
        {
            get => _bossesKilled;
            set => SetField(ref _bossesKilled, value, nameof(BossesKilled));
        }
        public double MoneySpent
        {
            get => _moneySpent;
            set => SetField(ref _moneySpent, value, nameof(MoneySpent));
        }
        public int UpgradeAmount
        {
            get => _upgradeAmount;
            set => SetField(ref _upgradeAmount, value, nameof(UpgradeAmount));
        }
        public double DamageTaken
        {
            get => _damageTaken;
            set => SetField(ref _damageTaken, value, nameof(DamageTaken));
        }
        public double HealthRepaired
        {
            get => _healthRepaired;
            set => SetField(ref _healthRepaired, value, nameof(HealthRepaired));
        }
        public int MissionsFailed
        {
            get => _missionsFailed;
            set => SetField(ref _missionsFailed, value, nameof(MissionsFailed));
        }
        public double MachineGunDamage
        {
            get => _machinegunDamage;
            set => SetField(ref _machinegunDamage, value, nameof(MachineGunDamage));
        }
        public double ShotgunDamage
        {
            get => _shotgunDamage;
            set => SetField(ref _shotgunDamage, value, nameof(ShotgunDamage));
        }
        public double SniperDamage
        {
            get => _sniperDamage;
            set => SetField(ref _sniperDamage, value, nameof(SniperDamage));
        }
        public double MissileLauncherDamage
        {
            get => _missileLauncherDamage;
            set => SetField(ref _missileLauncherDamage, value, nameof(MissileLauncherDamage));
        }
        public double LaserDamage
        {
            get => _laserDamage;
            set => SetField(ref _laserDamage, value, nameof(LaserDamage));
        }
    }
}