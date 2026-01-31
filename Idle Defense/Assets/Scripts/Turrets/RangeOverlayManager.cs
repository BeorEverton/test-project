using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using DG.Tweening;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Assets.Scripts.VFX
{
    /// <summary>
    /// Spawns (optionally in editor) and controls a dashed "range line" made of many small quads.
    /// The line is positioned so its Z sits at the turret's attack range using the same logic as before:
    /// BaseTurret uses absolute depth: enemy.z <= Range (not turret.z + Range).
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class RangeOverlayManager : MonoBehaviour
    {
        public static RangeOverlayManager Instance { get; private set; }

        [Header("Dash prefab (a simple Quad is best)")]
        [Tooltip("Prefab must have a MeshRenderer. Quad recommended. Use one shared material for all dashes.")]
        [SerializeField] private GameObject dashPrefab;

        [Header("Build / Bake")]
        [Tooltip("If true, rebuild dashes on Start(). If false, you can bake in editor and reuse.")]
        [SerializeField] private bool rebuildOnStart = false;

        [Tooltip("If true, existing children will be cleared when rebuilding.")]
        [SerializeField] private bool clearChildrenOnRebuild = true;

        [Header("Line shape")]
        [Tooltip("Total X length in world units. Max 55.")]
        [Range(0.1f, 55f)]
        [SerializeField] private float totalLengthX = 12f;

        [Tooltip("Dash length in world units (each plane's X scale).")]
        [Min(0.01f)]
        [SerializeField] private float dashLength = 1.4f;

        [Tooltip("Gap length in world units between dashes.")]
        [Min(0f)]
        [SerializeField] private float gapLength = 0.6f;

        [Tooltip("Dash thickness in world units (plane's Z scale if using Quad on XZ).")]
        [Min(0.001f)]
        [SerializeField] private float dashThickness = 0.35f;

        [Tooltip("Y offset to avoid z-fighting with ground.")]
        [SerializeField] private float yOffset = 0.02f;

        [Header("Positioning")]
        [Tooltip("Keep overlay X equal to turret X.")]
        [SerializeField] private bool alignXWithTurret = true;

        [Tooltip("Lock Y to turret Y + yOffset.")]
        [SerializeField] private bool lockYToTurret = true;

        [Tooltip("Extra push along +Z applied to the line position (positive pushes deeper).")]
        [SerializeField] private float edgeBias = 0f;

        [Header("Animation (DOTween)")]
        [Tooltip("Enable sideways scrolling animation.")]
        [SerializeField] private bool animate = true;

        [Tooltip("Sideways scroll speed in world-units per second.")]
        [Min(0f)]
        [SerializeField] private float scrollSpeed = 1.5f;

        [Tooltip("Scroll direction. (1,0,0)=to +X, (-1,0,0)=to -X")]
        [SerializeField] private Vector3 scrollDirection = Vector3.right;

        // runtime
        private BaseTurret _currentTurret;
        private float _lastRangeZ = float.NegativeInfinity;
        private Vector3 _lastTurretPos;

        // dashes
        private Transform[] _dashes;
        private Vector3[] _baseLocalPos;
        private int _dashCount;

        // scrolling (single tween)        
        private float _phase;
        private Tween _scrollTween;
        private Vector3 _scrollDirCached;


        private float PatternLength => dashLength + gapLength;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // Start hidden (disable whole GO to avoid per-renderer toggles)
            gameObject.SetActive(false);
        }

        private void Start()
        {
            if (rebuildOnStart)
                RebuildDashes();
            else
                CacheExistingChildren();

            // ensure hidden until ShowFor is called
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            TurretUpgradeManager.OnAnyTurretUpgraded += HandleAnyTurretUpgraded;

            if (animate)
                StartScrollTween();
        }

        private void OnDisable()
        {
            TurretUpgradeManager.OnAnyTurretUpgraded -= HandleAnyTurretUpgraded;
            StopScrollTween();
        }

        private void OnDestroy()
        {
            StopScrollTween();
        }

        private void HandleAnyTurretUpgraded()
        {
            if (_currentTurret != null && gameObject.activeSelf)
                RefreshOverlay();
        }

        public void ShowFor(BaseTurret turret)
        {
            if (turret == null) return;

            _currentTurret = turret;

            // Ensure we have dashes ready
            if (_dashes == null || _dashes.Length == 0)
                CacheExistingChildren();

            if (_dashes == null || _dashes.Length == 0)
            {
                // fallback: attempt rebuild if nothing exists
                RebuildDashes();
            }

            _lastRangeZ = float.NegativeInfinity; // force refresh
            gameObject.SetActive(true);
            RefreshOverlay();

            if (animate)
                StartScrollTween();
        }

        public void Hide()
        {
            _currentTurret = null;
            StopScrollTween();
            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_currentTurret == null || !gameObject.activeSelf) return;

            float curRangeZ = Mathf.Max(0f, _currentTurret.RuntimeStats.Range) + edgeBias;
            Vector3 curPos = _currentTurret.transform.position;

            if (!Mathf.Approximately(curRangeZ, _lastRangeZ) ||
                (curPos - _lastTurretPos).sqrMagnitude > 0.0001f)
            {
                RefreshOverlay();
            }
        }

        private void RefreshOverlay()
        {
            if (_currentTurret == null) return;

            float rangeZ = Mathf.Max(0f, _currentTurret.RuntimeStats.Range) + edgeBias;

            Vector3 pos = transform.position;
            pos.z = rangeZ;

            if (alignXWithTurret) pos.x = _currentTurret.transform.position.x;
            if (lockYToTurret) pos.y = _currentTurret.transform.position.y + yOffset;

            transform.position = pos;

            _lastRangeZ = rangeZ;
            _lastTurretPos = _currentTurret.transform.position;
        }

        private void CacheExistingChildren()
        {
            int childCount = transform.childCount;
            if (childCount <= 0)
            {
                _dashes = null;
                _baseLocalPos = null;
                _dashCount = 0;
                return;
            }

            _dashCount = childCount;
            _dashes = new Transform[_dashCount];
            _baseLocalPos = new Vector3[_dashCount];

            for (int i = 0; i < _dashCount; i++)
            {
                Transform t = transform.GetChild(i);
                _dashes[i] = t;
                _baseLocalPos[i] = t.localPosition;
            }
        }

        [ContextMenu("Rebuild Dashes (Editor/Runtime)")]
        public void RebuildDashes()
        {
            totalLengthX = Mathf.Clamp(totalLengthX, 0.1f, 55f);
            dashLength = Mathf.Max(0.01f, dashLength);
            gapLength = Mathf.Max(0f, gapLength);
            dashThickness = Mathf.Max(0.001f, dashThickness);

            float pattern = PatternLength;
            if (pattern <= 0.0001f) pattern = 0.01f;

            // +1 so we cover ends even after scroll wrap
            int needed = Mathf.Max(1, Mathf.CeilToInt(totalLengthX / pattern) + 2);

            if (clearChildrenOnRebuild)
                ClearChildrenImmediateOrRuntime();

            if (dashPrefab == null)
            {
                Debug.LogError("[RangeOverlayManager] dashPrefab is not assigned.");
                _dashes = null;
                _baseLocalPos = null;
                _dashCount = 0;
                return;
            }

            _dashCount = needed;
            _dashes = new Transform[_dashCount];
            _baseLocalPos = new Vector3[_dashCount];

            // local positions centered around 0
            float startX = -totalLengthX * 0.5f;

            for (int i = 0; i < _dashCount; i++)
            {
                GameObject go = Instantiate(dashPrefab, transform);
                go.name = $"Dash_{i:000}";

                Transform t = go.transform;
                t.localRotation = Quaternion.identity;

                float x = startX + i * pattern;

                // Local pos: line is along X, range depth is handled by manager's world Z.
                // Keep local z=0.
                Vector3 lp = new Vector3(x, 0f, 0f);
                t.localPosition = lp;

                // scale dash (assumes quad lies on XZ plane; if it's XY, rotate prefab accordingly)
                Vector3 ls = t.localScale;
                ls.x = dashLength;
                ls.z = dashThickness;
                t.localScale = ls;

                _dashes[i] = t;
                _baseLocalPos[i] = lp;
            }

            _phase = 0f;


#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                EditorUtility.SetDirty(gameObject);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif
        }

        private void ClearChildrenImmediateOrRuntime()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // editor-safe clear
                while (transform.childCount > 0)
                    DestroyImmediate(transform.GetChild(0).gameObject);
                return;
            }
