public class ConstantHeightProvider : HeightProvider
{
    public float Height { get; set; }

    public override bool IsValid => true;
    public override int VersionHash { get; set; }

    public override bool TryGetHeights(int size, out float[,] heights)
    {
        heights = new float[size, size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                heights[x, y] = Height;
            }
        }

        return true;
    }
}
