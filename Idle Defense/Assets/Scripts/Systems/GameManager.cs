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

        // Haven't comented this one, because it might be helpful in the future.
        public float dmgBonus { get; private set; }
        public float spdBonus { get; private set; }

        private const float maxSpdBonus = 100f;
        private const float holdIncreaseRate = 5f;
        private const float initialBoost = 5f;

        private bool isHolding;
        private float decreaseDelay = 1f;
        private float decreaseTimer = 0f;

        public static event Action<float> OnSpdBonusChanged; // Used for the tutorial

        private ulong money;
        public ulong Money => money;

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
            // Previous system that increases damage on wave completion
            //EnemySpawner.Instance.OnWaveCompleted += OnWaveCompleted;
            //PlayerBaseManager.Instance.OnWaveFailed += OnWaveFailed;
            EnemySpawner.Instance.OnEnemyDeath += OnEnemyDeath;
        }

        private void OnDestroy()
        {
            Input.Player.Click.performed -= OnClickStarted;
            Input.Player.Click.canceled -= OnClickReleased;
            Input.Player.Disable();
        }

        private void OnClickStarted(InputAction.CallbackContext ctx)
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;
            spdBonus += initialBoost;
            isHolding = true;
            decreaseTimer = 0f;
            OnSpdBonusChanged?.Invoke(spdBonus);
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
                    spdBonus -= holdIncreaseRate * 0.8f * Time.deltaTime;
                }
                else
                {
                    decreaseTimer += Time.deltaTime;
                }
            }

            spdBonus = Mathf.Clamp(spdBonus, 0f, maxSpdBonus);
            UIManager.Instance.UpdateSpdBonus(spdBonus);

        }
        private void OnEnemyDeath(object sender, EnemySpawner.OnEnemyDeathEventArgs e)
        {
            AddMoney(e.CoinDropAmount);
        }

        /*private void OnWaveCompleted(object sender, EventArgs e)
        {
            dmgBonus++;
            UIManager.Instance.UpdateDmgBonus(dmgBonus);
        }

        private void OnWaveFailed(object sender, EventArgs e)
        {
            dmgBonus = 0;
        }*/

        public void AddMoney(ulong amount)
        {
            money += amount;
            UIManager.Instance.UpdateMoney(money);
        }

        public void SpendMoney(ulong amount)
        {
            money -= amount;
            UIManager.Instance.UpdateMoney(money);
        }

        public void LoadMoney(ulong amount)
        {
            money = amount;
            UIManager.Instance.UpdateMoney(money);
        }
    }
}