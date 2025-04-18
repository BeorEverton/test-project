using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Assets.Scripts.WaveSystem;

namespace Assets.Scripts.Systems
{
    public class GameTutorialManager : MonoBehaviour
    {
        [Header("UI Reference")]
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private TypingText typingText;

        [Header("Steps")]
        [SerializeField] private List<TutorialStep> tutorialSteps;

        private int _currentStep = 0;
        private bool _tutorialRunning = false;
        private bool _waitingToStartStep = true;
        private GameObject _lastActiveObject;

        private void Start()
        {
            if (tutorialSteps == null || tutorialSteps.Count == 0)
                return;

            if (PlayerPrefs.GetInt("Tutorial_Completed", 0) == 1)
            {
                tutorialPanel.SetActive(false);
                return;
            }

            _tutorialRunning = true;
            tutorialPanel.SetActive(true);
            _waitingToStartStep = true;

            SubscribeToEvents();
            Debug.Log("Tutorial started!");
        }

        private void SubscribeToEvents()
        {
            GameManager.OnSpdBonusChanged += OnGameEventTriggered;
            PlayerBaseManager.Instance.OnHealthChanged += OnHealthChanged;
            WaveManager.Instance.OnWaveStarted += OnWaveStarted;
        }

        private void UnsubscribeFromEvents()
        {
            GameManager.OnSpdBonusChanged -= OnGameEventTriggered;
            PlayerBaseManager.Instance.OnHealthChanged -= OnHealthChanged;
            WaveManager.Instance.OnWaveStarted -= OnWaveStarted;
        }

        private void OnGameEventTriggered(float _)
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

        private void TryAdvanceTutorial()
        {
            if (!_tutorialRunning || _currentStep >= tutorialSteps.Count)
                return;

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
                    }
                    else
                    {
                        _waitingToStartStep = true;
                    }
                }
            }
        }

        private bool CheckCondition(TutorialConditionType condition, float threshold)
        {
            switch (condition)
            {
                case TutorialConditionType.SpdBonusAbove:
                    return GameManager.Instance.spdBonus >= threshold;
                case TutorialConditionType.HealthBelow:
                    return PlayerBaseManager.Instance.CurrentHealth < threshold;
                case TutorialConditionType.WaveReached:
                    return WaveManager.Instance.GetCurrentWaveIndex() >= (int)threshold;
                default:
                    return false;
            }
        }

        private void ShowStep(int index)
        {
            if (!tutorialPanel.activeInHierarchy) tutorialPanel.SetActive(true);
            if (_lastActiveObject != null)
                _lastActiveObject.SetActive(false);

            var step = tutorialSteps[index];
            if (step.associatedObject != null)
            {
                step.associatedObject.SetActive(true);
                _lastActiveObject = step.associatedObject;
            }
            else
            {
                _lastActiveObject = null;
            }

            typingText.StartTyping(step.instructionText);
        }

        private void CompleteTutorial()
        {
            _tutorialRunning = false;
            tutorialPanel.SetActive(false);
            UnsubscribeFromEvents();
            if (_lastActiveObject != null)
                _lastActiveObject.SetActive(false);

            PlayerPrefs.SetInt("Tutorial_Completed", 1);
            PlayerPrefs.Save();

            Debug.Log("Tutorial completed!");
        }

        [ContextMenu("Reset and Start Tutorial")]
        public void RestartTutorialForDebug()
        {
            PlayerPrefs.DeleteKey("Tutorial_Completed");
            _currentStep = 0;
            _tutorialRunning = true;
            _waitingToStartStep = true;
            SubscribeToEvents();
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
        public GameObject associatedObject;
    }

    public enum TutorialConditionType
    {
        SpdBonusAbove,
        HealthBelow,
        WaveReached,
    }
}
