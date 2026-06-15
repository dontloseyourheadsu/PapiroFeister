using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;

namespace PapiroFeister.Utils;

public static class TextureLoader
{
    public static Texture2D LoadTexture(GraphicsDevice graphicsDevice, string relativePath)
    {
        string[] candidatePaths =
        [
            Path.Combine(AppContext.BaseDirectory, relativePath),
            Path.Combine(Directory.GetCurrentDirectory(), relativePath),
            Path.Combine(AppContext.BaseDirectory, "PapiroFeister", relativePath),
            Path.Combine(Directory.GetCurrentDirectory(), "PapiroFeister", relativePath)
        ];

        foreach (string path in candidatePaths)
        {
            if (File.Exists(path))
            {
                try
                {
                    using FileStream stream = File.OpenRead(path);
                    return Texture2D.FromStream(graphicsDevice, stream);
                }
                catch
                {
                    // Ignore errors during stream loading, fallback to procedural
                }
            }
        }

        return null;
    }
}
