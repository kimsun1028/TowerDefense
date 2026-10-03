// AI-assisted code start
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>링 메뉴 버튼 하나의 그림: 아이콘, 자물쇠, 가격표, 단축키 표시. 마우스를 올리면 이름(툴팁).</summary>
public class RingMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    // 1. 상수
    private static readonly Color DISABLED_COLOR = new(0.45f, 0.45f, 0.55f, 1f);
    private static readonly Color COST_COLOR = new(1f, 0.86f, 0.35f, 1f);
    private static readonly Color COST_SHORT_COLOR = new(1f, 0.35f, 0.4f, 1f);
    private static readonly Color SELL_COLOR = new(0.5f, 1f, 0.6f, 1f);

    // 2. 필드
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private GameObject lockMark;
    [SerializeField] private GameObject costTag;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private GameObject shortcutTag;
    [SerializeField] private TMP_Text shortcutText;

    private RingMenu owner;
    private int index;

    // 4. 메서드 (public)
    public void Bind(RingMenu menu, int optionIndex, RingMenuOption option) {
        if (owner == null) {
            button.onClick.AddListener(() => owner.Click(index));
        }
        owner = menu;
        index = optionIndex;
        icon.sprite = option.Icon;
        icon.enabled = option.Icon != null && !option.IsLocked;
        lockMark.SetActive(option.IsLocked);
        costTag.SetActive(option.Cost >= 0 && !option.IsLocked);
        costText.text = option.IsSell ? $"+{option.Cost}" : option.Cost.ToString();
        bool hasShortcut = !string.IsNullOrEmpty(option.Shortcut);
        shortcutTag.SetActive(hasShortcut);
        if (hasShortcut) {
            shortcutText.text = option.Shortcut;
        }
        Refresh(option);
    }

    /// <summary>매 프레임: 지금 누를 수 있는지 (골드 등)에 따라 색을 바꾼다.</summary>
    public void Refresh(RingMenuOption option) {
        bool available = option.IsAvailable;
        button.interactable = available;
        icon.color = available ? Color.white : DISABLED_COLOR;
        costText.color = option.IsSell ? SELL_COLOR : available ? COST_COLOR : COST_SHORT_COLOR;
    }

    public void OnPointerEnter(PointerEventData eventData) {
        owner?.ShowTooltip(index);
    }

    public void OnPointerExit(PointerEventData eventData) {
        owner?.HideTooltip();
    }
}
// AI-assisted code end
