using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PapiroFeister.Textures.Generators;
using PapiroFeister.Utils;

namespace PapiroFeister.Worlds.Spheres;

public sealed class PaperWorldSphere : System.IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly BasicEffect _paperEffect;
    private readonly BasicEffect _outlineEffect;
    private readonly Texture2D _paperTexture;
    private readonly VertexBuffer _vertexBuffer;
    private readonly IndexBuffer _indexBuffer;
    private readonly int _indexCount;

    public float Radius { get; }
    public Texture2D PaperTexture => _paperTexture;

    public PaperWorldSphere(GraphicsDevice graphicsDevice, float radius = 30f, int tessellation = 48)
    {
        _graphicsDevice = graphicsDevice;
        Radius = radius;

        _paperTexture = PaperTextureGenerator.GenerateTexture(graphicsDevice);
        _paperEffect = new BasicEffect(graphicsDevice)
        {
            TextureEnabled = true,
            Texture = _paperTexture,
            LightingEnabled = false,
            VertexColorEnabled = false,
            DiffuseColor = Color.White.ToVector3()
        };

        _outlineEffect = new BasicEffect(graphicsDevice)
        {
            TextureEnabled = false,
            LightingEnabled = false,
            VertexColorEnabled = false,
            DiffuseColor = Color.Black.ToVector3(),
            Alpha = 0.11f
        };

        (_vertexBuffer, _indexBuffer, _indexCount) = SphereGenerator.GenerateSphere(graphicsDevice, radius, tessellation);
    }

    public void Draw(Matrix view, Matrix projection)
    {
        _graphicsDevice.SetVertexBuffer(_vertexBuffer);
        _graphicsDevice.Indices = _indexBuffer;

        RasterizerState previousRasterizer = _graphicsDevice.RasterizerState;
        BlendState previousBlend = _graphicsDevice.BlendState;
        DepthStencilState previousDepthStencil = _graphicsDevice.DepthStencilState;

        _graphicsDevice.RasterizerState = new RasterizerState { CullMode = CullMode.CullCounterClockwiseFace };

        _paperEffect.World = Matrix.Identity;
        _paperEffect.View = view;
        _paperEffect.Projection = projection;

        foreach (EffectPass pass in _paperEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _indexCount / 3);
        }

        _graphicsDevice.RasterizerState = new RasterizerState { CullMode = CullMode.CullClockwiseFace };
        _graphicsDevice.BlendState = BlendState.AlphaBlend;
        _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

        _outlineEffect.World = Matrix.CreateScale(1.004f);
        _outlineEffect.View = view;
        _outlineEffect.Projection = projection;

        foreach (EffectPass pass in _outlineEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _indexCount / 3);
        }

        _graphicsDevice.DepthStencilState = previousDepthStencil;
        _graphicsDevice.BlendState = previousBlend;
        _graphicsDevice.RasterizerState = previousRasterizer;
    }

    public void Dispose()
    {
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
        _paperEffect.Dispose();
        _outlineEffect.Dispose();
    }
}