using UnityEngine;

public abstract class TowerData : ScriptableObject
{
    public string towerName;
    public int[] costs;   // 0번 = 건설비, 1번 = Lv2 업그레이드비, 2번 = Lv3 업그레이드비
    public abstract int MaxLevel { get; }
}
