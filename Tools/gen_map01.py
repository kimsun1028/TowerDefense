# AI-assisted code start
"""
Map01 생성기 (Pillow + numpy + scipy). 레이아웃과 그림을 한 번에 만든다.

  python3 Tools/gen_map01.py

- 레이아웃: 킹덤러시 참고 맵의 경로(OLD_PATHS)를 16:9 화면(960x540)에 맞게 옮기고 매끈하게 다듬는다.
  길 그림은 경로 중심선을 일정한 폭으로 그려서 만든다 → 길 모양 = 적 이동 경로 (울퉁불퉁하지 않음).
- 그림: 타워 그림(48x64 픽셀아트, 외곽선 24,18,30)과 어울리게 1px = 1텍셀 픽셀아트로 그린다 (60px = 1칸).
- 결과:
  Assets/TowerDefense/Art/Map/ Map01_Background.png, Map01_Road.png(= 길 판정 마스크), Pad.png,
                               Portal_Top.png, Portal_Right.png
  Assets/TowerDefense/Scripts/Map/Editor/Map01Builder.cs 의 "맵 데이터" 구역 (경로·자리·입구 좌표)를 다시 쓴다.
"""
import math
import random
import re
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

# ── 경로·크기 ──
ROOT = Path(__file__).resolve().parent.parent
ART_DIR = ROOT / "Assets/TowerDefense/Art/Map"
BUILDER = ROOT / "Assets/TowerDefense/Scripts/Map/Editor/Map01Builder.cs"

VIEW_W, VIEW_H = 960, 540                      # 16:9 화면 = 16 x 9칸
MARGIN_X, MARGIN_Y = 80, 40                    # 다른 화면비용 여백 (그림은 여백까지 그린다)
W, H = VIEW_W + MARGIN_X * 2, VIEW_H + MARGIN_Y * 2
ROAD_WIDTH = 56
SUPERSAMPLE = 4
SEED = 11

# 참고 맵(820x622 px) 좌표 → 새 화면 좌표: 가로로 넓히고 세로는 살짝 줄인다
OLD_CENTER = (442, 116)
NEW_CENTER = (480 + MARGIN_X, 128 + MARGIN_Y)
SCALE_X, SCALE_Y = 1.2, 0.9

TOP_PORTAL = (586 + MARGIN_X, 58 + MARGIN_Y)   # 위 입구 블랙홀 중심 (길이 여기서 나온다)
RIGHT_PORTAL = (904 + MARGIN_X, 0)            # y는 경로에서 정한다
GOAL_X = 92 + MARGIN_X
PORTAL_TOP_RADIUS = 34
PORTAL_RIGHT_RADIUS = (18, 36)

PAD_W, PAD_TOP_H, PAD_SIDE_H = 48, 30, 5       # 타워 자리 영역 (클릭 범위·간격용). 그림은 가운데 작은 원만
SLOT_MARK = (28, 14)                           # 바닥에 그린 작은 원 (2.5D라 납작한 타원)
PAD_CLEARANCE = 5
SLOT_MIN_GAP = (54, 40)

