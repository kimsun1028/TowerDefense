// AI-assisted code start
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 클릭으로 적·타워를 골라 정보 바(InfoPanel)에 보여주고, 선택한 대상 발밑에 타원 표시를 그린다.
///   적: 적의 충돌체를 클릭 (적과 타워가 겹치면 적 우선)
///   병사 등: InfoClickArea가 붙은 충돌체 (적 다음, 타워보다 우선)
///   타워: 타워가 지어진 타워 자리(TowerSlot)를 클릭 → slot.Occupant가 IInfoSource면 표시
///   그 밖: FallbackFinder (타워 몸통 클릭 등, 타워 연동 쪽이 등록)
///   빈 곳 클릭: 정보 바 닫기
/// UI 위 클릭, 집결지 설정 중(RallyPointTool) 클릭은 무시한다.
/// </summary>
public class InfoSelector : MonoBehaviour {
    // 1. 상수
    private static readonly Color DEFAULT_RING_COLOR = new(1f, 0.9f, 0.3f, 0.9f);
    private const int RING_SEGMENTS = 40;
    private const float RING_WIDTH = 0.035f;
    private const float RING_RADIUS = 0.32f;
    private const int RING_SORTING_ORDER = -3;
    private const float RING_Z = 0f;

    // 2. 필드
    [SerializeField] private Material ringMaterial;
    [SerializeField] private Color ringColor = DEFAULT_RING_COLOR;

    private Camera cam;
    private LineRenderer ring;

    /// <summary>충돌체로 못 찾는 대상(자리 밖으로 솟은 타워 몸통 등)을 찾는 함수. UI가 모르는 쪽(타워 연동)이 등록한다.</summary>
    public static System.Func<Vector2, IInfoSource> FallbackFinder { get; set; }

    // 4. 메서드 (private)
    private void Awake() {
        cam = Camera.main;
        ring = CreateRing();
    }

    private void Update() {
        HandleClick();
        UpdateRing();
    }

    private void HandleClick() {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame || InfoPanel.Instance == null) {
            return;
        }
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) {
            return; // 정보 바 등 UI 위
        }
        if (RallyPointTool.Instance != null && RallyPointTool.Instance.IsActive) {
            return; // 집결지 설정 중
        }
        if (cam == null) {
            cam = Camera.main;
        }
        Vector2 worldPosition = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        InfoPanel.Instance.Show(FindSource(worldPosition));
    }

    /// <summary>클릭 위치의 정보 대상. 적 우선, 다음은 InfoClickArea 충돌체(병사 등), 다음은 타워가 지어진 자리, 마지막은 FallbackFinder.</summary>
    private IInfoSource FindSource(Vector2 worldPosition) {
        // 트리거 충돌체(적·병사)도 포함 (Physics2D.queriesHitTriggers 기본값 true)
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPosition);
        IInfoSource otherSource = null;
        IInfoSource towerSource = null;
        foreach (Collider2D hit in hits) {
            Enemy enemy = hit.GetComponentInParent<Enemy>();
            if (enemy != null && !enemy.IsDead) {
                return enemy;
            }
            // 클릭 영역으로 표시된 충돌체만 (타워 사거리 트리거 등은 무시)
            if (otherSource == null && hit.TryGetComponent(out InfoClickArea area) && area.Source != null && area.Source.IsInfoValid) {
                otherSource = area.Source;
            }
            TowerSlot slot = hit.GetComponent<TowerSlot>();
            if (slot != null && slot.Occupant is IInfoSource tower) {
                towerSource = tower;
            }
        }
        return otherSource ?? towerSource ?? FallbackFinder?.Invoke(worldPosition);
    }

    private void UpdateRing() {
        IInfoSource current = InfoPanel.Instance != null ? InfoPanel.Instance.Current : null;
        ring.enabled = current != null && current.IsInfoValid;
        if (!ring.enabled) {
            return;
        }
        Vector2 center = current.InfoFootPosition;
        float verticalRadius = RING_RADIUS * EllipseRange.VERTICAL_RATIO;
        for (int i = 0; i < RING_SEGMENTS; i++) {
            float angle = i * Mathf.PI * 2f / RING_SEGMENTS;
            ring.SetPosition(i, new Vector3(center.x + Mathf.Cos(angle) * RING_RADIUS,
                center.y + Mathf.Sin(angle) * verticalRadius, RING_Z));
        }
    }

    private LineRenderer CreateRing() {
        var ringObject = new GameObject("SelectionRing");
        ringObject.transform.SetParent(transform, false);
        LineRenderer line = ringObject.AddComponent<LineRenderer>();
        line.positionCount = RING_SEGMENTS;
        line.loop = true;
        line.useWorldSpace = true;
        line.widthMultiplier = RING_WIDTH;
        line.sortingOrder = RING_SORTING_ORDER;
        line.startColor = ringColor;
        line.endColor = ringColor;
        if (ringMaterial) {
            line.sharedMaterial = ringMaterial;
        }
        line.enabled = false;
        return line;
    }
}
// AI-assisted code end
