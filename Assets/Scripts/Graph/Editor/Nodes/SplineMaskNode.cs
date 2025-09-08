using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Splines;

[Serializable]
internal class SplineMaskNode : Node, IValidatedNode, IEvaluatedNode<float[,]>
{
    private float[,] _grid;

    // Options
    internal const string OPTION_ENABLE_PREVIEW_ID = "enable_preview";

    // Input
    internal const string INPUT_PORT_SPLINE_ID = "spline";
    internal const string INPUT_PORT_SIZE_ID = "size";
    internal const string INPUT_PORT_STEP_ID = "step";
    internal const string INPUT_PORT_PREVIEW_ID = "preview";

    // Output
    internal const string OUTPUT_PORT_GRID_ID = "grid";

    public override void OnEnable()
    {
        ResetNode();
    }

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<bool>(OPTION_ENABLE_PREVIEW_ID)
            .WithDisplayName("Enable Preview")
            .WithDefaultValue(false)
            .Build();
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        GetNodeOptionByName(OPTION_ENABLE_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);

        // Input
        context.AddInputPort<SplineWrapper>(INPUT_PORT_SPLINE_ID)
            .WithDisplayName("Spline")
            .Build();
        context.AddInputPort<int>(INPUT_PORT_SIZE_ID)
            .WithDisplayName("Size")
            .WithDefaultValue(256)
            .Build();
        context.AddInputPort<float>(INPUT_PORT_STEP_ID)
            .WithDisplayName("Step")
            .WithDefaultValue(10)
            .Build();

        if (isPreviewEnabled)
        {
            context.AddInputPort<PreviewImage>(INPUT_PORT_PREVIEW_ID)
                .WithDisplayName("Preview")
                .Build();
        }

        // Output
        context.AddOutputPort<float[,]>(OUTPUT_PORT_GRID_ID)
            .WithDisplayName("Grid")
            .Build();
    }

    public void ValidateNode(GraphLogger graphLogger)
    {
        // TODO
    }

    public bool TryGetPortValue(IPort outputPort, out float[,] value)
    {
        if (_grid == null)
        {
            // Only execute on demand
            if (!TryExecuteNode())
            {
                value = null;
                return false;
            }
        }

        value = _grid;
        return true;
    }

    public void ResetNode()
    {
        _grid = null;
    }

    private bool TryExecuteNode()
    {
        try
        {
            var splineWrapper = PortEvaluator.EvaluatePort<SplineWrapper>(GetInputPortByName(INPUT_PORT_SPLINE_ID));
            if (splineWrapper == null)
            {
                return false;
            }

            var size = PortEvaluator.EvaluatePort<int>(GetInputPortByName(INPUT_PORT_SIZE_ID));
            if (size <= 0)
            {
                return false;
            }

            var step = PortEvaluator.EvaluatePort<float>(GetInputPortByName(INPUT_PORT_STEP_ID));
            if (step <= 0)
            {
                // Can cause infinite loop
                return false;
            }

            var spline = splineWrapper.Spline;

            var heights = new float[size, size];

            var vertices = GetSplineVertices(spline, step);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    heights[x, y] = 0;

                    if (IsPointInPolygon(new Vector3(x, 0, y), vertices, performSanityCheck: true))
                    {
                        heights[x, y] = 1;
                    }
                }
            }

            _grid = heights;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return false;
        }
    }

    public static Vector3[] GetSplineVertices(Spline spline, float distanceStep)
    {
        List<Vector3> pathVertices = new List<Vector3>();

        float t = 0;
        while (t < 1)
        {
            pathVertices.Add(spline.EvaluatePosition(t));

            // Find the next time based on a step distance
            SplineUtility.GetPointAtLinearDistance(spline, t, distanceStep, out t);
        }

        // Add the last position, as we didn't evaluate it above
        pathVertices.Add(spline.EvaluatePosition(1));

        return pathVertices.ToArray();
    }

    // https://stackoverflow.com/questions/217578/how-can-i-determine-whether-a-2d-point-is-within-a-polygon
    public static bool IsPointInPolygon(Vector3 p, Vector3[] polygon, bool performSanityCheck = true)
    {
        if (!performSanityCheck)
        {
            float minX = polygon[0].x;
            float maxX = polygon[0].x;
            float minZ = polygon[0].z;
            float maxZ = polygon[0].z;
            for (int i = 1; i < polygon.Length; i++)
            {
                Vector3 q = polygon[i];
                minX = Mathf.Min(q.x, minX);
                maxX = Mathf.Max(q.x, maxX);
                minZ = Mathf.Min(q.z, minZ);
                maxZ = Mathf.Max(q.z, maxZ);
            }

            if (p.x < minX || p.x > maxX || p.z < minZ || p.z > maxZ)
            {
                return false;
            }
        }

        // https://wrf.ecse.rpi.edu/Research/Short_Notes/pnpoly.html
        bool isInside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            if ((polygon[i].z > p.z) != (polygon[j].z > p.z) &&
                 p.x < (polygon[j].x - polygon[i].x) * (p.z - polygon[i].z) / (polygon[j].z - polygon[i].z) + polygon[i].x)
            {
                isInside = !isInside;
            }
        }

        return isInside;
    }
}