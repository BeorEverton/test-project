using UnityEngine;

namespace Assets.Scripts.Systems
{
    public class GameSpeedManager : MonoBehaviour
    {
        private PlayerInput _playerInput;

        private void Start()
        {
            _playerInput = new();
            _playerInput.GameSpeed.Enable();
            _playerInput.GameSpeed.x1.performed += ctx => SetGameSpeed(1);
            _playerInput.GameSpeed.x2.performed += ctx => SetGameSpeed(2);
            _playerInput.GameSpeed.x3.performed += ctx => SetGameSpeed(3);
            _playerInput.GameSpeed.x5.performed += ctx => SetGameSpeed(5);
        }

        private void OnDestroy()
        {
            _playerInput.GameSpeed.x1.performed -= ctx => SetGameSpeed(1);
            _playerInput.GameSpeed.x2.performed -= ctx => SetGameSpeed(2);
            _playerInput.GameSpeed.x3.performed -= ctx => SetGameSpeed(3);
            _playerInput.GameSpeed.x5.performed -= ctx => SetGameSpeed(5);
            _playerInput.GameSpeed.Disable();
        }

        public void SetGameSpeed(int speed)
        {
            Time.timeScale = speed;
        }
    }
}