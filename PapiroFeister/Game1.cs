using System;
using System.Collections.Generic;
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
    private CraftingUI _craftingUI;
    private Texture2D _pixel;

    private struct AlertMessage
    {
        public string Text;
        public Color TextColor;
        public float Timer;
        public float MaxTime;
        public float Scale;
    }
    private readonly List<AlertMessage> _alerts = new();

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
        RecipeRegistry.Initialize();
        _proceduralFont = new ProceduralFont(GraphicsDevice);
        _inventorySystem = new InventorySystem();
        _inventoryUI = new InventoryUI(GraphicsDevice, _inventorySystem, _proceduralFont);
        _craftingUI = new CraftingUI(GraphicsDevice, _inventorySystem, _proceduralFont, _playerCharacter.Skills);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new Color[] { Color.White });

        // Hook Crafting UI and Skills events for screen notifications
        _craftingUI.OnCraftSuccess += (recipe) =>
        {
            AddAlert($"Crafted {recipe.OutputQuantity}x {recipe.OutputType.Name}!", new Color(60, 160, 60), 3.0f, 1.5f);
            AddAlert($"+{recipe.XpReward} {recipe.RequiredSkill} XP", new Color(255, 99, 71), 2.5f, 1.3f);
        };

        _playerCharacter.Skills.OnLevelUp += (skill, newLevel) =>
        {
            AddAlert($"[LEVEL UP] {skill} reached level {newLevel}!", new Color(255, 201, 14), 4.5f, 1.8f);
        };

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

        // Update active screen alerts
        for (int i = _alerts.Count - 1; i >= 0; i--)
        {
            var alert = _alerts[i];
            alert.Timer -= dt;
            if (alert.Timer <= 0f)
            {
                _alerts.RemoveAt(i);
            }
            else
            {
                _alerts[i] = alert;
            }
        }

        var keyboardState = Keyboard.GetState();
        var mouseState = Mouse.GetState();

        // 1. Update UI and Inventory
        _inventoryUI.Update(mouseState, _previousMouseState, keyboardState, _previousKeyboardState);
        _craftingUI.Update(mouseState, _previousMouseState, keyboardState, _previousKeyboardState);

        // Proximity detection for crafting tables
        PapiroFeister.Worlds.Objects.CraftingTableObject nearbyTable = null;
        float minDistance = 2.2f;
        if (_paperIslandWorld != null && _paperIslandWorld.WorldObjects != null)
        {
            foreach (var obj in _paperIslandWorld.WorldObjects)
            {
                if (obj is PapiroFeister.Worlds.Objects.CraftingTableObject table)
                {
                    float dist = Vector3.Distance(_playerCharacter.Position, table.Position);
                    if (dist < minDistance)
                    {
                        nearbyTable = table;
                        minDistance = dist;
                    }
                }
            }
        }

        // Auto-close crafting if we walk away from the table
        if (_craftingUI.IsOpen)
        {
            bool stillNear = false;
            foreach (var obj in _paperIslandWorld.WorldObjects)
            {
                if (obj is PapiroFeister.Worlds.Objects.CraftingTableObject table && table.TableType == _craftingUI.CurrentTableType)
                {
                    if (Vector3.Distance(_playerCharacter.Position, table.Position) < 2.5f)
                    {
                        stillNear = true;
                        break;
                    }
                }
            }
            if (!stillNear)
            {
                _craftingUI.IsOpen = false;
            }
        }

        // Press F to interact
        if (nearbyTable != null && !_inventoryUI.IsBackpackOpen)
        {
            if (keyboardState.IsKeyDown(Keys.F) && !_previousKeyboardState.IsKeyDown(Keys.F))
            {
                if (_craftingUI.IsOpen)
                {
                    _craftingUI.IsOpen = false;
                }
                else
                {
                    _craftingUI.Open(nearbyTable.TableType);
                }
            }
        }

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

        // 3. Freeze character movement inputs when managing panels (backpack or crafting)
        KeyboardState characterKeyboard = keyboardState;
        if (_inventoryUI.IsBackpackOpen || _craftingUI.IsOpen)
        {
            characterKeyboard = new KeyboardState();
        }
        _playerCharacter.Update(dt, characterKeyboard, _previousKeyboardState, _paperIslandWorld.PlayableHalfSize, _paperIslandWorld.WorldObjects);

        UpdateCamera();
        
        _previousKeyboardState = keyboardState;
        _previousMouseState = mouseState;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        int sw = GraphicsDevice.Viewport.Width;
        int sh = GraphicsDevice.Viewport.Height;

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
        _spriteBatch.Draw(
            _backgroundPaperTexture,
            new Rectangle(0, 0, sw, sh),
            Color.White);
        _spriteBatch.End();

        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.RasterizerState = new RasterizerState { CullMode = CullMode.CullCounterClockwiseFace };

        _paperIslandWorld.Draw(_view, _projection, _cameraPosition, (float)gameTime.TotalGameTime.TotalSeconds);
        _playerCharacter.Draw(_view, _projection, _cameraPosition);

        // Find nearby table for prompt drawing
        PapiroFeister.Worlds.Objects.CraftingTableObject nearbyTable = null;
        float minDistance = 2.2f;
        if (_paperIslandWorld != null && _paperIslandWorld.WorldObjects != null)
        {
            foreach (var obj in _paperIslandWorld.WorldObjects)
            {
                if (obj is PapiroFeister.Worlds.Objects.CraftingTableObject table)
                {
                    float dist = Vector3.Distance(_playerCharacter.Position, table.Position);
                    if (dist < minDistance)
                    {
                        nearbyTable = table;
                        minDistance = dist;
                    }
                }
            }
        }

        // Draw 2D overlays
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        if (nearbyTable != null && !_craftingUI.IsOpen && !_inventoryUI.IsBackpackOpen)
        {
            string labelName = nearbyTable.TableType switch
            {
                CraftingTableType.Workbench => "Workbench (Woodworking)",
                CraftingTableType.Forge => "Forge (Smithing)",
                CraftingTableType.CookingPot => "Cooking Pot (Cooking)",
                CraftingTableType.Loom => "Loom (Tailoring)",
                _ => "Crafting Table"
            };
            string promptText = $"Press [F] to use {labelName}";
            Vector2 sz = _proceduralFont.MeasureString(promptText, 1.5f);
            Vector2 pos = new Vector2((sw - sz.X) / 2f, sh - 150f);

            Rectangle cardRect = new Rectangle((int)pos.X - 12, (int)pos.Y - 6, (int)sz.X + 24, (int)sz.Y + 12);
            _spriteBatch.Draw(_pixel, cardRect, new Color(248, 244, 220));
            DrawRectangleOutline(_spriteBatch, cardRect, new Color(38, 30, 20), 2);

            _proceduralFont.DrawString(_spriteBatch, promptText, pos, new Color(255, 99, 71), 1.5f);
        }

        _inventoryUI.Draw(_spriteBatch);
        _craftingUI.Draw(_spriteBatch);

        // Draw active floating alerts at top-center of screen
        int alertY = 32;
        for (int i = 0; i < _alerts.Count; i++)
        {
            var alert = _alerts[i];
            Vector2 sz = _proceduralFont.MeasureString(alert.Text, alert.Scale);
            Vector2 pos = new Vector2((sw - sz.X) / 2f, alertY);

            // Calculate fade envelope if near expiry
            float alpha = MathHelper.Clamp(alert.Timer / 0.5f, 0f, 1f);
            Color cardColor = new Color(248, 244, 220) * alpha;
            Color borderCol = new Color(38, 30, 20) * alpha;
            Color textCol = alert.TextColor * alpha;

            Rectangle cardRect = new Rectangle((int)pos.X - 16, (int)pos.Y - 6, (int)sz.X + 32, (int)sz.Y + 12);
            _spriteBatch.Draw(_pixel, cardRect, cardColor);
            DrawRectangleOutline(_spriteBatch, cardRect, borderCol, 2);

            _proceduralFont.DrawString(_spriteBatch, alert.Text, pos, textCol, alert.Scale);

            alertY += (int)sz.Y + 18;
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawRectangleOutline(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness = 1)
    {
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
    }

    public void AddAlert(string text, Color color, float duration = 3.5f, float scale = 1.5f)
    {
        _alerts.Add(new AlertMessage
        {
            Text = text,
            TextColor = color,
            Timer = duration,
            MaxTime = duration,
            Scale = scale
        });
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
            _craftingUI?.Dispose();
            _pixel?.Dispose();
            ItemRegistry.Dispose();
        }

        base.Dispose(disposing);
    }
}
