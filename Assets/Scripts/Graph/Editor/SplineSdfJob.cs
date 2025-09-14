using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public struct SplineSegment
{
    public float2 a;     // segment start
    public float2 b;     // segment end
    public float2 ab;    // (b - a)
    public float ab2;   // dot(ab, ab) (squared length)
}

[BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
public struct SplineSdfJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<SplineSegment> Segments;
    [ReadOnly] public int Size;

    [WriteOnly] public NativeArray<float> Output;

    public void Execute(int index)
    {
        int x = index % Size;
        int y = index / Size;

        // Use pixel center
        float2 position = new float2(x + 0.5f, y + 0.5f);

        // Distance to polyline
        float minDistance2 = float.PositiveInfinity;
        float sign = 0;

        for (int i = 0; i < Segments.Length; i++)
        {
            var segment = Segments[i];

            // project p onto segment
            float t = 0f;
            if (segment.ab2 > 1e-12f)
            {
                t = math.saturate(math.dot(position - segment.a, segment.ab) / segment.ab2);
            }

            float2 q = segment.a + t * segment.ab;
            float2 d = position - q;
            float distance2 = math.dot(d, d);

            if (distance2 < minDistance2)
            {
                minDistance2 = distance2;

                // 2D cross product to get side of line
                float cross = segment.ab.x * d.y - segment.ab.y * d.x;
                sign = math.sign(cross);
            }
        }

        float distance = math.sqrt(minDistance2);

        Output[index] = distance * sign;
    }
}
