using Assets.Scripts.UI;
using Assets.Scripts.WaveSystem;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Systems
{
    public class GameManager : MonoBehaviour
    {
        public float dmgBonus { get; private set; }
        public float spdBonus { get; private set; }

        private const float maxSpdBonus = 200f;
        private const float holdIncreaseRate = 50f;
        private const float initialBoost = 5f;

        private bool isHolding = false;
        private float decreaseDelay = 1f;
        private float decreaseTimer = 0f;

        [SerializeField] private PlayerInput _playerInput;
        private InputAction _clickAction;

        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
                        
            _clickAction = _playerInput.actions["Click"];

            _clickAction.performed += OnClickStarted;
            _clickAction.canceled += OnClickReleased;
        }

        private void Start()
        {
            EnemySpawner.Instance.OnWaveCompleted += OnWaveCompleted;
        }

        private void OnDestroy()
        {
            _clickAction.performed -= OnClickStarted;
            _clickAction.canceled -= OnClickReleased;
        }

        private void OnClickStarted(InputAction.CallbackContext ctx)
        {
            spdBonus += initialBoost;
            isHolding = true;
            decreaseTimer = 0f;
        }

        private void OnClickReleased(InputAction.CallbackContext ctx)
        {
            isHolding = false;
            decreaseTimer = 0f;
        }


        private void Update()
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            if (isHolding)
            {
                spdBonus += holdIncreaseRate * Time.deltaTime;
                decreaseTimer = 0f;
            }
            else
            {
                if (decreaseTimer >= decreaseDelay)
                {
                    spdBonus -= holdIncreaseRate * 0.5f * Time.deltaTime;
                }
                else
                {
                    decreaseTimer += Time.deltaTime;
                }
            }

            spdBonus = Mathf.Clamp(spdBonus, 0f, maxSpdBonus);
            UIManager.Instance.UpdateSpdBonus(spdBonus);

        }


        private void OnWaveCompleted(object sender, EventArgs e)
        {
            dmgBonus++;
            UIManager.Instance.UpdateDmgBonus(dmgBonus);
        }

        private void OnWaveFailed(object sender, EventArgs e)
        {
            dmgBonus = 0;
        }

        
    }
}
