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
    
    private CircleCollider2D rangeCollider;
    private float cooldown;

    protected virtual void Start()
    {
        rangeCollider = GetComponent<CircleCollider2D>();
        rangeCollider.radius = CurrentStats.range;
        rangeCollider.isTrigger = true;
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
            if(Util.IsInEllipse(transform.position,enemy.transform.position,CurrentStats.range))
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
        Projectile projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        projectile.Init(target, CurrentStats.damage);
    }

}
