import os
import sys

def install_and_import(package):
    import importlib
    try:
        importlib.import_module(package)
    except ImportError:
        import subprocess
        subprocess.check_call([sys.executable, "-m", "pip", "install", package])
    finally:
        globals()[package] = importlib.import_module(package)

# Auto-install Pillow if not present
install_and_import('PIL')
from PIL import Image, ImageDraw

def create_sprite_from_grid(grid, colors, output_path, scale=2):
    size = 16
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    pixels = img.load()

    for y in range(size):
        row = grid[y]
        for x in range(size):
            char = row[x] if x < len(row) else '.'
            if char in colors:
                pixels[x, y] = colors[char]

    if scale > 1:
        img = img.resize((size * scale, size * scale), Image.NEAREST)

    # Ensure output directory exists
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    img.save(output_path)
    print(f"Generated sprite: {output_path}")

# Define colors for our sprites
# Theme, Lighter, Darker colors
wood_colors = {
    '#': (139, 90, 43, 255),
    'o': (185, 122, 87, 255),
    'x': (83, 59, 38, 255)
}

stone_colors = {
    '#': (120, 120, 120, 255),
    'o': (160, 160, 160, 255),
    'x': (70, 70, 70, 255)
}

iron_ore_colors = {
    '#': (160, 110, 80, 255),
    'o': (190, 140, 110, 255),
    'x': (90, 50, 40, 255)
}

iron_ingot_colors = {
    '#': (192, 192, 192, 255),
    'o': (235, 235, 235, 255),
    'x': (130, 130, 130, 255)
}

fiber_colors = {
    '#': (106, 172, 53, 255),
    'o': (156, 222, 103, 255),
    'x': (56, 112, 13, 255)
}

cloth_colors = {
    '#': (235, 225, 195, 255),
    'o': (255, 250, 230, 255),
    'x': (195, 185, 155, 255)
}

rope_colors = {
    '#': (202, 170, 126, 255),
    'o': (232, 200, 156, 255),
    'x': (152, 120, 76, 255)
}

clay_colors = {
    '#': (185, 115, 85, 255),
    'o': (215, 145, 115, 255),
    'x': (145, 75, 45, 255)
}

brick_colors = {
    '#': (180, 75, 60, 255),
    'o': (210, 105, 90, 255),
    'x': (140, 35, 20, 255)
}

fish_colors = {
    '#': (90, 166, 204, 255),
    'o': (140, 216, 254, 255),
    'x': (40, 116, 154, 255)
}

gr_fish_colors = {
    '#': (210, 130, 60, 255),
    'o': (250, 170, 100, 255),
    'x': (150, 70, 20, 255)
}

apple_colors = {
    '#': (230, 45, 45, 255),
    'o': (255, 95, 95, 255),
    'x': (150, 15, 15, 255)
}

jam_colors = {
    '#': (185, 30, 85, 255),
    'o': (225, 70, 125, 255),
    'x': (125, 10, 45, 255)
}

shield_colors = {
    '#': (160, 120, 80, 255),
    'o': (200, 160, 120, 255),
    'x': (100, 80, 40, 255)
}

iron_sword_colors = {
    '#': (130, 140, 150, 255),
    'o': (180, 190, 200, 255),
    'x': (80, 90, 100, 255)
}

def main():
    dest_dir = "Content/Textures"
    if not os.path.exists(dest_dir):
        dest_dir = "PapiroFeister/Content/Textures"
        if not os.path.exists(dest_dir):
            dest_dir = "PapiroFeister/PapiroFeister/Content/Textures"

    print(f"Targeting directory: {dest_dir}")

    # Generate Workbench sprite
    create_sprite_from_grid(
        [
            "................",
            ".xxxxxxxxxxxxxx.",
            "xoooooooooooooox",
            "xo############ox",
            "xo############ox",
            "xo#xo#xo#xo##ox",
            "xo############ox",
            "xo############ox",
            "xo#xo#xo#xo##ox",
            "xo############ox",
            "xoooooooooooooox",
            ".xxxxxxxxxxxxxx.",
            "..xx........xx..",
            "..xx........xx..",
            "..xx........xx..",
            "..xx........xx.."
        ],
        {'#': (139, 90, 43, 255), 'o': (185, 122, 87, 255), 'x': (83, 59, 38, 255)},
        os.path.join(dest_dir, "workbench.png"),
        scale=4
    )

    # Generate Forge sprite
    create_sprite_from_grid(
        [
            "................",
            "......xxxx......",
            "....xxxxxxxx....",
            "...xxooooooxx...",
            "..xoooooooooox..",
            "..xo########ox..",
            ".xo##########ox.",
            ".xo##xxxx##ox.",
            ".xo#xoooo#x##ox.",
            ".xo#xo##ox###ox.",
            ".xo#x####x###ox.",
            "..xoooooooooox..",
            "...xx######xx...",
            "....xxxxxxxx....",
            ".....xxxxxx.....",
            "................"
        ],
        {'#': (60, 60, 65, 255), 'o': (90, 50, 40, 255), 'x': (255, 127, 39, 255)},
        os.path.join(dest_dir, "forge.png"),
        scale=4
    )

    # Generate Cooking Pot sprite
    create_sprite_from_grid(
        [
            "................",
            "....xxxxxxxx....",
            "...xooooooooox..",
            "..xo#########ox.",
            "..xo#o#o#o#o#ox.",
            "..xo#########ox.",
            "..xo#########ox.",
            "..xo#########ox.",
            "..xo#########ox.",
            "..xo#########ox.",
            "..xo#########ox.",
            "..xoooooooooox..",
            "...x########x...",
            "....xx#xx#xx....",
            ".....xx..xx.....",
            "................"
        ],
        {'#': (45, 47, 52, 255), 'o': (50, 110, 80, 255), 'x': (220, 220, 255, 255)},
        os.path.join(dest_dir, "cooking_pot.png"),
        scale=4
    )

    # Generate Loom sprite
    create_sprite_from_grid(
        [
            "xxxxxxxxxxxxxxxx",
            "xoooooooooooooox",
            "xo############ox",
            "xo#ox#ox#ox##ox",
            "xo#o#o#o#o#o#ox",
            "xo#o#o#o#o#o#ox",
            "xo#o#o#o#o#o#ox",
            "xo#o#o#o#o#o#ox",
            "xo#o#o#o#o#o#ox",
            "xo#o#o#o#o#o#ox",
            "xo#o#o#o#o#o#ox",
            "xo#ox#ox#ox##ox",
            "xo############ox",
            "xoooooooooooooox",
            "xx............xx",
            "xx............xx"
        ],
        {'#': (205, 175, 125, 255), 'o': (225, 205, 165, 255), 'x': (237, 28, 36, 255)},
        os.path.join(dest_dir, "loom.png"),
        scale=4
    )

    # Generate items
    create_sprite_from_grid(
        [
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
        ],
        wood_colors,
        os.path.join(dest_dir, "wood.png")
    )

    print("All sprites generated successfully!")

if __name__ == "__main__":
    main()
