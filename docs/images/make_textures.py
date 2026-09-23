"""Makes the Tipsy Tail site's surfaces: the oiled-timber bar top (planks running across, in daylight and after dusk)
and the pulpboard the coasters are printed on. Procedural (numpy and Pillow, fixed seeds); no source images and no
generative model. Tiles repeat. Run from this folder: python make_textures.py"""
import numpy as np
from PIL import Image

def wrap_noise(rng, w, h, cw, ch):
    g = rng.normal(0, 1, (h // ch, w // cw)); g = (g - g.min()) / (g.max() - g.min())
    big = np.tile(g, (3, 3))
    up = np.asarray(Image.fromarray((big * 255).astype(np.uint8)).resize((w * 3, h * 3), Image.BICUBIC), float) / 255
    return up[h:2 * h, w:2 * w] - .5

# ---- the bar top: planks 96 px wide running across, each with its own tone, long grain and a few knots; seams between
for name, seed, base, spread in (("bar-day.webp", 6101, (207, 169, 114), 1.0), ("bar-dusk.webp", 6102, (58, 42, 28), .55)):
    rng = np.random.default_rng(seed); W, H, P = 768, 1536, 96   # sixteen planks, so the repeat doesn't show as stripes
    y, x = np.mgrid[0:H, 0:W].astype(float)
    plank = (y // P).astype(int)
    tone = rng.uniform(-10, 10, H // P + 1)[plank] * spread
    grain = (np.sin((y + wrap_noise(rng, W, H, 96, 8) * 40) / 1.6) * 3 + wrap_noise(rng, W, H, 4, 32) * 10) * spread
    knots = np.zeros((H, W))
    for _ in range(14):
        cx, cy, r = rng.uniform(0, W), rng.uniform(0, H), rng.uniform(5, 11)
        dx = np.minimum(abs(x - cx), W - abs(x - cx)); dy = np.minimum(abs(y - cy), H - abs(y - cy))
        knots -= 11 * np.exp(-(dx * dx / (r * r * 3) + dy * dy / (r * r)))
    seam = np.exp(-(((y % P) - 0.5) / 1.0) ** 2) * -34 * (1 if spread == 1 else .8)
    v = tone + grain + knots * spread + seam
    rgb = np.array(base, float) + v[..., None] * np.array([1, .86, .66])
    Image.fromarray(rgb.clip(0, 255).astype(np.uint8)).save(name, quality=86, method=6); print("made", name)

# ---- pulpboard: pale, felted, with coarse flecks pressed in
rng = np.random.default_rng(6103); N = 256
v = wrap_noise(rng, N, N, 16, 16) * 8 + wrap_noise(rng, N, N, 4, 4) * 6 + rng.normal(0, 2, (N, N))
fl = rng.random((N, N)) < .006
v -= fl * rng.uniform(8, 22, (N, N))
rgb = np.array((241, 231, 210), float) + v[..., None] * np.array([1, .97, .9])
Image.fromarray(rgb.clip(0, 255).astype(np.uint8)).save("pulpboard.webp", quality=88, method=6); print("made pulpboard.webp")

# ---- light over the bar: a pale sun wash by day, a warm pool of lamplight after dusk; laid over the planks as a layer
from PIL import ImageFilter
for name, colour, peak, cx, cy in (("wash-day.webp", (255, 246, 222), 120, .72, .08), ("wash-dusk.webp", (255, 196, 118), 70, .5, .02)):
    W2, H2 = 1600, 1100
    yy, xx = np.mgrid[0:H2, 0:W2].astype(float)
    d = np.sqrt(((xx / W2 - cx) / .55) ** 2 + ((yy / H2 - cy) / .75) ** 2)
    a = np.clip(1 - d, 0, 1) ** 1.8 * peak
    img = np.zeros((H2, W2, 4)); img[..., :3] = colour; img[..., 3] = a
    Image.fromarray(img.astype(np.uint8), "RGBA").save(name, quality=80, method=6); print("made", name)

# ---- the matchbook striker: a strip of dark red-brown grit
rng = np.random.default_rng(6104); W3, H3 = 512, 28
g = rng.normal(0, 1, (H3, W3)); g = np.asarray(Image.fromarray(((g - g.min()) / (g.max() - g.min()) * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(.6)), float) / 255 - .5
rgb = np.array((92, 44, 30), float) + g[..., None] * np.array((60, 40, 30))
edge = np.clip(np.minimum(np.arange(H3), H3 - 1 - np.arange(H3)) / 2.5, 0, 1)[:, None]
rgb = rgb * (0.75 + .25 * edge[..., None])
Image.fromarray(rgb.clip(0, 255).astype(np.uint8)).save("striker.webp", quality=86, method=6); print("made striker.webp")
