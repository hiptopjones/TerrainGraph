using UnityEngine;

public class RadialShapeHeightProvider : IHeightProvider
{
    public RadialShapeFunctions.ShapeType ShapeType { get; set; }
    public float Radius { get; set; }

    public bool IsValid => true;
    public int VersionHash { get; set; }

    public bool TryGetHeights(int size, out float[,] heights)
    {
        heights = null;

        if (!RadialShapeFunctions.TryGetFunction(ShapeType, out var shapeFunction))
        {
            return false;
        }

        var center = Vector2.one * size / 2f;

        heights = new float[size, size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var position = new Vector2(x, y) - center;
                heights[x, y] = shapeFunction(position, Radius);
            }
        }

        return true;
    }

}
