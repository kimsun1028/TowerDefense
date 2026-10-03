// AI-assisted code start
using UnityEngine;

/// <summary>
/// 원거리 적이 쏘는 투사체 (화살 등). 대상에게 날아가 도착하면 피해를 준다.
/// 날아가는 동안 대상이 죽거나 사라지면 마지막 위치까지 날아가서 사라진다.
/// </summary>
public class EnemyProjectile : MonoBehaviour {
    // 1. 상수
    private const float DEFAULT_SPEED = 7f;
    private const float HIT_DISTANCE = 0.08f;
    private const float MAX_LIFETIME = 3f;

    // 2. 필드
    [SerializeField] private float speed = DEFAULT_SPEED;

    private IDamageable target;
    private Vector2 lastTargetPosition;
    private float damage;
    private DamageType damageType;
    private float lifetime;

    // 4. 메서드 (public)
    /// <summary>발사. 스폰 직후 호출.</summary>
    public void Launch(IDamageable launchTarget, float launchDamage, DamageType type) {
        target = launchTarget;
        lastTargetPosition = launchTarget.Position;
        damage = launchDamage;
        damageType = type;
    }

    // 4. 메서드 (private)
    private void Update() {
        lifetime += Time.deltaTime;
        if (IsTargetValid()) {
            lastTargetPosition = target.Position;
        }

        Vector2 current = transform.position;
        Vector2 next = Vector2.MoveTowards(current, lastTargetPosition, speed * Time.deltaTime);
        Vector2 direction = lastTargetPosition - current;
        if (direction.sqrMagnitude > 0f) {
            transform.right = direction;
        }
        transform.position = next;

        if ((next - lastTargetPosition).sqrMagnitude <= HIT_DISTANCE * HIT_DISTANCE) {
            if (IsTargetValid()) {
                target.TakeDamage(damage, damageType);
            }
            Destroy(gameObject);
        } else if (lifetime > MAX_LIFETIME) {
            Destroy(gameObject);
        }
    }

    private bool IsTargetValid() {
        bool destroyed = target is Object unityObject && unityObject == null;
        return target != null && !destroyed && target.IsAlive;
    }
}
// AI-assisted code end
