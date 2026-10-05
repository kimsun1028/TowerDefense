using System.Collections.Generic;
using UnityEngine;

public class Shell: Projectile
{
    [SerializeField] private float splashRadius = 0.5f;
    public float SplashRadius => splashRadius;

    // 유도 중 목표가 죽어도 마지막 위치에 떨어져 폭발
    protected override bool HitsWithoutTarget => true;

    protected override void OnHit()
    {
        // 1. 착탄 지점 기준으로 반경 안 콜라이더 전부 가져오기
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position,splashRadius);
        foreach(Collider2D other in hits)
        {
            if(other.TryGetComponent(out Enemy enemy))
            {
                // 몸통이 폭발 범위 타원에 조금이라도 걸치면 피해
                if(Util.IsBodyTouchingEllipse(transform.position, splashRadius, enemy))
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