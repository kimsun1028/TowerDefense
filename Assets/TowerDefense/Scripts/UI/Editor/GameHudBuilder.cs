// AI-assisted code start
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tools > TD > UI > Build Game HUD
/// 게임 화면 UI 프리팹(Prefabs/UI/GameHud.prefab):
///   왼쪽 위 목숨·골드 / WAVE 3/15, 오른쪽 위 배속(정지·1·2배속 한 버튼)·메뉴(계속/설정/메인 메뉴),
///   설정 창(소리), 입구 옆 "다음 웨이브" 블랙홀 버튼, 타워 링 메뉴, 다음 웨이브 예상 경로 화살표.
/// 목숨·골드·배속은 게임 매니저가 GameHud에 연결한다 (SetLives / SetGold / SpeedRequested).
/// </summary>
public static class GameHudBuilder {
    // 1. 상수
    private const string PREFAB_DIR = "Assets/TowerDefense/Prefabs/UI";
    private const string PREFAB_PATH = PREFAB_DIR + "/GameHud.prefab";
    private const int CANVAS_SORTING_ORDER = 5;
    private const int RING_BUTTON_COUNT = 6;
    private const float PATH_ARROW_PIXELS_PER_UNIT = 60f;

    private const float MARGIN = 16f;
    private const float PANEL_HEIGHT = 70f;
    private const float STATS_WIDTH = 300f;
    private const float WAVE_WIDTH = 250f;
    private const float PANEL_GAP = 10f;
    private const float ICON_SIZE = 36f;
    private const float ICON_LEFT = 24f;              // 패널 왼쪽 끝 → 아이콘
    private const float VALUE_LEFT = 68f;             // 패널 왼쪽 끝 → 값 글자
    private const float LIVES_WIDTH = 80f;
    private const float GOLD_ICON_LEFT = 156f;
    private const float GOLD_VALUE_LEFT = 200f;
    private const float GOLD_WIDTH = 90f;
    private const float WAVE_TEXT_WIDTH = 170f;
    private const float BUTTON_SIZE = 70f;
    private const float WAVE_BADGE_SIZE = 78f;
    private const float RING_SIZE = 216f;
    private const float RING_BUTTON_SIZE = 66f;
    private const float COST_TAG_WIDTH = 64f;
    private const float COST_TAG_HEIGHT = 26f;
    private const float TOOLTIP_HEIGHT = 34f;
    private const float TOOLTIP_OFFSET = 150f;
    private const float MENU_WIDTH = 380f;
    private const float MENU_HEIGHT = 330f;
    private const float MENU_BUTTON_WIDTH = 300f;
    private const float MENU_BUTTON_HEIGHT = 64f;
    private const float MENU_BUTTON_GAP = 14f;
    private const float MENU_TITLE_TOP = 26f;
    private const float MENU_FONT_SIZE = 24f;
    private const float MENU_TITLE_FONT_SIZE = 28f;
    private const float MENU_TITLE_HEIGHT = 40f;
    private const float MENU_FIRST_BUTTON_GAP = 52f;   // 제목 위 → 첫 버튼
    private const float MENU_TEXT_LEFT = 80f;          // 버튼 왼쪽 끝 → 글자 (아이콘 오른쪽)
    private const float MENU_TEXT_RIGHT = 16f;
    private const float RESULT_TITLE_HEIGHT = 64f;
    private const float RESULT_FIRST_BUTTON_GAP = 80f;
    private const float SHORTCUT_OVERHANG = 0.3f;      // 단축키 표시가 버튼 모서리 밖으로 나오는 비율
    private const int TOOLTIP_PADDING = 12;
    private const float RESULT_HEIGHT = 300f;
    private const float RESULT_TITLE_FONT_SIZE = 48f;
    private static readonly Color DIM_COLOR = new(0.04f, 0.02f, 0.08f, 0.6f);

    private const float STAT_FONT_SIZE = 28f;
    private const float WAVE_FONT_SIZE = 26f;
    private const float COST_FONT_SIZE = 18f;
    private const float SHORTCUT_SIZE = 26f;
    private const float SHORTCUT_FONT_SIZE = 18f;
    private const float TOOLTIP_FONT_SIZE = 19f;
    private static readonly Color TEXT_COLOR = new(0.95f, 0.95f, 1f, 1f);
    private static readonly Color TOOLTIP_COLOR = Color.white;
    private static readonly Color WAVE_COLOR = new(1f, 0.6f, 0.9f, 1f);

