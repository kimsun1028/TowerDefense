// AI-assisted code start
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 경로(MapRouteData.Route)를 따라 출구까지 걷는 적.
///
/// 이동: 경로 거리(Progress) + 차선(Lane, -1~1). 무리로 퍼져 걸어도 길 폭 안으로 제한된다.
///       붙잡혔다 풀려나면 원래 차선으로 걸어서 다시 합류한다.
/// 진행도: Progress(지나온 거리), RemainingDistance(출구까지 남은 거리)
///       → 경로 길이가 달라도 RemainingDistance가 작을수록 "더 앞선 적"
/// 게임 연동: 골드/목숨은 직접 건드리지 않고 이벤트(Killed, ReachedGoal)로 알린다.
/// 충돌체: 타워 사거리 트리거(OnTriggerEnter2D)로 감지할 수 있게 트리거 Collider2D + Kinematic Rigidbody2D를 단다.
/// 원거리 공격(EnemyData.HasRangedAttack): 사거리 안에 병사(EnemyTargets)가 있으면 멈춰서 쏜다.
///       붙잡히면 근접전으로 바뀌고, 병사가 없어지면 다시 걷는다.
/// 보스: 범위 근접 공격(MeleeTargetCount), 변신(NextForm: 죽을 피해 → 다음 형태로 체력 가득),
///       최후의 저항(KillAllOnDeath: 죽으면 전장의 다른 적도 모두 죽음).
/// </summary>
public partial class Enemy : MonoBehaviour {
    // 1. 상수
    private const float REJOIN_THRESHOLD = 0.06f; // 차선 위치에서 이보다 벗어나 있으면 먼저 걸어서 합류
    private const float FLIP_THRESHOLD = 1e-4f;
    private const float FIRST_MELEE_DELAY_RATIO = 0.5f;
    private const float HALF = 0.5f;
    private const float DEFAULT_MAX_HP = 50f;
    private const float DEFAULT_SPEED = 1.5f;
    private const float DEFAULT_RADIUS = 0.18f;
    private const int DEFAULT_MELEE_DAMAGE_MIN = 1;
    private const int DEFAULT_MELEE_DAMAGE_MAX = 4;
    private const float DEFAULT_MELEE_INTERVAL = 1f;
    private const float FIRST_RANGED_DELAY_RATIO = 0.5f; // 대상을 처음 발견하고 첫 발까지 = 간격의 절반
    private const float EXPLOSIVE_ARMOR_RATIO = 0.5f;    // 폭발 피해는 방어력의 절반만 적용

    // 2. 필드
    private static readonly List<Enemy> active = new();

    /// <summary>처치됨 (골드 지급은 구독하는 쪽에서 enemy.Data.GoldReward)</summary>
    public static event Action<Enemy> Killed;
    /// <summary>출구 도착 (목숨 감소는 구독하는 쪽에서 enemy.Data.LifeDamage)</summary>
    public static event Action<Enemy> ReachedGoal;
    /// <summary>근접 공격을 회피함 ("Miss" 표시 등에 사용)</summary>
    public static event Action<Enemy> Evaded;
    /// <summary>다음 형태로 변신함 (예: 베즈난 → 악마 형태)</summary>
    public static event Action<Enemy> FormChanged;

    [SerializeField] private EnemyData data;

    [Header("Ranged")]
    [Tooltip("원거리 공격 투사체. 비어 있으면 즉시 피해")]
    [SerializeField] private EnemyProjectile rangedProjectile;

    [Header("Visual")]
    [SerializeField] private Transform healthBarFill;
    [Tooltip("이동 방향에 따라 좌우 반전할 스프라이트")]
    [SerializeField] private SpriteRenderer body;

    private Component blocker;
    private float meleeCooldown;
    private IDamageable rangedTarget;
    private float rangedCooldown;
    private static readonly List<IDamageable> areaTargets = new();

    /// <summary>살아있는 적 목록.</summary>
    public static IReadOnlyList<Enemy> Active => active;

