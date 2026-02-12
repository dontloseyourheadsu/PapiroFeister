using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PapiroFeister.Textures.Generators;
using PapiroFeister.Utils;
using PapiroFeister.Worlds.Spheres;

namespace PapiroFeister;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private PaperWorldSphere _paperWorldSphere;
    private BasicEffect _characterEffect;
    private VertexBuffer _characterVertexBuffer;
    private IndexBuffer _characterIndexBuffer;
    private int _characterIndexCount;

    private Matrix _view;
    private Matrix _projection;

    private Vector3 _characterPosition;
    private Vector3 _characterVelocity;
    private Vector3 _cameraPosition;
    private Vector3 _cameraForwardOnSurface = Vector3.Forward;
    private KeyboardState _previousKeyboardState;

    private const float CharacterRadius = 0.9f;
    private const float Gravity = 45f;
    private const float MoveAcceleration = 38f;
    private const float JumpImpulse = 16f;
    private const float GroundedTolerance = 0.05f;
    private const float GroundDrag = 0.985f;
    private const float CameraHorizontalDistance = 7f;
    private const float CameraFloorAngleDeg = 60f;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.ApplyChanges();

        float aspectRatio = (float)GraphicsDevice.Viewport.Width / GraphicsDevice.Viewport.Height;
        _projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 0.1f, 500f);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _paperWorldSphere = new PaperWorldSphere(GraphicsDevice, radius: 30f, tessellation: 64);

        (_characterVertexBuffer, _characterIndexBuffer, _characterIndexCount) =
            SphereGenerator.GenerateSphere(GraphicsDevice, radius: CharacterRadius, tessellation: 20);

        _characterEffect = new BasicEffect(GraphicsDevice)
        {
            VertexColorEnabled = false,
            LightingEnabled = false,
            DiffuseColor = new Vector3(1f, 0f, 0f)
        };

        float startRadius = _paperWorldSphere.Radius + CharacterRadius;
        _characterPosition = new Vector3(0f, startRadius, 0f);
        _characterVelocity = Vector3.Zero;

        UpdateCamera();
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var keyboardState = Keyboard.GetState();

        Vector3 currentUp = SafeNormalize(_characterPosition, Vector3.Up);
        Vector3 forwardOnSurface = ProjectOnPlane(_cameraForwardOnSurface, currentUp);
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
            _characterVelocity += desiredMove * MoveAcceleration * dt;

            _cameraForwardOnSurface = Vector3.Lerp(_cameraForwardOnSurface, desiredMove, 6f * dt);
            if (_cameraForwardOnSurface.LengthSquared() > 0.0001f)
                _cameraForwardOnSurface.Normalize();
        }

        _characterVelocity += -currentUp * Gravity * dt;

        float surfaceRadius = _paperWorldSphere.Radius + CharacterRadius;
        bool isGrounded = _characterPosition.Length() <= surfaceRadius + GroundedTolerance;
        bool jumpPressed = keyboardState.IsKeyDown(Keys.Space) && !_previousKeyboardState.IsKeyDown(Keys.Space);
        if (isGrounded && jumpPressed)
            _characterVelocity += currentUp * JumpImpulse;

        _characterVelocity *= MathF.Pow(GroundDrag, dt * 60f);
        _characterPosition += _characterVelocity * dt;

        float minDistanceFromCenter = surfaceRadius;
        float distanceFromCenter = _characterPosition.Length();
        if (distanceFromCenter < minDistanceFromCenter)
        {
            Vector3 normal = SafeNormalize(_characterPosition, Vector3.Up);
            _characterPosition = normal * minDistanceFromCenter;

            float normalVelocity = Vector3.Dot(_characterVelocity, normal);
            if (normalVelocity < 0f)
                _characterVelocity -= normal * normalVelocity;
        }

        UpdateCamera();
        _previousKeyboardState = keyboardState;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
        _spriteBatch.Draw(
            _paperWorldSphere.PaperTexture,
            new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height),
            Color.White);
        _spriteBatch.End();

        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.RasterizerState = new RasterizerState { CullMode = CullMode.CullCounterClockwiseFace };

        _paperWorldSphere.Draw(_view, _projection);

        _characterEffect.View = _view;
        _characterEffect.Projection = _projection;
        _characterEffect.World = Matrix.CreateTranslation(_characterPosition);
        _characterEffect.DiffuseColor = new Vector3(1f, 0f, 0f);

        GraphicsDevice.SetVertexBuffer(_characterVertexBuffer);
        GraphicsDevice.Indices = _characterIndexBuffer;

        foreach (EffectPass pass in _characterEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _characterIndexCount / 3);
        }

        base.Draw(gameTime);
    }

    private void UpdateCamera()
    {
        Vector3 localUp = SafeNormalize(_characterPosition, Vector3.Up);
        Vector3 lookDirection = ProjectOnPlane(_cameraForwardOnSurface, localUp);
        if (lookDirection.LengthSquared() < 0.0001f)
            lookDirection = ProjectOnPlane(Vector3.Forward, localUp);
        lookDirection.Normalize();

        Vector3 target = _characterPosition + lookDirection * 1.4f;
        float cameraAngleRad = MathHelper.ToRadians(CameraFloorAngleDeg);
        float cameraHeight = CameraHorizontalDistance * (float)Math.Tan(cameraAngleRad);

        _cameraPosition = _characterPosition + localUp * cameraHeight - lookDirection * CameraHorizontalDistance;
        _view = Matrix.CreateLookAt(_cameraPosition, target, localUp);
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            PaperTextureGenerator.Cleanup();
            _spriteBatch?.Dispose();
            _paperWorldSphere?.Dispose();
            _characterVertexBuffer?.Dispose();
            _characterIndexBuffer?.Dispose();
            _characterEffect?.Dispose();
        }

        base.Dispose(disposing);
    }
}
