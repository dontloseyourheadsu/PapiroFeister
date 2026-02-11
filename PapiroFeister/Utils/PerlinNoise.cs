using System;

namespace PapiroFeister.Utils;

/// <summary>
/// Perlin noise implementation for procedural texture generation.
/// Based on the Simplex noise algorithm.
/// </summary>
public class PerlinNoise
{
    private readonly int[] _permutation;
    private readonly int[] _p;

    public PerlinNoise(int seed = 0)
    {
        _permutation = new int[256];

        // Create permutation table from seed
        Random random = new Random(seed);
        for (int i = 0; i < 256; i++)
            _permutation[i] = i;

        // Shuffle using Fisher-Yates
        for (int i = 255; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (_permutation[i], _permutation[j]) = (_permutation[j], _permutation[i]);
        }

        // Duplicate permutation table
        _p = new int[512];
        for (int i = 0; i < 512; i++)
            _p[i] = _permutation[i % 256];
    }

    /// <summary>
    /// Get Perlin noise value at given coordinates.
    /// Returns value between -1 and 1 (approximately 0 to 1 in practice).
    /// </summary>
    public float Noise(float x, float y = 0f, float z = 0f)
    {
        // Find unit cube that contains the point
        int xi = (int)Math.Floor(x) & 255;
        int yi = (int)Math.Floor(y) & 255;
        int zi = (int)Math.Floor(z) & 255;

        // Find relative x, y, z of point in cube
        float xf = x - (float)Math.Floor(x);
        float yf = y - (float)Math.Floor(y);
        float zf = z - (float)Math.Floor(z);

        // Compute fade curves
        float u = Fade(xf);
        float v = Fade(yf);
        float w = Fade(zf);

        // Hash coordinates of cube corners
        int n00 = Hash(_p[xi] + yi);
        int n10 = Hash(_p[xi + 1] + yi);
        int n01 = Hash(_p[xi] + yi + 1);
        int n11 = Hash(_p[xi + 1] + yi + 1);

        int n000 = Hash(n00 + zi);
        int n100 = Hash(n10 + zi);
        int n010 = Hash(n01 + zi);
        int n110 = Hash(n11 + zi);

        int n001 = Hash(n00 + zi + 1);
        int n101 = Hash(n10 + zi + 1);
        int n011 = Hash(n01 + zi + 1);
        int n111 = Hash(n11 + zi + 1);

        // Compute noise values
        float n0_0_0 = Gradient(n000, xf, yf, zf);
        float n1_0_0 = Gradient(n100, xf - 1, yf, zf);
        float n0_1_0 = Gradient(n010, xf, yf - 1, zf);
        float n1_1_0 = Gradient(n110, xf - 1, yf - 1, zf);

        float n0_0_1 = Gradient(n001, xf, yf, zf - 1);
        float n1_0_1 = Gradient(n101, xf - 1, yf, zf - 1);
        float n0_1_1 = Gradient(n011, xf, yf - 1, zf - 1);
        float n1_1_1 = Gradient(n111, xf - 1, yf - 1, zf - 1);

        // Interpolate
        float nx0_0 = Lerp(n0_0_0, n1_0_0, u);
        float nx1_0 = Lerp(n0_1_0, n1_1_0, u);
        float nx0_1 = Lerp(n0_0_1, n1_0_1, u);
        float nx1_1 = Lerp(n0_1_1, n1_1_1, u);

        float nxy0 = Lerp(nx0_0, nx1_0, v);
        float nxy1 = Lerp(nx0_1, nx1_1, v);

        return Lerp(nxy0, nxy1, w);
    }

    private float Fade(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    private float Lerp(float a, float b, float t)
    {
        return a + t * (b - a);
    }

    private int Hash(int x)
    {
        return _p[x & 255];
    }

    private float Gradient(int hash, float x, float y, float z)
    {
        hash &= 15;
        float u = hash < 8 ? x : y;
        float v = hash < 8 ? y : z;

        return ((hash & 1) == 0 ? u : -u) + ((hash & 2) == 0 ? v : -v);
    }
}
