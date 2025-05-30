using Assets.Scripts.Systems;
using Assets.Scripts.Systems.Audio;
using Assets.Scripts.UI;
using Assets.Scripts.UpgradeSystem;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.PlayerBase
{
    public class PlayerBaseUpgradeManager : MonoBehaviour
    {
        private Dictionary<PlayerUpgradeType, PlayerBaseUpgrade> _playerUpgrades;

        private void Start()
        {
            InitializeUpgrades();
        }

        private void InitializeUpgrades()
        {
            _playerUpgrades = new Dictionary<PlayerUpgradeType, PlayerBaseUpgrade>
            {
                [PlayerUpgradeType.MaxHealth] = new()
                {
                    GetCurrentValue = p => p.MaxHealth,
                    SetCurrentValue = (p, v) => p.MaxHealth = v,
                    GetLevel = p => p.MaxHealthLevel,
                    SetLevel = (p, v) => p.MaxHealthLevel = v,
                    GetUpgradeAmount = p => p.MaxHealthUpgradeAmount,
                    GetBaseCost = p => p.MaxHealthUpgradeBaseCost,
                    GetMaxValue = p => float.MaxValue,
                    GetMinValue = p => 0f,
                    GetCost = (p, a) => GetCost(p, PlayerUpgradeType.MaxHealth, a),
                    GetAmount = p => GetMaxAmount(p.MaxHealthUpgradeBaseCost, 1.1f, p.MaxHealthLevel),
                    GetDisplayStrings = (p, a) =>
                    {
                        float current = p.MaxHealth;
                        float bonus = p.MaxHealthUpgradeAmount;
                        GetCost(p, PlayerUpgradeType.MaxHealth, a, out float cost, out int amount);

                        return ($"{current:F0}",
                                $"+{bonus:F0}",
                                $"${UIManager.AbbreviateNumber(cost)}",
                                $"{amount:F0}X");
                    }
                },
                [PlayerUpgradeType.RegenAmount] = new()
                {
                    GetCurrentValue = p => p.RegenAmount,
                    SetCurrentValue = (p, v) => p.RegenAmount = v,
                    GetLevel = p => p.RegenAmountLevel,
                    SetLevel = (p, v) => p.RegenAmountLevel = v,
                    GetUpgradeAmount = p => p.RegenAmountUpgradeAmount,
                    GetBaseCost = p => p.RegenAmountUpgradeBaseCost,
                    GetMaxValue = p => float.MaxValue,
                    GetMinValue = p => 0f,
                    GetCost = (p, a) => GetCost(p, PlayerUpgradeType.RegenAmount, a),
                    GetAmount = p => GetMaxAmount(p.RegenAmountUpgradeBaseCost, 1.1f, p.RegenAmountLevel),
                    GetDisplayStrings = (p, a) =>
                    {
                        float current = p.RegenAmount;
                        float bonus = p.RegenAmountUpgradeAmount;
                        GetCost(p, PlayerUpgradeType.RegenAmount, a, out float cost, out int amount);

                        return ($"{current:F2}",
                                $"+{bonus:F2}",
                                $"${UIManager.AbbreviateNumber(cost)}",
                                $"{amount:F0}X");
                    }
                },
                [PlayerUpgradeType.RegenInterval] = new()
                {
                    GetCurrentValue = p => p.RegenInterval,
                    SetCurrentValue = (p, v) => p.RegenInterval = v,
                    GetLevel = p => p.RegenIntervalLevel,
                    SetLevel = (p, v) => p.RegenIntervalLevel = v,
                    GetUpgradeAmount = p => p.RegenIntervalUpgradeAmount,
                    GetBaseCost = p => p.RegenIntervalUpgradeBaseCost,
                    GetMaxValue = p => 0.5f,
                    GetMinValue = p => 0f,
                    GetCost = (p, a) => GetCost(p, PlayerUpgradeType.RegenInterval, a),
                    GetAmount = p => GetMaxAmount(p.RegenIntervalUpgradeBaseCost, 1.1f, p.RegenIntervalLevel),
                    GetDisplayStrings = (p, a) =>
                    {
                        float current = p.RegenInterval;
                        float bonus = p.RegenIntervalUpgradeAmount;
                        GetCost(p, PlayerUpgradeType.RegenInterval, a, out float cost, out int amount);

                        if (current <= 0.5f)
                            return ($"{current:F2}s", "Max", "", "0");

                        return ($"{current:F2}s",
                            $"-{bonus:F2}s",
                            $"${UIManager.AbbreviateNumber(cost)}",
                            $"{amount:F0}X");
                    }
                }
            };
        }

        public float GetPlayerBaseUpgradeCost(PlayerBaseStatsInstance turret, PlayerUpgradeType type, int amount) =>
            !_playerUpgrades.TryGetValue(type, out PlayerBaseUpgrade upgrade) ? 0f : upgrade.GetCost(turret, amount);

        public int GetPlayerBaseAvailableUpgradeAmount(PlayerBaseStatsInstance turret, PlayerUpgradeType type) =>
            !_playerUpgrades.TryGetValue(type, out PlayerBaseUpgrade upgrade) ? 0 : upgrade.GetAmount(turret);

        public void UpgradePlayerBaseStat(PlayerBaseStatsInstance stats, PlayerUpgradeType type, PlayerUpgradeButton button)
        {
            if (!_playerUpgrades.TryGetValue(type, out PlayerBaseUpgrade upgrade))
                return;

            int amount = MultipleBuyOption.Instance.GetBuyAmount();
            GetCost(stats, type, amount, out float cost, out int maxAmount);


            if (upgrade.GetMaxValue != null && upgrade.GetCurrentValue(stats) >= upgrade.GetMaxValue(stats))
            {
                UpdateUpgradeDisplay(stats, type, button);
                return;
            }

            if (TrySpend(cost))
            {
                float newValue = upgrade.GetCurrentValue(stats) + (upgrade.GetUpgradeAmount(stats) * maxAmount);
                upgrade.SetCurrentValue(stats, newValue);
                upgrade.SetLevel(stats, upgrade.GetLevel(stats) + 1);
                AudioManager.Instance.Play("Upgrade");
                UpdateUpgradeDisplay(stats, type, button);
                PlayerBaseManager.Instance.UpdatePlayerBaseAppearance();

                if (type == PlayerUpgradeType.MaxHealth)
                    PlayerBaseManager.Instance.InvokeHealthChangedEvents();
            }
        }

        private void GetCost(PlayerBaseStatsInstance stats, PlayerUpgradeType type, int amount, out float cost, out int maxAmount)
        {
            if (!_playerUpgrades.TryGetValue(type, out PlayerBaseUpgrade upgrade))
            {
                cost = 0;
                maxAmount = 0;
                return;
            }

            int currentLevel = upgrade.GetLevel(stats);
            float baseCost = upgrade.GetBaseCost(stats);
            const float multiplier = 1.1f;

            maxAmount = amount == 9999
                ? GetMaxAmount(baseCost, multiplier, currentLevel) == 0
                    ? 1
                    : GetMaxAmount(baseCost, multiplier, currentLevel)
                : amount;

            cost = RecursiveCost(baseCost, multiplier, currentLevel, maxAmount);
        }

        private float GetCost(PlayerBaseStatsInstance stats, PlayerUpgradeType type, int amount)
        {
            if (!_playerUpgrades.TryGetValue(type, out PlayerBaseUpgrade upgrade))
            {
                return 0f;
            }

            int currentLevel = upgrade.GetLevel(stats);
            float baseCost = upgrade.GetBaseCost(stats);
            const float multiplier = 1.1f;

            if (amount == 9999)
                amount = GetMaxAmount(baseCost, multiplier, currentLevel);

            return RecursiveCost(baseCost, multiplier, currentLevel, amount);
        }

        private int GetMaxAmount(float baseCost, float multiplier, int currentLevel)
        {
            int amount = 0;
            float totalCost = 0f;
            float money = GameManager.Instance.Money;

            while (true)
            {
                float cost = baseCost * Mathf.Pow(multiplier, currentLevel + amount);
                if (totalCost + cost > money)
                    break;
                totalCost += cost;
                amount++;
            }

            return amount;
        }

        private float RecursiveCost(float baseCost, float multiplier, int level, int amount)
        {
            if (amount == 0)
                return 0f;
            float cost = baseCost * Mathf.Pow(multiplier, level);
            return cost + RecursiveCost(baseCost, multiplier, level + 1, amount - 1);
        }

        public void UpdateUpgradeDisplay(PlayerBaseStatsInstance stats, PlayerUpgradeType type, PlayerUpgradeButton button)
        {
            if (!_playerUpgrades.TryGetValue(type, out PlayerBaseUpgrade upgrade) || stats == null)
                return;

            int amount = MultipleBuyOption.Instance.GetBuyAmount();

            (string value, string bonus, string cost, string count) = upgrade.GetDisplayStrings(stats, amount);
            button.UpdateStats(value, bonus, cost, count);
        }

        private bool TrySpend(float cost) => GameManager.Instance.TrySpend(cost);
    }

    public enum PlayerUpgradeType
    {
        MaxHealth,
        RegenAmount,
        RegenInterval
    }
}