// AI-assisted code start
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 적 종류별 기본 스탯 (ScriptableObject). 밸런싱은 이 에셋만 고치면 된다.
/// 런타임에 값이 바뀌어야 하면(버프 등) Enemy 쪽에서 복사본을 쓰고 원본은 건드리지 않는다.
/// </summary>
[CreateAssetMenu(fileName = "EnemyData", menuName = "TD/Enemy Data")]
public class EnemyData : ScriptableObject {
    // 1. 상수
    private const int DEFAULT_GOLD_REWARD = 10;
    private const int DEFAULT_MELEE_DAMAGE_MAX = 4;
    private const float DEFAULT_RANGED_INTERVAL = 1.4f;
    private const float DEFAULT_RADIUS = 0.18f;

    // 2. 필드
    [SerializeField] private string displayName = "Enemy";
    [Tooltip("정보 바에 나오는 한 줄 설명")]
    [TextArea(2, 4)] [SerializeField] private string description = "";
    [Tooltip("정보 바 초상화 (없으면 비워 둠)")]
    [SerializeField] private Sprite portrait;

    [Header("Stats")]
    [Min(1)] [SerializeField] private float maxHp = 50f;
    [Tooltip("이동 속도 (칸/초). 원작 속도에서 변환: speed = 원작 속도 / 28 (예: 고블린 46.1 → 1.65)")]
    [Min(0)] [SerializeField] private float speed = 1.5f;
    [Tooltip("물리 피해 감소율. 폭발 피해는 이 값의 절반만큼 감소 (예: 0.8 → 폭발 40% 감소)")]
    [Range(0, 1)] [SerializeField] private float armor = 0f;
    [Tooltip("마법 피해 감소율")]
    [Range(0, 1)] [SerializeField] private float magicResist = 0f;
    [Tooltip("근접 공격 회피 확률 (0~1). 원거리 공격은 항상 맞는다. 예: 늑대 0.3")]
    [Range(0, 1)] [SerializeField] private float meleeEvasion = 0f;

    [Header("Reward / Penalty")]
    [SerializeField] private int goldReward = DEFAULT_GOLD_REWARD;
    [SerializeField] private int lifeDamage = 1;

    [Header("Melee (붙잡혔을 때 반격)")]
    [Tooltip("반격 피해 최댓값 (포함)")]
    [FormerlySerializedAs("meleeDamage")]
    [SerializeField] private int meleeDamageMax = DEFAULT_MELEE_DAMAGE_MAX;
    [Tooltip("반격 피해 최솟값 (포함)")]
    [SerializeField] private int meleeDamageMin = 1;
    [SerializeField] private float meleeInterval = 1f;
    [Tooltip("근접 공격 한 번에 때리는 최대 대상 수. 1 = 붙잡은 병사만, 2 이상 = 주변 병사까지 범위 공격 (예: 베즈난 8)")]
    [Min(1)] [SerializeField] private int meleeTargetCount = 1;
    [Tooltip("범위 근접 공격 가로 반경 (칸, 2.5D 타원). meleeTargetCount가 2 이상일 때만 사용")]
    [Min(0)] [SerializeField] private float meleeAreaRadius = 0.9f;

    [Header("Ranged (사거리 안의 병사를 멈춰서 쏨)")]
    [Tooltip("원거리 공격 가로 사거리 (칸, 2.5D 타원 = 타워와 같은 방식). 0이면 원거리 공격 없음")]
    [Min(0)] [SerializeField] private float rangedRange = 0f;
    [Tooltip("원거리 피해 최솟값 (포함)")]
    [SerializeField] private int rangedDamageMin = 0;
    [Tooltip("원거리 피해 최댓값 (포함)")]
    [SerializeField] private int rangedDamageMax = 0;
    [SerializeField] private float rangedInterval = DEFAULT_RANGED_INTERVAL;

    [Header("Boss")]
    [Tooltip("보스 등급 (UI 표시 등)")]
    [SerializeField] private bool isBoss = false;
    [Tooltip("죽을 피해를 받으면 이 데이터로 변신하고 체력이 가득 찬다 (예: 베즈난 → 악마 형태). 비우면 그냥 죽음")]
    [SerializeField] private EnemyData nextForm;
    [Tooltip("이 적이 죽으면 전장의 다른 적이 모두 죽는다 (최후의 저항)")]
    [SerializeField] private bool killAllOnDeath = false;
    [Tooltip("변신했을 때 몸 색 (알파 0이면 그대로)")]
    [SerializeField] private Color formTint = new(0f, 0f, 0f, 0f);

    [Header("Type")]
    [Tooltip("공중 유닛: 병영 병사가 붙잡을 수 없다 (공중 공격 가능한 타워만 공격)")]
    [SerializeField] private bool isFlying = false;
    [Tooltip("면역인 상태이상 (여러 개 선택 가능). 예: 거미 = Poison")]
    [SerializeField] private StatusEffect immunities = StatusEffect.None;

    [Header("Body")]
    [Tooltip("유닛 반지름: 차선 이동 시 길 가장자리에서 이만큼 안쪽까지만 간다")]
    [SerializeField] private float radius = DEFAULT_RADIUS;

    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Portrait => portrait;
    public float MaxHp => maxHp;
    public float Speed => speed;
    public float Armor => armor;
    public float MagicResist => magicResist;
    public float MeleeEvasion => meleeEvasion;
    public int GoldReward => goldReward;
    public int LifeDamage => lifeDamage;
    public int MeleeDamageMin => meleeDamageMin;
    public int MeleeDamageMax => meleeDamageMax;
    public float MeleeInterval => meleeInterval;
    public int MeleeTargetCount => meleeTargetCount;
    public float MeleeAreaRadius => meleeAreaRadius;
    public bool IsBoss => isBoss;
    public EnemyData NextForm => nextForm;
    public bool KillAllOnDeath => killAllOnDeath;
    public Color FormTint => formTint;
    public float Radius => radius;
    public float RangedRange => rangedRange;
    public int RangedDamageMin => rangedDamageMin;
    public int RangedDamageMax => rangedDamageMax;
    public float RangedInterval => rangedInterval;
    public bool HasRangedAttack => rangedRange > 0f && rangedDamageMax > 0;
    public bool IsFlying => isFlying;
    public StatusEffect Immunities => immunities;

    // 4. 메서드 (private)
    /// <summary>인스펙터에서 최솟값이 최댓값보다 커지지 않게 막는다.</summary>
    private void OnValidate() {
        meleeDamageMin = Mathf.Min(meleeDamageMin, meleeDamageMax);
        rangedDamageMin = Mathf.Min(rangedDamageMin, rangedDamageMax);
    }
}
// AI-assisted code end
