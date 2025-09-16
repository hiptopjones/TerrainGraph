using UnityEngine;

public static class GridHelpers
{
    public static float SafeIndex(HeightGrid grid, int x, int y)
    {
        int w = grid.Size;
        int h = grid.Size;

        x = Mathf.Clamp(x, 0, w - 1);
        y = Mathf.Clamp(y, 0, h - 1);

        return grid[x, y];
    }


    public static float SafeIndex(HeightGrid grid, float x, float y)
    {
        var x1 = Mathf.FloorToInt(x);
        var y1 = Mathf.FloorToInt(y);
        var x2 = Mathf.FloorToInt(x + 1);
        var y2 = Mathf.FloorToInt(y + 1);

        var q11 = GridHelpers.SafeIndex(grid, x1, y1);
        var q21 = GridHelpers.SafeIndex(grid, x2, y1);
        var q22 = GridHelpers.SafeIndex(grid, x2, y2);
        var q12 = GridHelpers.SafeIndex(grid, x1, y2);

        var height = GeometryHelpers.BilinearInterpolate(x, y, q11, q21, q22, q12, x1, y1, x2, y2);
        return height;
    }

    public static (float, float) GetRange(HeightGrid grid)
    {
        var size = grid.Size;

        var maxValue = float.MinValue;
        var minValue = float.MaxValue;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var value = grid[x, y];

                maxValue = Mathf.Max(maxValue, value);
                minValue = Mathf.Min(minValue, value);
            }
        }

        return (minValue, maxValue);
    }
}
