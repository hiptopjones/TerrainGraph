using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public static class SplineSdfJobRunner
{
    public static bool TryCreateSdf(Spline spline, int samples, int size, out float[,] distances, out Vector3[,] nearestPositions)
    {
        distances = null;
        nearestPositions = null;

        NativeArray<SplineSegment> segmentsNative = default;
        NativeArray<float> distancesNative = default;
        NativeArray<float3> nearestPositionsNative = default;

        try
        {
            // 1) Sample spline into a 2D polyline (XZ plane)
            int vertexCount = samples + 1;
            var vertices = new float2[vertexCount];
            var heights = new float[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                float t = (float)i / samples;
                Vector3 positions = SplineUtility.EvaluatePosition(spline, t);
                vertices[i] = new float2(positions.x, positions.z);
                heights[i] = positions.y;
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

                var height = (heights[i0] + heights[i1]) / 2;

                segmentsNative[i] = new SplineSegment
                {
                    a = a,
                    b = b,
                    ab = ab,
                    ab2 = math.dot(ab, ab),
                    height = height,
                };
            }

            // 3) Prepare job
            distancesNative = new NativeArray<float>(size * size, Allocator.TempJob);
            nearestPositionsNative = new NativeArray<float3>(size * size, Allocator.TempJob);
            var job = new SplineSdfJob
            {
                Segments = segmentsNative,
                Size = size,
                Distances = distancesNative,
                NearestPositions = nearestPositionsNative,
            };

            // 4) Dispatch
            JobHandle handle = job.Schedule(size * size, 128);
            handle.Complete();

            // 5) Copy data
            distances = new float[size, size];
            nearestPositions = new Vector3[size, size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    distances[x, y] = distancesNative[y * size + x];
                    nearestPositions[x, y] = nearestPositionsNative[y * size + x];
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);

            return false;
        }
        finally
        {
            // 6) Cleanup
            distancesNative.Dispose();
            nearestPositionsNative.Dispose();
            segmentsNative.Dispose();
        }
    }
}
