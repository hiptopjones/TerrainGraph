using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class ErosionNode : ExecutableNode<HeightGrid>
    {
        private class InputValues
        {
            public HeightGrid Grid;
            public int Iterations;
            public float Rain;
            public float Evaporation;
            public float Capacity;
            public float Erosion;
            public float Deposition;
            public float Gravity;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    Grid?.VersionHash, Iterations, Rain, Evaporation,
                    Capacity, Erosion, Deposition, Gravity);
            }
        }

        // Options

        // Inputs
        private const string NODE_INPUT_GRID_ID = "grid_input";
        private const string NODE_INPUT_GRID_TITLE = "Grid";

        private const string NODE_INPUT_ITERATIONS_ID = "iterations_input";
        private const string NODE_INPUT_ITERATIONS_TITLE = "Iterations";

        private const string NODE_INPUT_RAIN_ID = "rain_input";
        private const string NODE_INPUT_RAIN_TITLE = "Rain";

        private const string NODE_INPUT_EVAPORATION_ID = "evaporation_input";
        private const string NODE_INPUT_EVAPORATION_TITLE = "Evaporation";

        private const string NODE_INPUT_CAPACITY_ID = "capacity_input";
        private const string NODE_INPUT_CAPACITY_TITLE = "Capacity";

        private const string NODE_INPUT_EROSION_ID = "erosion_input";
        private const string NODE_INPUT_EROSION_TITLE = "Erosion";

        private const string NODE_INPUT_DEPOSITION_ID = "deposition_input";
        private const string NODE_INPUT_DEPOSITION_TITLE = "Deposition";

        private const string NODE_INPUT_GRAVITY_ID = "gravity_input";
        private const string NODE_INPUT_GRAVITY_TITLE = "Gravity";

        // Outputs
        private const string NODE_OUTPUT_GRID_ID = "grid_output";
        private const string NODE_OUTPUT_GRID_TITLE = "Grid";

        // Other
        private const int MIN_ITERATIONS = 1;
        private const int DEFAULT_ITERATIONS = 100;

        private const float MIN_RAIN = 0.0001f;
        private const float DEFAULT_RAIN = 0.2f;

        private const float MIN_EVAPORATION = 0f;
        private const float DEFAULT_EVAPORATION = 0f;

        private const float MIN_CAPACITY = 0.0001f;
        private const float DEFAULT_CAPACITY = 2;

        private const float MIN_EROSION = 0.0001f;
        private const float DEFAULT_EROSION = 0.75f;

        private const float MIN_DEPOSITION = 0.0001f;
        private const float DEFAULT_DEPOSITION = 3f;

        private const float MIN_GRAVITY = 0.0001f;
        private const float DEFAULT_GRAVITY = 9f;

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
            context.AddInputPort<HeightGrid>(NODE_INPUT_GRID_ID)
                .WithDisplayName(NODE_INPUT_GRID_TITLE)
                .Build();
            context.AddInputPort<int>(NODE_INPUT_ITERATIONS_ID)
                .WithDisplayName(NODE_INPUT_ITERATIONS_TITLE)
                .WithDefaultValue(DEFAULT_ITERATIONS)
                .Build();
            context.AddInputPort<float>(NODE_INPUT_RAIN_ID)
                .WithDisplayName(NODE_INPUT_RAIN_TITLE)
                .WithDefaultValue(DEFAULT_RAIN)
                .Build();
            context.AddInputPort<float>(NODE_INPUT_EVAPORATION_ID)
                .WithDisplayName(NODE_INPUT_EVAPORATION_TITLE)
                .WithDefaultValue(DEFAULT_EVAPORATION)
                .Build();
            context.AddInputPort<float>(NODE_INPUT_CAPACITY_ID)
                .WithDisplayName(NODE_INPUT_CAPACITY_TITLE)
                .WithDefaultValue(DEFAULT_CAPACITY)
                .Build();
            context.AddInputPort<float>(NODE_INPUT_EROSION_ID)
                .WithDisplayName(NODE_INPUT_EROSION_TITLE)
                .WithDefaultValue(DEFAULT_EROSION)
                .Build();
            context.AddInputPort<float>(NODE_INPUT_DEPOSITION_ID)
                .WithDisplayName(NODE_INPUT_DEPOSITION_TITLE)
                .WithDefaultValue(DEFAULT_DEPOSITION)
                .Build();
            context.AddInputPort<float>(NODE_INPUT_GRAVITY_ID)
                .WithDisplayName(NODE_INPUT_GRAVITY_TITLE)
                .WithDefaultValue(DEFAULT_GRAVITY)
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

            if (input.Iterations < MIN_ITERATIONS)
            {
                if (graphLogger != null) graphLogger.LogWarning($"{NODE_INPUT_ITERATIONS_TITLE} value invalid: {input.Iterations} (valid: {MIN_ITERATIONS} <= n)", this);
                input.Iterations = MIN_ITERATIONS;
            }

            if (input.Rain < MIN_RAIN)
            {
                if (graphLogger != null) graphLogger.LogWarning($"{NODE_INPUT_RAIN_TITLE} value invalid: {input.Rain} (valid: {MIN_RAIN} <= n)", this);
                input.Rain = MIN_RAIN;
            }

            if (input.Evaporation < MIN_EVAPORATION)
            {
                if (graphLogger != null) graphLogger.LogWarning($"{NODE_INPUT_EVAPORATION_TITLE} value invalid: {input.Evaporation} (valid: {MIN_EVAPORATION} <= n)", this);
                input.Evaporation = MIN_EVAPORATION;
            }

            if (input.Capacity < MIN_CAPACITY)
            {
                if (graphLogger != null) graphLogger.LogWarning($"{NODE_INPUT_CAPACITY_TITLE} value invalid: {input.Capacity} (valid: {MIN_CAPACITY} <= n)", this);
                input.Capacity = MIN_CAPACITY;
            }

            if (input.Erosion < MIN_EROSION)
            {
                if (graphLogger != null) graphLogger.LogWarning($"{NODE_INPUT_EROSION_TITLE} value invalid: {input.Erosion} (valid: {MIN_EROSION} <= n)", this);
                input.Erosion = MIN_EROSION;
            }

            if (input.Deposition <= MIN_DEPOSITION)
            {
                if (graphLogger != null) graphLogger.LogWarning($"{NODE_INPUT_DEPOSITION_TITLE} value invalid: {input.Deposition} (valid: {MIN_DEPOSITION} <= n)", this);
                input.Deposition = MIN_DEPOSITION;
            }

            if (input.Gravity <= MIN_GRAVITY)
            {
                if (graphLogger != null) graphLogger.LogWarning($"{NODE_INPUT_GRAVITY_TITLE} value invalid: {input.Gravity} (valid: {MIN_GRAVITY} <= n)", this);
                input.Gravity = MIN_GRAVITY;
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
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_ITERATIONS_ID, out temp.Iterations) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_RAIN_ID, out temp.Rain) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_EVAPORATION_ID, out temp.Evaporation) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_CAPACITY_ID, out temp.Capacity) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_EROSION_ID, out temp.Erosion) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_DEPOSITION_ID, out temp.Deposition) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRAVITY_ID, out temp.Gravity);

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
            RenderTexture tempHeightTexture1 = null;
            RenderTexture tempHeightTexture2 = null;
            RenderTexture tempWaterTexture1 = null;
            RenderTexture tempWaterTexture2 = null;
            RenderTexture tempSedimentTexture1 = null;
            RenderTexture tempSedimentTexture2 = null;

            try
            {
                var inputGrid = inputValues.Grid;
                var iterations = inputValues.Iterations;
                var rain = inputValues.Rain;
                var evaporation = inputValues.Evaporation;
                var capacity = inputValues.Capacity;
                var erosion = inputValues.Erosion;
                var deposition = inputValues.Deposition;
                var gravity = inputValues.Gravity;

                var size = inputGrid.Size;

                var inputTexture = inputGrid.RenderTexture;

                // Create ping-pong textures
                tempHeightTexture1 = TextureHelpers.CreateRenderTexture(size, RenderTextureFormat.RFloat);
                tempHeightTexture2 = TextureHelpers.CreateRenderTexture(size, RenderTextureFormat.RFloat);
                tempWaterTexture1 = TextureHelpers.CreateRenderTexture(size, RenderTextureFormat.RFloat, clear: true);
                tempWaterTexture2 = TextureHelpers.CreateRenderTexture(size, RenderTextureFormat.RFloat, clear: true);
                tempSedimentTexture1 = TextureHelpers.CreateRenderTexture(size, RenderTextureFormat.RFloat, clear: true);
                tempSedimentTexture2 = TextureHelpers.CreateRenderTexture(size, RenderTextureFormat.RFloat, clear: true);

                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader($"Shaders/{nameof(ErosionNode)}", out var shader))
                {
                    return false;
                }

                var kernel = shader.FindKernel("CSMain");

                var groups = Mathf.CeilToInt(size / 8.0f);

                Graphics.Blit(inputTexture, tempHeightTexture1);

                for (int i = 0; i < iterations; i++)
                {
                    shader.SetTexture(kernel, "_InHeightTexture", tempHeightTexture1);
                    shader.SetTexture(kernel, "_OutHeightTexture", tempHeightTexture2);
                    shader.SetTexture(kernel, "_InWaterTexture", tempWaterTexture1);
                    shader.SetTexture(kernel, "_OutWaterTexture", tempWaterTexture2);
                    shader.SetTexture(kernel, "_InSedimentTexture", tempSedimentTexture1);
                    shader.SetTexture(kernel, "_OutSedimentTexture", tempSedimentTexture2);
                    shader.SetInt("_Size", size);

                    shader.SetFloat("_DeltaTime", 0.16f); // 60 fps -- arbitrary scaler
                    shader.SetFloat("_Rain", rain);
                    shader.SetFloat("_Evaporation", evaporation);
                    shader.SetFloat("_Capacity", capacity);
                    shader.SetFloat("_Erosion", erosion);
                    shader.SetFloat("_Deposition", deposition);
                    shader.SetFloat("_Gravity", gravity);

                    shader.Dispatch(kernel, groups, groups, 1);

                    var swapHeightTexture = tempHeightTexture1;
                    tempHeightTexture1 = tempHeightTexture2;
                    tempHeightTexture2 = swapHeightTexture;

                    var swapWaterTexture = tempWaterTexture1;
                    tempWaterTexture1 = tempWaterTexture2;
                    tempWaterTexture2 = swapWaterTexture;

                    var swapSedimentTexture = tempSedimentTexture1;
                    tempSedimentTexture1 = tempSedimentTexture2;
                    tempSedimentTexture2 = swapSedimentTexture;
                }

                Graphics.Blit(tempHeightTexture1, outputTexture);

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
                if (tempHeightTexture1 != null)
                {
                    tempHeightTexture1.Release();
                    tempHeightTexture1 = null;
                }

                if (tempHeightTexture2 != null)
                {
                    tempHeightTexture2.Release();
                    tempHeightTexture2 = null;
                }

                if (tempWaterTexture1 != null)
                {
                    tempWaterTexture1.Release();
                    tempWaterTexture1 = null;
                }

                if (tempWaterTexture2 != null)
                {
                    tempWaterTexture2.Release();
                    tempWaterTexture2 = null;
                }

                if (tempSedimentTexture1 != null)
                {
                    tempSedimentTexture1.Release();
                    tempSedimentTexture1 = null;
                }

                if (tempSedimentTexture2 != null)
                {
                    tempSedimentTexture2.Release();
                    tempSedimentTexture2 = null;
                }
            }
        }
    }
}