// AI-assisted code start
/// <summary>
/// 피해 종류.
///   Physical  = 방어력만큼 감소 (아처, 병영)
///   Magic     = 마법 저항만큼 감소 (마법사)
///   Explosive = 방어력의 절반만큼 감소 (포병 포탄). 예: 방어력 80% → 폭발 40% 감소
/// 값 순서를 바꾸지 말 것 (에셋에 숫자로 저장됨). 새 종류는 맨 뒤에 추가.
/// </summary>
public enum DamageType {
    Physical,
    Magic,
    Explosive
}
// AI-assisted code end
