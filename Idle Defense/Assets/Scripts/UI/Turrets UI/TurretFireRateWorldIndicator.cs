using Assets.Scripts.Turrets;
using Assets.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    [RequireComponent(typeof(Image))]
    public class TurretFireRateWorldIndicator : MonoBehaviour
    {
        [SerializeField] private SlotWorldButton slot;   // optional manual assign
        [SerializeField] private bool hideWhenReady = false;
        [SerializeField] private bool invert = false;

        private Image _img;

        private void Awake()
        {
            _img = GetComponent<Image>();
        }

        private void OnEnable()
        {
            if (slot == null)
                slot = GetComponentInParent<SlotWorldButton>();
        }

        private void LateUpdate()
        {
            if (slot == null)
            {
                gameObject.SetActive(false);
                return;
            }

            BaseTurret turret = slot.CurrentTurret;

            // Disable indicator if no turret equipped
            if (turret == null)
            {
                _img.fillAmount = 0f;
                return;
            }

            float t = turret.FireCooldown01; // 0..1
            if (invert) t = 1f - t;

            _img.fillAmount = t;

            if (hideWhenReady)
                _img.enabled = t < 0.999f;
            else if (!_img.enabled)
                _img.enabled = true;
        }
    }
}
