using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class NormalizeNode : ExecutableNode<HeightGrid>
    {
        private class InputValues
        {
            public HeightGrid Grid;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(Grid?.VersionHash);
            }
        }

        // Options

        // Inputs
        private const string NODE_INPUT_GRID_ID = "grid_input";
        private const string NODE_INPUT_GRID_TITLE = "Grid";

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
            context.AddInputPort<HeightGrid>(NODE_INPUT_GRID_ID)
                .WithDisplayName(NODE_INPUT_GRID_TITLE)
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

            if (input.Grid == null || !input.Grid.IsValid)
            {
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID_TITLE} value missing", this);
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
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRID_ID, out temp.Grid);

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
                var inputGrid = inputValues.Grid;

                var size = inputGrid.Size;

                var inputTexture = inputGrid.RenderTexture;

                if (!TryGetMinMax(inputTexture, out var minValue, out var maxValue))
                {
                    return false;
                }

                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader("Shaders/NormalizeNode", out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                shader.SetTexture(kernel, "_InTexture", inputTexture);
                shader.SetTexture(kernel, "_OutTexture", outputTexture);
                shader.SetFloat("_MinValue", minValue);
                shader.SetFloat("_MaxValue", maxValue);

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

        public static bool TryGetMinMax(Texture texture, out float min, out float max)
        {
            try
            {
                const int THREADS_PER_GROUP = 128; // must match shader

                if (!ComputeHelpers.TryLoadComputeShader("Shaders/MinMaxReduction", out var shader))
                {
                    min = 0;
                    max = 0;
                    return false;
                }

                var kernelTexture = shader.FindKernel("ReduceTexture");
                var kernelBuffer = shader.FindKernel("ReduceBuffer");

                var totalPixels = texture.width * texture.height;
                var groups = Mathf.CeilToInt((float)totalPixels / THREADS_PER_GROUP);

                // Buffers to hold intermediate results
                var minBuffer = new ComputeBuffer(groups, sizeof(float));
                var maxBuffer = new ComputeBuffer(groups, sizeof(float));

                // First pass: from texture
                shader.SetTexture(kernelTexture, "_InputTexture", texture);
                shader.SetBuffer(kernelTexture, "_OutputMinValues", minBuffer);
                shader.SetBuffer(kernelTexture, "_OutputMaxValues", maxBuffer);
                shader.Dispatch(kernelTexture, groups, 1, 1);

                // Iterative passes: reduce until down to 1 group
                while (groups > 1)
                {
                    groups = Mathf.CeilToInt((float)groups / THREADS_PER_GROUP);

                    var newMinBuffer = new ComputeBuffer(groups, sizeof(float));
                    var newMaxBuffer = new ComputeBuffer(groups, sizeof(float));

                    var ignoredMinBuffer = new ComputeBuffer(groups, sizeof(float));
                    var ignoredMaxBuffer = new ComputeBuffer(groups, sizeof(float));

                    shader.SetBuffer(kernelBuffer, "_InputValues", minBuffer);
                    shader.SetBuffer(kernelBuffer, "_OutputMinValues", newMinBuffer);
                    shader.SetBuffer(kernelBuffer, "_OutputMaxValues", ignoredMaxBuffer);
                    shader.Dispatch(kernelBuffer, groups, 1, 1);

                    shader.SetBuffer(kernelBuffer, "_InputValues", maxBuffer);
                    shader.SetBuffer(kernelBuffer, "_OutputMinValues", ignoredMinBuffer);
                    shader.SetBuffer(kernelBuffer, "_OutputMaxValues", newMaxBuffer);
                    shader.Dispatch(kernelBuffer, groups, 1, 1);

                    ignoredMinBuffer.Release();
                    ignoredMaxBuffer.Release();

                    minBuffer.Release();
                    maxBuffer.Release();
                    minBuffer = newMinBuffer;
                    maxBuffer = newMaxBuffer;

                }

                // Read back result
                var minArray = new float[1];
                var maxArray = new float[1];
                minBuffer.GetData(minArray); // This blocks on the above having completed
                maxBuffer.GetData(maxArray); // This blocks on the above having completed
                minBuffer.Release();
                maxBuffer.Release();

                min = minArray[0];
                max = maxArray[0];
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);

                min = 0;
                max = 0;
                return false;
            }
        }
    }
}