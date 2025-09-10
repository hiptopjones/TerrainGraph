using System;
using UnityEngine;

[Serializable]
public class HeightGrid
{
    public int Width;
    public int Height;

    [HideInInspector] public float[] Values;
    [HideInInspector] public int GenerationHash;

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
