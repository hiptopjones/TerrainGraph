using System;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class IslandsHeightNode : ExecutableNode<HeightGrid>
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

        private const int MIN_SAMPLES = 10;

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
            Texture2D workingTexture = null;

            try
            {
                var inputGrid = inputValues.Grid;

                var size = inputGrid.Size;

                var inputTexture = inputGrid.RenderTexture;

                if (!TextureHelpers.TryCopyRenderTextureToTexture2D(inputTexture, TextureFormat.RFloat, out workingTexture))
                {
                    return false;
                }

                var workingGrid = new HeightGrid(size);
                var rawTextureData = workingTexture.GetRawTextureData<float>();
                rawTextureData.CopyTo(workingGrid.Values);

                var clusters = GridHelpers.GetClusters(workingGrid)
                    .OrderByDescending(c => c.Count)
                    .ToList();

                for (int i = 0; i < clusters.Count; i++)
                {
                    var cluster = clusters[i];

                    foreach (var p in cluster)
                    {
                        workingTexture.SetPixel(p.x, p.y, new Color(i, 0, 0));
                    }
                }

                workingTexture.Apply();

                var outputTexture = GetOrCreateNodeRenderTexture(size);

                Graphics.Blit(workingTexture, outputTexture);

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
                if (workingTexture != null)
                {
                    Object.DestroyImmediate(workingTexture);
                    workingTexture = null;
                }
            }
        }
    }
}