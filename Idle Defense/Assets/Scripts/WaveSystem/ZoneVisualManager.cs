using Assets.Scripts.WaveSystem;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    /// <summary>
    /// Applies ground / background / sky / particles based on the active zone.
    /// Listens to ZoneManager.OnZoneChanged.
    /// </summary>
    public class ZoneVisualManager : MonoBehaviour
    {
        [SerializeField] private ZoneManager _zoneManager;

        [Header("Sprites / Renderers")]
        [SerializeField] private MeshRenderer[] _groundRenderers;
        [SerializeField] private SpriteRenderer _backgroundRenderer;
        [SerializeField] private MeshRenderer _skyRenderer;

        [Header("Ground Overlay (optional)")]
        [SerializeField] private Renderer _groundOverlayRenderer;

        [Header("Particles")]
        [SerializeField] private Transform _particleRoot;

        private GameObject _currentZoneParticles;

        private Coroutine _groundFadeRoutine;
        public float GroundFadeDuration = 2f;

        private Coroutine _skyFadeRoutine;
        public float SkyFadeDuration = 2f;

        private void Awake()
        {
            if (_zoneManager == null)
                _zoneManager = FindFirstObjectByType<ZoneManager>();
        }

        private void OnEnable()
        {
            if (_zoneManager != null)
                _zoneManager.OnZoneChanged += HandleZoneChanged;
        }

        private void OnDisable()
        {
            if (_zoneManager != null)
                _zoneManager.OnZoneChanged -= HandleZoneChanged;
        }

        private void HandleZoneChanged(ZoneDefinitionSO zone)
        {
            if (zone == null)
                return;

            // Ground / background / sky
            if (_groundRenderers.Length > 0 && zone.GroundMat != null)
            {
                if (_groundFadeRoutine != null)
                    StopCoroutine(_groundFadeRoutine);

                _groundFadeRoutine = StartCoroutine(CrossfadeGround(zone.GroundMat, GroundFadeDuration));
            }


            if (_skyRenderer != null && zone.SkyMat != null)
            {
                if (_skyFadeRoutine != null)
                    StopCoroutine(_skyFadeRoutine);

                _skyFadeRoutine = StartCoroutine(CrossfadeSky(zone.SkyMat, SkyFadeDuration));
            }

            // Ground overlay material
            if (_groundOverlayRenderer != null && zone.GroundOverlayMaterial != null)
                _groundOverlayRenderer.material = zone.GroundOverlayMaterial;

            // Particles: destroy previous, spawn new
            if (_currentZoneParticles != null)
            {
                Destroy(_currentZoneParticles);
                _currentZoneParticles = null;
            }

            if (zone.ZoneParticlePrefab != null)
            {
                Transform parent = _particleRoot != null ? _particleRoot : transform;
                _currentZoneParticles = Instantiate(zone.ZoneParticlePrefab, parent);
                _currentZoneParticles.transform.localPosition = Vector3.zero;
            }
        }

        private IEnumerator CrossfadeGround(Material newMat, float duration)
        {
            int count = _groundRenderers.Length;

            // Original materials (we don't touch these)
            var fromMats = new Material[count];
            // Runtime lerp materials actually assigned to the renderers
            var lerpMats = new Material[count];

            // Setup: capture start mats and create runtime mats
            for (int i = 0; i < count; i++)
            {
                var r = _groundRenderers[i];

                // Use the shared material as the "from" state so we don't instantiate extra copies
                fromMats[i] = r.sharedMaterial;

                // Create a runtime material we can mutate during the fade
                var runtimeMat = new Material(fromMats[i]);
                lerpMats[i] = runtimeMat;

                // Assign once – no more .material calls inside the loop
                r.material = runtimeMat;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);

                for (int i = 0; i < count; i++)
                {
                    // Lerp all properties from old -> new into the runtime material
                    lerpMats[i].Lerp(fromMats[i], newMat, k);
                }

                yield return null;
            }

            // Finalize: use the new zone material and clean up
            for (int i = 0; i < count; i++)
            {
                _groundRenderers[i].material = newMat;
                Destroy(lerpMats[i]);
            }
        }

        private IEnumerator CrossfadeSky(Material newMat, float duration)
        {
            // Instance of the current sky material
            Material skyMat = _skyRenderer.material;

            bool hasC1Old = skyMat.HasProperty("_Color1");
            bool hasC2Old = skyMat.HasProperty("_Color2");
            bool hasC1New = newMat.HasProperty("_Color1");
            bool hasC2New = newMat.HasProperty("_Color2");

            // Fallbacks just in case properties are missing
            Color oldC1 = hasC1Old ? skyMat.GetColor("_Color1") : Color.black;
            Color oldC2 = hasC2Old ? skyMat.GetColor("_Color2") : Color.black;
            Color targetC1 = hasC1New ? newMat.GetColor("_Color1") : oldC1;
            Color targetC2 = hasC2New ? newMat.GetColor("_Color2") : oldC2;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = t / duration;

                if (hasC1Old && hasC1New)
                    skyMat.SetColor("_Color1", Color.Lerp(oldC1, targetC1, k));

                if (hasC2Old && hasC2New)
                    skyMat.SetColor("_Color2", Color.Lerp(oldC2, targetC2, k));

                yield return null;
            }

            // At the end, assign the new sky material so all other properties/textures match the zone
            _skyRenderer.material = newMat;
        }

    }
}
