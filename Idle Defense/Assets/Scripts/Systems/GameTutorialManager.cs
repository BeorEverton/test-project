using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Assets.Scripts.WaveSystem;
using Assets.Scripts.Turrets;
using Assets.Scripts.Systems.Save;

namespace Assets.Scripts.Systems
{
    public class GameTutorialManager : MonoBehaviour
    {
        [Header("UI Reference")]
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private TypingText typingText;

        [Header("Steps")]
        [SerializeField] private List<TutorialStep> tutorialSteps;

        public int _currentStep = 0;
        private bool _tutorialRunning = false;
        private bool _waitingToStartStep = true;
        private List<GameObject> _lastActiveObjects = new();
        private ulong _lastKnownMoney = 0;
        private bool _turretWasUpgraded = false;


        public static GameTutorialManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }


        private void Start()
        {
            if (tutorialSteps == null || tutorialSteps.Count == 0)
                return;

            if (_currentStep >= tutorialSteps.Count)
            {
                tutorialPanel.SetActive(false);
                return;
            }

            SubscribeToEvents();
            TryAdvanceTutorial();

            if (_currentStep == 0)
            {
                Time.timeScale = 0f; // Pause the game
            }

            _tutorialRunning = true;
            _waitingToStartStep = true;

            Debug.Log("Tutorial started!");
        }

        private void SubscribeToEvents()
        {
            GameManager.OnSpdBonusChanged += OnGameEventTriggered;
            PlayerBaseManager.Instance.OnHealthChanged += OnHealthChanged;
            WaveManager.Instance.OnWaveStarted += OnWaveStarted;
            GameManager.Instance.OnMoneyChanged += OnMoneyChanged;
            PlayerBaseManager.Instance.OnMaxHealthChanged += OnMaxHealthChanged;
            TurretSlotManager.I.OnEquippedChanged += OnAnyTurretEquipped;
            TurretSlotManager.I.OnSlotUnlocked += TryAdvanceTutorial;
            TurretUpgradeManager.OnAnyTurretUpgraded += OnAnyTurretUpgraded;
        }

        private void UnsubscribeFromEvents()
        {
            GameManager.OnSpdBonusChanged -= OnGameEventTriggered;
            PlayerBaseManager.Instance.OnHealthChanged -= OnHealthChanged;
            WaveManager.Instance.OnWaveStarted -= OnWaveStarted;
            GameManager.Instance.OnMoneyChanged -= OnMoneyChanged;
            PlayerBaseManager.Instance.OnMaxHealthChanged -= OnHealthChanged;
            TurretSlotManager.I.OnEquippedChanged -= OnAnyTurretEquipped;
            TurretSlotManager.I.OnSlotUnlocked -= TryAdvanceTutorial;
            TurretUpgradeManager.OnAnyTurretUpgraded -= OnAnyTurretUpgraded;

        }

        private void OnGameEventTriggered(float _)
        {
            TryAdvanceTutorial();
        }
        private void OnMaxHealthChanged(float maxHealth, float current)
        {
            TryAdvanceTutorial();
        }

        private void OnHealthChanged(float current, float max)
        {
            TryAdvanceTutorial();
        }

        private void OnWaveStarted(object sender, WaveManager.OnWaveStartedEventArgs e)
        {
            TryAdvanceTutorial();
        }

        private void OnMoneyChanged(ulong currentMoney)
        {
            TryAdvanceTutorial();
            _lastKnownMoney = currentMoney;
        }

        private void OnAnyTurretEquipped(int slotIndex, TurretStatsInstance stats)
        {
            if (_currentStep < tutorialSteps.Count &&
                tutorialSteps[_currentStep].completeCondition == TutorialConditionType.TurretEquipped)
            {
                TryAdvanceTutorial(); 
            }
        }
        private void OnAnyTurretUpgraded()
        {
            _turretWasUpgraded = true;
            TryAdvanceTutorial();
        }



