// AI-assisted code start
using System;

/// <summary>
/// 적에게 걸 수 있는 상태이상. 여러 개를 같이 고를 수 있다 (Flags).
/// 면역 판정: enemy.IsImmuneTo(StatusEffect.Poison) — 타워 쪽에서 효과를 걸기 전에 확인한다.
/// </summary>
[Flags]
public enum StatusEffect {
    None = 0,
    Poison = 1 << 0,  // 독 (예: 독화살)
    Slow = 1 << 1,    // 둔화
    Stun = 1 << 2,    // 기절
    Burn = 1 << 3     // 화상
}
// AI-assisted code end
