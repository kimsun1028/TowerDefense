// AI-assisted code start
using UnityEngine;

/// <summary>
/// 수치를 정보 바용 단계 글자로 바꾼다. 원작 표기 기준.
///   방어력·마법 저항: 없음 / 낮음(~30%) / 보통(~60%) / 높음(~90%) / 매우 높음
///   속도: 느림(원작 20 미만) / 보통(40 미만) / 빠름   ※ 우리 속도 = 원작 속도 / 28
/// </summary>
public static class StatRating {
    // 1. 상수
    private const float RESIST_LOW_MAX = 0.3f;
    private const float RESIST_MEDIUM_MAX = 0.6f;
    private const float RESIST_HIGH_MAX = 0.9f;
    private const float ORIGINAL_SPEED_RATIO = 28f;
    private const float SPEED_SLOW_MAX = 20f / ORIGINAL_SPEED_RATIO;
    private const float SPEED_NORMAL_MAX = 40f / ORIGINAL_SPEED_RATIO;
    private const float PERCENT = 100f;

    // 4. 메서드 (public)
    /// <summary>방어력·마법 저항 단계. 0이면 "없음", 아니면 "높음 (80%)"처럼 숫자를 같이 붙인다.</summary>
    public static string FormatResist(float ratio) {
        if (ratio <= 0f) {
            return "없음";
        }
        string grade = ratio <= RESIST_LOW_MAX ? "낮음"
            : ratio <= RESIST_MEDIUM_MAX ? "보통"
            : ratio <= RESIST_HIGH_MAX ? "높음"
            : "매우 높음";
        return $"{grade} ({Mathf.RoundToInt(ratio * PERCENT)}%)";
    }

    /// <summary>속도 단계.</summary>
    public static string FormatSpeed(float speed) {
        if (speed < SPEED_SLOW_MAX) {
            return "느림";
        }
        return speed < SPEED_NORMAL_MAX ? "보통" : "빠름";
    }

    /// <summary>"1~4" 처럼 범위 글자 (최솟값 = 최댓값이면 하나만).</summary>
    public static string FormatRange(int min, int max) {
        return min == max ? max.ToString() : $"{min}~{max}";
    }

    /// <summary>확률을 "30%"로.</summary>
    public static string FormatPercent(float ratio) {
        return $"{Mathf.RoundToInt(ratio * PERCENT)}%";
    }
}
// AI-assisted code end