        private void TryAdvanceTutorial()
        {
            if (!_tutorialRunning || _currentStep >= tutorialSteps.Count)
                return;

            if (_currentStep == 1)
            {
                // Just completed step 0 resume game
                Time.timeScale = 1f;
            }

            var step = tutorialSteps[_currentStep];

            if (_waitingToStartStep)
            {
                if (CheckCondition(step.startCondition, step.startThreshold))
                {
                    ShowStep(_currentStep);
                    _waitingToStartStep = false;
                }
            }
            else
            {
                if (CheckCondition(step.completeCondition, step.completeThreshold))
                {
                    _currentStep++;

                    if (_currentStep >= tutorialSteps.Count)
                    {
                        CompleteTutorial();
                        return;
                    }

                    var nextStep = tutorialSteps[_currentStep];
                    if (CheckCondition(nextStep.startCondition, nextStep.startThreshold))
                    {
                        ShowStep(_currentStep);
                        _waitingToStartStep = false;
                    }
                    else
                    {
                        tutorialPanel.SetActive(false); // Hide while waiting
                        foreach (var obj in _lastActiveObjects)
                        {
                            if (obj != null)
                                obj.SetActive(false);
                        }
                        _lastActiveObjects.Clear();

                        _waitingToStartStep = true;
                    }

                }
            }
            _turretWasUpgraded = false;

        }

        private bool CheckCondition(TutorialConditionType condition, float threshold)
        {
            switch (condition)
            {
                case TutorialConditionType.SpdBonusAbove:
                    return GameManager.Instance.spdBonus >= threshold;
                case TutorialConditionType.HealthBelow:
                    return PlayerBaseManager.Instance.CurrentHealth < threshold;
                case TutorialConditionType.HealthAbove:
                    return PlayerBaseManager.Instance.CurrentHealth > threshold;
                case TutorialConditionType.MaxHealthAbove:
                    return PlayerBaseManager.Instance.MaxHealth > threshold;
                case TutorialConditionType.WaveReached:
                    return WaveManager.Instance.GetCurrentWaveIndex() >= (int)threshold;
                case TutorialConditionType.MoneyChanged:
                    return true;
                case TutorialConditionType.MoneyAbove:
                    return GameManager.Instance.Money > threshold;
                case TutorialConditionType.MoneyBelow:
                    return GameManager.Instance.Money < threshold;
                case TutorialConditionType.MoneyIncreased:
                    return GameManager.Instance.Money > _lastKnownMoney;
                case TutorialConditionType.MoneyDecreased:
                    return GameManager.Instance.Money < _lastKnownMoney;
                case TutorialConditionType.TurretEquipped:
                    return TurretSlotManager.I.IsAnyTurretEquipped();
                case TutorialConditionType.SlotUnlocked:
                    return TurretSlotManager.I.UnlockedSlotCount() >= (int)threshold;
                case TutorialConditionType.TurretUpgraded:
                    return _turretWasUpgraded;

                default:
                    return false;
            }

        }

        private void ShowStep(int index)
        {
            if (!tutorialPanel.activeInHierarchy)
                tutorialPanel.SetActive(true);

            foreach (var obj in _lastActiveObjects)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
            _lastActiveObjects.Clear();

            var step = tutorialSteps[index];
            if (step.associatedObjects != null && step.associatedObjects.Length > 0)
            {
                foreach (var obj in step.associatedObjects)
                {
                    if (obj != null)
                    {
                        obj.SetActive(true);
                        _lastActiveObjects.Add(obj);
                    }
                }
            }

            typingText.StartTyping(step.instructionText);
        }

        private void CompleteTutorial()
        {
            _tutorialRunning = false;
            tutorialPanel.SetActive(false);
            UnsubscribeFromEvents();
            foreach (var obj in _lastActiveObjects)
            {
                if (obj != null)
                    obj.SetActive(false);
            }

            _currentStep = tutorialSteps.Count;
            SaveGameManager.Instance.SaveGame(); 

            Debug.Log("Tutorial completed!");
        }

        [ContextMenu("Reset and Start Tutorial")]
        public void RestartTutorialForDebug()
        {
            _currentStep = 0;
            _tutorialRunning = true;
            _waitingToStartStep = true;
            SubscribeToEvents();
        }

        public void LoadGame(int step)
        {
            _currentStep = step;
        }
    }

    [Serializable]
    public class TutorialStep
    {
        [TextArea(3, 10)]
        public string instructionText;

        [Header("Conditions")]
        public TutorialConditionType startCondition;
        public float startThreshold;

        public TutorialConditionType completeCondition;
        public float completeThreshold;

        [Tooltip("Optional object to activate while this step is active")]
        public GameObject[] associatedObjects;
    }

    public enum TutorialConditionType
    {
        SpdBonusAbove,
        HealthBelow,
        MaxHealthAbove,
        HealthAbove,
        WaveReached,
        MoneyChanged,
        MoneyAbove,
        MoneyBelow,
        MoneyIncreased,
        MoneyDecreased,
        TurretEquipped,
        SlotUnlocked,
        TurretUpgraded
    }
}
