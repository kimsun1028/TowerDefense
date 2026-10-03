// AI-assisted code start
using UnityEngine;

/// <summary>
/// 타워를 지을 수 있는 자리 (흙 패드). 맵은 "자리"와 "기본 집결지"만 제공하고,
/// 클릭 처리 · 건설 · 골드 같은 게임 로직은 타워/게임 쪽에서 한다.
///
///   slot.IsOccupied / slot.Occupant     지금 지어진 타워 (게임 쪽이 SetOccupant로 등록)
///   slot.RallyPointWorld / RallyRange   병영 기본 집결지와 이동 가능 거리
/// 클릭 판정용 Collider2D가 붙어 있다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TowerSlot : MonoBehaviour {
    // 1. 상수
    private const string LABEL_NAME = "Label";
    private const float MIN_RALLY_RANGE = 0.5f;
    private const float RALLY_GIZMO_RADIUS = 0.15f;
    private static readonly Color RALLY_COLOR = new(1f, 0.6f, 0f);
    private static readonly Color RANGE_COLOR = new(1f, 0.6f, 0f, 0.25f);

    // 2. 필드
    [SerializeField] private int slotNumber;
    [Tooltip("이 자리에 지은 병영의 기본 집결지 (슬롯 기준 오프셋). Map Editor의 '집결지 자동 계산'으로 채운다.")]
    [SerializeField] private Vector2 rallyOffset;
    [Tooltip("집결지를 옮길 수 있는 최대 거리")]
    [Min(MIN_RALLY_RANGE)] [SerializeField] private float rallyRange = 2.2f;

    public int SlotNumber { get => slotNumber; set => slotNumber = value; }
    public Vector2 RallyOffset { get => rallyOffset; set => rallyOffset = value; }
    public float RallyRange { get => rallyRange; set => rallyRange = Mathf.Max(MIN_RALLY_RANGE, value); }

    /// <summary>이 자리에 지어진 타워 (없으면 null).</summary>
    public Component Occupant { get; private set; }
    public bool IsOccupied => Occupant != null;
    public Vector2 RallyPointWorld => (Vector2)transform.position + rallyOffset;

    // 4. 메서드
    /// <summary>타워를 지은 뒤 게임 쪽에서 호출. 번호 표지판이 숨겨진다.</summary>
    public void SetOccupant(Component tower) {
        Occupant = tower;
        SetLabelVisible(tower == null);
    }

    /// <summary>타워를 팔거나 부순 뒤 호출.</summary>
    public void ClearOccupant() {
        SetOccupant(null);
    }

    private void SetLabelVisible(bool visible) {
        Transform label = transform.Find(LABEL_NAME);
        if (label) {
            label.gameObject.SetActive(visible);
        }
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = RALLY_COLOR;
        Gizmos.DrawLine(transform.position, RallyPointWorld);
        Gizmos.DrawWireSphere(RallyPointWorld, RALLY_GIZMO_RADIUS);
        Gizmos.color = RANGE_COLOR;
        Gizmos.DrawWireSphere(transform.position, rallyRange);
    }
}
// AI-assisted code end
