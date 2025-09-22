using System.Collections.Generic;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [ExecuteInEditMode]
    public class ContourVisualization : MonoBehaviour
    {
        private int _size;
        private float[,] _values;
        private float _level;

        private Dictionary<Vector2, SampleResult> _sampleResults;
        private Dictionary<Vector2, SegmentResult> _segmentResults;

        private class SampleResult
        {
            public int x;
            public int y;
            public float value;
            public byte sample;
        }

        private class SegmentResult
        {
            public int x;
            public int y;
            public Vector2 p1;
            public Vector2 p2;
        }

        public void InstallHandlers(ContourDetector detector)
        {
            Debug.Log("Install Handlers");

            Reset();

            detector.DetectionStart = OnDetectionStarted;
            detector.SampleResult = OnSampleResult;
            detector.SegmentResult = OnSegmentResult;
        }

        private void Reset()
        {
            _size = 0;
            _values = null;
            _level = 0;
            _sampleResults = null;
            _segmentResults = null;
        }

        private void OnDetectionStarted(float[,] values, float level)
        {
            Debug.Log("Detection Started");

            _size = values.GetLength(0);

            _values = values;
            _level = level;

            _sampleResults = new Dictionary<Vector2, SampleResult>(_size * _size);
            _segmentResults = new Dictionary<Vector2, SegmentResult>(_size * _size);
        }

        private void OnSampleResult(int x, int y, float value, byte sample)
        {
            var sampleResult = new SampleResult
            {
                x = x,
                y = y,
                value = value,
                sample = sample,
            };

            var position = new Vector2(x, y);
            _sampleResults[position] = sampleResult;
        }

        private void OnSegmentResult(int x, int y, Vector2 p1, Vector2 p2)
        {
            var segmentResult = new SegmentResult
            {
                x = x,
                y = y,
                p1 = p1,
                p2 = p2,
            };

            var position = new Vector2(x, y);
            _segmentResults[position] = segmentResult;
        }

        private void OnDrawGizmos()
        {
            const float CUBE_STRIDE = 1f;

            if (_values == null)
            {
                return;
            }

            for (int y = 0; y < _size; y++)
            {
                for (int x = 0; x < _size; x++)
                {
                    var position = new Vector2(x, y);

                    var height = _values[x, y];

                    if (_sampleResults.TryGetValue(position, out var sampleResult))
                    {
                        Gizmos.color = sampleResult.sample == 1 ? Color.green : Color.red;
                    }
                    else
                    {
                        if (Mathf.Abs(height - _level) < 1)
                        {
                            Gizmos.color = Color.black;
                        }
                        else
                        {
                            Gizmos.color = Color.gray;
                        }
                    }

                    Gizmos.DrawWireCube(transform.position + new Vector3((x + 0.5f) * CUBE_STRIDE, height, (y + 0.5f) * CUBE_STRIDE), Vector3.one);

                    if (_segmentResults.TryGetValue(position, out var segmentResult))
                    {
                        var p1 = transform.position + new Vector3(segmentResult.p1.x * CUBE_STRIDE, height + 1, segmentResult.p1.y * CUBE_STRIDE);
                        var p2 = transform.position + new Vector3(segmentResult.p2.x * CUBE_STRIDE, height + 1, segmentResult.p2.y * CUBE_STRIDE);

                        Gizmos.color = Color.white;
                        DrawArrow(p1, p2);
                    }
                }
            }
        }

        public static void DrawArrow(Vector3 from, Vector3 to, float headLength = 0.3f, float headAngle = 40.0f)
        {
            Gizmos.DrawLine(from, to);

            Vector3 direction = (to - from).normalized;
            Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + headAngle, 0) * Vector3.forward;
            Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - headAngle, 0) * Vector3.forward;

            Gizmos.DrawLine(to, to + right * headLength);
            Gizmos.DrawLine(to, to + left * headLength);
        }
    }
}
