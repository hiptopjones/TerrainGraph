using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Serializable]
public class ArithmeticNode : ExecutableNode<HeightGrid>
{
    public enum ArithmeticOperator
    {
        Add = 100,
        Subtract = 200,
        Multiply = 300,
        Divide = 400,
        Minimum = 500,
        Maximum = 600,
        Compare = 1000,
    }

    private class InputValues
    {
        public ArithmeticOperator ArithmeticOperator;
        public bool IsFlipped;
        public HeightGrid Grid;
        public float Value;

        public int VersionHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(ArithmeticOperator, IsFlipped, Grid?.VersionHash, Value);
        }
    }

    // Options
    private const string NODE_OPTION_OPERATOR_ID = "operator_option";
    private const string NODE_OPTION_OPERATOR_TITLE = "Operation";

    private const string NODE_OPTION_FLIP_ID = "flipped_option";
    private const string NODE_OPTION_FLIP_TITLE = "Flip Inputs";

    // Inputs
    private const string NODE_INPUT_GRID_ID = "grid_input";
    private const string NODE_INPUT_GRID_TITLE = "Grid";

    private const string NODE_INPUT_VALUE_ID = "value_input";
    private const string NODE_INPUT_VALUE_TITLE = "Value";

    // Outputs
    private const string NODE_OUTPUT_GRID_ID = "grid_output";
    private const string NODE_OUTPUT_GRID_TITLE = "Grid";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<ArithmeticOperator>(NODE_OPTION_OPERATOR_ID)
            .WithDisplayName(NODE_OPTION_OPERATOR_TITLE)
            .WithDefaultValue(ArithmeticOperator.Add)
            .Build();
        context.AddOption<bool>(NODE_OPTION_FLIP_ID)
            .WithDisplayName(NODE_OPTION_FLIP_TITLE)
            .WithDefaultValue(false)
            .Build();
        context.AddOption<bool>(NODE_OPTION_PREVIEW_ID)
            .WithDisplayName(NODE_OPTION_PREVIEW_TITLE)
            .WithDefaultValue(false)
            .Build();
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        GetNodeOptionByName(NODE_OPTION_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);
        GetNodeOptionByName(NODE_OPTION_FLIP_ID).TryGetValue<bool>(out var isFlipped);

        // Input
        var actions = new List<Action>
        {
            () => context.AddInputPort<HeightGrid>(NODE_INPUT_GRID_ID)
                .WithDisplayName(NODE_INPUT_GRID_TITLE)
                .Build(),
            () => context.AddInputPort<float>(NODE_INPUT_VALUE_ID)
                .WithDisplayName(NODE_INPUT_VALUE_TITLE)
                .WithDefaultValue(0.5f)
                .Build(),
        };

        // All this to avoid duplicating the port definitions
        actions = isFlipped ? actions.AsEnumerable().Reverse().ToList() : actions;
        foreach (var action in actions)
        {
            action.Invoke();
        }

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

        if (!Enum.IsDefined(typeof(ArithmeticOperator), input.ArithmeticOperator))
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_OPTION_OPERATOR_TITLE} option invalid", this);
            isValid = false;
        }

        if (input.Grid == null || !input.Grid.IsValid)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID_TITLE} input missing", this);
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
            GetNodeOptionByName(NODE_OPTION_OPERATOR_ID).TryGetValue(out temp.ArithmeticOperator) &&
            GetNodeOptionByName(NODE_OPTION_FLIP_ID).TryGetValue(out temp.IsFlipped) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRID_ID, out temp.Grid) &&
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_VALUE_ID, out temp.Value);

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

        var startTime = DateTime.Now;
        if (TryExecuteNodeInternal(inputValues))
        {
            CacheData.Output.ExecutionTime = (float)(DateTime.Now - startTime).TotalSeconds;
            return true;
        }

        return false;
    }

    private bool TryExecuteNodeInternal(InputValues inputValues)
    {
        try
        {
            var arithmeticOperator = inputValues.ArithmeticOperator;
            var isFlipped = inputValues.IsFlipped;
            var inputGrid = inputValues.Grid;
            var value = inputValues.Value;

            var size = inputGrid.Size;

            var outputGrid = new HeightGrid(size);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var a = inputGrid[x, y];
                    var b = value;

                    switch (arithmeticOperator)
                    {
                        case ArithmeticOperator.Add:
                            outputGrid[x, y] = isFlipped ? b + a : a + b;
                            break;
                        case ArithmeticOperator.Subtract:
                            outputGrid[x, y] = isFlipped ? b - a : a - b;
                            break;
                        case ArithmeticOperator.Multiply:
                            outputGrid[x, y] = isFlipped ? b * a : a * b;
                            break;
                        case ArithmeticOperator.Divide:
                            outputGrid[x, y] = isFlipped ? b / a : a / b;
                            break;
                        case ArithmeticOperator.Minimum:
                            outputGrid[x, y] = isFlipped ? Mathf.Min(b, a) : Mathf.Min(a, b);
                            break;
                        case ArithmeticOperator.Maximum:
                            outputGrid[x, y] = isFlipped ? Mathf.Max(b, a) : Mathf.Max(a, b);
                            break;
                        case ArithmeticOperator.Compare:
                            outputGrid[x, y] = isFlipped ? Compare(b, a) : Compare(a, b);
                            break;
                        default:
                            // Validation ensures we don't get here
                            break;
                    }
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

    private static float Compare(float a, float b)
    {
        if (a == b)
        {
            return 0;
        }

        return a > b ? -1 : 1.1f; // over 1 so it's green in the preview
    }
}