OLD_PATHS = {
    "Path_TopMiddle": [(530.0, 10.0), (530.0, 36.2), (528.2, 69.8), (519.1, 97.6), (504.2, 120.0), (487.9, 137.9), (471.7, 153.4), (460.8, 172.9), (453.7, 197.6), (446.4, 231.4), (442.6, 251.6), (435.9, 282.0), (426.2, 305.8), (414.8, 320.2), (397.6, 333.5), (372.0, 339.8), (339.1, 342.1), (321.2, 345.2), (302.4, 349.6), (282.6, 355.4), (262.2, 358.9), (241.2, 360.3), (219.6, 359.5), (197.4, 356.5), (173.3, 354.5), (147.4, 353.5), (119.8, 353.5), (90.2, 354.5), (65.6, 356.8)],
    "Path_TopLoop": [(530.0, 10.0), (530.0, 36.2), (527.2, 69.5), (513.5, 95.8), (490.3, 114.4), (462.4, 123.5), (428.1, 124.6), (406.9, 123.9), (382.9, 122.2), (356.1, 119.8), (331.5, 117.9), (309.0, 116.8), (288.6, 116.4), (270.4, 116.6), (240.1, 121.6), (218.0, 132.6), (203.8, 148.9), (197.4, 170.1), (195.9, 188.3), (201.8, 206.2), (221.6, 222.8), (239.8, 232.8), (260.2, 241.6), (278.4, 259.3), (292.9, 289.2), (301.5, 313.2), (294.4, 336.9), (272.1, 352.0), (239.5, 359.8), (219.6, 359.5), (197.4, 356.5), (173.3, 354.5), (147.4, 353.5), (119.8, 353.5), (90.2, 354.5), (65.6, 356.8)],
    "Path_Right": [(815.0, 255.0), (790.8, 254.8), (766.6, 254.6), (734.4, 254.4), (705.6, 254.2), (680.4, 254.1), (658.6, 254.0), (640.4, 254.0), (607.2, 254.0), (578.6, 254.0), (556.8, 258.1), (537.6, 276.6), (532.6, 304.5), (532.4, 323.5), (536.2, 350.8), (553.6, 368.4), (576.5, 376.6), (604.2, 386.8), (623.0, 407.4), (632.8, 434.2), (633.5, 454.2), (622.2, 474.9), (599.2, 489.4), (558.5, 494.1), (529.5, 494.9), (503.2, 494.9), (479.6, 494.3), (458.6, 493.0), (440.4, 491.0), (419.1, 488.1), (394.7, 484.4), (367.2, 479.8), (336.8, 474.2), (313.1, 469.4), (286.1, 461.5), (274.5, 441.1), (263.6, 408.4), (248.8, 380.9), (229.4, 365.4), (207.2, 356.8), (182.8, 353.5), (145.0, 353.0), (119.8, 353.5), (90.2, 354.5), (65.6, 356.8)],
}
OLD_SLOTS = [(258, 175), (325, 173), (387, 177), (335, 228), (366, 279), (234, 289), (469, 364), (320, 406),
             (384, 406), (443, 430), (510, 431), (577, 432), (585, 310), (358, 524), (595, 184), (527, 182)]

# ── 팔레트 (타워 그림과 같은 외곽선·회색) ──
OUTLINE = (24, 18, 30)
FLOOR = [(60, 30, 80), (48, 28, 72), (38, 26, 64), (32, 23, 56), (27, 20, 48)]   # 먼 곳(위) → 가까운 곳(아래)
GRID = [(112, 50, 132), (92, 46, 120), (74, 42, 108), (60, 40, 96), (52, 38, 86)]
GRID_DOT = (150, 70, 170)
# 길 윗면 음영 (빛은 왼쪽 위): 어두움 → 밝음
ROAD_SHADES = [(58, 50, 100), (72, 64, 118), (88, 80, 136), (104, 96, 154), (124, 118, 176)]
ROAD_SEAM = (78, 70, 124)
ROAD_RIM = (150, 236, 250)                      # 빛을 받는 가장자리 (네온)
ROAD_DARK_RIM = (44, 36, 78)                    # 그늘진 가장자리
ROAD_SIDE = [(74, 52, 112), (62, 44, 98), (52, 37, 84), (42, 30, 70), (34, 25, 58)]
ROAD_SIDE_SEAM = (30, 22, 50)
ROAD_THICKNESS = 10
SHADOW_FACTOR = 0.55
SHADOW_SOFT_FACTOR = 0.75
LIGHT = (-0.45, -0.9)                           # 빛 방향 (x, y), y는 아래가 +
PAD_TOP = (74, 74, 90)
PAD_INNER = (52, 50, 62)
PAD_NEON = (90, 220, 240)
HOLE = (10, 6, 20)
SWIRL = [(40, 20, 64), (84, 36, 120), (150, 60, 180)]
RING = [(150, 30, 120), (230, 70, 190), (255, 140, 220), (110, 230, 255)]
CUBE = [(96, 70, 150), (62, 44, 104), (44, 32, 78)]
CUBE_EDGE = (200, 90, 200)
BAYER = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16.0


