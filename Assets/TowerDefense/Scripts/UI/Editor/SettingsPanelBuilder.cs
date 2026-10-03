// AI-assisted code start
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정 창(SettingsPanel)을 캔버스 아래에 만든다. 게임 HUD(GameHudBuilder)와 메인 화면(MainMenuBuilder)이 같이 쓴다.
///   어두운 판(뒤 클릭 막음) + 틀 + 제목 "설정" + 음량 막대 3줄(전체 · 배경음 · 효과음) + 음소거 + 닫기
/// </summary>
public static class SettingsPanelBuilder {
    // 1. 상수
    private const string TITLE = "설정";
    private const string MASTER_LABEL = "전체 음량";
    private const string MUSIC_LABEL = "배경음";
    private const string EFFECTS_LABEL = "효과음";
    private const string MUTE_LABEL = "음소거";
    private const string CLOSE_LABEL = "닫기";

    private const float PANEL_WIDTH = 640f;
    private const float PANEL_HEIGHT = 500f;
    private const float TITLE_TOP = 26f;
    private const float TITLE_HEIGHT = 44f;
    private const float FIRST_ROW_TOP = 100f;
    private const float ROW_GAP = 78f;
    private const float ROW_HEIGHT = 44f;
    private const float SIDE_PADDING = 40f;
    private const float LABEL_WIDTH = 150f;
    private const float VALUE_WIDTH = 80f;
    private const float VALUE_GAP = 24f;            // 막대 끝(손잡이) ↔ 값 글자
    private const float SLIDER_HEIGHT = 24f;
    private const float HANDLE_SIZE = 36f;
    private const float TOGGLE_SIZE = 36f;
    private const float CLOSE_WIDTH = 220f;
    private const float CLOSE_HEIGHT = 60f;
    private const float CLOSE_BOTTOM = 28f;
    private const float TITLE_FONT_SIZE = 30f;
    private const float ROW_FONT_SIZE = 24f;
    private const float SLIDER_INSET = 4f;
    private const int SLIDER_BORDER = UiArt.TAG_BORDER;

    private static readonly Color DIM_COLOR = new(0.04f, 0.02f, 0.08f, 0.7f);
    private static readonly Color TITLE_COLOR = new(1f, 0.6f, 0.9f, 1f);
    private static readonly Color TEXT_COLOR = Color.white;

    // 4. 메서드 (public)
    /// <summary>parent(캔버스) 아래에 설정 창을 만들고 SettingsPanel을 돌려준다. 처음에는 닫혀 있다.</summary>
    public static SettingsPanel Create(Transform parent, TMP_FontAsset font) {
        Image dim = UiArt.CreateImage("SettingsPanel", parent, null, raycast: true);
        UiArt.Stretch(dim.rectTransform);
        dim.color = DIM_COLOR;
        Image panel = UiArt.CreateImage("Panel", dim.transform, UiArt.Load(UiArt.FRAME_SPRITE, UiArt.FRAME_BORDER), sliced: true, raycast: true);
        UiArt.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(PANEL_WIDTH, PANEL_HEIGHT));

