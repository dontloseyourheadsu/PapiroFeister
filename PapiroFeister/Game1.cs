using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PapiroFeister.Characters.Players;
using PapiroFeister.Textures.Generators;
using PapiroFeister.Worlds.Spheres;

namespace PapiroFeister;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _backgroundPaperTexture;

    private PaperWorldSphere _paperWorldSphere;
    private PlayerCharacter _playerCharacter;

    private Matrix _view;
    private Matrix _projection;

    private Vector3 _cameraPosition;
    private KeyboardState _previousKeyboardState;

    private const float CameraHorizontalDistance = 7f;
    private const float CameraFloorAngleDeg = 55f;

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
        _backgroundPaperTexture = PaperTextureGenerator.GenerateTexture(GraphicsDevice);
        _paperWorldSphere = new PaperWorldSphere(GraphicsDevice, radius: 30f, tessellation: 64);
        _playerCharacter = new PlayerCharacter(GraphicsDevice, _paperWorldSphere.Radius);

        UpdateCamera();
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var keyboardState = Keyboard.GetState();

        _playerCharacter.Update(dt, keyboardState, _previousKeyboardState, _paperWorldSphere.Radius);

        UpdateCamera();
        _previousKeyboardState = keyboardState;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
        _spriteBatch.Draw(
            _backgroundPaperTexture,
            new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height),
            Color.White);
        _spriteBatch.End();

        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.RasterizerState = new RasterizerState { CullMode = CullMode.CullCounterClockwiseFace };

        _paperWorldSphere.Draw(_view, _projection);
        _playerCharacter.Draw(_view, _projection, _cameraPosition);

        base.Draw(gameTime);
    }

    private void UpdateCamera()
    {
        Vector3 localUp = SafeNormalize(_playerCharacter.Position, Vector3.Up);
        Vector3 lookDirection = ProjectOnPlane(_playerCharacter.CameraForwardOnSurface, localUp);
        if (lookDirection.LengthSquared() < 0.0001f)
            lookDirection = ProjectOnPlane(Vector3.Forward, localUp);
        lookDirection.Normalize();

        Vector3 target = _playerCharacter.Position + lookDirection * 1.4f;
        float cameraAngleRad = MathHelper.ToRadians(CameraFloorAngleDeg);
        float cameraHeight = CameraHorizontalDistance * (float)Math.Tan(cameraAngleRad);

        _cameraPosition = _playerCharacter.Position + localUp * cameraHeight - lookDirection * CameraHorizontalDistance;
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
            _playerCharacter?.Dispose();
        }

        base.Dispose(disposing);
    }
}
