using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

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

    public struct MinimumDistanceInfo
    {
        public SplineSegment segment;
        public float2 position;
        public float2 direction;
        public float distance2;
    }

    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
    public struct SplineSdfJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<SplineSegment> Segments;
        [ReadOnly] public int Size;
        [ReadOnly] public bool IsClosed;

        [WriteOnly] public NativeArray<float> Distances;
        [WriteOnly] public NativeArray<float3> NearestPositions;

        public void Execute(int index)
        {
            int x = index % Size;
            int y = index / Size;

            // Use pixel center
            float2 position = new float2(x + 0.5f, y + 0.5f);

            var info = new MinimumDistanceInfo();
            info.distance2 = float.PositiveInfinity;

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

                if (distance2 < info.distance2)
                {
                    info.distance2 = distance2;
                    info.segment = segment;
                    info.position = q;
                    info.direction = math.normalize(d);
                }
            }

            float distanceSign = 0;

            if (!IsClosed)
            {
                // 2D cross product to get side of line
                // Left is -ve, right is +ve
                float cross = info.segment.ab.x * info.direction.y - info.segment.ab.y * info.direction.x;
                distanceSign = -math.sign(cross);
            }
            else
            {
                distanceSign = IsInsideSegments(position) ? 1 : -1;
            }

            float distance = math.sqrt(info.distance2);

            Distances[index] = distance * distanceSign;
            NearestPositions[index] = new float3(info.position.x, info.segment.height, info.position.y);
        }

        bool IsInsideSegments(float2 position)
        {
            bool inside = false;

            for (int i = 0; i < Segments.Length; i++)
            {
                float2 a = Segments[i].a;
                float2 b = Segments[i].a + Segments[i].ab; // segment endpoint

                // Ensure a.y <= b.y for consistency
                if (a.y > b.y)
                {
                    var temp = a;
                    a = b;
                    b = temp;
                }

                // Does ray from p cross edge [a, b]?
                if (position.y > a.y && position.y <= b.y) // ray intersects y-range
                {
                    float crossX = a.x + (position.y - a.y) * (b.x - a.x) / (b.y - a.y);

                    if (crossX > position.x) // intersection is to the right of p
                    {
                        inside = !inside;
                    }
                }
            }

            return inside;
        }
    }
}
