using UnityEngine;
using UnityEditor;
using TMPro;

public class TextColorChangerEditor : EditorWindow
{
    private Color targetColor = Color.white;

    [MenuItem("Tools/TMP Color Changer")]
    public static void ShowWindow()
    {
        GetWindow<TextColorChangerEditor>("TMP Color Changer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Change All TextMeshPro Colors", EditorStyles.boldLabel);

        targetColor = EditorGUILayout.ColorField("Target Color", targetColor);

        if (GUILayout.Button("Apply to All TextMeshPro in Scene"))
        {
            ApplyColorToAllTMP();
        }
    }

    private void ApplyColorToAllTMP()
    {
        int count = 0;

        // TextMeshProUGUI (for UI)
        foreach (var tmpUI in FindObjectsOfType<TextMeshProUGUI>(true))
        {
            Undo.RecordObject(tmpUI, "Change TMP Color");
            tmpUI.color = targetColor;
            EditorUtility.SetDirty(tmpUI);
            count++;
        }

        // TextMeshPro (for 3D world space text)
        foreach (var tmp in FindObjectsOfType<TextMeshPro>(true))
        {
            Undo.RecordObject(tmp, "Change TMP Color");
            tmp.color = targetColor;
            EditorUtility.SetDirty(tmp);
            count++;
        }

        Debug.Log($"[TMP Color Changer] Changed color on {count} TextMeshPro components.");
    }
}
