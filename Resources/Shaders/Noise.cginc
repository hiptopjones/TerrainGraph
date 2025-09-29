#ifndef NOISE_CGINC
#define NOISE_CGINC

// Hash function (value noise style gradient selection)
float2 Hash2D(float2 p)
{
    p = float2(dot(p, float2(127.1, 311.7)),
               dot(p, float2(269.5, 183.3)));
    return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
}

// Returns value between -1 and 1
float PerlinNoise2D(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);

    // Four corners in 2D
    float2 u = f * f * (3.0 - 2.0 * f);

    float2 g00 = Hash2D(i + float2(0.0, 0.0));
    float2 g10 = Hash2D(i + float2(1.0, 0.0));
    float2 g01 = Hash2D(i + float2(0.0, 1.0));
    float2 g11 = Hash2D(i + float2(1.0, 1.0));

    float n00 = dot(g00, f - float2(0.0, 0.0));
    float n10 = dot(g10, f - float2(1.0, 0.0));
    float n01 = dot(g01, f - float2(0.0, 1.0));
    float n11 = dot(g11, f - float2(1.0, 1.0));

    float nx0 = lerp(n00, n10, u.x);
    float nx1 = lerp(n01, n11, u.x);
    float nxy = lerp(nx0, nx1, u.y);

    return nxy;
}

float FBM2D(float2 p)
{
    float sum = 0.0;

    sum += 0.5 * PerlinNoise2D(p);
    sum += 0.25 * PerlinNoise2D(p * 0.5 + 17);
    sum += 0.125 * PerlinNoise2D(p * 0.25 + 43);
    
    return sum;
}
#endif // NOISE_CGINC
