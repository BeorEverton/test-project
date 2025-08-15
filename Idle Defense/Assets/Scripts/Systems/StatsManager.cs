using Assets.Scripts.SO;
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
        private int _totalZonesSecured;
        private int _enemiesKilled;
        private int _bossesKilled;
        private double _moneySpent;
        private int _upgradeAmount;
        private double _totalDamageTaken;
        private double _totalHealthRepaired;
        private int _missionsFailed;
        private int _speedBoostClicks;

        private double _machinegunDamage;
        private double _shotgunDamage;
        private double _sniperDamage;
        private double _missileLauncherDamage;
        private double _laserDamage;

        private double _startTime;
        private double _gameTime;
        private double _loadedTime;
        private bool oneMin, fiveMin, tenMin, fifteenMin, thirtyMin, oneHour, twoHours,
            threeHours, fiveHours, tenHours, oneDay;


        private Dictionary<TurretType, Action<double>> _damageDictionary;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            _damageDictionary = new Dictionary<TurretType, Action<double>>()
            {
                { TurretType.MachineGun, amount => MachineGunDamage += amount },
                { TurretType.Shotgun, amount => ShotgunDamage += amount },
                { TurretType.Sniper, amount => SniperDamage += amount },
                { TurretType.MissileLauncher, amount => MissileLauncherDamage += amount },
                { TurretType.Laser, amount => LaserDamage += amount }
            };
        }

        void Start()
        {
            _startTime = Time.realtimeSinceStartupAsDouble;
        }

        void FixedUpdate()
        {
            GameTime = (Time.realtimeSinceStartupAsDouble - _startTime) + _loadedTime;
        }

        public void LoadStats(StatsDTO statsDTO)
        {
            TotalDamage = statsDTO.TotalDamage;
            MaxZone = statsDTO.MaxZone;
            TotalZonesSecured = statsDTO.TotalZonesSecured;
            EnemiesKilled = statsDTO.EnemiesKilled;
            BossesKilled = statsDTO.BossesKilled;
            MoneySpent = statsDTO.MoneySpent;
            UpgradeAmount = statsDTO.UpgradeAmount;
            TotalDamageTaken = statsDTO.TotalDamageTaken;
            TotalHealthRepaired = statsDTO.TotalHealthRepaired;
            MissionsFailed = statsDTO.MissionsFailed;
            SpeedBoostClicks = statsDTO.SpeedBoostClicks;
            MachineGunDamage = statsDTO.MachineGunDamage;
            ShotgunDamage = statsDTO.ShotgunDamage;
            SniperDamage = statsDTO.SniperDamage;
            MissileLauncherDamage = statsDTO.MissileLauncherDamage;
            LaserDamage = statsDTO.LaserDamage;
            _loadedTime = statsDTO.GameTime;

            #region Analytics bools
            // _loadedTime is in seconds.
            // Every milestone met (and the ones before it) are switched on.
            switch (_loadedTime)           // still in seconds
            {
                case >= 86_400:            // 1 day
                    oneDay = tenHours = fiveHours = threeHours = twoHours =
                    oneHour = thirtyMin = fifteenMin = tenMin = fiveMin = oneMin = true;
                    break;

                case >= 36_000:            // 10 h
                    tenHours = fiveHours = threeHours = twoHours =
                    oneHour = thirtyMin = fifteenMin = tenMin = fiveMin = oneMin = true;
                    break;

                case >= 18_000:            // 5 h
                    fiveHours = threeHours = twoHours =
                    oneHour = thirtyMin = fifteenMin = tenMin = fiveMin = oneMin = true;
                    break;

                case >= 10_800:            // 3 h
                    threeHours = twoHours =
                    oneHour = thirtyMin = fifteenMin = tenMin = fiveMin = oneMin = true;
                    break;

                case >= 7_200:             // 2 h
                    twoHours =
                    oneHour = thirtyMin = fifteenMin = tenMin = fiveMin = oneMin = true;
                    break;

                case >= 3_600:             // 1 h
                    oneHour = thirtyMin = fifteenMin = tenMin = fiveMin = oneMin = true;
                    break;

                case >= 1_800:             // 30 min
                    thirtyMin = fifteenMin = tenMin = fiveMin = oneMin = true;
                    break;

                case >= 900:               // 15 min
                    fifteenMin = tenMin = fiveMin = oneMin = true;
                    break;

                case >= 600:               // 10 min
                    tenMin = fiveMin = oneMin = true;
                    break;

                case >= 300:               // 5 min
                    fiveMin = oneMin = true;
                    break;

                case >= 60:                // 1 min
                    oneMin = true;
                    break;

                default:                   // < 1 min — nothing yet
                    break;
            }
            #endregion
        }

        public void ResetStats()
        {
            _totalDamage = 0;
            _maxZone = 0;
            _totalZonesSecured = 0;
            _enemiesKilled = 0;
            _bossesKilled = 0;
            _moneySpent = 0;
            _upgradeAmount = 0;
            _totalDamageTaken = 0;
            _totalHealthRepaired = 0;
            _missionsFailed = 0;
            _speedBoostClicks = 0;
            _machinegunDamage = 0;
            _shotgunDamage = 0;
            _sniperDamage = 0;
            _missileLauncherDamage = 0;
            _laserDamage = 0;
            _gameTime = 0;
        }

        private void SetField<T>(ref T field, T newValue, string statName)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
                return;

            field = newValue;
            OnStatChanged?.Invoke(statName, newValue);
        }

        public double GameTime
        {
            get => _gameTime;
            set => SetField(ref _gameTime, value, nameof(GameTime));
        }

        public double TotalDamage
        {
            get => _totalDamage;
            set => SetField(ref _totalDamage, value, nameof(TotalDamage));
        }
        public int MaxZone
        {
            get => _maxZone;
            set
            {
                if (value > _maxZone)
                    SetField(ref _maxZone, value, nameof(MaxZone));
            }
        }

        public int TotalZonesSecured
        {
            get => _totalZonesSecured;
            set => SetField(ref _totalZonesSecured, value, nameof(TotalZonesSecured));
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
        public double TotalDamageTaken
        {
            get => _totalDamageTaken;
            set => SetField(ref _totalDamageTaken, value, nameof(TotalDamageTaken));
        }
        public double TotalHealthRepaired
        {
            get => _totalHealthRepaired;
            set => SetField(ref _totalHealthRepaired, value, nameof(TotalHealthRepaired));
        }
        public int MissionsFailed
        {
            get => _missionsFailed;
            set => SetField(ref _missionsFailed, value, nameof(MissionsFailed));
        }
        public int SpeedBoostClicks
        {
            get => _speedBoostClicks;
            set => SetField(ref _speedBoostClicks, value, nameof(SpeedBoostClicks));
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

        public void AddTurretDamage(TurretType type, double damage)
        {
            if (_damageDictionary.TryGetValue(type, out Action<double> handler))
                handler(damage);
            else
            {
                Debug.LogError($"[STATMANAGER] No damage handler for turret type {type}");
            }
        }
    }
}