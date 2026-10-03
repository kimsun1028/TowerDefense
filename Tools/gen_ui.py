# AI-assisted code start
"""
게임 UI 픽셀아트 생성기 (Pillow). 맵(gen_map01.py)과 같은 팔레트·외곽선을 쓴다.

  python3 Tools/gen_ui.py

결과: Assets/TowerDefense/Art/UI/
  Frame.png        패널 틀 (9-slice, 테두리 7px)
  Button.png       버튼 (9-slice, 테두리 5px)
  PortraitFrame.png 초상화 둥근 틀 (가운데 투명)
  WaveBadge.png    입구 옆 "다음 웨이브" 버튼 (블랙홀) / WaveRing.png 남은 시간 고리
  Icon_*.png       12x12 아이콘 (목숨·골드·웨이브·능력치·버튼, 타워 종류: Hero = 고무 오리 용사)
  PathArrow.png    다음 웨이브 예상 경로 화살표 (길 위에 그려짐, 60px = 1칸)
  RingFrame.png    타워 링 메뉴 고리 / RingButton.png 링 버튼 틀 / CostTag.png 가격표 (9-slice, 테두리 3px)
  SliderTrack.png / SliderFill.png (9-slice, 테두리 3px) / SliderHandle.png  설정 화면 음량 막대
  Toggle.png / ToggleOn.png  켜기·끄기 칸
"""
import math
from pathlib import Path

from PIL import Image, ImageDraw

OUT = Path(__file__).resolve().parent.parent / "Assets/TowerDefense/Art/UI"

OUTLINE = (24, 18, 30)
PANEL = (36, 28, 62)
PANEL_LIGHT = (62, 50, 100)
PANEL_DARK = (28, 21, 48)
METAL = (74, 74, 90)
METAL_LIGHT = (120, 126, 140)
METAL_DARK = (44, 42, 56)
NEON = (90, 220, 240)
PINK = (230, 70, 190)
PINK_LIGHT = (255, 140, 220)
MAGENTA = (150, 30, 120)
HOLE = (10, 6, 20)

# 아이콘 글자 → 색
COLORS = {
    ".": None, "o": OUTLINE, "W": (255, 255, 255),
    "P": (255, 90, 180), "p": (190, 40, 130), "w": (255, 190, 225),          # 분홍 (하트)
    "Y": (255, 204, 70), "y": (200, 140, 40), "l": (255, 240, 170),          # 금색 (코인·별·번개)
    "K": (52, 50, 62), "k": (90, 220, 240),                                  # 칩 / 칩 핀(네온)
    "S": (200, 206, 220), "s": (140, 146, 170), "G": (230, 170, 60), "H": (120, 70, 40),  # 검
    "B": (110, 120, 150), "b": (70, 74, 100), "C": (90, 220, 240),           # 방패
    "V": (170, 110, 255), "v": (110, 60, 190),                               # 마법 결정
    "F": (255, 120, 200), "R": (255, 80, 110), "g": (220, 220, 250), "h": (150, 150, 190),
    "O": (255, 130, 40),                                                     # 오리 부리
}

