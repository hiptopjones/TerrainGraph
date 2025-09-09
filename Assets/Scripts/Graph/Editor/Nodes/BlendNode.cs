using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Serializable]
internal class BlendNode : Node, IValidatedNode, IEvaluatedNode<float[,]>
{
    private float[,] _cachedOutput;

    // Options
    internal const string NODE_OPTION_PREVIEW_ID = TerrainEditorGraph.NODE_OPTION_PREVIEW_ID;

    // Input
    internal const string NODE_INPUT_GRID1_ID = "grid1_input";
    internal const string NODE_INPUT_GRID2_ID = "grid2_input";
    internal const string NODE_INPUT_METHOD_ID = "method_input";
    internal const string NODE_INPUT_PREVIEW_ID = TerrainEditorGraph.NODE_INPUT_PREVIEW_ID;

    // Output
    internal const string NODE_OUTPUT_GRID_ID = TerrainEditorGraph.NODE_OUTPUT_GRID_ID;

    public override void OnEnable()
    {
        ResetNode();
    }

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<bool>(NODE_OPTION_PREVIEW_ID)
            .WithDisplayName("Enable Preview")
            .WithDefaultValue(false)
            .Build();
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        GetNodeOptionByName(NODE_OPTION_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);

        // Input
        context.AddInputPort<float[,]>(NODE_INPUT_GRID1_ID)
            .WithDisplayName("Grid 1")
            .Build();
        context.AddInputPort<float[,]>(NODE_INPUT_GRID2_ID)
            .WithDisplayName("Grid 2")
            .Build();
        context.AddInputPort<BlendMethod>(NODE_INPUT_METHOD_ID)
            .WithDisplayName("Method")
            .Build();

        if (isPreviewEnabled)
        {
            context.AddInputPort<PreviewImage>(NODE_INPUT_PREVIEW_ID)
                .WithDisplayName("Preview")
                .Build();
        }

        // Output
        context.AddOutputPort<float[,]>(NODE_OUTPUT_GRID_ID)
            .WithDisplayName("Grid")
            .Build();
    }

    public void ValidateNode(GraphLogger graphLogger)
    {
        // TODO
    }

    public void ResetNode()
    {
        _cachedOutput = null;
    }

    public bool TryGetPortValue(IPort outputPort, out float[,] value)
    {
        if (_cachedOutput == null)
        {
            // Only execute on demand
            if (!TryExecuteNode())
            {
                value = null;
                return false;
            }
        }

        value = _cachedOutput;
        return true;
    }

    public bool TryExecuteNode()
    {
        try
        {
            var grid1 = PortEvaluator.EvaluatePort<float[,]>(GetInputPortByName(NODE_INPUT_GRID1_ID));
            if (grid1 == null)
            {
                return false;
            }

            var grid2 = PortEvaluator.EvaluatePort<float[,]>(GetInputPortByName(NODE_INPUT_GRID2_ID));
            if (grid2 == null)
            {
                return false;
            }

            Func<float, float, float> blendFunction = null;

            var method = PortEvaluator.EvaluatePort<BlendMethod>(GetInputPortByName(NODE_INPUT_METHOD_ID));
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

            _cachedOutput = output;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return false;
        }
    }
}