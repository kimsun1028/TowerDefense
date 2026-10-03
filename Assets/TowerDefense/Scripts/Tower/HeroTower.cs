using System.Collections;
using UnityEngine;

// 영웅 타워: 병영과 같은 구조(소환·리스폰·집결지)에 병사 1명, 강한 SoldierData를 쓴다.
// 차이는 데이터(HeroTowerData, Hero_Lv1~4), 집결지 범위(무제한), 리스폰 연출뿐.
public class HeroTower : BarracksTower
{
    // 맵 어디든 길 위라면 집결지로 지정 가능 (사실상 무제한)
    private const float UNLIMITED_RALLY_RANGE = 1000f;

    // 리스폰 연출 (Sprites/Towers/HeroTower/프레임정보.md):
    // Respawn 8프레임 @10fps(0.8초) 중 index 5(섬광)에서 영웅 생성, 위치는 타워 pivot 기준 (+16px, +19px) @ PPU 100
    private const string IDLE_STATE = "Idle";
    private const string RESPAWN_STATE = "Respawn";
    private const float RESPAWN_ANIM_LENGTH = 0.8f;

    public override float RallyRange => UNLIMITED_RALLY_RANGE;

    protected override float SpawnDelay => 0.5f;
    protected override Vector2 SpawnOffset => new Vector2(0.16f, 0.19f);

    // HeroTower_Lv1~4 컨트롤러는 파라미터 없이 Idle/Respawn 상태만 있으므로 상태를 직접 재생
    protected override void PlaySpawnAnimation()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        StopCoroutine(nameof(PlayRespawn));
        StartCoroutine(nameof(PlayRespawn));
    }

    private IEnumerator PlayRespawn()
    {
        animator.Play(RESPAWN_STATE, 0, 0f);
        yield return new WaitForSeconds(RESPAWN_ANIM_LENGTH);
        animator.Play(IDLE_STATE, 0, 0f);
    }
}
