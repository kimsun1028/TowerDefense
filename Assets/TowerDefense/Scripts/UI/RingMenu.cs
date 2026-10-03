// AI-assisted code start
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 타워 자리 링 메뉴 (킹덤러시처럼 타워 둘레에 버튼). 타워를 만드는 쪽이 RingMenuOption 목록을 만들어 Open을 부른다.
///   빈 자리: 타워 종류 버튼들 / 지은 타워: 강화(위), 판매(아래), 집결지 등
/// 버튼을 누르면 메뉴를 닫고 그 버튼의 동작을 실행한다. 메뉴 밖(UI가 아닌 곳)을 누르면 닫힌다 (closeOnOutsideClick, 끌 수 있음).
/// 맵 위 위치를 따라다니는 화면 UI (Screen Space Overlay 캔버스 안). 화면 끝 자리에서는 잘리지 않게 안쪽으로 밀린다.
/// </summary>
public class RingMenu : MonoBehaviour {
    // 1. 상수
    private const float RADIUS = 104f;
    private const float OPEN_TIME = 0.12f;
    private const float OPEN_START_SCALE = 0.6f;
    private const int NO_HOVER = -1;
    // 화면 끝에서 메뉴가 잘리지 않게 중심을 안쪽으로 미는 여유 (버튼 반 + 가격표 + 툴팁)
    private const float EDGE_SIDE = RADIUS + 44f;
    private const float EDGE_TOP = RADIUS + 44f;
    private const float EDGE_BOTTOM = RADIUS + 72f;
    // 버튼 자리 → 각도 (도, 오른쪽 = 0, 반시계)
    private static readonly System.Collections.Generic.Dictionary<RingPosition, float> ANGLES = new() {
        [RingPosition.Right] = 0f, [RingPosition.TopRight] = 45f, [RingPosition.Top] = 90f, [RingPosition.TopLeft] = 135f,
        [RingPosition.Left] = 180f, [RingPosition.BottomLeft] = 225f, [RingPosition.Bottom] = 270f, [RingPosition.BottomRight] = 315f,
    };
    private static readonly RingPosition[] AUTO_ORDER = {
        RingPosition.TopLeft, RingPosition.TopRight, RingPosition.BottomLeft, RingPosition.BottomRight,
        RingPosition.Top, RingPosition.Bottom, RingPosition.Left, RingPosition.Right,
    };

    // 2. 필드
    [SerializeField] private RectTransform menuRoot;
    [SerializeField] private RingMenuButton[] buttons;
    [SerializeField] private GameObject tooltipRoot;
    [SerializeField] private TMP_Text tooltipText;
    [Tooltip("메뉴 밖(UI가 아닌 곳)을 누르면 스스로 닫기. 선택을 타워 쪽이 관리하면(SelectionCleared) 끈다")]
    [SerializeField] private bool closeOnOutsideClick = true;

    private readonly List<RingMenuOption> options = new();
    private readonly List<RingPosition> usedPositions = new();
    private RectTransform area;
    private Camera cam;
    private Vector2 worldPosition;
    private float openedAt;
    private int openedFrame;
    private int hoveredIndex = NO_HOVER;

    private static RingMenu instance;

    /// <summary>씬의 RingMenu. 플레이 중 스크립트를 다시 컴파일해 static이 비면 다시 찾는다.</summary>
    public static RingMenu Instance => instance ? instance : instance = FindAnyObjectByType<RingMenu>();
    public bool IsOpen => menuRoot.gameObject.activeSelf;
    public bool CloseOnOutsideClick { get => closeOnOutsideClick; set => closeOnOutsideClick = value; }

