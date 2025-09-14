using UnityEngine;

public class RadialShapeHeightProvider : HeightProvider
{
    public RadialShapeFunctions.ShapeType ShapeType { get; set; }
    public float Radius { get; set; }

    public override bool IsValid => true;
    public override int VersionHash { get; set; }

    public override bool TryGetHeights(int size, out float[,] heights)
    {
        var shapeFunction = RadialShapeFunctions.GetShapeFunction(ShapeType);

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
