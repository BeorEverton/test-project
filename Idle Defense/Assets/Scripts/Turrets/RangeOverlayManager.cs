using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using UnityEngine;

namespace Assets.Scripts.VFX
{
    /// <summary>
    /// Repositions ONE pre-sized, pre-rotated overlay image/mesh so that its FAR edge
    /// matches the turret's attack limit along +Z. No scaling or rotation performed.
    /// Matches BaseTurret's absolute-depth check (enemy.z <= Range).
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class RangeOverlayManager : MonoBehaviour
    {
        public static RangeOverlayManager Instance { get; private set; }

        [Header("Single overlay renderer (quad or sprite)")]
        [SerializeField] private Renderer overlayRenderer;

        [Header("Positioning")]
        [Tooltip("Meters from overlay pivot to its FAR (+Z) edge. If <= 0, auto-estimate from bounds.")]
        [SerializeField] private float pivotToFarEdge = -1f;

        [Tooltip("Extra push along +Z applied to the FAR edge. Positive moves the red edge deeper, negative pulls it toward the turret.")]
        [SerializeField] private float edgeBias = 0f;

        [Tooltip("Keep overlay X equal to turret X.")]
        [SerializeField] private bool alignXWithTurret = true;

        [Tooltip("Lock Y to turret Y plus yOffset.")]
        [SerializeField] private bool lockYToTurret = true;

        [Tooltip("Small Y offset to avoid z-fighting when Y is locked.")]
        [SerializeField] private float yOffset = 0.02f;

        private BaseTurret _currentTurret;
        private float _lastRange = float.NegativeInfinity;
        private Vector3 _lastTurretPos;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (overlayRenderer != null) overlayRenderer.enabled = false;
        }

        private void OnEnable()
        {
            TurretUpgradeManager.OnAnyTurretUpgraded += HandleAnyTurretUpgraded;
        }

        private void OnDisable()
        {
            TurretUpgradeManager.OnAnyTurretUpgraded -= HandleAnyTurretUpgraded;
        }

        private void HandleAnyTurretUpgraded()
        {
            if (_currentTurret != null && overlayRenderer != null && overlayRenderer.enabled)
                RefreshOverlay();
        }

        public void ShowFor(BaseTurret turret)
        {
            if (turret == null || overlayRenderer == null) return;
            _currentTurret = turret;
            overlayRenderer.enabled = true;
            _lastRange = float.NegativeInfinity; // force refresh
            RefreshOverlay();
        }

        public void Hide()
        {
            if (overlayRenderer != null) overlayRenderer.enabled = false;
            _currentTurret = null;
        }

        public void SetPivotToFarEdge(float meters)
        {
            pivotToFarEdge = Mathf.Max(0f, meters);
            if (_currentTurret != null && overlayRenderer != null && overlayRenderer.enabled)
                RefreshOverlay();
        }

        public void SetEdgeBias(float meters)
        {
            edgeBias = meters;
            if (_currentTurret != null && overlayRenderer != null && overlayRenderer.enabled)
                RefreshOverlay();
        }

        private void LateUpdate()
        {
            if (_currentTurret == null || overlayRenderer == null || !overlayRenderer.enabled) return;

            float curRange = Mathf.Max(0f, _currentTurret.RuntimeStats.Range);
            Vector3 curPos = _currentTurret.transform.position;

            if (!Mathf.Approximately(curRange, _lastRange) ||
                (curPos - _lastTurretPos).sqrMagnitude > 0.0001f)
            {
                RefreshOverlay();
            }
        }

        private void RefreshOverlay()
        {
            if (_currentTurret == null || overlayRenderer == null) return;

            // 1) Distance from pivot to FAR (+Z) edge of the overlay
            float pivotToEdge = pivotToFarEdge;
            if (pivotToEdge <= 0f)
            {
                // Estimate with world AABB extents projected onto +Z
                var e = overlayRenderer.bounds.extents;
                pivotToEdge = e.z; // assuming the mesh already lies along +Z
            }

            // 2) BaseTurret uses ABSOLUTE world depth: enemy.z <= Range (not turret.z + Range)
            float rangeZ = Mathf.Max(0f, _currentTurret.RuntimeStats.Range);

            // Optional fine-tune
            rangeZ += edgeBias;

            // 3) Position so that: overlayPos.z + pivotToEdge == rangeZ
            Transform ov = overlayRenderer.transform;
            Vector3 pos = ov.position;
            pos.z = rangeZ - pivotToEdge;

            if (alignXWithTurret) pos.x = _currentTurret.transform.position.x;
            if (lockYToTurret) pos.y = _currentTurret.transform.position.y + yOffset;

            ov.position = pos;

            // cache
            _lastRange = rangeZ;
            _lastTurretPos = _currentTurret.transform.position;
        }
    }
}
