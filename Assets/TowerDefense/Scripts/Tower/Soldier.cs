using System.Collections.Generic;
using UnityEngine;

public class Soldier : MonoBehaviour , IDamageable
{
    private BarracksTower owner;
    private SoldierData data;
    private Vector2 homePos;    // 집결지 기준 내자리
    private float engageRange;  // 집결지 기준 적 탐지 반경 (타워가 넘겨줌)
    private float hp;
    private float attackCoolDown;
    private Enemy target;

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
    }

    public void SetHome(Vector2 pos)
    {
        homePos = pos;
    }

    private void OnEnable()
    {
        EnemyTargets.Register(this);
    }

    private void OnDisable()
    {
        EnemyTargets.Unregister(this);
    }


    // Update is called once per frame
    void Update()
    {
        if (!IsAlive)
        {
            return;
        }

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
                    target.TakeDamage(data.damage, DamageType.Physical, AttackKind.Melee);
                    attackCoolDown = data.attackInterval;
                }
            }
        }
        else
        {
            MoveTo(homePos);
        }

        // 3-a. target이 있으면
        //      - 거리 > data.attackDistance → MoveTo(target 위치)
        //      - 거리 <= data.attackDistance → 쿨다운 감소, 0 이하면 공격 후 쿨다운 = data.attackInterval
        //        target.TakeDamage(data.damage, DamageType.Physical, AttackKind.Melee);
        // 3-b. target이 없으면 → MoveTo(homePos)
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
        transform.position = Vector2.MoveTowards(transform.position,pos,data.moveSpeed * Time.deltaTime);
    }

    //IDamageable
    public void TakeDamage(float amount, DamageType type)
    {
        if (!IsAlive)
        {
            return;
        }
        hp-= amount;
        if(hp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        ReleaseTarget();
        owner.OnSoldierDied(this);
        Destroy(gameObject, data.deathDuration);
    }
}
