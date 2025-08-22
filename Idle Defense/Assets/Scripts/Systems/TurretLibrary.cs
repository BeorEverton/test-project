// Assets/Scripts/Systems/TurretLibrary.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.Scripts.SO;
using Assets.Scripts.Turrets;

namespace Assets.Scripts.Systems
{
    /// <summary>
    /// Auto-builds dictionaries from Resources:
    ///  - Scriptable Objects: Resources/Scriptable Objects/Turrets
    ///  - Prefabs:           Resources/Turrets
    /// </summary>
    public class TurretLibrary : MonoBehaviour
    {
        [Header("Resources paths (change if you use a different structure)")]
        [SerializeField] private string infoFolder = "Scriptable Objects/Turrets";
        [SerializeField] private string prefabFolder = "Turrets from Interfaces";

        private static TurretLibrary _instance;
        public static TurretLibrary Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 1) Prefer an existing scene instance (with your inspector paths)
                    _instance = FindFirstObjectByType<TurretLibrary>();
                    if (_instance == null)
                    {
                        // 2) Create one only if none exists
                        var go = new GameObject(nameof(TurretLibrary));
                        _instance = go.AddComponent<TurretLibrary>();
                        DontDestroyOnLoad(go);
                    }
                    // Build once here (uses whatever paths are on the instance)
                    _instance.BuildIndex();
                }
                return _instance;
            }
        }

        // Make sure the scene instance takes ownership and builds if needed
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // If nothing was built yet, build now (uses the serialized paths shown in inspector)
            if (_infos.Count == 0 || _prefabs.Count == 0)
                BuildIndex();
        }


        private readonly Dictionary<TurretType, TurretInfoSO> _infos = new();
        private readonly Dictionary<TurretType, GameObject> _prefabs = new();

        public TurretInfoSO GetInfo(TurretType type) =>
            _infos.TryGetValue(type, out var so) ? so : null;

        public GameObject GetPrefab(TurretType type) =>
            _prefabs.TryGetValue(type, out var p) ? p : null;

        /// <summary>
        /// Loads all SOs and prefabs from Resources and validates coverage vs enum.
        /// Call at boot or lazy via Instance getter.
        /// </summary>
        public void BuildIndex()
        {
            _infos.Clear();
            _prefabs.Clear();

            // 1) Load all TurretInfoSO
            var infos = Resources.LoadAll<TurretInfoSO>(infoFolder);
            if (infos == null || infos.Length == 0)
                Debug.LogError($"[TurretLibrary] No TurretInfoSO found at Resources/{infoFolder}. Check path and Resources folder.");

            foreach (var so in infos)
            {
                var t = so.TurretType;
                if (_infos.ContainsKey(t))
                {
                    Debug.LogError($"[TurretLibrary] Duplicate TurretInfoSO for {t}: {_infos[t].name} and {so.name}");
                    continue;
                }
                _infos.Add(t, so);
            }

            // 2) Load all prefabs (relative to Resources root)
            var prefabs = Resources.LoadAll<GameObject>(prefabFolder);
            if (prefabs == null || prefabs.Length == 0)
                Debug.LogError($"[TurretLibrary] No prefabs found at Resources/{prefabFolder}. Check that your prefabs are under Assets/Resources/{prefabFolder}");

            foreach (var go in prefabs)
            {
                // Accept BaseTurret on root OR in children
                var baseTurret = go.GetComponent<BaseTurret>() ?? go.GetComponentInChildren<BaseTurret>(true);
                if (baseTurret == null) continue;

                // Prefer explicit type from assigned stats
                TurretType? typeFromComponent = null;
                if (baseTurret._turretInfo != null)
                    typeFromComponent = baseTurret._turretInfo.TurretType;

                TurretType resolvedType;
                if (typeFromComponent.HasValue)
                {
                    resolvedType = typeFromComponent.Value;
                }
                else
                {
                    // Fallback: name-match with normalization (strip spaces/punct + "Turret" suffix)
                    string pName = Normalize(go.name);
                    var match = _infos.Keys
                        .Select(t => new { t, key = Normalize(t.ToString()) })
                        .FirstOrDefault(m => pName.Contains(m.key) || m.key.Contains(pName));

                    if (match == null)
                    {
                        Debug.LogWarning($"[TurretLibrary] Could not infer TurretType for prefab '{go.name}'. " +
                                         $"Assign Permanent/Runtime stats on the prefab or ensure its name contains an enum value (suffix 'Turret' is ignored).");
                        continue;
                    }
                    resolvedType = match.t;
                }

                if (_prefabs.ContainsKey(resolvedType))
                {
                    Debug.LogError($"[TurretLibrary] Duplicate prefab for {resolvedType}: {_prefabs[resolvedType].name} and {go.name}");
                    continue;
                }
                _prefabs.Add(resolvedType, go);
            }

            // 3) Validate coverage
            var allTypes = (TurretType[])Enum.GetValues(typeof(TurretType));
            foreach (var t in allTypes)
            {
                if (!_infos.ContainsKey(t))
                    Debug.LogError($"[TurretLibrary] Missing TurretInfoSO for {t} in Resources/{infoFolder}");
                if (!_prefabs.ContainsKey(t))
                    Debug.LogError($"[TurretLibrary] Missing Prefab for {t} in Resources/{prefabFolder}");
            }

            
        }

        private static string Normalize(string s)
        {
            // lower, alphanumeric only, strip "turret" suffix
            var chars = s.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray();
            var cleaned = new string(chars);
            if (cleaned.EndsWith("turret"))
                cleaned = cleaned.Substring(0, cleaned.Length - "turret".Length);
            return cleaned;
        }

    }
}
