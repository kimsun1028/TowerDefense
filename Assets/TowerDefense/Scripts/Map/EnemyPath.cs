// AI-assisted code start
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스폰 → 골까지의 노드 리스트 (편집용). 예: 스폰A → 1 → 4 → 7 → 골
/// 첫 노드는 스폰 위치, 마지막 노드는 골 위치. 게임에서는 구운 MapRouteData를 쓴다.
/// </summary>
public class EnemyPath : MonoBehaviour {
    // 2. 필드
    [SerializeField] private List<Waypoint> nodes = new();

    /// <summary>노드 목록 (에디터 편집용으로 직접 수정 가능).</summary>
    public List<Waypoint> Nodes => nodes;
    public int Count => nodes.Count;
    public Vector3 this[int index] => nodes[index].transform.position;

    public float TotalLength {
        get {
            float length = 0f;
            for (int i = 1; i < nodes.Count; i++) {
                length += Vector3.Distance(this[i - 1], this[i]);
            }
            return length;
        }
    }

    // 4. 메서드
    private void OnDrawGizmos() {
        if (nodes == null) {
            return;
        }
        Gizmos.color = Color.cyan;
        for (int i = 1; i < nodes.Count; i++) {
            if (nodes[i - 1] && nodes[i]) {
                Gizmos.DrawLine(nodes[i - 1].transform.position, nodes[i].transform.position);
            }
        }
    }
}
// AI-assisted code end
