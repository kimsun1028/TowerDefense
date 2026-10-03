// AI-assisted code start
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이 중 병영 집결지를 옮기는 도구 (씬에 하나).
/// 게임 쪽(타워 메뉴 등)에서 Begin(타워, 위치) 호출 → 범위 원 표시 → 길 위 클릭 → IRallyPointUser.SetRallyPoint
/// 마우스 위치가 범위 밖/길 밖이면 실제로 옮겨질 지점(가장 가까운 유효 지점)을 미리 보여준다.
/// 우클릭 또는 ESC로 취소.
/// </summary>
public class RallyPointTool : MonoBehaviour {
    // 1. 상수
    private static readonly Color DEFAULT_VALID_COLOR = new(0.3f, 1f, 0.3f, 0.9f);     // 길 위 그대로
    private static readonly Color DEFAULT_CLAMPED_COLOR = new(1f, 0.75f, 0.2f, 0.9f);  // 가까운 길로 옮겨짐
    private const int RANGE_CIRCLE_SEGMENTS = 64;
    private const int MARKER_SEGMENTS = 16;
    private const float RANGE_LINE_WIDTH = 0.05f;
    private const float MARKER_LINE_WIDTH = 0.06f;
    private const float MARKER_RADIUS = 0.18f;
    private const int LINE_SORTING_ORDER = 50;
    private const float RANGE_ALPHA = 0.5f;
    private const float EXACT_SQR_TOLERANCE = 1e-6f;
    private const string RANGE_LINE_NAME = "RallyRange";
    private const string MARKER_LINE_NAME = "RallyMarker";

    // 2. 필드
    [SerializeField] private Material lineMaterial;
    [SerializeField] private Color validColor = DEFAULT_VALID_COLOR;
    [SerializeField] private Color clampedColor = DEFAULT_CLAMPED_COLOR;

    private IRallyPointUser user;
    private Vector2 origin;
    private LineRenderer rangeCircle;
    private LineRenderer marker;
    private Camera cam;

    public static RallyPointTool Instance { get; private set; }
    public bool IsActive => user != null;
    public Material LineMaterial { get => lineMaterial; set => lineMaterial = value; }

    // 4. 메서드 (public)
    /// <summary>집결지 설정을 시작한다. 범위 원이 표시된다.</summary>
    public void Begin(IRallyPointUser rallyUser, Vector2 towerPosition) {
        user = rallyUser;
        origin = towerPosition;
        DrawCircle(rangeCircle, origin, rallyUser.RallyRange, TDMap.RALLY_VERTICAL_RATIO); // 2.5D 타원
        SetVisible(true);
    }

    public void Cancel() {
        user = null;
        SetVisible(false);
    }

    /// <summary>활성 상태일 때 게임 쪽 입력 처리에서 좌클릭 위치를 넘겨준다.</summary>
    public void HandleClick(Vector2 worldPosition) {
        if (TDMap.Instance.TryGetRallyPoint(origin, worldPosition, user.RallyRange, out Vector2 rallyPoint)) {
            user.SetRallyPoint(rallyPoint);
        }
        Cancel();
    }

    // 4. 메서드 (private)
    private void Awake() {
        Instance = this;
        cam = Camera.main;
        rangeCircle = CreateLine(RANGE_LINE_NAME, RANGE_CIRCLE_SEGMENTS, RANGE_LINE_WIDTH);
        marker = CreateLine(MARKER_LINE_NAME, MARKER_SEGMENTS, MARKER_LINE_WIDTH);
        SetVisible(false);
    }

    private void Update() {
        if (!IsActive) {
            return;
        }
        if ((user as Object) == null) {
            Cancel(); // 타워가 팔렸음
            return;
        }

        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;
        bool cancelPressed = (mouse != null && mouse.rightButton.wasPressedThisFrame)
            || (keyboard != null && keyboard.escapeKey.wasPressedThisFrame);
        if (cancelPressed) {
            Cancel();
            return;
        }
        if (mouse == null) {
            return;
        }

        Vector2 worldPosition = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        bool found = TDMap.Instance.TryGetRallyPoint(origin, worldPosition, user.RallyRange, out Vector2 rallyPoint);
        bool exact = found && (rallyPoint - worldPosition).sqrMagnitude < EXACT_SQR_TOLERANCE;
        marker.enabled = found;
        DrawCircle(marker, rallyPoint, MARKER_RADIUS);
        Color color = exact ? validColor : clampedColor;
        marker.startColor = color;
        marker.endColor = color;
        Color rangeColor = new(color.r, color.g, color.b, RANGE_ALPHA);
        rangeCircle.startColor = rangeColor;
        rangeCircle.endColor = rangeColor;
    }

    private LineRenderer CreateLine(string lineName, int pointCount, float width) {
        var lineObject = new GameObject(lineName);
        lineObject.transform.SetParent(transform, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.positionCount = pointCount;
        line.loop = true;
        line.widthMultiplier = width;
        line.useWorldSpace = true;
        line.sortingOrder = LINE_SORTING_ORDER;
        if (lineMaterial) {
            line.sharedMaterial = lineMaterial;
        }
        return line;
    }

    /// <summary>verticalRatio &lt; 1이면 세로가 눌린 타원.</summary>
    private static void DrawCircle(LineRenderer line, Vector2 center, float radius, float verticalRatio = 1f) {
        int count = line.positionCount;
        for (int i = 0; i < count; i++) {
            float angle = i * Mathf.PI * 2f / count;
            line.SetPosition(i, new Vector3(center.x + Mathf.Cos(angle) * radius, center.y + Mathf.Sin(angle) * radius * verticalRatio, 0f));
        }
    }

    private void SetVisible(bool visible) {
        rangeCircle.enabled = visible;
        marker.enabled = visible;
    }
}
// AI-assisted code end
