using UnityEngine;

[CreateAssetMenu(fileName = "BarracksTowerData", menuName = "TowerDefense/BarracksTowerData")]
public class BarracksTowerData : TowerData
{
    public BarracksLevelStats[] levels;   // 0번 = 레벨 1
    public override int MaxLevel => levels.Length;
}

[System.Serializable]
public class BarracksLevelStats
{
    public SoldierData soldier;         // 이 레벨에서 소환하는 병사
    public int soldierCount = 3;
    public float respawnTime = 10f;     // 병사 사망 후 리스폰까지 (초)
    public float engageRange = 1.2f;    // 집결지 기준 적 탐지 반경
}
