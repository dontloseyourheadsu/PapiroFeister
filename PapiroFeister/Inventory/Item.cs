using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PapiroFeister.Inventory;

public enum ItemCategory
{
    Tool,
    Consumable,
    Utility
}

public sealed class ItemType : IDisposable
{
    public int Id { get; }
    public string Name { get; }
    public string Description { get; }
    public ItemCategory Category { get; }
    public int MaxStack { get; }
    public Color ThemeColor { get; }
    public Texture2D IconTexture { get; private set; }

    public ItemType(int id, string name, string description, ItemCategory category, int maxStack, Color themeColor, GraphicsDevice graphicsDevice, string[] iconGrid)
    {
        Id = id;
        Name = name;
        Description = description;
        Category = category;
        MaxStack = maxStack;
        ThemeColor = themeColor;

        CreateIconTexture(graphicsDevice, iconGrid);
    }

    private void CreateIconTexture(GraphicsDevice graphicsDevice, string[] grid)
    {
        int size = 16;
        IconTexture = new Texture2D(graphicsDevice, size, size);
        Color[] data = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            string row = grid[y];
            for (int x = 0; x < size; x++)
            {
                char c = x < row.Length ? row[x] : '.';
                if (c == '#')
                {
                    data[y * size + x] = ThemeColor;
                }
                else if (c == 'x')
                {
                    // Dark shade (shadow/outline)
                    data[y * size + x] = Color.Lerp(ThemeColor, Color.Black, 0.45f);
                }
                else if (c == 'o')
                {
                    // Lighter shade (highlight)
                    data[y * size + x] = Color.Lerp(ThemeColor, Color.White, 0.45f);
                }
                else
                {
                    data[y * size + x] = Color.Transparent;
                }
            }
        }

        IconTexture.SetData(data);
    }

    public void Dispose()
    {
        IconTexture?.Dispose();
        IconTexture = null;
    }
}

public sealed class Item
{
    public ItemType Type { get; }
    public int Quantity { get; set; }
    public int Durability { get; set; } // 0 to 100

    public Item(ItemType type, int quantity = 1)
    {
        Type = type;
        Quantity = Math.Clamp(quantity, 1, type.MaxStack);
        Durability = 100;
    }

    public Item Clone(int quantity)
    {
        return new Item(Type, quantity) { Durability = this.Durability };
    }
}

public static class ItemRegistry
{
    private static ItemType[] _types;

    public static ItemType WoodenSword => _types[0];
    public static ItemType SteelPickaxe => _types[1];
    public static ItemType GoldenAxe => _types[2];
    public static ItemType PaperShovel => _types[3];
    public static ItemType FishingRod => _types[4];
    public static ItemType HealingPotion => _types[5];
    public static ItemType MagicScroll => _types[6];
    public static ItemType Compass => _types[7];

