using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System;
using System.Collections.Generic;

public class CanvasUIOrientationManager : EditorWindow
{
    private Canvas selectedCanvas;

    [MenuItem("Tools/Canvas UI Orientation Manager")]
    public static void ShowWindow()
    {
        GetWindow<CanvasUIOrientationManager>("Canvas UI Orientation Manager");
    }

    private void OnGUI()
    {
        GUILayout.Label("Canvas UI Orientation Manager", EditorStyles.boldLabel);
        selectedCanvas = EditorGUILayout.ObjectField("Canvas", selectedCanvas, typeof(Canvas), true) as Canvas;

        if (selectedCanvas == null)
        {
            EditorGUILayout.HelpBox("Please assign a Canvas to manage all child UI elements.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space();

        GUILayout.Label("Save Layouts", EditorStyles.boldLabel);
        if (GUILayout.Button("Save Portrait Layout"))
            ProcessAllChildren("Portrait", SaveLayout);

        if (GUILayout.Button("Save Landscape Layout"))
            ProcessAllChildren("Landscape", SaveLayout);

        EditorGUILayout.Space();

        GUILayout.Label("Apply Layouts", EditorStyles.boldLabel);
        if (GUILayout.Button("Apply Portrait Layout"))
            ProcessAllChildren("Portrait", ApplyLayout);

        if (GUILayout.Button("Apply Landscape Layout"))
            ProcessAllChildren("Landscape", ApplyLayout);
    }

    private void ProcessAllChildren(string layoutKey, Action<RectTransform, string> processFunc)
    {
        RectTransform[] rects = selectedCanvas.GetComponentsInChildren<RectTransform>(true);
        foreach (var rect in rects)
        {
            if (rect == selectedCanvas.transform) continue; // skip the root canvas
            processFunc(rect, layoutKey);
        }
    }

    private void SaveLayout(RectTransform rect, string layoutKey)
    {
        string id = GenerateKey(rect, layoutKey);
        EditorPrefs.SetString(id + "_anchoredPosition", JsonUtility.ToJson(rect.anchoredPosition));
        EditorPrefs.SetString(id + "_sizeDelta", JsonUtility.ToJson(rect.sizeDelta));
        EditorPrefs.SetString(id + "_anchorMin", JsonUtility.ToJson(rect.anchorMin));
        EditorPrefs.SetString(id + "_anchorMax", JsonUtility.ToJson(rect.anchorMax));
        EditorPrefs.SetString(id + "_pivot", JsonUtility.ToJson(rect.pivot));

        LayoutGroup group = rect.GetComponent<LayoutGroup>();
        if (group != null)
        {
            LayoutGroupData data = new LayoutGroupData(group);
            EditorPrefs.SetString(id + "_layoutGroup", JsonUtility.ToJson(data));
        }
    }

    private void ApplyLayout(RectTransform rect, string layoutKey)
    {
        string id = GenerateKey(rect, layoutKey);
        try
        {
            rect.anchoredPosition = JsonUtility.FromJson<Vector2>(EditorPrefs.GetString(id + "_anchoredPosition"));
            rect.sizeDelta = JsonUtility.FromJson<Vector2>(EditorPrefs.GetString(id + "_sizeDelta"));
            rect.anchorMin = JsonUtility.FromJson<Vector2>(EditorPrefs.GetString(id + "_anchorMin"));
            rect.anchorMax = JsonUtility.FromJson<Vector2>(EditorPrefs.GetString(id + "_anchorMax"));
            rect.pivot = JsonUtility.FromJson<Vector2>(EditorPrefs.GetString(id + "_pivot"));

            LayoutGroup group = rect.GetComponent<LayoutGroup>();
            if (group != null && EditorPrefs.HasKey(id + "_layoutGroup"))
            {
                LayoutGroupData data = JsonUtility.FromJson<LayoutGroupData>(EditorPrefs.GetString(id + "_layoutGroup"));
                data.ApplyTo(group);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Layout could not be applied to '{rect.name}': {e.Message}");
        }
    }

    private string GenerateKey(RectTransform rect, string layoutKey)
    {
        string path = GetHierarchyPath(rect.transform);
        return $"{selectedCanvas.name}_{path}_{layoutKey}";
    }

    private string GetHierarchyPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null && t.parent != selectedCanvas.transform)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }



    [Serializable]
    private class LayoutGroupData
    {
        public RectOffset padding;
        public float spacing;
        public TextAnchor childAlignment;

        public LayoutGroupData(LayoutGroup group)
        {
            padding = new RectOffset(group.padding.left, group.padding.right, group.padding.top, group.padding.bottom);
            childAlignment = group.childAlignment;

            if (group is HorizontalOrVerticalLayoutGroup hvGroup)
                spacing = hvGroup.spacing;
        }

        public void ApplyTo(LayoutGroup group)
        {
            group.padding = new RectOffset(padding.left, padding.right, padding.top, padding.bottom);
            group.childAlignment = childAlignment;

            if (group is HorizontalOrVerticalLayoutGroup hvGroup)
                hvGroup.spacing = spacing;
        }
    }
}
