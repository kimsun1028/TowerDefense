// AI-assisted code start
using UnityEngine;

/// <summary>
/// 적 종류별 기본 스탯 (ScriptableObject). 밸런싱은 이 에셋만 고치면 된다.
/// 런타임에 값이 바뀌어야 하면(버프 등) Enemy 쪽에서 복사본을 쓰고 원본은 건드리지 않는다.
/// </summary>
[CreateAssetMenu(fileName = "EnemyData", menuName = "TD/Enemy Data")]
public class EnemyData : ScriptableObject {
    // 2. 필드
    [SerializeField] private string displayName = "Enemy";

    [Header("Stats")]
    [Min(1)] [SerializeField] private float maxHp = 50f;
    [Min(0)] [SerializeField] private float speed = 1.5f;
    [Tooltip("물리 피해 감소율")]
    [Range(0, 1)] [SerializeField] private float armor = 0f;
    [Tooltip("마법 피해 감소율")]
    [Range(0, 1)] [SerializeField] private float magicResist = 0f;

    [Header("Reward / Penalty")]
    [SerializeField] private int goldReward = 10;
    [SerializeField] private int lifeDamage = 1;

    [Header("Melee (붙잡혔을 때 반격)")]
    [SerializeField] private float meleeDamage = 4f;
    [SerializeField] private float meleeInterval = 1f;

    [Header("Type")]
    [Tooltip("공중 유닛: 병영 병사가 붙잡을 수 없다 (공중 공격 가능한 타워만 공격)")]
    [SerializeField] private bool isFlying = false;

    [Header("Body")]
    [Tooltip("유닛 반지름: 차선 이동 시 길 가장자리에서 이만큼 안쪽까지만 간다")]
    [SerializeField] private float radius = 0.18f;

    public string DisplayName => displayName;
    public float MaxHp => maxHp;
    public float Speed => speed;
    public float Armor => armor;
    public float MagicResist => magicResist;
    public int GoldReward => goldReward;
    public int LifeDamage => lifeDamage;
    public float MeleeDamage => meleeDamage;
    public float MeleeInterval => meleeInterval;
    public float Radius => radius;
    public bool IsFlying => isFlying;
}
// AI-assisted code end
