#if __MICROVERSE__
using JBooth.MicroVerseCore;
using System;
using System.IO;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;
using static Indiecat.TerrainGraph.Editor.NodeConstants;
using Object = UnityEngine.Object;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class ExportStampNode : Node,
        IValidatableNode,
        IExecutableNode
    {
        private class InputValues
        {
            public HeightGrid Grid;
            public string StampName;
            public string FilePath;

            public int VersionHash;

            public override int GetHashCode()
            {
                return HashCode.Combine(Grid?.VersionHash, StampName, FilePath);
            }
        }

        // Options

        // Inputs
        private const string NODE_INPUT_GRID_ID = "grid_input";
        private const string NODE_INPUT_GRID_TITLE = "Grid";

        private const string NODE_INPUT_NAME_ID = "name_input";
        private const string NODE_INPUT_NAME_TITLE = "Stamp Name";

        private const string NODE_INPUT_PATH_ID = "path_input";
        private const string NODE_INPUT_PATH_TITLE = "Path";

        // Outputs
        
        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<bool>(NODE_OPTION_DISABLE_ID)
                .WithDisplayName(NODE_OPTION_DISABLE_TITLE)
                .WithDefaultValue(false)
                .Build();
            context.AddOption<WarningBanner>(NODE_OPTION_WARNING_ID)
                .WithDisplayName(NODE_OPTION_WARNING_TITLE)
                .Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Input
            context.AddInputPort<HeightGrid>(NODE_INPUT_GRID_ID)
                .WithDisplayName(NODE_INPUT_GRID_TITLE)
                .Build();

            context.AddInputPort<AdaptiveStringParameter>(NODE_INPUT_NAME_ID)
                .WithDisplayName(NODE_INPUT_NAME_TITLE)
                .WithDefaultValue("Height Stamp")
                .Build();
            context.AddInputPort<LongStringParameter>(NODE_INPUT_PATH_ID)
                .WithDisplayName(NODE_INPUT_PATH_TITLE)
                .WithDefaultValue("Assets/Textures/ExportedStamp.png")
                .Build();
        }


        public bool TryValidateNode(GraphLogger graphLogger = null)
        {
            GetNodeOptionByName(NODE_OPTION_DISABLE_ID).TryGetValue(out bool isNodeSkipped);
            NodeHelpers.TrySetWarningBanner(this, isNodeSkipped ? "DISABLED" : null);
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
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_NAME_ID, out temp.StampName) &&
                PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_PATH_ID, out temp.FilePath);

            if (success)
            {
                temp.VersionHash = temp.GetHashCode();

                input = temp;
                return true;
            }

            return false;
        }

        public bool TryExecuteNode()
        {
            GetNodeOptionByName(NODE_OPTION_DISABLE_ID).TryGetValue(out bool isNodeDisabled);
            if (isNodeDisabled)
            {
                // Execution skipped
                return true;
            }

            if (!TryGetValidatedInputValues(out var inputValues))
            {
                // Not in valid state
                return false;
            }

            try
            {
                var inputGrid = inputValues.Grid;
                var stampName = inputValues.StampName;
                var exportFilePath = inputValues.FilePath;

                var size = inputGrid.Size;

                if (!TextureHelpers.TryExportHeightGridTexture(inputGrid, exportFilePath))
                {
                    return false;
                }

                var microverse = MicroVerse.instance;
                if (microverse == null)
                {
                    // NOTE: The user must create MicroVerse themselves to avoid complexity here
                    throw new Exception("Missing MicroVerse scene object");
                }

                microverse.enabled = false;

                var heightStamps = microverse.GetComponentsInChildren<HeightStamp>();

                var heightStamp = heightStamps.FirstOrDefault(x => x.name == stampName);
                if (heightStamp == null)
                {
                    heightStamp = CreateGO(stampName).AddComponent<HeightStamp>();
                    heightStamp.transform.parent = microverse.transform;
                }

                // NOTE: Only expecting a single terrain
                var terrain = microverse.GetComponentInChildren<Terrain>();
                if (terrain == null)
                {
                    // NOTE: The user must create the terrain themselves to avoid complexity here
                    throw new Exception("Missing Terrain scene object under MicroVerse");
                }

                heightStamp.transform.localScale = terrain.terrainData.size;
                heightStamp.transform.position = new Vector3(terrain.terrainData.size.x, 0, terrain.terrainData.size.z) / 2;

                // NOTE: This may return null until the asset database picks up the new file
                var stampTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(exportFilePath);
                heightStamp.stamp = stampTexture;

                microverse.enabled = true;
                microverse.Invalidate();

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }

        // Copied from Packages\com.jbooth.microverse\Scripts\Editor\MenuItems.cs
        public static GameObject CreateGO(string name)
        {
            GameObject go = new GameObject(name);
            if (Selection.activeObject != null)
            {
                if (Selection.activeObject as GameObject)
                {
                    go.transform.SetParent(((GameObject)Selection.activeObject).transform);
                }
            }

            if (Selection.activeObject is GameObject)
            {
                GameObject parent = Selection.activeObject as GameObject;
                go.transform.SetParent(parent.transform, false);
            }
            if (go.GetComponentInParent<MicroVerse>() == null && MicroVerse.instance != null)
            {
                go.transform.SetParent(MicroVerse.instance.gameObject.transform, true);
            }
            go.transform.localScale = new Vector3(100, 100, 100);
            Selection.activeObject = go;
            return go;
        }
    }
}
#endif