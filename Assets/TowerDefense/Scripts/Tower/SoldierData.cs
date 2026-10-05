using UnityEngine;

[CreateAssetMenu(fileName = "SoldierData", menuName = "TowerDefense/SoldierData")]
public class SoldierData : ScriptableObject
{
    public string soldierName;
    public string displayName;          // 정보 바에 보이는 이름 (비면 soldierName)
    [TextArea] public string description;
    public Sprite portrait;             // 정보 바 초상화 (없으면 지금 그림)
    public float healthBarHeight = 0.55f;   // 발밑 기준 체력바 높이
    public float maxHp = 50f;
    public float regenPerSecond = 4f;   // 싸우지 않을 때 초당 체력 회복
    public float damage = 3f;
    public float attackInterval = 1f;   // 공격 간격 (초)
    public float moveSpeed = 1.5f;
    public float attackDistance = 0.3f; // 이 거리 안이면 공격
    public float deathDuration = 0.5f;  // 사망 애니메이션 길이 (이후 오브젝트 삭제)
    public RuntimeAnimatorController animator;   // 이 병사의 외형
}
