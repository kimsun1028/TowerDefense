// AI-assisted code start
using UnityEngine;

/// <summary>
/// 적에게 공격받을 수 있는 대상 (병사, 영웅 등).
/// 적이 자신을 붙잡은 상대에게 반격하거나, 원거리 적이 사거리 안의 대상을 쏠 때 쓴다.
/// 원거리 공격 대상이 되려면 EnemyTargets.Register(this)로 등록해야 한다.
/// </summary>
public interface IDamageable {
    bool IsAlive { get; }
    /// <summary>월드 위치 (원거리 적이 사거리를 잴 때 사용).</summary>
    Vector2 Position { get; }

    void TakeDamage(float amount, DamageType type);
}
// AI-assisted code end