#endif
            // runtime clear
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
        }

        private void StartScrollTween()
        {
            if (_dashes == null || _dashes.Length == 0) return;
            if (scrollSpeed <= 0f) return;

            StopScrollTween();

            float pattern = Mathf.Max(0.001f, PatternLength);
            float fullDuration = pattern / scrollSpeed;

            // Cache normalized direction once (avoid recompute each ApplyScroll call)
            _scrollDirCached = scrollDirection.sqrMagnitude > 0.0001f ? scrollDirection.normalized : Vector3.right;

            // Clamp phase into [0,1) so resume is always stable
            _phase = Mathf.Repeat(_phase, 1f);

            // Snap immediately to the current phase so re-enabling doesn't show stale positions for a frame
            ApplyScroll(_scrollDirCached, _phase * pattern);

            // If we're mid-phase, do a short tween to finish this cycle, then loop from 0..1 forever.
            float remaining01 = 1f - _phase;
            float remainingDuration = remaining01 * fullDuration;

            if (remaining01 > 0.0001f)
            {
                // Phase -> 1
                _scrollTween = DOTween.To(
                        () => _phase,
                        v =>
                        {
                            _phase = v;
                            ApplyScroll(_scrollDirCached, _phase * pattern);
                        },
                        1f,
                        remainingDuration)
                    .SetEase(Ease.Linear)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        // Restart clean loop 0..1
                        _phase = 0f;
                        StartLoopTween(pattern, fullDuration);
                    });
            }
            else
            {
                // Already at end; start looping immediately
                _phase = 0f;
                StartLoopTween(pattern, fullDuration);
            }
        }

        private void StartLoopTween(float pattern, float fullDuration)
        {
            StopScrollTween();

            _scrollTween = DOTween.To(
                    () => _phase,
                    v =>
                    {
                        _phase = v;
                        ApplyScroll(_scrollDirCached, _phase * pattern);
                    },
                    1f,
                    fullDuration)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true);
        }


        private void StopScrollTween()
        {
            if (_scrollTween != null && _scrollTween.IsActive())
                _scrollTween.Kill(false);
            _scrollTween = null;
        }

        private void ApplyScroll(Vector3 dir, float scrollWorldUnits)
        {
            if (_dashes == null) return;

            float pattern = Mathf.Max(0.001f, PatternLength);
            float startX = -totalLengthX * 0.5f;
            float endX = startX + totalLengthX;

            // Only support sideways scroll along X (as intended)
            float dx = dir.x * scrollWorldUnits;

            // tight loop, no allocations
            for (int i = 0; i < _dashCount; i++)
            {
                Vector3 p = _baseLocalPos[i];
                float x = p.x + dx;

                // Wrap x into [startX - pattern, endX + pattern]
                float range = (endX - startX) + pattern * 2f;
                float shifted = x - (startX - pattern);
                shifted -= Mathf.Floor(shifted / range) * range;
                x = (startX - pattern) + shifted;

                p.x = x;
                _dashes[i].localPosition = p;
            }
        }


        // Optional: quick setters if you want to drive these from UI later
        public void SetTotalLength(float lengthX)
        {
            totalLengthX = Mathf.Clamp(lengthX, 0.1f, 55f);
        }

        public void SetScrollSpeed(float unitsPerSecond)
        {
            scrollSpeed = Mathf.Max(0f, unitsPerSecond);
            if (gameObject.activeSelf && animate)
                StartScrollTween();
        }
    }
}
