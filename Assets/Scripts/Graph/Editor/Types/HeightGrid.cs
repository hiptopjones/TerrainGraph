using System;
using UnityEngine;

[Serializable]
public class HeightGrid : IVersionedObject
{
    public int Width;
    public int Height;

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
        get => Values[x + y * Width];
        set => Values[x + y * Width] = value;
    }

    public HeightGrid(int size)
    {
        Width = size;
        Height = size;
        Values = new float[size * size];
    }
}