    // 4. 메서드 (public)
    [MenuItem("Tools/TD/UI/Build Game HUD")]
    public static void BuildFromMenu() {
        Build();
    }

    public static GameObject Build() {
        Directory.CreateDirectory(PREFAB_DIR);
        TMP_FontAsset boldFont = UIFonts.Bold;
        Sprite frame = UiArt.Load(UiArt.FRAME_SPRITE, UiArt.FRAME_BORDER);
        Sprite buttonSprite = UiArt.Load(UiArt.BUTTON_SPRITE, UiArt.BUTTON_BORDER);
        GameObject root = UiArt.CreateCanvas("GameHud", CANVAS_SORTING_ORDER);

        // 왼쪽 위: 목숨 · 골드
        Image stats = UiArt.CreateImage("Stats", root.transform, frame, sliced: true, raycast: true);
        UiArt.Place(stats.rectTransform, new Vector2(0f, 1f), new Vector2(MARGIN, -MARGIN), new Vector2(STATS_WIDTH, PANEL_HEIGHT));
        CreateIcon("LivesIcon", stats.transform, "Heart", ICON_LEFT);
        TMP_Text livesText = CreateValue("Lives", stats.transform, boldFont, STAT_FONT_SIZE, TEXT_COLOR, VALUE_LEFT, LIVES_WIDTH);
        CreateIcon("GoldIcon", stats.transform, "Coin", GOLD_ICON_LEFT);
        TMP_Text goldText = CreateValue("Gold", stats.transform, boldFont, STAT_FONT_SIZE, TEXT_COLOR, GOLD_VALUE_LEFT, GOLD_WIDTH);

        // 웨이브
        Image wave = UiArt.CreateImage("Wave", root.transform, frame, sliced: true, raycast: true);
        UiArt.Place(wave.rectTransform, new Vector2(0f, 1f), new Vector2(MARGIN + STATS_WIDTH + PANEL_GAP, -MARGIN),
            new Vector2(WAVE_WIDTH, PANEL_HEIGHT));
        CreateIcon("WaveIcon", wave.transform, "Wave", ICON_LEFT);
        TMP_Text waveText = CreateValue("WaveText", wave.transform, boldFont, WAVE_FONT_SIZE, WAVE_COLOR, VALUE_LEFT, WAVE_TEXT_WIDTH);
        waveText.text = "WAVE 0/0";

        // 오른쪽 위: 배속(한 버튼) · 메뉴
        (Button menuButton, _) = CreateIconButton("Menu", root.transform, buttonSprite, "Gear",
            new Vector2(-MARGIN, -MARGIN));
        (Button speedButton, Image speedIcon) = CreateIconButton("Speed", root.transform, buttonSprite, "Speed1",
            new Vector2(-MARGIN - BUTTON_SIZE - PANEL_GAP, -MARGIN));

        CreateWaveCalls(root.transform);
        CreateRingMenu(root.transform, boldFont);
        (GameObject menuPanel, Button resume, Button settings, Button mainMenu) = CreateMenu(root.transform, boldFont, frame, buttonSprite);
        (GameObject resultPanel, TMP_Text resultTitle, Button retry, Button resultMainMenu) = CreateResult(root.transform, boldFont, frame, buttonSprite);
        SettingsPanel settingsPanel = SettingsPanelBuilder.Create(root.transform, boldFont);

        GameHud hud = root.AddComponent<GameHud>();
        UiArt.SetField(hud, "livesText", livesText);
        UiArt.SetField(hud, "goldText", goldText);
        UiArt.SetField(hud, "waveText", waveText);
        UiArt.SetField(hud, "speedButton", speedButton);
        UiArt.SetField(hud, "speedIcon", speedIcon);
        UiArt.SetField(hud, "normalSpeedSprite", UiArt.LoadIcon("Speed1"));
        UiArt.SetField(hud, "fastSpeedSprite", UiArt.LoadIcon("Speed"));
        UiArt.SetField(hud, "pausedSprite", UiArt.LoadIcon("Pause"));
        UiArt.SetField(hud, "menuButton", menuButton);
        UiArt.SetField(hud, "menuPanel", menuPanel);
        UiArt.SetField(hud, "resumeButton", resume);
        UiArt.SetField(hud, "settingsButton", settings);
        UiArt.SetField(hud, "mainMenuButton", mainMenu);
        UiArt.SetField(hud, "settingsPanel", settingsPanel);
        UiArt.SetField(hud, "resultPanel", resultPanel);
        UiArt.SetField(hud, "resultTitle", resultTitle);
        UiArt.SetField(hud, "retryButton", retry);
        UiArt.SetField(hud, "resultMainMenuButton", resultMainMenu);

        WavePathPreview preview = root.AddComponent<WavePathPreview>();
        UiArt.SetField(preview, "arrowSprite", UiArt.Load("PathArrow", 0, PATH_ARROW_PIXELS_PER_UNIT));

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        return prefab;
    }

