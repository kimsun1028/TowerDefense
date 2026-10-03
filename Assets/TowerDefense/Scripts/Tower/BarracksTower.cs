using System.Collections.Generic;
using UnityEngine;

public class BarracksTower : Tower, IRallyPointUser
{
    [SerializeField] private BarracksTowerData data;
    [SerializeField] private Soldier soldierPrefeb;
    [SerializeField] private float formationRadius = 0.25f; // 집결지 주변 병사 간격

    private Soldier[] soldiers;
    private float[] respawnTimers; // 남은 리스폰 시간 리스트

    private BarracksLevelStats CurrentStats => data.levels[level - 1];
    public override TowerData Data => data;
    public override bool HasRallyPoint => true;

    public override void BeginRallyPoint()
    {
        // TODO: RallyPointTool.Instance.Begin(this, transform.position)
    }

    // IRallyPointUser
    public float RallyRange => slot.RallyRange;
    public Vector2 RallyPoint {get; private set; }

    private void Start()
    {
        // 1. RallyPoint = slot.RallyPointWorld   (Init에서 slot이 이미 들어와 있음)
        RallyPoint = slot.RallyPointWorld;
        // 2. CurrentStats.soldierCount만큼 SpawnSoldier()
        soldiers = new Soldier[CurrentStats.soldierCount];
        respawnTimers = new float[CurrentStats.soldierCount];
        for(int i = 0; i < CurrentStats.soldierCount; i++)
        {
            SpawnSoldier(i);
        }
    }

    private void Update()
    {   
       for(int i = 0; i < soldiers.Length; i++)
        {
            if (soldiers[i] != null) continue;

            respawnTimers[i] -= Time.deltaTime;
            if(respawnTimers[i] <= 0f)
            {
                SpawnSoldier(i);
            }
        }
    }

    private void SpawnSoldier(int index)
    {   
        // 1. 병영 위치에서 soldierPrefab 생성
        Soldier soldier = Instantiate(
            soldierPrefeb,
            transform.position,
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