using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class PrestigeConnectionViewLR : MonoBehaviour
{
    [Header("Baked Path (local to this object)")]
    public List<Vector3> bakedLocalPoints = new List<Vector3>(); // set by editor tool

    [Header("Appearance")]
    public float width = 6f;
    public Color fullAlphaColor = Color.white;
    public Color greyedColor = new Color(0.6f, 0.6f, 0.6f, 0.7f);
    public Color blackFaded = new Color(0f, 0f, 0f, 0.28f);

    [Header("Optional FX (cheap)")]
    public bool enablePulseWhenAvailable = true;
    public float pulseWidthMul = 1.25f;
    public float pulseSpeed = 4f;  // Hz
    public bool enableWaveWhenAvailable = false;
    public float waveAmplitude = 4f; // local pixels
    public float waveFrequency = 1f; // cycles along length
    public float waveSpeed = 1f;

    [HideInInspector] public string parentNodeId;
    [HideInInspector] public string childNodeId;

    private LineRenderer lr;
    private Vector3[] runtimePoints; // world points converted from baked locals
    private float baseWidth;
    private bool isAvailableState; // for FX gating

    void Awake()
    {
        lr = GetComponent<LineRenderer>();

        if (string.IsNullOrEmpty(parentNodeId) || string.IsNullOrEmpty(childNodeId))
        {
            Debug.LogWarning($"[PrestigeConnection] '{name}' missing parent/child IDs. " +
                             $"Set 'parentNodeId' and 'childNodeId' before baking.");
        }

        EnsureLR();
        if (lr != null)
        {
            lr.positionCount = 0;
            baseWidth = width;
            lr.useWorldSpace = false;
            ApplyBakedPathToRenderer();
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        EnsureLR();
        if (lr != null)
        {
            baseWidth = width;
            // keep preview fresh when values change in inspector
            if (bakedLocalPoints != null && bakedLocalPoints.Count > 0)
                ApplyBakedPathToRenderer();
        }
    }
#endif


    public void SetStateFullAlpha()
    {
        lr.startColor = lr.endColor = fullAlphaColor;
        isAvailableState = false;
        lr.widthMultiplier = baseWidth / Mathf.Max(0.0001f, lr.widthMultiplier); // normalize
        lr.widthMultiplier = 1f; // reset pulse
    }

    public void SetStateGreyed()
    {
        lr.startColor = lr.endColor = greyedColor;
        isAvailableState = true; // can pulse/wave if enabled
    }

    public void SetStateBlackFaded()
    {
        lr.startColor = lr.endColor = blackFaded;
        isAvailableState = true; // can pulse/wave if enabled
    }

    void Update()
    {
        // Optional FX (very cheap, no allocations)
        if (!isAvailableState) return;

        // Pulse width
        if (enablePulseWhenAvailable)
        {
            float m = 1f + (pulseWidthMul - 1f) * 0.5f * (1f + Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f));
            lr.widthMultiplier = m;
        }

        // Wave offset (applies a small sin displacement perpendicular to segment direction)
        if (enableWaveWhenAvailable && runtimePoints != null && runtimePoints.Length >= 2)
        {
            int n = runtimePoints.Length;
            for (int i = 0; i < n; i++)
            {
                float t = (n == 1) ? 0f : (float)i / (n - 1);
                Vector3 p = runtimePoints[i];

                // Compute tangent (approx) to get a normal vector
                Vector3 tangent;
                if (i == 0) tangent = (runtimePoints[1] - runtimePoints[0]).normalized;
                else if (i == n - 1) tangent = (runtimePoints[n - 1] - runtimePoints[n - 2]).normalized;
                else tangent = (runtimePoints[i + 1] - runtimePoints[i - 1]).normalized;

                Vector3 normal = new Vector3(-tangent.y, tangent.x, tangent.z); // screen-space-ish
                float phase = (t * waveFrequency * Mathf.PI * 2f) + (Time.time * waveSpeed);
                Vector3 off = normal * (waveAmplitude * Mathf.Sin(phase));
                lr.SetPosition(i, p + off);
            }
        }
    }

    // Called at Awake and whenever you rebake in editor
    public void ApplyBakedPathToRenderer()
    {
        EnsureLR();
        if (lr == null) return;

        if (bakedLocalPoints == null || bakedLocalPoints.Count == 0)
        {
            lr.positionCount = 0;
            runtimePoints = null;
            return;
        }

        // No transform conversion: points are already in our parent's local space
        int n = bakedLocalPoints.Count;

        if (runtimePoints == null || runtimePoints.Length != n)
            runtimePoints = new Vector3[n];

        for (int i = 0; i < n; i++)
            runtimePoints[i] = bakedLocalPoints[i];

        lr.positionCount = n;
        lr.SetPositions(runtimePoints);
        lr.widthMultiplier = 1f;
        lr.startWidth = lr.endWidth = width;
    }


    // Editor utility: called by router to set baked points
    public void SetBakedLocalPoints(List<Vector3> pts)
    {
        bakedLocalPoints = pts ?? new List<Vector3>();
        ApplyBakedPathToRenderer();
    }


    private void EnsureLR()
    {
        if (lr == null)
            lr = GetComponent<LineRenderer>();

        if (lr == null)
            return; // should never happen because of RequireComponent

        // Reasonable defaults (also useful in Editor before play)
        lr.useWorldSpace = false;
        lr.alignment = LineAlignment.View;
        lr.numCornerVertices = 2;
        lr.numCapVertices = 2;

#if UNITY_EDITOR
        // Give it a default material so it shows up in scene view/game view
        if (lr.sharedMaterial == null)
        {
            var sh = Shader.Find("Sprites/Default");
            if (sh != null) lr.sharedMaterial = new Material(sh);
        }
#endif
    }

}
