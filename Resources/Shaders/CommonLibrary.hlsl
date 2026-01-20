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

#endif
