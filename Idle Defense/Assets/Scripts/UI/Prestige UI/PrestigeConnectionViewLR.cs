using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(UILineGraphic))]
public class PrestigeConnectionViewLR : MonoBehaviour
{
    [Header("Baked Path (local to this object / canvasRoot space)")]
    public List<Vector3> bakedLocalPoints = new List<Vector3>(); // set by editor tool

    [Header("Appearance")]
    public float width = 6f;
    public Color fullAlphaColor = Color.white;
    public Color greyedColor = new Color(0.6f, 0.6f, 0.6f, 0.7f);
    public Color blackFaded = new Color(0f, 0f, 0f, 0.28f);

    [Header("Optional FX (cheap)")]
    public bool enablePulseWhenAvailable = false;
    public float pulseWidthMul = 1.25f;
    public float pulseSpeed = 4f; // Hz
    public bool enableWaveWhenAvailable = false;
    public float waveAmplitude = 4f; // local pixels
    public float waveFrequency = 1f; // cycles along length
    public float waveSpeed = 1f;

    [HideInInspector] public string parentNodeId;
    [HideInInspector] public string childNodeId;

    private UILineGraphic uiLine;

    // Cached points (no allocations in Update)
    private Vector2[] basePoints2D;
    private Vector2[] runtimePoints2D;
    private readonly List<Vector2> runtimeList = new List<Vector2>(64);

    private float baseWidth;
    private bool fxEnabledState;

    [Header("Point Space Root")]
    [Tooltip("If bakedLocalPoints were authored in canvasRoot space, assign that RectTransform here so we can convert into this line's local space.")]
    public RectTransform spaceRoot;

    void Awake()
    {
        EnsureLine();
        baseWidth = width;
        ApplyBakedPathToRenderer();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        EnsureLine();
        baseWidth = width;

        if (bakedLocalPoints != null && bakedLocalPoints.Count > 0)
            ApplyBakedPathToRenderer();
    }
#endif

    public void SetStateFullAlpha()
    {
        EnsureLine();
        SetColor(fullAlphaColor);
        fxEnabledState = false;
        SetWidth(width);
    }

    public void SetStateGreyed()
    {
        EnsureLine();
        SetColor(greyedColor);
        fxEnabledState = true;
        SetWidth(width);
    }

    public void SetStateBlackFaded()
    {
        EnsureLine();
        SetColor(blackFaded);
        fxEnabledState = true;
        SetWidth(width);
    }

    void Update()
    {
        if (!fxEnabledState) return;
        EnsureLine();
        if (uiLine == null) return;

        // Pulse width
        if (enablePulseWhenAvailable)
        {
            float m = 1f + (pulseWidthMul - 1f) * 0.5f * (1f + Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f));
            SetWidth(baseWidth * m);
        }

        // Wave offset (rebuilds points list, but no GC allocations)
        if (enableWaveWhenAvailable && basePoints2D != null && basePoints2D.Length >= 2)
        {
            int n = basePoints2D.Length;
            EnsureRuntimeBuffers(n);

            float tNow = Time.time * waveSpeed;

            for (int i = 0; i < n; i++)
            {
                float u = (n == 1) ? 0f : (float)i / (n - 1);

                // Tangent approx
                Vector2 tangent;
                if (i == 0) tangent = (basePoints2D[1] - basePoints2D[0]).normalized;
                else if (i == n - 1) tangent = (basePoints2D[n - 1] - basePoints2D[n - 2]).normalized;
                else tangent = (basePoints2D[i + 1] - basePoints2D[i - 1]).normalized;

                Vector2 normal = new Vector2(-tangent.y, tangent.x);
                float phase = (u * waveFrequency * Mathf.PI * 2f) + tNow;

                runtimePoints2D[i] = basePoints2D[i] + normal * (waveAmplitude * Mathf.Sin(phase));
            }

            runtimeList.Clear();
            for (int i = 0; i < n; i++) runtimeList.Add(runtimePoints2D[i]);
            uiLine.SetPoints(runtimeList);
        }
    }

    public void ApplyBakedPathToRenderer()
    {
        EnsureLine();
        if (uiLine == null) return;

        if (bakedLocalPoints == null || bakedLocalPoints.Count == 0)
        {
            uiLine.SetPoints(System.Array.Empty<Vector2>());
            return;
        }

        int n = bakedLocalPoints.Count;
        EnsureRuntimeBuffers(n);

        RectTransform lineRT = transform as RectTransform;

        // If no spaceRoot is set, fall back to assuming baked points are already in line local space.
        bool canConvert = (spaceRoot != null && lineRT != null);

        for (int i = 0; i < n; i++)
        {
            Vector3 p = bakedLocalPoints[i];

            if (canConvert)
            {
                // bakedLocalPoints are in spaceRoot local space -> convert to world -> convert to line local
                Vector3 world = spaceRoot.TransformPoint(p);
                Vector3 local = lineRT.InverseTransformPoint(world);
                basePoints2D[i] = new Vector2(local.x, local.y);
            }
            else
            {
                basePoints2D[i] = new Vector2(p.x, p.y);
            }
        }

        runtimeList.Clear();
        for (int i = 0; i < n; i++) runtimeList.Add(basePoints2D[i]);

        uiLine.SetPoints(runtimeList);
        SetWidth(width);
    }

    public void SetBakedLocalPoints(List<Vector3> pts)
    {
        bakedLocalPoints = pts ?? new List<Vector3>();
        ApplyBakedPathToRenderer();
    }

    public void SetColor(Color c)
    {
        EnsureLine();
        if (uiLine == null) return;

        uiLine.color = c;
        uiLine.SetVerticesDirty();
    }

    private void SetWidth(float w)
    {
        EnsureLine();
        if (uiLine == null) return;

        uiLine.SetThickness(w);
    }

    private void EnsureLine()
    {
        if (uiLine == null)
            uiLine = GetComponent<UILineGraphic>();

        if (uiLine != null)
        {
            uiLine.raycastTarget = false;

            var rt = transform as RectTransform;
            if (rt != null && rt.parent is RectTransform prt)
            {
                // Stretch to parent (not “canvas”), and align pivot to parent so local-space math stays consistent.
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                rt.pivot = prt.pivot;

                rt.localRotation = Quaternion.identity;
                rt.localScale = Vector3.one;
                rt.localPosition = Vector3.zero;
            }
        }
    }

    private void EnsureRuntimeBuffers(int n)
    {
        if (basePoints2D == null || basePoints2D.Length != n)
            basePoints2D = new Vector2[n];

        if (runtimePoints2D == null || runtimePoints2D.Length != n)
            runtimePoints2D = new Vector2[n];
    }
}