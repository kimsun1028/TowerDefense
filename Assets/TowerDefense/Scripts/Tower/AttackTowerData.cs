using UnityEngine;

[CreateAssetMenu(fileName = "AttackTowerData", menuName = "TowerDefense/AttackTowerData")]
public class AttackTowerData : TowerData
{
    public AttackLevelStats[] levels;   // 0번 = 레벨 1
    public RuntimeAnimatorController[] projectileAnimators;   // 레벨별 발사체 외형 (0번 = Lv1)
    public override int MaxLevel => levels.Length;
}

[System.Serializable]
public class AttackLevelStats
{
    public int damage;
    public float range;
    public float attackSpeed;     // 초당 공격 횟수
}
