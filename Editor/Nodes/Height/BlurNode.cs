using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class BlurNode : ExecutableNode<HeightGrid>
    {
        private class InputValues
        {
            public HeightGrid Grid;
            public int Radius;
            public int IterationCount;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(Grid?.VersionHash, Radius, IterationCount);
            }
        }

        // Options

        // Inputs
        private const string NODE_INPUT_GRID_ID = "grid_input";
        private const string NODE_INPUT_GRID_TITLE = "Grid";

        private const string NODE_INPUT_RADIUS_ID = "radius_input";
        private const string NODE_INPUT_RADIUS_TITLE = "Radius";

        private const string NODE_INPUT_ITERATIONS_ID = "iterations_input";
        private const string NODE_INPUT_ITERATIONS_TITLE = "Iterations";

        // Outputs
        private const string NODE_OUTPUT_GRID_ID = "grid_output";
        private const string NODE_OUTPUT_GRID_TITLE = "Grid";

        // Other
        private const int MIN_ITERATION_COUNT = 1;
        private const int MAX_ITERATION_COUNT = 50;
        private const int DEFAULT_ITERATION_COUNT = 1;

        private const int MIN_RADIUS = 1;
        private const int DEFAULT_RADIUS = 5;

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<bool>(NODE_OPTION_PREVIEW_ID)
                .WithDisplayName(NODE_OPTION_PREVIEW_TITLE)
                .WithDefaultValue(true)
                .Build();
            context.AddOption<bool>(NODE_OPTION_DISABLE_ID)
                .WithDisplayName(NODE_OPTION_DISABLE_TITLE)
                .WithDefaultValue(false)
                .Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            GetNodeOptionByName(NODE_OPTION_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);

            // Input
            context.AddInputPort<HeightGrid>(NODE_INPUT_GRID_ID)
                .WithDisplayName(NODE_INPUT_GRID_TITLE)
                .Build();
            context.AddInputPort<int>(NODE_INPUT_RADIUS_ID)
                .WithDisplayName(NODE_INPUT_RADIUS_TITLE)
                .WithDefaultValue(DEFAULT_RADIUS)
                .Build();
            context.AddInputPort<int>(NODE_INPUT_ITERATIONS_ID)
                .WithDisplayName(NODE_INPUT_ITERATIONS_TITLE)
                .WithDefaultValue(DEFAULT_ITERATION_COUNT)
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
            GetNodeOptionByName(NODE_OPTION_DISABLE_ID).TryGetValue(out bool isNodeSkipped);
            if (isNodeSkipped)
            {
                return true;
            }

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

            if (input.Grid == null || !input.Grid.IsValid)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID_TITLE} input missing", this);
                isValid = false;
            }

            if (input.Radius < MIN_RADIUS)
            {
                if (graphLogger != null) graphLogger.LogWarning($"{NODE_INPUT_RADIUS_TITLE} value invalid: {input.Radius} (valid: {MIN_RADIUS} <= n)", this);
                input.Radius = MIN_RADIUS;
            }

            if (input.IterationCount < MIN_ITERATION_COUNT || input.IterationCount > MAX_ITERATION_COUNT)
            {
                if (graphLogger != null) graphLogger.LogWarning($"{NODE_INPUT_ITERATIONS_TITLE} value invalid: {input.IterationCount} (valid: {MIN_ITERATION_COUNT} <= n <= {MAX_ITERATION_COUNT})", this);
                input.IterationCount = Mathf.Clamp(input.IterationCount, MIN_ITERATION_COUNT, MAX_ITERATION_COUNT);
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
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRID_ID, out temp.Grid) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_RADIUS_ID, out temp.Radius) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_ITERATIONS_ID, out temp.IterationCount);

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
            GetNodeOptionByName(NODE_OPTION_DISABLE_ID).TryGetValue(out bool isNodeDisabled);
            if (isNodeDisabled)
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
            ComputeBuffer weightBuffer = null;
            RenderTexture tempTexture1 = null;
            RenderTexture tempTexture2 = null;

            try
            {
                var inputGrid = inputValues.Grid;
                var radius = inputValues.Radius;
                var iterationCount = inputValues.IterationCount;

                var size = inputGrid.Size;

                var inputTexture = inputGrid.RenderTexture;

                var sigma = 3f;
                var weights = GetGaussianWeights(radius, sigma);

                weightBuffer = new ComputeBuffer(weights.Length, sizeof(float));
                weightBuffer.SetData(weights);

                // Create ping-pong textures
                tempTexture1 = TextureHelpers.CreateRenderTexture(size, RenderTextureFormat.RFloat);
                tempTexture2 = TextureHelpers.CreateRenderTexture(size, RenderTextureFormat.RFloat);

                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader($"Shaders/{nameof(BlurNode)}", out var shader))
                {
                    return false;
                }

                var groups = Mathf.CeilToInt(size / 8.0f);

                Graphics.Blit(inputTexture, tempTexture1);

                for (int i = 0; i < iterationCount; i++)
                {
                    var horzKernel = shader.FindKernel("CSMain_Horizontal");

                    shader.SetTexture(horzKernel, "_InTexture", tempTexture1);
                    shader.SetTexture(horzKernel, "_OutTexture", tempTexture2);
                    shader.SetBuffer(horzKernel, "_Weights", weightBuffer);
                    shader.SetFloat("_Radius", radius);
                    shader.SetInt("_Size", size);
                    shader.Dispatch(horzKernel, groups, groups, 1);

                    var vertKernel = shader.FindKernel("CSMain_Vertical");

                    shader.SetTexture(vertKernel, "_InTexture", tempTexture2);
                    shader.SetTexture(vertKernel, "_OutTexture", tempTexture1);
                    shader.SetBuffer(vertKernel, "_Weights", weightBuffer);
                    shader.SetFloat("_Radius", radius);
                    shader.SetInt("_Size", size);
                    shader.Dispatch(vertKernel, groups, groups, 1);
                }

                Graphics.Blit(tempTexture1, outputTexture);

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
            finally
            {
                if (tempTexture1 != null)
                {
                    tempTexture1.Release();
                    tempTexture1 = null;
                }

                if (tempTexture2 != null)
                {
                    tempTexture2.Release();
                    tempTexture2 = null;
                }

                if (weightBuffer != null)
                {
                    weightBuffer.Release();
                    weightBuffer = null;
                }
            }
        }

        private float[] GetGaussianWeights(int radius, float sigma)
        {
            var weights = new float[radius * 2 + 1];

            var sum = 0f;

            for (int i = -radius; i <= radius; i++)
            {
                var w = Mathf.Exp(-(i * i) / (2 * sigma * sigma));
                weights[i + radius] = w;
                sum += w;
            }

            // Normalize so sum = 1
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] /= sum;
            }

            return weights;
        }
    }
}