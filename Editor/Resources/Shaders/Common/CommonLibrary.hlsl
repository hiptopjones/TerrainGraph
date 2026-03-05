#ifndef INDIECAT_COMMON_LIBRARY_INCLUDED
#define INDIECAT_COMMON_LIBRARY_INCLUDED

// Perlin bias
float Bias(float t, float bias)
{
    return t / ((((1 / bias) - 2) * (1 - t)) + 1);
}

// Perlin gain
float Gain(float t, float gain)
{
    if (t < 0.5)
    {
        return Bias(t * 2, gain) / 2;
    }
    else
    {
        return Bias(t * 2 - 1, 1 - gain) / 2 + 0.5;
    }
}

bool IsPointInPolygon(float2 p, StructuredBuffer<float3> points, int pointsCount)
{
    bool inside = false;

    for (int i = 0; i < pointsCount; i++)
    {
        int j = (i + 1) % pointsCount;
        
        float2 a = points[i].xz;
        float2 b = points[j].xz;

        // Check if point crosses edge
        bool intersect = ((a.y > p.y) != (b.y > p.y)) &&
                         (p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y + 1e-6) + a.x);

        if (intersect)
        {
            inside = !inside;
        }
    }

    return inside;
}

bool IsPointOnLineSegment(float2 p, float2 a, float2 b)
{
    float2 ab = b - a;
    float2 ap = p - a;

    // 2D cross product magnitude (z-component)
    float cross = ab.x * ap.y - ab.y * ap.x;

    // Not collinear
    if (abs(cross) > 1e-4)
    {
        return false;
    }
    
    // Check if p is between a and b using dot product
    float dp = dot(ap, ab);
    if (dp < 0.0)
    {
        return false;
    }

    float abLenSq = dot(ab, ab);
    if (dp > abLenSq)
    {
        return false;
    }
    
    return true;
}

bool IsPointOnRight(float2 p, float2 a, float2 b, float2 d)
{
    // 2D cross product to get side of line
    float2 ab = b - a;
    float cross = ab.x * d.y - ab.y * d.x;
    return cross < 0;
}

// Distance from p to segment ab; returns distance and sets t in [0,1] along the segment.
float DistanceToSegment(float2 p, float2 a, float2 b, out float t)
{
    float2 ab = b - a;
    float len2 = dot(ab, ab);

    // Avoid division by zero for degenerate segments
    if (len2 < 1e-12)
    {
        t = 0.0;
        return length(p - a);
    }

    t = saturate(dot(p - a, ab) / len2);
    float2 c = a + ab * t;
    return length(p - c);
}

float SampleCurve(Texture2D<float> tex, SamplerState samplerState, float t)
{
    // Sample at y=0.5 of the 1px height
    return tex.SampleLevel(samplerState, float2(saturate(t), 0.5), 0);
}

#endif
