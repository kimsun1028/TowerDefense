// AI-assisted code start
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정 창 (소리): 전체 · 배경음 · 효과음 음량 막대(0~100%)와 음소거. 바꾸면 바로 적용·저장된다 (GameAudioSettings).
/// 메인 화면과 게임(일시 정지 메뉴) 양쪽에서 같은 창을 쓴다. 닫기 / Esc로 닫힌다.
/// </summary>
public class SettingsPanel : MonoBehaviour {
    // 1. 상수
    private const string PERCENT_FORMAT = "{0}%";
    private const float PERCENT = 100f;

    // 2. 필드
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider effectsSlider;
    [SerializeField] private TMP_Text masterValue;
    [SerializeField] private TMP_Text musicValue;
    [SerializeField] private TMP_Text effectsValue;
    [SerializeField] private Button muteButton;
    [SerializeField] private Image muteCheck;
    [SerializeField] private Sprite checkedSprite;
    [SerializeField] private Sprite uncheckedSprite;
    [SerializeField] private Button closeButton;

    public bool IsOpen => panelRoot.activeSelf;

    // 4. 메서드 (public)
    public void Open() {
        Refresh();
        panelRoot.SetActive(true);
    }

    public void Close() {
        panelRoot.SetActive(false);
    }

    // 4. 메서드 (private)
    private void Awake() {
        masterSlider.onValueChanged.AddListener(value => { GameAudioSettings.SetMaster(value); Refresh(); });
        musicSlider.onValueChanged.AddListener(value => { GameAudioSettings.SetMusic(value); Refresh(); });
        effectsSlider.onValueChanged.AddListener(value => { GameAudioSettings.SetEffects(value); Refresh(); });
        muteButton.onClick.AddListener(() => { GameAudioSettings.SetMuted(!GameAudioSettings.IsMuted); Refresh(); });
        closeButton.onClick.AddListener(Close); // 처음에 닫혀 있는 건 빌더가 정한다 (꺼진 채로 Awake가 늦게 불려도 안 닫히게)
    }

    private void Refresh() {
        masterSlider.SetValueWithoutNotify(GameAudioSettings.Master);
        musicSlider.SetValueWithoutNotify(GameAudioSettings.Music);
        effectsSlider.SetValueWithoutNotify(GameAudioSettings.Effects);
        masterValue.text = FormatPercent(GameAudioSettings.Master);
        musicValue.text = FormatPercent(GameAudioSettings.Music);
        effectsValue.text = FormatPercent(GameAudioSettings.Effects);
        muteCheck.sprite = GameAudioSettings.IsMuted ? checkedSprite : uncheckedSprite;
    }

    private static string FormatPercent(float value) {
        return string.Format(PERCENT_FORMAT, Mathf.RoundToInt(value * PERCENT));
    }
}
// AI-assisted code end