    public EnemyData Data => data;
    public float Hp { get; private set; }
    public bool IsDead { get; private set; }
    public MapRouteData.Route Route { get; private set; }
    public float Lane { get; private set; }
    /// <summary>경로 시작점부터 지나온 거리.</summary>
    public float Progress { get; private set; }
    /// <summary>출구까지 남은 거리. 작을수록 위험한 적 (타워 타겟팅용).</summary>
    public float RemainingDistance => Route != null ? Route.GetRemainingDistance(Progress) : float.MaxValue;
    /// <summary>이 상태이상에 면역인가. 타워 쪽에서 독·둔화 등을 걸기 전에 확인한다.</summary>
    public bool IsImmuneTo(StatusEffect effect) => data && (data.Immunities & effect) != 0;
    /// <summary>공중 유닛인가 (병영이 붙잡을 수 없음).</summary>
    public bool IsFlying => data && data.IsFlying;
    /// <summary>병영 병사 등에게 붙잡혀 멈춘 상태.</summary>
    public bool IsBlocked => blocker != null;
    /// <summary>지금 이 적을 붙잡고 있는 대상 (없으면 null).</summary>
    public Component Blocker => blocker;
    /// <summary>보스 등급인가.</summary>
    public bool IsBoss => data && data.IsBoss;
    /// <summary>멈춰서 원거리 공격 중인가.</summary>
    public bool IsShooting => rangedTarget != null;

    private float MaxHp => data ? data.MaxHp : DEFAULT_MAX_HP;
    private float Speed => data ? data.Speed : DEFAULT_SPEED;
    private float Radius => data ? data.Radius : DEFAULT_RADIUS;
    private float MeleeInterval => data ? data.MeleeInterval : DEFAULT_MELEE_INTERVAL;

    // 4. 메서드 (public)
    /// <summary>center 기준 가로 반경 radius 타원(2.5D 사거리) 안의 살아있는 적. 결과 리스트를 넘기면 재사용한다 (GC 방지).</summary>
    public static List<Enemy> FindInRadius(Vector2 center, float radius, List<Enemy> results = null) {
        results ??= new List<Enemy>();
        results.Clear();
        foreach (Enemy enemy in active) {
            if (!enemy.IsDead && EllipseRange.Contains(center, enemy.transform.position, radius)) {
                results.Add(enemy);
            }
        }
        return results;
    }

    /// <summary>
    /// 후보 중 "가장 앞선 적" = 출구까지 남은 거리(RemainingDistance)가 가장 짧은 살아있는 적. 없으면 null.
    /// 추월당하거나 경로 길이가 달라도 정확하다. (타워 타겟팅용)
    /// </summary>
    public static Enemy FindMostAdvanced(IEnumerable<Enemy> candidates) {
        Enemy best = null;
        foreach (Enemy enemy in candidates) {
            if (enemy == null || enemy.IsDead) {
                continue;
            }
            if (best == null || enemy.RemainingDistance < best.RemainingDistance) {
                best = enemy;
            }
        }
        return best;
    }

    /// <summary>경로에 올려놓고 출발 준비를 한다. 스폰 직후 호출.</summary>
    /// <param name="route">따라갈 경로</param>
    /// <param name="lane">-1(오른쪽 끝) ~ 0(중앙) ~ 1(왼쪽 끝), 진행 방향 기준</param>
    /// <param name="startDistance">경로 시작점에서 이만큼 앞에서 출발 (무리 스폰 시 앞뒤 간격)</param>
    public void Initialize(MapRouteData.Route route, float lane = 0f, float startDistance = 0f) {
        Route = route;
        Lane = Mathf.Clamp(lane, -1f, 1f);
        Progress = Mathf.Max(0f, startDistance);
        Hp = MaxHp;
        transform.position = Route.GetPosition(Progress, Lane, Radius);
        UpdateHealthBar();
    }

    /// <summary>
    /// 피해를 받는다. 피해 종류에 따라 방어력/마법 저항이 적용된다.
    /// 근접 공격(AttackKind.Melee)은 회피율(EnemyData.MeleeEvasion)만큼 피할 수 있다. 원거리는 항상 맞는다.
    /// </summary>
    /// <param name="type">생략하면 물리 피해</param>
    /// <param name="kind">생략하면 원거리 (병사·영웅의 근접 공격은 AttackKind.Melee를 넘길 것)</param>
    /// <returns>맞았으면 true, 이미 죽었거나 회피했으면 false</returns>
    public bool TakeDamage(float amount, DamageType type = DamageType.Physical, AttackKind kind = AttackKind.Ranged) {
        if (IsDead) {
            return false;
        }
        if (kind == AttackKind.Melee && TryEvade()) {
            Evaded?.Invoke(this);
            return false;
        }
        Hp -= amount * (1f - GetDamageReduction(type));
        UpdateHealthBar();
        if (Hp <= 0f && !TryChangeForm()) {
            Die();
        }
        return true;
    }

