using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PapiroFeister.Cameras;
using PapiroFeister.Textures.Backgrounds;
using PapiroFeister.Utils;

namespace PapiroFeister;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _paperTexture;

    // 3D rendering components
    private Camera3D _camera;
    private VertexBuffer _sphereVertexBuffer;
    private IndexBuffer _sphereIndexBuffer;
    private int _sphereIndexCount;
    private BasicEffect _basicEffect;

    // Camera control parameters
    private float _cameraRotationX = -MathHelper.PiOver4; // Pitch (looking down)
    private float _cameraRotationY = 0f; // Yaw (orbiting left/right)
    private float _cameraDistance = 15f; // Distance from target
    private Vector3 _spherePosition = Vector3.Zero; // Position of the sphere to watch

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // Initialize 3D graphics
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.ApplyChanges();

        // Create camera
        float aspectRatio = (float)GraphicsDevice.Viewport.Width / GraphicsDevice.Viewport.Height;
        _camera = new Camera3D(aspectRatio);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Generate paper background texture
        _paperTexture = PaperBackgroundTexture.GenerateTexture(GraphicsDevice);

        // Generate 3D sphere
        (_sphereVertexBuffer, _sphereIndexBuffer, _sphereIndexCount) =
            SphereGenerator.GenerateSphere(GraphicsDevice, radius: 2f, tessellation: 16);

        // Create basic effect for rendering
        _basicEffect = new BasicEffect(GraphicsDevice)
        {
            VertexColorEnabled = false,
            LightingEnabled = true,
            AmbientLightColor = new Vector3(0.5f, 0.5f, 0.5f),
        };

        // Set up light
        _basicEffect.DirectionalLight0.Enabled = true;
        _basicEffect.DirectionalLight0.DiffuseColor = Color.White.ToVector3();
        _basicEffect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(1f, -1f, -1f));

        // Set up light for sphere color
        _basicEffect.SpecularColor = Color.White.ToVector3();
        _basicEffect.SpecularPower = 16f;
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // Get keyboard input for camera control
        var keyboardState = Keyboard.GetState();

        // Rotate camera with arrow keys
        if (keyboardState.IsKeyDown(Keys.Left))
            _cameraRotationY += 2f * (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (keyboardState.IsKeyDown(Keys.Right))
            _cameraRotationY -= 2f * (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (keyboardState.IsKeyDown(Keys.Up))
            _cameraRotationX = MathHelper.Clamp(_cameraRotationX + 2f * (float)gameTime.ElapsedGameTime.TotalSeconds,
                -MathHelper.PiOver2 + 0.1f, 0.1f);
        if (keyboardState.IsKeyDown(Keys.Down))
            _cameraRotationX = MathHelper.Clamp(_cameraRotationX - 2f * (float)gameTime.ElapsedGameTime.TotalSeconds,
                -MathHelper.PiOver2 + 0.1f, 0.1f);

        // Zoom in/out with Z/X keys
        if (keyboardState.IsKeyDown(Keys.Z))
            _cameraDistance = MathHelper.Max(_cameraDistance - 2f * (float)gameTime.ElapsedGameTime.TotalSeconds, 2f);
        if (keyboardState.IsKeyDown(Keys.X))
            _cameraDistance += 2f * (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Update camera position: pass target, rotations, and distance
        _camera.Update(_spherePosition, _cameraRotationX, _cameraRotationY, _cameraDistance);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // Draw paper texture as fullscreen background parallax
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
        Rectangle screenRect = new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        _spriteBatch.Draw(_paperTexture, screenRect, Color.White);
        _spriteBatch.End();

        // Enable 3D rendering
        RasterizerState rasterizerState = new RasterizerState();
        rasterizerState.CullMode = CullMode.CullCounterClockwiseFace;
        GraphicsDevice.RasterizerState = rasterizerState;

        // Set up basic effect with camera matrices
        _basicEffect.View = _camera.View;
        _basicEffect.Projection = _camera.Projection;
        _basicEffect.World = Matrix.CreateTranslation(_spherePosition);
        _basicEffect.DiffuseColor = new Vector3(1f, 0f, 0f); // Red sphere

        // Draw the sphere
        GraphicsDevice.SetVertexBuffer(_sphereVertexBuffer);
        GraphicsDevice.Indices = _sphereIndexBuffer;

        foreach (var pass in _basicEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _sphereIndexCount / 3);
        }

        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        PaperBackgroundTexture.Cleanup();
        _sphereVertexBuffer?.Dispose();
        _sphereIndexBuffer?.Dispose();
        _basicEffect?.Dispose();
        base.Dispose(disposing);
    }
}
