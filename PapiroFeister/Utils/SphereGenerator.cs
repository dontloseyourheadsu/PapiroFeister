using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace PapiroFeister.Utils;

/// <summary>
/// Utility class for generating 3D sphere geometry.
/// </summary>
public static class SphereGenerator
{
    /// <summary>
    /// Generates a sphere mesh with the specified parameters.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device to use for creating vertex and index buffers.</param>
    /// <param name="radius">Radius of the sphere.</param>
    /// <param name="tessellation">Number of divisions (higher = smoother but more vertices).</param>
    /// <returns>A tuple containing the vertex buffer and index buffer for the sphere.</returns>
    public static (VertexBuffer vertexBuffer, IndexBuffer indexBuffer, int indexCount) GenerateSphere(
        GraphicsDevice graphicsDevice, float radius = 1f, int tessellation = 12)
    {
        tessellation = Math.Max(tessellation, 3);

        int verticalSegments = tessellation;
        int horizontalSegments = tessellation * 2;

        var vertices = new VertexPositionNormalTexture[(verticalSegments + 1) * (horizontalSegments + 1)];
        var indices = new ushort[verticalSegments * horizontalSegments * 6];

        float deltaTheta = MathHelper.TwoPi / horizontalSegments;
        float deltaPhi = MathHelper.Pi / verticalSegments;

        int vertexIndex = 0;

        // Generate vertices
        for (int i = 0; i <= verticalSegments; i++)
        {
            float phi = i * deltaPhi;
            float sinPhi = (float)Math.Sin(phi);
            float cosPhi = (float)Math.Cos(phi);

            for (int j = 0; j <= horizontalSegments; j++)
            {
                float theta = j * deltaTheta;
                float sinTheta = (float)Math.Sin(theta);
                float cosTheta = (float)Math.Cos(theta);

                // Calculate position
                float x = cosTheta * sinPhi;
                float y = cosPhi;
                float z = sinTheta * sinPhi;

                Vector3 position = new Vector3(x, y, z) * radius;
                Vector3 normal = new Vector3(x, y, z);

                // Calculate texture coordinates
                float u = (float)j / horizontalSegments;
                float v = (float)i / verticalSegments;

                vertices[vertexIndex++] = new VertexPositionNormalTexture(position, normal, new Vector2(u, v));
            }
        }

        // Generate indices
        int indexIndex = 0;
        for (int i = 0; i < verticalSegments; i++)
        {
            for (int j = 0; j < horizontalSegments; j++)
            {
                int nextI = i + 1;
                int nextJ = (j + 1) % (horizontalSegments + 1);

                ushort v0 = (ushort)(i * (horizontalSegments + 1) + j);
                ushort v1 = (ushort)(nextI * (horizontalSegments + 1) + j);
                ushort v2 = (ushort)(i * (horizontalSegments + 1) + nextJ);
                ushort v3 = (ushort)(nextI * (horizontalSegments + 1) + nextJ);

                // First triangle
                indices[indexIndex++] = v0;
                indices[indexIndex++] = v1;
                indices[indexIndex++] = v2;

                // Second triangle
                indices[indexIndex++] = v2;
                indices[indexIndex++] = v1;
                indices[indexIndex++] = v3;
            }
        }

        // Create vertex buffer
        var vertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionNormalTexture.VertexDeclaration,
            vertices.Length, BufferUsage.WriteOnly);
        vertexBuffer.SetData(vertices);

        // Create index buffer
        var indexBuffer = new IndexBuffer(graphicsDevice, typeof(ushort),
            indices.Length, BufferUsage.WriteOnly);
        indexBuffer.SetData(indices);

        return (vertexBuffer, indexBuffer, indices.Length);
    }
}