    /// <summary>병사가 이 적을 붙잡는다. 이미 다른 병사에게 잡혀 있거나 공중 유닛이면 false (1:1 규칙).</summary>
    public bool TryEngage(Component engager) {
        if (IsDead || IsFlying) {
            return false;
        }
        if (blocker != null && blocker != engager) {
            return false;
        }
        if (blocker != engager) {
            meleeCooldown = MeleeInterval * FIRST_MELEE_DELAY_RATIO;
        }
        blocker = engager;
        return true;
    }

    /// <summary>붙잡은 대상이 놓아준다. 붙잡은 대상이 아니면 무시.</summary>
    public void Release(Component engager) {
        if (blocker == engager) {
            blocker = null;
        }
    }

    // 4. 메서드 (private)
    private void OnEnable() {
        active.Add(this);
    }

    private void OnDisable() {
        active.Remove(this);
    }

    private void Update() {
        if (IsDead || Route == null) {
            return;
        }
        float deltaTime = Time.deltaTime;
        if (IsBlocked) {
            rangedTarget = null;
            FightBlocker(deltaTime);
            return;
        }
        if (TickRangedAttack(deltaTime)) {
            return; // 멈춰서 쏘는 중
        }
        MoveAlongRoute(deltaTime);
    }

    /// <summary>사거리 안에 병사가 있으면 멈춰서 쏜다. 쏘는 중이면 true (이동하지 않음).</summary>
    private bool TickRangedAttack(float deltaTime) {
        if (data == null || !data.HasRangedAttack) {
            return false;
        }
        IDamageable target = EnemyTargets.FindClosest(transform.position, data.RangedRange);
        if (target == null) {
            rangedTarget = null;
            return false;
        }
        if (target != rangedTarget) {
            rangedTarget = target;
            rangedCooldown = data.RangedInterval * FIRST_RANGED_DELAY_RATIO;
        }
        if (body && Mathf.Abs(target.Position.x - transform.position.x) > FLIP_THRESHOLD) {
            body.flipX = target.Position.x < transform.position.x;
        }
        rangedCooldown -= deltaTime;
        if (rangedCooldown <= 0f) {
            FireAt(target);
            rangedCooldown = data.RangedInterval;
        }
        return true;
    }

    private void FireAt(IDamageable target) {
        int damage = UnityEngine.Random.Range(data.RangedDamageMin, data.RangedDamageMax + 1); // 최댓값 포함
        if (rangedProjectile == null) {
            target.TakeDamage(damage, DamageType.Physical);
            return;
        }
        Vector3 origin = body ? body.transform.position : transform.position;
        EnemyProjectile projectile = Instantiate(rangedProjectile, origin, Quaternion.identity);
        projectile.Launch(target, damage, DamageType.Physical);
    }

    private void MoveAlongRoute(float deltaTime) {
        Vector3 lanePosition = Route.GetPosition(Progress, Lane, Radius);
        Vector3 current = transform.position;
        Vector3 next;
        if ((current - lanePosition).sqrMagnitude > REJOIN_THRESHOLD * REJOIN_THRESHOLD) {
            // 교전 등으로 밀려난 상태: 진행은 멈추고 차선 위치로 걸어서 합류
            next = Vector3.MoveTowards(current, lanePosition, Speed * deltaTime);
        } else {
            Progress += Speed * deltaTime;
            if (Route.IsFinished(Progress)) {
                Finish();
                return;
            }
            next = Route.GetPosition(Progress, Lane, Radius);
        }

        if (body && Mathf.Abs(next.x - current.x) > FLIP_THRESHOLD) {
            body.flipX = next.x < current.x;
        }
        transform.position = next;
    }

    /// <summary>붙잡은 상대가 IDamageable이면 반격한다.</summary>
    private void FightBlocker(float deltaTime) {
        if (blocker is not IDamageable target || !target.IsAlive) {
            return;
        }
        meleeCooldown -= deltaTime;
        if (meleeCooldown > 0f) {
            return;
        }
        PerformMeleeAttack(target);
        meleeCooldown = MeleeInterval;
    }

