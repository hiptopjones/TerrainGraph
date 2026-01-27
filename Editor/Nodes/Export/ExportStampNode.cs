#if __MICROVERSE__
using JBooth.MicroVerseCore;
#endif
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class ExportStampNode
        : BaseNode<ExportStampNode.OptionValues, ExportStampNode.InputValues, NullOutput>, IExportableNode
    {
        public class OptionValues : OptionValuesBase
        {
        }

        public class InputValues : InputValuesBase
        {
            public HeightGrid Grid;

            [DefaultValue("Stamp Name")]
            public string StampName;
            
            [DisplayName("Path")]
            [DefaultValue("Assets/Textures/ExportedStamp.png")]
            public string FilePath;

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    Grid?.VersionHash, StampName, FilePath
                );
            }
        }

        protected override bool TryExecuteNodeInternal()
        {
            return true;
        }

#if __MICROVERSE__
        public bool TryExportNode()
        {
            if (Inputs == null)
            {
                // Node is not in valid state
                return false;
            }

            var inputGrid = Inputs.Grid;
            var stampName = Inputs.StampName;
            var exportFilePath = Inputs.FilePath;

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
#else
        public bool TryExportNode()
        {
            Debug.LogWarning($"{nameof(ExportStampNode)} requires MicroVerse to be installed");
            return false;
        }
#endif
    }
}