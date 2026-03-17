using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    public static class VectorExtensions
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
        public static Vector2 WithX(this Vector2 v, float x)
        {
            return new Vector2(x, v.y);
        }

        public static Vector2 WithY(this Vector2 v, float y)
        {
            return new Vector2(v.x, y);
        }

        public static Vector2 AddX(this Vector2 v, float x)
        {
            return new Vector2(v.x + x, v.y);
        }

        public static Vector2 AddY(this Vector2 v, float y)
        {
            return new Vector2(v.x, v.y + y);
        }

        public static Vector2 SubtractX(this Vector2 v, float x)
        {
            return new Vector2(v.x - x, v.y);
        }

        public static Vector2 SubtractY(this Vector2 v, float y)
        {
            return new Vector2(v.x, v.y - y);
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

        public static Vector3 WithXZ(this Vector3 v, float x, float z)
        {
            return new Vector3(x, v.y, z);
        }

        public static Vector3 WithXY(this Vector3 v, float x, float y)
        {
            return new Vector3(x, y, v.z);
        }

        public static Vector3 WithYZ(this Vector3 v, float y, float z)
        {
            return new Vector3(v.x, y, z);
        }

        public static Vector3 AddX(this Vector3 v, float x)
        {
            return new Vector3(v.x + x, v.y, v.z);
        }

        public static Vector3 AddY(this Vector3 v, float y)
        {
            return new Vector3(v.x, v.y + y, v.z);
        }

        public static Vector3 AddZ(this Vector3 v, float z)
        {
            return new Vector3(v.x, v.y, v.z + z);
        }

        public static Vector3 SubtractX(this Vector3 v, float x)
        {
            return new Vector3(v.x - x, v.y, v.z);
        }

        public static Vector3 SubtractY(this Vector3 v, float y)
        {
            return new Vector3(v.x, v.y - y, v.z);
        }

        public static Vector3 SubtractZ(this Vector3 v, float z)
        {
            return new Vector3(v.x, v.y, v.z - z);
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

        public static float MaxComponent(this Vector2 v)
        {
            return Mathf.Max(v.x, v.y);
        }

        public static float MaxComponent(this Vector3 v)
        {
            return Mathf.Max(Mathf.Max(v.x, v.y), v.z);
        }

        public static bool ApproximatelyEquals(this Vector2 a, Vector2 b, float tolerance = 0.0001f)
        {
            return (a - b).sqrMagnitude <= tolerance * tolerance;
        }

        public static bool ApproximatelyEquals(this Vector3 a, Vector3 b, float tolerance = 0.0001f)
        {
            return (a - b).sqrMagnitude <= tolerance * tolerance;
        }

        public static Vector2 PiecewiseMultiply(this Vector2 a, Vector2 b)
        {
            return new Vector2(a.x * b.x, a.y * b.y);
        }

        public static Vector3 PiecewiseMultiply(this Vector3 a, Vector3 b)
        {
            return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        }

        public static Vector2 PiecewiseDivide(this Vector2 a, Vector2 b)
        {
            return new Vector2(a.x / b.x, a.y / b.y);
        }

        public static Vector3 PiecewiseDivide(this Vector3 a, Vector3 b)
        {
            return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
        }
    }
}
