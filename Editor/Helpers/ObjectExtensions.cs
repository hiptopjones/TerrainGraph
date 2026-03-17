using UnityObject = UnityEngine.Object;

namespace Indiecat.TerrainGraph.Editor
{
    public static partial class ObjectExtensions
    {
        public static bool IsUnityNull(this object obj)
        {
            // Copied from Unity.VisualScripting
            return obj == null || ((obj is UnityObject) && ((UnityObject)obj) == null);
        }
    }
}
