using UnityEngine;
using System.Collections;

public class MissileController : MonoBehaviour
{
    [SerializeField] private Transform _startPoint;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    private Vector3 _targetPosition;
    private float _travelDuration;
    private float _reloadDuration;

    private bool _inFlight;

    private void Awake()
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_startPoint == null)
            _startPoint = transform;
    }

    /// <summary>
    /// Launches the missile to a fixed target over 50% of the fire rate.
    /// </summary>
    public void Launch(Vector3 targetPosition, float fullFireRate)
    {
        if (_inFlight) return;

        _inFlight = true;

        transform.position = _startPoint.position;
        _targetPosition = targetPosition;
        _travelDuration = fullFireRate / 2f;
        _reloadDuration = fullFireRate / 2f;

        Vector3 direction = (_targetPosition - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);

        _spriteRenderer.color = new Color(1f, 1f, 1f, 1f); // Full visible before launch
        _spriteRenderer.enabled = true;

        StartCoroutine(MoveToTarget());
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
            transform.position = Vector3.Lerp(start, _targetPosition, progress);
            yield return null;
        }

        _spriteRenderer.enabled = false;

        // Start fading in while "reloading"
        yield return StartCoroutine(FadeInAndReset());
    }

    private IEnumerator FadeInAndReset()
    {
        float t = 0f;
        Color invisible = new Color(1f, 1f, 1f, 0f);
        Color visible = new Color(1f, 1f, 1f, 1f);

        transform.position = _startPoint.position;
        _spriteRenderer.color = invisible;
        _spriteRenderer.enabled = true;

        while (t < _reloadDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / _reloadDuration);
            _spriteRenderer.color = Color.Lerp(invisible, visible, progress);
            yield return null;
        }

        _inFlight = false;
    }
}
