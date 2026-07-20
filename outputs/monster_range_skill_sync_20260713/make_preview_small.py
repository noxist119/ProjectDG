from pathlib import Path

from PIL import Image


root = Path(r"D:\GameDev\ProjectDG\outputs\monster_range_skill_sync_20260713")
source = root / "monster_detail_preview.png"
target = root / "monster_detail_preview_small.jpg"
with Image.open(source) as image:
    image.thumbnail((1200, 900), Image.Resampling.LANCZOS)
    image.convert("RGB").save(target, quality=58, optimize=True)
    print(target, image.size)
