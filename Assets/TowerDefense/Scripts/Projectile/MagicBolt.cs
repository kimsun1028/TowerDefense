using UnityEngine;

public class MagicBolt : Projectile
{
    protected override void OnHit()
    {
        target.TakeDamage(damage, DamageType.Magic);
    }
}