using Assets.Scripts.SO;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    public class StatsManager : MonoBehaviour
    {
        public static StatsManager Instance { get; private set; }

        public event Action<string, object> OnStatChanged;

        // Non-turret stats (unchanged)
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

        private double _startTime;
        private double _gameTime;
        private double _loadedTime;
        private bool oneMin, fiveMin, tenMin, fifteenMin, thirtyMin, oneHour, twoHours,
            threeHours, fiveHours, tenHours, oneDay;

        // centralized tracker for all turret damage
        private TurretDamageTracker _damageTracker;

        // Keep your public entry point for “add damage per type”
        private IReadOnlyDictionary<TurretType, Action<double>> _damageDictionary;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            // Build the tracker and pipe its changes to your OnStatChanged listeners.
            _damageTracker = new TurretDamageTracker();
            _damageTracker.DamageChanged += (type, value) =>
            {
                // Always raise with the new canonical name (e.g., "DoubleSplitterDamage")
                OnStatChanged?.Invoke(PropertyNameFor(type), value);

                // Also raise legacy aliases so old listeners keep working.
                foreach (var alias in GetLegacyAliases(type))
                    OnStatChanged?.Invoke(alias, value);
            };

            _damageDictionary = _damageTracker.DamageDictionary;
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
            // non-turret stats
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

            // legacy DTO fields — routed through aliases so old saves/loaders work
            MachineGunDamage = statsDTO.MachineGunDamage;
            ShotgunDamage = statsDTO.ShotgunDamage;          // alias → DoubleSplitter
            SniperDamage = statsDTO.SniperDamage;
            MissileLauncherDamage = statsDTO.MissileLauncherDamage;  // alias → SteamMortar
            LaserDamage = statsDTO.LaserDamage;            // alias → ObsidianLens

            _loadedTime = statsDTO.GameTime;

            #region Analytics bools (unchanged)
            switch (_loadedTime)
            {
                case >= 86_400: oneDay = tenHours = fiveHours = threeHours = twoHours = oneHour = thirtyMin = fifteenMin = tenMin = fiveMin = oneMin = true; break;
                case >= 36_000: tenHours = fiveHours = threeHours = twoHours = oneHour = thirtyMin = fifteenMin = tenMin = fiveMin = oneMin = true; break;
                case >= 18_000: fiveHours = threeHours = twoHours = oneHour = thirtyMin = fifteenMin = tenMin = fiveMin = oneMin = true; break;
                case >= 10_800: threeHours = twoHours = oneHour = thirtyMin = fifteenMin = tenMin = fiveMin = oneMin = true; break;
                case >= 7_200: twoHours = oneHour = thirtyMin = fifteenMin = tenMin = fiveMin = oneMin = true; break;
                case >= 3_600: oneHour = thirtyMin = fifteenMin = tenMin = fiveMin = oneMin = true; break;
                case >= 1_800: thirtyMin = fifteenMin = tenMin = fiveMin = oneMin = true; break;
                case >= 900: fifteenMin = tenMin = fiveMin = oneMin = true; break;
                case >= 600: tenMin = fiveMin = oneMin = true; break;
                case >= 300: fiveMin = oneMin = true; break;
                case >= 60: oneMin = true; break;
                default: break;
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

            // Reset ALL turrets in one go (fix for legacy per-field resets)
            _damageTracker.ResetAll(0);

            _gameTime = 0;
        }

        private void SetField<T>(ref T field, T newValue, string statName)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
                return;

            field = newValue;
            OnStatChanged?.Invoke(statName, newValue);
        }

        // -------------------------
        // Non-turret public props
        // -------------------------
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
            set { if (value > _maxZone) SetField(ref _maxZone, value, nameof(MaxZone)); }
        }

        public int TotalZonesSecured { get => _totalZonesSecured; set => SetField(ref _totalZonesSecured, value, nameof(TotalZonesSecured)); }
        public int EnemiesKilled { get => _enemiesKilled; set => SetField(ref _enemiesKilled, value, nameof(EnemiesKilled)); }
        public int BossesKilled { get => _bossesKilled; set => SetField(ref _bossesKilled, value, nameof(BossesKilled)); }
        public double MoneySpent { get => _moneySpent; set => SetField(ref _moneySpent, value, nameof(MoneySpent)); }
        public int UpgradeAmount { get => _upgradeAmount; set => SetField(ref _upgradeAmount, value, nameof(UpgradeAmount)); }
        public double TotalDamageTaken { get => _totalDamageTaken; set => SetField(ref _totalDamageTaken, value, nameof(TotalDamageTaken)); }
        public double TotalHealthRepaired { get => _totalHealthRepaired; set => SetField(ref _totalHealthRepaired, value, nameof(TotalHealthRepaired)); }
        public int MissionsFailed { get => _missionsFailed; set => SetField(ref _missionsFailed, value, nameof(MissionsFailed)); }
        public int SpeedBoostClicks { get => _speedBoostClicks; set => SetField(ref _speedBoostClicks, value, nameof(SpeedBoostClicks)); }

        // -------------------------
        // Turret damage properties
        // -------------------------
        public double MachineGunDamage { get => _damageTracker[TurretType.MachineGun]; set => _damageTracker[TurretType.MachineGun] = value; }
        public double DoubleSplitterDamage { get => _damageTracker[TurretType.DoubleSplitter]; set => _damageTracker[TurretType.DoubleSplitter] = value; }
        public double SniperDamage { get => _damageTracker[TurretType.Sniper]; set => _damageTracker[TurretType.Sniper] = value; }
        public double VolatileFlaskLobberDamage { get => _damageTracker[TurretType.VolatileFlaskLobber]; set => _damageTracker[TurretType.VolatileFlaskLobber] = value; }
        public double ObsidianLensDamage { get => _damageTracker[TurretType.ObsidianLens]; set => _damageTracker[TurretType.ObsidianLens] = value; }
        public double WrenchSpinnerDamage { get => _damageTracker[TurretType.WrenchSpinner]; set => _damageTracker[TurretType.WrenchSpinner] = value; }
        public double FlameBelcherDamage { get => _damageTracker[TurretType.FlameBelcher]; set => _damageTracker[TurretType.FlameBelcher] = value; }
        public double SteamMortarDamage { get => _damageTracker[TurretType.SteamMortar]; set => _damageTracker[TurretType.SteamMortar] = value; }
        public double ClockBombDistributorDamage { get => _damageTracker[TurretType.ClockBombDistributor]; set => _damageTracker[TurretType.ClockBombDistributor] = value; }
        public double TeslaArcDamage { get => _damageTracker[TurretType.TeslaArc]; set => _damageTracker[TurretType.TeslaArc] = value; }
        public double RicochetSpikesDamage { get => _damageTracker[TurretType.RicochetSpikes]; set => _damageTracker[TurretType.RicochetSpikes] = value; }
        public double FrostTetherDamage { get => _damageTracker[TurretType.FrostTether]; set => _damageTracker[TurretType.FrostTether] = value; }
        public double GeargrinderDamage { get => _damageTracker[TurretType.Geargrinder]; set => _damageTracker[TurretType.Geargrinder] = value; }
        public double AlchemicalSprayerDamage { get => _damageTracker[TurretType.AlchemicalSprayer]; set => _damageTracker[TurretType.AlchemicalSprayer] = value; }
        public double HammerSlammerDamage { get => _damageTracker[TurretType.HammerSlammer]; set => _damageTracker[TurretType.HammerSlammer] = value; }
        public double GreenGooBombDispenserDamage { get => _damageTracker[TurretType.GreenGooBombDispenser]; set => _damageTracker[TurretType.GreenGooBombDispenser] = value; }
        public double CapacitorCannonDamage { get => _damageTracker[TurretType.CapacitorCannon]; set => _damageTracker[TurretType.CapacitorCannon] = value; }
        public double VaporJetTurretDamage { get => _damageTracker[TurretType.VaporJetTurret]; set => _damageTracker[TurretType.VaporJetTurret] = value; }
        public double ThermiteCoreDamage { get => _damageTracker[TurretType.ThermiteCore]; set => _damageTracker[TurretType.ThermiteCore] = value; }
        public double HeavySlammerDamage { get => _damageTracker[TurretType.HeavySlammer]; set => _damageTracker[TurretType.HeavySlammer] = value; }
        public double SteamSawCutterDamage { get => _damageTracker[TurretType.SteamSawCutter]; set => _damageTracker[TurretType.SteamSawCutter] = value; }
        public double PulseSlammerDamage { get => _damageTracker[TurretType.PulseSlammer]; set => _damageTracker[TurretType.PulseSlammer] = value; }
        public double EtherNeedleDamage { get => _damageTracker[TurretType.EtherNeedle]; set => _damageTracker[TurretType.EtherNeedle] = value; }
        public double RailSpikeDriverDamage { get => _damageTracker[TurretType.RailSpikeDriver]; set => _damageTracker[TurretType.RailSpikeDriver] = value; }
        public double EnergyCondenserDamage { get => _damageTracker[TurretType.EnergyCondenser]; set => _damageTracker[TurretType.EnergyCondenser] = value; }
        public double MagneticRevolverDamage { get => _damageTracker[TurretType.MagneticRevolver]; set => _damageTracker[TurretType.MagneticRevolver] = value; }
        public double OozeNetLauncherDamage { get => _damageTracker[TurretType.OozeNetLauncher]; set => _damageTracker[TurretType.OozeNetLauncher] = value; }
        public double ReverseTimeModuleDamage { get => _damageTracker[TurretType.ReverseTimeModule]; set => _damageTracker[TurretType.ReverseTimeModule] = value; }
        public double WeakpointFinderDamage { get => _damageTracker[TurretType.WeakpointFinder]; set => _damageTracker[TurretType.WeakpointFinder] = value; }
        public double DartLauncherDamage { get => _damageTracker[TurretType.DartLauncher]; set => _damageTracker[TurretType.DartLauncher] = value; }
        public double MagnetSpikecasterDamage { get => _damageTracker[TurretType.MagnetSpikecaster]; set => _damageTracker[TurretType.MagnetSpikecaster] = value; }
        public double FungalOvergrowthPodDamage { get => _damageTracker[TurretType.FungalOvergrowthPod]; set => _damageTracker[TurretType.FungalOvergrowthPod] = value; }
        public double TrapGridProjectorDamage { get => _damageTracker[TurretType.TrapGridProjector]; set => _damageTracker[TurretType.TrapGridProjector] = value; }
        public double ExplosiveMineshaftDamage { get => _damageTracker[TurretType.ExplosiveMineshaft]; set => _damageTracker[TurretType.ExplosiveMineshaft] = value; }

        // Legacy aliases — keep old call sites working with zero changes.
        // If your mapping differs, change the target properties here.
        public double ShotgunDamage
        {
            get => DoubleSplitterDamage;          // alias mapping
            set => DoubleSplitterDamage = value;  // alias mapping
        }

        public double MissileLauncherDamage
        {
            get => SteamMortarDamage;             // alias mapping
            set => SteamMortarDamage = value;     // alias mapping
        }

        public double LaserDamage
        {
            get => ObsidianLensDamage;            // alias mapping
            set => ObsidianLensDamage = value;    // alias mapping
        }

        public void AddTurretDamage(TurretType type, double damage)
        {
            if (_damageDictionary.TryGetValue(type, out var handler))
                handler(damage);
            else
                Debug.LogError($"[STATSMANAGER] No damage handler for turret type {type}");
        }

        // Helpers
        private static string PropertyNameFor(TurretType type) => $"{type}Damage";

        private static IEnumerable<string> GetLegacyAliases(TurretType type)
        {
            if (type == TurretType.DoubleSplitter) yield return nameof(ShotgunDamage);
            if (type == TurretType.SteamMortar) yield return nameof(MissileLauncherDamage);
            if (type == TurretType.ObsidianLens) yield return nameof(LaserDamage);
        }
    }
}

