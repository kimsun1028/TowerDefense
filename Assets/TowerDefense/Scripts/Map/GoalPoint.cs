// AI-assisted code start
using UnityEngine;

/// <summary>
/// 지키는 곳 (모든 경로의 출구). 맵은 위치와 표시만 맡는다.
/// 적이 도착했는지는 적 쪽 Enemy.ReachedGoal 이벤트가 알려준다.
/// </summary>
public class GoalPoint : MonoBehaviour {
    // 1. 상수
    private const float DEFAULT_RADIUS = 0.6f;
    private static readonly Color GIZMO_COLOR = new(0.25f, 0.45f, 0.9f);

    // 2. 필드
    [Tooltip("지키는 곳 표시 반경 (UI/이펙트용)")]
    [SerializeField] private float radius = DEFAULT_RADIUS;

    public float Radius { get => radius; set => radius = value; }
    public Vector2 Position => transform.position;

    // 4. 메서드 (private)
    private void OnDrawGizmos() {
        Gizmos.color = GIZMO_COLOR;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
// AI-assisted code end
