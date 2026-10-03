// AI-assisted code start
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 설명 창(GuidePanel)을 캔버스 아래에 만든다 (메인 화면 "?" 버튼).
///   1 목표          : 실제 게임 화면(Art/UI/Guide/Guide_Map.png) + 설명 말풍선
///   2 타워 건설      : 빈 자리 → 링 메뉴 → 지은 타워 (게임 화면 잘라낸 그림 + 실제 타워 스프라이트)
///   3 기본 타워 유형 : 타워 5종 카드 (Assets/Sprites/Towers 첫 프레임, 영웅은 고무 오리 용사)
///   4 적 유형        : 적 8종 카드 (Assets/Sprites/Enemies 첫 프레임)
/// 게임 화면 그림은 게임 씬을 실행해 찍은 것이다 (맵이 바뀌면 다시 찍어서 같은 이름으로 덮어쓰면 된다).
/// 타워·적 이름은 설명용 이름 (게임 안 이름과 따로).
/// </summary>
public static class GuidePanelBuilder {
    // 1. 상수
    private const string GUIDE_DIR = "Guide/";
    private const string TOWER_SPRITE_DIR = "Assets/Sprites/Towers";
    private const string ENEMY_SPRITE_DIR = "Assets/Sprites/Enemies";
    private const string SKIP_LABEL = "건너뛰기";
    private const string NEXT_LABEL = "다음";

    private const float PANEL_WIDTH = 1640f;
    private const float PANEL_HEIGHT = 960f;
    private const float TITLE_TOP = 28f;
    private const float TITLE_HEIGHT = 56f;
    private const float TITLE_FONT_SIZE = 40f;
    private const float FOOTER_BOTTOM = 112f;         // 패널 아래 → 안내 한 줄
    private const float FOOTER_HEIGHT = 44f;
    private const float FOOTER_FONT_SIZE = 28f;
    private const float BUTTON_WIDTH = 260f;
    private const float BUTTON_HEIGHT = 70f;
    private const float BUTTON_MARGIN = 40f;
    private const float BUTTON_FONT_SIZE = 30f;
    private const float PAGE_FONT_SIZE = 24f;
    private const float CONTENT_TOP = 100f;           // 패널 위 → 내용 시작

    // 1장: 게임 화면 + 말풍선
    private static readonly Vector2 MAP_SIZE = new(1180f, 664f);
    private const float CALLOUT_FONT_SIZE = 24f;
    private const float CALLOUT_PADDING_X = 18f;
    private const float CALLOUT_PADDING_Y = 10f;
    private const float CALLOUT_DOT_SIZE = 14f;
    private const float CALLOUT_LINE_WIDTH = 3f;

    // 2장: 세 단계
    private const float STEP_IMAGE_SIZE = 340f;
    private const float STEP_GAP = 500f;              // 가운데 단계 ↔ 양옆 단계
    private const float STEP_ARROW_SIZE = 56f;
    private const float STEP_CAPTION_GAP = 24f;
    private const float STEP_CAPTION_WIDTH = 440f;
    private const float STEP_CAPTION_HEIGHT = 110f;
    private const float STEP_FONT_SIZE = 26f;
    private const float STEP_NUMBER_SIZE = 52f;
    private const float STEP_NUMBER_FONT_SIZE = 30f;
    private static readonly Vector2 BUILT_TOWER_SIZE = new(168f, 224f);   // 96x128 그림 × 1.75
    private static readonly Vector2 BUILT_TOWER_PIVOT = new(0.5f, 0.07f); // 그림 바닥 = 자리 가운데 (스프라이트 pivot과 같게)

    // 3장: 타워 카드
    private static readonly Vector2 TOWER_CARD_SIZE = new(300f, 480f);
    private const float TOWER_CARD_GAP = 18f;
    private static readonly Vector2 TOWER_ART_SIZE = new(180f, 240f);
    private const float TOWER_ART_TOP = 24f;
    private const float CARD_NAME_FONT_SIZE = 34f;
    private const float CARD_ROLE_FONT_SIZE = 22f;
    private const float CARD_TEXT_FONT_SIZE = 22f;
    private const float CARD_PADDING = 18f;
    private const float CARD_LINE_HEIGHT = 44f;

