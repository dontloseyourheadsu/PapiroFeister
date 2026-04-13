using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PapiroFeister.Worlds.Objects;

public sealed class FenceObject : WorldStaticObject
{
    public FenceObject(Vector3 initialPosition, float halfSize)
        : base(
            initialPosition,
            new FenceRenderer(halfSize),
            new FenceRingCollider(halfSize, thickness: 0.14f))
    {
    }

    private sealed class FenceRenderer : IObjectRenderer
    {
        private readonly VertexPositionColor[] _vertices;
        private readonly short[] _indices;

        public FenceRenderer(float halfSize)
        {
            (_vertices, _indices) = BuildFenceMesh(halfSize);
        }

        public void Draw(
            GraphicsDevice graphicsDevice,
            BasicEffect effect,
            Vector3 worldPosition,
            Matrix view,
            Matrix projection,
            Vector3 cameraPosition)
        {
            effect.World = Matrix.CreateTranslation(worldPosition);
            effect.View = view;
            effect.Projection = projection;

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    _vertices,
                    0,
                    _vertices.Length,
                    _indices,
                    0,
                    _indices.Length / 3);
            }
        }

        public void Dispose()
        {
        }

        private static (VertexPositionColor[] vertices, short[] indices) BuildFenceMesh(float halfSize)
        {
            const float fenceHeight = 1.05f;
            const float fenceThickness = 0.14f;
            const float yBottom = 0.04f;

            List<VertexPositionColor> vertices = new List<VertexPositionColor>(32);
            List<short> indices = new List<short>(48);

            Color panelColor = new Color(83, 59, 38);
            AddFenceSegment(vertices, indices,
                new Vector3(-halfSize, yBottom, -halfSize),
                new Vector3(halfSize, yBottom, -halfSize),
                new Vector3(0f, 0f, -fenceThickness),
                fenceHeight,
                panelColor);

            AddFenceSegment(vertices, indices,
                new Vector3(halfSize, yBottom, -halfSize),
                new Vector3(halfSize, yBottom, halfSize),
                new Vector3(fenceThickness, 0f, 0f),
                fenceHeight,
                panelColor);

            AddFenceSegment(vertices, indices,
                new Vector3(halfSize, yBottom, halfSize),
                new Vector3(-halfSize, yBottom, halfSize),
                new Vector3(0f, 0f, fenceThickness),
                fenceHeight,
                panelColor);

            AddFenceSegment(vertices, indices,
                new Vector3(-halfSize, yBottom, halfSize),
                new Vector3(-halfSize, yBottom, -halfSize),
                new Vector3(-fenceThickness, 0f, 0f),
                fenceHeight,
                panelColor);

            return (vertices.ToArray(), indices.ToArray());
        }

        private static void AddFenceSegment(
            List<VertexPositionColor> vertices,
            List<short> indices,
            Vector3 start,
            Vector3 end,
            Vector3 thicknessOffset,
            float height,
            Color color)
        {
            short baseIndex = (short)vertices.Count;

            vertices.Add(new VertexPositionColor(start, color));
            vertices.Add(new VertexPositionColor(end, color));
            vertices.Add(new VertexPositionColor(start + Vector3.Up * height, color));
            vertices.Add(new VertexPositionColor(end + Vector3.Up * height, color));

            vertices.Add(new VertexPositionColor(start + thicknessOffset, color));
            vertices.Add(new VertexPositionColor(end + thicknessOffset, color));
            vertices.Add(new VertexPositionColor(start + thicknessOffset + Vector3.Up * height, color));
            vertices.Add(new VertexPositionColor(end + thicknessOffset + Vector3.Up * height, color));

            AddQuad(indices, (short)(baseIndex + 0), (short)(baseIndex + 1), (short)(baseIndex + 2), (short)(baseIndex + 3));
            AddQuad(indices, (short)(baseIndex + 5), (short)(baseIndex + 4), (short)(baseIndex + 7), (short)(baseIndex + 6));
            AddQuad(indices, (short)(baseIndex + 4), (short)(baseIndex + 0), (short)(baseIndex + 6), (short)(baseIndex + 2));
            AddQuad(indices, (short)(baseIndex + 1), (short)(baseIndex + 5), (short)(baseIndex + 3), (short)(baseIndex + 7));
        }

        private static void AddQuad(List<short> indices, short a, short b, short c, short d)
        {
            indices.Add(a);
            indices.Add(b);
            indices.Add(c);

            indices.Add(c);
            indices.Add(b);
            indices.Add(d);
        }
    }
}
