using UnityEngine;

internal class GeometryHelpers
{
    // https://stackoverflow.com/questions/217578/how-can-i-determine-whether-a-2d-point-is-within-a-polygon
    public static bool IsPointInPolygon(Vector3 p, Vector3[] polygon, bool performSanityCheck = true)
    {
        if (!performSanityCheck)
        {
            float minX = polygon[0].x;
            float maxX = polygon[0].x;
            float minZ = polygon[0].z;
            float maxZ = polygon[0].z;
            for (int i = 1; i < polygon.Length; i++)
            {
                Vector3 q = polygon[i];
                minX = Mathf.Min(q.x, minX);
                maxX = Mathf.Max(q.x, maxX);
                minZ = Mathf.Min(q.z, minZ);
                maxZ = Mathf.Max(q.z, maxZ);
            }

            if (p.x < minX || p.x > maxX || p.z < minZ || p.z > maxZ)
            {
                return false;
            }
        }

        // https://wrf.ecse.rpi.edu/Research/Short_Notes/pnpoly.html
        bool isInside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            if ((polygon[i].z > p.z) != (polygon[j].z > p.z) &&
                 p.x < (polygon[j].x - polygon[i].x) * (p.z - polygon[i].z) / (polygon[j].z - polygon[i].z) + polygon[i].x)
            {
                isInside = !isInside;
            }
        }

        return isInside;
    }
}
