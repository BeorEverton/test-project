using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    public class EnemyDeathEffect : MonoBehaviour
    {
        [Header("Effect Settings")]
        [SerializeField] private float _scaleUpAmount = 1.1f;
        [SerializeField] private float _scaleDownDuration = 0.2f;
        [SerializeField] private float _fadeDuration = 0.2f;
        [SerializeField] private SpriteRenderer _burstSprite; // A red star/splash sprite
        [SerializeField] private float _burstDuration = 0.08f;

        private SpriteRenderer[] _spriteRenderers;
        private Vector3 _originalScale;

        private void Awake()
        {
            _spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
            _originalScale = transform.localScale;

            if (_burstSprite != null)
                _burstSprite.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (_spriteRenderers.Length >= 1)
            {
                for (int i = 0; i < _spriteRenderers.Length; i++)
                {
                    Color c = _spriteRenderers[i].color;
                    c.a = 1f;
                    _spriteRenderers[i].color = c; // ? You forgot this line
                }

                transform.localScale = _originalScale;
            }
            float angle = Random.Range(0f, 180f);
            _burstSprite.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }


        public IEnumerator PlayEffectRoutine()
        {
            // Show burst sprite
            if (_burstSprite != null)
            {
                _burstSprite.gameObject.SetActive(true);
                _burstSprite.color = Color.red;
                yield return new WaitForSeconds(_burstDuration);
                _burstSprite.gameObject.SetActive(false);
            }

            // Scale up
            transform.localScale = _originalScale * _scaleUpAmount;

            // Fade and scale down
            float t = 0f;
            while (t < _scaleDownDuration)
            {
                t += Time.deltaTime;
                float progress = t / _scaleDownDuration;
                transform.localScale = Vector3.Lerp(_originalScale * _scaleUpAmount, Vector3.zero, progress);

                foreach (var sr in _spriteRenderers)
                {
                    Color c = sr.color;
                    c.a = Mathf.Lerp(1f, 0f, progress);
                    sr.color = c;
                }

                yield return null;
            }
        }
    }
}