    public static void Initialize(GraphicsDevice gd)
    {
        _types = new ItemType[8];

        _types[0] = new ItemType(
            0,
            "Wooden Sword",
            "A sturdy sword made of thick cardboard. Good for defense.",
            ItemCategory.Tool,
            1,
            new Color(185, 122, 87),
            gd,
            new string[] {
                "............ooo.",
                "...........oo##.",
                "..........oo##..",
                ".........oo##...",
                "........oo##....",
                ".......oo##.....",
                "......oo##......",
                ".....oo##.......",
                "....oo##........",
                "...oo##.........",
                "..oo##..........",
                ".x#xx...........",
                "x##xx...........",
                ".xxx............",
                "..xx............",
                "...x............"
            });

        _types[1] = new ItemType(
            1,
            "Steel Pickaxe",
            "Heavy and sharp. Ideal for mining tough resources.",
            ItemCategory.Tool,
            1,
            new Color(112, 146, 190),
            gd,
            new string[] {
                "......xxxx......",
                "....xxxxxxxxx...",
                "..xxxxxxxxxxxxx.",
                "..xx..oxxxo..xx.",
                "......oxxxo.....",
                ".......xxx......",
                ".......###......",
                "......###.......",
                "......###.......",
                ".....###........",
                ".....###........",
                "....###.........",
                "....###.........",
                "...###..........",
                "...###..........",
                "..###..........."
            });

        _types[2] = new ItemType(
            2,
            "Golden Axe",
            "Shines brightly. Cuts down trees with swift speed.",
            ItemCategory.Tool,
            1,
            new Color(255, 201, 14),
            gd,
            new string[] {
                ".......xxx......",
                "......xxxxx.....",
                "....xxxxxxxxx...",
                "...xxxxxxxxxxx..",
                "...xxxxxxxxxx#..",
                "....xxxxxxx###..",
                "......xxxx####..",
                ".......#######..",
                ".......###......",
                "......###.......",
                "......###.......",
                ".....###........",
                ".....###........",
                "....###.........",
                "....###.........",
                "...###.........."
            });

        _types[3] = new ItemType(
            3,
            "Paper Shovel",
            "Lightweight shovel. Moves sand and dirt quickly.",
            ItemCategory.Tool,
            1,
            new Color(220, 224, 230),
            gd,
            new string[] {
                "......oooo......",
                ".....oooooo.....",
                ".....o####o.....",
                ".....o####o.....",
                ".....o####o.....",
                "......####......",
                ".......##.......",
                ".......##.......",
                "......###.......",
                "......###.......",
                ".....###........",
                ".....###........",
                "....###.........",
                "....###.........",
                "...###..........",
                "...###.........."
            });

        _types[4] = new ItemType(
            4,
            "Fishing Rod",
            "Cast into the water to catch some paper-fish.",
            ItemCategory.Tool,
            1,
            new Color(185, 140, 90),
            gd,
            new string[] {
                "..............##",
                "............####",
                "..........###.oo",
                "........###...oo",
                "......###....oo.",
                "....###......oo.",
                "....###......oo.",
                "..###........oo.",
                "###..........oo.",
                ".............o..",
                ".............o..",
                ".............o..",
                "............o...",
                "............o...",
                "...........o....",
                "...........o...."
            });

        _types[5] = new ItemType(
            5,
            "Healing Potion",
            "Restores 50 health. Smells like cherry paper-mache.",
            ItemCategory.Consumable,
            99,
            new Color(237, 28, 36),
            gd,
            new string[] {
                "......xxxx......",
                "......x..x......",
                "......x..x......",
                "....xxxxxxxx....",
                "...xooooooooo...",
                "..xooooooooooo..",
                "..xoo#######oo..",
                ".xooo#######ooo.",
                ".xooo#######ooo.",
                ".xooo#######ooo.",
                ".xooo#######ooo.",
                ".xooooooooooooo.",
                "..xooooooooooo..",
                "...xooooooooo...",
                "....xxxxxxxx....",
                "................"
            });

        _types[6] = new ItemType(
            6,
            "Magic Scroll",
            "Unleashes a gentle folding wind to push obstacles.",
            ItemCategory.Consumable,
            99,
            new Color(163, 73, 164),
            gd,
            new string[] {
                "....xxxxxxx.....",
                "...xooooooox....",
                "...xo#####ox....",
                "...xooooooox....",
                "....xxxxxxx.....",
                ".....#####......",
                ".....#####......",
                ".....#####......",
                ".....#####......",
                ".....#####......",
                ".....#####......",
                "....xxxxxxx.....",
                "...xooooooox....",
                "...xo#####ox....",
                "...xooooooox....",
                "....xxxxxxx....."
            });

        _types[7] = new ItemType(
            7,
            "Compass",
            "Always points towards the center of Paper Island.",
            ItemCategory.Utility,
            1,
            new Color(63, 72, 204),
            gd,
            new string[] {
                "......xxxx......",
                "....xxoooooxx...",
                "...xooooooooox..",
                "..xooooooooooox.",
                "..xoooo#oooooox.",
                ".xoooo###oooooox",
                ".xooo#####ooooxx",
                ".xoo#######oooxx",
                ".xoo#######oooxx",
                ".xooo#####ooooxx",
                ".xoooo###oooooox",
                "..xoooo#oooooox.",
                "..xooooooooooox.",
                "...xooooooooox..",
                "....xxoooooxx...",
                "......xxxx......"
            });
    }

    public static ItemType GetRandomType()
    {
        var random = new Random();
        return _types[random.Next(_types.Length)];
    }

    public static void Dispose()
    {
        if (_types != null)
        {
            foreach (var type in _types)
            {
                type?.Dispose();
            }
            _types = null;
        }
    }
}
