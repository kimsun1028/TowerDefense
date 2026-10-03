// AI-assisted code start
/// <summary>
/// 공격 방식. 피해 종류(DamageType: 물리/마법)와는 다른 축이다.
/// 예: 병사 = Melee + Physical, 마법사 = Ranged + Magic. 회피(근접만) 같은 판정에 쓴다.
/// </summary>
public enum AttackKind {
    Melee,  // 병사, 영웅
    Ranged  // 화살, 마법, 포탄
}
// AI-assisted code end
