using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent (typeof(CircleCollider2D))]
public abstract class AttackTower : Tower
{
    [SerializeField] protected AttackTowerData data;
    [SerializeField, FormerlySerializedAs("arrowPrefab")] protected Projectile projectilePrefab;
    [SerializeField] protected Transform firePoint;

    protected List<Enemy> targetList = new List<Enemy>();

    protected Enemy target;
    protected AttackLevelStats CurrentStats => data.levels[level - 1];
    public override TowerData Data => data;
    protected override float RangeRadius => CurrentStats.range;
    
    private CircleCollider2D rangeCollider;
    private float cooldown;

    protected virtual void Start()
    {
        rangeCollider = GetComponent<CircleCollider2D>();
        rangeCollider.radius = CurrentStats.range;
        rangeCollider.isTrigger = true;
    }

    protected override void OnLevelChanged()
    {
        // 업그레이드로 사거리가 바뀌면 감지 트리거도 같이 갱신
        if (rangeCollider != null)
        {
            rangeCollider.radius = CurrentStats.range;
        }
    }

    protected virtual void Update()
    {   
        FindTarget();
        cooldown -= Time.deltaTime;
        if(cooldown <= 0 && target != null)
        {   
            Attack();
            cooldown = 1f / CurrentStats.attackSpeed;
        }
    }

    public void AddTargetList(Enemy enemy)
    {
        if(!targetList.Contains(enemy))
        {
            targetList.Add(enemy);    
        }
    }

    public void DeleteTargetList(Enemy enemy)
    {
        targetList.Remove(enemy);
    }

    private void FindTarget()
    {   
        targetList.RemoveAll(e => e == null);
        target = null;
        foreach(Enemy enemy in targetList)
        {
            if(CanTarget(enemy) && Util.IsInEllipse(transform.position,enemy.transform.position,CurrentStats.range))
            {
                target = enemy;
                break;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {     
        if(other.TryGetComponent(out Enemy enemy))
        {
            AddTargetList(enemy);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if(other.TryGetComponent(out Enemy enemy))
        {
            DeleteTargetList(enemy);
        }   
    }

    protected virtual void Attack()
    {
        PlayTrigger("Shoot");
        GameSounds.Instance?.PlayTowerShoot(GetType().Name, level);
        Projectile projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        projectile.Init(target, CurrentStats.damage);

        // 레벨별 발사체 외형
        RuntimeAnimatorController[] list = data.projectileAnimators;
        if (list != null && level - 1 < list.Length && list[level - 1] != null)
        {
            projectile.SetAnimator(list[level - 1]);
        }
    }

    protected virtual bool CanTarget(Enemy enemy)
    {
        return CanTargetFlying || !enemy.IsFlying;
    }

    // 정보 바 표시용 (실제 피해 종류는 발사체가 정함 → 발사체와 맞춰서 덮어씀)
    protected virtual DamageType DamageKind => DamageType.Physical;
    protected virtual bool CanTargetFlying => true;

    // 정보 바: 피해 종류, 공격력(DPS), 공격 간격, 사거리, 비행 적
    protected override void AddInfoRows(List<InfoRow> rows)
    {
        AttackLevelStats stats = CurrentStats;
        float interval = 1f / stats.attackSpeed;
        rows.Add(new InfoRow("피해 종류", FormatDamageType(DamageKind)));
        rows.Add(new InfoRow("공격력", $"{stats.damage} (DPS {stats.damage * stats.attackSpeed:0.#})"));
        rows.Add(new InfoRow("공격 간격", FormatInterval(interval)));
        rows.Add(new InfoRow("사거리", FormatRange(stats.range)));
        rows.Add(new InfoRow("비행 적", CanTargetFlying ? "공격 가능" : "공격 불가"));
    }

}
