using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public static class SplineSdfJobRunner
{
    public static bool TryCreateSdf(Spline spline, int samples, int size, out float[,] distances, out Vector2[,] nearestPositions)
    {
        NativeArray<SplineSegment> segmentsNative = default;
        NativeArray<float> heightsNative = default;
        NativeArray<float2> nearestPositionsNative = default;

        try
        {
            // 1) Sample spline into a 2D polyline (XZ plane)
            int vertexCount = samples + 1;
            var vertices = new float2[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                float t = (float)i / samples;
                Vector3 positions = SplineUtility.EvaluatePosition(spline, t);
                vertices[i] = new float2(positions.x, positions.z);
            }

            // 2) Build segments
            int segmentCount = spline.Closed ? vertexCount : vertexCount - 1;
            segmentsNative = new NativeArray<SplineSegment>(segmentCount, Allocator.TempJob);
            for (int i = 0; i < segmentCount; i++)
            {
                int i0 = i;
                int i1 = (i + 1) % vertexCount;

                var a = vertices[i0];
                var b = vertices[i1];

                var ab = b - a;

                segmentsNative[i] = new SplineSegment
                {
                    a = a,
                    b = b,
                    ab = ab,
                    ab2 = math.dot(ab, ab)
                };
            }

            // 3) Prepare job
            heightsNative = new NativeArray<float>(size * size, Allocator.TempJob);
            nearestPositionsNative = new NativeArray<float2>(size * size, Allocator.TempJob);
            var job = new SplineSdfJob
            {
                Segments = segmentsNative,
                Size = size,
                Heights = heightsNative,
                NearestPositions = nearestPositionsNative
            };

            // 4) Dispatch
            JobHandle handle = job.Schedule(size * size, 128);
            handle.Complete();

            // 5) Copy data
            distances = new float[size, size];
            nearestPositions = new Vector2[size, size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    distances[x, y] = heightsNative[y * size + x];
                    nearestPositions[x, y] = nearestPositionsNative[y * size + x];
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);

            distances = null;
            nearestPositions = null;
            return false;
        }
        finally
        {
            // 6) Cleanup
            heightsNative.Dispose();
            nearestPositionsNative.Dispose();
            segmentsNative.Dispose();
        }
    }
}
