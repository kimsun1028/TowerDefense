// AI-assisted code start
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tools > TD > UI > Build Info Panel
/// 화면 아래 작은 정보 바 프리팹(Prefabs/UI/InfoPanel.prefab): 둥근 초상화 + 이름 탭 + "아이콘 값" 한 줄.
/// 씬에 넣기만 하면 클릭 선택까지 동작한다 (EventSystem은 씬에 따로 하나 필요).
/// 그림은 Tools/gen_ui.py (Art/UI), 글자는 TextMeshPro + 나눔고딕 (UIFonts).
/// </summary>
public static class InfoPanelBuilder {
    // 1. 상수
    private const string PREFAB_DIR = "Assets/TowerDefense/Prefabs/UI";
    private const string PREFAB_PATH = PREFAB_DIR + "/InfoPanel.prefab";
    private const int CANVAS_SORTING_ORDER = 10;
    private const int CELL_COUNT = 8;

    private const float BAR_HEIGHT = 64f;
    private const float BAR_BOTTOM = 8f;   // 맨 아래 14번 타워 자리와 겹치지 않게 낮게
    private const int BAR_PADDING_LEFT = 116;
    private const int BAR_PADDING_RIGHT = 56;
    private const float CELL_SPACING = 26f;
    private const float ICON_SIZE = 36f;
    private const float ICON_TEXT_GAP = 8f;
    private const float PORTRAIT_FRAME_SIZE = 102f;
    private const float PORTRAIT_SIZE = 84f;
    private const float PORTRAIT_LEFT = 6f;
    private const float NAME_TAB_LEFT = 104f;
    private const float NAME_TAB_HEIGHT = 34f;
    private const int NAME_TAB_PADDING = 12;
    private const float CLOSE_SIZE = 36f;
    private const float CLOSE_ICON_SIZE = 24f;
    private const float CLOSE_RIGHT = 12f;

    private const float NAME_FONT_SIZE = 19f;
    private const float VALUE_FONT_SIZE = 22f;
    private static readonly Color TEXT_COLOR = new(0.95f, 0.95f, 1f, 1f);
    private static readonly Color NAME_COLOR = Color.white; // 어두운 탭 위 흰 글씨 (아래 바와 같은 느낌)

    // 정보 줄 이름 → 아이콘 (적: EnemyInfo, 타워: Tower.FillInfoRows)
    private static readonly (string label, string icon)[] STAT_ICONS = {
        ("근접 공격", "Sword"), ("원거리 공격", "Arrow"), ("방어력", "Shield"), ("마법 저항", "Crystal"),
        ("속도", "Speed"), ("라이프", "HeartBreak"), ("특성", "Star"),
        ("레벨", "Up"), ("공격력", "Sword"), ("공격 간격", "Clock"), ("사거리", "Target"),
        ("피해 종류", "Bolt"), ("비행 적", "Wing"),
    };

    // 4. 메서드 (public)
    [MenuItem("Tools/TD/UI/Build Info Panel")]
    public static void BuildFromMenu() {
        Build();
    }

    /// <summary>정보 바 프리팹을 만들고 돌려준다.</summary>
    public static GameObject Build() {
        Directory.CreateDirectory(PREFAB_DIR);
        TMP_FontAsset font = UIFonts.Regular;
        TMP_FontAsset boldFont = UIFonts.Bold;
        GameObject root = UiArt.CreateCanvas("InfoPanel", CANVAS_SORTING_ORDER);

        // 아래 가운데 바: 칸 수에 맞춰 너비가 바뀐다
        Image panel = UiArt.CreateImage("Panel", root.transform, UiArt.Load("Frame", 7), sliced: true, raycast: true);
        UiArt.Place(panel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, BAR_BOTTOM), new Vector2(0f, BAR_HEIGHT));
        HorizontalLayoutGroup layout = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(BAR_PADDING_LEFT, BAR_PADDING_RIGHT, 0, 0);
        layout.spacing = CELL_SPACING;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        panel.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 능력치 칸 (아이콘 + 값)
        var icons = new Image[CELL_COUNT];
        var values = new TMP_Text[CELL_COUNT];
        for (int i = 0; i < CELL_COUNT; i++) {
            RectTransform cell = UiArt.CreateRect($"Stat{i}", panel.transform);
            cell.sizeDelta = new Vector2(0f, ICON_SIZE);
            HorizontalLayoutGroup cellLayout = cell.gameObject.AddComponent<HorizontalLayoutGroup>();
            cellLayout.spacing = ICON_TEXT_GAP;
            cellLayout.childAlignment = TextAnchor.MiddleLeft;
            cellLayout.childControlWidth = true;
            cellLayout.childControlHeight = true;
            cellLayout.childForceExpandWidth = false;
            cellLayout.childForceExpandHeight = false;
            icons[i] = UiArt.CreateImage("Icon", cell, null);
            LayoutElement iconLayout = icons[i].gameObject.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = ICON_SIZE;
            iconLayout.preferredHeight = ICON_SIZE;
            values[i] = UiArt.CreateText("Value", cell, boldFont, VALUE_FONT_SIZE, TEXT_COLOR, TextAlignmentOptions.MidlineLeft);
        }

