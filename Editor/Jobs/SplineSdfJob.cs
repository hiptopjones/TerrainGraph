using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Indiecat.TerrainGraph.Editor
{
    public struct SplineSegment
    {
        public float2 a;     // segment start
        public float2 b;     // segment end
        public float2 ab;    // (b - a)
        public float ab2;    // dot(ab, ab) (squared length)
        public float height;
    }

    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
    public struct SplineSdfJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<SplineSegment> Segments;
        [ReadOnly] public int Size;

        [WriteOnly] public NativeArray<float> Distances;
        [WriteOnly] public NativeArray<float3> NearestPositions;

        public void Execute(int index)
        {
            int x = index % Size;
            int y = index / Size;

            // Use pixel center
            float2 position = new float2(x + 0.5f, y + 0.5f);

            float minDistance2 = float.PositiveInfinity;
            float distanceSign = 0;
            float height = 0;
            float2 nearestPosition = float2.zero;

            for (int i = 0; i < Segments.Length; i++)
            {
                var segment = Segments[i];

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
                    // Left is -ve, right is +ve
                    float cross = segment.ab.x * d.y - segment.ab.y * d.x;
                    distanceSign = -math.sign(cross);

                    nearestPosition = q;
                    height = segment.height;
                }
            }

            float distance = math.sqrt(minDistance2);

            Distances[index] = distance * distanceSign;
            NearestPositions[index] = new float3(nearestPosition.x, height, nearestPosition.y);
        }
    }
}
