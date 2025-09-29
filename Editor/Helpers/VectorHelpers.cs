using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    public static class VectorHelpers
    {
        public static Vector2 SwizzleXY(this Vector3 v)
        {
            return new Vector2(v.x, v.y);
        }

        public static Vector2 SwizzleXZ(this Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }

        public static Vector2 SwizzleYZ(this Vector3 v)
        {
            return new Vector2(v.y, v.z);
        }

        public static Vector3 WithX(this Vector3 v, float x)
        {
            return new Vector3(x, v.y, v.z);
        }

        public static Vector3 WithY(this Vector3 v, float y)
        {
            return new Vector3(v.x, y, v.z);
        }

        public static Vector3 WithZ(this Vector3 v, float z)
        {
            return new Vector3(v.x, v.y, z);
        }

        public static Vector3 ToVector3XY(this Vector2 v)
        {
            return new Vector3(v.x, v.y, 0);
        }
        public static Vector3 ToVector3XZ(this Vector2 v)
        {
            return new Vector3(v.x, 0, v.y);
        }
        public static Vector3 ToVector3YZ(this Vector2 v)
        {
            return new Vector3(0, v.x, v.y);
        }
    }
}
