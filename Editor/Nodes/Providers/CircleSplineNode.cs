using System;
using Unity.GraphToolkit.Editor;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class CircleSplineNode : ProviderNode<IProvider>
    {
        private class InputValues
        {
            public int Size;
            public float Angle;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(Size, Angle);
            }
        }

        // Options

        // Inputs
        private const string NODE_INPUT_SIZE_ID = "size_input";
        private const string NODE_INPUT_SIZE_TITLE = "Size";

        private const string NODE_INPUT_ANGLE_ID = "angle_input";
        private const string NODE_INPUT_ANGLE_TITLE = "Angle";

        // Outputs
        private const string NODE_OUTPUT_PROVIDER_ID = "provider_output";
        private const string NODE_OUTPUT_PROVIDER_TITLE = "Provider";

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            // N/A
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Input
            context.AddInputPort<int>(NODE_INPUT_SIZE_ID)
                .WithDisplayName(NODE_INPUT_SIZE_TITLE)
                .WithDefaultValue(256)
                .Build();
            context.AddInputPort<float>(NODE_INPUT_ANGLE_ID)
                .WithDisplayName(NODE_INPUT_ANGLE_TITLE)
                .WithDefaultValue(360)
                .Build();

            // Output
            context.AddOutputPort<IProvider>(NODE_OUTPUT_PROVIDER_ID)
                .WithDisplayName(NODE_OUTPUT_PROVIDER_TITLE)
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

            if (input.Size <= 0)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_SIZE_TITLE} value invalid: {input.Size} (valid: 0 < n)", this);
                isValid = false;
            }

            if (input.Angle <= 0 || input.Angle > 360)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_ANGLE_TITLE} value invalid: {input.Angle} (valid: 0 < n <= 360)", this);
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
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SIZE_ID, out temp.Size) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_ANGLE_ID, out temp.Angle);

            if (success)
            {
                temp.VersionHash = temp.GetHashCode();

                input = temp;
                return true;
            }

            return false;
        }

        public override bool TryGetOutputValue(IPort _, out IProvider value)
        {
            if (!TryGetValidatedInputValues(out var inputValues))
            {
                value = null;
                return false;
            }

            value = new CircleSplineProvider()
            {
                Size = inputValues.Size,
                Angle = inputValues.Angle,
            
                VersionHash = inputValues.VersionHash
            };

            return true;
        }
    }
}