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

    // f(x1, y1) q11    x      f(x2, y1) q21 
    //                  |
    //                  v
    //       y  -----> (p)
    // f(x1, y2) q12           f(x2, y2) q22
    public static float BilinearInterpolate(float x, float y, float q11, float q21, float q22, float q12, float x1, float y1, float x2, float y2)
    {
        // Calculate the interpolation factors
        float r1 = ((x2 - x) / (x2 - x1)) * q11 + ((x - x1) / (x2 - x1)) * q21;
        float r2 = ((x2 - x) / (x2 - x1)) * q12 + ((x - x1) / (x2 - x1)) * q22;

        // Perform the final interpolation
        // (this can introduce some precision errors, which matter if close to a height boundary)
        return ((y2 - y) / (y2 - y1)) * r1 + ((y - y1) / (y2 - y1)) * r2;
    }

}
