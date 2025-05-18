using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Turrets
{
    public class MissileController : MonoBehaviour
    {
        public event EventHandler<MissileHitEventArgs> OnMissileHit;
        public class MissileHitEventArgs : EventArgs
        {
            public Vector3 HitPosition;
        }

        [SerializeField] private Transform _startPoint;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private SpriteRenderer _thrustSpriteRenderer;
        private List<Sprite> _thrustSprites;

        private Transform _parent;
        private Vector3 _targetPosition;
        private float _travelDuration;

        private float _thrustTimer;

        private bool _inFlight;

        private void Awake()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_startPoint == null)
                _startPoint = transform;

            _parent = transform.parent;
        }

        private void Update()
        {
            _thrustTimer += Time.deltaTime;
        }

        public void SetThrustSprites(List<Sprite> sprites) => _thrustSprites = sprites;

        /// <summary>
        /// Launches the missile to a fixed target over the duration of travelTime.
        /// </summary>
        public void Launch(Vector3 targetPosition, float travelTime)
        {
            if (_inFlight)
                return;

            _inFlight = true;
            transform.parent = null;

            transform.position = _startPoint.position;
            _targetPosition = targetPosition;
            _travelDuration = travelTime;

            Vector3 direction = (_targetPosition - transform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);

            _spriteRenderer.enabled = true;

            StartCoroutine(MoveToTarget());
            StartCoroutine(PlayMuzzleFlashWhileMissileFlying(travelTime));
        }

        private IEnumerator PlayMuzzleFlashWhileMissileFlying(float duration)
        {
            const float FlashInterval = 0.06f;
            _thrustTimer = 0f;

            while (_thrustTimer < duration)
            {
                if (_thrustSprites.Count > 0)
                {
                    Sprite randomMuzzleFlash = _thrustSprites[Random.Range(0, _thrustSprites.Count)];
                    _thrustSpriteRenderer.sprite = randomMuzzleFlash;
                }

                yield return new WaitForSeconds(FlashInterval);
            }

            // Clear the muzzle flash when missile hits
            _thrustSpriteRenderer.sprite = null;
        }

        private IEnumerator MoveToTarget()
        {
            float t = 0f;
            Vector3 start = transform.position;
            Vector3 destination = _targetPosition; // snapshot the target position at launch

            while (t < _travelDuration)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / _travelDuration);
                transform.position = Vector3.Lerp(start, destination, progress);
                yield return null;
            }

            _spriteRenderer.enabled = false;

            OnMissileHit?.Invoke(this, new MissileHitEventArgs
            {
                HitPosition = destination
            });

            ResetPosition();
        }

        private void ResetPosition()
        {
            transform.SetParent(_parent);
            transform.localRotation = Quaternion.Euler(0, 0, 0);

            transform.position = _startPoint.position;
            _spriteRenderer.enabled = true;

            _inFlight = false;
        }
    }
}