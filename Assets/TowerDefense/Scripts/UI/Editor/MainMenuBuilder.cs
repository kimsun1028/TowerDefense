// AI-assisted code start
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tools > TD > UI > Build Main Menu
/// 메인 화면 씬(Scenes/MainMenu.unity): 실제 맵을 어둡게 깐 배경 + 길 위로 흐르는 화살표 + 제목 + 버튼(시작/설정/종료).
/// 빌드 설정의 맨 앞(0번)에 넣는다. 제목 글자는 TITLE_TEXT 한 곳만 바꾸면 된다.
/// </summary>
public static class MainMenuBuilder {
    // 1. 상수
    private const string SCENE_PATH = "Assets/TowerDefense/Scenes/" + MainMenu.SCENE_NAME + ".unity";
    private const string MAP_PREFAB_PATH = "Assets/TowerDefense/Prefabs/Map/Map01.prefab";
    private const string TOWER_SLOTS_NAME = "TowerSlots";
    private const string GOAL_MARKER_NAME = "Goal (지키는곳)";
    private const string TITLE_TEXT = "TOWER DEFENSE";
    private const string SUBTITLE_TEXT = "블랙홀에서 쏟아지는 적을 막아라";
    private const string VERSION_TEXT = "v0.1 prototype";
    private const string NOT_READY_TEXT = "설정 화면은 준비 중이에요";
    private const float PATH_ARROW_PIXELS_PER_UNIT = 60f;
    private const int CANVAS_SORTING_ORDER = 0;

    private const float TITLE_Y = 210f;
    private static readonly Vector2 TITLE_SIZE = new(1600f, 150f);
    private static readonly Vector2 SUBTITLE_SIZE = new(1200f, 50f);
    private static readonly Vector2 VERSION_OFFSET = new(-20f, 16f);   // 오른쪽 아래 모서리에서
    private static readonly Vector2 VERSION_SIZE = new(400f, 30f);
    private const float BUTTON_ICON_LEFT = 28f;
    private const float POPUP_MESSAGE_FONT_SIZE = 26f;
    private const float POPUP_MESSAGE_TOP = 36f;
    private const float POPUP_MESSAGE_PADDING = 60f;
    private const float POPUP_MESSAGE_HEIGHT = 60f;
    private const float POPUP_CLOSE_BOTTOM = 30f;
    private const float POPUP_CLOSE_WIDTH_RATIO = 0.6f;
    private const float POPUP_CLOSE_HEIGHT_RATIO = 0.85f;
    private const float SUBTITLE_Y = 118f;
    private const float FIRST_BUTTON_Y = -20f;
    private const float BUTTON_GAP = 94f;
    private const float BUTTON_WIDTH = 380f;
    private const float BUTTON_HEIGHT = 76f;
    private const float ICON_SIZE = 36f;
    private const float TITLE_FONT_SIZE = 112f;
    private const float SUBTITLE_FONT_SIZE = 30f;
    private const float BUTTON_FONT_SIZE = 30f;
    private const float SMALL_FONT_SIZE = 20f;
    private const float POPUP_WIDTH = 480f;
    private const float POPUP_HEIGHT = 220f;

    private static readonly Color DIM_COLOR = new(0.03f, 0.02f, 0.07f, 0.55f);
    private static readonly Color POPUP_DIM_COLOR = new(0.03f, 0.02f, 0.07f, 0.6f);
    private static readonly Color TITLE_COLOR = new(1f, 0.45f, 0.85f, 1f);
    private static readonly Color SUBTITLE_COLOR = new(0.55f, 0.92f, 1f, 1f);
    private static readonly Color TEXT_COLOR = Color.white;
    private static readonly Color VERSION_COLOR = new(0.7f, 0.68f, 0.8f, 1f);

