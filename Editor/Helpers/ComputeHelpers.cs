using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    public class ComputeHelpers
    {
        public static bool TryLoadComputeShader(string shaderPath, out ComputeShader shader)
        {
            shader = Resources.Load<ComputeShader>(shaderPath);
            if (shader == null)
            {
                Debug.LogError($"Unable to find compute shader: {shaderPath}");
                return false;
            }

            return true;
        }
    }
}