        // 둥근 초상화 (바 왼쪽, 위아래로 살짝 튀어나옴)
        RectTransform portraitRoot = UiArt.CreateRect("Portrait", panel.transform);
        portraitRoot.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        UiArt.Place(portraitRoot, new Vector2(0f, 0.5f), new Vector2(PORTRAIT_LEFT, 0f), Vector2.one * PORTRAIT_FRAME_SIZE);
        Image mask = UiArt.CreateImage("Mask", portraitRoot, MapAuthoring.Circle);
        UiArt.Place(mask.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * PORTRAIT_SIZE);
        mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        Image portrait = UiArt.CreateImage("Image", mask.transform, null);
        UiArt.Stretch(portrait.rectTransform);
        portrait.preserveAspect = true;
        Image portraitFrame = UiArt.CreateImage("Frame", portraitRoot, UiArt.Load("PortraitFrame"));
        UiArt.Stretch(portraitFrame.rectTransform);

        // 이름 탭 (바 위)
        Image nameTab = UiArt.CreateImage("NameTab", panel.transform, UiArt.Load("CostTag", 3), sliced: true); // 얇은 틀
        nameTab.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        UiArt.Place(nameTab.rectTransform, Vector2.zero, Vector2.zero, new Vector2(0f, NAME_TAB_HEIGHT));
        nameTab.rectTransform.anchorMin = nameTab.rectTransform.anchorMax = new Vector2(0f, 1f);
        nameTab.rectTransform.pivot = new Vector2(0f, 0f);
        nameTab.rectTransform.anchoredPosition = new Vector2(NAME_TAB_LEFT, -4f);
        HorizontalLayoutGroup tabLayout = nameTab.gameObject.AddComponent<HorizontalLayoutGroup>();
        tabLayout.padding = new RectOffset(NAME_TAB_PADDING, NAME_TAB_PADDING, 0, 0);
        tabLayout.childAlignment = TextAnchor.MiddleLeft;
        tabLayout.childControlWidth = true;
        tabLayout.childControlHeight = true;
        nameTab.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        TMP_Text nameText = UiArt.CreateText("Name", nameTab.transform, boldFont, NAME_FONT_SIZE, NAME_COLOR, TextAlignmentOptions.MidlineLeft);

        // 닫기 버튼
        Image closeImage = UiArt.CreateImage("Close", panel.transform, UiArt.Load("Button", 5), sliced: true, raycast: true);
        closeImage.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        UiArt.Place(closeImage.rectTransform, new Vector2(1f, 0.5f), new Vector2(-CLOSE_RIGHT, 0f), Vector2.one * CLOSE_SIZE);
        Button closeButton = closeImage.gameObject.AddComponent<Button>();
        Image closeIcon = UiArt.CreateImage("Icon", closeImage.transform, UiArt.Icon("Close"));
        UiArt.Place(closeIcon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * CLOSE_ICON_SIZE);

        // 동작 컴포넌트
        InfoPanel infoPanel = root.AddComponent<InfoPanel>();
        UiArt.SetField(infoPanel, "panelRoot", panel.gameObject);
        UiArt.SetField(infoPanel, "portrait", portrait);
        UiArt.SetField(infoPanel, "nameText", nameText);
        UiArt.SetArray(infoPanel, "cellIcons", icons);
        UiArt.SetArray(infoPanel, "cellValues", values);
        UiArt.SetField(infoPanel, "closeButton", closeButton);
        UiArt.SetField(infoPanel, "healthIcon", UiArt.Icon("Heart"));
        UiArt.SetField(infoPanel, "fallbackIcon", UiArt.Icon("Star"));
        var serialized = new SerializedObject(infoPanel);
        SerializedProperty statIcons = serialized.FindProperty("statIcons");
        statIcons.arraySize = STAT_ICONS.Length;
        for (int i = 0; i < STAT_ICONS.Length; i++) {
            SerializedProperty entry = statIcons.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("label").stringValue = STAT_ICONS[i].label;
            entry.FindPropertyRelative("icon").objectReferenceValue = UiArt.Icon(STAT_ICONS[i].icon);
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();

        InfoSelector selector = root.AddComponent<InfoSelector>();
        UiArt.SetField(selector, "ringMaterial", MapAuthoring.RoadMaterial);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        return prefab;
    }
}
// AI-assisted code end
