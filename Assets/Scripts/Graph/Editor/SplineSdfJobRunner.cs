using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public static class SplineSdfJobRunner
{
    public static bool TryCreateSdf(Spline spline, int samples, int size, out float[,] sdf)
    {
        NativeArray<SplineSegment> segments = default;
        NativeArray<float> output = default;

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
            segments = new NativeArray<SplineSegment>(segmentCount, Allocator.TempJob);
            for (int i = 0; i < segmentCount; i++)
            {
                int i0 = i;
                int i1 = (i + 1) % vertexCount;

                var a = vertices[i0];
                var b = vertices[i1];

                var ab = b - a;

                segments[i] = new SplineSegment
                {
                    a = a,
                    b = b,
                    ab = ab,
                    ab2 = math.dot(ab, ab)
                };
            }

            // 3) Prepare job
            output = new NativeArray<float>(size * size, Allocator.TempJob);
            var job = new SplineSdfJob
            {
                Segments = segments,
                Size = size,
                Output = output
            };

            // 4) Dispatch
            JobHandle handle = job.Schedule(size * size, 128);
            handle.Complete();

            // 5) Copy data
            sdf = new float[size, size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    sdf[x, y] = output[y * size + x];
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);

            sdf = null;
            return false;
        }
        finally
        {
            // 6) Cleanup
            output.Dispose();
            segments.Dispose();
        }
    }
}
