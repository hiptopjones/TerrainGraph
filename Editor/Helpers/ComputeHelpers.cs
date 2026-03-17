using UnityEngine;

namespace CodeFirst.TerrainGraph.Editor
{
    public class ComputeHelpers
    {
        private const string COMPUTE_SHADERS_ROOT = "Shaders/Compute";

        public static bool TryLoadComputeShader(string shaderName, out ComputeShader shader)
        {
            shader = Resources.Load<ComputeShader>($"{COMPUTE_SHADERS_ROOT}/{shaderName}");
            if (shader == null)
            {
                Debug.LogError($"Unable to find compute shader: {shaderName}");
                return false;
            }

            return true;
        }
    }
}
