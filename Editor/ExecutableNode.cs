using System;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    public abstract class ExecutableNode<T> : Node,
        IValidatableNode,
        IExecutableNode,
        IEvaluatableNode<T>, 
        ICacheableNode<T>,
        IPreviewableNode
        where T : IVersionedObject
    {
        protected const string NODE_OPTION_PREVIEW_ID = "preview_option";
        protected const string NODE_OPTION_PREVIEW_TITLE = "Enable Preview";

        protected const string NODE_INPUT_PREVIEW_ID = "preview_input";
        protected const string NODE_INPUT_PREVIEW_TITLE = "Preview";

        public CacheData<T> CacheData { get; set; } = new();

        public abstract bool TryExecuteNode();
        public abstract bool TryGetOutputValue(IPort outputPort, out T value);
        public abstract bool TryValidateNode(GraphLogger graphLogger);

        public bool TryUpdatePreview()
        {
            GetNodeOptionByName(NODE_OPTION_PREVIEW_ID).TryGetValue<bool>(out var isPreviewEnabled);

            // Ensure the node state is up to date
            //  - Needed for standalone nodes that have nobody else to poke them
            //  - Needed to eventually try and cache input values
            if (TryExecuteNode())
            {
                if (!isPreviewEnabled)
                {
                    // Force generation when next enabled
                    CacheData.PreviewHash = 0;

                    // Preview is disabled, treat as up-to-date
                    return true;
                }

                // TODO: Should not be re-creating this if nothing has changed
                if (TryCreatePreviewTexture(CacheData.Output, out var texture))
                {
                    if (TrySetPreviewTexture(texture))
                    {
                        // Cache generation value to avoid unnecessary updates
                        CacheData.PreviewHash = CacheData.Output.VersionHash;
                        CacheData.PreviewTexture = texture;

                        // Preview was successfully updated
                        return true;
                    }
                }
            }

            if (isPreviewEnabled)
            {
                // Make it very clear there is a problem
                var warningTexture = (Texture2D)EditorGUIUtility.IconContent("console.warnicon.sml").image;

                // Best effort, not checking the return
                TrySetPreviewTexture(warningTexture);
            }

            // Preview failed to update
            return false;
        }

        private bool TryCreatePreviewTexture(T value, out Texture2D texture)
        {
            texture = null;

            if (CacheData.PreviewHash == CacheData.Output.VersionHash)
            {
                // Preview is already up-to-date
                texture = CacheData.PreviewTexture;
                return true;
            }

            if (CacheData.Output == null || !CacheData.Output.IsValid)
            {
                // Cached data is not present
                return false;
            }

            if (TextureHelpers.TryCreatePreviewTexture(value, out texture))
            {
                // Successfully created texture
                return true;
            }

            // Unable to create texture
            return false;
        }

        private bool TrySetPreviewTexture(Texture2D texture)
        {
            try
            {
                // TODO: Can we get the object once, instead of on every update?
                var previewPort = GetInputPortByName(NODE_INPUT_PREVIEW_ID);
                if (previewPort == null)
                {
                    Debug.Log("Unable to get the preview port");
                    return false;
                }

                if (!previewPort.TryGetValue(out PreviewImage previewImage))
                {
                    // Unable to get preview port value, so cannot display anything
                    Debug.LogError("Unable to get the preview image");
                    return false;
                }

                if (previewImage == null)
                {
                    Debug.Log("Preview port image is null");
                    return false;
                }

                previewImage.UpdateTexture(texture);

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
