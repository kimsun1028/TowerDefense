using System.Collections.Generic;
using UnityEngine;

public partial class Soldier : MonoBehaviour , IDamageable
{
    [SerializeField] private Transform healthBarFill;   // HealthBarBG/Fill (적과 같은 구조)

    private BarracksTower owner;
    private SoldierData data;
    private Vector2 homePos;    // 집결지 기준 내자리
    private float engageRange;  // 집결지 기준 적 탐지 반경 (타워가 넘겨줌)
    private float hp;
    private float attackCoolDown;
    private Enemy target;

    private Animator animator;         // Visual 자식 (파라미터: Moving, Dir, Attack, Hurt, Death)
    private SpriteRenderer body;
    private bool movedThisFrame;

    // IDamageable
    public bool IsAlive => hp > 0f;
    public Vector2 Position => transform.position;

    // 병영이 생성 직후 호출
    public void Init(BarracksTower owner, Vector2 homePos, SoldierData data, float engageRange)
    {
        this.owner = owner;
        this.homePos = homePos;
        this.data = data;
        this.engageRange = engageRange;

        this.hp = data.maxHp;

        animator = GetComponentInChildren<Animator>();
        body = GetComponentInChildren<SpriteRenderer>();
        ApplyAnimator();
    }

    private void ApplyAnimator()
    {
        if (animator != null && data.animator != null) animator.runtimeAnimatorController = data.animator;
        UpdateHealthBar();
    }

    // 체력 비율만큼 Fill을 줄임 (왼쪽 끝 고정), 높이는 병사/영웅 그림에 맞춤
    private void UpdateHealthBar()
    {
        if (healthBarFill == null) return;
        healthBarFill.parent.localPosition = new Vector3(0f, data.healthBarHeight, 0f);
        float ratio = Mathf.Clamp01(hp / data.maxHp);
        healthBarFill.localScale = new Vector3(ratio, 1f, 1f);
        healthBarFill.localPosition = new Vector3((ratio - 1f) * 0.5f, 0f, 0f);
    }

    private bool HasParam(string paramName)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.name == paramName) return true;
        }
        return false;
    }

    private void PlayTrigger(string paramName)
    {
        if (HasParam(paramName)) animator.SetTrigger(paramName);
    }

    // 공격(또는 Lv3 Block) 애니메이션 재생 중인지
    private bool IsAttacking()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        return state.IsName("Attack") || state.IsName("Block");
    }

    // 이동/공격 방향으로 바라보기: 0 오른쪽(왼쪽이면 flipX), 1 위, 2 아래
    private void Face(Vector2 delta)
    {
        if (delta.sqrMagnitude < 0.000001f) return;
        int dir = Mathf.Abs(delta.y) > Mathf.Abs(delta.x) ? (delta.y > 0f ? 1 : 2) : 0;
        if (HasParam("Dir")) animator.SetInteger("Dir", dir);
        if (body != null && Mathf.Abs(delta.x) > 0.0001f) body.flipX = delta.x < 0f;
    }

    public void SetHome(Vector2 pos)
    {
        homePos = pos;
    }

    // 병영 업그레이드 시 호출: 새 스탯 적용, 체력 가득
    public void ApplyData(SoldierData newData, float newEngageRange)
    {
        if (!IsAlive) return;
        data = newData;
        engageRange = newEngageRange;
        hp = data.maxHp;
        ApplyAnimator();
    }

    private void OnEnable()
    {
        EnemyTargets.Register(this);
        WaveSpawner.WaveCleared += OnWaveCleared;
    }

    private void OnDisable()
    {
        EnemyTargets.Unregister(this);
        WaveSpawner.WaveCleared -= OnWaveCleared;
    }

    // 웨이브가 끝나면 체력 가득 (다음 웨이브를 언제 불러도 손해 없게)
    private void OnWaveCleared(int wave)
    {
        if (!IsAlive) return;
        hp = data.maxHp;
        UpdateHealthBar();
    }


    // Update is called once per frame
    void Update()
    {
        if (!IsAlive)
        {
            return;
        }
        movedThisFrame = false;

        if(target == null || !Util.IsInEllipse(homePos, target.transform.position, engageRange))
        {
            ReleaseTarget();
        }

        if(target == null)
        {
            FindTarget();
        }
        
        if(target != null)
        {
           if(Vector2.Distance(transform.position,target.transform.position) > data.attackDistance)
            {
                MoveTo(target.transform.position);
            }
            else
            {
                attackCoolDown -= Time.deltaTime;
                if(attackCoolDown <= 0)
                {
                    Face(target.transform.position - transform.position);
                    PlayTrigger("Attack");
                    target.TakeDamage(data.damage, DamageType.Physical, AttackKind.Melee);
                    GameSounds.Instance?.PlayAllyHit(owner is HeroTower, owner.Level);
                    attackCoolDown = data.attackInterval;
                }
            }
        }
        else
        {
            MoveTo(homePos);
            Regenerate();
        }

        if (HasParam("Moving")) animator.SetBool("Moving", movedThisFrame);

        // 3-a. target이 있으면
        //      - 거리 > data.attackDistance → MoveTo(target 위치)
        //      - 거리 <= data.attackDistance → 쿨다운 감소, 0 이하면 공격 후 쿨다운 = data.attackInterval
        //        target.TakeDamage(data.damage, DamageType.Physical, AttackKind.Melee);
        // 3-b. target이 없으면 → MoveTo(homePos)
    }

    // 싸우는 적이 없으면 체력 회복 (KR처럼)
    private void Regenerate()
    {
        if (hp >= data.maxHp) return;
        hp = Mathf.Min(data.maxHp, hp + data.regenPerSecond * Time.deltaTime);
        UpdateHealthBar();
    }

    private void FindTarget()
    {
        List<Enemy> enemies = Enemy.FindInRadius(homePos, engageRange);
        foreach(Enemy enemy in enemies)
        {
            if (enemy.TryEngage(this))
            {
                target = enemy;
                break;
            }
        }
    }
    private void ReleaseTarget()
    {
        if(target != null)
        {
            target.Release(this);
        }
        target = null;
    }

    private void MoveTo(Vector2 pos)
    {
        Vector2 current = transform.position;
        Vector2 next = Vector2.MoveTowards(current, pos, data.moveSpeed * Time.deltaTime);
        if ((next - current).sqrMagnitude > 0.0000001f)
        {
            movedThisFrame = true;
            Face(next - current);
        }
        transform.position = next;
    }

    //IDamageable
    public void TakeDamage(float amount, DamageType type)
    {
        if (!IsAlive)
        {
            return;
        }
        hp-= amount;
        UpdateHealthBar();
        if(hp <= 0)
        {
            Die();
        }
        else if (!IsAttacking())
        {
            PlayTrigger("Hurt");   // 공격 모션 중에는 피격 모션으로 끊지 않음
        }
    }

    private void Die()
    {
        ReleaseTarget();
        owner.OnSoldierDied(this);
        if (HasParam("Moving")) animator.SetBool("Moving", false);
        PlayTrigger("Death");
        if (healthBarFill != null) healthBarFill.parent.gameObject.SetActive(false);
        GameSounds.Instance?.PlayAllyDeath();
        Destroy(gameObject, data.deathDuration);
    }
}
