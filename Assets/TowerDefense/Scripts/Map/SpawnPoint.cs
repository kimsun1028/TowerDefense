
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 입구 (스폰 지점). 이 입구에서 출발하는 경로 목록과 "다음 웨이브" 아이콘 위치를 갖는다.
///
///   spawn.SpawnId              입구 ID (예: "Top", "Right")
///   spawn.RouteNames           이 입구에서 출발하는 경로 이름들 (갈림길이면 여러 개)
///   spawn.PickRandomRouteName() 그중 하나를 랜덤으로
///   spawn.WaveIconPosition     다음 웨이브 아이콘을 띄울 위치 (화면 안쪽)
///
/// 아이콘 위치는 자식 오브젝트(WaveIcon)라 씬에서 드래그로 옮길 수 있다.
/// </summary>
public class SpawnPoint : MonoBehaviour {
    // 1. 상수
    private const float GIZMO_RADIUS = 0.3f;
    private const float ICON_GIZMO_SIZE = 0.35f;
    private static readonly Color GIZMO_COLOR = new(0.9f, 0.2f, 0.2f);
    private static readonly Color ICON_GIZMO_COLOR = new(1f, 0.85f, 0.2f);

    // 2. 필드
    [Tooltip("입구 ID. 웨이브 데이터에서 이 이름으로 입구를 고른다.")]
    [SerializeField] private string spawnId;
    [Tooltip("이 입구에서 출발하는 경로 이름들 (EnemyPath 이름)")]
    [SerializeField] private List<string> routeNames = new();
    [Tooltip("다음 웨이브 아이콘 위치. 비어 있으면 입구 위치를 쓴다.")]
    [SerializeField] private Transform waveIcon;

    public string SpawnId { get => spawnId; set => spawnId = value; }
    public IReadOnlyList<string> RouteNames => routeNames;
    public Transform WaveIcon { get => waveIcon; set => waveIcon = value; }
    public Vector2 Position => transform.position;
    public Vector2 WaveIconPosition => waveIcon ? (Vector2)waveIcon.position : Position;

    // 4. 메서드 (public)
    /// <summary>경로 목록을 통째로 바꾼다 (맵 빌더/에디터용).</summary>
    public void SetRouteNames(List<string> names) {
        routeNames = names;
    }

    /// <summary>이 입구의 경로 중 하나를 랜덤으로. 경로가 없으면 null.</summary>
    public string PickRandomRouteName() {
        if (routeNames.Count == 0) {
            return null;
        }
        return routeNames[Random.Range(0, routeNames.Count)];
    }

    // 4. 메서드 (private)
    private void OnDrawGizmos() {
        Gizmos.color = GIZMO_COLOR;
        Gizmos.DrawWireSphere(transform.position, GIZMO_RADIUS);
        if (waveIcon) {
            Gizmos.color = ICON_GIZMO_COLOR;
            Gizmos.DrawLine(transform.position, waveIcon.position);
            Gizmos.DrawWireCube(waveIcon.position, Vector3.one * ICON_GIZMO_SIZE);
        }
    }
}
