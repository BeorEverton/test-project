#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Custom inspector + menu action for GameIconLibrarySO.
// Adds all keys from GameIconKeys and from project SOs (turret/gunner/limitbreak).
[CustomEditor(typeof(GameIconLibrarySO))]
public class GameIconLibrarySOEditor : Editor
{
    private GameIconLibrarySO lib;
    private Vector2 _scroll;
    private List<string> _missing = new();

    private static readonly StringComparer CI = StringComparer.OrdinalIgnoreCase;

    private void OnEnable() => lib = (GameIconLibrarySO)target;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space(8);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Icon Library Tools", EditorStyles.boldLabel);

            if (GUILayout.Button("Populate Now (GameIconKeys + Project Types)"))
            {
                PopulateAll(lib);
                EditorUtility.SetDirty(lib);
                AssetDatabase.SaveAssets();
            }

            if (GUILayout.Button("Validate (List Missing Sprites)"))
            {
                _missing = ValidateMissingSprites(lib);
            }

            if (_missing != null && _missing.Count > 0)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField($"Missing sprites: {_missing.Count}", EditorStyles.boldLabel);
                _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(200));
                foreach (var k in _missing) EditorGUILayout.LabelField("• " + k);
                EditorGUILayout.EndScrollView();
            }
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "Populate adds rows for every known key (no sprites). Validate shows which rows still need sprites.",
            MessageType.Info);
    }

    // --------- Menu entry (works with current selection or prompts) ---------
    [MenuItem("Tools/Icons/Populate Selected Icon Library")]
    private static void PopulateSelected()
    {
        var lib = Selection.activeObject as GameIconLibrarySO;
        if (!lib)
        {
            EditorUtility.DisplayDialog("Populate Icon Library",
                "Select a GameIconLibrarySO asset first.", "OK");
            return;
        }
        PopulateAll(lib);
        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();
    }

    // ====================== Core populate logic ======================
    private static void PopulateAll(GameIconLibrarySO lib)
    {
        if (lib == null) return;

        // Case-insensitive set of existing keys
        var keys = new HashSet<string>(CI);
        foreach (var e in lib.entries)
            if (!string.IsNullOrWhiteSpace(e.key))
                keys.Add(e.key);

        // 1) All constant string keys from GameIconKeys (stats, currencies, unlocks, etc.)
        foreach (var k in GetAllConstStrings(typeof(GameIconKeys)))
            EnsureKey(lib, keys, k);

        // 2) Turret types -> turret.<TurretType>
        foreach (var key in DiscoverTurretKeys())
            EnsureKey(lib, keys, key);

        // 3) Gunners -> gunner.<GunnerId>
        foreach (var key in DiscoverGunnerKeys())
            EnsureKey(lib, keys, key);

        // 4) Limit breaks -> limitbreak.<Type>
        foreach (var key in DiscoverLimitBreakKeys())
            EnsureKey(lib, keys, key);

        // Optional: keep list sorted (nice inspector UX)
        lib.entries = lib.entries
            .OrderBy(p => p.key, CI)
            .ToList();

        Debug.Log($"[Icons] Populate complete. Keys in library: {lib.entries.Count}. Now assign sprites where missing.");
    }

    private static IEnumerable<string> GetAllConstStrings(Type t)
    {
        // All public const string fields on GameIconKeys
        var fs = t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                  .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string));
        foreach (var f in fs)
        {
            var v = (string)f.GetRawConstantValue();
            if (!string.IsNullOrWhiteSpace(v)) yield return v;
        }
    }

    private static void EnsureKey(GameIconLibrarySO lib, HashSet<string> keys, string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        if (keys.Contains(key)) return;
        keys.Add(key);
        lib.entries.Add(new GameIconLibrarySO.Pair { key = key, sprite = null });
    }

    // ====================== Project discovery ======================
    private static IEnumerable<string> DiscoverTurretKeys()
    {
        // Find all TurretInfoSO assets; each exposes a TurretType enum we use in the key.
        var guids = AssetDatabase.FindAssets("t:Assets.Scripts.SO.TurretInfoSO t:TurretInfoSO");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (!so) continue;

            var tp = so.GetType().GetField("TurretType", BindingFlags.Public | BindingFlags.Instance);
            if (tp == null) continue;

            var enumVal = tp.GetValue(so);
            if (enumVal == null) continue;

            var key = GameIconKeys.TurretType((Assets.Scripts.SO.TurretType)enumVal);
            yield return key;
        }
    }

    private static IEnumerable<string> DiscoverGunnerKeys()
    {
        // Find all GunnerSO assets; each should have a GunnerId string we use in the key.
        var guids = AssetDatabase.FindAssets("t:GunnerSO");
        foreach (var guid in guids)
        {
            var so = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(guid));
            if (so == null) continue;

            var f = so.GetType().GetField("GunnerId", BindingFlags.Public | BindingFlags.Instance);
            if (f == null) continue;

            var id = f.GetValue(so) as string;
            if (string.IsNullOrWhiteSpace(id)) continue;

            yield return GameIconKeys.GunnerId(id);
        }
    }

    private static IEnumerable<string> DiscoverLimitBreakKeys()
    {
        // Find all LimitBreakSkillSO assets; each should have a Type enum we use in the key.
        var guids = AssetDatabase.FindAssets("t:LimitBreakSkillSO");
        var seen = new HashSet<string>(CI);
        foreach (var guid in guids)
        {
            var so = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(guid));
            if (so == null) continue;

            var f = so.GetType().GetField("Type", BindingFlags.Public | BindingFlags.Instance);
            if (f == null) continue;

            var enumVal = f.GetValue(so);
            if (enumVal == null) continue;

            var key = GameIconKeys.LimitBreakType((LimitBreakType)enumVal);
            if (seen.Add(key)) yield return key;
        }
    }

    // ====================== Validation ======================
    private static List<string> ValidateMissingSprites(GameIconLibrarySO lib)
    {
        var missing = new List<string>();
        foreach (var p in lib.entries)
        {
            if (string.IsNullOrWhiteSpace(p.key)) continue;
            if (p.sprite == null) missing.Add(p.key);
        }
        missing.Sort(StringComparer.OrdinalIgnoreCase);

        if (missing.Count == 0) Debug.Log("[Icons] All keys have sprites. ✔");
        else Debug.LogWarning($"[Icons] Missing sprites for {missing.Count} key(s).");
        return missing;
    }
}
#endif
