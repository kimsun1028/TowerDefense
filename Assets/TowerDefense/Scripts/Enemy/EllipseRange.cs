// AI-assisted code start
using UnityEngine;

/// <summary>
/// 2.5D 화면(카메라 45°)용 사거리 판정. 세로 반경 = 가로 반경 × sin(45°) 인 타원으로 잰다.
/// 타워 쪽 Util.IsInEllipse와 같은 공식이라, 적의 사거리·범위 공격 모양이 타워와 일치한다.
/// </summary>
public static class EllipseRange {
    // 1. 상수
    /// <summary>땅 기준 카메라 각도 (도). 타워 쪽 Util과 같은 값.</summary>
    public const float CAMERA_ANGLE = 45f;

    // 2. 필드
    /// <summary>세로 반경 / 가로 반경.</summary>
    public static readonly float VERTICAL_RATIO = Mathf.Sin(Mathf.Deg2Rad * CAMERA_ANGLE);

    // 4. 메서드 (public)
    /// <summary>target이 center 기준 가로 반경 range의 타원 안에 있는가.</summary>
    public static bool Contains(Vector2 center, Vector2 target, float range) {
        return GetNormalizedSqrDistance(center, target, range) <= 1f;
    }

    /// <summary>타원 기준 정규화 거리의 제곱 (1 이하면 안쪽). 가까운 순 정렬에도 쓴다.</summary>
    public static float GetNormalizedSqrDistance(Vector2 center, Vector2 target, float range) {
        if (range <= 0f) {
            return float.MaxValue;
        }
        Vector2 diff = target - center;
        float x = diff.x / range;
        float y = diff.y / (range * VERTICAL_RATIO);
        return x * x + y * y;
    }
}
// AI-assisted code end
