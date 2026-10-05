using UnityEngine;

public class MagicTower : AttackTower

{
    protected override DamageType DamageKind => DamageType.Magic;   // MagicBolt
}
