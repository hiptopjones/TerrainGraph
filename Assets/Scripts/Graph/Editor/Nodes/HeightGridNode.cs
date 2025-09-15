using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Serializable]
public class HeightGridNode : ExecutableNode<HeightGrid>
{
    private class InputValues
    {
        public IProvider Provider;
        public int Size;
        
        public int VersionHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(
                HashCode.Combine(Provider?.VersionHash, Size)
            );
        }
    }

    // Options

    // Inputs
    private const string NODE_INPUT_PROVIDER_ID = "provider_input";
    private const string NODE_INPUT_PROVIDER_TITLE = "Provider";

    private const string NODE_INPUT_SIZE_ID = "size_input";
    private const string NODE_INPUT_SIZE_TITLE = "Size";

    // Outputs
    private const string NODE_OUTPUT_GRID_ID = "grid_output";
    private const string NODE_OUTPUT_GRID_TITLE = "Grid";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<bool>(NODE_OPTION_PREVIEW_ID)
            .WithDisplayName(NODE_OPTION_PREVIEW_TITLE)
            .WithDefaultValue(false)
            .Build();
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        GetNodeOptionByName(NODE_OPTION_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);

        // Input
        context.AddInputPort<IProvider>(NODE_INPUT_PROVIDER_ID)
            .WithDisplayName(NODE_INPUT_PROVIDER_TITLE)
            .Build();
        context.AddInputPort<int>(NODE_INPUT_SIZE_ID)
            .WithDisplayName(NODE_INPUT_SIZE_TITLE)
            .WithDefaultValue(256)
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

        if (input.Provider == null || !input.Provider.IsValid)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_PROVIDER_TITLE} value missing", this);
            isValid = false;
        }
        else if (input.Provider is not IHeightProvider)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_PROVIDER_TITLE} value incorrect", this);
            isValid = false;
        }

        if (input.Size <= 0)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_SIZE_TITLE} value invalid: {input.Size} (valid: 0 < n)", this);
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
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_PROVIDER_ID, out temp.Provider) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SIZE_ID, out temp.Size);

        if (success)
        {
            temp.VersionHash = temp.GetHashCode();

            input = temp;
            return true;
        }

        return false;
    }

    public override bool TryGetOutputValue(IPort _, out HeightGrid grid)
    {
        if (!TryExecuteNode())
        {
            grid = null;
            return false;
        }

        grid = CacheData.Output;
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
            var heightProvider = inputValues.Provider as IHeightProvider;
            var size = inputValues.Size;

            if (!heightProvider.TryGetHeights(size, out var heights))
            {
                return false;
            }

            var outputGrid = new HeightGrid(size);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    outputGrid[x, y] = heights[x, y];
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
}