    // 4. 메서드 (public)
    [MenuItem("Tools/TD/UI/Build Main Menu")]
    public static void Build() {
        Directory.CreateDirectory(Path.GetDirectoryName(SCENE_PATH));
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Map01Builder.CreateCamera();
        CreateBackground();

        TMP_FontAsset boldFont = UIFonts.Bold;
        TMP_FontAsset font = UIFonts.Regular;
        Sprite frame = UiArt.Load(UiArt.FRAME_SPRITE, UiArt.FRAME_BORDER);
        Sprite buttonSprite = UiArt.Load(UiArt.BUTTON_SPRITE, UiArt.BUTTON_BORDER);
        GameObject root = UiArt.CreateCanvas("MainMenu", CANVAS_SORTING_ORDER);

        Image dim = UiArt.CreateImage("Dim", root.transform, null);
        UiArt.Stretch(dim.rectTransform);
        dim.color = DIM_COLOR;

        TMP_Text title = UiArt.CreateText("Title", root.transform, boldFont, TITLE_FONT_SIZE, TITLE_COLOR, TextAlignmentOptions.Center);
        UiArt.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, TITLE_Y), TITLE_SIZE);
        title.text = TITLE_TEXT;
        TMP_Text subtitle = UiArt.CreateText("Subtitle", root.transform, font, SUBTITLE_FONT_SIZE, SUBTITLE_COLOR, TextAlignmentOptions.Center);
        UiArt.Place(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, SUBTITLE_Y), SUBTITLE_SIZE);
        subtitle.text = SUBTITLE_TEXT;

        Button start = CreateButton("Start", root.transform, boldFont, buttonSprite, "Play", "게임 시작", FIRST_BUTTON_Y);
        Button settings = CreateButton("Settings", root.transform, boldFont, buttonSprite, "Gear", "설정", FIRST_BUTTON_Y - BUTTON_GAP);
        Button quit = CreateButton("Quit", root.transform, boldFont, buttonSprite, "Close", "종료", FIRST_BUTTON_Y - BUTTON_GAP * 2f);

        TMP_Text version = UiArt.CreateText("Version", root.transform, font, SMALL_FONT_SIZE, VERSION_COLOR, TextAlignmentOptions.BottomRight);
        UiArt.Place(version.rectTransform, new Vector2(1f, 0f), VERSION_OFFSET, VERSION_SIZE);
        version.text = VERSION_TEXT;

        (GameObject notReady, Button notReadyClose) = CreateNotReadyPopup(root.transform, boldFont, frame, buttonSprite);

        MainMenu menu = root.AddComponent<MainMenu>();
        UiArt.SetField(menu, "title", title.rectTransform);
        UiArt.SetField(menu, "startButton", start);
        UiArt.SetField(menu, "settingsButton", settings);
        UiArt.SetField(menu, "quitButton", quit);
        UiArt.SetField(menu, "notReadyPanel", notReady);
        UiArt.SetField(menu, "notReadyCloseButton", notReadyClose);

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        AddToBuildSettingsFirst(SCENE_PATH);
        AssetDatabase.SaveAssets();
        Debug.Log($"[MainMenuBuilder] {SCENE_PATH} 생성 완료");
    }

    /// <summary>scenePath를 빌드 설정 0번(처음 뜨는 씬)으로. 나머지 씬은 그대로 뒤에 둔다.</summary>
    public static void AddToBuildSettingsFirst(string scenePath) {
        var scenes = new List<EditorBuildSettingsScene> { new(scenePath, true) };
        foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes) {
            if (existing.path != scenePath) {
                scenes.Add(existing);
            }
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // 4. 메서드 (private)
    /// <summary>실제 맵 프리팹을 풀어서 깔고, 게임용 표시(타워 자리, 지키는 곳)만 뺀다. 길 위에는 화살표가 계속 흐른다.</summary>
    private static void CreateBackground() {
        var mapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MAP_PREFAB_PATH);
        if (mapPrefab == null) {
            Debug.LogWarning("[MainMenuBuilder] Map01 프리팹이 없어 배경 없이 만듭니다 (Tools > TD > Map > Build Map01).");
            return;
        }
        var map = (GameObject)PrefabUtility.InstantiatePrefab(mapPrefab);
        PrefabUtility.UnpackPrefabInstance(map, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        map.name = "Background (Map01)";
        DestroyChild(map.transform, TOWER_SLOTS_NAME);
        foreach (Transform child in map.GetComponentsInChildren<Transform>(true)) {
            if (child.name == GOAL_MARKER_NAME) {
                Object.DestroyImmediate(child.gameObject);
                break;
            }
        }

        var flow = new GameObject("PathFlow").AddComponent<WavePathPreview>();
        var serialized = new SerializedObject(flow);
        serialized.FindProperty("arrowSprite").objectReferenceValue = UiArt.Load("PathArrow", 0, PATH_ARROW_PIXELS_PER_UNIT);
        serialized.FindProperty("showAllRoutes").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void DestroyChild(Transform parent, string childName) {
        Transform child = parent.Find(childName);
        if (child != null) {
            Object.DestroyImmediate(child.gameObject);
        }
    }

    private static Button CreateButton(string objectName, Transform parent, TMP_FontAsset font, Sprite sprite,
                                       string icon, string label, float y) {
        Image background = UiArt.CreateImage(objectName, parent, sprite, sliced: true, raycast: true);
        UiArt.Place(background.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(BUTTON_WIDTH, BUTTON_HEIGHT));
        Button button = background.gameObject.AddComponent<Button>();
        Image iconImage = UiArt.CreateImage("Icon", background.transform, UiArt.LoadIcon(icon));
        UiArt.Place(iconImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(BUTTON_ICON_LEFT, 0f), Vector2.one * ICON_SIZE);
        TMP_Text text = UiArt.CreateText("Text", background.transform, font, BUTTON_FONT_SIZE, TEXT_COLOR, TextAlignmentOptions.Center);
        UiArt.Stretch(text.rectTransform);
        text.text = label;
        return button;
    }

    private static (GameObject, Button) CreateNotReadyPopup(Transform parent, TMP_FontAsset font, Sprite frame, Sprite buttonSprite) {
        Image dim = UiArt.CreateImage("NotReady", parent, null, raycast: true);
        UiArt.Stretch(dim.rectTransform);
        dim.color = POPUP_DIM_COLOR;
        Image panel = UiArt.CreateImage("Panel", dim.transform, frame, sliced: true, raycast: true);
        UiArt.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(POPUP_WIDTH, POPUP_HEIGHT));
        TMP_Text message = UiArt.CreateText("Message", panel.transform, font, POPUP_MESSAGE_FONT_SIZE, TEXT_COLOR, TextAlignmentOptions.Center);
        UiArt.Place(message.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -POPUP_MESSAGE_TOP),
            new Vector2(POPUP_WIDTH - POPUP_MESSAGE_PADDING, POPUP_MESSAGE_HEIGHT));
        message.text = NOT_READY_TEXT;
        Button close = CreateButton("Close", panel.transform, font, buttonSprite, "Close", "닫기", 0f);
        RectTransform closeRect = (RectTransform)close.transform;
        UiArt.Place(closeRect, new Vector2(0.5f, 0f), new Vector2(0f, POPUP_CLOSE_BOTTOM),
            new Vector2(BUTTON_WIDTH * POPUP_CLOSE_WIDTH_RATIO, BUTTON_HEIGHT * POPUP_CLOSE_HEIGHT_RATIO));
        return (dim.gameObject, close);
    }
}
// AI-assisted code end
