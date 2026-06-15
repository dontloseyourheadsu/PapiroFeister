using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PapiroFeister.Characters.Players;
using PapiroFeister.Utils;

namespace PapiroFeister.Inventory;

public sealed class CraftingUI : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly InventorySystem _inventorySystem;
    private readonly ProceduralFont _font;
    private readonly Texture2D _pixel;

    public bool IsOpen { get; set; } = false;
    public CraftingTableType CurrentTableType { get; set; } = CraftingTableType.Workbench;
    public int SelectedRecipeIndex { get; set; } = 0;
    public PlayerSkills PlayerSkills { get; set; }

    public event Action<CraftingRecipe> OnCraftSuccess;

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
    private static readonly Color RedColor = new Color(220, 60, 60);           // Red pencil check
    private static readonly Color GreenColor = new Color(60, 160, 60);         // Green pencil check

    public CraftingUI(GraphicsDevice graphicsDevice, InventorySystem inventorySystem, ProceduralFont font, PlayerSkills playerSkills)
    {
        _graphicsDevice = graphicsDevice;
        _inventorySystem = inventorySystem;
        _font = font;
        PlayerSkills = playerSkills;

        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new Color[] { Color.White });
    }

    public void Open(CraftingTableType tableType)
    {
        CurrentTableType = tableType;
        SelectedRecipeIndex = 0;
        IsOpen = true;
    }

    public void Update(MouseState mouseState, MouseState previousMouseState, KeyboardState keyboardState, KeyboardState previousKeyboardState)
    {
        if (!IsOpen) return;

        int sw = _graphicsDevice.Viewport.Width;
        int sh = _graphicsDevice.Viewport.Height;

        // Toggle / Close
        if ((keyboardState.IsKeyDown(Keys.Tab) && !previousKeyboardState.IsKeyDown(Keys.Tab)) ||
            (keyboardState.IsKeyDown(Keys.E) && !previousKeyboardState.IsKeyDown(Keys.E)) ||
            (keyboardState.IsKeyDown(Keys.Escape) && !previousKeyboardState.IsKeyDown(Keys.Escape)) ||
            (keyboardState.IsKeyDown(Keys.F) && !previousKeyboardState.IsKeyDown(Keys.F)))
        {
            IsOpen = false;
            return;
        }

        List<CraftingRecipe> recipes = RecipeRegistry.GetRecipesForTable(CurrentTableType);
        if (recipes.Count == 0) return;

        // Left Click checks
        if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
        {
            Point mousePos = mouseState.Position;

            // 1. Check recipe list clicks
            for (int i = 0; i < recipes.Count; i++)
            {
                Rectangle rect = GetRecipeRowRect(i, sw, sh);
                if (rect.Contains(mousePos))
                {
                    SelectedRecipeIndex = i;
                    return;
                }
            }

            // 2. Check craft button click
            if (SelectedRecipeIndex >= 0 && SelectedRecipeIndex < recipes.Count)
            {
                CraftingRecipe selectedRecipe = recipes[SelectedRecipeIndex];
                bool isUnlocked = PlayerSkills.GetLevel(selectedRecipe.RequiredSkill) >= selectedRecipe.RequiredLevel;
                bool hasResources = CanCraft(selectedRecipe);

                if (isUnlocked && hasResources)
                {
                    Rectangle craftBtn = GetCraftButtonRect(sw, sh);
                    if (craftBtn.Contains(mousePos))
                    {
                        PerformCraft(selectedRecipe);
                    }
                }
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsOpen) return;

        int sw = _graphicsDevice.Viewport.Width;
        int sh = _graphicsDevice.Viewport.Height;
        MouseState mouse = Mouse.GetState();

        Rectangle panel = GetPanelRect(sw, sh);

        // Draw shadow backing card
        DrawFilledRectangle(spriteBatch, new Rectangle(panel.X + 6, panel.Y + 6, panel.Width, panel.Height), PaperDarkColor, Color.Transparent, 0);

        // Draw main card
        DrawFilledRectangle(spriteBatch, panel, PaperColor, BorderColor, 3);

        // Header ruled line
        spriteBatch.Draw(_pixel, new Rectangle(panel.X + 10, panel.Y + 52, panel.Width - 20, 2), Color.Lerp(BorderColor, PaperColor, 0.7f));

        // Draw Header Title
        string title = GetTableName(CurrentTableType);
        _font.DrawString(spriteBatch, title, new Vector2(panel.X + 24, panel.Y + 20), TextColor, 2.5f);

        // Draw skill level in header
        string skillName = CurrentTableType switch
        {
            CraftingTableType.Workbench => "WOODWORKING",
            CraftingTableType.Forge => "SMITHING",
            CraftingTableType.CookingPot => "COOKING",
            CraftingTableType.Loom => "TAILORING",
            _ => "CRAFTING"
        };
        int skillLvl = PlayerSkills.GetLevel(CurrentTableType switch
        {
            CraftingTableType.Workbench => SkillType.Woodworking,
            CraftingTableType.Forge => SkillType.Smithing,
            CraftingTableType.CookingPot => SkillType.Cooking,
            CraftingTableType.Loom => SkillType.Tailoring,
            _ => SkillType.Woodworking
        });
        string skillText = $"{skillName} LVL: {skillLvl}";
        Vector2 skillSize = _font.MeasureString(skillText, 1.5f);
        _font.DrawString(spriteBatch, skillText, new Vector2(panel.Right - skillSize.X - 24, panel.Y + 24), HighlightColor, 1.5f);

        // Get recipes
        List<CraftingRecipe> recipes = RecipeRegistry.GetRecipesForTable(CurrentTableType);
        if (recipes.Count == 0)
        {
            _font.DrawString(spriteBatch, "No crafts available.", new Vector2(panel.X + 40, panel.Y + 100), TextMutedColor, 1.5f);
            return;
        }

        // Draw recipe list (Left Panel)
        int dividerX = panel.X + 260;
        spriteBatch.Draw(_pixel, new Rectangle(dividerX, panel.Y + 64, 2, panel.Height - 88), Color.Lerp(BorderColor, PaperColor, 0.7f));

        for (int i = 0; i < recipes.Count; i++)
        {
            CraftingRecipe recipe = recipes[i];
            Rectangle rowRect = GetRecipeRowRect(i, sw, sh);
            bool isSelected = (i == SelectedRecipeIndex);
            bool isHovered = rowRect.Contains(mouse.Position);
            bool isUnlocked = PlayerSkills.GetLevel(recipe.RequiredSkill) >= recipe.RequiredLevel;

            Color rowBg = isSelected ? SlotBgColor : (isHovered ? Color.Lerp(PaperColor, PaperDarkColor, 0.3f) : Color.Transparent);
            Color rowBorder = isSelected ? HighlightColor : BorderColor;
            int borderT = isSelected ? 2 : 0;

            if (rowBg != Color.Transparent)
            {
                DrawFilledRectangle(spriteBatch, rowRect, rowBg, rowBorder, borderT);
            }
            else if (borderT > 0)
            {
                DrawRectangle(spriteBatch, rowRect, rowBorder, borderT);
            }

            // Draw recipe item icon and text
            Rectangle iconRect = new Rectangle(rowRect.X + 6, rowRect.Y + 6, 28, 28);
            spriteBatch.Draw(recipe.OutputType.IconTexture, iconRect, isUnlocked ? Color.White : Color.Black * 0.4f);

            string text = recipe.OutputType.Name;
            if (!isUnlocked)
            {
                text += " [LOCKED]";
            }
            _font.DrawString(spriteBatch, text, new Vector2(rowRect.X + 40, rowRect.Y + 12), isUnlocked ? TextColor : TextMutedColor, 1.25f);
        }

        // Draw details (Right Panel)
        if (SelectedRecipeIndex >= 0 && SelectedRecipeIndex < recipes.Count)
        {
            CraftingRecipe selectedRecipe = recipes[SelectedRecipeIndex];
            bool isUnlocked = PlayerSkills.GetLevel(selectedRecipe.RequiredSkill) >= selectedRecipe.RequiredLevel;

            int detailsX = dividerX + 20;
            int detailsY = panel.Y + 74;

            // Output Icon
            Rectangle detailIconRect = new Rectangle(detailsX, detailsY, 64, 64);
            DrawFilledRectangle(spriteBatch, detailIconRect, SlotBgColor, BorderColor, 2);
            spriteBatch.Draw(selectedRecipe.OutputType.IconTexture, new Rectangle(detailsX + 8, detailsY + 8, 48, 48), isUnlocked ? Color.White : Color.Black * 0.4f);

            // Name
            _font.DrawString(spriteBatch, selectedRecipe.OutputType.Name, new Vector2(detailsX + 80, detailsY + 8), isUnlocked ? selectedRecipe.OutputType.ThemeColor : TextMutedColor, 2f);
            
            // Description
            _font.DrawString(spriteBatch, selectedRecipe.OutputType.Description, new Vector2(detailsX, detailsY + 80), isUnlocked ? TextColor : TextMutedColor, 1.4f);

            if (!isUnlocked)
            {
                // Locked Alert
                string lockedText = $"Locked: Requires {GetSkillLabel(selectedRecipe.RequiredSkill)} Level {selectedRecipe.RequiredLevel}";
                _font.DrawString(spriteBatch, lockedText, new Vector2(detailsX, detailsY + 120), RedColor, 1.5f);
            }
            else
            {
                // Ingredients List
                _font.DrawString(spriteBatch, "Ingredients Required:", new Vector2(detailsX, detailsY + 114), TextMutedColor, 1.35f);

                int ingY = detailsY + 134;
                bool hasAll = true;

                foreach (var ingredient in selectedRecipe.Ingredients)
                {
                    int owned = _inventorySystem.GetItemCount(ingredient.Type);
                    bool hasEnough = owned >= ingredient.Quantity;
                    if (!hasEnough) hasAll = false;

                    Color countCol = hasEnough ? GreenColor : RedColor;

                    // Draw tiny icon slot
                    Rectangle ingIconRect = new Rectangle(detailsX, ingY - 2, 20, 20);
                    spriteBatch.Draw(ingredient.Type.IconTexture, ingIconRect, Color.White);

                    string ingText = $"{ingredient.Type.Name} :";
                    _font.DrawString(spriteBatch, ingText, new Vector2(detailsX + 28, ingY), TextColor, 1.25f);

                    string qtyText = $"{owned} / {ingredient.Quantity}";
                    _font.DrawString(spriteBatch, qtyText, new Vector2(detailsX + 200, ingY), countCol, 1.25f);

                    ingY += 22;
                }

                // XP Reward info
                _font.DrawString(spriteBatch, $"+{selectedRecipe.XpReward} {GetSkillLabel(selectedRecipe.RequiredSkill)} XP", new Vector2(detailsX, panel.Bottom - 44), HighlightColor, 1.25f);

                // Craft Button
                Rectangle btnRect = GetCraftButtonRect(sw, sh);
                bool btnHovered = btnRect.Contains(mouse.Position);
                bool craftable = hasAll;

                Color btnCol = craftable ? (btnHovered ? ButtonHoverColor : ButtonColor) : PaperDarkColor;
                Color textCol = craftable ? TextColor : TextMutedColor;

                DrawFilledRectangle(spriteBatch, btnRect, btnCol, BorderColor, 2);
                string btnText = "CRAFT ITEM";
                Vector2 btnTextSz = _font.MeasureString(btnText, 1.5f);
                Vector2 btnTextPos = new Vector2(btnRect.X + (btnRect.Width - btnTextSz.X) / 2f, btnRect.Y + (btnRect.Height - btnTextSz.Y) / 2f);
                _font.DrawString(spriteBatch, btnText, btnTextPos, textCol, 1.5f);
            }
        }
    }

    private bool CanCraft(CraftingRecipe recipe)
    {
        foreach (var ingredient in recipe.Ingredients)
        {
            if (_inventorySystem.GetItemCount(ingredient.Type) < ingredient.Quantity)
            {
                return false;
            }
        }
        return true;
    }

    private void PerformCraft(CraftingRecipe recipe)
    {
        // Deduct materials
        foreach (var ingredient in recipe.Ingredients)
        {
            _inventorySystem.RemoveItems(ingredient.Type, ingredient.Quantity);
        }

        // Award XP
        PlayerSkills.AddXP(recipe.RequiredSkill, recipe.XpReward, out bool leveledUp);

        // Special handling: Loom Backpack Upgrade (we chose Compass item slot as a placeholder for recipe but we cycle upgrade level directly)
        if (recipe.TableType == CraftingTableType.Loom && recipe.OutputType.Id == ItemRegistry.Compass.Id)
        {
            _inventorySystem.CycleUpgrade();
            // We don't add the compass item to inventory; the backpack itself is upgraded!
        }
        else
        {
            // Add output item
            _inventorySystem.AddItem(new Item(recipe.OutputType, recipe.OutputQuantity));
        }

        // Trigger event
        OnCraftSuccess?.Invoke(recipe);
    }

    private Rectangle GetPanelRect(int sw, int sh)
    {
        int panelWidth = 640;
        int panelHeight = 440;
        int panelX = (sw - panelWidth) / 2;
        int panelY = (sh - panelHeight) / 2 - 20;
        return new Rectangle(panelX, panelY, panelWidth, panelHeight);
    }

    private Rectangle GetRecipeRowRect(int index, int sw, int sh)
    {
        Rectangle panel = GetPanelRect(sw, sh);
        int rowHeight = 40;
        int startX = panel.X + 16;
        int startY = panel.Y + 70;
        return new Rectangle(startX, startY + index * rowHeight, 228, rowHeight - 4);
    }

    private Rectangle GetCraftButtonRect(int sw, int sh)
    {
        Rectangle panel = GetPanelRect(sw, sh);
        // Positioned in bottom-right corner of details panel
        return new Rectangle(panel.Right - 200 - 24, panel.Bottom - 52, 200, 36);
    }

    private string GetTableName(CraftingTableType type)
    {
        return type switch
        {
            CraftingTableType.Workbench => "CRAFTING WORKBENCH",
            CraftingTableType.Forge => "FORGE & ANVIL",
            CraftingTableType.CookingPot => "COOKING STOVE/POT",
            CraftingTableType.Loom => "TAILORING LOOM",
            _ => "CRAFTING TABLE"
        };
    }

    private string GetSkillLabel(SkillType skill)
    {
        return skill switch
        {
            SkillType.Woodworking => "Woodworking",
            SkillType.Smithing => "Smithing",
            SkillType.Cooking => "Cooking",
            SkillType.Tailoring => "Tailoring",
            _ => "Crafting"
        };
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
