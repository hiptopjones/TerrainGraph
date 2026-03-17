namespace CodeFirst.TerrainGraph.Editor
{
    public static class HashHelpers
    {
        // Takes (x, y, seed) and produces a deterministic, pseudo-random 32-bit integer hash.
        // Useful for proc gen (like noise, terrain, or randomness that depends on coordinates).
        public static uint U32(int x, int y, uint seed)
        {
            unchecked
            {
                uint h = (uint)x * 374761393u ^ (uint)y * 668265263u ^ seed * 362437u;
                h ^= h >> 13;
                h *= 1274126177u;
                return h;
            }
        }

        public static int S32(int x, int y, int seed)
        {
            return (int)U32(x, y, (uint)seed);
        }

        // Maps an integer hash (above) into a floating-point "random-looking" number in [0.0, 1.0]
        public static float F01(uint u)
        {
            return (u & 0x00FFFFFF) / 16777215f;
        }
    }
}