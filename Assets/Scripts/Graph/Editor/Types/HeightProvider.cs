public abstract class HeightProvider : IVersionedObject
{
    public abstract bool IsValid { get; }
    public abstract int VersionHash { get; set; }

    public abstract bool TryGetHeights(int size, out float[,] heights);
}