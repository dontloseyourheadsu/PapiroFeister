using System;
using System.Collections.Generic;
using PapiroFeister.Characters.Players;

namespace PapiroFeister.Inventory;

public enum CraftingTableType
{
    Workbench,
    Forge,
    CookingPot,
    Loom
}

public struct CraftingIngredient
{
    public ItemType Type { get; }
    public int Quantity { get; }

    public CraftingIngredient(ItemType type, int quantity)
    {
        Type = type;
        Quantity = quantity;
    }
}

public sealed class CraftingRecipe
{
    public int Id { get; }
    public ItemType OutputType { get; }
    public int OutputQuantity { get; }
    public List<CraftingIngredient> Ingredients { get; }
    public CraftingTableType TableType { get; }
    public SkillType RequiredSkill { get; }
    public int RequiredLevel { get; }
    public int XpReward { get; }

    public CraftingRecipe(
        int id,
        ItemType outputType,
        int outputQuantity,
        List<CraftingIngredient> ingredients,
        CraftingTableType tableType,
        SkillType requiredSkill,
        int requiredLevel,
        int xpReward)
    {
        Id = id;
        OutputType = outputType;
        OutputQuantity = outputQuantity;
        Ingredients = ingredients;
        TableType = tableType;
        RequiredSkill = requiredSkill;
        RequiredLevel = requiredLevel;
        XpReward = xpReward;
    }
}

public static class RecipeRegistry
{
    private static readonly List<CraftingRecipe> _recipes = new();

    public static IReadOnlyList<CraftingRecipe> Recipes => _recipes;

    public static void Initialize()
    {
        _recipes.Clear();

        int id = 0;

        // --- WORKBENCH (WOODWORKING) ---
        _recipes.Add(new CraftingRecipe(
            id++,
            ItemRegistry.WoodenSword, 1,
            new List<CraftingIngredient> { new(ItemRegistry.Wood, 3) },
            CraftingTableType.Workbench,
            SkillType.Woodworking, 1,
            15
        ));

        _recipes.Add(new CraftingRecipe(
            id++,
            ItemRegistry.PaperShovel, 1,
            new List<CraftingIngredient> { new(ItemRegistry.Wood, 2), new(ItemRegistry.Fiber, 2) },
            CraftingTableType.Workbench,
            SkillType.Woodworking, 1,
            15
        ));

        _recipes.Add(new CraftingRecipe(
            id++,
            ItemRegistry.FishingRod, 1,
            new List<CraftingIngredient> { new(ItemRegistry.Wood, 3), new(ItemRegistry.PaperRope, 2) },
            CraftingTableType.Workbench,
            SkillType.Woodworking, 1,
            20
        ));

        _recipes.Add(new CraftingRecipe(
            id++,
            ItemRegistry.CardboardShield, 1,
            new List<CraftingIngredient> { new(ItemRegistry.Wood, 4), new(ItemRegistry.Fiber, 2) },
            CraftingTableType.Workbench,
            SkillType.Woodworking, 1,
            25
        ));

        _recipes.Add(new CraftingRecipe(
            id++,
            ItemRegistry.GoldenAxe, 1,
            new List<CraftingIngredient> { new(ItemRegistry.Wood, 2), new(ItemRegistry.IronIngot, 3) },
            CraftingTableType.Workbench,
            SkillType.Woodworking, 2,
            40
        ));

        // --- FORGE & ANVIL (SMITHING) ---
        _recipes.Add(new CraftingRecipe(
            id++,
            ItemRegistry.IronIngot, 1,
            new List<CraftingIngredient> { new(ItemRegistry.IronOre, 2), new(ItemRegistry.Wood, 1) },
            CraftingTableType.Forge,
            SkillType.Smithing, 1,
            10
        ));

        _recipes.Add(new CraftingRecipe(
            id++,
            ItemRegistry.Brick, 1,
            new List<CraftingIngredient> { new(ItemRegistry.Clay, 2), new(ItemRegistry.Wood, 1) },
            CraftingTableType.Forge,
            SkillType.Smithing, 1,
            10
        ));

        _recipes.Add(new CraftingRecipe(
            id++,
            ItemRegistry.IronSword, 1,
            new List<CraftingIngredient> { new(ItemRegistry.Wood, 1), new(ItemRegistry.IronIngot, 2) },
            CraftingTableType.Forge,
            SkillType.Smithing, 2,
            30
        ));

        _recipes.Add(new CraftingRecipe(
            id++,
            ItemRegistry.IronPickaxe, 1,
            new List<CraftingIngredient> { new(ItemRegistry.Wood, 2), new(ItemRegistry.IronIngot, 3) },
            CraftingTableType.Forge,
            SkillType.Smithing, 2,
            35
        ));

        _recipes.Add(new CraftingRecipe(
            id++,
            ItemRegistry.IronAxe, 1,
            new List<CraftingIngredient> { new(ItemRegistry.Wood, 2), new(ItemRegistry.IronIngot, 3) },
            CraftingTableType.Forge,
            SkillType.Smithing, 2,
            35
        ));

        // --- COOKING POT (COOKING) ---
        _recipes.Add(new CraftingRecipe(
            id++,
            ItemRegistry.GrilledFish, 1,
            new List<CraftingIngredient> { new(ItemRegistry.RawFish, 1) },
            CraftingTableType.CookingPot,
            SkillType.Cooking, 1,
            15
        ));

        _recipes.Add(new CraftingRecipe(
            id++,
            ItemRegistry.AppleJam, 1,
            new List<CraftingIngredient> { new(ItemRegistry.Apple, 2), new(ItemRegistry.Fiber, 1) },
            CraftingTableType.CookingPot,
            SkillType.Cooking, 1,
            20
        ));

        _recipes.Add(new CraftingRecipe(
            id++,
            ItemRegistry.HealingPotion, 1,
            new List<CraftingIngredient> { new(ItemRegistry.Apple, 1), new(ItemRegistry.MagicScroll, 1) },
            CraftingTableType.CookingPot,
            SkillType.Cooking, 2,
            40
        ));

        // --- LOOM (TAILORING) ---
        _recipes.Add(new CraftingRecipe(
            id++,
            ItemRegistry.PaperRope, 1,
            new List<CraftingIngredient> { new(ItemRegistry.Fiber, 3) },
            CraftingTableType.Loom,
            SkillType.Tailoring, 1,
            10
        ));

        _recipes.Add(new CraftingRecipe(
            id++,
            ItemRegistry.Cloth, 1,
            new List<CraftingIngredient> { new(ItemRegistry.Fiber, 4) },
            CraftingTableType.Loom,
            SkillType.Tailoring, 1,
            15
        ));

        // A special backpack expansion item (we reuse compass index or check output to cycle upgrade directly)
        _recipes.Add(new CraftingRecipe(
            id++,
            ItemRegistry.Compass, 1, // We will trigger backpack upgrade when crafting this from Loom!
            new List<CraftingIngredient> { new(ItemRegistry.Cloth, 2), new(ItemRegistry.PaperRope, 2) },
            CraftingTableType.Loom,
            SkillType.Tailoring, 2,
            50
        ));
    }

    public static List<CraftingRecipe> GetRecipesForTable(CraftingTableType tableType)
    {
        List<CraftingRecipe> result = new();
        foreach (var recipe in _recipes)
        {
            if (recipe.TableType == tableType)
            {
                result.Add(recipe);
            }
        }
        return result;
    }
}
