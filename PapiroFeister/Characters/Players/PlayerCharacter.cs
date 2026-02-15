using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PapiroFeister.Characters.Players;

public sealed class PlayerCharacter : IDisposable
{
    private const float ColliderRadius = 0.9f;
    private const float Gravity = 45f;
    private const float MoveAcceleration = 38f;
    private const float JumpImpulse = 16f;
    private const float GroundedTolerance = 0.05f;
    private const float GroundDrag = 0.985f;
    private const float SpriteWidth = 2.6f;
    private const float SpriteHeight = 3.8f;
    private const float ShadowHorizontalRadius = 1.35f;
    private const float ShadowForwardRadius = 0.78f;
    private const float ShadowOffsetFromSurface = 0.03f;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly BasicEffect _spriteEffect;
    private readonly BasicEffect _decalEffect;
    private readonly VertexPositionTexture[] _quadVertices;
    private readonly VertexPositionTexture[] _decalVertices;
    private readonly short[] _quadIndices;
    private readonly Texture2D _texture;
    private readonly Texture2D _shadowTexture;
    private readonly float _worldRadius;

    private Vector3 _stabilizedRight = Vector3.Right;

    public Vector3 Position { get; private set; }
    public Vector3 Velocity { get; private set; }
    public Vector3 CameraForwardOnSurface { get; private set; } = Vector3.Forward;

    public PlayerCharacter(GraphicsDevice graphicsDevice, float worldRadius)
    {
        _graphicsDevice = graphicsDevice;
        _worldRadius = worldRadius;
        _quadVertices = new VertexPositionTexture[4];
        _decalVertices = new VertexPositionTexture[4];
        _quadIndices = [0, 1, 2, 2, 1, 3];

        _texture = LoadTexture(graphicsDevice);
        _shadowTexture = CreateShadowTexture(graphicsDevice, size: 128);

        _spriteEffect = new BasicEffect(graphicsDevice)
        {
            TextureEnabled = true,
            Texture = _texture,
            VertexColorEnabled = false,
            LightingEnabled = false,
            DiffuseColor = Color.White.ToVector3(),
            Alpha = 1f
        };

        _decalEffect = new BasicEffect(graphicsDevice)
        {
            TextureEnabled = true,
            VertexColorEnabled = false,
            LightingEnabled = false,
            DiffuseColor = Color.White.ToVector3(),
            Alpha = 1f
        };

        float startRadius = worldRadius + ColliderRadius;
        Position = new Vector3(0f, startRadius, 0f);
        Velocity = Vector3.Zero;
    }

    public void Update(float dt, KeyboardState keyboardState, KeyboardState previousKeyboardState, float worldRadius)
    {
        Vector3 currentUp = SafeNormalize(Position, Vector3.Up);
        Vector3 forwardOnSurface = ProjectOnPlane(CameraForwardOnSurface, currentUp);
        if (forwardOnSurface.LengthSquared() < 0.0001f)
            forwardOnSurface = ProjectOnPlane(Vector3.Forward, currentUp);
        forwardOnSurface.Normalize();

        Vector3 rightOnSurface = Vector3.Cross(forwardOnSurface, currentUp);
        if (rightOnSurface.LengthSquared() > 0.0001f)
            rightOnSurface.Normalize();

        float moveX = 0f;
        float moveY = 0f;

        if (keyboardState.IsKeyDown(Keys.A))
            moveX -= 1f;
        if (keyboardState.IsKeyDown(Keys.D))
            moveX += 1f;
        if (keyboardState.IsKeyDown(Keys.W))
            moveY += 1f;
        if (keyboardState.IsKeyDown(Keys.S))
            moveY -= 1f;

        Vector3 desiredMove = forwardOnSurface * moveY + rightOnSurface * moveX;
        if (desiredMove.LengthSquared() > 0.0001f)
        {
            desiredMove.Normalize();
            Velocity += desiredMove * MoveAcceleration * dt;

            CameraForwardOnSurface = Vector3.Lerp(CameraForwardOnSurface, desiredMove, 6f * dt);
            if (CameraForwardOnSurface.LengthSquared() > 0.0001f)
                CameraForwardOnSurface.Normalize();
        }

        Velocity += -currentUp * Gravity * dt;

        float surfaceRadius = worldRadius + ColliderRadius;
        bool isGrounded = Position.Length() <= surfaceRadius + GroundedTolerance;
        bool jumpPressed = keyboardState.IsKeyDown(Keys.Space) && !previousKeyboardState.IsKeyDown(Keys.Space);
        if (isGrounded && jumpPressed)
            Velocity += currentUp * JumpImpulse;

        Velocity *= MathF.Pow(GroundDrag, dt * 60f);
        Position += Velocity * dt;

        float distanceFromCenter = Position.Length();
        if (distanceFromCenter < surfaceRadius)
        {
            Vector3 normal = SafeNormalize(Position, Vector3.Up);
            Position = normal * surfaceRadius;

            float normalVelocity = Vector3.Dot(Velocity, normal);
            if (normalVelocity < 0f)
                Velocity -= normal * normalVelocity;
        }
    }

