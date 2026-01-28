using Indiecat.UnityCommon.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;

namespace Indiecat.TerrainGraph.Editor
{
    public static class ShaderWrappers
    {

        public static bool TryGetRange(Texture texture, out float min, out float max)
        {
            try
            {
                const int THREADS_PER_GROUP = 128; // must match shader

                if (!ComputeHelpers.TryLoadComputeShader("Shaders/MinMaxReduction", out var shader))
                {
                    min = 0;
                    max = 0;
                    return false;
                }

                var kernelTexture = shader.FindKernel("ReduceTexture");
                var kernelBuffer = shader.FindKernel("ReduceBuffer");

                var totalPixels = texture.width * texture.height;
                var groups = Mathf.CeilToInt((float)totalPixels / THREADS_PER_GROUP);

                // Buffers to hold intermediate results
                var minBuffer = new ComputeBuffer(groups, sizeof(float));
                var maxBuffer = new ComputeBuffer(groups, sizeof(float));

                // First pass: from texture
                shader.SetTexture(kernelTexture, "_InputTexture", texture);
                shader.SetBuffer(kernelTexture, "_OutputMinValues", minBuffer);
                shader.SetBuffer(kernelTexture, "_OutputMaxValues", maxBuffer);
                shader.Dispatch(kernelTexture, groups, 1, 1);

                // Iterative passes: reduce until down to 1 group
                while (groups > 1)
                {
                    groups = Mathf.CeilToInt((float)groups / THREADS_PER_GROUP);

                    var newMinBuffer = new ComputeBuffer(groups, sizeof(float));
                    var newMaxBuffer = new ComputeBuffer(groups, sizeof(float));

                    var ignoredMinBuffer = new ComputeBuffer(groups, sizeof(float));
                    var ignoredMaxBuffer = new ComputeBuffer(groups, sizeof(float));

                    shader.SetBuffer(kernelBuffer, "_InputValues", minBuffer);
                    shader.SetBuffer(kernelBuffer, "_OutputMinValues", newMinBuffer);
                    shader.SetBuffer(kernelBuffer, "_OutputMaxValues", ignoredMaxBuffer);
                    shader.Dispatch(kernelBuffer, groups, 1, 1);

                    shader.SetBuffer(kernelBuffer, "_InputValues", maxBuffer);
                    shader.SetBuffer(kernelBuffer, "_OutputMinValues", ignoredMinBuffer);
                    shader.SetBuffer(kernelBuffer, "_OutputMaxValues", newMaxBuffer);
                    shader.Dispatch(kernelBuffer, groups, 1, 1);

                    ignoredMinBuffer.Release();
                    ignoredMaxBuffer.Release();

                    minBuffer.Release();
                    maxBuffer.Release();
                    minBuffer = newMinBuffer;
                    maxBuffer = newMaxBuffer;

                }

                // Read back result
                var minArray = new float[1];
                var maxArray = new float[1];
                minBuffer.GetData(minArray); // This blocks on the above having completed
                maxBuffer.GetData(maxArray); // This blocks on the above having completed
                minBuffer.Release();
                maxBuffer.Release();

                min = minArray[0];
                max = maxArray[0];
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);

                min = 0;
                max = 0;
                return false;
            }
        }

