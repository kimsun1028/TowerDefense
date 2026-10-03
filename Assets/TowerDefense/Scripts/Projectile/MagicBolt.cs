using UnityEngine;

public class MagicBolt : Projectile
{
    protected override void OnHit()
    {
        Debug.Log($"[MagicBolt] 명중 -> {target.name}, damage {damage}");
    }
}