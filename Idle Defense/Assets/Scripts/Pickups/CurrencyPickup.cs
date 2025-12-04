using System;
using DG.Tweening;
using UnityEngine;

public class CurrencyPickup : MonoBehaviour
{
    [NonSerialized] public CurrencyPickupManager Manager;
    [NonSerialized] public Currency CurrencyType;
    [NonSerialized] public ulong Amount;

    [NonSerialized] public float Age;
    [NonSerialized] public bool Collected;
    [NonSerialized] public Vector2Int GridPos; // which pickup-grid cell we occupy

    private SpriteRenderer _renderer;
    private Tween _activeTween;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(
        CurrencyPickupManager manager,
        Currency currency,
        ulong amount,
        Vector3 worldPos,
        Sprite sprite,
        Vector2Int gridPos)
    {
        Manager = manager;
        CurrencyType = currency;
        Amount = amount;
        Age = 0f;
        Collected = false;
        GridPos = gridPos;

        transform.position = worldPos;
        gameObject.SetActive(true);

        if (_renderer != null)
        {
            _renderer.sprite = sprite;
            Color c = _renderer.color;
            c.a = 1f;
            _renderer.color = c;
        }

        _activeTween?.Kill();
        _activeTween = null;
    }

    public void TickLifetime(float dt, float maxLifetime, float fadeOutDuration)
    {
        if (Collected)
            return;

        Age += dt;
        if (Age >= maxLifetime)
        {
            Collected = true;

            // Let the manager decide whether to award currency and how to fade.
            if (Manager != null)
                Manager.OnPickupExpired(this, fadeOutDuration);
            else
                PlayFadeOutOnly(fadeOutDuration);
        }

    }

    public void CollectViaManager(Vector3 targetWorldPos, float travelDuration, float scaleUp, float fadeOutDuration)
    {
        if (Collected)
            return;

        Collected = true;

        // Manager will both award currency (if enabled) and play the travel animation.
        if (Manager != null)
            Manager.OnPickupCollected(this, targetWorldPos, travelDuration, scaleUp, fadeOutDuration);
        else
            PlayPickupPop(.5f, travelDuration, scaleUp, fadeOutDuration);

    }

    public void PlayFadeOutOnly(float fadeOutDuration)
    {
        if (_renderer != null)
        {
            _activeTween?.Kill();
            _activeTween = _renderer.DOFade(0f, fadeOutDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => Manager.RecyclePickup(this));
        }
        else
        {
            Manager.RecyclePickup(this);
        }
    }

    public void PlayPickupPop(float riseDistance, float duration, float scaleUp, float fadeDuration)
    {
        _activeTween?.Kill();

        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + new Vector3(0f, riseDistance, 0f);

        Sequence seq = DOTween.Sequence();

        // Pop up + slight scale
        seq.Join(transform.DOMove(endPos, duration).SetEase(Ease.OutQuad));
        seq.Join(transform.DOScale(scaleUp, duration * 0.4f).SetEase(Ease.OutBack));

        // Shrink + fade
        if (_renderer != null)
        {
            seq.Append(transform.DOScale(0.1f, fadeDuration).SetEase(Ease.InQuad));
            seq.Join(_renderer.DOFade(0f, fadeDuration).SetEase(Ease.InQuad));
        }

        seq.OnComplete(() => Manager.RecyclePickup(this));
    }


    public void ResetForPool()
    {
        _activeTween?.Kill();
        _activeTween = null;

        if (_renderer != null)
        {
            Color c = _renderer.color;
            c.a = 0f;
            _renderer.color = c;
        }

        gameObject.SetActive(false);
    }
}
