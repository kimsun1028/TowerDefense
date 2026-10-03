using System.Collections.Generic;
using UnityEngine;

public class Shell: Projectile
{
    [SerializeField] private float splashRadius = 0.5f;

    protected override void OnHit()
    {
        // 1. 착탄 지점 기준으로 반경 안 콜라이더 전부 가져오기
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position,splashRadius);
        foreach(Collider2D other in hits)
        {
            if(other.TryGetComponent(out Enemy enemy))
            {
                if(Util.IsInEllipse(transform.position,enemy.transform.position,splashRadius))
                {
                    if(!enemy.IsFlying)
                    {
                        enemy.TakeDamage(damage, DamageType.Explosive);
                    }
                }
            }
        }
    }
}