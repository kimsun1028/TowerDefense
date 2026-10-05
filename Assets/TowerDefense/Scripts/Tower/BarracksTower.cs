using System.Collections.Generic;
using UnityEngine;

public class BarracksTower : Tower, IRallyPointUser
{
    [SerializeField] private BarracksTowerData data;
    [SerializeField] private Soldier soldierPrefeb;
    [SerializeField] private float formationRadius = 0.25f; // 집결지 주변 병사 간격

    private Soldier[] soldiers;
    private float[] respawnTimers; // 남은 리스폰 시간 리스트
    private bool[] spawning;       // 소환 연출 중(지연 소환 대기)인 칸

    // 소환 연출 (영웅 타워가 덮어씀)
    protected virtual string SpawnTrigger => "DoorOpen";        // 소환 시 타워 애니메이션 트리거
    protected virtual float SpawnDelay => 0f;                   // 트리거 후 실제 생성까지 (초)
    protected virtual Vector2 SpawnOffset => Vector2.zero;      // 타워 기준 생성 위치

    private BarracksLevelStats CurrentStats => data.levels[level - 1];
    public override TowerData Data => data;
    protected override float RangeRadius => 0f;   // 병영(영웅 포함)은 선택해도 회색 사거리를 표시하지 않음 (집결지 범위는 RallyPointTool이 표시)
    public override bool HasRallyPoint => true;

    public override void BeginRallyPoint()
    {
        RallyPointTool.Instance.Begin(this, transform.position);
    }

    // 업그레이드: 살아있는 병사는 새 레벨 데이터로 교체(체력 가득), 병사 수가 늘면 빈 칸 추가
    protected override void OnLevelChanged()
    {
        if (soldiers == null) return;   // Start 전(지은 직후 같은 프레임)이면 Start에서 현재 레벨로 소환됨
        int count = CurrentStats.soldierCount;
        if (count != soldiers.Length)
        {
            for (int i = count; i < soldiers.Length; i++)   // 줄어드는 경우 남는 병사 정리
            {
                if (soldiers[i] != null) Destroy(soldiers[i].gameObject);
            }
            System.Array.Resize(ref soldiers, count);
            System.Array.Resize(ref respawnTimers, count);
            System.Array.Resize(ref spawning, count);
        }

        for (int i = 0; i < soldiers.Length; i++)
        {
            if (soldiers[i] == null) continue;
            soldiers[i].ApplyData(CurrentStats.soldier, CurrentStats.engageRange);
            soldiers[i].SetHome(GetHomePos(i));
        }
    }

    // 정보 바: 병사 수·체력(회복)·공격·리스폰 (영웅도 같은 줄, 이름만 영웅 데이터)
    protected override void AddInfoRows(List<InfoRow> rows)
    {
        BarracksLevelStats stats = CurrentStats;
        SoldierData soldier = stats.soldier;
        if (stats.soldierCount > 1) rows.Add(new InfoRow("병사 수", $"{stats.soldierCount}명"));
        if (soldier != null)
        {
            rows.Add(new InfoRow("체력", $"{soldier.maxHp:0} (초당 +{soldier.regenPerSecond:0.#})"));
            rows.Add(new InfoRow("근접 공격", $"{soldier.damage:0.#} ({soldier.attackInterval:0.##}초마다)"));
        }
        rows.Add(new InfoRow("리스폰", $"{stats.respawnTime:0.#}초"));
    }

    // IRallyPointUser
    public virtual float RallyRange => slot.RallyRange;
    public Vector2 RallyPoint {get; private set; }

    private void Start()
    {
        // 1. RallyPoint = slot.RallyPointWorld   (Init에서 slot이 이미 들어와 있음)
        RallyPoint = slot.RallyPointWorld;
        // 2. CurrentStats.soldierCount만큼 SpawnSoldier()
        soldiers = new Soldier[CurrentStats.soldierCount];
        respawnTimers = new float[CurrentStats.soldierCount];
        spawning = new bool[CurrentStats.soldierCount];
        for(int i = 0; i < CurrentStats.soldierCount; i++)
        {
            BeginSpawn(i);
        }
    }

    private void Update()
    {
       for(int i = 0; i < soldiers.Length; i++)
        {
            if (soldiers[i] != null || spawning[i]) continue;

            respawnTimers[i] -= Time.deltaTime;
            if(respawnTimers[i] <= 0f)
            {
                BeginSpawn(i);
            }
        }
    }

    // 소환 연출 시작: 타워 애니메이션 → SpawnDelay 뒤 실제 생성
    private void BeginSpawn(int index)
    {
        spawning[index] = true;
        PlaySpawnAnimation();
        if (SpawnDelay <= 0f) SpawnSoldier(index);
        else StartCoroutine(SpawnAfterDelay(index));
    }

    protected virtual void PlaySpawnAnimation()
    {
        PlayTrigger(SpawnTrigger);
    }

    private System.Collections.IEnumerator SpawnAfterDelay(int index)
    {
        yield return new WaitForSeconds(SpawnDelay);
        if (index < soldiers.Length) SpawnSoldier(index);
    }

    private void SpawnSoldier(int index)
    {
        spawning[index] = false;
        // 1. 병영 위치(+연출 오프셋)에서 soldierPrefab 생성
        Soldier soldier = Instantiate(
            soldierPrefeb,
            (Vector2)transform.position + SpawnOffset,
            Quaternion.identity,
            transform
        );
        // 2. soldier.Init(this, GetHomePos(soldiers.Count), CurrentStats.soldier, CurrentStats.engageRange)
        soldier.Init(this, GetHomePos(index), CurrentStats.soldier, CurrentStats.engageRange);
        // 3. soldiers에 추가
        soldiers[index] = soldier;

    }

    // 집결지 주변에 삼각형으로 배치 (index 0, 1, 2)
    private Vector2 GetHomePos(int index)
    {   
        float rad = index * 120 * Mathf.Deg2Rad;
        Vector2 homePos = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * formationRadius;
        // 각도 = index * 120도 → RallyPoint + (cos, sin) * formationRadius
        return RallyPoint + homePos;
    }

    // Soldier.Die()에서 호출
    public void OnSoldierDied(Soldier soldier)
    {
        int i = System.Array.IndexOf(soldiers, soldier);
        if(i < 0) return;
        soldiers[i] = null;
        respawnTimers[i] = CurrentStats.respawnTime;

    }

    // IRallyPointUser — 집결지 이동 (RallyPointTool이 호출)
    public void SetRallyPoint(Vector2 point)
    {
        RallyPoint = point;
        for(int i = 0; i < soldiers.Length; i++){
            // RallyPoint 저장 후, soldiers 각각에 SetHome(GetHomePos(i))
            if(soldiers[i] == null) continue;
            soldiers[i].SetHome(GetHomePos(i));
        }
    }

    
}