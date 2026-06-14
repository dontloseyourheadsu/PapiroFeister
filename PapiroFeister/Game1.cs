using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PapiroFeister.Characters.Players;
using PapiroFeister.Textures.Generators;
using PapiroFeister.Worlds.Islands;
using PapiroFeister.Inventory;
using PapiroFeister.Utils;

namespace PapiroFeister;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _backgroundPaperTexture;

    private PaperIslandWorld _paperIslandWorld;
    private PlayerCharacter _playerCharacter;

    private Matrix _view;
    private Matrix _projection;

    private Vector3 _cameraPosition;
    private KeyboardState _previousKeyboardState;
    private MouseState _previousMouseState;

    private ProceduralFont _proceduralFont;
    private InventorySystem _inventorySystem;
    private InventoryUI _inventoryUI;

    private const float CameraHorizontalDistance = 7f;
    private const float CameraFloorAngleDeg = 44f;

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
        _paperIslandWorld = new PaperIslandWorld(GraphicsDevice, playableHalfSize: 24f, oceanHalfSize: 80f);
        _playerCharacter = new PlayerCharacter(GraphicsDevice, _paperIslandWorld.PlayableHalfSize);

        // Initialize Inventory System
        ItemRegistry.Initialize(GraphicsDevice);
        _proceduralFont = new ProceduralFont(GraphicsDevice);
        _inventorySystem = new InventorySystem();
        _inventoryUI = new InventoryUI(GraphicsDevice, _inventorySystem, _proceduralFont);

        // Setup starting inventory: 8 quick slots + backpack items
        _inventorySystem.AddItem(new Item(ItemRegistry.WoodenSword));
        _inventorySystem.AddItem(new Item(ItemRegistry.SteelPickaxe));
        _inventorySystem.AddItem(new Item(ItemRegistry.GoldenAxe));
        _inventorySystem.AddItem(new Item(ItemRegistry.PaperShovel));
        _inventorySystem.AddItem(new Item(ItemRegistry.HealingPotion, 5));
        _inventorySystem.AddItem(new Item(ItemRegistry.MagicScroll, 3));
        _inventorySystem.AddItem(new Item(ItemRegistry.FishingRod));
        _inventorySystem.AddItem(new Item(ItemRegistry.Compass));

        UpdateCamera();
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var keyboardState = Keyboard.GetState();
        var mouseState = Mouse.GetState();

        // 1. Update UI and Inventory
        _inventoryUI.Update(mouseState, _previousMouseState, keyboardState, _previousKeyboardState);

        // 2. Handle cheat/test shortcut keys
        if (keyboardState.IsKeyDown(Keys.G) && !_previousKeyboardState.IsKeyDown(Keys.G))
        {
            // Gain a random test item
            var type = ItemRegistry.GetRandomType();
            int qty = type.MaxStack > 1 ? new Random().Next(1, 10) : 1;
            _inventorySystem.AddItem(new Item(type, qty));
        }

        if (keyboardState.IsKeyDown(Keys.U) && !_previousKeyboardState.IsKeyDown(Keys.U))
        {
            // Cycle backpack upgrade level
            _inventorySystem.CycleUpgrade();
        }

        if (keyboardState.IsKeyDown(Keys.C) && !_previousKeyboardState.IsKeyDown(Keys.C))
        {
            // Clear inventory
            _inventorySystem.ClearInventory();
        }

        if (keyboardState.IsKeyDown(Keys.Q) && !_previousKeyboardState.IsKeyDown(Keys.Q))
        {
            // Drop/consume 1 from selected slot
            _inventorySystem.RemoveSelectedItem(1);
        }

        // 3. Freeze character movement inputs when managing the backpack panel
        KeyboardState characterKeyboard = keyboardState;
        if (_inventoryUI.IsBackpackOpen)
        {
            characterKeyboard = new KeyboardState();
        }
        _playerCharacter.Update(dt, characterKeyboard, _previousKeyboardState, _paperIslandWorld.PlayableHalfSize);

        UpdateCamera();
        
        _previousKeyboardState = keyboardState;
        _previousMouseState = mouseState;

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

        _paperIslandWorld.Draw(_view, _projection, _cameraPosition, (float)gameTime.TotalGameTime.TotalSeconds);
        _playerCharacter.Draw(_view, _projection, _cameraPosition);

        // Draw 2D Inventory Overlay
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        _inventoryUI.Draw(_spriteBatch);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void UpdateCamera()
    {
        Vector3 localUp = Vector3.Up;
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            PaperTextureGenerator.Cleanup();
            _spriteBatch?.Dispose();
            _paperIslandWorld?.Dispose();
            _playerCharacter?.Dispose();

            // Clean up inventory system textures
            _proceduralFont?.Dispose();
            _inventoryUI?.Dispose();
            ItemRegistry.Dispose();
        }

        base.Dispose(disposing);
    }
}
