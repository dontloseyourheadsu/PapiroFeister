using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PapiroFeister.Inventory;

namespace PapiroFeister.Worlds.Objects;

public sealed class CraftingTableObject : CameraFacingObject
{
    public CraftingTableType TableType { get; }

    public CraftingTableObject(Vector3 initialPosition, CraftingTableType tableType)
        : base(
            initialPosition,
            new CraftingTableRenderer(tableType),
            new SphereCollider(centerOffset: new Vector3(0f, 0.8f, 0f), radius: 0.85f))
    {
        TableType = tableType;
    }

    private sealed class CraftingTableRenderer : IObjectRenderer
    {
        private readonly CraftingTableType _tableType;
        private static readonly short[] QuadIndices = [0, 1, 2, 2, 1, 3];
        private readonly VertexPositionColor[] _vertices = new VertexPositionColor[12]; // We can support layered details
        private readonly short[] _indices;

        public CraftingTableRenderer(CraftingTableType tableType)
        {
            _tableType = tableType;
            _indices = CreateIndices();
        }

        public void Draw(
            GraphicsDevice graphicsDevice,
            BasicEffect effect,
            Vector3 worldPosition,
            Matrix view,
            Matrix projection,
            Vector3 cameraPosition)
        {
            float width = 1.6f;
            float height = 1.6f;

            // Compute camera-facing billboard vectors
            Vector3 toCamera = cameraPosition - worldPosition;
            Vector3 toCameraOnSurface = new Vector3(toCamera.X, 0f, toCamera.Z);
            if (toCameraOnSurface.LengthSquared() < 0.0001f)
                toCameraOnSurface = Vector3.Forward;
            toCameraOnSurface.Normalize();

            Vector3 right = Vector3.Cross(Vector3.Up, toCameraOnSurface);
            if (right.LengthSquared() < 0.0001f)
                right = Vector3.Right;
            right.Normalize();

            Vector3 bottomCenter = worldPosition;
            Vector3 topCenter = worldPosition + Vector3.Up * height;
            float halfWidth = width * 0.5f;

            // Layer 1: Base Cardboard Backing
            Color baseColor = GetBaseColor();
            _vertices[0] = new VertexPositionColor(bottomCenter - right * halfWidth, baseColor);
            _vertices[1] = new VertexPositionColor(bottomCenter + right * halfWidth, baseColor);
            _vertices[2] = new VertexPositionColor(topCenter - right * halfWidth, baseColor);
            _vertices[3] = new VertexPositionColor(topCenter + right * halfWidth, baseColor);

            // Layer 2: Inner Panel/Detail (Inset box)
            Color innerColor = GetInnerColor();
            float insetW = halfWidth * 0.8f;
            float insetH_bottom = height * 0.15f;
            float insetH_top = height * 0.85f;
            _vertices[4] = new VertexPositionColor(bottomCenter - right * insetW + Vector3.Up * insetH_bottom, innerColor);
            _vertices[5] = new VertexPositionColor(bottomCenter + right * insetW + Vector3.Up * insetH_bottom, innerColor);
            _vertices[6] = new VertexPositionColor(bottomCenter - right * insetW + Vector3.Up * insetH_top, innerColor);
            _vertices[7] = new VertexPositionColor(bottomCenter + right * insetW + Vector3.Up * insetH_top, innerColor);

            // Layer 3: Dynamic Emblem / Sketch details (e.g. fire/yarn/tool shape)
            Color detailColor = GetDetailColor();
            float detW = halfWidth * 0.4f;
            float detH_bottom = height * 0.35f;
            float detH_top = height * 0.65f;
            _vertices[8] = new VertexPositionColor(bottomCenter - right * detW + Vector3.Up * detH_bottom, detailColor);
            _vertices[9] = new VertexPositionColor(bottomCenter + right * detW + Vector3.Up * detH_bottom, detailColor);
            _vertices[10] = new VertexPositionColor(bottomCenter - right * detW + Vector3.Up * detH_top, detailColor);
            _vertices[11] = new VertexPositionColor(bottomCenter + right * detW + Vector3.Up * detH_top, detailColor);

            effect.World = Matrix.Identity;
            effect.View = view;
            effect.Projection = projection;

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                // Draw Base Quad
                graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    _vertices,
                    0,
                    12,
                    _indices,
                    0,
                    _indices.Length / 3);
            }
        }

        private Color GetBaseColor()
        {
            return _tableType switch
            {
                CraftingTableType.Workbench => new Color(139, 90, 43),  // Brown wood
                CraftingTableType.Forge => new Color(60, 60, 65),       // Dark steel/coal grey
                CraftingTableType.CookingPot => new Color(45, 47, 52),  // Cauldron black
                CraftingTableType.Loom => new Color(205, 175, 125),     // Pine/straw tan
                _ => Color.Gray
            };
        }

        private Color GetInnerColor()
        {
            return _tableType switch
            {
                CraftingTableType.Workbench => new Color(185, 122, 87),  // Lighter wood inset
                CraftingTableType.Forge => new Color(90, 50, 40),       // Clay/ember warm tone
                CraftingTableType.CookingPot => new Color(50, 110, 80),  // Green bubbly soup
                CraftingTableType.Loom => new Color(225, 205, 165),     // Light weave canvas
                _ => Color.LightGray
            };
        }

        private Color GetDetailColor()
        {
            return _tableType switch
            {
                CraftingTableType.Workbench => new Color(38, 30, 20),      // Pencil/tools sketch charcoal
                CraftingTableType.Forge => new Color(255, 127, 39),        // Bright fire orange
                CraftingTableType.CookingPot => new Color(220, 220, 255),   // Steam/bubble white
                CraftingTableType.Loom => new Color(237, 28, 36),          // Red woven thread highlight
                _ => Color.White
            };
        }

        private short[] CreateIndices()
        {
            // We have 3 layers of quads: vertices 0-3, 4-7, 8-11
            // In C#, we offset by +0.005f forward relative to camera to avoid z-fighting!
            // But since they are camera-facing and sharing the same matrix, we draw them sequentially,
            // or we can adjust vertices slightly forward. Sequencing is fine for opaque/alpha blends.
            short[] indices = new short[18];
            // Quad 1: Base (0, 1, 2, 3)
            indices[0] = 0; indices[1] = 1; indices[2] = 2;
            indices[3] = 2; indices[4] = 1; indices[5] = 3;

            // Quad 2: Inset (4, 5, 6, 7)
            indices[6] = 4; indices[7] = 5; indices[8] = 6;
            indices[9] = 6; indices[10] = 5; indices[11] = 7;

            // Quad 3: Emblem (8, 9, 10, 11)
            indices[12] = 8; indices[13] = 9; indices[14] = 10;
            indices[15] = 10; indices[16] = 9; indices[17] = 11;

            return indices;
        }

        public void Dispose()
        {
        }
    }
}
