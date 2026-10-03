using UnityEngine;

public class ArtilleryTower : AttackTower
{
    protected override bool CanTarget(Enemy enemy)
    {
        return !enemy.IsFlying;
    }
}
