using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Serializable]
public class ShapeMaskNode : ExecutableNode<HeightGrid>
{
    private class InputValues
    {
        public ShapeType ShapeType;
        public float Radius;
        public int Size;

        public int VersionHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(ShapeType, Radius, Size);
        }
    }

    private enum ShapeType
    {
        Cone = 100,
        Cylinder = 200,
        Gaussian = 300,
    }

    // Options
    private const string NODE_OPTION_TYPE_ID = "type_option";
    private const string NODE_OPTION_TYPE_TITLE = "Shape Type";

    // Inputs
    private const string NODE_INPUT_SIZE_ID = "size_input";
    private const string NODE_INPUT_SIZE_TITLE = "Size";

    private const string NODE_INPUT_RADIUS_ID = "radius_input";
    private const string NODE_INPUT_RADIUS_TITLE = "Radius";

    // Outputs
    private const string NODE_OUTPUT_GRID_ID = "grid_output";
    private const string NODE_OUTPUT_GRID_TITLE = "Grid";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<ShapeType>(NODE_OPTION_TYPE_ID)
            .WithDisplayName(NODE_OPTION_TYPE_TITLE)
            .WithDefaultValue(ShapeType.Cone)
            .Build();
        context.AddOption<bool>(NODE_OPTION_PREVIEW_ID)
            .WithDisplayName(NODE_OPTION_PREVIEW_TITLE)
            .WithDefaultValue(false)
            .Build();
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        GetNodeOptionByName(NODE_OPTION_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);

        // Input
        context.AddInputPort<int>(NODE_INPUT_SIZE_ID)
            .WithDisplayName(NODE_INPUT_SIZE_TITLE)
            .WithDefaultValue(256)
            .Build();
        context.AddInputPort<float>(NODE_INPUT_RADIUS_ID)
            .WithDisplayName(NODE_INPUT_RADIUS_TITLE)
            .WithDefaultValue(0.5f)
            .Build();

        if (isPreviewEnabled)
        {
            context.AddInputPort<PreviewImage>(NODE_INPUT_PREVIEW_ID)
                .WithDisplayName(NODE_INPUT_PREVIEW_TITLE)
                .Build();
        }

        // Output
        context.AddOutputPort<HeightGrid>(NODE_OUTPUT_GRID_ID)
            .WithDisplayName(NODE_OUTPUT_GRID_TITLE)
            .Build();
    }

    public override bool TryValidateNode(GraphLogger graphLogger = null)
    {
        return TryGetValidatedInputValues(out _, graphLogger);
    }

    private bool TryGetValidatedInputValues(out InputValues validatedInput, GraphLogger graphLogger = null)
    {
        validatedInput = null;

        if (!TryGetInputValues(out var input))
        {
            if (graphLogger != null) graphLogger.LogError("Upstream failure", this);
            return false;
        }

        var isValid = true;

        if (!Enum.IsDefined(typeof(ShapeType), input.ShapeType))
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_OPTION_TYPE_TITLE} option invalid", this);
            isValid = false;
        }

        if (input.Size <= 0)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_SIZE_TITLE} value invalid: {input.Size} (valid: 0 < n)", this);
            isValid = false;
        }

        if (input.Radius <= 0)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_RADIUS_TITLE} value invalid: {input.Radius} (valid: 0 < n)", this);
            isValid = false;
        }

        if (isValid)
        {
            validatedInput = input;
        }

        return isValid;
    }

    private bool TryGetInputValues(out InputValues input)
    {
        input = null;

        var temp = new InputValues();
        var success =
            GetNodeOptionByName(NODE_OPTION_TYPE_ID).TryGetValue(out temp.ShapeType) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SIZE_ID, out temp.Size) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_RADIUS_ID, out temp.Radius);

        if (success)
        {
            temp.VersionHash = temp.GetHashCode();

            input = temp;
            return true;
        }

        return false;
    }

    public override bool TryGetOutputValue(IPort _, out HeightGrid value)
    {
        if (!TryExecuteNode())
        {
            value = null;
            return false;
        }

        value = CacheData.Output;
        return true;
    }

    public override bool TryExecuteNode()
    {
        if (!TryGetValidatedInputValues(out var inputValues))
        {
            // Not in valid state
            CacheData.Output = null;
            return false;
        }

        if (CacheData.Output != null && CacheData.Output.VersionHash == inputValues.VersionHash)
        {
            // Node is already up-to-date
            return true;
        }

        // Clear the cached values in case there's an early exit below
        CacheData.Output = null;

        try
        {
            var shapeFunction = GetShapeFunction(inputValues);

            var size = inputValues.Size;
            var radius = inputValues.Radius * size;

            var center = Vector2.one * size / 2f;

            var outputGrid = new HeightGrid(size);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var position = new Vector2(x, y) - center;
                    outputGrid[x, y] = shapeFunction(position, radius);
                }
            }

            outputGrid.VersionHash = inputValues.VersionHash;

            CacheData.Output = outputGrid;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return false;
        }
    }

    private Func<Vector2, float, float> GetShapeFunction(InputValues inputValues)
    {
        switch (inputValues.ShapeType)
        {
            case ShapeType.Cone:
                return ShapeFunctions.Cone;
            case ShapeType.Cylinder:
                return ShapeFunctions.Cylinder;
            case ShapeType.Gaussian:
                return ShapeFunctions.Gaussian;
            default:
                Debug.LogError($"Unhandled shape type: {inputValues.ShapeType}");
                return ShapeFunctions.Invalid;
        }
    }
}