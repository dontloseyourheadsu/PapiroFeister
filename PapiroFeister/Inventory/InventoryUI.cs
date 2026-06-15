using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PapiroFeister.Utils;

namespace PapiroFeister.Inventory;

public sealed class InventoryUI : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly InventorySystem _inventorySystem;
    private readonly ProceduralFont _font;
    private readonly Texture2D _pixel;

    public bool IsBackpackOpen { get; set; } = false;
    private Item _heldItem = null;

    // UI Colors matching paper-cartoon / sketchpad aesthetic
    private static readonly Color PaperColor = new Color(248, 244, 220);      // Sketchbook page
    private static readonly Color PaperDarkColor = new Color(230, 222, 192);  // Cardboard/shadow
    private static readonly Color BorderColor = new Color(38, 30, 20);         // Charcoal/pencil black
    private static readonly Color SlotBgColor = new Color(238, 234, 208);      // Inset slot background
    private static readonly Color HighlightColor = new Color(255, 99, 71);     // Tomato red pencil marker
    private static readonly Color TextColor = new Color(48, 40, 30);           // Charcoal dark text
    private static readonly Color TextMutedColor = new Color(110, 105, 95);    // Graphite gray text
    private static readonly Color ButtonColor = new Color(162, 210, 162);      // Light green crayon
    private static readonly Color ButtonHoverColor = new Color(192, 230, 192); // Bright green crayon

    public InventoryUI(GraphicsDevice graphicsDevice, InventorySystem inventorySystem, ProceduralFont font)
    {
        _graphicsDevice = graphicsDevice;
        _inventorySystem = inventorySystem;
        _font = font;

        // Create 1x1 white texture for drawing solid colored boxes
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new Color[] { Color.White });
    }

    public void Update(MouseState mouseState, MouseState previousMouseState, KeyboardState keyboardState, KeyboardState previousKeyboardState)
    {
        int sw = _graphicsDevice.Viewport.Width;
        int sh = _graphicsDevice.Viewport.Height;

        // Toggle backpack
        if ((keyboardState.IsKeyDown(Keys.Tab) && !previousKeyboardState.IsKeyDown(Keys.Tab)) ||
            (keyboardState.IsKeyDown(Keys.E) && !previousKeyboardState.IsKeyDown(Keys.E)))
        {
            IsBackpackOpen = !IsBackpackOpen;
        }

        // Hotbar numeric selection
        for (int i = 0; i < InventorySystem.HotbarSize; i++)
        {
            Keys key = (Keys)((int)Keys.D1 + i);
            if (keyboardState.IsKeyDown(key) && !previousKeyboardState.IsKeyDown(key))
            {
                _inventorySystem.SelectedHotbarIndex = i;
            }
        }

        // Scroll wheel selection
        int scrollDiff = mouseState.ScrollWheelValue - previousMouseState.ScrollWheelValue;
        if (scrollDiff != 0)
        {
            int change = scrollDiff > 0 ? -1 : 1;
            int nextSlot = (_inventorySystem.SelectedHotbarIndex + change) % InventorySystem.HotbarSize;
            if (nextSlot < 0) nextSlot += InventorySystem.HotbarSize;
            _inventorySystem.SelectedHotbarIndex = nextSlot;
        }

        // Left click interactions
        if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
        {
            HandleLeftClick(mouseState.Position, sw, sh);
        }

        // Right click interactions (Split stack / Place 1)
        if (mouseState.RightButton == ButtonState.Pressed && previousMouseState.RightButton == ButtonState.Released)
        {
            HandleRightClick(mouseState.Position, sw, sh);
        }
    }

    private void HandleLeftClick(Point mousePos, int sw, int sh)
    {
        // 1. Upgrade button click
        if (IsBackpackOpen && _inventorySystem.UpgradeLevel < 3)
        {
            Rectangle upgradeBtn = GetUpgradeButtonRect(sw, sh);
            if (upgradeBtn.Contains(mousePos))
            {
                _inventorySystem.CycleUpgrade();
                return;
            }
        }

        // 2. Check slots click
        int clickedIndex = -1;
        bool clickedIsHotbar = false;

        // Check hotbar
        for (int i = 0; i < InventorySystem.HotbarSize; i++)
        {
            if (GetHotbarSlotRect(i, sw, sh).Contains(mousePos))
            {
                clickedIndex = i;
                clickedIsHotbar = true;
                break;
            }
        }

        // Check backpack
        if (IsBackpackOpen && clickedIndex == -1)
        {
            for (int i = 0; i < _inventorySystem.BackpackCapacity; i++)
            {
                if (GetBackpackSlotRect(i, sw, sh).Contains(mousePos))
                {
                    clickedIndex = i;
                    clickedIsHotbar = false;
                    break;
                }
            }
        }

        if (clickedIndex != -1)
        {
            // Swap held item and slot item
            if (clickedIsHotbar)
            {
                Item temp = _inventorySystem.GetHotbarItem(clickedIndex);
                _inventorySystem.SetHotbarItem(clickedIndex, _heldItem);
                _heldItem = temp;
            }
            else
            {
                Item temp = _inventorySystem.GetBackpackItem(clickedIndex);
                _inventorySystem.SetBackpackItem(clickedIndex, _heldItem);
                _heldItem = temp;
            }
        }
        else if (_heldItem != null)
        {
            // Clicked outside slots, check if we should drop the item
            Rectangle panelRect = GetBackpackPanelRect(sw, sh);
            Rectangle hotbarRect = GetHotbarRect(sw, sh);
            bool outsidePanel = !panelRect.Contains(mousePos);
            bool outsideHotbar = !hotbarRect.Contains(mousePos);

            if (outsideHotbar && (!IsBackpackOpen || outsidePanel))
            {
                // Discard / drop
                _heldItem = null;
            }
        }
    }

    private void HandleRightClick(Point mousePos, int sw, int sh)
    {
        int clickedIndex = -1;
        bool clickedIsHotbar = false;

        // Check hotbar
        for (int i = 0; i < InventorySystem.HotbarSize; i++)
        {
            if (GetHotbarSlotRect(i, sw, sh).Contains(mousePos))
            {
                clickedIndex = i;
                clickedIsHotbar = true;
                break;
            }
        }

        // Check backpack
        if (IsBackpackOpen && clickedIndex == -1)
        {
            for (int i = 0; i < _inventorySystem.BackpackCapacity; i++)
            {
                if (GetBackpackSlotRect(i, sw, sh).Contains(mousePos))
                {
                    clickedIndex = i;
                    clickedIsHotbar = false;
                    break;
                }
            }
        }

        if (clickedIndex != -1)
        {
            Item slotItem = clickedIsHotbar ? _inventorySystem.GetHotbarItem(clickedIndex) : _inventorySystem.GetBackpackItem(clickedIndex);

            if (_heldItem == null)
            {
                // Pick up half of slotItem
                if (slotItem != null && slotItem.Quantity > 1)
                {
                    int take = slotItem.Quantity / 2;
                    int remain = slotItem.Quantity - take;
                    
                    _heldItem = slotItem.Clone(take);
                    slotItem.Quantity = remain;
                }
                else if (slotItem != null && slotItem.Quantity == 1)
                {
                    // Take the single item
                    _heldItem = slotItem;
                    if (clickedIsHotbar)
                        _inventorySystem.SetHotbarItem(clickedIndex, null);
                    else
                        _inventorySystem.SetBackpackItem(clickedIndex, null);
                }
            }
            else
            {
                // Place 1 of held item into slot
                if (slotItem == null)
                {
                    Item placed = _heldItem.Clone(1);
                    if (clickedIsHotbar)
                        _inventorySystem.SetHotbarItem(clickedIndex, placed);
                    else
                        _inventorySystem.SetBackpackItem(clickedIndex, placed);

                    _heldItem.Quantity--;
                    if (_heldItem.Quantity <= 0)
                        _heldItem = null;
                }
                else if (slotItem.Type.Id == _heldItem.Type.Id && slotItem.Quantity < slotItem.Type.MaxStack)
                {
                    slotItem.Quantity++;
                    _heldItem.Quantity--;
                    if (_heldItem.Quantity <= 0)
                        _heldItem = null;
                }
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        int sw = _graphicsDevice.Viewport.Width;
        int sh = _graphicsDevice.Viewport.Height;
        MouseState mouse = Mouse.GetState();

        // 1. Draw Hotbar HUD
        DrawHotbar(spriteBatch, sw, sh, mouse);

        // 2. Draw Backpack Panel if open
        Item hoveredItem = null;
        if (IsBackpackOpen)
        {
            hoveredItem = DrawBackpackPanel(spriteBatch, sw, sh, mouse);
        }

        // Draw instructions at top-left
        DrawHUDInstructions(spriteBatch);

        // Draw active hotbar item description just above the hotbar (if backpack is closed)
        if (!IsBackpackOpen)
        {
            Item selected = _inventorySystem.GetHotbarItem(_inventorySystem.SelectedHotbarIndex);
            if (selected != null)
            {
                string label = $"{selected.Type.Name} - {selected.Type.Description}";
                Vector2 textSize = _font.MeasureString(label, 1.5f);
                Rectangle hb = GetHotbarRect(sw, sh);
                Vector2 textPos = new Vector2((sw - textSize.X) / 2f, hb.Y - 26);
                
                // Draw paper backing for name label
                Rectangle backRect = new Rectangle((int)textPos.X - 10, (int)textPos.Y - 4, (int)textSize.X + 20, (int)textSize.Y + 8);
                DrawFilledRectangle(spriteBatch, backRect, PaperColor, BorderColor, 2);
                _font.DrawString(spriteBatch, label, textPos, TextColor, 1.5f);
            }
        }

        // 3. Draw Hovered item details if any (when backpack is open)
        if (IsBackpackOpen)
        {
            DrawHoveredDetails(spriteBatch, sw, sh, hoveredItem);
        }

        // 4. Draw Floating Held Item on cursor
        if (_heldItem != null)
        {
            Rectangle dest = new Rectangle(mouse.X - 24, mouse.Y - 24, 48, 48);
            spriteBatch.Draw(_heldItem.Type.IconTexture, dest, Color.White);
            if (_heldItem.Quantity > 1)
            {
                string qStr = _heldItem.Quantity.ToString();
                _font.DrawString(spriteBatch, qStr, new Vector2(mouse.X + 6, mouse.Y + 6), Color.White, 1.5f);
                _font.DrawString(spriteBatch, qStr, new Vector2(mouse.X + 5, mouse.Y + 5), BorderColor, 1.5f);
            }
        }
    }

    private void DrawHotbar(SpriteBatch spriteBatch, int sw, int sh, MouseState mouse)
    {
        Rectangle hbRect = GetHotbarRect(sw, sh);
        
        // Draw hotbar panel background (sketchbook card style)
        Rectangle bgCard = new Rectangle(hbRect.X - 12, hbRect.Y - 12, hbRect.Width + 24, hbRect.Height + 24);
        DrawFilledRectangle(spriteBatch, bgCard, PaperColor, BorderColor, 3);

        // Draw slots
        for (int i = 0; i < InventorySystem.HotbarSize; i++)
        {
            Rectangle slotRect = GetHotbarSlotRect(i, sw, sh);
            Item item = _inventorySystem.GetHotbarItem(i);
            bool isSelected = (i == _inventorySystem.SelectedHotbarIndex);

            // Background slot box
            DrawFilledRectangle(spriteBatch, slotRect, SlotBgColor, BorderColor, isSelected ? 3 : 2);
            
            // Draw red marker highlight if selected
            if (isSelected)
            {
                Rectangle outline = new Rectangle(slotRect.X - 2, slotRect.Y - 2, slotRect.Width + 4, slotRect.Height + 4);
                DrawRectangle(spriteBatch, outline, HighlightColor, 2);
            }

            // Draw Item
            if (item != null)
            {
                Rectangle iconRect = new Rectangle(slotRect.X + 8, slotRect.Y + 8, 48, 48);
                spriteBatch.Draw(item.Type.IconTexture, iconRect, Color.White);
                
                // Quantity
                if (item.Quantity > 1)
                {
                    string qStr = item.Quantity.ToString();
                    _font.DrawString(spriteBatch, qStr, new Vector2(slotRect.X + 46, slotRect.Y + 46), Color.White, 1.5f);
                    _font.DrawString(spriteBatch, qStr, new Vector2(slotRect.X + 45, slotRect.Y + 45), BorderColor, 1.5f);
                }
            }

            // Draw slot hotkey number
            string keyStr = (i + 1).ToString();
            _font.DrawString(spriteBatch, keyStr, new Vector2(slotRect.X + 4, slotRect.Y + 4), TextMutedColor, 1f);
        }
    }

    private Item DrawBackpackPanel(SpriteBatch spriteBatch, int sw, int sh, MouseState mouse)
    {
        Rectangle panelRect = GetBackpackPanelRect(sw, sh);

        // Draw shadow backing card
        DrawFilledRectangle(spriteBatch, new Rectangle(panelRect.X + 6, panelRect.Y + 6, panelRect.Width, panelRect.Height), PaperDarkColor, Color.Transparent, 0);

        // Draw main card
        DrawFilledRectangle(spriteBatch, panelRect, PaperColor, BorderColor, 3);

        // Ruled line decoration at top of backpack to fit paper island notebook theme
        spriteBatch.Draw(_pixel, new Rectangle(panelRect.X + 10, panelRect.Y + 52, panelRect.Width - 20, 2), Color.Lerp(BorderColor, PaperColor, 0.7f));

        // Draw Header Title
        string title = "MY BACKPACK";
        _font.DrawString(spriteBatch, title, new Vector2(panelRect.X + 24, panelRect.Y + 20), TextColor, 2.5f);

        // Draw capacity info
        int usedSlots = 0;
        for (int i = 0; i < _inventorySystem.BackpackCapacity; i++)
            if (_inventorySystem.GetBackpackItem(i) != null) usedSlots++;
        string capacityText = $"{usedSlots}/{_inventorySystem.BackpackCapacity}";
        Vector2 capSize = _font.MeasureString(capacityText, 1.5f);
        _font.DrawString(spriteBatch, capacityText, new Vector2(panelRect.Right - capSize.X - 24, panelRect.Y + 24), TextMutedColor, 1.5f);

        Item hovered = null;

        // Draw Slots Grid
        for (int i = 0; i < _inventorySystem.BackpackCapacity; i++)
        {
            Rectangle slotRect = GetBackpackSlotRect(i, sw, sh);
            Item item = _inventorySystem.GetBackpackItem(i);
            bool isHovered = slotRect.Contains(mouse.Position);

            // Draw Slot outline
            DrawFilledRectangle(spriteBatch, slotRect, SlotBgColor, BorderColor, isHovered ? 3 : 2);
            if (isHovered)
            {
                hovered = item;
                Rectangle hoverOutline = new Rectangle(slotRect.X - 1, slotRect.Y - 1, slotRect.Width + 2, slotRect.Height + 2);
                DrawRectangle(spriteBatch, hoverOutline, HighlightColor, 1);
            }

            // Draw Item Icon
            if (item != null)
            {
                Rectangle iconRect = new Rectangle(slotRect.X + 8, slotRect.Y + 8, 48, 48);
                spriteBatch.Draw(item.Type.IconTexture, iconRect, Color.White);

                // Draw Quantity
                if (item.Quantity > 1)
                {
                    string qStr = item.Quantity.ToString();
                    _font.DrawString(spriteBatch, qStr, new Vector2(slotRect.X + 46, slotRect.Y + 46), Color.White, 1.5f);
                    _font.DrawString(spriteBatch, qStr, new Vector2(slotRect.X + 45, slotRect.Y + 45), BorderColor, 1.5f);
                }
            }
        }

        // Draw Upgrade Button
        Rectangle btnRect = GetUpgradeButtonRect(sw, sh);
        bool btnHovered = btnRect.Contains(mouse.Position);
        Color btnCol = btnHovered ? ButtonHoverColor : ButtonColor;
        
        string btnText = "";
        int currentLevel = _inventorySystem.UpgradeLevel;
        if (currentLevel == 0) btnText = "UPGRADE (+8 slots) - level 1";
        else if (currentLevel == 1) btnText = "UPGRADE (+16 slots) - level 2";
        else if (currentLevel == 2) btnText = "UPGRADE (+24 slots) - level 3";
        else btnText = "MAXIMUM UPGRADE (+24 slots)";

        if (currentLevel < 3)
        {
            DrawFilledRectangle(spriteBatch, btnRect, btnCol, BorderColor, 2);
            Vector2 textSz = _font.MeasureString(btnText, 1.5f);
            Vector2 textPos = new Vector2(btnRect.X + (btnRect.Width - textSz.X) / 2f, btnRect.Y + (btnRect.Height - textSz.Y) / 2f);
            _font.DrawString(spriteBatch, btnText, textPos, TextColor, 1.5f);
        }
        else
        {
            // Disabled upgrade button
            DrawFilledRectangle(spriteBatch, btnRect, PaperDarkColor, BorderColor, 2);
            Vector2 textSz = _font.MeasureString(btnText, 1.5f);
            Vector2 textPos = new Vector2(btnRect.X + (btnRect.Width - textSz.X) / 2f, btnRect.Y + (btnRect.Height - textSz.Y) / 2f);
            _font.DrawString(spriteBatch, btnText, textPos, TextMutedColor, 1.5f);
        }

        return hovered;
    }

    private void DrawHoveredDetails(SpriteBatch spriteBatch, int sw, int sh, Item item)
    {
        Rectangle panel = GetBackpackPanelRect(sw, sh);
        
        // Draw a separator line above details
        spriteBatch.Draw(_pixel, new Rectangle(panel.X + 10, panel.Y + panel.Height - 118, panel.Width - 20, 2), Color.Lerp(BorderColor, PaperColor, 0.7f));

        int detailsX = panel.X + 24;
        int detailsY = panel.Y + panel.Height - 106;

        if (item != null)
        {
            // Name
            string name = item.Type.Name;
            _font.DrawString(spriteBatch, name, new Vector2(detailsX, detailsY), item.Type.ThemeColor, 2f);

            // Category tag
            string cat = $"[{item.Type.Category.ToString().ToUpper()}]";
            _font.DrawString(spriteBatch, cat, new Vector2(detailsX + 160, detailsY + 4), TextMutedColor, 1.25f);

            // Description
            string desc = item.Type.Description;
            _font.DrawString(spriteBatch, desc, new Vector2(detailsX, detailsY + 24), TextColor, 1.5f);
        }
        else
        {
            _font.DrawString(spriteBatch, "Hover over an item for details.", new Vector2(detailsX, detailsY + 12), TextMutedColor, 1.5f);
        }
    }

    private void DrawHUDInstructions(SpriteBatch spriteBatch)
    {
        int startX = 16;
        int startY = 16;
        
        string[] instructions = new string[] {
            "KEYS & CHEATS:",
            "  Tab / E  : Toggle Backpack",
            "  1 - 8    : Select Hotbar Item",
            "  F        : Use Crafting Table (when near)",
            "  I        : Give Crafting Materials (Cheat)",
            "  L        : Gain 100 Skill XP (Cheat)",
            "  G        : Gain random test item",
            "  U        : Cycle Backpack upgrade",
            "  C        : Clear inventory slots",
            "  Q        : Drop selected item",
            "  Left Clk : Interact with UIs",
            "  Right Clk: Split slot item"
        };

        // Draw card background for instructions
        int width = 360;
        int height = 202;
        DrawFilledRectangle(spriteBatch, new Rectangle(startX, startY, width, height), PaperColor, BorderColor, 2);

        for (int i = 0; i < instructions.Length; i++)
        {
            float scale = instructions[i].StartsWith(" ") ? 1.2f : 1.35f;
            Color col = instructions[i].EndsWith(":") ? HighlightColor : (instructions[i].StartsWith(" ") ? TextColor : TextMutedColor);
            _font.DrawString(spriteBatch, instructions[i], new Vector2(startX + 12, startY + 8 + i * 15), col, scale);
        }
    }

    private Rectangle GetHotbarRect(int sw, int sh)
    {
        int slotSize = 64;
        int padding = 8;
        int totalWidth = InventorySystem.HotbarSize * slotSize + (InventorySystem.HotbarSize - 1) * padding;
        int startX = (sw - totalWidth) / 2;
        int startY = sh - slotSize - 20;
        return new Rectangle(startX, startY, totalWidth, slotSize);
    }

    private Rectangle GetHotbarSlotRect(int index, int sw, int sh)
    {
        Rectangle hbRect = GetHotbarRect(sw, sh);
        int slotSize = 64;
        int padding = 8;
        int offset = (index == _inventorySystem.SelectedHotbarIndex) ? -8 : 0;
        return new Rectangle(hbRect.X + index * (slotSize + padding), hbRect.Y + offset, slotSize, slotSize);
    }

    private Rectangle GetBackpackPanelRect(int sw, int sh)
    {
        int panelWidth = 640;
        int panelHeight = 440;
        int panelX = (sw - panelWidth) / 2;
        int panelY = (sh - panelHeight) / 2 - 20;
        return new Rectangle(panelX, panelY, panelWidth, panelHeight);
    }

    private Rectangle GetBackpackSlotRect(int index, int sw, int sh)
    {
        Rectangle panel = GetBackpackPanelRect(sw, sh);
        int slotSize = 64;
        int padding = 8;
        int cols = 8;
        int gridWidth = cols * slotSize + (cols - 1) * padding;
        int startX = panel.X + (panel.Width - gridWidth) / 2;
        int startY = panel.Y + 70;
        
        int col = index % cols;
        int row = index / cols;
        return new Rectangle(startX + col * (slotSize + padding), startY + row * (slotSize + padding), slotSize, slotSize);
    }

    private Rectangle GetUpgradeButtonRect(int sw, int sh)
    {
        Rectangle panel = GetBackpackPanelRect(sw, sh);
        // Positioned in bottom-right corner of details panel
        return new Rectangle(panel.Right - 320 - 24, panel.Bottom - 52, 320, 36);
    }

    private void DrawRectangle(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness = 1)
    {
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
    }

    private void DrawFilledRectangle(SpriteBatch spriteBatch, Rectangle rect, Color fillColor, Color borderColor, int borderThickness = 1)
    {
        spriteBatch.Draw(_pixel, rect, fillColor);
        if (borderThickness > 0)
        {
            DrawRectangle(spriteBatch, rect, borderColor, borderThickness);
        }
    }

    public void Dispose()
    {
        _pixel?.Dispose();
    }
}
