using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using static Indiecat.TerrainGraph.Editor.NodeConstants;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class ParticleErosionNode : ExecutableNode<HeightGrid>
    {
        private class InputValues
        {
            public HeightGrid Grid;
            public int Droplets;
            public float Erosion;
            public float Deposition;
            public float Inertia;
            public float Evaporation;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    Grid?.VersionHash, Droplets, Erosion, Deposition,
                    Inertia, Evaporation);
            }
        }

        // Inputs
        private const string NODE_INPUT_GRID_ID = "grid_input";
        private const string NODE_INPUT_GRID_TITLE = "Grid";

        private const string NODE_INPUT_DROPLETS_ID = "droplets_input";
        private const string NODE_INPUT_DROPLETS_TITLE = "Droplets";

        private const string NODE_INPUT_EROSION_ID = "erosion_input";
        private const string NODE_INPUT_EROSION_TITLE = "Erosion";

        private const string NODE_INPUT_DEPOSITION_ID = "deposition_input";
        private const string NODE_INPUT_DEPOSITION_TITLE = "Deposition";

        private const string NODE_INPUT_INERTIA_ID = "inertia_input";
        private const string NODE_INPUT_INERTIA_TITLE = "Inertia";

        private const string NODE_INPUT_EVAPORATION_ID = "evaporation_input";
        private const string NODE_INPUT_EVAPORATION_TITLE = "Evaporation";

        // Outputs
        private const string NODE_OUTPUT_GRID_ID = "grid_output";
        private const string NODE_OUTPUT_GRID_TITLE = "Grid";

        // Parameter constraints
        private const int MIN_DROPLETS = 1000;
        private const int MAX_DROPLETS = 500000;
        private const int DEFAULT_DROPLETS = 50000;

        private const float MIN_EROSION = 0.01f;
        private const float MAX_EROSION = 1.0f;
        private const float DEFAULT_EROSION = 0.3f;

        private const float MIN_DEPOSITION = 0.01f;
        private const float MAX_DEPOSITION = 1.0f;
        private const float DEFAULT_DEPOSITION = 0.3f;

        private const float MIN_INERTIA = 0.0f;
        private const float MAX_INERTIA = 1.0f;
        private const float DEFAULT_INERTIA = 0.05f;

        private const float MIN_EVAPORATION = 0.0f;
        private const float MAX_EVAPORATION = 0.1f;
        private const float DEFAULT_EVAPORATION = 0.02f;

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
            context.AddInputPort<int>(NODE_INPUT_DROPLETS_ID)
                .WithDisplayName(NODE_INPUT_DROPLETS_TITLE)
                .WithDefaultValue(DEFAULT_DROPLETS)
                .Build();
            context.AddInputPort<float>(NODE_INPUT_EROSION_ID)
                .WithDisplayName(NODE_INPUT_EROSION_TITLE)
                .WithDefaultValue(DEFAULT_EROSION)
                .Build();
            context.AddInputPort<float>(NODE_INPUT_DEPOSITION_ID)
                .WithDisplayName(NODE_INPUT_DEPOSITION_TITLE)
                .WithDefaultValue(DEFAULT_DEPOSITION)
                .Build();
            context.AddInputPort<float>(NODE_INPUT_INERTIA_ID)
                .WithDisplayName(NODE_INPUT_INERTIA_TITLE)
                .WithDefaultValue(DEFAULT_INERTIA)
                .Build();
            context.AddInputPort<float>(NODE_INPUT_EVAPORATION_ID)
                .WithDisplayName(NODE_INPUT_EVAPORATION_TITLE)
                .WithDefaultValue(DEFAULT_EVAPORATION)
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
                if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_GRID_TITLE} value missing", this);
                isValid = false;
            }

            if (input.Droplets < MIN_DROPLETS || input.Droplets > MAX_DROPLETS)
            {
                if (graphLogger != null) graphLogger.LogWarning($"{NODE_INPUT_DROPLETS_TITLE} value invalid: {input.Droplets} (valid: {MIN_DROPLETS} <= n <= {MAX_DROPLETS})", this);
                input.Droplets = Mathf.Clamp(input.Droplets, MIN_DROPLETS, MAX_DROPLETS);
            }

            if (input.Erosion < MIN_EROSION || input.Erosion > MAX_EROSION)
            {
                if (graphLogger != null) graphLogger.LogWarning($"{NODE_INPUT_EROSION_TITLE} value invalid: {input.Erosion} (valid: {MIN_EROSION} <= n <= {MAX_EROSION})", this);
                input.Erosion = Mathf.Clamp(input.Erosion, MIN_EROSION, MAX_EROSION);
            }

            if (input.Deposition < MIN_DEPOSITION || input.Deposition > MAX_DEPOSITION)
            {
                if (graphLogger != null) graphLogger.LogWarning($"{NODE_INPUT_DEPOSITION_TITLE} value invalid: {input.Deposition} (valid: {MIN_DEPOSITION} <= n <= {MAX_DEPOSITION})", this);
                input.Deposition = Mathf.Clamp(input.Deposition, MIN_DEPOSITION, MAX_DEPOSITION);
            }

            if (input.Inertia < MIN_INERTIA || input.Inertia > MAX_INERTIA)
            {
                if (graphLogger != null) graphLogger.LogWarning($"{NODE_INPUT_INERTIA_TITLE} value invalid: {input.Inertia} (valid: {MIN_INERTIA} <= n <= {MAX_INERTIA})", this);
                input.Inertia = Mathf.Clamp(input.Inertia, MIN_INERTIA, MAX_INERTIA);
            }

            if (input.Evaporation < MIN_EVAPORATION || input.Evaporation > MAX_EVAPORATION)
            {
                if (graphLogger != null) graphLogger.LogWarning($"{NODE_INPUT_EVAPORATION_TITLE} value invalid: {input.Evaporation} (valid: {MIN_EVAPORATION} <= n <= {MAX_EVAPORATION})", this);
                input.Evaporation = Mathf.Clamp(input.Evaporation, MIN_EVAPORATION, MAX_EVAPORATION);
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
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_DROPLETS_ID, out temp.Droplets) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_EROSION_ID, out temp.Erosion) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_DEPOSITION_ID, out temp.Deposition) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_INERTIA_ID, out temp.Inertia) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_EVAPORATION_ID, out temp.Evaporation);

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
                CacheData.Output = null;
                return false;
            }

            if (CacheData.Output != null && CacheData.Output.VersionHash == inputValues.VersionHash)
            {
                return true;
            }

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
            ComputeBuffer heightDeltaBuffer = null;

            try
            {
                var inputGrid = inputValues.Grid;
                var dropletCount = inputValues.Droplets;
                var erosion = inputValues.Erosion;
                var deposition = inputValues.Deposition;
                var inertia = inputValues.Inertia;
                var evaporation = inputValues.Evaporation;

                var size = inputGrid.Size;
                var inputTexture = inputGrid.RenderTexture;

                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader($"Shaders/{nameof(ParticleErosionNode)}", out var shader))
                {
                    return false;
                }

                // Create buffer for atomic height modifications
                heightDeltaBuffer = new ComputeBuffer(size * size, sizeof(int));

                // Clear buffer to zero
                int[] zeros = new int[size * size];
                heightDeltaBuffer.SetData(zeros);

                // Kernel 0: Simulate droplets
                var simulateKernel = shader.FindKernel("CSSimulateDroplets");

                shader.SetTexture(simulateKernel, "_InHeightTexture", inputTexture);
                shader.SetBuffer(simulateKernel, "_HeightDeltaBuffer", heightDeltaBuffer);
                shader.SetInt("_Size", size);
                shader.SetInt("_DropletCount", dropletCount);
                shader.SetInt("_Seed", UnityEngine.Random.Range(0, int.MaxValue));
                shader.SetFloat("_Erosion", erosion);
                shader.SetFloat("_Deposition", deposition);
                shader.SetFloat("_Inertia", inertia);
                shader.SetFloat("_Evaporation", evaporation);

                int dropletGroups = Mathf.CeilToInt(dropletCount / 256.0f);
                shader.Dispatch(simulateKernel, dropletGroups, 1, 1);

                // Kernel 1: Apply height deltas
                var applyKernel = shader.FindKernel("CSApplyHeightDeltas");

                shader.SetTexture(applyKernel, "_InHeightTexture", inputTexture);
                shader.SetTexture(applyKernel, "_OutHeightTexture", outputTexture);
                shader.SetBuffer(applyKernel, "_HeightDeltaBuffer", heightDeltaBuffer);
                shader.SetInt("_Size", size);

                int pixelGroups = Mathf.CeilToInt(size / 8.0f);
                shader.Dispatch(applyKernel, pixelGroups, pixelGroups, 1);

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
                if (heightDeltaBuffer != null)
                {
                    heightDeltaBuffer.Release();
                    heightDeltaBuffer = null;
                }
            }
        }
    }
}
