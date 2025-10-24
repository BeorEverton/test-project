// File: Assets/Editor/GunnerPhraseImporterWindow.cs
// Purpose: Paste CSV-like text for each phrase category and import into a GunnerSO.
// Additions:
// - Delimiter Mode: Lines Only (default) or CSV
// - Collapsible sections (foldouts) per category
// - Per-section adjustable height (with EditorPrefs persistence)
// - Expand All / Collapse All controls
// - Dedup, Append/Replace, Trim, Load From SO

using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public class GunnerPhraseImporterWindow : EditorWindow
{
    // --- Persisted prefs keys ---
    private const string PREF_FOLD_IDLE = "GPIW_Fold_Idle";
    private const string PREF_FOLD_PRAISE = "GPIW_Fold_Praise";
    private const string PREF_FOLD_NEG = "GPIW_Fold_Negative";
    private const string PREF_FOLD_COMBAT = "GPIW_Fold_Combat";

    private const string PREF_H_IDLE = "GPIW_Height_Idle";
    private const string PREF_H_PRAISE = "GPIW_Height_Praise";
    private const string PREF_H_NEGATIVE = "GPIW_Height_Negative";
    private const string PREF_H_COMBAT = "GPIW_Height_Combat";

    private const string PREF_MODE = "GPIW_DelimiterMode";

    public enum DelimiterMode
    {
        LinesOnly = 0, // Each line is a phrase; commas are preserved.
        Csv = 1        // Commas/newlines outside quotes are delimiters.
    }

    [SerializeField] private GunnerSO targetSO;

    [TextArea(5, 12)][SerializeField] private string idleCsv = "";
    [TextArea(5, 12)][SerializeField] private string praiseCsv = "";
    [TextArea(5, 12)][SerializeField] private string negativeCsv = "";
    [TextArea(5, 12)][SerializeField] private string combatCsv = "";

    [SerializeField] private bool appendMode = false;
    [SerializeField] private bool removeDuplicates = true;
    [SerializeField] private bool trimWhitespace = true;
    [SerializeField] private DelimiterMode delimiterMode = DelimiterMode.LinesOnly;

    // Foldouts
    [SerializeField] private bool foldIdle;
    [SerializeField] private bool foldPraise;
    [SerializeField] private bool foldNegative;
    [SerializeField] private bool foldCombat;

    // Heights (per-section)
    [SerializeField] private float heightIdle = 140f;
    [SerializeField] private float heightPraise = 140f;
    [SerializeField] private float heightNegative = 140f;
    [SerializeField] private float heightCombat = 140f;

    private Vector2 _scroll;

    [MenuItem("Tools/Idle Defense/Gunner Phrase Importer")]
    public static void ShowWindow()
    {
        var w = GetWindow<GunnerPhraseImporterWindow>("Gunner Phrase Importer");
        w.minSize = new Vector2(680, 520);
        w.LoadPrefs();
        w.Show();
    }

    public static void Open(GunnerSO gunner)
    {
        var w = GetWindow<GunnerPhraseImporterWindow>("Gunner Phrase Importer");
        w.targetSO = gunner;
        w.minSize = new Vector2(680, 520);
        w.LoadPrefs();
        w.Show();
    }

    private void OnEnable() => LoadPrefs();
    private void OnDisable() => SavePrefs();

    private void LoadPrefs()
    {
        foldIdle = EditorPrefs.GetBool(PREF_FOLD_IDLE, true);
        foldPraise = EditorPrefs.GetBool(PREF_FOLD_PRAISE, true);
        foldNegative = EditorPrefs.GetBool(PREF_FOLD_NEG, true);
        foldCombat = EditorPrefs.GetBool(PREF_FOLD_COMBAT, true);

        heightIdle = EditorPrefs.GetFloat(PREF_H_IDLE, heightIdle);
        heightPraise = EditorPrefs.GetFloat(PREF_H_PRAISE, heightPraise);
        heightNegative = EditorPrefs.GetFloat(PREF_H_NEGATIVE, heightNegative);
        heightCombat = EditorPrefs.GetFloat(PREF_H_COMBAT, heightCombat);

        delimiterMode = (DelimiterMode)EditorPrefs.GetInt(PREF_MODE, (int)DelimiterMode.LinesOnly);
    }

    private void SavePrefs()
    {
        EditorPrefs.SetBool(PREF_FOLD_IDLE, foldIdle);
        EditorPrefs.SetBool(PREF_FOLD_PRAISE, foldPraise);
        EditorPrefs.SetBool(PREF_FOLD_NEG, foldNegative);
        EditorPrefs.SetBool(PREF_FOLD_COMBAT, foldCombat);

        EditorPrefs.SetFloat(PREF_H_IDLE, heightIdle);
        EditorPrefs.SetFloat(PREF_H_PRAISE, heightPraise);
        EditorPrefs.SetFloat(PREF_H_NEGATIVE, heightNegative);
        EditorPrefs.SetFloat(PREF_H_COMBAT, heightCombat);

        EditorPrefs.SetInt(PREF_MODE, (int)delimiterMode);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Target Gunner", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            targetSO = (GunnerSO)EditorGUILayout.ObjectField("GunnerSO", targetSO, typeof(GunnerSO), false);
            if (targetSO != null)
            {
                if (GUILayout.Button("Load From SO", GUILayout.Width(120)))
                {
                    LoadFromSO();
                }
            }
        }

        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
            appendMode = EditorGUILayout.ToggleLeft("Append (instead of Replace)", appendMode);
            removeDuplicates = EditorGUILayout.ToggleLeft("Remove Duplicates", removeDuplicates);
            trimWhitespace = EditorGUILayout.ToggleLeft("Trim Whitespace", trimWhitespace);
        }

        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            if (GUILayout.Button("Expand All", GUILayout.Width(110))) SetAllFoldouts(true);
            if (GUILayout.Button("Collapse All", GUILayout.Width(110))) SetAllFoldouts(false);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Delimiter Mode", GUILayout.Width(110));
            delimiterMode = (DelimiterMode)EditorGUILayout.EnumPopup(delimiterMode, GUILayout.Width(140));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear All", GUILayout.Width(90)))
            {
                idleCsv = praiseCsv = negativeCsv = combatCsv = "";
            }
        }

        EditorGUILayout.Space(4);
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawCsvAreaFoldout(
            title: "Idle Phrases (Lines Only by default: one phrase per line)",
            ref foldIdle, ref idleCsv, ref heightIdle);

        DrawCsvAreaFoldout(
            title: "Praise Phrases (use *name* token to address a partner)",
            ref foldPraise, ref praiseCsv, ref heightPraise);

        DrawCsvAreaFoldout(
            title: "Negative Phrases (use *name* token to address a partner)",
            ref foldNegative, ref negativeCsv, ref heightNegative);

        DrawCsvAreaFoldout(
            title: "Combat Chat Phrases",
            ref foldCombat, ref combatCsv, ref heightCombat);

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = targetSO != null;
            if (GUILayout.Button(targetSO == null ? "Select a GunnerSO" : (appendMode ? "Apply (Append)" : "Apply (Replace)"), GUILayout.Height(28)))
            {
                ApplyToSO();
            }
            GUI.enabled = true;
        }

        EditorGUILayout.Space(6);
        string tip =
            delimiterMode == DelimiterMode.LinesOnly
            ? "Tip: Each line is a phrase. Commas are preserved. Quotes are optional and only used to trim surrounding quotes."
            : "Tip: CSV mode: commas/newlines outside quotes are delimiters. Use quotes to include commas inside a phrase (e.g., \"Hey, *name*, nice shot!\").";
        EditorGUILayout.HelpBox(tip, MessageType.Info);
    }

    private void SetAllFoldouts(bool open)
    {
        foldIdle = foldPraise = foldNegative = foldCombat = open;
        SavePrefs(); // persist immediately
    }

    private void DrawCsvAreaFoldout(string title, ref bool fold, ref string text, ref float height)
    {
        GUILayout.Space(6);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            // Header row
            using (new EditorGUILayout.HorizontalScope())
            {
                fold = EditorGUILayout.Foldout(fold, title, true);
                GUILayout.FlexibleSpace();
                GUILayout.Label("Height", GUILayout.Width(45));
                height = EditorGUILayout.Slider(height, 80f, 400f, GUILayout.Width(220));
            }

            if (fold)
            {
                GUILayout.Space(4);
                text = EditorGUILayout.TextArea(text, GUILayout.Height(height));
            }
        }
    }

    private void LoadFromSO()
    {
        if (targetSO == null) return;

        idleCsv = JoinForEditor(targetSO.IdlePhrases);
        praiseCsv = JoinForEditor(targetSO.PraisePhrases);
        negativeCsv = JoinForEditor(targetSO.NegativePhrases);
        combatCsv = JoinForEditor(targetSO.CombatChatPhrases);
    }

    private string JoinForEditor(List<string> list)
    {
        if (list == null || list.Count == 0) return "";
        var sb = new StringBuilder();
        for (int i = 0; i < list.Count; i++)
        {
            string line = list[i] ?? "";
            // Quote only if it contains newline to keep it one line when shown.
            if (line.Contains("\n") || line.Contains("\r"))
                line = "\"" + line.Replace("\"", "\"\"") + "\"";
            sb.Append(line);
            if (i < list.Count - 1) sb.Append("\n");
        }
        return sb.ToString();
    }

    private void ApplyToSO()
    {
        if (targetSO == null)
        {
            EditorUtility.DisplayDialog("No Target", "Please assign a GunnerSO.", "OK");
            return;
        }

        Undo.RecordObject(targetSO, "Import Gunner Phrases");

        var idle = ParsePhrases(idleCsv, delimiterMode, trimWhitespace);
        var praise = ParsePhrases(praiseCsv, delimiterMode, trimWhitespace);
        var negative = ParsePhrases(negativeCsv, delimiterMode, trimWhitespace);
        var combat = ParsePhrases(combatCsv, delimiterMode, trimWhitespace);

        if (removeDuplicates)
        {
            idle = Dedup(idle);
            praise = Dedup(praise);
            negative = Dedup(negative);
            combat = Dedup(combat);
        }

        if (!appendMode)
        {
            targetSO.IdlePhrases.Clear();
            targetSO.PraisePhrases.Clear();
            targetSO.NegativePhrases.Clear();
            targetSO.CombatChatPhrases.Clear();
        }

        targetSO.IdlePhrases.AddRange(idle);
        targetSO.PraisePhrases.AddRange(praise);
        targetSO.NegativePhrases.AddRange(negative);
        targetSO.CombatChatPhrases.AddRange(combat);

        EditorUtility.SetDirty(targetSO);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Import Complete",
            $"Imported phrases into:\n- Idle: {idle.Count}\n- Praise: {praise.Count}\n- Negative: {negative.Count}\n- Combat: {combat.Count}",
            "Nice!");
    }

    // Unified parser using selected delimiter mode
    private static List<string> ParsePhrases(string raw, DelimiterMode mode, bool trim)
    {
        if (string.IsNullOrEmpty(raw)) return new List<string>();

        if (mode == DelimiterMode.LinesOnly)
            return ParseLinesOnly(raw, trim);

        // CSV mode (legacy behavior)
        return ParseCsv(raw, trim);
    }

    // LinesOnly: Every line is a phrase. Commas are preserved.
    private static List<string> ParseLinesOnly(string raw, bool trim)
    {
        var list = new List<string>();
        int len = raw.Length;
        var sb = new StringBuilder();

        for (int i = 0; i < len; i++)
        {
            char c = raw[i];
            if (c == '\r')
            {
                // handle CRLF
                if (i + 1 < len && raw[i + 1] == '\n') i++;
                PushLine(list, sb, trim);
            }
            else if (c == '\n')
            {
                PushLine(list, sb, trim);
            }
            else
            {
                sb.Append(c);
            }
        }
        // last line
        PushLine(list, sb, trim);

        // remove empties
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (string.IsNullOrWhiteSpace(list[i]))
                list.RemoveAt(i);
        }

        return list;
    }

    private static void PushLine(List<string> dst, StringBuilder sb, bool trim)
    {
        string s = sb.ToString();
        sb.Length = 0;

        s = StripSurroundingQuotes(s);
        if (trim) s = s.Trim();

        dst.Add(s);
    }

    // CSV: commas/newlines outside quotes are delimiters; supports "" escaping
    private static List<string> ParseCsv(string raw, bool trim)
    {
        var result = new List<string>();
        var token = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < raw.Length && raw[i + 1] == '"')
                {
                    token.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (!inQuotes && (c == ',' || c == '\n' || c == '\r'))
            {
                PushTokenCsv(result, token, trim);
                if (c == '\r' && i + 1 < raw.Length && raw[i + 1] == '\n') i++;
            }
            else
            {
                token.Append(c);
            }
        }

        PushTokenCsv(result, token, trim);

        for (int r = result.Count - 1; r >= 0; r--)
        {
            if (string.IsNullOrWhiteSpace(result[r]))
                result.RemoveAt(r);
        }

        return result;
    }

    private static void PushTokenCsv(List<string> list, StringBuilder token, bool trim)
    {
        string s = token.ToString();
        token.Length = 0;

        s = StripSurroundingQuotes(s);
        if (trim) s = s.Trim();

        list.Add(s);
    }

    private static string StripSurroundingQuotes(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
        {
            var inner = s.Substring(1, s.Length - 2);
            return inner.Replace("\"\"", "\"");
        }
        return s;
    }

    private static List<string> Dedup(List<string> src)
    {
        var seen = new HashSet<string>();
        var dst = new List<string>();
        foreach (var s in src)
        {
            if (seen.Add(s)) dst.Add(s);
        }
        return dst;
    }
}