    // 4장: 적 카드
    private static readonly Vector2 ENEMY_CARD_SIZE = new(370f, 300f);
    private const float ENEMY_CARD_GAP = 22f;
    private const float ENEMY_ART_SIZE = 120f;
    private const float ENEMY_ART_TOP = 16f;
    private const float ENEMY_NAME_FONT_SIZE = 28f;
    private const float ENEMY_TEXT_FONT_SIZE = 22f;
    private const int ENEMY_COLUMNS = 4;

    private const float HALF = 0.5f;
    private static readonly Color DIM_COLOR = new(0.03f, 0.02f, 0.07f, 0.85f);
    private static readonly Color TITLE_COLOR = new(1f, 0.6f, 0.9f, 1f);
    private static readonly Color TEXT_COLOR = Color.white;
    private static readonly Color NAME_COLOR = new(0.55f, 0.95f, 1f, 1f);
    private static readonly Color ROLE_COLOR = new(0.75f, 0.72f, 0.85f, 1f);
    private static readonly Color CALLOUT_BACK_COLOR = new(0.04f, 0.03f, 0.08f, 0.88f);
    private static readonly Color CALLOUT_LINE_COLOR = new(1f, 0.45f, 0.85f, 1f);
    private static readonly Color NUMBER_COLOR = new(1f, 0.85f, 0.3f, 1f);

    /// <summary>1장 말풍선: 가리키는 곳(그림 기준 0~1, 왼쪽 아래 = 0), 말풍선 위치(가리키는 곳에서 떨어진 거리), 글.</summary>
    private static readonly (Vector2 point, Vector2 offset, string text)[] GOAL_CALLOUTS = {
        (new Vector2(0.609f, 0.917f), new Vector2(-330f, -30f), "블랙홀에서 적이 몰려와요"),
        (new Vector2(0.94f, 0.56f), new Vector2(-120f, 120f), "오른쪽 포탈에서도 나와요"),
        (new Vector2(0.096f, 0.394f), new Vector2(170f, -150f), "파란 포탈을 지키세요!\n적이 들어가면 목숨이 줄어요"),
        (new Vector2(0.086f, 0.954f), new Vector2(40f, -110f), "목숨 · 골드 · 웨이브"),
        (new Vector2(0.668f, 0.919f), new Vector2(210f, -60f), "누르면 다음 웨이브를 바로 불러요"),
        (new Vector2(0.34f, 0.787f), new Vector2(-40f, -120f), "화살표 = 다음 웨이브 적이 올 길"),
        (new Vector2(0.405f, 0.519f), new Vector2(-40f, -110f), "빈 자리에 타워를 지어 막아요"),
    };

    private static readonly (string image, string caption)[] BUILD_STEPS = {
        ("Guide_Slot", "빈 자리를 클릭해요"),
        ("Guide_Ring", "지을 타워를 골라요\n(숫자는 가격, 숫자키 1~4도 돼요)"),
        ("Guide_Slot", "골드를 내면 완성!\n타워를 다시 누르면 강화 · 판매"),
    };
    private const string BUILT_TOWER_SPRITE = TOWER_SPRITE_DIR + "/ArcherTower/ArcherTower_Lv1_Idle.png";

    private static readonly (string sprite, string name, string role, string text)[] TOWERS = {
        ("ArcherTower/ArcherTower_Lv1_Idle", "방전 코일", "궁수 타워", "전기를 빠르게 쏘아\n하나씩 맞혀요.\n땅 · 하늘 적 모두 공격!"),
        ("MagicTower/MagicTower_Lv1_Idle", "예외 처리기", "마법 타워", "마법 공격은 방어력을\n무시해요.\n단단한 적에게 강해요."),
        ("ArtilleryTower/ArtilleryTower_Lv1_Idle", "방열포", "포병 타워", "떨어진 곳 주변 적을\n한꺼번에 공격해요.\n하늘 적은 못 맞혀요."),
        ("BarracksTower/BarracksTower_Lv1_Idle", "스레드 풀", "병영", "스레더들이 나가서\n길을 막고 싸워요.\n집결지를 옮길 수 있어요."),
        ("HeroTower/Hero/Hero_Lv1_Idle", "용사의 수조", "영웅 타워", "고무 오리 용사가\n나가서 싸워요.\n쓰러져도 다시 나와요."),
    };

