using UnityEngine;

// 영웅 타워: 병영과 같은 구조(소환·리스폰·집결지)에 병사 1명, 강한 SoldierData를 쓴다.
// 차이는 데이터(HeroTowerData, Hero_Lv1~4)와 집결지 범위(무제한)뿐.
public class HeroTower : BarracksTower
{
    // 맵 어디든 길 위라면 집결지로 지정 가능 (사실상 무제한)
    private const float UNLIMITED_RALLY_RANGE = 1000f;

    public override float RallyRange => UNLIMITED_RALLY_RANGE;
}