        public static bool TryGenerateSdf(Spline spline, int size, int sampleCount, bool isCentered, bool applySplineHeight, ref RenderTexture outputTexture)
        {
            const float SPLINE_HEIGHT_BLEND_DISTANCE = 40f;

            ComputeBuffer pointsBuffer = null;

            try
            {
                var points = SplineHelpers.GetSplineVertices3d(spline, sampleCount);
                if (isCentered)
                {
                    var splineCenter = SplineHelpers.GetCenter(spline).ToVector3XZ();
                    var gridCenter = (Vector2.one * size / 2).ToVector3XZ();

                    points = points.Select(p => p - splineCenter + gridCenter).ToList();
                }

                pointsBuffer = new ComputeBuffer(points.Count, sizeof(float) * 3);
                pointsBuffer.SetData(points);

                if (outputTexture == null)
                {
                    outputTexture = TextureHelpers.CreateRenderTexture(size, RenderTextureFormat.RFloat);
                }

                if (!ComputeHelpers.TryLoadComputeShader($"Shaders/{nameof(SplineHeightNode)}", out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetBuffer(kernel, "_Points", pointsBuffer);
                shader.SetInt("_PointsCount", points.Count);
                shader.SetBool("_Closed", spline.Closed);
                shader.SetBool("_ApplySplineHeight", applySplineHeight);
                shader.SetFloat("_SplineHeightBlendDistance", SPLINE_HEIGHT_BLEND_DISTANCE);
                shader.SetInt("_Size", size);

                var groups = Mathf.CeilToInt(size / 8.0f);
                shader.Dispatch(kernel, groups, groups, 1);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
            finally
            {
                if (pointsBuffer != null)
                {
                    pointsBuffer.Release();
                    pointsBuffer = null;
                }
            }
        }

        private struct Segment
        {
            public Vector2 p1;
            public Vector2 p2;
        }

        public static bool TryGenerateContour(HeightGrid grid, float contourHeight, int contourIndex, int vertexCount, int size, out Spline spline)
        {
            if (!TryGenerateContours(grid, contourHeight, vertexCount, size, out var splines))
            {
                spline = null;
                return false;
            }

            if (splines.Count <= contourIndex)
            {
                Debug.LogError($"Contour index invalid ({splines.Count} contours returned)");

                spline = null;
                return false;
            }

            spline = splines[contourIndex];
            return true;
        }

        public static bool TryGenerateContours(HeightGrid grid, float contourHeight, int vertexCount, int size, out List<Spline> splines)
        {
            ComputeBuffer segmentBuffer = null;
            ComputeBuffer counterBuffer = null;

            try
            {
                var maxSegmentCount = size * size;

                segmentBuffer = new ComputeBuffer(maxSegmentCount, sizeof(float) * 4, ComputeBufferType.Append | ComputeBufferType.Counter);
                counterBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

                var inputTexture = grid.RenderTexture;

                if (!ComputeHelpers.TryLoadComputeShader($"Shaders/{nameof(ContourSplineNode)}", out var shader))
                {
                    splines = null;
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_InTexture", inputTexture);
                shader.SetInt("_Size", size);
                shader.SetFloat("_Height", contourHeight);
                shader.SetBuffer(kernel, "_OutSegments", segmentBuffer);
                shader.SetBuffer(kernel, "_SegmentCount", counterBuffer);

                var groups = Mathf.CeilToInt(size / 8.0f);
                shader.Dispatch(kernel, groups, groups, 1);

                ComputeBuffer.CopyCount(segmentBuffer, counterBuffer, 0);
                var countArray = new int[] { 0 };
                counterBuffer.GetData(countArray);
                var count = countArray[0];

                var segmentArray = new Segment[count];
                segmentBuffer.GetData(segmentArray, 0, 0, count);

                var segments = segmentArray.Select(s => new KeyValuePair<Vector2, Vector2>(s.p1, s.p2)).ToList();

                var contours = ContourDetector.GetContours(segments, contourHeight);
                if (contours == null || !contours.Any())
                {
                    Debug.LogError("Contours not detected");

                    splines = null;
                    return false;
                }

                var simplifiedContours = SplineHelpers.CreateSplines(contours, vertexCount);

                splines = simplifiedContours;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);

                splines = null;
                return false;
            }
            finally
            {
                if (segmentBuffer != null)
                {
                    segmentBuffer.Release();
                    segmentBuffer = null;
                }

                if (counterBuffer != null)
                {
                    counterBuffer.Release();
                    counterBuffer = null;
                }
            }
        }

        public static bool TryGrow(RenderTexture inputTexture, int radius, int size, ref RenderTexture outputTexture)
        {
            try
            {
                if (outputTexture == null)
                {
                    outputTexture = TextureHelpers.CreateRenderTexture(size, RenderTextureFormat.RFloat);
                }

                if (!ComputeHelpers.TryLoadComputeShader($"Shaders/{nameof(GrowNode)}", out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_InTexture", inputTexture);
                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetInt("_Radius", radius);
                shader.SetInt("_Size", size);

                var groups = Mathf.CeilToInt(size / 8.0f);
                shader.Dispatch(kernel, groups, groups, 1);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }

        public static bool TryArithmetic(
            RenderTexture inputTexture,
            float value,
            ArithmeticNode.ArithmeticOperator arithmeticOperator,
            bool isZeroIgnored,
            bool isFlipped,
            int size,
            ref RenderTexture outputTexture)
        {
            if (outputTexture == null)
            {
                outputTexture = TextureHelpers.CreateRenderTexture(size, RenderTextureFormat.RFloat);
            }

            var keywordBuilder = new KeywordBuilder();
            keywordBuilder.AddKeyword($"OP_{arithmeticOperator.ToString().ToUpper()}");
            keywordBuilder.AddKeyword(isFlipped ? "ARGS_FLIPPED" : "ARGS_NORMAL");
            keywordBuilder.AddKeyword(isZeroIgnored ? "ZERO_EXCLUDE" : "ZERO_INCLUDE");

            if (!ComputeHelpers.TryLoadComputeShader($"Shaders/{nameof(ArithmeticNode)}", out var shader))
            {
                return false;
            }

            var kernel = shader.FindKernel("CSMain");

            shader.SetTexture(kernel, "_InTexture", inputTexture);
            shader.SetTexture(kernel, "_OutTexture", outputTexture);
            shader.SetFloat("_Value", value);

            shader.shaderKeywords = keywordBuilder.GetKeywords();

            var groups = Mathf.CeilToInt(size / 8.0f);
            shader.Dispatch(kernel, groups, groups, 1);

            return true;
        }
    }
}
