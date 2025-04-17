using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
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
            ShowStep(_currentStep);

        }


        private void Update()
        {
            if (!_tutorialRunning || _currentStep >= tutorialSteps.Count) return;

            if (CheckCondition(tutorialSteps[_currentStep]))
            {
                _currentStep++;

                if (_currentStep >= tutorialSteps.Count)
                {
                    CompleteTutorial();
                }
                else
                {
                    ShowStep(_currentStep);

                }
            }
        }

        private bool CheckCondition(TutorialStep step)
        {
            switch (step.conditionType)
            {
                case TutorialConditionType.SpdBonusAbove:
                    return GameManager.Instance.spdBonus >= step.threshold;

                case TutorialConditionType.HealthBelow:
                    return PlayerBaseManager.Instance.CurrentHealth < step.threshold;

                case TutorialConditionType.WaveReached:
                    return WaveManager.Instance.GetCurrentWaveIndex() >= (int)step.threshold;

                default:
                    return false;
            }
        }

        

        private void ShowStep(int index)
        {
            // Deactivate previous step's object
            if (_lastActiveObject != null)
                _lastActiveObject.SetActive(false);

            // Activate new step object if it exists
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
            if (_lastActiveObject != null)
                _lastActiveObject.SetActive(false);

            Debug.Log("Tutorial completed!");
        }

        [ContextMenu("Reset and Start Tutorial")]
        public void RestartTutorialForDebug()
        {
            PlayerPrefs.DeleteKey("Tutorial_Completed");
            _currentStep = 0;
            _tutorialRunning = true;
            tutorialPanel.SetActive(true);
            typingText.StartTyping(tutorialSteps[_currentStep].instructionText);
        }

    }


    [Serializable]
    public class TutorialStep
    {
        public string instructionText;
        public TutorialConditionType conditionType;
        public float threshold = 0f;

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