    // 4. 메서드 (private)
    private static Image CreateIcon(string objectName, Transform parent, string icon, float left) {
        Image image = UiArt.CreateImage(objectName, parent, UiArt.LoadIcon(icon));
        UiArt.Place(image.rectTransform, new Vector2(0f, 0.5f), new Vector2(left, 0f), Vector2.one * ICON_SIZE);
        return image;
    }

    private static TMP_Text CreateValue(string objectName, Transform parent, TMP_FontAsset font, float size, Color color,
                                        float left, float width) {
        TMP_Text text = UiArt.CreateText(objectName, parent, font, size, color, TextAlignmentOptions.MidlineLeft);
        UiArt.Place(text.rectTransform, new Vector2(0f, 0.5f), new Vector2(left, 0f), new Vector2(width, PANEL_HEIGHT));
        text.text = "0";
        return text;
    }

    private static (Button, Image) CreateIconButton(string objectName, Transform parent, Sprite sprite, string icon, Vector2 position) {
        Image background = UiArt.CreateImage(objectName, parent, sprite, sliced: true, raycast: true);
        UiArt.Place(background.rectTransform, new Vector2(1f, 1f), position, Vector2.one * BUTTON_SIZE);
        Button button = background.gameObject.AddComponent<Button>();
        Image iconImage = UiArt.CreateImage("Icon", background.transform, UiArt.LoadIcon(icon));
        UiArt.Place(iconImage.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * ICON_SIZE);
        return (button, iconImage);
    }

