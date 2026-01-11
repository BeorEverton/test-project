using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using Assets.Scripts.Systems.Audio;
using Assets.Scripts.Turrets;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Assets.Scripts.Turrets
{
    public class ManualTurret : BaseTurret
    {
        [Header("Manual Aim")]
        [SerializeField] private float maxAimDistance = 200f;
        [SerializeField] private float lineWidth = 0.25f; // world meters around the ray on XZ

        [Header("UI Click Ignore (optional)")]
        [SerializeField] private GraphicRaycaster uiRaycaster;

        // Reuse buffers (avoid GC)
        private static readonly List<Enemy> _enemyBuf = new List<Enemy>(256);
        private readonly List<RaycastResult> _uiHits = new List<RaycastResult>(16);
        private PointerEventData _ped;

        protected override void Start()
        {
            base.Start();
            if (EventSystem.current != null)
                _ped = new PointerEventData(EventSystem.current);
        }

        protected override void Attack()
        {
            // 1) Aim every frame at mouse (rotation speed + angle threshold respected)
            Vector3 aimPoint = GetMouseOnXZ(_rotationPoint != null ? _rotationPoint.position.y : 0f);
            AimTowardsPoint(aimPoint, _bonusSpdMultiplier);

            // 2) Only shoot on click (never auto-fire)
            if (!WasShootClickedThisFrame())
                return;

            // 3) Fire-rate gate
            if (_timeSinceLastShot < _atkSpeed)
                return;

            // 4) Require being aimed (same feel as auto turrets)
            //if (!_targetInAim)
              //  return;

            // 5) Line hit: first enemy only
            Enemy hit = FindFirstEnemyAlongLine(out Vector3 start, out Vector3 end);
            if (hit == null || !hit.IsAlive)
                return;

            // 6) Run the same shoot “presentation” (muzzle flash, audio, recoil, chatter),
            //    but apply damage to the chosen enemy (single target).
            ShootManual(hit);

            _timeSinceLastShot = 0f;
        }

        private void ShootManual(Enemy enemy)
        {
            // Keep the same presentation pipeline as BaseTurret.Shoot()
            if (GunnerManager.Instance != null)
                GunnerManager.Instance.NotifyTurretAttack(SlotIndex);

            StartCoroutine(ShowMuzzleFlash_Proxy());

            if (_shotSounds != null && _shotSounds.Length > 0 && AudioManager.Instance != null)
            {
                _currentShotSound = _shotSounds[Random.Range(0, _shotSounds.Length)];
                AudioManager.Instance.PlayWithVariation(_currentShotSound, 0.8f, 1f);
            }

            // Damage via your effect pipeline (crit, knockback, pierce, LB dmg multiplier, etc.)
            // This uses EffectiveStats, so gunner/prestige layers are included.
            if (DamageEffects != null && enemy != null)
                DamageEffects.ApplyAll(enemy, EffectiveStats);

            // Recoil & chatter (same logic as BaseTurret.Shoot)
            var recoil = GetComponentInChildren<Recoil>(true);
            recoil?.AddRecoil();

            if (GunnerManager.Instance != null &&
                GunnerChatterSystem.Instance != null &&
                Random.value < GunnerChatterSystem.Instance.PeriodicChatChance &&
                GunnerManager.Instance.GetEquippedGunnerId(SlotIndex) != null)
            {
                string gid = GunnerManager.Instance.GetEquippedGunnerId(SlotIndex);
                var so = GunnerManager.Instance.GetSO(gid);
                if (so != null)
                    GunnerChatterSystem.TryForceCombat(so);
            }
        }

        // We can’t call BaseTurret.ShowMuzzleFlash() directly (it’s private),
        // so we do a tiny proxy that reuses the same renderer setup you already use.
        private System.Collections.IEnumerator ShowMuzzleFlash_Proxy()
        {
            if (_muzzleFlashPosition == null || _muzzleFlashSprites == null || _muzzleFlashSprites.Count == 0)
                yield break;

            var sr = _muzzleFlashPosition.GetComponent<SpriteRenderer>();
            if (sr == null)
                yield break;

            sr.sprite = _muzzleFlashSprites[Random.Range(0, _muzzleFlashSprites.Count)];
            yield return new WaitForSeconds(0.03f);
            sr.sprite = null;
        }

        private bool WasShootClickedThisFrame()
        {
            // Ignore UI clicks if raycaster provided
            if (PointerOverUI())
                return false;

            // New Input System
            if (Mouse.current != null)
                return Mouse.current.leftButton.wasPressedThisFrame;

            // Fallback
            return Input.GetMouseButtonDown(0);
        }

        private bool PointerOverUI()
        {
            if (uiRaycaster == null || _ped == null || EventSystem.current == null)
                return false;

            _uiHits.Clear();
            _ped.position = (Pointer.current != null) ? Pointer.current.position.ReadValue() : Vector2.zero;
            uiRaycaster.Raycast(_ped, _uiHits);
            return _uiHits.Count > 0;
        }

        private Vector3 GetMouseOnXZ(float y)
        {
            var cam = Camera.main;
            if (cam == null) return Vector3.zero;

            Vector2 screen = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
            Ray ray = cam.ScreenPointToRay(screen);

            // XZ plane at height y
            Plane plane = new Plane(Vector3.up, new Vector3(0f, y, 0f));
            if (plane.Raycast(ray, out float t))
                return ray.GetPoint(t);

            return Vector3.zero;
        }

        private void AimTowardsPoint(Vector3 worldPoint, float bonusMultiplier)
        {
            if (_rotationPoint == null)
            {
                _targetInAim = false;
                return;
            }

            Transform basis = _rotationPoint.parent != null ? _rotationPoint.parent : _rotationPoint;

            Vector3 toWorld = worldPoint - _rotationPoint.position;
            Vector3 toLocal = basis.InverseTransformDirection(toWorld);

            // Your project uses Z as depth (yaw around Y, aim on XZ).
            toLocal.y = 0f;
            if (toLocal.sqrMagnitude < 1e-6f) { _targetInAim = false; return; }

            float yaw = Mathf.Atan2(toLocal.x, toLocal.z) * Mathf.Rad2Deg;
            Quaternion targetLocal = Quaternion.Euler(0f, yaw, 0f);

            _rotationPoint.localRotation = Quaternion.Slerp(
                _rotationPoint.localRotation,
                targetLocal,
                RuntimeStats.RotationSpeed * bonusMultiplier * Time.deltaTime
            );

            float diff = Quaternion.Angle(_rotationPoint.localRotation, targetLocal);
            _targetInAim = diff <= RuntimeStats.AngleThreshold;
        }

        private Enemy FindFirstEnemyAlongLine(out Vector3 start, out Vector3 end)
        {
            start = (_muzzleFlashPosition != null) ? _muzzleFlashPosition.position : transform.position;

            Vector3 aim = GetMouseOnXZ(start.y);
            Vector3 dir = aim - start;
            dir.y = 0f;

            if (dir.sqrMagnitude < 0.0001f)
                dir = new Vector3(0f, 0f, 1f);

            dir.Normalize();

            // "Infinite" for your playfield; just use a big max
            float distMax = Mathf.Max(50f, maxAimDistance);
            end = start + dir * distMax;

            var gm = GridManager.Instance;
            if (gm == null)
                return null;

            float cell = Mathf.Max(0.01f, gm._cellSize);

            // Start/end in "grid space": x is world.x, y is world.depth(z)
            float sx = start.x / cell;
            float sy = start.z / cell;
            float dx = dir.x;
            float dy = dir.z;

            // Which cell are we in?
            int x = Mathf.FloorToInt(sx);
            int y = Mathf.FloorToInt(sy);

            // DDA setup
            int stepX = (dx >= 0f) ? 1 : -1;
            int stepY = (dy >= 0f) ? 1 : -1;

            // Next grid boundary in each axis (in grid space)
            float nextBoundaryX = (dx >= 0f) ? (x + 1f) : x;
            float nextBoundaryY = (dy >= 0f) ? (y + 1f) : y;

            float tMaxX = (Mathf.Abs(dx) < 1e-6f) ? float.PositiveInfinity : (nextBoundaryX - sx) / dx;
            float tMaxY = (Mathf.Abs(dy) < 1e-6f) ? float.PositiveInfinity : (nextBoundaryY - sy) / dy;

            float tDeltaX = (Mathf.Abs(dx) < 1e-6f) ? float.PositiveInfinity : (stepX / dx);
            float tDeltaY = (Mathf.Abs(dy) < 1e-6f) ? float.PositiveInfinity : (stepY / dy);

            // Convert distance to "t" in world units along dir, but DDA uses dir components directly.
            // We'll stop after N cells based on distMax.
            int maxSteps = Mathf.CeilToInt(distMax / cell) + 2;

            Enemy best = null;
            float bestDist2 = float.MaxValue;

            // Small neighborhood width to allow slight aim error / sprite pivot offset
            // 0 = only the exact ray cell. 1 = also adjacent cells.
            int sideCells = Mathf.Max(0, Mathf.CeilToInt(lineWidth / cell));

            for (int step = 0; step < maxSteps; step++)
            {
                // Check current cell and a small lateral neighborhood
                // This makes close targets reliable even if pivot is offset.
                for (int ox = -sideCells; ox <= sideCells; ox++)
                {
                    for (int oy = -sideCells; oy <= sideCells; oy++)
                    {
                        Vector2Int cellPos = new Vector2Int(x + ox, y + oy);
                        var enemiesHere = gm.GetEnemiesInGrid(cellPos);
                        if (enemiesHere == null || enemiesHere.Count == 0) continue;

                        for (int i = 0; i < enemiesHere.Count; i++)
                        {
                            Enemy e = enemiesHere[i];
                            if (e == null || !e.IsAlive) continue;

                            if (e.Info != null && e.Info.IsFlying && !EffectiveStats.CanHitFlying)
                                continue;

                            // Must be in front of the muzzle along the ray direction
                            Vector3 toE = e.transform.position - start;
                            toE.y = 0f;
                            float forward = Vector3.Dot(toE, dir);
                            if (forward < 0f) continue;

                            // Prefer the closest along the ray (first enemy)
                            float d2 = toE.sqrMagnitude;
                            if (d2 < bestDist2)
                            {
                                bestDist2 = d2;
                                best = e;
                            }
                        }
                    }
                }

                // If we found an enemy in the current "front-most" visited region, return immediately:
                // because DDA visits cells in increasing distance order.
                if (best != null)
                    return best;

                // Step to next cell along the ray
                if (tMaxX < tMaxY)
                {
                    x += stepX;
                    tMaxX += tDeltaX;
                }
                else
                {
                    y += stepY;
                    tMaxY += tDeltaY;
                }
            }

            return null;
        }

    }
}
