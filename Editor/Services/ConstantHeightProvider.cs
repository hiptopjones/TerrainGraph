namespace Indiecat.TerrainGraph.Editor
{
    public class ConstantHeightProvider : IHeightProvider
    {
        public float Height { get; set; }

        public bool IsValid => true;
        public float ExecutionTime => 0;
        public int VersionHash { get; set; }

        public bool TryGetHeights(int size, out float[,] heights)
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
}
