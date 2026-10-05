using UnityEngine;

public abstract class TowerData : ScriptableObject
{
    public string towerName;
    public string displayName;          // 정보 바에 보이는 이름 (비면 towerName)
    [TextArea] public string description;
    public Sprite[] portraits;          // 레벨별 정보 바 초상화 (0번 = Lv1, 없으면 지금 그림)
    public int[] costs;   // 0번 = 건설비, 1번 = Lv2 업그레이드비, 2번 = Lv3 업그레이드비
    public RuntimeAnimatorController[] animators;   // 레벨별 타워 외형 (0번 = Lv1)
    public abstract int MaxLevel { get; }
}
