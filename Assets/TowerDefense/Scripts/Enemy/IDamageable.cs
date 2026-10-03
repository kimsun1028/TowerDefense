// AI-assisted code start
/// <summary>피해를 받을 수 있는 대상 (병사, 영웅 등). 적이 자신을 붙잡은 상대를 공격할 때 쓴다.</summary>
public interface IDamageable {
    bool IsAlive { get; }

    void TakeDamage(float amount, DamageType type);
}
// AI-assisted code end
