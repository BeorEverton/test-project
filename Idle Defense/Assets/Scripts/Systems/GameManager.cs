using Assets.Scripts.Systems.Audio;
using Assets.Scripts.UI;
using Assets.Scripts.WaveSystem;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public PlayerInput Input { get; private set; }
        public GameState CurrentGameState { get; private set; } = GameState.InGame;
        public event Action<GameState> OnGameStateChanged;

        // Currency Management
        [SerializeField]
        [Tooltip("If true, normal enemy currency is only added when pickups are collected/expired instead of on death.")]
        private bool awardCurrencyOnPickup = false;
        public bool AwardCurrencyOnPickup => awardCurrencyOnPickup;
        private readonly Dictionary<Currency, ulong> currencies = new Dictionary<Currency, ulong>
        {
            { Currency.Scraps, 0UL },
            { Currency.BlackSteel, 0UL },
            { Currency.CrimsonCore, 0UL }
        };

        public ulong GetCurrency(Currency currency) => currencies[currency];

        public event Action<Currency, ulong> OnCurrencyChanged;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            foreach (Currency currency in Enum.GetValues(typeof(Currency)))
            {
                currencies[currency] = 0;
            }
        }

        private void Start()
        {
            EnemySpawner.Instance.OnEnemyDeath += OnEnemyDeath;
        }


        private void OnEnemyDeath(object sender, EnemySpawner.OnEnemyDeathEventArgs e)
        {
            // add currency immediately on death, visual pickups are just cosmetics
            if (!awardCurrencyOnPickup)
            {
                AddCurrency(e.CurrencyType, e.Amount);
            }
            // else, we do nothing CurrencyPickupManager will award the currency when pickups are collected or expire
        }


        public void AddCurrency(Currency currency, ulong amount)
        {
            currencies[currency] += amount;
            OnCurrencyChanged?.Invoke(currency, currencies[currency]);
        }

        public bool TrySpendCurrency(Currency currency, ulong cost)
        {
            if (currencies[currency] >= cost)
            {
                SpendCurrency(currency, cost);
                if (currency == Currency.Scraps)
                    StatsManager.Instance.UpgradeAmount++;
                return true;
            }

            AudioManager.Instance.Play("No Money");
            return false;
        }

        public void SpendCurrency(Currency currency, ulong amount)
        {
            currencies[currency] -= amount;

            if (currency == Currency.Scraps)
                StatsManager.Instance.MoneySpent += amount;

            OnCurrencyChanged?.Invoke(currency, currencies[currency]);
        }

        public void LoadCurrency(Currency currency, ulong amount)
        {
            if (!currencies.ContainsKey(currency))
            {
                currencies[currency] = 0;
            }

            currencies[currency] = amount;

            OnCurrencyChanged?.Invoke(currency, amount);
        }

        public void ResetGame()
        {
            foreach (Currency currency in Enum.GetValues(typeof(Currency)))
            {
                currencies[currency] = 0;
            }
            CurrentGameState = GameState.InGame;
        }

        public void DebugAddMoneyInt(int moneyToAdd)
        {
            AddCurrency(Currency.Scraps, (ulong)moneyToAdd);
        }

        public void DebugAddBlackSteel(int moneyToAdd)
        {
            AddCurrency(Currency.BlackSteel, (ulong)moneyToAdd);
        }

        public void ChangeGameState(GameState newState)
        {
            if (CurrentGameState == newState) return; // No change needed
            if (newState == GameState.Management)
            {
                currencies[Currency.Scraps] = 0;
                OnCurrencyChanged?.Invoke(Currency.Scraps, 0);
            }
            CurrentGameState = newState;
            OnGameStateChanged?.Invoke(newState);
            UIManager.Instance.ToggleUpgradePanels(newState);
        }
    }
}


public enum GameState
{
    InGame,
    Management
}

public enum Currency
{
    Scraps,      // ⚙ U+2699 – The junk that keeps your machines moving... for now.
    BlackSteel,  // §  U+00A7 – Forged in battle. Reinforced between wars. Used to rebuild and evolve.
    CrimsonCore  // Ø  U+00D8 – Red-hot cores forged in chaos. Unlocking them reshapes your arsenal.

}