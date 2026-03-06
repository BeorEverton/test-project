using Assets.Scripts.Enemies;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class BossHealthBarUI : MonoBehaviour
    {
        private const int SegmentCount = 10;

        [Header("Root")]
        [SerializeField] private RectTransform _root;
        [SerializeField] private RectTransform _frameToShake;

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI _bossNameText;
        [SerializeField] private TextMeshProUGUI _hpText;

        [Header("Segments (bottom = index 0, top = index 9)")]
        [SerializeField] private Image[] _segmentFills = new Image[SegmentCount];

        [Header("Tuning")]
        [SerializeField] private float _spawnFillDuration = 0.35f;
        [SerializeField] private float _damageLerpDuration = 0.18f;
        [SerializeField] private float _boundaryShakeDuration = 0.12f;
        [SerializeField] private float _boundaryShakeStrength = 8f;
        [SerializeField] private float _spawnShakeStrength = 6f;

        [Tooltip("If max HP is small, show integer counting down. If huge, use abbreviated formatting.")]
        [SerializeField] private float _integerHpDisplayMax = 50000f;

        private Enemy _boss;
        private float _maxHealth;
        private float _targetHealth;
        private float _displayHealth;

        private int _lastActiveSegment = SegmentCount - 1;

        private Tween _healthTween;
        private Sequence _spawnSeq;

        [Header("HP Display")]
        [Tooltip("When true: show current/max HP. When false: show remaining full health bars as '10x', '9x', etc.")]
        [SerializeField] private bool _showHealth = true;

        public bool ShowHealth => _showHealth;

        // Call this from a UI Toggle (or anywhere) during gameplay.
        public void SetShowHealth(bool showHealth)
        {
            _showHealth = showHealth;
            UpdateHpText(_displayHealth);
        }

        private void Awake()
        {
            if (_root == null) _root = transform as RectTransform;
        }

        public void Bind(Enemy boss)
        {
            if (boss == null) return;

            // Rebind safety
            if (_boss != null) Unbind(_boss);

            _boss = boss;
            _boss.OnMaxHealthChanged += HandleMaxHealthChanged;
            _boss.OnCurrentHealthChanged += HandleHealthChanged;

            _maxHealth = Mathf.Max(1f, _boss.MaxHealth);
            _targetHealth = Mathf.Clamp(_boss.CurrentHealth, 0f, _maxHealth);
            _displayHealth = 0f; // start empty for spawn animation

            _bossNameText?.SetText(_boss.Info != null ? _boss.Info.Name : "BOSS");

            // Ensure all segments start empty
            for (int i = 0; i < _segmentFills.Length; i++)
            {
                if (_segmentFills[i] != null) _segmentFills[i].fillAmount = 0f;
            }
            UpdateHpText(0f);

            PlaySpawnSequence();
        }

        public void Unbind(Enemy boss)
        {
            if (_boss == null) return;

            _healthTween?.Kill();
            _spawnSeq?.Kill();

            _boss.OnMaxHealthChanged -= HandleMaxHealthChanged;
            _boss.OnCurrentHealthChanged -= HandleHealthChanged;
            _boss = null;
        }

        private void HandleMaxHealthChanged(object sender, System.EventArgs e)
        {
            if (_boss == null) return;
            _maxHealth = Mathf.Max(1f, _boss.MaxHealth);
            _targetHealth = Mathf.Clamp(_boss.CurrentHealth, 0f, _maxHealth);

            // Snap displayed if we were uninitialized
            if (_displayHealth > _maxHealth) _displayHealth = _maxHealth;

            UpdateAllVisuals(_displayHealth, allowShake: false);
        }

        private void HandleHealthChanged(object sender, System.EventArgs e)
        {
            if (_boss == null) return;
            SetTargetHealth(_boss.CurrentHealth);
        }

        private void SetTargetHealth(float newHealth)
        {
            newHealth = Mathf.Clamp(newHealth, 0f, _maxHealth);
            _targetHealth = newHealth;

            _healthTween?.Kill();

            float start = _displayHealth;
            float end = _targetHealth;

            // Very small deltas should snap (avoids micro tweens)
            if (Mathf.Abs(end - start) < 0.01f)
            {
                _displayHealth = end;
                UpdateAllVisuals(_displayHealth, allowShake: false);
                return;
            }

            _healthTween = DOTween.To(
                    () => _displayHealth,
                    v =>
                    {
                        float prev = _displayHealth;
                        _displayHealth = v;
                        UpdateAllVisuals(_displayHealth, allowShake: true, previousDisplay: prev);
                    },
                    end,
                    _damageLerpDuration
                )
                .SetEase(Ease.OutQuad)
                .SetUpdate(true); // ignore timescale (your speed-up won’t spam UI)
        }

        private void PlaySpawnSequence()
        {
            _healthTween?.Kill();
            _spawnSeq?.Kill();

            if (_root != null)
            {
                _root.localScale = Vector3.one * 0.92f;
            }

            // 1) Name appears (quick punch)
            _bossNameText?.alpha.ToString(); // no-op; keeps TMP referenced
            _bossNameText?.SetAlpha(0f);

            _spawnSeq = DOTween.Sequence().SetUpdate(true);

            _spawnSeq.Append(_bossNameText.DOFade(1f, 0.12f).SetEase(Ease.OutQuad));

            // 2) Fill bars fast + frame subtle shake
            _spawnSeq.Join(DOTween.To(
                    () => _displayHealth,
                    v =>
                    {
                        _displayHealth = v;
                        UpdateAllVisuals(_displayHealth, allowShake: false);
                    },
                    _targetHealth > 0f ? _targetHealth : _maxHealth,
                    _spawnFillDuration
                ).SetEase(Ease.OutCubic));

            if (_frameToShake != null)
            {
                _spawnSeq.Join(_frameToShake.DOShakeAnchorPos(
                    _spawnFillDuration,
                    _spawnShakeStrength,
                    vibrato: 18,
                    randomness: 60f,
                    snapping: false,
                    fadeOut: true
                ));
            }

            // 3) Pop out and return
            if (_root != null)
            {
                _spawnSeq.Append(_root.DOPunchScale(Vector3.one * 0.08f, 0.18f, vibrato: 8, elasticity: 0.6f));
            }

            // After spawn, ensure we are exactly at target
            _spawnSeq.OnComplete(() =>
            {
                _displayHealth = _targetHealth > 0f ? _targetHealth : _maxHealth;
                UpdateAllVisuals(_displayHealth, allowShake: false);
            });
        }

        private void UpdateAllVisuals(float hp, bool allowShake, float previousDisplay = -1f)
        {
            hp = Mathf.Clamp(hp, 0f, _maxHealth);

            // Segment math
            float chunk = _maxHealth / SegmentCount;
            if (chunk <= 0f) chunk = 1f;

            // Detect segment boundary crossing (shake when one bar fully empties)
            int activeSeg = (hp <= 0f)
                ? -1
                : Mathf.Clamp(Mathf.FloorToInt((hp - 0.0001f) / chunk), 0, SegmentCount - 1);

            if (allowShake && previousDisplay >= 0f && _frameToShake != null)
            {
                // If we crossed from segment k to k-1, it means segment k just emptied.
                if (activeSeg < _lastActiveSegment)
                {
                    _frameToShake.DOKill();
                    _frameToShake.DOShakeAnchorPos(
                        _boundaryShakeDuration,
                        _boundaryShakeStrength,
                        vibrato: 14,
                        randomness: 70f,
                        snapping: false,
                        fadeOut: true
                    ).SetUpdate(true);
                }
            }
            _lastActiveSegment = activeSeg;

            // Fill each stacked bar: bottom fills first, then above
            for (int i = 0; i < SegmentCount && i < _segmentFills.Length; i++)
            {
                Image img = _segmentFills[i];
                if (img == null) continue;

                float segHp = Mathf.Clamp(hp - (i * chunk), 0f, chunk);
                img.fillAmount = segHp / chunk;
            }

            UpdateHpText(hp);
        }

        private void UpdateHpText(float hp)
        {
            if (_hpText == null) return;

            hp = Mathf.Clamp(hp, 0f, _maxHealth);

            // Mode B: show "bars remaining" as 10x..0x (changes only when a full segment is depleted)
            if (!_showHealth)
            {
                float chunk = _maxHealth / SegmentCount;
                if (chunk <= 0f) chunk = 1f;

                int activeSeg = (hp <= 0f)
                    ? -1
                    : Mathf.Clamp(Mathf.FloorToInt((hp - 0.0001f) / chunk), 0, SegmentCount - 1);

                int barsRemaining = Mathf.Max(activeSeg + 1, 0);
                _hpText.SetText($"{barsRemaining}x");
                return;
            }

            // Mode A: original behavior
            if (_maxHealth <= _integerHpDisplayMax)
            {
                int shown = Mathf.RoundToInt(hp);
                int maxShown = Mathf.RoundToInt(_maxHealth);
                _hpText.SetText($"{shown}/{maxShown}");
            }
            else
            {
                _hpText.SetText($"{UIManager.AbbreviateNumber(hp, false, true)}/{UIManager.AbbreviateNumber(_maxHealth, false, true)}");
            }
        }

    }

    internal static class TMPAlphaExt
    {
        public static void SetAlpha(this TextMeshProUGUI tmp, float a)
        {
            if (tmp == null) return;
            Color c = tmp.color;
            c.a = a;
            tmp.color = c;
        }
    }
}
