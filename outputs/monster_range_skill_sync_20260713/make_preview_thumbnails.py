from pathlib import Path

from PIL import Image


root = Path(r"D:\GameDev\ProjectDG\outputs\monster_range_skill_sync_20260713")
for name in ("monster_summary_preview", "monster_detail_preview"):
    source = root / f"{name}.png"
    target = root / f"{name}_thumb.png"
    with Image.open(source) as image:
        image.thumbnail((1800, 1600), Image.Resampling.LANCZOS)
        image.save(target)
        print(target, image.size)
