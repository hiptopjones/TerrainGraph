using System;
using System.IO;
using UnityEngine;

public static class Raw16Writer
{
    public static bool TryEncodeRaw16(
        float[,] heights,
        out byte[] bytes,
        bool littleEndian = true,
        bool flipVertically = false,
        bool flipHorizontally = false,
        bool normalizeUsingMinMax = false,
        float minValue = 0f,
        float maxValue = 1f)
    {
        int height = heights.GetLength(0);
        int width = heights.GetLength(1);

        // Unity's RAW import expects width*height samples, 16-bit unsigned.
        // Optional flips are applied here so the file lands correctly on import.
        using (var stream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(stream))
            {
                for (int y = 0; y < height; y++)
                {
                    int sourceY = flipVertically ? (height - 1 - y) : y;

                    for (int x = 0; x < width; x++)
                    {
                        int sourceX = flipHorizontally ? (width - 1 - x) : x;

                        float value = heights[sourceX, sourceY];

                        if (normalizeUsingMinMax)
                        {
                            // Map minValue..maxValue -> 0..1
                            value = Mathf.InverseLerp(minValue, maxValue, value);
                        }

                        // Clamp to [0,1] just in case
                        value = Mathf.Clamp01(value);

                        // Scale to 0..65535 (unsigned 16-bit)
                        // Use rounding to preserve plateaus where possible.
                        ushort sample = (ushort)MathF.Round(value * 65535f);

                        // Emit bytes in requested endianness
                        byte lo = (byte)(sample & 0xFF);
                        byte hi = (byte)((sample >> 8) & 0xFF);

                        if (littleEndian)
                        {
                            writer.Write(lo);
                            writer.Write(hi);
                        }
                        else
                        {
                            writer.Write(hi);
                            writer.Write(lo);
                        }
                    }
                }
            }

            bytes = stream.GetBuffer();
            return true;
        }
    }

    /// <summary>
    /// Convenience: normalize from the data's own min/max, then write.
    /// Useful when your input floats are in meters (not 0..1).
    /// </summary>
    public static bool TryEncodeRaw16AutoRange(
        float[,] heights,
        out byte[] bytes,
        bool littleEndian = true,
        bool flipVertically = true,
        bool flipHorizontally = false)
    {
        float minValue = float.PositiveInfinity;
        float maxValue = float.NegativeInfinity;

        int rows = heights.GetLength(0);
        int cols = heights.GetLength(1);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float v = heights[r, c];

                minValue = Mathf.Min(minValue, v);
                maxValue = Mathf.Max(maxValue, v);
            }
        }

        return TryEncodeRaw16(
            heights,
            out bytes,
            littleEndian,
            flipVertically,
            flipHorizontally,
            normalizeUsingMinMax: true,
            minValue: minValue,
            maxValue: maxValue
        );
    }
}