/// <summary>
/// Scalable damage model: no per-turret fields or properties.
/// </summary>
public class TurretDamageTracker
{
    private readonly double[] _damageByType;
    private readonly TurretType[] _types;
    private readonly Dictionary<TurretType, Action<double>> _addDamage;

    public event Action<TurretType, double> DamageChanged;

    public TurretDamageTracker()
    {
        _types = (TurretType[])Enum.GetValues(typeof(TurretType));

        // Enum must be contiguous 0..N-1 for array indexing
        bool isContiguous = _types.Select(t => (int)t).OrderBy(i => i).SequenceEqual(Enumerable.Range(0, _types.Length));
        if (!isContiguous)
            throw new InvalidOperationException("TurretType must have contiguous values starting at 0.");

        _damageByType = new double[_types.Length];

        _addDamage = _types.ToDictionary(
            t => t,
            t => (Action<double>)(amount => AddDamage(t, amount))
        );
    }

    public double this[TurretType type]
    {
        get => _damageByType[(int)type];
        set => SetDamage(type, value);
    }

    public IReadOnlyDictionary<TurretType, Action<double>> DamageDictionary => _addDamage;

    public void AddDamage(TurretType type, double amount)
    {
        int i = (int)type;
        double newVal = _damageByType[i] + amount;
        if (!newVal.Equals(_damageByType[i]))
        {
            _damageByType[i] = newVal;
            DamageChanged?.Invoke(type, newVal);
        }
    }

    public void SetDamage(TurretType type, double value)
    {
        int i = (int)type;
        if (!_damageByType[i].Equals(value))
        {
            _damageByType[i] = value;
            DamageChanged?.Invoke(type, value);
        }
    }

    public void ResetAll(double to = 0)
    {
        for (int i = 0; i < _damageByType.Length; i++)
            _damageByType[i] = to;
        // No event spam on full reset; raise totals elsewhere if needed.
    }
}