# ── 레이아웃 ──
def transform(point):
    x, y = point
    return ((x - OLD_CENTER[0]) * SCALE_X + NEW_CENTER[0], (y - OLD_CENTER[1]) * SCALE_Y + NEW_CENTER[1])


def smooth(points, iterations=3):
    """Chaikin: 꺾인 곳을 둥글게 (양 끝점 유지)."""
    for _ in range(iterations):
        result = [points[0]]
        for (x0, y0), (x1, y1) in zip(points, points[1:]):
            result += [(x0 * 0.75 + x1 * 0.25, y0 * 0.75 + y1 * 0.25), (x0 * 0.25 + x1 * 0.75, y0 * 0.25 + y1 * 0.75)]
        result.append(points[-1])
        points = result
    return points


def resample(points, spacing=22.0):
    distances = [0.0]
    for (x0, y0), (x1, y1) in zip(points, points[1:]):
        distances.append(distances[-1] + math.hypot(x1 - x0, y1 - y0))
    total = distances[-1]
    count = max(2, round(total / spacing) + 1)
    result, index = [], 0
    for i in range(count):
        target = total * i / (count - 1)
        while index < len(distances) - 2 and distances[index + 1] < target:
            index += 1
        span = distances[index + 1] - distances[index] or 1.0
        t = (target - distances[index]) / span
        (x0, y0), (x1, y1) = points[index], points[index + 1]
        result.append((x0 + (x1 - x0) * t, y0 + (y1 - y0) * t))
    return result


def cut_at_x(points, x_limit, keep_greater):
    """x_limit 경계에서 자른다 (경계점 보간)."""
    result = []
    for a, b in zip(points, points[1:]):
        inside_a = (a[0] >= x_limit) == keep_greater
        inside_b = (b[0] >= x_limit) == keep_greater
        if inside_a:
            result.append(a)
        if inside_a != inside_b:
            t = (x_limit - a[0]) / (b[0] - a[0])
            result.append((x_limit, a[1] + (b[1] - a[1]) * t))
    if (points[-1][0] >= x_limit) == keep_greater:
        result.append(points[-1])
    return result


def build_paths():
    paths = {}
    for name, old in OLD_PATHS.items():
        points = [transform(p) for p in old]
        if name == "Path_Right":
            points = cut_at_x(points, RIGHT_PORTAL[0], keep_greater=False)
        else:
            # 위 블랙홀 중심에서 시작해 곧게 내려온다
            points = [TOP_PORTAL] + [p for p in points if p[1] > TOP_PORTAL[1] + 24]
        points = cut_at_x(points, GOAL_X, keep_greater=True)
        paths[name] = resample(smooth(points))
    return paths


def draw_road_mask(paths):
    s = SUPERSAMPLE
    image = Image.new("L", (W * s, H * s), 0)
    draw = ImageDraw.Draw(image)
    radius = ROAD_WIDTH * s / 2

    def stroke(points):
        scaled = [(x * s, y * s) for x, y in points]
        draw.line(scaled, fill=255, width=ROAD_WIDTH * s, joint="curve")
        for x, y in scaled:
            draw.ellipse([x - radius, y - radius, x + radius, y + radius], fill=255)

    for points in paths.values():
        stroke(points)
    # 지키는 곳 너머 화면 왼쪽 끝까지 이어지는 출구 (그림만)
    end = paths["Path_TopMiddle"]
    (x0, y0), (x1, y1) = end[-2], end[-1]
    direction = ((x1 - x0), (y1 - y0))
    length = math.hypot(*direction) or 1.0
    stroke([end[-1], (-ROAD_WIDTH, y1 + direction[1] / length * (x1 + ROAD_WIDTH))])
    small = image.resize((W, H), Image.LANCZOS)
    road = np.asarray(small) > 127
    # 길은 블랙홀에서 시작한다: 구멍 중심 너머로 튀어나온 끝은 지운다
    cx, cy = (round(v) for v in paths["Path_Right"][0])
    road[cy - PORTAL_RIGHT_RADIUS[1] - 4:cy + PORTAL_RIGHT_RADIUS[1] + 4, cx + 1:] = False
    tx, ty = (round(v) for v in paths["Path_TopMiddle"][0])
    road[:ty, tx - PORTAL_TOP_RADIUS - 4:tx + PORTAL_TOP_RADIUS + 4] = False
    return road


