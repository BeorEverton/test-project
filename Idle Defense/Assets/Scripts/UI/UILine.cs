using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class UILine : Graphic
{
    public List<Vector2> points = new List<Vector2>()
    {
        new Vector2(0f, 0f),
        new Vector2(100f, 0f)
    };

    [Min(0.1f)] public float thickness = 4f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (points == null || points.Count < 2) return;

        float half = thickness * 0.5f;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 a = points[i];
            Vector2 b = points[i + 1];
            Vector2 dir = (b - a).normalized;

            // explicit type instead of target-typed `new`
            Vector2 normal = new Vector2(-dir.y, dir.x) * half;

            // quad corners in local (UI) space
            Vector2 v0 = a - normal;
            Vector2 v1 = a + normal;
            Vector2 v2 = b + normal;
            Vector2 v3 = b - normal;

            int idx = vh.currentVertCount;

            UIVertex vert = UIVertex.simpleVert;
            vert.color = color;

            vert.position = v0; vh.AddVert(vert);
            vert.position = v1; vh.AddVert(vert);
            vert.position = v2; vh.AddVert(vert);
            vert.position = v3; vh.AddVert(vert);

            vh.AddTriangle(idx + 0, idx + 1, idx + 2);
            vh.AddTriangle(idx + 2, idx + 3, idx + 0);
        }
    }

    public void SetPoints(IList<Vector2> newPoints)
    {
        points = new List<Vector2>(newPoints);
        SetVerticesDirty();
    }
}