    public void Draw(Matrix view, Matrix projection, Vector3 cameraPosition)
    {
        Vector3 upFromCenter = SafeNormalize(Position, Vector3.Up);
        Vector3 toCameraOnSurface = ProjectOnPlane(cameraPosition - Position, upFromCenter);
        if (toCameraOnSurface.LengthSquared() < 0.0001f)
            toCameraOnSurface = ProjectOnPlane(CameraForwardOnSurface, upFromCenter);
        toCameraOnSurface = SafeNormalize(toCameraOnSurface, Vector3.Forward);

        Vector3 movementForward = ProjectOnPlane(CameraForwardOnSurface, upFromCenter);
        if (movementForward.LengthSquared() < 0.0001f)
            movementForward = toCameraOnSurface;
        movementForward = SafeNormalize(movementForward, toCameraOnSurface);

        Vector3 cameraRight = Vector3.Cross(upFromCenter, toCameraOnSurface);
        if (cameraRight.LengthSquared() < 0.0001f)
            cameraRight = Vector3.Cross(upFromCenter, Vector3.Forward);
        cameraRight = SafeNormalize(cameraRight, Vector3.Right);

        Vector3 movementRight = Vector3.Cross(movementForward, upFromCenter);
        if (movementRight.LengthSquared() < 0.0001f)
            movementRight = cameraRight;
        movementRight = SafeNormalize(movementRight, cameraRight);

        Vector3 targetRight = SafeNormalize(cameraRight * 0.7f + movementRight * 0.3f, cameraRight);
        if (Vector3.Dot(_stabilizedRight, targetRight) < 0f)
            targetRight = -targetRight;
        _stabilizedRight = SafeNormalize(Vector3.Lerp(_stabilizedRight, targetRight, 0.16f), targetRight);
        Vector3 right = _stabilizedRight;

        Vector3 bottomCenter = Position - upFromCenter * ColliderRadius;
        Vector3 topCenter = bottomCenter + upFromCenter * SpriteHeight;
        float halfWidth = SpriteWidth * 0.5f;

        _quadVertices[0] = new VertexPositionTexture(bottomCenter - right * halfWidth, new Vector2(0f, 1f));
        _quadVertices[1] = new VertexPositionTexture(bottomCenter + right * halfWidth, new Vector2(1f, 1f));
        _quadVertices[2] = new VertexPositionTexture(topCenter - right * halfWidth, new Vector2(0f, 0f));
        _quadVertices[3] = new VertexPositionTexture(topCenter + right * halfWidth, new Vector2(1f, 0f));

        RasterizerState previousRasterizer = _graphicsDevice.RasterizerState;
        BlendState previousBlend = _graphicsDevice.BlendState;
        DepthStencilState previousDepthStencil = _graphicsDevice.DepthStencilState;

        _graphicsDevice.RasterizerState = new RasterizerState { CullMode = CullMode.None };
        _graphicsDevice.BlendState = BlendState.AlphaBlend;
        _graphicsDevice.DepthStencilState = DepthStencilState.Default;

        DrawContactShadow(view, projection, upFromCenter, movementForward, right);

        float cameraDistance = Vector3.Distance(cameraPosition, Position);
        float depthDarken = MathHelper.Clamp((cameraDistance - 8f) / 40f, 0f, 0.22f);
        float brightness = 1f - depthDarken;

        _spriteEffect.View = view;
        _spriteEffect.Projection = projection;
        _spriteEffect.World = Matrix.Identity;
        _spriteEffect.DiffuseColor = new Vector3(brightness, brightness, brightness);

        foreach (EffectPass pass in _spriteEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList,
                _quadVertices,
                0,
                4,
                _quadIndices,
                0,
                2);
        }

