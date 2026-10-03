// AI-assisted code start
using UnityEngine;

/// <summary>
/// 교차로/코너 노드. EnemyPath가 이 노드들을 순서대로 묶어 경로를 만든다.
/// </summary>
public class Waypoint : MonoBehaviour {
    // 1. 상수
    private const float GIZMO_RADIUS = 0.2f;

    // 4. 메서드
    private void OnDrawGizmos() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, GIZMO_RADIUS);
    }
}
// AI-assisted code end