def place_slots(road):
    """참고 맵의 자리를 옮긴 뒤, 길과 겹치면 가장 가까운 빈 곳으로 민다."""
    blocked = ndimage.binary_dilation(road, iterations=PAD_CLEARANCE)
    half_w, half_h = PAD_W // 2, (PAD_TOP_H + PAD_SIDE_H) // 2

    def fits(x, y, placed):
        x0, y0 = int(x - half_w), int(y - half_h)
        if x0 < MARGIN_X or y0 < MARGIN_Y or x0 + PAD_W > W - MARGIN_X or y0 + half_h * 2 > H - MARGIN_Y:
            return False
        if blocked[y0:y0 + half_h * 2, x0:x0 + PAD_W].any():
            return False
        return all(abs(x - px) >= SLOT_MIN_GAP[0] or abs(y - py) >= SLOT_MIN_GAP[1] for px, py in placed)

    offsets = sorted(((dx, dy) for dx in range(-40, 41, 2) for dy in range(-30, 31, 2)), key=lambda o: o[0] ** 2 + o[1] ** 2)
    placed, moved = [], []
    for number, old in enumerate(OLD_SLOTS, start=1):
        x, y = (round(v) for v in transform(old))
        for dx, dy in offsets:
            if fits(x + dx, y + dy, placed):
                placed.append((x + dx, y + dy))
                if dx or dy:
                    moved.append((number, dx, dy))
                break
        else:
            raise RuntimeError(f"자리 {number}을 둘 곳이 없습니다")
    return placed, moved


# ── 픽셀아트 도우미 ──
def dither_bands(value, palette, rows_mask=None):
    """0~1 값 → 팔레트 단계 + Bayer 디더 (부드러운 그라데이션 대신 픽셀 느낌)."""
    scaled = value * (len(palette) - 1)
    low = np.floor(scaled).astype(int)
    frac = scaled - low
    yy, xx = np.indices(value.shape)
    step = low + (frac > BAYER[yy % 4, xx % 4])
    step = np.clip(step, 0, len(palette) - 1)
    return np.asarray(palette, dtype=np.uint8)[step]


def outline_of(mask):
    return ndimage.binary_dilation(mask) & ~mask


