// AI-assisted code start
using System;
using UnityEngine;

/// <summary>링 메뉴 버튼 자리 (타워 둘레). Auto는 왼쪽 위 → 오른쪽 위 → 왼쪽 아래 → 오른쪽 아래 → 위 → 아래 순.</summary>
public enum RingPosition {
    Auto,
    Top,
    TopLeft,
    TopRight,
    Left,
    Right,
    Bottom,
    BottomLeft,
    BottomRight,
}

/// <summary>
/// 링 메뉴 버튼 하나. 타워를 만드는 쪽이 채워서 RingMenu.Open에 넘긴다 (UI는 타워 코드를 모른다).
///   cost  : 0 이상이면 아래에 가격표. sell = true면 "+가격" (판매)
///   isAvailable : 매 프레임 확인 (골드가 모자라면 false → 흐리게, 가격 빨강)
///   locked : 자물쇠 표시, 누를 수 없음 (최대 강화 등)
///   shortcut : 버튼 왼쪽 위에 표시할 단축키 글자 (예: "1"). 비우면 표시 안 함
///   hoverChanged : 마우스가 버튼에 올라오면 true, 벗어나거나 메뉴가 닫히면 false (예: 지었을 때 사거리 미리보기)
/// </summary>
public class RingMenuOption {
    // 1. 상수
    public const int NO_COST = -1;

    // 2. 필드
    private readonly Sprite icon;
    private readonly string label;
    private readonly int cost;
    private readonly bool sell;
    private readonly bool locked;
    private readonly RingPosition position;
    private readonly Func<bool> isAvailable;
    private readonly Action onClick;
    private readonly string shortcutText;
    private readonly Action<bool> hoverChanged;

    public Sprite Icon => icon;
    public string Label => label;
    public int Cost => cost;
    public bool IsSell => sell;
    public bool IsLocked => locked;
    public RingPosition Position => position;
    public Action OnClick => onClick;
    public string Shortcut => shortcutText;
    public Action<bool> HoverChanged => hoverChanged;
    public bool IsAvailable => !locked && (isAvailable == null || isAvailable());

    // 3. 생성자
    public RingMenuOption(Sprite optionIcon, string optionLabel, Action click, int optionCost = NO_COST,
                          Func<bool> available = null, RingPosition optionPosition = RingPosition.Auto,
                          bool isLocked = false, bool isSell = false, string shortcut = null,
                          Action<bool> hovered = null) {
        icon = optionIcon;
        label = optionLabel;
        onClick = click;
        cost = optionCost;
        isAvailable = available;
        position = optionPosition;
        locked = isLocked;
        sell = isSell;
        shortcutText = shortcut;
        hoverChanged = hovered;
    }
}
// AI-assisted code end
