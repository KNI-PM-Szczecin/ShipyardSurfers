import math
import random
from pathlib import Path

from PIL import Image

SIZE = 128
OUT = Path(__file__).resolve().parents[2] / "Assets" / "ThirdParty" / "Generated" / "Menu_SmokePuff.png"

random.seed(7)


def noise_layer(cells):
    grid = Image.new("F", (cells, cells))
    grid.putdata([random.random() for _ in range(cells * cells)])
    return grid.resize((SIZE, SIZE), Image.BICUBIC)


octaves = [(4, 0.5), (8, 0.3), (16, 0.2)]
layers = [(noise_layer(c).load(), w) for c, w in octaves]

image = Image.new("RGBA", (SIZE, SIZE))
pixels = image.load()
center = (SIZE - 1) / 2.0
for y in range(SIZE):
    for x in range(SIZE):
        dx = (x - center) / center
        dy = (y - center) / center
        radius = math.sqrt(dx * dx + dy * dy)
        falloff = max(0.0, 1.0 - radius)
        falloff = falloff * falloff * (3 - 2 * falloff)
        n = sum(layer[x, y] * w for layer, w in layers)
        n = min(1.0, max(0.0, (n - 0.2) / 0.6))
        alpha = falloff ** 1.4 * (0.35 + 0.65 * n)
        shade = int(235 + 20 * n)
        pixels[x, y] = (shade, shade, shade, int(255 * min(1.0, alpha)))

OUT.parent.mkdir(parents=True, exist_ok=True)
image.save(OUT)
print("wrote", OUT)