    private static readonly (string sprite, string name, string text)[] ENEMIES = {
        ("Goblin/Enemy_Goblin_Walk", "노란 경고 마크", "기본 적. 떼로 몰려와요"),
        ("Wolf/Enemy_Wolf_Walk", "가출한 마우스", "아주 빠르고 공격을 피해요"),
        ("Spider/Enemy_Spider_Walk", "암호화된 압축파일", "빠르고 마법에 강해요"),
        ("Gargoyle/Enemy_Gargoyle_Fly", "떠돌이 클라우드", "하늘을 날아요 (방열포 X)"),
        ("ShadowArcher/Enemy_ShadowArcher_Walk", "피싱 메일", "멀리서 화살을 쏴요"),
        ("DarkKnight/Enemy_DarkKnight_Walk", "방치된 서버", "방어력이 높아요 → 예외 처리기"),
        ("VeznanDemon/Enemy_Veznan_Walk", "수상한 메일", "보스! 체력이 아주 많아요"),
        ("VeznanDemon/Enemy_VeznanDemon_Walk", "랜섬 웨어", "수상한 메일이 변신한 최종 보스"),
    };

    // 4. 메서드 (public)
    /// <summary>parent(캔버스) 아래에 설명 창을 만들고 GuidePanel을 돌려준다. 처음에는 닫혀 있다.</summary>
    public static GuidePanel Create(Transform parent) {
        TMP_FontAsset boldFont = UIFonts.Bold;
        TMP_FontAsset font = UIFonts.Regular;
        Sprite frame = UiArt.Load(UiArt.FRAME_SPRITE, UiArt.FRAME_BORDER);
        Sprite buttonSprite = UiArt.Load(UiArt.BUTTON_SPRITE, UiArt.BUTTON_BORDER);

        Image dim = UiArt.CreateImage("GuidePanel", parent, null, raycast: true);
        UiArt.Stretch(dim.rectTransform);
        dim.color = DIM_COLOR;
        Image panel = UiArt.CreateImage("Panel", dim.transform, frame, sliced: true, raycast: true);
        UiArt.Place(panel.rectTransform, new Vector2(HALF, HALF), Vector2.zero, new Vector2(PANEL_WIDTH, PANEL_HEIGHT));

        GameObject[] pages = {
            CreateGoalPage(panel.transform, boldFont, font),
            CreateBuildPage(panel.transform, boldFont, font),
            CreateTowerPage(panel.transform, boldFont, font, frame),
            CreateEnemyPage(panel.transform, boldFont, font, frame),
        };

        Button skip = CreateButton("Skip", panel.transform, boldFont, buttonSprite, SKIP_LABEL, new Vector2(0f, 0f),
            new Vector2(BUTTON_MARGIN, BUTTON_MARGIN), out _);
        Button next = CreateButton("Next", panel.transform, boldFont, buttonSprite, NEXT_LABEL, new Vector2(1f, 0f),
            new Vector2(-BUTTON_MARGIN, BUTTON_MARGIN), out TMP_Text nextText);
        TMP_Text pageText = UiArt.CreateText("Page", panel.transform, font, PAGE_FONT_SIZE, ROLE_COLOR, TextAlignmentOptions.Center);
        UiArt.Place(pageText.rectTransform, new Vector2(HALF, 0f), new Vector2(0f, BUTTON_MARGIN), new Vector2(BUTTON_WIDTH, BUTTON_HEIGHT));

        GuidePanel guide = dim.gameObject.AddComponent<GuidePanel>();
        UiArt.SetField(guide, "panelRoot", dim.gameObject);
        UiArt.SetArray(guide, "pages", pages);
        UiArt.SetField(guide, "skipButton", skip);
        UiArt.SetField(guide, "nextButton", next);
        UiArt.SetField(guide, "nextText", nextText);
        UiArt.SetField(guide, "pageText", pageText);
        dim.gameObject.SetActive(false);
        return guide;
    }