def build_background(road, slots, paths):
    rng = random.Random(SEED)
    yy, xx = np.indices((H, W))
    depth = np.clip(yy / (H - 1), 0, 1) ** 0.8
    image = dither_bands(depth, FLOOR).astype(np.int32)

    # 원근 그리드: 가로선 간격이 아래로 갈수록 넓어진다, 세로선은 32px 간격
    grid = np.zeros((H, W), bool)
    y = 0.0
    while y < H:
        grid[int(y)] = True
        y += 16 + 20 * (y / H)
    grid[:, (np.arange(W) - W // 2) % 32 == 0] = True
    grid_color = dither_bands(depth, GRID)
    image[grid] = grid_color[grid]
    dots = grid & np.roll(grid, 1, 0) & np.roll(grid, 1, 1) & ~np.roll(grid, 2, 1)
    image[dots & (yy < H * 0.45)] = GRID_DOT

    # 입구 바닥 빛 (디더 타원)
    for (cx, cy), (rx, ry) in ((TOP_PORTAL, (70, 26)), (paths_start(paths, "Path_Right"), (40, 46))):
        glow = 1 - ((xx - cx) / rx) ** 2 - ((yy - cy - (24 if ry < 30 else 0)) / ry) ** 2
        lit = glow > BAYER[yy % 4, xx % 4] * 1.4
        image[lit] = (image[lit] * 0.5 + np.array(RING[1]) * 0.5).astype(np.int32)

    # 길: 그림자(진한 안쪽 + 디더 바깥) → 옆면(두께, 아래로 갈수록 어둡게) → 외곽선
    thickness = ROAD_THICKNESS
    shadow_core = ndimage.shift(road, (thickness + 8, 8), order=0)
    shadow_soft = ndimage.binary_dilation(shadow_core, iterations=4) & ~shadow_core
    shadow_soft &= BAYER[yy % 4, xx % 4] < 0.5
    for mask, factor in ((shadow_soft, SHADOW_SOFT_FACTOR), (shadow_core, SHADOW_FACTOR)):
        mask = mask & ~road
        image[mask] = (image[mask] * factor).astype(np.int32)
    side = np.zeros_like(road)
    side_depth = np.zeros((H, W), int)
    for d in range(1, thickness + 1):
        layer = ndimage.shift(road, (d, 0), order=0) & ~road & ~side
        side |= layer
        side_depth[layer] = d
    side_shade = dither_bands(np.clip((side_depth - 1) / (thickness - 1), 0, 1), ROAD_SIDE)
    image[side] = side_shade[side]
    side_seams = side & ((xx - W // 2) % 32 == 0)
    image[side_seams] = ROAD_SIDE_SEAM
    body = road | side
    image[outline_of(body)] = OUTLINE

    # 떠 있는 큐브 (빈 곳만)
    free = ~ndimage.binary_dilation(body, iterations=40)
    for x, y in slots:
        free[max(0, y - 60):y + 60, max(0, x - 70):x + 70] = False
    free[:TOP_PORTAL[1] + 70, TOP_PORTAL[0] - 110:TOP_PORTAL[0] + 110] = False
    placed = 0
    for _ in range(4000):
        size = rng.choice([10, 12, 14, 18])
        x, y = rng.randrange(20, W - 20), rng.randrange(24, H - 60)
        if free[max(0, y - size - 6):y + size * 3 + 6, max(0, x - size - 6):x + size + 6].all():
            draw_cube(image, x, y, size)
            free[max(0, y - 90):y + 90, max(0, x - 90):x + 90] = False
            placed += 1
            if placed >= 7:
                break

    # 가장자리 어둡게 (디더)
    nx = (xx - W / 2) / (W / 2)
    ny = (yy - H / 2) / (H / 2)
    edge = np.clip((nx ** 2 + ny ** 2 - 0.55) / 0.6, 0, 1)
    dark = edge > BAYER[yy % 4, xx % 4]
    image[dark] = (image[dark] * 0.7).astype(np.int32)
    return Image.fromarray(np.clip(image, 0, 255).astype(np.uint8))


def paths_start(paths, name):
    x, y = paths[name][0]
    return int(x), int(y)


def draw_cube(image, cx, cy, size):
    """픽셀아트 큐브 (위·왼쪽·오른쪽 면 + 외곽선) + 바닥 그림자."""
    h = size // 2
    canvas = Image.new("RGBA", (size * 2 + 3, size * 2 + 3), (0, 0, 0, 0))
    draw = ImageDraw.Draw(canvas)
    o = 1
    top = [(size + o, o), (size * 2 + o, h + o), (size + o, size + o), (o, h + o)]
    left = [(o, h + o), (size + o, size + o), (size + o, size * 2 + o), (o, size + h + o)]
    right = [(size * 2 + o, h + o), (size + o, size + o), (size + o, size * 2 + o), (size * 2 + o, size + h + o)]
    draw.polygon(left, fill=CUBE[1] + (255,))
    draw.polygon(right, fill=CUBE[2] + (255,))
    draw.polygon(top, fill=CUBE[0] + (255,))
    draw.line(top + [top[0]], fill=CUBE_EDGE + (255,))
    draw.line([left[0], left[3], left[2], right[3], right[0]], fill=OUTLINE + (255,))
    draw.line([top[2], left[2]], fill=CUBE_EDGE + (255,))
    shadow_y = cy + size * 2 + 6
    image[shadow_y:shadow_y + max(3, size // 3), cx - size + 2:cx + size - 2] = (
        image[shadow_y:shadow_y + max(3, size // 3), cx - size + 2:cx + size - 2] * SHADOW_FACTOR).astype(np.int32)
    array = np.asarray(canvas)
    alpha = array[..., 3] > 0
    y0, x0 = cy - size, cx - size
    region = image[y0:y0 + array.shape[0], x0:x0 + array.shape[1]]
    region[alpha[:region.shape[0], :region.shape[1]]] = array[..., :3][:region.shape[0], :region.shape[1]][alpha[:region.shape[0], :region.shape[1]]]


# ── 길 윗면 (= 길 판정 마스크) ──
def build_road(road):
    """윗면 음영: 가장자리가 빛(왼쪽 위)을 향하면 밝게, 반대면 어둡게 (모서리를 깎은 판 느낌)."""
    yy, xx = np.indices((H, W))
    distance = ndimage.distance_transform_edt(road)
    soft = ndimage.gaussian_filter(road.astype(np.float32), 3)
    gy, gx = np.gradient(soft)
    length = np.hypot(gx, gy) + 1e-6
    lx, ly = LIGHT
    light_length = math.hypot(lx, ly)
    facing = (-gx / length * lx - gy / length * ly) / light_length       # 바깥 방향 법선 · 빛
    bevel = np.clip(1 - (distance - 1) / 8, 0, 1)
    middle = (len(ROAD_SHADES) - 1) / 2
    level = (middle + facing * bevel * middle * 1.1) / (len(ROAD_SHADES) - 1)
    # 가운데는 아주 살짝 밝게 (볼록한 느낌)
    level += np.clip((distance - 8) / 40, 0, 1) * 0.08
    shades = dither_bands(np.clip(level, 0, 1), ROAD_SHADES)

    image = np.zeros((H, W, 4), np.uint8)
    image[road, :3] = shades[road]
    image[road, 3] = 255
    seams = road & (((xx - W // 2) % 32 == 0) | (yy % 32 == 0)) & (distance > 3)
    image[seams, :3] = ROAD_SEAM
    rim = road & (distance <= 1.5)
    image[rim & (facing > 0.25), :3] = ROAD_RIM
    image[rim & (facing < -0.25), :3] = ROAD_DARK_RIM
    return Image.fromarray(image)


# ── 스프라이트 ──
def build_pad():
    """'여기에 설치할 수 있다'만 알려주는 작은 바닥 원. 그림 크기는 클릭 범위 그대로 두고 가운데만 그린다."""
    height = PAD_TOP_H + PAD_SIDE_H
    image = Image.new("RGBA", (PAD_W, height), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    w, h = SLOT_MARK
    x0, y0 = (PAD_W - w) // 2, (height - h) // 2
    draw.ellipse([x0, y0 + 1, x0 + w - 1, y0 + h], fill=OUTLINE)              # 바닥에 살짝 파인 그늘
    draw.ellipse([x0, y0, x0 + w - 1, y0 + h - 1], fill=PAD_NEON)
    draw.ellipse([x0 + 1, y0 + 1, x0 + w - 2, y0 + h - 2], fill=OUTLINE)
    draw.ellipse([x0 + 3, y0 + 3, x0 + w - 4, y0 + h - 3], fill=PAD_INNER)
    draw.line([x0 + 8, y0 + 3, x0 + w - 9, y0 + 3], fill=PAD_TOP)             # 안쪽 윗면 반사
    return image


def build_portal(radius_x, radius_y):
    """블랙홀: 바깥 고리(분홍→시안) + 검은 구멍 + 소용돌이 점."""
    size = (radius_x * 2 + 3, radius_y * 2 + 3)
    image = Image.new("RGBA", size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    cx, cy = radius_x + 1, radius_y + 1
    rings = [(0, OUTLINE), (1, RING[0]), (3, RING[1]), (5, RING[2]), (6, RING[3]), (7, OUTLINE)]
    for inset, color in rings:
        draw.ellipse([cx - radius_x + inset, cy - radius_y + inset, cx + radius_x - inset, cy + radius_y - inset], fill=color)
    inner_x, inner_y = radius_x - 8, radius_y - 8
    draw.ellipse([cx - inner_x, cy - inner_y, cx + inner_x, cy + inner_y], fill=HOLE)
    for arm in range(3):
        for step in range(28):
            t = step / 27
            angle = arm * 2 * math.pi / 3 + t * 4.2
            r = 0.15 + 0.8 * t
            x = cx + math.cos(angle) * inner_x * r
            y = cy + math.sin(angle) * inner_y * r
            color = SWIRL[min(2, int((1 - t) * 3))]
            draw.point((round(x), round(y)), fill=color)
    return image


# ── C# 데이터 구역 다시 쓰기 ──
def to_cs_vector(point):
    return f"new({point[0]:.1f}f, {point[1]:.1f}f)"


def write_builder(paths, slots, top_spawn, right_spawn, goal):
    lines = ["    // ── 맵 데이터 (Tools/gen_map01.py가 만든다. 직접 고치지 말고 생성기를 고칠 것) ──"]
    lines.append(f"    private const float IMAGE_WIDTH = {W}f;")
    lines.append(f"    private const float IMAGE_HEIGHT = {H}f;")
    lines.append(f"    private static readonly Rect VIEW_PX = Rect.MinMaxRect({MARGIN_X}, {MARGIN_Y}, {W - MARGIN_X}, {H - MARGIN_Y});")
    lines.append(f"    private static readonly Vector2 GOAL_PX = {to_cs_vector(goal)};")
    lines.append(f"    private static readonly Vector2 SPAWN_TOP_PX = {to_cs_vector(top_spawn)};")
    lines.append(f"    private static readonly Vector2 SPAWN_RIGHT_PX = {to_cs_vector(right_spawn)};")
    lines.append(f"    private static readonly Vector2 WAVE_ICON_TOP_PX = {to_cs_vector((top_spawn[0] + 56, top_spawn[1]))};")
    lines.append(f"    private static readonly Vector2 WAVE_ICON_RIGHT_PX = {to_cs_vector((right_spawn[0], right_spawn[1] - 56))};")
    lines.append("    private static readonly Dictionary<string, Vector2[]> PATHS_PX = new() {")
    for name, points in paths.items():
        lines.append(f'        ["{name}"] = new Vector2[] {{ ' + ", ".join(to_cs_vector(p) for p in points) + " },")
    lines.append("    };")
    lines.append("    private static readonly Vector2[] SLOTS_PX = { " + ", ".join(to_cs_vector(p) for p in slots) + " };")
    lines.append("    // ── 맵 데이터 끝 ──")
    source = BUILDER.read_text(encoding="utf-8")
    pattern = re.compile(r"    // ── 맵 데이터 \(Tools/gen_map01\.py.*?// ── 맵 데이터 끝 ──", re.S)
    if not pattern.search(source):
        raise RuntimeError("Map01Builder.cs 에 맵 데이터 구역 표시가 없습니다")
    BUILDER.write_text(pattern.sub(lambda _: "\n".join(lines), source), encoding="utf-8")


def main():
    paths = build_paths()
    road = draw_road_mask(paths)
    slots, moved = place_slots(road)
    top_spawn = paths["Path_TopMiddle"][0]
    right_spawn = paths["Path_Right"][0]
    goal = paths["Path_TopMiddle"][-1]

    build_background(road, slots, paths).save(ART_DIR / "Map01_Background.png")
    build_road(road).save(ART_DIR / "Map01_Road.png")
    build_pad().save(ART_DIR / "Pad.png")
    build_portal(PORTAL_TOP_RADIUS, PORTAL_TOP_RADIUS).save(ART_DIR / "Portal_Top.png")
    build_portal(*PORTAL_RIGHT_RADIUS).save(ART_DIR / "Portal_Right.png")
    write_builder(paths, slots, top_spawn, right_spawn, goal)
    print("image", (W, H), "view", (VIEW_W, VIEW_H))
    print("top", top_spawn, "right", right_spawn, "goal", goal)
    print("slots moved:", moved)


if __name__ == "__main__":
    main()
# AI-assisted code end
