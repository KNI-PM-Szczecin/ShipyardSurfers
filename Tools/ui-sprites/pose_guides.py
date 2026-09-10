"""Generates the calibration pose guides and the info icon into Assets/UI/Sprites.

Run from the project root:  py -3.13 Tools/ui-sprites/pose_guides.py

Sprites are white shapes on transparent background - the UI tints them, so do not bake colour in.
Replace any generated file with your own PNG of the same name and size and the UI picks it up on the
next "Shipyard Surfers/UI/Rebuild All UI".
"""

from pathlib import Path

from PIL import Image, ImageDraw

OUT = Path("Assets/UI/Sprites")
GUIDE_SIZE = 320
ICON_SIZE = 96
WHITE = (255, 255, 255, 255)
CLEAR = (0, 0, 0, 0)

LIMB = 20
TORSO = 26
HEAD_R = 0.075
FIGURE_SCALE = 0.82

NEUTRAL_ARM = {"elbow": (0.20, 0.47), "wrist": (0.085, 0.375)}

POSES = {
    "Pose_Neutral": {
        "left": NEUTRAL_ARM,
        "right": NEUTRAL_ARM,
        "arrow": None,
    },
    "Pose_Left": {
        "left": {"elbow": (0.19, 0.30), "wrist": (0.30, 0.28)},
        "right": NEUTRAL_ARM,
        "arrow": ((-0.36, 0.32), "left"),
    },
    "Pose_Right": {
        "left": NEUTRAL_ARM,
        "right": {"elbow": (0.19, 0.30), "wrist": (0.30, 0.28)},
        "arrow": ((0.36, 0.32), "right"),
    },
    "Pose_Jump": {
        "left": {"elbow": (0.22, 0.24), "wrist": (0.24, 0.10)},
        "right": {"elbow": (0.22, 0.24), "wrist": (0.24, 0.10)},
        "arrow": ((0.0, 0.035), "up"),
    },
    "Pose_Roll": {
        "left": {"elbow": (0.20, 0.42), "wrist": (0.22, 0.60)},
        "right": {"elbow": (0.20, 0.42), "wrist": (0.22, 0.60)},
        "arrow": ((0.0, 0.93), "down"),
    },
}


def px(point, size):
    x, y = point
    return (size * (0.5 + x * FIGURE_SCALE), size * (0.5 + (y - 0.5) * FIGURE_SCALE))


def px_flat(point, size):
    x, y = point
    return (size * (0.5 + x), size * y)


def dot(draw, point, radius, size):
    cx, cy = px(point, size)
    r = radius * size
    draw.ellipse((cx - r, cy - r, cx + r, cy + r), fill=WHITE)


def bone(draw, points, width, size):
    draw.line([px(p, size) for p in points], fill=WHITE, width=width, joint="curve")
    for point in points:
        dot(draw, point, width / (2.0 * size), size)


def arrow(draw, point, direction, size):
    cx, cy = px_flat(point, size)
    span = 0.062 * size
    depth = 0.045 * size
    if direction == "left":
        points = [(cx - depth, cy), (cx + depth, cy - span), (cx + depth, cy + span)]
    elif direction == "right":
        points = [(cx + depth, cy), (cx - depth, cy - span), (cx - depth, cy + span)]
    elif direction == "up":
        points = [(cx, cy - depth), (cx - span, cy + depth), (cx + span, cy + depth)]
    else:
        points = [(cx, cy + depth), (cx - span, cy - depth), (cx + span, cy - depth)]
    draw.polygon(points, fill=WHITE)


def figure(draw, pose, size):
    dot(draw, (0.0, 0.135), HEAD_R, size)
    bone(draw, [(0.0, 0.235), (0.0, 0.56)], TORSO, size)

    for side, sign in (("left", -1.0), ("right", 1.0)):
        arm = pose[side]
        shoulder = (sign * 0.10, 0.265)
        elbow = (sign * arm["elbow"][0], arm["elbow"][1])
        wrist = (sign * arm["wrist"][0], arm["wrist"][1])
        bone(draw, [shoulder, elbow, wrist], LIMB, size)

        hip = (sign * 0.07, 0.56)
        bone(draw, [hip, (sign * 0.085, 0.72), (sign * 0.09, 0.88)], LIMB, size)

    if pose["arrow"] is not None:
        arrow(draw, pose["arrow"][0], pose["arrow"][1], size)


def write_guides():
    for name, pose in POSES.items():
        image = Image.new("RGBA", (GUIDE_SIZE, GUIDE_SIZE), CLEAR)
        figure(ImageDraw.Draw(image), pose, GUIDE_SIZE)
        image.save(OUT / f"{name}.png")
        print("wrote", OUT / f"{name}.png")


def write_info_icon():
    # circle with the "i" punched out, so the glyph reads on any tint
    image = Image.new("RGBA", (ICON_SIZE, ICON_SIZE), CLEAR)
    draw = ImageDraw.Draw(image)
    margin = 4
    draw.ellipse((margin, margin, ICON_SIZE - margin, ICON_SIZE - margin), fill=WHITE)
    cx = ICON_SIZE / 2
    draw.ellipse((cx - 6, 20, cx + 6, 32), fill=CLEAR)
    draw.rounded_rectangle((cx - 6, 40, cx + 6, 74), radius=6, fill=CLEAR)
    image.save(OUT / "Icon_Info.png")
    print("wrote", OUT / "Icon_Info.png")


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    write_guides()
    write_info_icon()
