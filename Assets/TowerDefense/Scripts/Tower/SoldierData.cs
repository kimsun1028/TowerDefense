using UnityEngine;

[CreateAssetMenu(fileName = "SoldierData", menuName = "TowerDefense/SoldierData")]
public class SoldierData : ScriptableObject
{
    public string soldierName;
    public float maxHp = 50f;
    public float damage = 3f;
    public float attackInterval = 1f;   // 공격 간격 (초)
    public float moveSpeed = 1.5f;
    public float attackDistance = 0.3f; // 이 거리 안이면 공격
    public float deathDuration = 0.5f;  // 사망 애니메이션 길이 (이후 오브젝트 삭제)
    public RuntimeAnimatorController animator;   // 이 병사의 외형
}
