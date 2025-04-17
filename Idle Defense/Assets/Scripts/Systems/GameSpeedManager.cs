using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Systems
{
    public class GameSpeedManager : MonoBehaviour
    {
        [SerializeField] private List<GameSpeedButton> _gameSpeedButtons;

        private PlayerInput _playerInput;
        private int _currentButtonIndex = 0;

        private void Start()
        {
            _playerInput = new();
            _playerInput.GameSpeed.Enable();

            InitGameSpeedButtons();
        }

        private void InitGameSpeedButtons()
        {
            _gameSpeedButtons.ForEach(button =>
            {
                button.Button.gameObject.SetActive(false);
                button.Button.onClick.AddListener(ActivateNextButton);
            });

            //Activate 1x button
            _gameSpeedButtons[_currentButtonIndex].Button.gameObject.SetActive(true);
            SetGameSpeed(_gameSpeedButtons[_currentButtonIndex].GameSpeed);
        }

        private void OnDestroy()
        {
            _playerInput.GameSpeed.Disable();
        }

        public void ActivateNextButton()
        {
            _gameSpeedButtons[_currentButtonIndex].Button.gameObject.SetActive(false);
            _currentButtonIndex = (_currentButtonIndex + 1) % _gameSpeedButtons.Count;
            _gameSpeedButtons[_currentButtonIndex].Button.gameObject.SetActive(true);

            SetGameSpeed(_gameSpeedButtons[_currentButtonIndex].GameSpeed);
        }

        public void SetGameSpeed(int speed)
        {
            Time.timeScale = speed;
        }
    }

    [System.Serializable]
    public class GameSpeedButton
    {
        public Button Button;
        public int GameSpeed;
    }
}