ICONS = {
    "Heart": [
        "............",
        "..oo....oo..",
        ".oPPo..oPPo.",
        "oPwPPooPPPPo",
        "oPwPPPPPPPpo",
        "oPPPPPPPPPpo",
        ".oPPPPPPPpo.",
        "..oPPPPPpo..",
        "...oPPPpo...",
        "....oPpo....",
        ".....oo.....",
        "............",
    ],
    "HeartBreak": [
        "............",
        "..oo....oo..",
        ".oPPo..oPPo.",
        "oPwPPooPoPPo",
        "oPwPPPPoPPpo",
        "oPPPPPoPPPpo",
        ".oPPPPPoPPo.",
        "..oPPPoPPo..",
        "...oPPoPo...",
        "....oPpo....",
        ".....oo.....",
        "............",
    ],
    "Gear": [
        "....oooo....",
        ".oo.oSSo.oo.",
        ".oSooSSooSo.",
        "..oSSSSSSo..",
        ".ooSSooSSoo.",
        "oSSSo..oSSSo",
        "oSSSo..oSSSo",
        ".ooSSooSSoo.",
        "..oSsSSsSo..",
        ".oSoosSooSo.",
        ".oo.oSso.oo.",
        "....oooo....",
    ],
    "Home": [
        ".....oo.....",
        "....oSSo....",
        "...oSSSSo...",
        "..oSSSSSSo..",
        ".oSSSSSSSSo.",
        "ooooSSSSoooo",
        "..oSSSSSso..",
        "..oSSoooso..",
        "..oSSoPoso..",
        "..oSSoPoso..",
        "..oSsoPoso..",
        "..oooooooo..",
    ],
    "Coin": [
        "....oooo....",
        "..ooYYYYoo..",
        ".oYYllYYYYo.",
        ".oYlYYYYYyo.",
        "oYYYkooKYYyo",
        "oYYYoKKoYYyo",
        "oYYYoKKoYYyo",
        "oYYYkooKYYyo",
        ".oYYYYYYYyo.",
        ".oyYYYYYyyo.",
        "..ooyyyyoo..",
        "....oooo....",
    ],
    "Sword": [
        ".........ooo",
        "........oSWo",
        ".......oSWso",
        "......oSWso.",
        ".....oSWso..",
        ".oo.oSWso...",
        ".oGooWso....",
        "..oGSso.....",
        "...oGo......",
        "..oHooGo....",
        ".oHo..oGo...",
        ".oo....oo...",
    ],
    "Arrow": [
        ".......ooooo",
        "........oWWo",
        ".......oSoWo",
        "......oSo.oo",
        ".....oSo....",
        "....oSo.....",
        ".o.oSo......",
        "oFooSo......",
        "oFFSo.......",
        ".oFFFo......",
        ".oFoFFo.....",
        "..o..oo.....",
    ],
    "Shield": [
        ".oooooooooo.",
        "oBBBBBBBBBBo",
        "oBWWBBBBBBbo",
        "oBWBBCCBBBbo",
        "oBBBBCCBBBbo",
        "oBBCCCCCCBbo",
        "oBBCCCCCCBbo",
        ".oBBBCCBBbo.",
        ".oBBBCCBBbo.",
        "..oBBBBBbo..",
        "...oBBBbo...",
        "....oooo....",
    ],
    "Hero": [   # 고무 오리 용사 (빨간 머리띠)
        "....oooo....",
        "...oYYYYo...",
        "..oRRRRRRo..",
        "..oYYYoYYo..",
        "..oYlYYYYOOo",
        "..oYlYYYOOo.",
        "...oYYYYoo..",
        "o..oYYYYYo..",
        "oYooYYYYYYo.",
        "oYYYYlYYYYyo",
        ".oyYYYYYYyyo",
        "..oooooooo..",
    ],
    "Crystal": [
        ".....oo.....",
        "....oVVo....",
        "...oVWVVo...",
        "..oVWVVVVo..",
        ".oVVVVVVVvo.",
        "oVVVVVVVVvvo",
        "oVVVVVVVvvvo",
        ".oVVVVVVvvo.",
        "..oVVVVvvo..",
        "...oVVvvo...",
        "....ovvo....",
        ".....oo.....",
    ],
    "Speed": [
        "............",
        "oo....oo....",
        "oCo...oCo...",
        ".oCo...oCo..",
        "..oCo...oCo.",
        "...oCo...oCo",
        "...oCo...oCo",
        "..oCo...oCo.",
        ".oCo...oCo..",
        "oCo...oCo...",
        "oo....oo....",
        "............",
    ],
    "Speed1": [
        "............",
        "...oo.......",
        "...oCo......",
        "....oCo.....",
        ".....oCo....",
        "......oCo...",
        "......oCo...",
        ".....oCo....",
        "....oCo.....",
        "...oCo......",
        "...oo.......",
        "............",
    ],
    "Bolt": [
        "......oooo..",
        ".....oYYYo..",
        "....oYlYo...",
        "...oYlYo....",
        "..oYYYYoooo.",
        ".oYYYYYYYYo.",
        ".ooooYYYYo..",
        "....oYYYo...",
        "...oYYyo....",
        "..oYyo......",
        "..oyo.......",
        "..oo........",
    ],
    "Wing": [
        "............",
        "oo..........",
        "ogoo........",
        "ogggoo......",
        ".oggggooo...",
        ".ogggggggoo.",
        "..oggggggggo",
        "..ohgggggggo",
        "...ohggggo..",
        "....ohhho...",
        ".....ooo....",
        "............",
    ],
    "Star": [
        ".....oo.....",
        ".....oYo....",
        "....oYYo....",
        "ooooYlYYoooo",
        "oYYYYlYYYYYo",
        ".oYYYYYYYYo.",
        "..oYYYYYyo..",
        "..oYYYYYyo..",
        ".oYYYooYYyo.",
        ".oYyo..oYyo.",
        "oYoo....ooyo",
        "oo........oo",
    ],
    "Close": [
        "............",
        ".oo......oo.",
        ".oRo....oRo.",
        "..oRo..oRo..",
        "...oRooRo...",
        "....oRRo....",
        "....oRRo....",
        "...oRooRo...",
        "..oRo..oRo..",
        ".oRo....oRo.",
        ".oo......oo.",
        "............",
    ],
    "Pause": [
        "............",
        "..oooo.oooo.",
        "..oWWo.oWWo.",
        "..oWso.oWso.",
        "..oWso.oWso.",
        "..oWso.oWso.",
        "..oWso.oWso.",
        "..oWso.oWso.",
        "..oWso.oWso.",
        "..oWso.oWso.",
        "..oooo.oooo.",
        "............",
    ],
    "Play": [
        "............",
        "..oo........",
        "..oWoo......",
        "..oWWWoo....",
        "..oWWWWWoo..",
        "..oWWWWWWso.",
        "..oWWWWWWso.",
        "..oWWWWsoo..",
        "..oWWsoo....",
        "..oWoo......",
        "..oo........",
        "............",
    ],
    "Up": [
        ".....oo.....",
        "....oCCo....",
        "...oCWWCo...",
        "..oCWWWWCo..",
        ".oCCCWWCCCo.",
        ".ooooWWoooo.",
        "....oWWo....",
        "....oWWo....",
        "....oCCo....",
        "....oCCo....",
        "....oooo....",
        "............",
    ],
    "Lock": [
        "....oooo....",
        "...oSSSSo...",
        "..oSo..oSo..",
        "..oSo..oSo..",
        ".oooooooooo.",
        ".oYYYYYYYYo.",
        ".oYlYooYYyo.",
        ".oYYYooYYyo.",
        ".oYYYYYYYyo.",
        ".oyyyyyyyyo.",
        ".oooooooooo.",
        "............",
    ],
    "Flag": [
        ".oo.........",
        ".oSoooooo...",
        ".oSoPPPPPoo.",
        ".oSoPwPPPPPo",
        ".oSoPPPPPPo.",
        ".oSoPPPPPpo.",
        ".oSooooooo..",
        ".oSo........",
        ".oSo........",
        ".oSo........",
        "ooSoo.......",
        "ooooo.......",
    ],
    "Sell": [
        "....oooo....",
        "..ooYYYYoo..",
        ".oYYYoYYYYo.",
        ".oYYoooooyo.",
        "oYYoYoYYYYyo",
        "oYYYooooYYyo",
        "oYYYYoYoYYyo",
        "oYYoooooYYyo",
        ".oYYYoYYYyo.",
        ".oyYYYYYyyo.",
        "..ooyyyyoo..",
        "....oooo....",
    ],
}


