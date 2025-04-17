using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class HeartbeatLine : MonoBehaviour
{
    public int points = 200;
    public float width = 0.05f;
    public float waveAmplitude = 0.5f;
    public float speed = 5f;

    private LineRenderer lr;
    private Vector3[] positions;
    public bool isTyping = false;

    void Start()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = points;
        lr.startWidth = lr.endWidth = width;
        positions = new Vector3[points];
        ResetLine();
    }

    void Update()
    {
        if (!isTyping) return;

        float time = Time.time * speed;
        for (int i = 0; i < points; i++)
        {
            float x = i * 0.05f;
            float y = Mathf.PerlinNoise(time + i * 0.1f, 0f) * waveAmplitude;
            positions[i] = new Vector3(x, y, 0);
        }

        lr.SetPositions(positions);
    }

    public void SetTypingActive(bool state)
    {
        isTyping = state;

        if (!state)
            ResetLine();
    }

    private void ResetLine()
    {
        for (int i = 0; i < points; i++)
            positions[i] = new Vector3(i * 0.05f, 0, 0);

        if (lr != null)
            lr.SetPositions(positions);
    }
}
