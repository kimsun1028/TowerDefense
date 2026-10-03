// AI-assisted code start
using UnityEngine;

/// <summary>
/// 집결지(깃발)를 쓰는 타워가 구현한다. (병영, 영웅 타워 등)
/// 플레이어가 길 위를 찍으면 맵이 검증한 위치로 SetRallyPoint가 호출된다.
/// </summary>
public interface IRallyPointUser {
    /// <summary>집결지를 옮길 수 있는 최대 거리 (타워 위치 기준).</summary>
    float RallyRange { get; }

    /// <summary>현재 집결지 (월드 좌표).</summary>
    Vector2 RallyPoint { get; }

    /// <summary>맵이 검증한 새 집결지. 항상 길 위, RallyRange 이내.</summary>
    void SetRallyPoint(Vector2 worldPoint);
}
// AI-assisted code end
