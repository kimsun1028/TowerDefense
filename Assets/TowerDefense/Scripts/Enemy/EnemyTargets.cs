// AI-assisted code start
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 원거리 적이 쏠 수 있는 대상(병사, 영웅 등) 목록.
/// 적 모듈은 병사 클래스를 모르므로, 병사 쪽에서 생길 때 Register, 사라질 때 Unregister 한다.
///
///   private void OnEnable() { EnemyTargets.Register(this); }
///   private void OnDisable() { EnemyTargets.Unregister(this); }
/// </summary>
public static class EnemyTargets {
    // 2. 필드
    private static readonly List<IDamageable> targets = new();

    public static IReadOnlyList<IDamageable> All => targets;

    // 4. 메서드 (public)
    public static void Register(IDamageable target) {
        if (target != null && !targets.Contains(target)) {
            targets.Add(target);
        }
    }

    public static void Unregister(IDamageable target) {
        targets.Remove(target);
    }

    /// <summary>center 기준 가로 반경 radius 타원(2.5D 사거리) 안의 살아있는 대상들 (가까운 순).</summary>
    public static List<IDamageable> FindInRadius(Vector2 center, float radius, List<IDamageable> results = null) {
        results ??= new List<IDamageable>();
        results.Clear();
        RemoveDestroyed();
        foreach (IDamageable target in targets) {
            if (target.IsAlive && EllipseRange.Contains(center, target.Position, radius)) {
                results.Add(target);
            }
        }
        results.Sort((a, b) => EllipseRange.GetNormalizedSqrDistance(center, a.Position, radius)
            .CompareTo(EllipseRange.GetNormalizedSqrDistance(center, b.Position, radius)));
        return results;
    }

    /// <summary>center 기준 가로 반경 range 타원(2.5D 사거리) 안의 살아있는 대상 중 가장 가까운 것. 없으면 null.</summary>
    public static IDamageable FindClosest(Vector2 center, float range) {
        IDamageable best = null;
        float bestDistance = 1f; // 정규화 거리 1 = 타원 경계
        RemoveDestroyed();
        foreach (IDamageable target in targets) {
            if (!target.IsAlive) {
                continue;
            }
            float distance = EllipseRange.GetNormalizedSqrDistance(center, target.Position, range);
            if (distance <= bestDistance) {
                bestDistance = distance;
                best = target;
            }
        }
        return best;
    }

    // 4. 메서드 (private)
    /// <summary>파괴된(Destroy된) 대상을 목록에서 정리한다.</summary>
    private static void RemoveDestroyed() {
        targets.RemoveAll(target => target == null || (target is Object unityObject && unityObject == null));
    }
}
// AI-assisted code end
