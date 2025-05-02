using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class HeartbeatLine : MonoBehaviour
{
    public int bars = 64;
    public float barSpacing = 0.15f;
    public float maxAmplitude = 1.2f;
    public float pulseSpeed = 10f;
    public bool isTyping = false;

    private LineRenderer lr;
    private Vector3[] positions;
    private float[] barHeights;
    private float noiseOffset;

    void Start()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = bars * 2; // each bar = 2 points (bottom & top)
        lr.widthMultiplier = 0.1f;

        positions = new Vector3[lr.positionCount];
        barHeights = new float[bars];

        ResetWaveform();
    }

    void Update()
    {
        if (isTyping)
        {
            AnimateWaveform();
            ApplyWaveform();
        }
    }

    void AnimateWaveform()
    {
        noiseOffset += Time.unscaledDeltaTime * pulseSpeed;

        for (int i = 0; i < bars; i++)
        {
            float randomPulse = Mathf.PerlinNoise(i * 0.5f, noiseOffset);
            barHeights[i] = randomPulse * maxAmplitude;
        }
    }

    void ApplyWaveform()
    {
        for (int i = 0; i < bars; i++)
        {
            float x = i * barSpacing;
            float height = barHeights[i];

            positions[i * 2] = new Vector3(x, -height / 2f, 0f); // bottom of bar
            positions[i * 2 + 1] = new Vector3(x, height / 2f, 0f); // top of bar
        }

        lr.positionCount = positions.Length;
        lr.SetPositions(positions);
    }

    public void SetTypingActive(bool state)
    {
        isTyping = state;
        if (!state)
            ResetWaveform();
    }

    void ResetWaveform()
    {
        for (int i = 0; i < bars; i++)
        {
            float x = i * barSpacing;
            positions[i * 2] = new Vector3(x, 0f, 0f);
            positions[i * 2 + 1] = new Vector3(x, 0f, 0f);
        }

        lr.positionCount = positions.Length;
        lr.SetPositions(positions);
    }
}
