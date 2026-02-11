using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PapiroFeister.Utils;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace PapiroFeister.Textures.Backgrounds;

static class PaperBackgroundTexture
{
    // Paper texture parameters (from the UI screenshot)
    private const int Wrinkle = 2;           // Rugosidad (Arrugas)
    private const int Depth = 8;             // Profundidad de Arrugas
    private const int Dirt = 1;               // Suciedad/Manchas
    private const int Fiber = 2;              // Textura de Fibra
    private const bool ShowLines = true;      // Mostrar Líneas
    private const int LineSpacing = 30;       // Espaciado de Líneas (30px)

    // Colors
    private static readonly Color PaperColor = Color.White;              // Color del Papel
    private static readonly Color LineColor = new Color(168, 213, 255);  // Color de Líneas (#a8d5ff)
    private static readonly Color MarginColor = new Color(255, 100, 100); // Margen Rojo

    // Texture dimensions
    private const int TextureWidth = 1024;
    private const int TextureHeight = 1280;

    private static Texture2D _paperTexture;
    private static PerlinNoise _noise;

    public static Texture2D GenerateTexture(GraphicsDevice graphicsDevice)
    {
        if (_paperTexture != null)
            return _paperTexture;

        int seed = (int)(DateTime.Now.Ticks % int.MaxValue);
        _noise = new PerlinNoise(seed);

        _paperTexture = new Texture2D(graphicsDevice, TextureWidth, TextureHeight);
        Color[] textureData = new Color[TextureWidth * TextureHeight];

        // Generate paper base texture with wrinkles, fiber, and dirt
        GeneratePaperTexture(textureData);

        // Add lines if enabled
        if (ShowLines)
            DrawLines(textureData);

        _paperTexture.SetData(textureData);
        return _paperTexture;
    }

    private static void GeneratePaperTexture(Color[] textureData)
    {
        for (int y = 0; y < TextureHeight; y++)
        {
            for (int x = 0; x < TextureWidth; x++)
            {
                int idx = x + y * TextureWidth;

                // RUGOSIDAD: Arrugas grandes (pliegues del papel)
                float wrinkleNoise = _noise.Noise(x / (float)Depth, y / (float)Depth);
                float wrinkleEffect = Map(wrinkleNoise, 0, 1, -Wrinkle, Wrinkle);

                // Arrugas secundarias (más detalladas)
                float wrinkleDetail = _noise.Noise(x / (Depth * 0.5f), y / (Depth * 0.5f) + 1000);
                float wrinkleDetail2 = Map(wrinkleDetail, 0, 1, -Wrinkle * 0.4f, Wrinkle * 0.4f);

                // Sombras en los pliegues profundos
                float creaseShadow = 0;
                if (wrinkleNoise < 0.3f)
                {
                    creaseShadow = Map(wrinkleNoise, 0, 0.3f, -Wrinkle * 0.5f, 0);
                }
                else if (wrinkleNoise > 0.7f)
                {
                    creaseShadow = Map(wrinkleNoise, 0.7f, 1, 0, Wrinkle * 0.3f);
                }

                // FIBRA: Textura sutil del papel
                float fiberNoise = _noise.Noise(x / 100f, y / 100f + 2000);
                float fiberEffect = Map(fiberNoise, 0, 1, -Fiber, Fiber);

                // Fibras direccionales
                float fiberDir = _noise.Noise(x / 80f, y / 20f + 3000);
                float fiberDirEffect = Map(fiberDir, 0, 1, -Fiber * 0.3f, Fiber * 0.3f);

                // SUCIEDAD: Manchas y mugre
                float dirtEffect = 0;
                if (Dirt > 0)
                {
                    float dirtNoise = _noise.Noise(x / 60f + 4000, y / 60f + 4000);
                    if (dirtNoise > 0.6f)
                    {
                        dirtEffect = Map(dirtNoise, 0.6f, 1, 0, -Dirt);
                    }

                    // Manchas pequeñas
                    float smallDirt = _noise.Noise(x / 20f + 5000, y / 20f + 5000);
                    if (smallDirt > 0.75f)
                    {
                        dirtEffect += Map(smallDirt, 0.75f, 1, 0, -Dirt * 0.5f);
                    }
                }

                // Combinar todos los efectos
                float total = wrinkleEffect + wrinkleDetail2 + creaseShadow + fiberEffect + fiberDirEffect + dirtEffect;

                byte r = (byte)Math.Clamp((int)(PaperColor.R + total), 0, 255);
                byte g = (byte)Math.Clamp((int)(PaperColor.G + total), 0, 255);
                byte b = (byte)Math.Clamp((int)(PaperColor.B + total), 0, 255);

                textureData[idx] = new Color(r, g, b, (byte)255);
            }
        }
    }

    private static void DrawLines(Color[] textureData)
    {
        // Draw horizontal lines with slight wobble
        for (int y = LineSpacing; y < TextureHeight; y += LineSpacing)
        {
            for (int x = 0; x < TextureWidth; x++)
            {
                // Calculate wobble using noise
                float wobble = (_noise.Noise(x / 50f, y / 50f) * 1.5f) - 0.75f;
                int yPos = (int)(y + wobble);

                if (yPos >= 0 && yPos < TextureHeight)
                {
                    int idx = x + yPos * TextureWidth;

                    // Blend line color with existing color
                    Color existing = textureData[idx];
                    textureData[idx] = BlendColor(existing, LineColor, 0.7f);
                }
            }
        }
    }

    private static float Map(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        return toMin + (value - fromMin) / (fromMax - fromMin) * (toMax - toMin);
    }

    private static Color BlendColor(Color existing, Color blend, float blendStrength)
    {
        return new Color(
            (byte)MathHelper.Lerp(existing.R, blend.R, blendStrength),
            (byte)MathHelper.Lerp(existing.G, blend.G, blendStrength),
            (byte)MathHelper.Lerp(existing.B, blend.B, blendStrength),
            existing.A
        );
    }

    public static void Cleanup()
    {
        _paperTexture?.Dispose();
        _paperTexture = null;
    }
}