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
        public static GameManager Instance { get; private set; }
        public PlayerInput Input { get; private set; }

        public float dmgBonus { get; private set; }
        public float spdBonus { get; private set; }

        private const float maxSpdBonus = 200f;
        private const float holdIncreaseRate = 5f;
        private const float initialBoost = 5f;

        private bool isHolding = false;
        private float decreaseDelay = 1f;
        private float decreaseTimer = 0f;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            Input = new PlayerInput();
            Input.Player.Click.performed += OnClickStarted;
            Input.Player.Click.canceled += OnClickReleased;
            Input.Player.Enable();
        }

        private void Start()
        {
            EnemySpawner.Instance.OnWaveCompleted += OnWaveCompleted;
        }

        private void OnDestroy()
        {
            Input.Player.Click.performed -= OnClickStarted;
            Input.Player.Click.canceled -= OnClickReleased;
            Input.Player.Disable();
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
