using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class CellularNoiseHeightNode : ProviderNode<IProvider>
    {
        private class InputValues
        {
            public Vector2 Offset;
            public int CellSize;
            public float RadiusPercent;
            public int Seed;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    HashCode.Combine(Offset, CellSize, RadiusPercent, Seed)
                );
            }
        }

        // Options

        // Inputs
        private const string NODE_INPUT_OFFSET_ID = "offset_input";
        private const string NODE_INPUT_OFFSET_TITLE = "Offset";

        private const string NODE_INPUT_CELL_SIZE_ID = "cell_size_input";
        private const string NODE_INPUT_CELL_SIZE_TITLE = "Cell Size";

        private const string NODE_INPUT_RADIUS_ID = "radius_input";
        private const string NODE_INPUT_RADIUS_TITLE = "Radius Percent";

        private const string NODE_INPUT_SEED_ID = "seed_input";
        private const string NODE_INPUT_SEED_TITLE = "Seed";

        // Outputs
        private const string NODE_OUTPUT_NOISE_ID = "provider_output";
        private const string NODE_OUTPUT_NOISE_TITLE = "Provider";

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            // N/A
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Input
            context.AddInputPort<Vector2>(NODE_INPUT_OFFSET_ID)
                .WithDisplayName(NODE_INPUT_OFFSET_TITLE)
                .Build();
            context.AddInputPort<int>(NODE_INPUT_CELL_SIZE_ID)
                .WithDisplayName(NODE_INPUT_CELL_SIZE_TITLE)
                .WithDefaultValue(20)
                .Build();
            context.AddInputPort<float>(NODE_INPUT_RADIUS_ID)
                .WithDisplayName(NODE_INPUT_RADIUS_TITLE)
                .WithDefaultValue(0.5f)
                .Build();
            context.AddInputPort<int>(NODE_INPUT_SEED_ID)
                .WithDisplayName(NODE_INPUT_SEED_TITLE)
                .Build();

            // Output
            context.AddOutputPort<IProvider>(NODE_OUTPUT_NOISE_ID)
                .WithDisplayName(NODE_OUTPUT_NOISE_TITLE)
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

            if (input.CellSize <= 0)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_CELL_SIZE_TITLE} value invalid: {input.CellSize} (valid: 0 < n)", this);
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
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_OFFSET_ID, out temp.Offset) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_CELL_SIZE_ID, out temp.CellSize) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_RADIUS_ID, out temp.RadiusPercent) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SEED_ID, out temp.Seed);

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

            value = new CellularNoiseProvider()
            {
                Offset = inputValues.Offset,
                CellSize = inputValues.CellSize,
                RadiusPercent = inputValues.RadiusPercent,
                Seed = inputValues.Seed,

                VersionHash = inputValues.VersionHash
            };

            return true;
        }
    }
}
