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
}
