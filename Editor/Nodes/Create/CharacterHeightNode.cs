using System;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class CharacterHeightNode : ExecutableNode<HeightGrid>
    {
        private class InputValues
        {
            public char Character;
            public int Size;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(Character, Size);
            }
        }

        // Options

        // Inputs
        private const string NODE_INPUT_CHARACTER_ID = "char_input";
        private const string NODE_INPUT_CHARACTER_TITLE = "Character";

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
            context.AddInputPort<char>(NODE_INPUT_CHARACTER_ID)
                .WithDisplayName(NODE_INPUT_CHARACTER_TITLE)
                .WithDefaultValue('P')
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
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_CHARACTER_ID, out temp.Character) &&
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
                var character = inputValues.Character;
                var size = inputValues.Size;

                var outputTexture = GetOrCreateNodeRenderTexture(size);

                var fontTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/CascadiaMono.png");

                const int GLYPH_PIXEL_SIZE = 203;
                const int GLYPHS_PER_ROW = 10;
                const int GLYPHS_PER_COLUMN = 10;

                var glyphIndex = character - ' ';
                var glyphIndexX = glyphIndex % GLYPHS_PER_ROW;
                var glyphIndexY = (GLYPHS_PER_COLUMN - 1) - glyphIndex / GLYPHS_PER_ROW;
                var glyphRect = new Rect(
                    glyphIndexX * GLYPH_PIXEL_SIZE, 
                    glyphIndexY * GLYPH_PIXEL_SIZE,
                    GLYPH_PIXEL_SIZE,
                    GLYPH_PIXEL_SIZE);

                CopyGlyph(outputTexture, fontTexture, glyphRect);

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

        public void CopyGlyph(RenderTexture target, Texture2D fontAtlas, Rect glyphRect)
        {
            var savedRenderTexture = RenderTexture.active;

            RenderTexture.active = target;

            // Convert rect coords from UV space if needed
            int x = (int)glyphRect.x;
            int y = (int)glyphRect.y;
            int w = (int)glyphRect.width;
            int h = (int)glyphRect.height;

            // Copy the region from the atlas to the RenderTexture
            Graphics.CopyTexture(
                fontAtlas, 0, 0, x, y, w, h,
                target, 0, 0, 0, 0
            );

            RenderTexture.active = savedRenderTexture;
        }
    }
}