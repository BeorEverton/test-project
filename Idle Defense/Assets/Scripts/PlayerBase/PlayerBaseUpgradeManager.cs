using Assets.Scripts.Systems;
using Assets.Scripts.Systems.Audio;
using Assets.Scripts.Turrets;
using Assets.Scripts.UI;
using Assets.Scripts.UpgradeSystem;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.InputSystem.HID;

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
                    GetCostMultiplier = p => p.MaxHealthUpgradeBaseCost,
                    GetMaxValue = p => float.MaxValue,
                    GetMinValue = p => 0f,
                    GetCost = GetPlayerHealthUpgradeCost,
                    GetDisplayStrings = p =>
                    {
                        float current = p.MaxHealth;
                        float bonus = p.MaxHealthUpgradeAmount;
                        float cost = GetPlayerHealthUpgradeCost(p);

                        return ($"{current:F0}",
                                $"+{bonus:F0}",
                                $"${UIManager.AbbreviateNumber(cost)}");
                    }
                },
                [PlayerUpgradeType.RegenAmount] = new()
                {
                    GetCurrentValue = p => p.RegenAmount,
                    SetCurrentValue = (p, v) => p.RegenAmount = v,
                    GetLevel = p => p.RegenAmountLevel,
                    SetLevel = (p, v) => p.RegenAmountLevel = v,
                    GetUpgradeAmount = p => p.RegenAmountUpgradeAmount,
                    GetCostMultiplier = p => p.RegenAmountUpgradeBaseCost,
                    GetMaxValue = p => float.MaxValue,
                    GetMinValue = p => 0f,
                    GetCost = GetRegenAmountUpgradeCost,
                    GetDisplayStrings = p =>
                    {
                        float current = p.RegenAmount;
                        float bonus = p.RegenAmountUpgradeAmount;
                        float cost = GetRegenAmountUpgradeCost(p);

                        return ($"{current:F0}",
                                $"+{bonus:F0}",
                                $"${UIManager.AbbreviateNumber(cost)}");
                    }
                },
                [PlayerUpgradeType.RegenInterval] = new()
                {
                    GetCurrentValue = p => p.RegenInterval,
                    SetCurrentValue = (p, v) => p.RegenInterval = v,
                    GetLevel = p => p.RegenIntervalLevel,
                    SetLevel = (p, v) => p.RegenIntervalLevel = v,
                    GetUpgradeAmount = p => p.RegenIntervalUpgradeAmount,
                    GetCostMultiplier = p => p.RegenIntervalUpgradeBaseCost,
                    GetMaxValue = p => 0.5f,
                    GetMinValue = p => 0f,
                    GetCost = GetRegenIntervalUpgradeCost,
                    GetDisplayStrings = p =>
                    {
                        float current = p.RegenInterval;
                        float bonus = p.RegenIntervalUpgradeAmount;
                        float cost = GetRegenIntervalUpgradeCost(p);
                        if (current <= 0.5f)
                            return ($"{current:F2}s", "Max", "");

                        return ($"{current:F2}s",
                            $"-{bonus:F2}s",
                            $"${UIManager.AbbreviateNumber(cost)}");
                    }
                }
            };
        }

        private float GetPlayerHealthUpgradeCost(PlayerBaseStatsInstance stats) =>
            stats.MaxHealthUpgradeBaseCost * Mathf.Pow(1.1f, stats.MaxHealthLevel);
        private float GetRegenAmountUpgradeCost(PlayerBaseStatsInstance stats) =>
            stats.RegenAmountUpgradeBaseCost * Mathf.Pow(1.1f, stats.RegenAmountLevel);
        private float GetRegenIntervalUpgradeCost(PlayerBaseStatsInstance stats) =>
            stats.RegenIntervalUpgradeBaseCost * Mathf.Pow(1.1f, stats.RegenIntervalLevel);

        public float GetPlayerBaseUpgradeCost(PlayerBaseStatsInstance turret, PlayerUpgradeType type) =>
            !_playerUpgrades.TryGetValue(type, out PlayerBaseUpgrade upgrade) ? 0f : upgrade.GetCost(turret);

        public void UpgradePlayerBaseStat(PlayerBaseStatsInstance stats, PlayerUpgradeType type, PlayerUpgradeButton button)
        {
            if (!_playerUpgrades.TryGetValue(type, out PlayerBaseUpgrade upgrade))
                return;

            float cost = upgrade.GetCost(stats);

            if (upgrade.GetMaxValue != null && upgrade.GetCurrentValue(stats) >= upgrade.GetMaxValue(stats))
            {
                UpdateUpgradeDisplay(stats, type, button);
                return;
            }

            if (TrySpend(cost))
            {
                float newValue = upgrade.GetCurrentValue(stats) + upgrade.GetUpgradeAmount(stats);
                upgrade.SetCurrentValue(stats, newValue);
                upgrade.SetLevel(stats, upgrade.GetLevel(stats) + 1);
                AudioManager.Instance.Play("Upgrade");
                UpdateUpgradeDisplay(stats, type, button);
            }
        }

        public void UpdateUpgradeDisplay(PlayerBaseStatsInstance stats, PlayerUpgradeType type, PlayerUpgradeButton button)
        {
            if (!_playerUpgrades.TryGetValue(type, out PlayerBaseUpgrade upgrade) || stats == null)
                return;

            (string value, string bonus, string cost) = upgrade.GetDisplayStrings(stats);
            button.UpdateStats(value, bonus, cost);
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