    /// <summary>
    /// 근접 공격. 붙잡은 대상은 항상 맞고, MeleeTargetCount가 2 이상이면
    /// 반경 안의 다른 대상(가까운 순)까지 최대 그 수만큼 맞는다. 피해는 대상마다 따로 뽑는다.
    /// </summary>
    private void PerformMeleeAttack(IDamageable blockerTarget) {
        blockerTarget.TakeDamage(RollMeleeDamage(), DamageType.Physical);
        int maxTargets = data ? data.MeleeTargetCount : 1;
        if (maxTargets <= 1) {
            return;
        }
        int hit = 1;
        foreach (IDamageable other in EnemyTargets.FindInRadius(transform.position, data.MeleeAreaRadius, areaTargets)) {
            if (hit >= maxTargets) {
                break;
            }
            if (other == blockerTarget) {
                continue;
            }
            other.TakeDamage(RollMeleeDamage(), DamageType.Physical);
            hit++;
        }
    }

    /// <summary>다음 형태가 있으면 변신하고 체력을 가득 채운다. 변신했으면 true.</summary>
    private bool TryChangeForm() {
        if (data == null || data.NextForm == null) {
            return false;
        }
        data = data.NextForm;
        Hp = MaxHp;
        meleeCooldown = MeleeInterval * FIRST_MELEE_DELAY_RATIO;
        if (body && data.FormTint.a > 0f) {
            body.color = data.FormTint;
        }
        UpdateHealthBar();
        FormChanged?.Invoke(this);
        return true;
    }

    /// <summary>피해 종류별 감소율. 물리 = 방어력, 마법 = 마법 저항, 폭발 = 방어력의 절반.</summary>
    private float GetDamageReduction(DamageType type) {
        if (data == null) {
            return 0f;
        }
        switch (type) {
            case DamageType.Magic:
                return data.MagicResist;
            case DamageType.Explosive:
                return data.Armor * EXPLOSIVE_ARMOR_RATIO;
            default:
                return data.Armor;
        }
    }

    /// <summary>근접 공격 회피 판정. 회피율 0이면 항상 false.</summary>
    private bool TryEvade() {
        float evasion = data ? data.MeleeEvasion : 0f;
        // Random.value는 0~1 → value < 0.3 이면 30% (System.Random과 헷갈리지 않게 UnityEngine. 명시)
        return UnityEngine.Random.value < evasion;
    }

    /// <summary>반격 피해를 최솟값~최댓값(둘 다 포함) 사이에서 뽑는다.</summary>
    private int RollMeleeDamage() {
        int min = data ? data.MeleeDamageMin : DEFAULT_MELEE_DAMAGE_MIN;
        int max = data ? data.MeleeDamageMax : DEFAULT_MELEE_DAMAGE_MAX;
        // 정수 Random.Range는 최댓값을 포함하지 않으므로 +1 (System.Random과 헷갈리지 않게 UnityEngine. 명시)
        return UnityEngine.Random.Range(min, max + 1);
    }

    private void UpdateHealthBar() {
        if (healthBarFill == null) {
            return;
        }
        // 가운데 pivot 기준: 왼쪽 끝을 고정한 채 줄어들도록 위치도 같이 옮긴다
        float ratio = Mathf.Clamp01(Hp / MaxHp);
        healthBarFill.localScale = new Vector3(ratio, 1f, 1f);
        healthBarFill.localPosition = new Vector3((ratio - 1f) * HALF, 0f, 0f);
    }

    private void Die() {
        IsDead = true;
        Killed?.Invoke(this);
        Destroy(gameObject);
        if (data && data.KillAllOnDeath) {
            KillAllOthers();
        }
    }

    /// <summary>최후의 저항: 전장의 다른 적을 모두 죽인다 (처치로 처리 → Killed 이벤트, 골드 지급).</summary>
    private void KillAllOthers() {
        foreach (Enemy other in new List<Enemy>(active)) {
            if (other != this && !other.IsDead) {
                other.Die();
            }
        }
    }

    private void Finish() {
        IsDead = true;
        ReachedGoal?.Invoke(this);
        Destroy(gameObject);
    }
}
// AI-assisted code end
