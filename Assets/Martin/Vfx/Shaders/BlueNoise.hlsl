// Shader Graph > Custom Function Node > Type: File > Source: este archivo
// Colocar en Assets/Martin/VFX/Shaders/ (o referenciarlo desde ahi). Sin texturas: todo procedural.
#ifndef BLUE_NOISE_INCLUDED
#define BLUE_NOISE_INCLUDED

float Hash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float2 Hash22(float2 p)
{
    float n = Hash21(p);
    return float2(n, Hash21(p + n + 17.13));
}

float ValueNoise(float2 uv)
{
    float2 i = floor(uv), f = frac(uv);
    f = f * f * (3.0 - 2.0 * f);
    float a = Hash21(i);
    float b = Hash21(i + float2(1, 0));
    float c = Hash21(i + float2(0, 1));
    float d = Hash21(i + float2(1, 1));
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

// Inputs : UV (Vector2), Scale (Float), Speed (Float), Time (Float)
// Output : Out (Float 0..1)   -> fBm de 4 octavas, animado por scroll
void FBM_float(float2 UV, float Scale, float Speed, float Time, out float Out)
{
    float2 p = UV * Scale + float2(0.0, -Time * Speed);
    float a = 0.5, v = 0.0;
    [unroll] for (int i = 0; i < 4; i++)
    {
        v += a * ValueNoise(p);
        p = p * 2.03 + 11.7;
        a *= 0.5;
    }
    Out = v;
}

// Inputs : UV (Vector2), Scale (Float), Jitter (Float 0..1)
// Output : Edge (Float, 0 en la grieta -> 1 en celda), CellId (Float 0..1)
void VoronoiCracks_float(float2 UV, float Scale, float Jitter, out float Edge, out float CellId)
{
    float2 g = UV * Scale;
    float2 ip = floor(g), fp = frac(g);
    float f1 = 8.0, f2 = 8.0;
    CellId = 0.0;
    [unroll] for (int y = -1; y <= 1; y++)
    [unroll] for (int x = -1; x <= 1; x++)
    {
        float2 o = float2(x, y);
        float2 pt = o + lerp(0.5, Hash22(ip + o), Jitter);
        float d = distance(pt, fp);
        if (d < f1) { f2 = f1; f1 = d; CellId = Hash21(ip + o); }
        else if (d < f2) { f2 = d; }
    }
    Edge = saturate((f2 - f1) * 3.0);
}

#endif
