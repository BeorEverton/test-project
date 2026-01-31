using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(GunnerUnlockTableSO))]
public class GunnerUnlockTableSOEditor : Editor
{
    private ReorderableList list;

    private void OnEnable()
    {
        SerializedProperty entriesProp = serializedObject.FindProperty("Entries");

        list = new ReorderableList(serializedObject, entriesProp, true, true, true, true);

        list.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, "Gunner Unlock Entries");
        };

        list.elementHeightCallback = index =>
        {
            SerializedProperty element = entriesProp.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(element, true) + 6f;
        };

        list.drawElementCallback = (rect, index, active, focused) =>
        {
            SerializedProperty element = entriesProp.GetArrayElementAtIndex(index);
            rect.y += 2f;
            EditorGUI.PropertyField(rect, element, true);
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        float availableHeight =
            EditorGUIUtility.currentViewWidth > 0
                ? EditorGUIUtility.currentViewWidth
                : 300f;

        list.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }
}
