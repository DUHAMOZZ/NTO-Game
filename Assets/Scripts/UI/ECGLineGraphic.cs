using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class ECGLineGraphic : MaskableGraphic
{
    [Header("Параметры линии ЭКГ")]
    [SerializeField, Range(16, 128)] private int _sampleCount = 72;
    [SerializeField] private float _lineThickness = 3.0f;
    [SerializeField] private float _cyclesVisible = 2.0f;
    [SerializeField, Range(0f, 1f)] private float _tailMinAlpha = 0.15f;

    private float _scrollPhase;
    private float _jitterAmount;
    private float _noiseTimer;

    public void UpdateWave(float scrollPhase, float jitterAmount, Color lineColor)
    {
        _scrollPhase = scrollPhase;
        _jitterAmount = jitterAmount;
        _noiseTimer = Time.time;

        if (color != lineColor)
        {
            color = lineColor;
        }

        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        if (rect.width <= 0f || rect.height <= 0f || _sampleCount < 2) return;

        float halfHeight = rect.height * 0.40f;
        float centerY = rect.center.y;
        float halfThickness = _lineThickness * 0.5f;

        Color baseColor = color;
        Vector2 prevPoint = EvaluatePoint(0, rect, centerY, halfHeight);

        for (int i = 1; i < _sampleCount; i++)
        {
            float tCurr = i / (float)(_sampleCount - 1);
            float tPrev = (i - 1) / (float)(_sampleCount - 1);
            Vector2 currPoint = EvaluatePoint(i, rect, centerY, halfHeight);

            Vector2 dir = currPoint - prevPoint;
            float len = dir.magnitude;
            if (len > 0.0001f)
            {
                dir /= len;
                Vector2 normal = new Vector2(-dir.y, dir.x) * halfThickness;

                Color32 c0 = WithAlpha(baseColor, Mathf.Lerp(_tailMinAlpha, 1f, tPrev));
                Color32 c1 = WithAlpha(baseColor, Mathf.Lerp(_tailMinAlpha, 1f, tCurr));

                int vertStart = vh.currentVertCount;
                vh.AddVert(prevPoint - normal, c0, Vector4.zero);
                vh.AddVert(prevPoint + normal, c0, Vector4.zero);
                vh.AddVert(currPoint + normal, c1, Vector4.zero);
                vh.AddVert(currPoint - normal, c1, Vector4.zero);

                vh.AddTriangle(vertStart, vertStart + 1, vertStart + 2);
                vh.AddTriangle(vertStart + 2, vertStart + 3, vertStart);
            }

            prevPoint = currPoint;
        }
    }

    private Vector2 EvaluatePoint(int index, Rect rect, float centerY, float halfHeight)
    {
        float xNorm = index / (float)(_sampleCount - 1);
        float x = Mathf.Lerp(rect.xMin, rect.xMax, xNorm);

        float cycleT = (xNorm * _cyclesVisible) - _scrollPhase;
        cycleT -= Mathf.Floor(cycleT);

        float waveY = EvaluatePQRST(cycleT);

        if (_jitterAmount > 0.001f)
        {
            float noise = (Mathf.PerlinNoise(xNorm * 24f + _noiseTimer * 18f, _noiseTimer * 7f) - 0.5f) * 2f;
            waveY += noise * _jitterAmount * 0.35f;
        }

        waveY = Mathf.Clamp(waveY, -1f, 1f);
        return new Vector2(x, centerY + waveY * halfHeight);
    }

    private static float EvaluatePQRST(float t)
    {
        // P-зубец
        if (t >= 0.06f && t < 0.16f)
        {
            float u = (t - 0.06f) / 0.10f;
            return Mathf.Sin(u * Mathf.PI) * 0.16f;
        }
        // Q-провал
        if (t >= 0.21f && t < 0.24f)
        {
            float u = (t - 0.21f) / 0.03f;
            return -Mathf.Sin(u * Mathf.PI) * 0.20f;
        }
        // R-пик
        if (t >= 0.24f && t < 0.29f)
        {
            float u = (t - 0.24f) / 0.05f;
            return (u < 0.5f ? (u * 2f) : (2f - u * 2f)) * 1.0f;
        }
        // S-провал
        if (t >= 0.29f && t < 0.34f)
        {
            float u = (t - 0.29f) / 0.05f;
            return -Mathf.Sin(u * Mathf.PI) * 0.34f;
        }
        // T-волна
        if (t >= 0.44f && t < 0.58f)
        {
            float u = (t - 0.44f) / 0.14f;
            return Mathf.Sin(u * Mathf.PI) * 0.26f;
        }
        return 0f;
    }

    private static Color32 WithAlpha(Color c, float alphaFactor)
    {
        return new Color(c.r, c.g, c.b, c.a * alphaFactor);
    }
}