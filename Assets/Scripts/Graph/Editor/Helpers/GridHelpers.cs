using UnityEngine;

public static class GridHelpers
{
    public static float SafeGet(HeightGrid grid, int x, int y)
    {
        int w = grid.Width;
        int h = grid.Height;
        x = Mathf.Clamp(x, 0, w - 1);
        y = Mathf.Clamp(y, 0, h - 1);
        return grid[x, y];
    }

    public static (float, float) GetRange(HeightGrid grid)
    {
        var size = grid.Width;

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
