using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using static Indiecat.TerrainGraph.Editor.NodeConstants;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class TestNode : ExecutableNode<HeightGrid>
    {
        public enum ArithmeticOperator
        {
            Add = 100,
            Subtract = 200,
            Multiply = 300,
            Divide = 400,
            Minimum = 500,
            Maximum = 600,
            Average = 700,
            Compare = 1000,
            Power = 2000,
        }

        private class OptionValues
        {
            [DisplayName("Operation")]
            [DefaultValue(ArithmeticOperator.Multiply)]
            public ArithmeticOperator ArithmeticOperator { get; set; }

            public bool IsFlipped { get; set; }

            [DefaultValue(true)]
            public bool IsPreviewEnabled { get; set; }
            public bool IsNodeDisabled { get; set; }
            public WarningBanner Warning { get; set; }
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

        // Other
        private const float DEFAULT_VALUE = 0.5f;

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            var customContext = context.UseType<OptionValues>();

            customContext.AddOption(x => x.ArithmeticOperator).Build();
            customContext.AddOption(x => x.IsFlipped).Build();
            customContext.AddOption(x => x.IsPreviewEnabled).Build();
            customContext.AddOption(x => x.IsNodeDisabled).Build();
            customContext.AddOption(x => x.Warning).Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            var options = GetNodeOptions<OptionValues>();

            // Input
            var actions = new List<Action>
            {
                () => context.AddInputPort<HeightGrid>(NODE_INPUT_GRID_ID)
                    .WithDisplayName(NODE_INPUT_GRID_TITLE)
                    .Build(),
                () => context.AddInputPort<float>(NODE_INPUT_VALUE_ID)
                    .WithDisplayName(NODE_INPUT_VALUE_TITLE)
                    .WithDefaultValue(DEFAULT_VALUE)
                    .Build(),
            };

            // All this to avoid duplicating the port definitions
            actions = options.IsFlipped ? actions.AsEnumerable().Reverse().ToList() : actions;
            foreach (var action in actions)
            {
                action.Invoke();
            }

            if (options.IsPreviewEnabled)
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

        private T GetNodeOptions<T>()
        {
            var options = Activator.CreateInstance<T>();

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                var optionName = GetOptionName(property);
                var nodeOption = GetNodeOptionByName(optionName);

                var propertyType = property.PropertyType;

                var method = typeof(INodeOption)
                    .GetMethod(nameof(INodeOption.TryGetValue))
                    .MakeGenericMethod(propertyType);

                var parameters = new object[] { null };

                if (!(bool)method.Invoke(nodeOption, parameters))
                {
                    throw new Exception($"Unable to get option value: {optionName}");
                }

                var propertyValue = Convert.ChangeType(parameters[0], propertyType);

                Debug.Log($"{property.Name} => {propertyValue}");
                property.SetValue(options, propertyValue);
            }

            return options;
        }

        private string GetOptionName(MemberInfo memberInfo)
        {
            return $"{memberInfo.Name}Option";
        }

        public override bool TryValidateNode(GraphLogger graphLogger = null)
        {
            var options = GetNodeOptions<OptionValues>();
            if (options.IsNodeDisabled)
            {
                NodeHelpers.TrySetWarningBanner(this, "DISABLED");
                return true;
            }
            else
            {
                NodeHelpers.TrySetWarningBanner(this, null);
                return TryGetValidatedInputValues(out _, graphLogger);
            }
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

            var options = GetNodeOptions<OptionValues>();

            var temp = new InputValues();
            var success =
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRID_ID, out temp.Grid) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_VALUE_ID, out temp.Value);

            temp.ArithmeticOperator = options.ArithmeticOperator;
            temp.IsFlipped = options.IsFlipped;

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
            var options = GetNodeOptions<OptionValues>();
            if (options.IsNodeDisabled)
            {
                return PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRID_ID, out value);
            }

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

                var keywordBuilder = new KeywordBuilder();
                keywordBuilder.AddKeyword($"OP_{arithmeticOperator.ToString().ToUpper()}");
                keywordBuilder.AddKeyword(isFlipped ? "ARGS_FLIPPED" : "ARGS_NORMAL");

                var inputTexture = inputGrid.RenderTexture;
                var outputTexture = GetOrCreateNodeRenderTexture(size);

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

                var outputGrid = new HeightGrid(size);

                outputGrid.RenderTexture = outputTexture;
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
}