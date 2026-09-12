import math
from pathlib import Path

from PIL import Image

SIZE = 64
OUT = Path(__file__).resolve().parents[2] / "Assets" / "ThirdParty" / "Generated" / "Player_Droplet.png"

image = Image.new("RGBA", (SIZE, SIZE))
pixels = image.load()
center = (SIZE - 1) / 2.0
for y in range(SIZE):
    for x in range(SIZE):
        dx = (x - center) / center
        dy = (y - center) / center
        radius = math.sqrt(dx * dx + dy * dy)
        core = 1.0 if radius < 0.55 else max(0.0, 1.0 - (radius - 0.55) / 0.45)
        alpha = core * core * (3 - 2 * core)
        highlight = max(0.0, 1.0 - math.sqrt((dx + 0.3) ** 2 + (dy + 0.3) ** 2) / 0.45)
        shade = int(215 + 40 * highlight)
        pixels[x, y] = (shade, min(255, shade + 8), 255, int(255 * alpha))

OUT.parent.mkdir(parents=True, exist_ok=True)
image.save(OUT)
print("wrote", OUT)
