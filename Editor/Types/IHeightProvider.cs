public interface IHeightProvider : IProvider
{
    bool TryGetHeights(int size, out float[,] heights);
}