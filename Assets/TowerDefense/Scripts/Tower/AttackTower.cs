using System.Collections.Generic;
using UnityEngine;

public abstract class AttackTower : Tower
{
    [SerializeField] protected AttackTowerData data;

    protected List<Enemy> targetList = new List<Enemy>();

    protected Enemy target;
    protected AttackLevelStats CurrentStats => data.levels[level - 1];
    private float cooldown;


    void Start()
    {
        
    }
    void Update()
    {   
        FindTarget();
        cooldown -= Time.deltaTime;
        if(cooldown <= 0 && target != null)
        {   
            Attack();
            cooldown = 1f / CurrentStats.attackSpeed;
        }
        // 타겟 찾기 -> 쿨감소 -> 0초 이하면 Attack()호출 후 cooldown = 1f / attackSpeed;
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
        if(targetList.Count == 0)
        {
            target = null;
        }
        else
        {

            target = targetList[0];
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

    protected abstract void Attack();

}
