"""Makes the site's cut-out renders: each Blender render of the building (TipsyTail-*.png, flat grey background) with the
background keyed out into a soft alpha matte, trimmed and saved as WebP. The matte comes from each image's own
background colour (sampled from its corners), not from a drawn mask. Run from this folder: python make_cutouts.py"""
import numpy as np
from PIL import Image, ImageFilter

for name in ("preview", "front", "side", "dry"):
    im = Image.open(f"TipsyTail-{name}.png").convert("RGB")
    a = np.asarray(im, float)
    corners = np.concatenate([a[:12, :12].reshape(-1, 3), a[:12, -12:].reshape(-1, 3), a[-12:, :12].reshape(-1, 3), a[-12:, -12:].reshape(-1, 3)])
    bg = np.median(corners, axis=0)
    d = np.sqrt(((a - bg) ** 2).sum(-1))
    alpha = np.clip((d - 6) / 18, 0, 1)                      # a soft edge between 6 and 24 colour steps from the grey
    m = Image.fromarray((alpha * 255).astype(np.uint8)).filter(ImageFilter.MedianFilter(3))
    rgba = im.copy(); rgba.putalpha(m)
    box = m.point(lambda v: 255 if v > 40 else 0).getbbox()
    rgba = rgba.crop(box)
    rgba.thumbnail((1000, 1000), Image.LANCZOS)
    rgba.save(f"tipsytail-{name}-cut.webp", quality=86, method=6)
    print(name, bg.round(), rgba.size)
