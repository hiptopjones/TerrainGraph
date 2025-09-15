using System;

[Serializable]
public class HeightGrid : IVersionedObject
{
    public int Size;

    public float[] Values { get; set; }
    public int VersionHash { get; set; }

    public bool IsValid => Values != null && Values.Length > 0;

    public float this[int index]
    {
        get => Values[index];
        set => Values[index] = value;
    }

    public float this[int x, int y]
    {
        get => Values[x + y * Size];
        set => Values[x + y * Size] = value;
    }

    public HeightGrid(int size)
    {
        Size = size;
        Values = new float[size * size];
    }
}
