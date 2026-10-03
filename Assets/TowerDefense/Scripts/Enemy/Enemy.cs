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
/// </summary>
public class Enemy : MonoBehaviour {
    // 1. 상수
    private const float REJOIN_THRESHOLD = 0.06f; // 차선 위치에서 이보다 벗어나 있으면 먼저 걸어서 합류
    private const float FLIP_THRESHOLD = 1e-4f;
    private const float SAME_POSITION_EPSILON = 1e-4f;
    private const float FIRST_MELEE_DELAY_RATIO = 0.5f;
    private const float HALF = 0.5f;
    private const float DEFAULT_MAX_HP = 50f;
    private const float DEFAULT_SPEED = 1.5f;
    private const float DEFAULT_RADIUS = 0.18f;
    private const float DEFAULT_MELEE_DAMAGE = 4f;
    private const float DEFAULT_MELEE_INTERVAL = 1f;

    // 2. 필드
    private static readonly List<Enemy> active = new();

    /// <summary>처치됨 (골드 지급은 구독하는 쪽에서 enemy.Data.GoldReward)</summary>
    public static event Action<Enemy> Killed;
    /// <summary>출구 도착 (목숨 감소는 구독하는 쪽에서 enemy.Data.LifeDamage)</summary>
    public static event Action<Enemy> ReachedGoal;

    [SerializeField] private EnemyData data;

    [Header("Separation (교전 중 겹침 방지)")]
    [Tooltip("붙잡힌 적끼리 이 타원(가로 x, 세로 y) 안에 들어오면 서로 밀어낸다. 전투 장면 하나의 크기.")]
    [SerializeField] private Vector2 fightSpacing = new(0.8f, 0.5f);
    [SerializeField] private float separationSpeed = 2f;

    [Header("Visual")]
    [SerializeField] private Transform healthBarFill;
    [Tooltip("이동 방향에 따라 좌우 반전할 스프라이트")]
    [SerializeField] private SpriteRenderer body;

    private Component blocker;
    private float meleeCooldown;

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
    /// <summary>공중 유닛인가 (병영이 붙잡을 수 없음).</summary>
    public bool IsFlying => data && data.IsFlying;
    /// <summary>병영 병사 등에게 붙잡혀 멈춘 상태.</summary>
    public bool IsBlocked => blocker != null;
    /// <summary>지금 이 적을 붙잡고 있는 대상 (없으면 null).</summary>
    public Component Blocker => blocker;

    private float MaxHp => data ? data.MaxHp : DEFAULT_MAX_HP;
    private float Speed => data ? data.Speed : DEFAULT_SPEED;
    private float Radius => data ? data.Radius : DEFAULT_RADIUS;
    private float MeleeInterval => data ? data.MeleeInterval : DEFAULT_MELEE_INTERVAL;

    // 4. 메서드 (public)
    /// <summary>center 주변 radius 이내의 살아있는 적. 결과 리스트를 넘기면 재사용한다 (GC 방지).</summary>
    public static List<Enemy> FindInRadius(Vector2 center, float radius, List<Enemy> results = null) {
        results ??= new List<Enemy>();
        results.Clear();
        float sqrRadius = radius * radius;
        foreach (Enemy enemy in active) {
            if (!enemy.IsDead && ((Vector2)enemy.transform.position - center).sqrMagnitude <= sqrRadius) {
                results.Add(enemy);
            }
        }
        return results;
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

    /// <summary>피해를 받는다. 피해 종류에 따라 방어력/마법 저항이 적용된다.</summary>
    public void TakeDamage(float amount, DamageType type) {
        if (IsDead) {
            return;
        }
        float reduction = data == null ? 0f : type == DamageType.Physical ? data.Armor : data.MagicResist;
        Hp -= amount * (1f - reduction);
        UpdateHealthBar();
        if (Hp <= 0f) {
            Die();
        }
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
            SeparateFromOtherFights(deltaTime);
            FightBlocker(deltaTime);
            return;
        }
        MoveAlongRoute(deltaTime);
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

    /// <summary>
    /// 다른 붙잡힌 적과 타원 범위 안에서 겹치면 밀어낸다 → 전투 장면마다 따로 보인다.
    /// 길 밖으로 밀리면 길 방향 성분만 남겨 길을 따라 미끄러지게 한다.
    /// </summary>
    private void SeparateFromOtherFights(float deltaTime) {
        Vector2 self = transform.position;
        Vector2 push = Vector2.zero;
        foreach (Enemy other in active) {
            if (other == this || other.IsDead || !other.IsBlocked) {
                continue;
            }
            Vector2 delta = self - (Vector2)other.transform.position;
            Vector2 normalized = new(delta.x / fightSpacing.x, delta.y / fightSpacing.y); // 타원 → 원으로 정규화
            float distance = normalized.magnitude;
            if (distance >= 1f) {
                continue;
            }
            Vector2 direction = distance > SAME_POSITION_EPSILON
                ? new Vector2(normalized.x * fightSpacing.x, normalized.y * fightSpacing.y).normalized
                : GetTieBreakDirection(other);
            push += direction * (1f - distance);
        }
        if (push == Vector2.zero) {
            return;
        }

        Vector2 step = Vector2.ClampMagnitude(push, 1f) * separationSpeed * deltaTime;
        Vector2 next = self + step;
        TDMap map = TDMap.Instance;
        if (map != null && !map.IsOnRoad(next)) {
            Vector2 roadDirection = Route.GetDirection(Progress);
            next = self + roadDirection * Vector2.Dot(step, roadDirection);
            if (!map.IsOnRoad(next)) {
                return;
            }
        }
        transform.position = next;
    }

    /// <summary>완전히 같은 위치일 때 밀어낼 방향 (두 적이 서로 반대 방향이 되도록 ID로 결정).</summary>
    private Vector2 GetTieBreakDirection(Enemy other) {
        Vector2 direction = Route != null ? Route.GetDirection(Progress) : Vector2.right;
        return GetInstanceID() > other.GetInstanceID() ? direction : -direction;
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
        target.TakeDamage(data ? data.MeleeDamage : DEFAULT_MELEE_DAMAGE, DamageType.Physical);
        meleeCooldown = MeleeInterval;
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
    }

    private void Finish() {
        IsDead = true;
        ReachedGoal?.Invoke(this);
        Destroy(gameObject);
    }
}
// AI-assisted code end
