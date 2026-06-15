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
    public static ItemType Wood => _types[8];
    public static ItemType Stone => _types[9];
    public static ItemType IronOre => _types[10];
    public static ItemType IronIngot => _types[11];
    public static ItemType Fiber => _types[12];
    public static ItemType Cloth => _types[13];
    public static ItemType PaperRope => _types[14];
    public static ItemType Clay => _types[15];
    public static ItemType Brick => _types[16];
    public static ItemType RawFish => _types[17];
    public static ItemType GrilledFish => _types[18];
    public static ItemType Apple => _types[19];
    public static ItemType AppleJam => _types[20];
    public static ItemType CardboardShield => _types[21];
    public static ItemType IronSword => _types[22];
    public static ItemType IronPickaxe => _types[23];
    public static ItemType IronAxe => _types[24];

    public static void Initialize(GraphicsDevice gd)
    {
        _types = new ItemType[25];

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

        _types[8] = new ItemType(
            8,
            "Wood",
            "Raw wood block, chopped from pine logs.",
            ItemCategory.Utility,
            99,
            new Color(139, 90, 43),
            gd,
            new string[] {
                "....xxxxxxxx....",
                "...xooooooooox..",
                "..xo#########ox.",
                "..xo#ooooooo#ox.",
                "..xo#oxxxxxo#ox.",
                "..xo#ox###xo#ox.",
                "..xo#ox#o#xo#ox.",
                "..xo#ox###xo#ox.",
                "..xo#oxxxxxo#ox.",
                "..xo#ooooooo#ox.",
                "..xo#########ox.",
                "...xooooooooox..",
                "....xxxxxxxx....",
                "................",
                "................",
                "................"
            });

        _types[9] = new ItemType(
            9,
            "Stone",
            "Rough stone fragment, mined from rock boulders.",
            ItemCategory.Utility,
            99,
            new Color(120, 120, 120),
            gd,
            new string[] {
                "......xxxx......",
                "....xxoo##xx....",
                "...xoo######x...",
                "..xo#########x..",
                ".xo##########ox.",
                ".xo##########ox.",
                "xo############x.",
                "xo############x.",
                "x##############x",
                ".x############x.",
                "..x##########x..",
                "...x########x...",
                "....xxxxxxxx....",
                "................",
                "................",
                "................"
            });

        _types[10] = new ItemType(
            10,
            "Iron Ore",
            "Raw iron ore with trace metal bits.",
            ItemCategory.Utility,
            99,
            new Color(160, 110, 80),
            gd,
            new string[] {
                "......xxxx......",
                "....xxoo##xx....",
                "...xo###o##ox...",
                "..xo##oo####ox..",
                ".xo###o######ox.",
                ".xo##########ox.",
                "xo###o########x.",
                "xo############x.",
                "x#######o######x",
                ".x############x.",
                "..x####o#####x..",
                "...x########x...",
                "....xxxxxxxx....",
                "................",
                "................",
                "................"
            });

        _types[11] = new ItemType(
            11,
            "Iron Ingot",
            "A heavy, refined bar of pure iron.",
            ItemCategory.Utility,
            99,
            new Color(192, 192, 192),
            gd,
            new string[] {
                ".....xxxxxx.....",
                "...xxooooooxx...",
                "..xoooooooooox..",
                ".xo##########ox.",
                "xo############x.",
                "xo############x.",
                "xo############x.",
                "xo############x.",
                "x##############x",
                "x##############x",
                ".x############x.",
                "..x##########x..",
                "...xx######xx...",
                ".....xxxxxx.....",
                "................",
                "................"
            });

        _types[12] = new ItemType(
            12,
            "Fiber",
            "Raw fibrous plant stalks gathered from weeds.",
            ItemCategory.Utility,
            99,
            new Color(106, 172, 53),
            gd,
            new string[] {
                ".....x.....x....",
                "....xo....xo....",
                "....xo....xo....",
                "...xo#...xo#....",
                "...xo#...xo#....",
                "..xo#o..xo#o....",
                "..xo#o..xo#o....",
                ".xo#o#.xo#o#....",
                ".xo#o#.xo#o#....",
                "xo#o#o#xo#o#....",
                "xo#o#o#xo#o#....",
                ".x#o#o##o#o#....",
                "..x#o###o##x....",
                "...xx####xx.....",
                ".....xxxx.......",
                "................"
            });

        _types[13] = new ItemType(
            13,
            "Cloth",
            "Weaved organic cloth canvas, light and strong.",
            ItemCategory.Utility,
            99,
            new Color(235, 225, 195),
            gd,
            new string[] {
                "...xxxxxxxxxx...",
                "..xoooooooooox..",
                ".xo#o#o#o#o##ox.",
                ".xo#o#o#o#o##ox.",
                ".xo#o#o#o#o##ox.",
                ".xo#o#o#o#o##ox.",
                ".xo#o#o#o#o##ox.",
                ".xo#o#o#o#o##ox.",
                ".xo#o#o#o#o##ox.",
                ".xo#o#o#o#o##ox.",
                ".xo#o#o#o#o##ox.",
                ".xo#o#o#o#o##ox.",
                "..xoooooooooox..",
                "...xxxxxxxxxx...",
                "................",
                "................"
            });

        _types[14] = new ItemType(
            14,
            "Paper Rope",
            "Twisted paper fiber, very sturdy.",
            ItemCategory.Utility,
            99,
            new Color(202, 170, 126),
            gd,
            new string[] {
                ".............x..",
                "............xo..",
                "...........xo#..",
                "..........xo#o..",
                ".........xo#o#..",
                "........xo#o#...",
                ".......xo#o#....",
                "......xo#o#.....",
                ".....xo#o#......",
                "....xo#o#.......",
                "...xo#o#........",
                "..xo#o#.........",
                ".xo#o#..........",
                ".xo#o...........",
                ".xo.............",
                ".x.............."
            });

        _types[15] = new ItemType(
            15,
            "Clay",
            "Soft brown mud clay dug from coastal sands.",
            ItemCategory.Utility,
            99,
            new Color(185, 115, 85),
            gd,
            new string[] {
                "......xxxx......",
                "....xxoooooxx...",
                "...xoo#####oox..",
                "..xoo########ox.",
                ".xo###########ox",
                "xo#############x",
                "xo#############x",
                "xo#############x",
                "x##############x",
                "x##############x",
                ".x############x.",
                "..x##########x..",
                "...xx######xx...",
                ".....xxxxxx.....",
                "................",
                "................"
            });

        _types[16] = new ItemType(
            16,
            "Brick",
            "A baked clay brick, hard and heavy.",
            ItemCategory.Utility,
            99,
            new Color(180, 75, 60),
            gd,
            new string[] {
                ".....xxxxxx.....",
                "...xxooooooxx...",
                "..xo########ox..",
                ".xo##########ox.",
                "xo############x.",
                "xo############x.",
                "xo############x.",
                "xo############x.",
                "x##############x",
                "x##############x",
                ".x############x.",
                "..x##########x..",
                "...xx######xx...",
                ".....xxxxxx.....",
                "................",
                "................"
            });

        _types[17] = new ItemType(
            17,
            "Raw Fish",
            "A fresh fish caught from the paper ocean.",
            ItemCategory.Consumable,
            99,
            new Color(90, 166, 204),
            gd,
            new string[] {
                ".......xx.......",
                "......xoxx......",
                "....xxo#oxx.....",
                "...xo#o##ox.....",
                "..xo#######x....",
                ".xo########ox...",
                "xo##########ox..",
                "x############x..",
                "x#############x.",
                ".x############x.",
                "..xx########xx..",
                "....x######x....",
                ".....x####x.....",
                "......x##x......",
                "......xo#x......",
                "......xx.x......"
            });

        _types[18] = new ItemType(
            18,
            "Grilled Fish",
            "Smells amazing! Restores 30 health.",
            ItemCategory.Consumable,
            99,
            new Color(210, 130, 60),
            gd,
            new string[] {
                ".......xx.......",
                "......xoxx......",
                "....xxo#oxx.....",
                "...xo#o##ox.....",
                "..xo#x#x#x#x....",
                ".xo#x#x#x#oxx...",
                "xo#x#x#x#x#oxx..",
                "x#x#x#x#x#x#xx..",
                "x#############x.",
                ".x############x.",
                "..xx########xx..",
                "....x######x....",
                ".....x####x.....",
                "......x##x......",
                "......xo#x......",
                "......xx.x......"
            });

        _types[19] = new ItemType(
            19,
            "Apple",
            "A crispy, delicious red apple. Restores 10 health.",
            ItemCategory.Consumable,
            99,
            new Color(230, 45, 45),
            gd,
            new string[] {
                ".......x........",
                "......x#x.......",
                ".....xoo#x......",
                "....xxo###xx....",
                "...xoo#####ox...",
                "..xoo#######ox..",
                "..xo#########x..",
                ".xo##########ox.",
                ".xo##########ox.",
                ".xo##########ox.",
                "..xo########x...",
                "..xo########x...",
                "...xo######x....",
                "....xxxxxxx.....",
                ".....xx.xx......",
                "................"
            });

        _types[20] = new ItemType(
            20,
            "Apple Jam",
            "A jar of sweet, home-cooked jam. Restores 40 health.",
            ItemCategory.Consumable,
            99,
            new Color(185, 30, 85),
            gd,
            new string[] {
                ".....xxxxxx.....",
                "....xoooooox....",
                "....xo#####ox....",
                "....xoooooox....",
                ".....xxxxxx.....",
                "....xoooooox....",
                "...xooooooooo...",
                "..xoo#######oo..",
                "..xo#########o..",
                "..xo#########o..",
                "..xo#########o..",
                "..xo#########o..",
                "..xoo#######oo..",
                "...xooooooooo...",
                "....xxxxxxxx....",
                "................"
            });

        _types[21] = new ItemType(
            21,
            "Cardboard Shield",
            "Flat defensive shield. Offers basic protection.",
            ItemCategory.Tool,
            1,
            new Color(160, 120, 80),
            gd,
            new string[] {
                "xxxxxxxxxxxxxxxx",
                "xoooooooooooooox",
                "xo############ox",
                "xo############ox",
                ".xo##########ox.",
                ".xo##########ox.",
                ".xo##########ox.",
                "..xo########ox..",
                "..xo########ox..",
                "..xo########ox..",
                "...xo######ox...",
                "...xo######ox...",
                "....xo####ox....",
                "....xo####ox....",
                ".....xo##ox.....",
                "......xxx......."
            });

        _types[22] = new ItemType(
            22,
            "Iron Sword",
            "Forged iron blade. Deals heavy damage and lasts long.",
            ItemCategory.Tool,
            1,
            new Color(130, 140, 150),
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

        _types[23] = new ItemType(
            23,
            "Iron Pickaxe",
            "Advanced mining pickaxe made of heavy forged iron.",
            ItemCategory.Tool,
            1,
            new Color(120, 130, 140),
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

        _types[24] = new ItemType(
            24,
            "Iron Axe",
            "Advanced felling axe made of heavy forged iron.",
            ItemCategory.Tool,
            1,
            new Color(120, 130, 140),
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