    // 4. 메서드 (private)
    /// <summary>1장 목표: 실제 게임 화면 위에 무엇이 무엇인지 말풍선.</summary>
    private static GameObject CreateGoalPage(Transform panel, TMP_FontAsset boldFont, TMP_FontAsset font) {
        RectTransform page = CreatePage("Goal", panel, boldFont, "안내 · 목표", font, "목숨이 0이 되면 패배, 모든 웨이브를 막으면 승리!");
        Image map = UiArt.CreateImage("Map", page, UiArt.Load(GUIDE_DIR + "Guide_Map"));
        UiArt.Place(map.rectTransform, new Vector2(HALF, 1f), new Vector2(0f, -CONTENT_TOP), MAP_SIZE);
        foreach ((Vector2 point, Vector2 offset, string text) in GOAL_CALLOUTS) {
            CreateCallout(map.rectTransform, font, point, offset, text);
        }
        return page.gameObject;
    }

    /// <summary>2장 타워 건설: 빈 자리 → 링 메뉴 → 지은 타워.</summary>
    private static GameObject CreateBuildPage(Transform panel, TMP_FontAsset boldFont, TMP_FontAsset font) {
        RectTransform page = CreatePage("Build", panel, boldFont, "타워 건설", font, "버튼에 마우스를 올리면 지었을 때의 사거리가 보여요");
        float imageTop = -CONTENT_TOP - STEP_NUMBER_SIZE;
        for (int i = 0; i < BUILD_STEPS.Length; i++) {
            float x = (i - (BUILD_STEPS.Length - 1) * HALF) * STEP_GAP;
            Image image = UiArt.CreateImage($"Step{i + 1}", page, UiArt.Load(GUIDE_DIR + BUILD_STEPS[i].image));
            UiArt.Place(image.rectTransform, new Vector2(HALF, 1f), new Vector2(x, imageTop), Vector2.one * STEP_IMAGE_SIZE);
            image.rectTransform.pivot = new Vector2(HALF, 1f);

            TMP_Text number = UiArt.CreateText("Number", image.transform, boldFont, STEP_NUMBER_FONT_SIZE, NUMBER_COLOR, TextAlignmentOptions.Center);
            UiArt.Place(number.rectTransform, new Vector2(HALF, 1f), new Vector2(0f, STEP_NUMBER_SIZE), Vector2.one * STEP_NUMBER_SIZE);
            number.text = (i + 1).ToString();

            TMP_Text caption = UiArt.CreateText("Caption", image.transform, font, STEP_FONT_SIZE, TEXT_COLOR, TextAlignmentOptions.Top);
            UiArt.Place(caption.rectTransform, new Vector2(HALF, 0f), new Vector2(0f, -STEP_CAPTION_GAP), new Vector2(STEP_CAPTION_WIDTH, STEP_CAPTION_HEIGHT));
            caption.rectTransform.pivot = new Vector2(HALF, 1f);
            caption.text = BUILD_STEPS[i].caption;

            if (i == BUILD_STEPS.Length - 1) {
                Image tower = UiArt.CreateImage("Tower", image.transform, LoadFirstFrame(BUILT_TOWER_SPRITE));
                tower.preserveAspect = true;
                UiArt.Place(tower.rectTransform, new Vector2(HALF, HALF), Vector2.zero, BUILT_TOWER_SIZE);
                tower.rectTransform.pivot = BUILT_TOWER_PIVOT;
            }
            if (i > 0) {
                Image arrow = UiArt.CreateImage("Arrow", page, UiArt.LoadIcon("Play"));
                UiArt.Place(arrow.rectTransform, new Vector2(HALF, 1f), new Vector2(x - STEP_GAP * HALF, imageTop - STEP_IMAGE_SIZE * HALF),
                    Vector2.one * STEP_ARROW_SIZE);
                arrow.rectTransform.pivot = new Vector2(HALF, HALF);
            }
        }
        return page.gameObject;
    }

