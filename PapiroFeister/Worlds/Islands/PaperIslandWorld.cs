using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PapiroFeister.Textures.Generators;

namespace PapiroFeister.Worlds.Islands;

public sealed class PaperIslandWorld : System.IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly BasicEffect _surfaceEffect;
    private readonly BasicEffect _borderEffect;
    private readonly Texture2D _paperTexture;

    private readonly VertexPositionNormalTexture[] _oceanVertices;
    private readonly VertexPositionNormalTexture[] _landVertices;
    private readonly VertexPositionColor[][] _borderQuads;
    private readonly short[] _quadIndices = [0, 1, 2, 2, 1, 3];

    public float PlayableHalfSize { get; }

    public PaperIslandWorld(GraphicsDevice graphicsDevice, float playableHalfSize = 24f, float oceanHalfSize = 80f)
    {
        _graphicsDevice = graphicsDevice;
        PlayableHalfSize = MathHelper.Max(playableHalfSize, 8f);
        oceanHalfSize = MathHelper.Max(oceanHalfSize, PlayableHalfSize + 6f);

        _paperTexture = PaperTextureGenerator.GenerateTextureWithoutDots(graphicsDevice);

        _surfaceEffect = new BasicEffect(graphicsDevice)
        {
            TextureEnabled = true,
            Texture = _paperTexture,
            LightingEnabled = false,
            VertexColorEnabled = false,
            DiffuseColor = Color.White.ToVector3()
        };

        _borderEffect = new BasicEffect(graphicsDevice)
        {
            TextureEnabled = false,
            LightingEnabled = false,
            VertexColorEnabled = true,
            DiffuseColor = Color.White.ToVector3(),
            Alpha = 1f
        };

        _oceanVertices = CreateQuad(oceanHalfSize, y: -0.08f, uvScale: 9f);
        _landVertices = CreateQuad(PlayableHalfSize, y: 0f, uvScale: 3f);

        float borderThickness = 0.55f;
        float borderTop = 0.045f;

        _borderQuads =
        [
            CreateBorderQuad(-PlayableHalfSize - borderThickness, -PlayableHalfSize, -PlayableHalfSize, PlayableHalfSize, borderTop),
            CreateBorderQuad(PlayableHalfSize, PlayableHalfSize + borderThickness, -PlayableHalfSize, PlayableHalfSize, borderTop),
            CreateBorderQuad(-PlayableHalfSize, PlayableHalfSize, -PlayableHalfSize - borderThickness, -PlayableHalfSize, borderTop),
            CreateBorderQuad(-PlayableHalfSize, PlayableHalfSize, PlayableHalfSize, PlayableHalfSize + borderThickness, borderTop)
        ];
    }

    public void Draw(Matrix view, Matrix projection)
    {
        RasterizerState previousRasterizer = _graphicsDevice.RasterizerState;
        BlendState previousBlend = _graphicsDevice.BlendState;
        DepthStencilState previousDepthStencil = _graphicsDevice.DepthStencilState;

        _graphicsDevice.RasterizerState = new RasterizerState { CullMode = CullMode.None };
        _graphicsDevice.BlendState = BlendState.Opaque;
        _graphicsDevice.DepthStencilState = DepthStencilState.Default;

        _surfaceEffect.World = Matrix.Identity;
        _surfaceEffect.View = view;
        _surfaceEffect.Projection = projection;

        _surfaceEffect.DiffuseColor = new Vector3(0.63f, 0.8f, 0.9f);
        DrawTexturedQuad(_oceanVertices);

        _surfaceEffect.DiffuseColor = Color.White.ToVector3();
        DrawTexturedQuad(_landVertices);

        _borderEffect.World = Matrix.Identity;
        _borderEffect.View = view;
        _borderEffect.Projection = projection;

        foreach (EffectPass pass in _borderEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            foreach (VertexPositionColor[] borderQuad in _borderQuads)
            {
                _graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    borderQuad,
                    0,
                    4,
                    _quadIndices,
                    0,
                    2);
            }
        }

        _graphicsDevice.DepthStencilState = previousDepthStencil;
        _graphicsDevice.BlendState = previousBlend;
        _graphicsDevice.RasterizerState = previousRasterizer;
    }

    public void Dispose()
    {
        _surfaceEffect.Dispose();
        _borderEffect.Dispose();
        _paperTexture.Dispose();
    }

    private void DrawTexturedQuad(VertexPositionNormalTexture[] vertices)
    {
        foreach (EffectPass pass in _surfaceEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList,
                vertices,
                0,
                4,
                _quadIndices,
                0,
                2);
        }
    }

    private static VertexPositionNormalTexture[] CreateQuad(float halfSize, float y, float uvScale)
    {
        Vector3 normal = Vector3.Up;

        return
        [
            new VertexPositionNormalTexture(new Vector3(-halfSize, y, -halfSize), normal, new Vector2(0f, uvScale)),
            new VertexPositionNormalTexture(new Vector3(halfSize, y, -halfSize), normal, new Vector2(uvScale, uvScale)),
            new VertexPositionNormalTexture(new Vector3(-halfSize, y, halfSize), normal, new Vector2(0f, 0f)),
            new VertexPositionNormalTexture(new Vector3(halfSize, y, halfSize), normal, new Vector2(uvScale, 0f))
        ];
    }

    private static VertexPositionColor[] CreateBorderQuad(float minX, float maxX, float minZ, float maxZ, float y)
    {
        Color color = Color.Black;

        return
        [
            new VertexPositionColor(new Vector3(minX, y, minZ), color),
            new VertexPositionColor(new Vector3(maxX, y, minZ), color),
            new VertexPositionColor(new Vector3(minX, y, maxZ), color),
            new VertexPositionColor(new Vector3(maxX, y, maxZ), color)
        ];
    }
}
