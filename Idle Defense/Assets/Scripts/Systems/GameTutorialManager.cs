using Assets.Scripts.Systems.Save;
using Assets.Scripts.Turrets;
using Assets.Scripts.WaveSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    public class GameTutorialManager : MonoBehaviour
    {
        public static GameTutorialManager Instance { get; private set; }

        [Header("UI Reference")]
        [SerializeField] private GameObject tutorialPanel, skipButton;
        [SerializeField] private TypingText typingText;

        [Header("Steps")]
        [SerializeField] private List<TutorialStep> tutorialSteps;

        public int _currentStep = 0;
        private bool _tutorialRunning = false;
        private bool _waitingToStartStep = true;
        private List<GameObject> _lastActiveObjects = new();
        private ulong _lastKnownMoney = 0;
        private bool _turretWasUpgraded = false;

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
            TurretSlotManager.Instance.OnEquippedChanged += OnAnyTurretEquipped;
            TurretSlotManager.Instance.OnSlotUnlocked += TryAdvanceTutorial;
            TurretUpgradeManager.OnAnyTurretUpgraded += OnAnyTurretUpgraded;
        }

        private void UnsubscribeFromEvents()
        {
            GameManager.OnSpdBonusChanged -= OnGameEventTriggered;
            PlayerBaseManager.Instance.OnHealthChanged -= OnHealthChanged;
            WaveManager.Instance.OnWaveStarted -= OnWaveStarted;
            GameManager.Instance.OnMoneyChanged -= OnMoneyChanged;
            PlayerBaseManager.Instance.OnMaxHealthChanged -= OnHealthChanged;
            TurretSlotManager.Instance.OnEquippedChanged -= OnAnyTurretEquipped;
            TurretSlotManager.Instance.OnSlotUnlocked -= TryAdvanceTutorial;
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

            TutorialStep step = tutorialSteps[_currentStep];

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

                    TutorialStep nextStep = tutorialSteps[_currentStep];
                    if (CheckCondition(nextStep.startCondition, nextStep.startThreshold))
                    {
                        ShowStep(_currentStep);
                        _waitingToStartStep = false;
                    }
                    else
                    {
                        if (tutorialPanel)
                            tutorialPanel.SetActive(false); // Hide while waiting
                        foreach (GameObject obj in _lastActiveObjects.Where(obj => obj != null))
                        {
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
            return condition switch
            {
                TutorialConditionType.SpdBonusAbove => GameManager.Instance.spdBonus >= threshold,
                TutorialConditionType.HealthBelow => PlayerBaseManager.Instance.CurrentHealth < threshold,
                TutorialConditionType.HealthAbove => PlayerBaseManager.Instance.CurrentHealth > threshold,
                TutorialConditionType.MaxHealthAbove => PlayerBaseManager.Instance.MaxHealth > threshold,
                TutorialConditionType.WaveReached => WaveManager.Instance.GetCurrentWaveIndex() >= (int)threshold,
                TutorialConditionType.MoneyChanged => true,
                TutorialConditionType.MoneyAbove => GameManager.Instance.Money > threshold,
                TutorialConditionType.MoneyBelow => GameManager.Instance.Money < threshold,
                TutorialConditionType.MoneyIncreased => GameManager.Instance.Money > _lastKnownMoney,
                TutorialConditionType.MoneyDecreased => GameManager.Instance.Money < _lastKnownMoney,
                TutorialConditionType.TurretEquipped => TurretSlotManager.Instance.IsAnyTurretEquipped(),
                TutorialConditionType.SlotUnlocked => TurretSlotManager.Instance.UnlockedSlotCount() >= (int)threshold,
                TutorialConditionType.TurretUpgraded => _turretWasUpgraded,
                _ => false
            };
        }

        private void ShowStep(int index)
        {
            if (tutorialPanel && !tutorialPanel.activeInHierarchy)
                tutorialPanel.SetActive(true);

            if (skipButton && !skipButton.activeInHierarchy)
                skipButton.SetActive(index != 0);

            if (_currentStep == 2)
            {
                // Just completed step 0 resume game
                Time.timeScale = 1f;
            }

            foreach (GameObject obj in _lastActiveObjects
                         .Where(obj => obj != null)
                         .Where(obj => obj))
            {
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

            if (typingText)
                typingText.StartTyping(step.instructionText);
        }

        private void CompleteTutorial()
        {
            _tutorialRunning = false;
            tutorialPanel.SetActive(false);
            UnsubscribeFromEvents();
            foreach (GameObject obj in _lastActiveObjects.Where(obj => obj != null))
                obj.SetActive(false);

            _currentStep = tutorialSteps.Count;
            SaveGameManager.Instance.SaveGame();

            Debug.Log("Tutorial completed!");
        }

        public void SkipCurrentStep()
        {
            if (!_tutorialRunning || _currentStep >= tutorialSteps.Count)
                return;

            _currentStep++;

            foreach (var obj in _lastActiveObjects)
            {
                if (obj == null)
                    continue;
                if (!obj)
                    continue; // optional, for safety

                obj.SetActive(false);
            }

            _lastActiveObjects.Clear();

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
                tutorialPanel.SetActive(false);
                _waitingToStartStep = true;
            }
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