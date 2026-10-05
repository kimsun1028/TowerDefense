// AI-assisted code start
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타워의 정보 바 표시 (IInfoSource 구현, 킹덤러시처럼 클릭하면 아래 바에 능력치).
/// 공통 줄(레벨)은 여기서, 타워 종류별 줄은 AddInfoRows를 덮어써서 채운다 (AttackTower, BarracksTower).
/// 클릭: 타워 자리(TowerSlot)는 InfoSelector가, 자리 밖으로 솟은 몸통은 TowerBuildMenuAdapter가 찾아 준다.
/// </summary>
public abstract partial class Tower : IInfoSource {
    // 1. 상수
    // 공격 간격(초) 단계: 원작 표기(매우 빠름 ~ 매우 느림)
    private const float INTERVAL_VERY_FAST_MAX = 0.5f;
    private const float INTERVAL_FAST_MAX = 0.8f;
    private const float INTERVAL_NORMAL_MAX = 1f;
    private const float INTERVAL_SLOW_MAX = 2f;
    // 사거리(유닛) 단계
    private const float RANGE_SHORT_MAX = 2f;
    private const float RANGE_NORMAL_MAX = 2.5f;
    private const float RANGE_LONG_MAX = 3f;

    // 4. 메서드 (public)
    public bool IsInfoValid => this != null;
    public string InfoName => string.IsNullOrEmpty(Data.displayName) ? Data.towerName : Data.displayName;
    public Sprite InfoPortrait {
        get {
            Sprite[] list = Data.portraits;
            if (list != null && level - 1 < list.Length && list[level - 1] != null) {
                return list[level - 1];
            }
            SpriteRenderer body = GetComponentInChildren<SpriteRenderer>();
            return body != null ? body.sprite : null;
        }
    }
    public bool HasInfoHealth => false;
    public float InfoHp => 0f;
    public float InfoMaxHp => 0f;
    public string InfoDescription => Data.description ?? string.Empty;
    public Vector2 InfoFootPosition => transform.position;

    public void FillInfoRows(List<InfoRow> rows) {
        rows.Clear();
        rows.Add(new InfoRow("레벨", level < MaxLevel ? $"Lv{level}" : $"Lv{level} (최대)"));
        AddInfoRows(rows);
    }

    // 4. 메서드 (protected)
    /// <summary>타워 종류별 정보 줄을 덧붙인다.</summary>
    protected virtual void AddInfoRows(List<InfoRow> rows) {
    }

    /// <summary>"0.8초 (빠름)"처럼 공격 간격과 단계.</summary>
    protected static string FormatInterval(float seconds) {
        string grade = seconds <= INTERVAL_VERY_FAST_MAX ? "매우 빠름"
            : seconds <= INTERVAL_FAST_MAX ? "빠름"
            : seconds <= INTERVAL_NORMAL_MAX ? "보통"
            : seconds <= INTERVAL_SLOW_MAX ? "느림"
            : "매우 느림";
        return $"{seconds:0.##}초 ({grade})";
    }

    /// <summary>"2.5 (보통)"처럼 사거리와 단계.</summary>
    protected static string FormatRange(float range) {
        string grade = range < RANGE_SHORT_MAX ? "짧음"
            : range < RANGE_NORMAL_MAX ? "보통"
            : range < RANGE_LONG_MAX ? "김"
            : "매우 김";
        return $"{range:0.##} ({grade})";
    }

    /// <summary>피해 종류 이름. 물리 = 방어력에 감소, 마법 = 마법 저항에 감소, 폭발 = 방어력 절반만 적용.</summary>
    protected static string FormatDamageType(DamageType type) {
        return type switch {
            DamageType.Magic => "마법",
            DamageType.Explosive => "폭발",
            _ => "물리",
        };
    }
}
// AI-assisted code end