    /// <summary>3장 기본 타워 유형: 5종 카드.</summary>
    private static GameObject CreateTowerPage(Transform panel, TMP_FontAsset boldFont, TMP_FontAsset font, Sprite frame) {
        RectTransform page = CreatePage("Towers", panel, boldFont, "기본 타워 유형", font, "적에 맞는 타워를 섞어서 지어 보세요");
        for (int i = 0; i < TOWERS.Length; i++) {
            float x = (i - (TOWERS.Length - 1) * HALF) * (TOWER_CARD_SIZE.x + TOWER_CARD_GAP);
            Image card = UiArt.CreateImage(TOWERS[i].name, page, frame, sliced: true);
            UiArt.Place(card.rectTransform, new Vector2(HALF, 1f), new Vector2(x, -CONTENT_TOP), TOWER_CARD_SIZE);
            card.rectTransform.pivot = new Vector2(HALF, 1f);

            Image art = UiArt.CreateImage("Art", card.transform, LoadFirstFrame($"{TOWER_SPRITE_DIR}/{TOWERS[i].sprite}.png"));
            art.preserveAspect = true;
            UiArt.Place(art.rectTransform, new Vector2(HALF, 1f), new Vector2(0f, -TOWER_ART_TOP), TOWER_ART_SIZE);

            float top = TOWER_ART_TOP + TOWER_ART_SIZE.y;
            TMP_Text name = CreateCardText("Name", card.transform, boldFont, CARD_NAME_FONT_SIZE, NAME_COLOR, top, TOWER_CARD_SIZE.x);
            name.text = TOWERS[i].name;
            TMP_Text role = CreateCardText("Role", card.transform, font, CARD_ROLE_FONT_SIZE, ROLE_COLOR, top + CARD_LINE_HEIGHT, TOWER_CARD_SIZE.x);
            role.text = TOWERS[i].role;
            TMP_Text text = CreateCardText("Text", card.transform, font, CARD_TEXT_FONT_SIZE, TEXT_COLOR, top + CARD_LINE_HEIGHT * 2f, TOWER_CARD_SIZE.x);
            text.text = TOWERS[i].text;
        }
        return page.gameObject;
    }

    /// <summary>4장 적 유형: 8종 카드 (4 x 2).</summary>
    private static GameObject CreateEnemyPage(Transform panel, TMP_FontAsset boldFont, TMP_FontAsset font, Sprite frame) {
        RectTransform page = CreatePage("Enemies", panel, boldFont, "적 유형", font, "적마다 약점이 달라요");
        for (int i = 0; i < ENEMIES.Length; i++) {
            int column = i % ENEMY_COLUMNS;
            int row = i / ENEMY_COLUMNS;
            float x = (column - (ENEMY_COLUMNS - 1) * HALF) * (ENEMY_CARD_SIZE.x + ENEMY_CARD_GAP);
            float y = -CONTENT_TOP - row * (ENEMY_CARD_SIZE.y + ENEMY_CARD_GAP);
            Image card = UiArt.CreateImage(ENEMIES[i].name, page, frame, sliced: true);
            UiArt.Place(card.rectTransform, new Vector2(HALF, 1f), new Vector2(x, y), ENEMY_CARD_SIZE);
            card.rectTransform.pivot = new Vector2(HALF, 1f);

            Image art = UiArt.CreateImage("Art", card.transform, LoadFirstFrame($"{ENEMY_SPRITE_DIR}/{ENEMIES[i].sprite}.png"));
            art.preserveAspect = true;
            UiArt.Place(art.rectTransform, new Vector2(HALF, 1f), new Vector2(0f, -ENEMY_ART_TOP), Vector2.one * ENEMY_ART_SIZE);

            float top = ENEMY_ART_TOP + ENEMY_ART_SIZE + CARD_PADDING * HALF;
            TMP_Text name = CreateCardText("Name", card.transform, boldFont, ENEMY_NAME_FONT_SIZE, NAME_COLOR, top, ENEMY_CARD_SIZE.x);
            name.text = ENEMIES[i].name;
            TMP_Text text = CreateCardText("Text", card.transform, font, ENEMY_TEXT_FONT_SIZE, TEXT_COLOR, top + CARD_LINE_HEIGHT, ENEMY_CARD_SIZE.x);
            text.text = ENEMIES[i].text;
        }
        return page.gameObject;
    }

    /// <summary>한 장: 제목(위) + 안내 한 줄(아래, 버튼 위). 내용은 돌려받은 곳에 붙인다.</summary>
    private static RectTransform CreatePage(string objectName, Transform panel, TMP_FontAsset boldFont, string title,
                                            TMP_FontAsset font, string footer) {
        RectTransform page = UiArt.CreateRect(objectName, panel);
        UiArt.Stretch(page);
        TMP_Text titleText = UiArt.CreateText("Title", page, boldFont, TITLE_FONT_SIZE, TITLE_COLOR, TextAlignmentOptions.Center);
        UiArt.Place(titleText.rectTransform, new Vector2(HALF, 1f), new Vector2(0f, -TITLE_TOP), new Vector2(PANEL_WIDTH, TITLE_HEIGHT));
        titleText.text = title;
        TMP_Text footerText = UiArt.CreateText("Footer", page, font, FOOTER_FONT_SIZE, NUMBER_COLOR, TextAlignmentOptions.Center);
        UiArt.Place(footerText.rectTransform, new Vector2(HALF, 0f), new Vector2(0f, FOOTER_BOTTOM), new Vector2(PANEL_WIDTH, FOOTER_HEIGHT));
        footerText.text = footer;
        return page;
    }