    // 4. 메서드 (public)
    /// <summary>worldPosition(타워 자리) 둘레에 버튼들을 띄운다. 버튼 수보다 많은 항목은 무시.</summary>
    public void Open(Vector2 position, IReadOnlyList<RingMenuOption> menuOptions) {
        EndHover();
        worldPosition = position;
        options.Clear();
        usedPositions.Clear();
        for (int i = 0; i < menuOptions.Count && i < buttons.Length; i++) {
            options.Add(menuOptions[i]);
            if (menuOptions[i].Position != RingPosition.Auto) {
                usedPositions.Add(menuOptions[i].Position);
            }
        }
        for (int i = 0; i < buttons.Length; i++) {
            bool active = i < options.Count;
            buttons[i].gameObject.SetActive(active);
            if (active) {
                buttons[i].Bind(this, i, options[i]);
                ((RectTransform)buttons[i].transform).anchoredPosition = GetOffset(ResolvePosition(options[i]));
            }
        }
        menuRoot.gameObject.SetActive(true);
        tooltipRoot.SetActive(false);
        openedAt = Time.unscaledTime;
        openedFrame = Time.frameCount;
        FollowTarget();
    }

    public void Close() {
        EndHover();
        menuRoot.gameObject.SetActive(false);
    }

    /// <summary>버튼에서 호출: 메뉴를 닫고 동작 실행 (동작 안에서 다시 Open해도 된다).</summary>
    public void Click(int index) {
        if (index >= options.Count || !options[index].IsAvailable) {
            return;
        }
        RingMenuOption option = options[index];
        Close();
        option.OnClick?.Invoke();
    }

    public void ShowTooltip(int index) {
        if (index >= options.Count) {
            return;
        }
        tooltipText.text = options[index].Label;
        tooltipRoot.SetActive(true);
        if (hoveredIndex != index) {
            EndHover();
            hoveredIndex = index;
            options[index].HoverChanged?.Invoke(true);
        }
    }

    public void HideTooltip() {
        tooltipRoot.SetActive(false);
        EndHover();
    }

    // 4. 메서드 (private)
    /// <summary>올려 둔 버튼이 있으면 "벗어남"을 알린다.</summary>
    private void EndHover() {
        if (hoveredIndex == NO_HOVER) {
            return;
        }
        int index = hoveredIndex;
        hoveredIndex = NO_HOVER;
        if (index < options.Count) {
            options[index].HoverChanged?.Invoke(false);
        }
    }

    private void Awake() {
        instance = this;
        area = (RectTransform)menuRoot.parent;
        Close();
    }

    private void Update() {
        if (!IsOpen) {
            return;
        }
        if (IsClickOutside()) {
            Close();
            return;
        }
        FollowTarget();
        float t = Mathf.Clamp01((Time.unscaledTime - openedAt) / OPEN_TIME);
        menuRoot.localScale = Vector3.one * Mathf.Lerp(OPEN_START_SCALE, 1f, 1f - (1f - t) * (1f - t));
        for (int i = 0; i < options.Count; i++) {
            buttons[i].Refresh(options[i]);
        }
    }

    private bool IsClickOutside() {
        Mouse mouse = Mouse.current;
        if (!closeOnOutsideClick || mouse == null || !mouse.leftButton.wasPressedThisFrame || Time.frameCount == openedFrame) {
            return false;
        }
        return EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject();
    }

    private void FollowTarget() {
        if (cam == null) {
            cam = Camera.main;
        }
        Vector2 screen = cam.WorldToScreenPoint(worldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, null, out Vector2 local);
        Rect bounds = area.rect;
        local.x = Mathf.Clamp(local.x, bounds.xMin + EDGE_SIDE, bounds.xMax - EDGE_SIDE);
        local.y = Mathf.Clamp(local.y, bounds.yMin + EDGE_BOTTOM, bounds.yMax - EDGE_TOP);
        menuRoot.anchoredPosition = local;
    }

    private RingPosition ResolvePosition(RingMenuOption option) {
        if (option.Position != RingPosition.Auto) {
            return option.Position;
        }
        foreach (RingPosition candidate in AUTO_ORDER) {
            if (!usedPositions.Contains(candidate)) {
                usedPositions.Add(candidate);
                return candidate;
            }
        }
        return RingPosition.Top;
    }

    private static Vector2 GetOffset(RingPosition position) {
        float angle = ANGLES.TryGetValue(position, out float degrees) ? degrees : ANGLES[RingPosition.BottomRight];
        float radians = angle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * RADIUS;
    }
}
// AI-assisted code end