        TMP_Text title = UiArt.CreateText("Title", panel.transform, font, TITLE_FONT_SIZE, TITLE_COLOR, TextAlignmentOptions.Center);
        UiArt.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -TITLE_TOP), new Vector2(PANEL_WIDTH, TITLE_HEIGHT));
        title.text = TITLE;

        (Slider master, TMP_Text masterValue) = CreateSliderRow("Master", panel.transform, font, MASTER_LABEL, FIRST_ROW_TOP);
        (Slider music, TMP_Text musicValue) = CreateSliderRow("Music", panel.transform, font, MUSIC_LABEL, FIRST_ROW_TOP + ROW_GAP);
        (Slider effects, TMP_Text effectsValue) = CreateSliderRow("Effects", panel.transform, font, EFFECTS_LABEL, FIRST_ROW_TOP + ROW_GAP * 2f);
        (Button mute, Image muteCheck) = CreateToggleRow(panel.transform, font, FIRST_ROW_TOP + ROW_GAP * 3f);

        Image closeImage = UiArt.CreateImage("Close", panel.transform, UiArt.Load(UiArt.BUTTON_SPRITE, UiArt.BUTTON_BORDER), sliced: true, raycast: true);
        UiArt.Place(closeImage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, CLOSE_BOTTOM), new Vector2(CLOSE_WIDTH, CLOSE_HEIGHT));
        Button close = closeImage.gameObject.AddComponent<Button>();
        TMP_Text closeText = UiArt.CreateText("Text", closeImage.transform, font, ROW_FONT_SIZE, TEXT_COLOR, TextAlignmentOptions.Center);
        UiArt.Stretch(closeText.rectTransform);
        closeText.text = CLOSE_LABEL;

        SettingsPanel settings = dim.gameObject.AddComponent<SettingsPanel>();
        UiArt.SetField(settings, "panelRoot", dim.gameObject);
        UiArt.SetField(settings, "masterSlider", master);
        UiArt.SetField(settings, "musicSlider", music);
        UiArt.SetField(settings, "effectsSlider", effects);
        UiArt.SetField(settings, "masterValue", masterValue);
        UiArt.SetField(settings, "musicValue", musicValue);
        UiArt.SetField(settings, "effectsValue", effectsValue);
        UiArt.SetField(settings, "muteButton", mute);
        UiArt.SetField(settings, "muteCheck", muteCheck);
        UiArt.SetField(settings, "checkedSprite", UiArt.Load("ToggleOn"));
        UiArt.SetField(settings, "uncheckedSprite", UiArt.Load("Toggle"));
        UiArt.SetField(settings, "closeButton", close);
        dim.gameObject.SetActive(false);
        return settings;
    }

    // 4. 메서드 (private)
    /// <summary>한 줄: 이름 | 막대 | 값(%).</summary>
    private static (Slider, TMP_Text) CreateSliderRow(string objectName, Transform parent, TMP_FontAsset font, string label, float top) {
        RectTransform row = UiArt.CreateRect(objectName, parent);
        UiArt.Place(row, new Vector2(0.5f, 1f), new Vector2(0f, -top), new Vector2(PANEL_WIDTH - SIDE_PADDING * 2f, ROW_HEIGHT));
        TMP_Text name = UiArt.CreateText("Label", row, font, ROW_FONT_SIZE, TEXT_COLOR, TextAlignmentOptions.MidlineLeft);
        UiArt.Place(name.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(LABEL_WIDTH, ROW_HEIGHT));
        name.text = label;
        TMP_Text value = UiArt.CreateText("Value", row, font, ROW_FONT_SIZE, TEXT_COLOR, TextAlignmentOptions.MidlineRight);
        UiArt.Place(value.rectTransform, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(VALUE_WIDTH, ROW_HEIGHT));

        float sliderWidth = PANEL_WIDTH - SIDE_PADDING * 2f - LABEL_WIDTH - VALUE_WIDTH - VALUE_GAP;
        RectTransform sliderRect = UiArt.CreateRect("Slider", row);
        UiArt.Place(sliderRect, new Vector2(0f, 0.5f), new Vector2(LABEL_WIDTH, 0f), new Vector2(sliderWidth, SLIDER_HEIGHT));
        Image track = UiArt.CreateImage("Track", sliderRect, UiArt.Load("SliderTrack", SLIDER_BORDER), sliced: true, raycast: true);
        UiArt.Stretch(track.rectTransform);

        RectTransform fillArea = UiArt.CreateRect("FillArea", sliderRect);
        UiArt.Stretch(fillArea);
        fillArea.offsetMin = new Vector2(SLIDER_INSET, 0f);
        fillArea.offsetMax = new Vector2(-SLIDER_INSET, 0f);
        Image fill = UiArt.CreateImage("Fill", fillArea, UiArt.Load("SliderFill", SLIDER_BORDER), sliced: true);
        UiArt.Stretch(fill.rectTransform);

        RectTransform handleArea = UiArt.CreateRect("HandleArea", sliderRect);
        UiArt.Stretch(handleArea);
        handleArea.offsetMin = new Vector2(SLIDER_INSET, 0f); // 손잡이가 막대 양 끝 밖으로 덜 나가게
        handleArea.offsetMax = new Vector2(-SLIDER_INSET, 0f);
        Image handle = UiArt.CreateImage("Handle", handleArea, UiArt.Load("SliderHandle"), raycast: true);
        handle.rectTransform.sizeDelta = Vector2.one * HANDLE_SIZE;

        Slider slider = sliderRect.gameObject.AddComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        return (slider, value);
    }

    /// <summary>음소거 줄: 이름 | 켜기·끄기 칸.</summary>
    private static (Button, Image) CreateToggleRow(Transform parent, TMP_FontAsset font, float top) {
        RectTransform row = UiArt.CreateRect("Mute", parent);
        UiArt.Place(row, new Vector2(0.5f, 1f), new Vector2(0f, -top), new Vector2(PANEL_WIDTH - SIDE_PADDING * 2f, ROW_HEIGHT));
        TMP_Text name = UiArt.CreateText("Label", row, font, ROW_FONT_SIZE, TEXT_COLOR, TextAlignmentOptions.MidlineLeft);
        UiArt.Place(name.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(LABEL_WIDTH, ROW_HEIGHT));
        name.text = MUTE_LABEL;
        Image check = UiArt.CreateImage("Check", row, UiArt.Load("Toggle"), raycast: true);
        UiArt.Place(check.rectTransform, new Vector2(0f, 0.5f), new Vector2(LABEL_WIDTH, 0f), Vector2.one * TOGGLE_SIZE);
        Button button = check.gameObject.AddComponent<Button>();
        return (button, check);
    }
}
// AI-assisted code end
