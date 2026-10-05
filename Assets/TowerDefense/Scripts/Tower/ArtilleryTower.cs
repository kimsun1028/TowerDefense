using System.Collections.Generic;
using UnityEngine;

public class ArtilleryTower : AttackTower
{
    protected override DamageType DamageKind => DamageType.Explosive;   // Shell
    protected override bool CanTargetFlying => false;

    protected override void AddInfoRows(List<InfoRow> rows)
    {
        base.AddInfoRows(rows);
        if (projectilePrefab is Shell shell)
        {
            rows.Add(new InfoRow("특성", $"범위 피해 (반경 {shell.SplashRadius:0.##})"));
        }
    }
}