def draw_ascii(rows):
    image = Image.new("RGBA", (12, 12), (0, 0, 0, 0))
    for y, row in enumerate(rows):
        for x, ch in enumerate(row.ljust(12, ".")[:12]):
            color = COLORS.get(ch)
            if color:
                image.putpixel((x, y), color + (255,))
    return image


def draw_circle_icon(kind):
    """원 기반 아이콘: Wave(블랙홀), Clock(시계), Target(사거리)."""
    image = Image.new("RGBA", (12, 12), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    if kind == "Wave":
        draw.ellipse([0, 0, 11, 11], fill=OUTLINE)
        draw.ellipse([1, 1, 10, 10], fill=PINK)
        draw.ellipse([2, 2, 9, 9], fill=PINK_LIGHT)
        draw.ellipse([3, 3, 8, 8], fill=NEON)
        draw.ellipse([4, 4, 7, 7], fill=HOLE)
        draw.point([(1, 4), (2, 3)], fill=(255, 220, 245))
    elif kind == "Clock":
        draw.ellipse([0, 0, 11, 11], fill=OUTLINE)
        draw.ellipse([1, 1, 10, 10], fill=METAL_LIGHT)
        draw.ellipse([2, 2, 9, 9], fill=(230, 232, 240))
        draw.line([5, 3, 5, 6], fill=OUTLINE)
        draw.line([6, 6, 8, 6], fill=OUTLINE)
        draw.point((5, 6), fill=PINK)
    elif kind == "Target":
        draw.ellipse([0, 0, 11, 11], fill=OUTLINE)
        draw.ellipse([1, 1, 10, 10], fill=PINK)
        draw.ellipse([2, 2, 9, 9], fill=OUTLINE)
        draw.ellipse([3, 3, 8, 8], fill=(255, 255, 255))
        draw.ellipse([4, 4, 7, 7], fill=PINK)
        draw.rectangle([5, 5, 6, 6], fill=OUTLINE)
    return image


def frame(size, border):
    """패널: 외곽선 → 금속 테 → 네온 안쪽 선 → 어두운 바탕. 모서리에 분홍 리벳."""
    image = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    s = size - 1
    draw.rectangle([1, 0, s - 1, s], fill=OUTLINE)
    draw.rectangle([0, 1, s, s - 1], fill=OUTLINE)
    draw.rectangle([1, 1, s - 1, s - 1], fill=METAL)
    draw.line([2, 1, s - 2, 1], fill=METAL_LIGHT)
    draw.line([1, 2, 1, s - 2], fill=METAL_LIGHT)
    draw.line([2, s - 1, s - 1, s - 1], fill=METAL_DARK)
    draw.line([s - 1, 2, s - 1, s - 1], fill=METAL_DARK)
    inset = border - 3
    draw.rectangle([inset, inset, s - inset, s - inset], fill=OUTLINE)
    draw.rectangle([inset + 1, inset + 1, s - inset - 1, s - inset - 1], fill=NEON)
    draw.rectangle([inset + 2, inset + 2, s - inset - 2, s - inset - 2], fill=PANEL)
    draw.line([inset + 2, inset + 2, s - inset - 2, inset + 2], fill=PANEL_LIGHT)
    for x, y in ((2, 2), (s - 3, 2), (2, s - 3), (s - 3, s - 3)):
        draw.rectangle([x, y, x + 1, y + 1], fill=PINK)
        draw.point((x, y), fill=PINK_LIGHT)
    return image


def button(size):
    image = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    s = size - 1
    draw.rectangle([1, 0, s - 1, s], fill=OUTLINE)
    draw.rectangle([0, 1, s, s - 1], fill=OUTLINE)
    draw.rectangle([1, 1, s - 1, s - 2], fill=METAL)
    draw.rectangle([2, 2, s - 2, s - 3], fill=PANEL_LIGHT)
    draw.line([2, 1, s - 2, 1], fill=METAL_LIGHT)
    draw.line([1, s - 1, s - 1, s - 1], fill=METAL_DARK)
    draw.line([3, 3, s - 3, 3], fill=(86, 72, 130))
    return image


def portrait_frame(size=34):
    image = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    s = size - 1
    draw.ellipse([0, 0, s, s], fill=OUTLINE)
    draw.ellipse([1, 1, s - 1, s - 1], fill=METAL)
    draw.arc([1, 1, s - 1, s - 1], 160, 290, fill=METAL_LIGHT)
    draw.arc([1, 1, s - 1, s - 1], 340, 110, fill=METAL_DARK)
    draw.ellipse([3, 3, s - 3, s - 3], fill=OUTLINE)
    draw.ellipse([4, 4, s - 4, s - 4], fill=NEON)
    draw.ellipse([5, 5, s - 5, s - 5], fill=(0, 0, 0, 0))
    return image


def wave_badge(size=26):
    """KR의 해골 버튼 자리: 테두리 + 블랙홀 + 소용돌이."""
    image = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    s = size - 1
    draw.ellipse([0, 0, s, s], fill=OUTLINE)
    draw.ellipse([1, 1, s - 1, s - 1], fill=METAL)
    draw.arc([1, 1, s - 1, s - 1], 160, 290, fill=METAL_LIGHT)
    draw.ellipse([3, 3, s - 3, s - 3], fill=OUTLINE)
    draw.ellipse([4, 4, s - 4, s - 4], fill=PINK)
    draw.ellipse([5, 5, s - 5, s - 5], fill=PINK_LIGHT)
    draw.ellipse([6, 6, s - 6, s - 6], fill=NEON)
    draw.ellipse([7, 7, s - 7, s - 7], fill=HOLE)
    c = s / 2
    for arm in range(2):
        for step in range(10):
            t = step / 9
            angle = arm * math.pi + t * 3.6
            r = 1 + t * 4.5
            draw.point((round(c + math.cos(angle) * r), round(c + math.sin(angle) * r)), fill=(150, 60, 180))
    return image


def wave_ring(size=26):
    image = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.ellipse([0, 0, size - 1, size - 1], outline=(255, 230, 120), width=2)
    return image


PATH_ARROW = [
    "oooo..........",
    "oCCWo.........",
    ".oCCWo........",
    "..oCCWo.......",
    "...oCCWo......",
    "....oCCWo.....",
    ".....oCCWo....",
    "......oCCWo...",
    "......oCCWo...",
    ".....oCCWo....",
    "....oCCWo.....",
    "...oCCWo......",
    "..oCCWo.......",
    ".oCCWo........",
    "oCCWo.........",
    "oooo..........",
]


def path_arrow():
    """길 테두리와 같은 네온색 꺾쇠 (오른쪽을 향함, 코드가 길 방향으로 돌린다)."""
    image = Image.new("RGBA", (len(PATH_ARROW[0]), len(PATH_ARROW)), (0, 0, 0, 0))
    for y, row in enumerate(PATH_ARROW):
        for x, ch in enumerate(row):
            color = {"o": OUTLINE, "C": NEON, "W": (210, 250, 255)}.get(ch)
            if color:
                image.putpixel((x, y), color + (255,))
    return image


def ring_frame(size=72):
    """링 메뉴 고리: 외곽선 + 금속 띠(4px) + 리벳 8개. 가운데는 비어 있다."""
    image = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    s = size - 1
    draw.ellipse([0, 0, s, s], fill=OUTLINE)
    draw.ellipse([1, 1, s - 1, s - 1], fill=METAL)
    draw.arc([1, 1, s - 1, s - 1], 150, 300, fill=METAL_LIGHT)
    draw.arc([1, 1, s - 1, s - 1], 330, 120, fill=METAL_DARK)
    draw.ellipse([5, 5, s - 5, s - 5], fill=OUTLINE)
    draw.ellipse([6, 6, s - 6, s - 6], fill=(0, 0, 0, 0))
    c, r = s / 2, s / 2 - 3
    for k in range(8):
        a = k * math.pi / 4 + math.pi / 8
        x, y = round(c + math.cos(a) * r), round(c + math.sin(a) * r)
        draw.rectangle([x - 1, y - 1, x, y], fill=PINK)
    return image


def ring_button(size=22):
    """링 버튼 틀: 둥근 사각 금속 테 + 네온 안쪽 선 + 어두운 바탕."""
    image = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    s = size - 1
    draw.rounded_rectangle([0, 0, s, s], radius=4, fill=OUTLINE)
    draw.rounded_rectangle([1, 1, s - 1, s - 1], radius=3, fill=METAL)
    draw.line([3, 1, s - 3, 1], fill=METAL_LIGHT)
    draw.line([3, s - 1, s - 3, s - 1], fill=METAL_DARK)
    draw.rounded_rectangle([3, 3, s - 3, s - 3], radius=2, fill=OUTLINE)
    draw.rectangle([4, 4, s - 4, s - 4], fill=NEON)
    draw.rectangle([5, 5, s - 5, s - 5], fill=PANEL)
    draw.line([5, 5, s - 5, 5], fill=PANEL_LIGHT)
    return image


def cost_tag():
    image = Image.new("RGBA", (10, 8), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle([0, 0, 9, 7], radius=2, fill=OUTLINE)
    draw.rectangle([1, 1, 8, 6], fill=PANEL_DARK)
    draw.line([2, 1, 7, 1], fill=PANEL_LIGHT)
    return image


def slider_track():
    image = Image.new("RGBA", (10, 8), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle([0, 0, 9, 7], radius=2, fill=OUTLINE)
    draw.rectangle([1, 2, 8, 5], fill=PANEL_DARK)
    draw.line([2, 6, 7, 6], fill=METAL_DARK)
    return image


def slider_fill():
    image = Image.new("RGBA", (10, 8), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.rectangle([1, 2, 8, 5], fill=NEON)
    draw.line([1, 2, 8, 2], fill=(200, 250, 255))
    return image


def slider_handle(size=12):
    image = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    s = size - 1
    draw.rounded_rectangle([0, 0, s, s], radius=3, fill=OUTLINE)
    draw.rounded_rectangle([1, 1, s - 1, s - 1], radius=2, fill=METAL_LIGHT)
    draw.rectangle([3, 3, s - 3, s - 3], fill=PINK)
    draw.point((3, 3), fill=PINK_LIGHT)
    return image


def toggle(on, size=12):
    image = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    s = size - 1
    draw.rounded_rectangle([0, 0, s, s], radius=2, fill=OUTLINE)
    draw.rectangle([1, 1, s - 1, s - 1], fill=METAL)
    draw.rectangle([2, 2, s - 2, s - 2], fill=PANEL_DARK)
    if on:
        draw.rectangle([3, 3, s - 3, s - 3], fill=NEON)
        draw.line([3, 3, s - 3, 3], fill=(200, 250, 255))
    return image


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    frame(24, 7).save(OUT / "Frame.png")
    button(16).save(OUT / "Button.png")
    portrait_frame().save(OUT / "PortraitFrame.png")
    wave_badge().save(OUT / "WaveBadge.png")
    wave_ring().save(OUT / "WaveRing.png")
    path_arrow().save(OUT / "PathArrow.png")
    ring_frame().save(OUT / "RingFrame.png")
    ring_button().save(OUT / "RingButton.png")
    cost_tag().save(OUT / "CostTag.png")
    slider_track().save(OUT / "SliderTrack.png")
    slider_fill().save(OUT / "SliderFill.png")
    slider_handle().save(OUT / "SliderHandle.png")
    toggle(False).save(OUT / "Toggle.png")
    toggle(True).save(OUT / "ToggleOn.png")
    for name, rows in ICONS.items():
        draw_ascii(rows).save(OUT / f"Icon_{name}.png")
    for kind in ("Wave", "Clock", "Target"):
        draw_circle_icon(kind).save(OUT / f"Icon_{kind}.png")
    print("done:", OUT)


if __name__ == "__main__":
    main()
# AI-assisted code end
