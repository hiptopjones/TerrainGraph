using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class SpiralVoronoiHeightNode : ExecutableNode<HeightGrid>
    {
        private class InputValues
        {
            public Vector2 Center;
            public int PointCount;
            public float RotationCount;
            public float Squareness;
            public float JitterStrength;
            public int Size;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    HashCode.Combine(Center, PointCount, RotationCount, Squareness, JitterStrength, Size)
                );
            }
        }

        // Options

        // Inputs
        private const string NODE_INPUT_CENTER_ID = "center_input";
        private const string NODE_INPUT_CENTER_TITLE = "Center";

        private const string NODE_INPUT_POINTS_ID = "points_input";
        private const string NODE_INPUT_POINTS_TITLE = "Point Count";

        private const string NODE_INPUT_ROTATIONS_ID = "rotations_input";
        private const string NODE_INPUT_ROTATIONS_TITLE = "Rotation Count";

        private const string NODE_INPUT_SQUARENESS_ID = "squareness_input";
        private const string NODE_INPUT_SQUARENESS_TITLE = "Squareness";

        private const string NODE_INPUT_JITTER_ID = "jitter_input";
        private const string NODE_INPUT_JITTER_TITLE = "Jitter";

        private const string NODE_INPUT_SIZE_ID = "size_input";
        private const string NODE_INPUT_SIZE_TITLE = "Size";

        // Outputs
        private const string NODE_OUTPUT_GRID_ID = "grid_output";
        private const string NODE_OUTPUT_GRID_TITLE = "Grid";

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<bool>(NODE_OPTION_PREVIEW_ID)
                .WithDisplayName(NODE_OPTION_PREVIEW_TITLE)
                .WithDefaultValue(true)
                .Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            GetNodeOptionByName(NODE_OPTION_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);

            // Input
            context.AddInputPort<Vector2>(NODE_INPUT_CENTER_ID)
                .WithDisplayName(NODE_INPUT_CENTER_TITLE)
                .Build();
            context.AddInputPort<int>(NODE_INPUT_POINTS_ID)
                .WithDisplayName(NODE_INPUT_POINTS_TITLE)
                .WithDefaultValue(10)
                .Build();
            context.AddInputPort<float>(NODE_INPUT_ROTATIONS_ID)
                .WithDisplayName(NODE_INPUT_ROTATIONS_TITLE)
                .WithDefaultValue(1.5f)
                .Build();
            context.AddInputPort<float>(NODE_INPUT_SQUARENESS_ID)
                .WithDisplayName(NODE_INPUT_SQUARENESS_TITLE)
                .WithDefaultValue(0f)
                .Build();
            context.AddInputPort<float>(NODE_INPUT_JITTER_ID)
                .WithDisplayName(NODE_INPUT_JITTER_TITLE)
                .WithDefaultValue(2f)
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

            if (input.Center.x < 0 || input.Center.x >= 1 ||
                input.Center.y < 0 || input.Center.y >= 1)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_CENTER_TITLE} value invalid: {input.Center} (valid: 0 <= n < 1)", this);
                isValid = false;
            }

            if (input.PointCount < 2)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_POINTS_TITLE} value invalid: {input.PointCount} (valid: 1 < n)", this);
                isValid = false;
            }

            if (input.RotationCount < 1)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_ROTATIONS_TITLE} value invalid: {input.RotationCount} (valid: 1 <= n)", this);
                isValid = false;
            }

            if (input.Squareness < 0 || input.Squareness > 1)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_SQUARENESS_TITLE} value invalid: {input.Squareness} (valid: 0 <= n <= 1)", this);
                isValid = false;
            }

            if (input.JitterStrength < 0)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_JITTER_TITLE} value invalid: {input.JitterStrength} (valid: 0 <= n)", this);
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
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_CENTER_ID, out temp.Center) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_POINTS_ID, out temp.PointCount) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_ROTATIONS_ID, out temp.RotationCount) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SQUARENESS_ID, out temp.Squareness) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_JITTER_ID, out temp.JitterStrength) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_SIZE_ID, out temp.Size);

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
                var center = inputValues.Center;
                var pointCount = inputValues.PointCount;
                var rotationCount = inputValues.RotationCount;
                var squareness = inputValues.Squareness;
                var jitterStrength = inputValues.JitterStrength;
                var size = inputValues.Size;

                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader($"Shaders/{nameof(SpiralVoronoiHeightNode)}", out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetFloat("_PointCount", pointCount);
                shader.SetFloat("_RotationCount", rotationCount);
                shader.SetFloat("_Squareness", squareness);
                shader.SetFloat("_JitterStrength", jitterStrength);
                shader.SetVector("_Center", center);
                shader.SetInt("_Size", size);

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