    /// <summary>가운데 메뉴: 화면을 어둡게 덮는 판(클릭 막음) + 틀 + 제목 + 버튼 3개.</summary>
    private static (GameObject, Button, Button, Button) CreateMenu(Transform parent, TMP_FontAsset font, Sprite frame, Sprite buttonSprite) {
        Image dim = UiArt.CreateImage("PauseMenu", parent, null, raycast: true);
        UiArt.Stretch(dim.rectTransform);
        dim.color = DIM_COLOR;
        Image panel = UiArt.CreateImage("Panel", dim.transform, frame, sliced: true, raycast: true);
        UiArt.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(MENU_WIDTH, MENU_HEIGHT));
        TMP_Text title = UiArt.CreateText("Title", panel.transform, font, MENU_TITLE_FONT_SIZE, WAVE_COLOR, TextAlignmentOptions.Center);
        UiArt.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -MENU_TITLE_TOP), new Vector2(MENU_BUTTON_WIDTH, MENU_TITLE_HEIGHT));
        title.text = "일시 정지";

        float top = MENU_TITLE_TOP + MENU_FIRST_BUTTON_GAP;
        Button resume = CreateMenuButton("Resume", panel.transform, font, buttonSprite, "Play", "계속하기", top);
        Button settings = CreateMenuButton("Settings", panel.transform, font, buttonSprite, "Gear", "설정",
            top + MENU_BUTTON_HEIGHT + MENU_BUTTON_GAP);
        Button mainMenu = CreateMenuButton("MainMenu", panel.transform, font, buttonSprite, "Home", "메인 메뉴",
            top + (MENU_BUTTON_HEIGHT + MENU_BUTTON_GAP) * 2f);
        return (dim.gameObject, resume, settings, mainMenu);
    }

    /// <summary>승리/패배 창: 어두운 판 + 틀 + 큰 제목(승리!/패배...) + 버튼 2개(다시 하기, 메인 메뉴).</summary>
    private static (GameObject, TMP_Text, Button, Button) CreateResult(Transform parent, TMP_FontAsset font, Sprite frame, Sprite buttonSprite) {
        Image dim = UiArt.CreateImage("ResultPanel", parent, null, raycast: true);
        UiArt.Stretch(dim.rectTransform);
        dim.color = DIM_COLOR;
        Image panel = UiArt.CreateImage("Panel", dim.transform, frame, sliced: true, raycast: true);
        UiArt.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(MENU_WIDTH, RESULT_HEIGHT));
        TMP_Text title = UiArt.CreateText("Title", panel.transform, font, RESULT_TITLE_FONT_SIZE, TEXT_COLOR, TextAlignmentOptions.Center);
        UiArt.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -MENU_TITLE_TOP), new Vector2(MENU_BUTTON_WIDTH, RESULT_TITLE_HEIGHT));
        float top = MENU_TITLE_TOP + RESULT_FIRST_BUTTON_GAP;
        Button retry = CreateMenuButton("Retry", panel.transform, font, buttonSprite, "Play", "다시 하기", top);
        Button mainMenu = CreateMenuButton("MainMenu", panel.transform, font, buttonSprite, "Home", "메인 메뉴",
            top + MENU_BUTTON_HEIGHT + MENU_BUTTON_GAP);
        return (dim.gameObject, title, retry, mainMenu);
    }

    private static Button CreateMenuButton(string objectName, Transform parent, TMP_FontAsset font, Sprite sprite,
                                           string icon, string label, float top) {
        Image background = UiArt.CreateImage(objectName, parent, sprite, sliced: true, raycast: true);
        UiArt.Place(background.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -top),
            new Vector2(MENU_BUTTON_WIDTH, MENU_BUTTON_HEIGHT));
        Button button = background.gameObject.AddComponent<Button>();
        Image iconImage = UiArt.CreateImage("Icon", background.transform, UiArt.LoadIcon(icon));
        UiArt.Place(iconImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(ICON_LEFT, 0f), Vector2.one * ICON_SIZE);
        TMP_Text text = UiArt.CreateText("Text", background.transform, font, MENU_FONT_SIZE, TEXT_COLOR, TextAlignmentOptions.MidlineLeft);
        UiArt.Place(text.rectTransform, new Vector2(0f, 0.5f), new Vector2(MENU_TEXT_LEFT, 0f),
            new Vector2(MENU_BUTTON_WIDTH - MENU_TEXT_LEFT - MENU_TEXT_RIGHT, MENU_BUTTON_HEIGHT));
        text.text = label;
        return button;
    }

    /// <summary>입구 옆 버튼: 화면 전체 영역 + 버튼 원본 1개 (실행할 때 입구 수만큼 복제).</summary>
    private static void CreateWaveCalls(Transform parent) {
        RectTransform area = UiArt.CreateRect("WaveCalls", parent);
        UiArt.Stretch(area);
        Image badge = UiArt.CreateImage("WaveCallTemplate", area, UiArt.Load("WaveBadge"), raycast: true);
        UiArt.Place(badge.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * WAVE_BADGE_SIZE);
        badge.gameObject.AddComponent<Button>();
        Image ring = UiArt.CreateImage("Ring", badge.transform, UiArt.Load("WaveRing"));
        UiArt.Stretch(ring.rectTransform);
        ring.type = Image.Type.Filled;
        ring.fillMethod = Image.FillMethod.Radial360;
        ring.fillOrigin = (int)Image.Origin360.Top;
        ring.fillClockwise = true;

        WaveCallButtons waveCalls = area.gameObject.AddComponent<WaveCallButtons>();
        UiArt.SetField(waveCalls, "template", badge.rectTransform);
    }

    /// <summary>링 메뉴: 화면 전체 영역 > 메뉴(고리 + 버튼 6개 + 툴팁).</summary>
    private static void CreateRingMenu(Transform parent, TMP_FontAsset font) {
        RectTransform area = UiArt.CreateRect("RingMenu", parent);
        UiArt.Stretch(area);
        RectTransform menu = UiArt.CreateRect("Menu", area);
        UiArt.Place(menu, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * RING_SIZE);
        Image ring = UiArt.CreateImage("Ring", menu, UiArt.Load("RingFrame"));
        UiArt.Stretch(ring.rectTransform);

        Sprite buttonSprite = UiArt.Load("RingButton");
        Sprite costSprite = UiArt.Load(UiArt.TAG_SPRITE, UiArt.TAG_BORDER);
        Sprite lockSprite = UiArt.LoadIcon("Lock");
        var buttons = new Object[RING_BUTTON_COUNT];
        for (int i = 0; i < RING_BUTTON_COUNT; i++) {
            Image background = UiArt.CreateImage($"Option{i}", menu, buttonSprite, raycast: true);
            UiArt.Place(background.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * RING_BUTTON_SIZE);
            Button button = background.gameObject.AddComponent<Button>();
            Image icon = UiArt.CreateImage("Icon", background.transform, null);
            UiArt.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * ICON_SIZE);
            Image lockMark = UiArt.CreateImage("Lock", background.transform, lockSprite);
            UiArt.Place(lockMark.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * ICON_SIZE);
            Image cost = UiArt.CreateImage("Cost", background.transform, costSprite, sliced: true);
            UiArt.Place(cost.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -COST_TAG_HEIGHT * 0.5f),
                new Vector2(COST_TAG_WIDTH, COST_TAG_HEIGHT));
            TMP_Text costText = UiArt.CreateText("Text", cost.transform, font, COST_FONT_SIZE, Color.white, TextAlignmentOptions.Center);
            UiArt.Stretch(costText.rectTransform);

            // 단축키 표시 (왼쪽 위 모서리)
            Image shortcut = UiArt.CreateImage("Shortcut", background.transform, costSprite, sliced: true);
            UiArt.Place(shortcut.rectTransform, new Vector2(0f, 1f), new Vector2(-SHORTCUT_SIZE * SHORTCUT_OVERHANG, SHORTCUT_SIZE * SHORTCUT_OVERHANG),
                Vector2.one * SHORTCUT_SIZE);
            TMP_Text shortcutText = UiArt.CreateText("Text", shortcut.transform, font, SHORTCUT_FONT_SIZE, Color.white, TextAlignmentOptions.Center);
            UiArt.Stretch(shortcutText.rectTransform);

            RingMenuButton ringButton = background.gameObject.AddComponent<RingMenuButton>();
            UiArt.SetField(ringButton, "button", button);
            UiArt.SetField(ringButton, "icon", icon);
            UiArt.SetField(ringButton, "lockMark", lockMark.gameObject);
            UiArt.SetField(ringButton, "costTag", cost.gameObject);
            UiArt.SetField(ringButton, "costText", costText);
            UiArt.SetField(ringButton, "shortcutTag", shortcut.gameObject);
            UiArt.SetField(ringButton, "shortcutText", shortcutText);
            buttons[i] = ringButton;
        }

        // 툴팁 (고리 위, 이름 길이에 맞춰 너비 조절)
        Image tooltip = UiArt.CreateImage("Tooltip", menu, costSprite, sliced: true); // 얇은 틀
        UiArt.Place(tooltip.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, TOOLTIP_OFFSET), new Vector2(0f, TOOLTIP_HEIGHT));
        HorizontalLayoutGroup layout = tooltip.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(TOOLTIP_PADDING, TOOLTIP_PADDING, 0, 0);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        tooltip.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        TMP_Text tooltipText = UiArt.CreateText("Text", tooltip.transform, font, TOOLTIP_FONT_SIZE, TOOLTIP_COLOR, TextAlignmentOptions.Center);

        RingMenu ringMenu = area.gameObject.AddComponent<RingMenu>();
        UiArt.SetField(ringMenu, "menuRoot", menu);
        UiArt.SetArray(ringMenu, "buttons", buttons);
        UiArt.SetField(ringMenu, "tooltipRoot", tooltip.gameObject);
        UiArt.SetField(ringMenu, "tooltipText", tooltipText);
    }
}
// AI-assisted code end
