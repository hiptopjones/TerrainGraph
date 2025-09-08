using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Serializable]
internal class BlendNode : Node, IValidatedNode, IEvaluatedNode<float[,]>
{
    private float[,] _grid;

    // Options
    internal const string OPTION_ENABLE_PREVIEW_ID = "enable_preview";

    // Input
    internal const string INPUT_PORT_GRID1_ID = "grid1";
    internal const string INPUT_PORT_GRID2_ID = "grid2";
    internal const string INPUT_PORT_METHOD_ID = "method";
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
        context.AddInputPort<float[,]>(INPUT_PORT_GRID1_ID)
            .WithDisplayName("Grid 1")
            .Build();
        context.AddInputPort<float[,]>(INPUT_PORT_GRID2_ID)
            .WithDisplayName("Grid 2")
            .Build();
        context.AddInputPort<BlendMethod>(INPUT_PORT_METHOD_ID)
            .WithDisplayName("Method")
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

    public void ResetNode()
    {
        _grid = null;
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

    public bool TryExecuteNode()
    {
        try
        {
            var grid1 = PortEvaluator.EvaluatePort<float[,]>(GetInputPortByName(INPUT_PORT_GRID1_ID));
            if (grid1 == null)
            {
                return false;
            }

            var grid2 = PortEvaluator.EvaluatePort<float[,]>(GetInputPortByName(INPUT_PORT_GRID2_ID));
            if (grid2 == null)
            {
                return false;
            }

            Func<float, float, float> blendFunction = null;

            var method = PortEvaluator.EvaluatePort<BlendMethod>(GetInputPortByName(INPUT_PORT_METHOD_ID));
            switch (method)
            {
                case BlendMethod.Add:
                    blendFunction = (a, b) => a + b;
                    break;

                case BlendMethod.Multiply:
                    blendFunction = (a, b) => a * b;
                    break;

                default:
                    Debug.Log($"Unhandled blend method: {method}");
                    break;
            }

            // TODO: Validate all lengths are expected
            var size = grid1.GetLength(0);

            var output = new float[size, size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    output[x, y] = blendFunction(grid1[x, y], grid2[x, y]);
                }
            }

            _grid = output;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return false;
        }
    }
}