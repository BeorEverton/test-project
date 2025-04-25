using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;

public class CanvasUIOrientationManager : EditorWindow
{
    private Canvas selectedCanvas;
    private Camera mainCamera;
    private const string folderPath = "Assets/LayoutData";

    [MenuItem("Tools/Canvas UI Orientation Manager")]
    public static void ShowWindow()
    {
        GetWindow<CanvasUIOrientationManager>("Canvas UI Orientation Manager");
    }

    private void OnEnable()
    {
        GameObject canvasGO = GameObject.Find("Main Canvas");
        if (canvasGO != null)
            selectedCanvas = canvasGO.GetComponent<Canvas>();

        mainCamera = Camera.main;
        Directory.CreateDirectory(folderPath);
    }

    private void OnGUI()
    {
        GUILayout.Label("Canvas UI Orientation Manager", EditorStyles.boldLabel);
        selectedCanvas = EditorGUILayout.ObjectField("Canvas", selectedCanvas, typeof(Canvas), true) as Canvas;

        if (selectedCanvas == null)
        {
            EditorGUILayout.HelpBox("Main Canvas not found. Please assign one.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space();

        GUILayout.Label("Save Layouts", EditorStyles.boldLabel);
        if (GUILayout.Button("Save Portrait Layout"))
            SaveAll("Portrait");

        if (GUILayout.Button("Save Landscape Layout"))
            SaveAll("Landscape");

        EditorGUILayout.Space();

        GUILayout.Label("Apply Layouts", EditorStyles.boldLabel);
        if (GUILayout.Button("Apply Portrait Layout"))
            ApplyAll("Portrait");

        if (GUILayout.Button("Apply Landscape Layout"))
            ApplyAll("Landscape");
    }

    private void SaveAll(string layoutKey)
    {
        foreach (var rect in selectedCanvas.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect == selectedCanvas.transform) continue;
            SaveLayout(rect, layoutKey);
        }

        SaveCamera(layoutKey);
        AssetDatabase.Refresh();
        Debug.Log($"{layoutKey} layout saved (Canvas + Camera).");
    }

    private void ApplyAll(string layoutKey)
    {
        foreach (var rect in selectedCanvas.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect == selectedCanvas.transform) continue;
            ApplyLayout(rect, layoutKey);
        }

        ApplyCamera(layoutKey);
        Debug.Log($"{layoutKey} layout applied (Canvas + Camera).");
    }

    // Corrected GenerateKey method
   

    // SaveLayout (no change needed for file name — still use rect.name for the JSON file)
    private void SaveLayout(RectTransform rect, string layoutKey)
    {
        string fileName = $"{folderPath}/{rect.gameObject.name}_{layoutKey}.json";
        RectTransformData data = new RectTransformData(rect);

        LayoutGroup group = rect.GetComponent<LayoutGroup>();
        if (group != null)
            data.layoutGroup = new LayoutGroupData(group);

        File.WriteAllText(fileName, JsonUtility.ToJson(data, true));

        // Save a backup to EditorPrefs
        EditorPrefs.SetString(GenerateKey(rect, layoutKey), JsonUtility.ToJson(data));
    }

    // ApplyLayout (already good — it tries File first, then EditorPrefs with corrected key)


    private void ApplyLayout(RectTransform rect, string layoutKey)
    {
        string fileName = $"{folderPath}/{rect.gameObject.name}_{layoutKey}.json";
        string key = GenerateKey(rect, layoutKey);

        string json = File.Exists(fileName) ? File.ReadAllText(fileName) :
                      EditorPrefs.HasKey(key) ? EditorPrefs.GetString(key) : null;

        if (string.IsNullOrEmpty(json)) return;

        RectTransformData data = JsonUtility.FromJson<RectTransformData>(json);
        data.ApplyTo(rect);
    }

    private void SaveCamera(string layoutKey)
    {
        if (mainCamera == null) return;

        CameraData data = new CameraData(mainCamera);
        string filePath = $"{folderPath}/MainCamera_{layoutKey}.json";
        File.WriteAllText(filePath, JsonUtility.ToJson(data, true));
    }

    private void ApplyCamera(string layoutKey)
    {
        if (mainCamera == null) return;

        string filePath = $"{folderPath}/MainCamera_{layoutKey}.json";
        if (!File.Exists(filePath)) return;

        CameraData data = JsonUtility.FromJson<CameraData>(File.ReadAllText(filePath));
        data.ApplyTo(mainCamera);
    }

    private string GenerateKey(RectTransform rect, string layoutKey)
    {
        return $"{selectedCanvas.name}_{rect.GetInstanceID()}_{layoutKey}";
    }

    // --- DATA CLASSES ---

    [Serializable]
    private class RectTransformData
    {
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public LayoutGroupData layoutGroup;

        public RectTransformData(RectTransform rect)
        {
            anchoredPosition = rect.anchoredPosition;
            sizeDelta = rect.sizeDelta;
            anchorMin = rect.anchorMin;
            anchorMax = rect.anchorMax;
            pivot = rect.pivot;
        }

        public void ApplyTo(RectTransform rect)
        {
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;

            if (layoutGroup != null)
            {
                LayoutGroup group = rect.GetComponent<LayoutGroup>();
                if (group != null)
                    layoutGroup.ApplyTo(group);
            }
        }
    }

    [Serializable]
    private class LayoutGroupData
    {
        public RectOffset padding = new RectOffset();
        public float spacing = 0;
        public TextAnchor childAlignment;

        public LayoutGroupData(LayoutGroup group)
        {
            padding = new RectOffset(group.padding.left, group.padding.right, group.padding.top, group.padding.bottom);
            childAlignment = group.childAlignment;

            if (group is HorizontalOrVerticalLayoutGroup hv)
                spacing = hv.spacing;
        }

        public void ApplyTo(LayoutGroup group)
        {
            group.padding = new RectOffset(padding.left, padding.right, padding.top, padding.bottom);
            group.childAlignment = childAlignment;

            if (group is HorizontalOrVerticalLayoutGroup hv)
                hv.spacing = spacing;
        }
    }

    [Serializable]
    private class CameraData
    {
        public Vector3 position;
        public Quaternion rotation;
        public bool orthographic;
        public float orthographicSize;
        public float fieldOfView;

        public CameraData(Camera cam)
        {
            position = cam.transform.position;
            rotation = cam.transform.rotation;
            orthographic = cam.orthographic;
            orthographicSize = cam.orthographicSize;
            fieldOfView = cam.fieldOfView;
        }

        public void ApplyTo(Camera cam)
        {
            cam.transform.position = position;
            cam.transform.rotation = rotation;
            cam.orthographic = orthographic;
            cam.orthographicSize = orthographicSize;
            cam.fieldOfView = fieldOfView;
        }
    }
}