        _graphicsDevice.DepthStencilState = previousDepthStencil;
        _graphicsDevice.BlendState = previousBlend;
        _graphicsDevice.RasterizerState = previousRasterizer;
    }

    public void Dispose()
    {
        _spriteEffect.Dispose();
        _decalEffect.Dispose();
        _texture.Dispose();
        _shadowTexture.Dispose();
    }

    private static Texture2D LoadTexture(GraphicsDevice graphicsDevice)
    {
        string[] candidatePaths =
        [
            Path.Combine(AppContext.BaseDirectory, "Content", "Characters", "Players", "stickman.png"),
            Path.Combine(AppContext.BaseDirectory, "Content", "Characters", "stickman.png"),
            Path.Combine(Directory.GetCurrentDirectory(), "Content", "Characters", "Players", "stickman.png"),
            Path.Combine(Directory.GetCurrentDirectory(), "Content", "Characters", "stickman.png")
        ];

        foreach (string path in candidatePaths)
        {
            if (!File.Exists(path))
                continue;

            using FileStream stream = File.OpenRead(path);
            return Texture2D.FromStream(graphicsDevice, stream);
        }

        throw new FileNotFoundException(
            "Player texture not found. Expected stickman.png at Content/Characters/stickman.png or Content/Characters/Players/stickman.png.");
    }

    private static Texture2D CreateShadowTexture(GraphicsDevice graphicsDevice, int size)
    {
        Texture2D texture = new Texture2D(graphicsDevice, size, size);
        Color[] data = new Color[size * size];

        float center = (size - 1) * 0.5f;
        float radius = center;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / radius;
                float dy = (y - center) / radius;
                float distance = MathF.Sqrt(dx * dx + dy * dy);
                float falloff = MathHelper.Clamp(1f - distance, 0f, 1f);
                float alpha = falloff * falloff * 0.85f;

                data[y * size + x] = new Color(0f, 0f, 0f, alpha);
            }
        }

        texture.SetData(data);
        return texture;
    }

    private void DrawContactShadow(Matrix view, Matrix projection, Vector3 upFromCenter, Vector3 forwardOnSurface, Vector3 rightOnSurface)
    {
        float heightAboveGround = MathF.Max(0f, Position.Length() - (_worldRadius + ColliderRadius));
        float shadowStrength = MathHelper.Clamp(1f - (heightAboveGround * 0.55f), 0.28f, 1f);
        float shadowScale = MathHelper.Lerp(0.6f, 1f, shadowStrength);
        float horizontalRadius = ShadowHorizontalRadius * shadowScale;
        float forwardRadius = ShadowForwardRadius * shadowScale;

        Vector3 shadowCenter = Position - upFromCenter * (ColliderRadius - ShadowOffsetFromSurface);
        Vector3 forward = SafeNormalize(forwardOnSurface, Vector3.Forward);
        Vector3 right = SafeNormalize(rightOnSurface, Vector3.Right);

        _decalVertices[0] = new VertexPositionTexture(shadowCenter - right * horizontalRadius - forward * forwardRadius, new Vector2(0f, 1f));
        _decalVertices[1] = new VertexPositionTexture(shadowCenter + right * horizontalRadius - forward * forwardRadius, new Vector2(1f, 1f));
        _decalVertices[2] = new VertexPositionTexture(shadowCenter - right * horizontalRadius + forward * forwardRadius, new Vector2(0f, 0f));
        _decalVertices[3] = new VertexPositionTexture(shadowCenter + right * horizontalRadius + forward * forwardRadius, new Vector2(1f, 0f));

        _decalEffect.Texture = _shadowTexture;
        _decalEffect.View = view;
        _decalEffect.Projection = projection;
        _decalEffect.World = Matrix.Identity;
        _decalEffect.DiffuseColor = new Vector3(0f, 0f, 0f);
        _decalEffect.Alpha = 0.42f * shadowStrength;

        foreach (EffectPass pass in _decalEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList,
                _decalVertices,
                0,
                4,
                _quadIndices,
                0,
                2);
        }
    }

    private static Vector3 ProjectOnPlane(Vector3 vector, Vector3 planeNormal)
    {
        return vector - Vector3.Dot(vector, planeNormal) * planeNormal;
    }

    private static Vector3 SafeNormalize(Vector3 value, Vector3 fallback)
    {
        if (value.LengthSquared() < 0.0001f)
            return fallback;

        value.Normalize();
        return value;
    }
}