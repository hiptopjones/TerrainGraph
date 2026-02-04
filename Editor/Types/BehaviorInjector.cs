using System;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class BehaviorInjector
    {
        public string OptionsTypeName;
        public string InputsTypeName;

        public Texture PreviewTexture; 
        public string PreviewDescription;

        public Action SetMeshPreview; // Drawer uses this to call the node
        public Action UpdatePreview;  // Node uses this to (indirectly) call the drawer

        // TODO: Can use binding instead?
        public void SetPreviewTexture(Texture texture, string description)
        {
            PreviewTexture = texture;
            PreviewDescription = description;

            UpdatePreview?.Invoke();
        }
    }
}