    /// <summary>점(가리키는 곳) — 선 — 검은 말풍선(흰 글자). point는 그림 기준 0~1.</summary>
    private static void CreateCallout(RectTransform image, TMP_FontAsset font, Vector2 point, Vector2 offset, string message) {
        RectTransform anchor = UiArt.CreateRect("Callout", image);
        UiArt.Place(anchor, point, Vector2.zero, Vector2.zero);

        Image line = UiArt.CreateImage("Line", anchor, null);
        line.color = CALLOUT_LINE_COLOR;
        line.rectTransform.anchorMin = line.rectTransform.anchorMax = new Vector2(HALF, HALF);
        line.rectTransform.pivot = new Vector2(0f, HALF);
        line.rectTransform.anchoredPosition = Vector2.zero;
        line.rectTransform.sizeDelta = new Vector2(offset.magnitude, CALLOUT_LINE_WIDTH);
        line.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg);

        Image dot = UiArt.CreateImage("Dot", anchor, null);
        dot.color = CALLOUT_LINE_COLOR;
        UiArt.Place(dot.rectTransform, new Vector2(HALF, HALF), Vector2.zero, Vector2.one * CALLOUT_DOT_SIZE);

        Image box = UiArt.CreateImage("Box", anchor, null);
        box.color = CALLOUT_BACK_COLOR;
        TMP_Text text = UiArt.CreateText("Text", box.transform, font, CALLOUT_FONT_SIZE, TEXT_COLOR, TextAlignmentOptions.Center);
        text.text = message;
        Vector2 size = text.GetPreferredValues(message) + new Vector2(CALLOUT_PADDING_X, CALLOUT_PADDING_Y) * 2f;
        UiArt.Place(box.rectTransform, new Vector2(HALF, HALF), offset, size);
        UiArt.Stretch(text.rectTransform);
    }

    /// <summary>카드 위에서 top만큼 내려온 가운데 정렬 글자 (여러 줄이면 아래로 늘어난다).</summary>
    private static TMP_Text CreateCardText(string objectName, Transform card, TMP_FontAsset font, float size, Color color,
                                           float top, float cardWidth) {
        TMP_Text text = UiArt.CreateText(objectName, card, font, size, color, TextAlignmentOptions.Top);
        UiArt.Place(text.rectTransform, new Vector2(HALF, 1f), new Vector2(0f, -top), new Vector2(cardWidth - CARD_PADDING * 2f, CARD_LINE_HEIGHT));
        return text;
    }

    private static Button CreateButton(string objectName, Transform parent, TMP_FontAsset font, Sprite sprite, string label,
                                       Vector2 corner, Vector2 position, out TMP_Text text) {
        Image background = UiArt.CreateImage(objectName, parent, sprite, sliced: true, raycast: true);
        UiArt.Place(background.rectTransform, corner, position, new Vector2(BUTTON_WIDTH, BUTTON_HEIGHT));
        Button button = background.gameObject.AddComponent<Button>();
        text = UiArt.CreateText("Text", background.transform, font, BUTTON_FONT_SIZE, TEXT_COLOR, TextAlignmentOptions.Center);
        UiArt.Stretch(text.rectTransform);
        text.text = label;
        return button;
    }

    /// <summary>가로로 잘린 시트의 첫 프레임 (가장 왼쪽 조각). 잘리지 않았으면 그림 전체.</summary>
    private static Sprite LoadFirstFrame(string path) {
        Sprite first = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(sprite => sprite.rect.x).FirstOrDefault();
        if (first == null) {
            Debug.LogWarning($"[GuidePanelBuilder] 스프라이트가 없습니다: {path}");
        }
        return first;
    }
}
// AI-assisted code end
