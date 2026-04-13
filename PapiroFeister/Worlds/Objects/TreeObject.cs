using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PapiroFeister.Worlds.Objects;

public sealed class TreeObject : CameraFacingObject
{
    public TreeObject(Vector3 initialPosition)
        : base(
            initialPosition,
            new TreeBillboardRenderer(),
            new SphereCollider(centerOffset: new Vector3(0f, 1.3f, 0f), radius: 0.95f))
    {
    }

    private sealed class TreeBillboardRenderer : IObjectRenderer
    {
        private const int CanopySegments = 14;
        private static readonly short[] QuadIndices = [0, 1, 2, 2, 1, 3];

        private readonly VertexPositionColor[] _trunkVertices = new VertexPositionColor[4];
        private readonly VertexPositionColor[] _canopyVertices = new VertexPositionColor[CanopySegments + 1];
        private readonly short[] _canopyIndices = CreateTriangleFanIndices(CanopySegments);

        public void Draw(
            GraphicsDevice graphicsDevice,
            BasicEffect effect,
            Vector3 worldPosition,
            Matrix view,
            Matrix projection,
            Vector3 cameraPosition)
        {
            const float trunkHeight = 1.35f;
            const float trunkWidth = 0.52f;
            const float canopyRadius = 0.9f;

            Vector3 toCamera = cameraPosition - worldPosition;
            Vector3 toCameraOnSurface = new Vector3(toCamera.X, 0f, toCamera.Z);
            if (toCameraOnSurface.LengthSquared() < 0.0001f)
                toCameraOnSurface = Vector3.Forward;
            toCameraOnSurface.Normalize();

            Vector3 right = Vector3.Cross(Vector3.Up, toCameraOnSurface);
            if (right.LengthSquared() < 0.0001f)
                right = Vector3.Right;
            right.Normalize();

            Vector3 trunkBottom = worldPosition;
            Vector3 trunkTop = worldPosition + Vector3.Up * trunkHeight;
            float halfTrunkWidth = trunkWidth * 0.5f;

            _trunkVertices[0] = new VertexPositionColor(trunkBottom - right * halfTrunkWidth, new Color(112, 73, 42));
            _trunkVertices[1] = new VertexPositionColor(trunkBottom + right * halfTrunkWidth, new Color(112, 73, 42));
            _trunkVertices[2] = new VertexPositionColor(trunkTop - right * halfTrunkWidth, new Color(132, 87, 53));
            _trunkVertices[3] = new VertexPositionColor(trunkTop + right * halfTrunkWidth, new Color(132, 87, 53));

            Vector3 canopyCenter = trunkTop + Vector3.Up * 0.82f;
            _canopyVertices[0] = new VertexPositionColor(canopyCenter, new Color(41, 121, 63));

            for (int i = 0; i < CanopySegments; i++)
            {
                float angle = MathHelper.TwoPi * i / CanopySegments;
                float x = MathF.Cos(angle);
                float y = MathF.Sin(angle);
                Vector3 ringOffset = (right * x + Vector3.Up * y) * canopyRadius;
                Color canopyColor = y > 0f ? new Color(56, 148, 77) : new Color(36, 108, 57);
                _canopyVertices[i + 1] = new VertexPositionColor(canopyCenter + ringOffset, canopyColor);
            }

            effect.World = Matrix.Identity;
            effect.View = view;
            effect.Projection = projection;

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    _trunkVertices,
                    0,
                    _trunkVertices.Length,
                    QuadIndices,
                    0,
                    QuadIndices.Length / 3);

                graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    _canopyVertices,
                    0,
                    _canopyVertices.Length,
                    _canopyIndices,
                    0,
                    _canopyIndices.Length / 3);
            }
        }

        public void Dispose()
        {
        }

        private static short[] CreateTriangleFanIndices(int segments)
        {
            short[] indices = new short[segments * 3];
            int write = 0;

            for (int i = 0; i < segments; i++)
            {
                short current = (short)(i + 1);
                short next = (short)(((i + 1) % segments) + 1);

                indices[write++] = 0;
                indices[write++] = current;
                indices[write++] = next;
            }

            return indices;
        }
    }
}
