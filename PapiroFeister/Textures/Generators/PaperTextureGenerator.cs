using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PapiroFeister.Utils;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace PapiroFeister.Textures.Generators;

public static class PaperTextureGenerator
{
    private const int TextureWidth = 1024;
    private const int TextureHeight = 1024;
    private const int LineSpacing = 36;
    private static readonly Color PaperColor = new Color(247, 242, 210);
    private static readonly Color LineColor = new Color(155, 205, 255);
    private static readonly Color DoodleColorA = new Color(255, 153, 198);
    private static readonly Color DoodleColorB = new Color(143, 226, 255);
    private static readonly Color DoodleColorC = new Color(255, 229, 153);
    private const int FallbackSeed = 77123;
    private static readonly string TextureFilePath = Path.Combine("Content", "Textures", "paper_cartoon.bmp");

    private static Texture2D _paperTexture;
    private static PerlinNoise _noise;

    public static Texture2D GenerateTexture(GraphicsDevice graphicsDevice)
    {
        if (_paperTexture != null)
            return _paperTexture;

        string fullTexturePath = Path.Combine(AppContext.BaseDirectory, TextureFilePath);
        if (File.Exists(fullTexturePath))
        {
            using FileStream fileStream = File.OpenRead(fullTexturePath);
            _paperTexture = Texture2D.FromStream(graphicsDevice, fileStream);
            return _paperTexture;
        }

        _noise = new PerlinNoise(FallbackSeed);

        _paperTexture = new Texture2D(graphicsDevice, TextureWidth, TextureHeight);
        Color[] textureData = new Color[TextureWidth * TextureHeight];

        GenerateCartoonPaperTexture(textureData);

        _paperTexture.SetData(textureData);
        return _paperTexture;
    }

    private static void GenerateCartoonPaperTexture(Color[] textureData)
    {
        for (int y = 0; y < TextureHeight; y++)
        {
            for (int x = 0; x < TextureWidth; x++)
            {
                int idx = x + y * TextureWidth;

                float toneNoise = _noise.Noise(x / 150f, y / 150f);
                float tone = Map(toneNoise, 0f, 1f, -10f, 10f);
                byte r = (byte)Math.Clamp(PaperColor.R + tone, 0f, 255f);
                byte g = (byte)Math.Clamp(PaperColor.G + tone, 0f, 255f);
                byte b = (byte)Math.Clamp(PaperColor.B + tone, 0f, 255f);
                textureData[idx] = new Color(r, g, b);
            }
        }

        DrawNotebookLines(textureData);
        DrawDoodles(textureData);
    }

    private static void DrawNotebookLines(Color[] textureData)
    {
        for (int y = LineSpacing; y < TextureHeight; y += LineSpacing + 2)
        {
            for (int x = 0; x < TextureWidth; x++)
            {
                float wobble = (_noise.Noise(x / 60f, y / 60f) * 1.2f) - 0.6f;
                int yPos = (int)(y + wobble);

                if (yPos >= 0 && yPos < TextureHeight)
                {
                    int idx = x + yPos * TextureWidth;
                    Color existing = textureData[idx];
                    textureData[idx] = BlendColor(existing, LineColor, 0.48f);
                }
            }
        }
    }

    private static void DrawDoodles(Color[] textureData)
    {
        var random = new Random(FallbackSeed);
        for (int i = 0; i < 130; i++)
        {
            int cx = random.Next(20, TextureWidth - 20);
            int cy = random.Next(20, TextureHeight - 20);
            int radius = random.Next(2, 6);
            Color color = i % 3 == 0 ? DoodleColorA : (i % 3 == 1 ? DoodleColorB : DoodleColorC);

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y > radius * radius)
                        continue;

                    int px = cx + x;
                    int py = cy + y;
                    if (px < 0 || px >= TextureWidth || py < 0 || py >= TextureHeight)
                        continue;

                    int idx = px + py * TextureWidth;
                    textureData[idx] = BlendColor(textureData[idx], color, 0.35f);
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
        _noise = null;
    }
}