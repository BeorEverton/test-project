using TMPro;
using UnityEditor;
using UnityEngine;

public class ReplaceTMPFont : EditorWindow
{
    private TMP_FontAsset newFont;

    [MenuItem("Tools/TMP/Replace All TMP Fonts In Scene")]
    public static void ShowWindow()
    {
        GetWindow<ReplaceTMPFont>("Replace TMP Fonts");
    }

    private void OnGUI()
    {
        GUILayout.Label("Replace TextMeshPro Font in Scene", EditorStyles.boldLabel);
        newFont = (TMP_FontAsset)EditorGUILayout.ObjectField("New TMP Font", newFont, typeof(TMP_FontAsset), false);

        if (GUILayout.Button("Replace Fonts"))
        {
            if (newFont == null)
            {
                Debug.LogWarning("Please assign a TMP Font Asset before replacing.");
                return;
            }

            ReplaceAllTMPFonts();
        }
    }

    private void ReplaceAllTMPFonts()
    {
        int count = 0;
        var texts = FindObjectsOfType<TMP_Text>(true); // includes inactive objects
        foreach (var tmp in texts)
        {
            Undo.RecordObject(tmp, "Replace TMP Font");
            tmp.font = newFont;
            EditorUtility.SetDirty(tmp);
            count++;
        }

        Debug.Log($"Replaced font on {count} TextMeshPro objects in the scene.");
    }
}
