# Enemy (적 모듈)

담당: 이수 · 맵 모듈(`TD.Map`)만 참조하고 타워·GameManager는 모른다 — `TD.Enemy` asmdef가 강제.

## 파일 위치 (`Assets/TowerDefense/` 아래)

| 위치 | 내용 |
|---|---|
| `Scripts/Enemy/Enemy.cs` | 경로 거리 + 차선 이동, 붙잡힘/반격, 교전 겹침 방지, 이벤트 |
| `Scripts/Enemy/EnemyData.cs` | 적 종류별 스탯 SO |
| `Scripts/Enemy/WaveSpawner.cs` | 웨이브/그룹 스폰, 경로 랜덤 선택, 무리(pack) 퍼짐 |
| `Scripts/Enemy/DamageType.cs`, `IDamageable.cs` | 전투 공용 타입 (타워 담당도 사용) |
| `Scripts/Enemy/Editor/EnemyBuilder.cs` | Tools > TD > Enemy > Build Placeholder Enemies |
| `Data/Enemy/*.asset` | 스탯 SO: Grunt, Runner, Armored |
| `Prefabs/Enemy/Enemy_*.prefab` | 임시 그래픽 프리팹 (트리거 Collider2D + Kinematic Rigidbody2D 포함) |

## 게임 쪽 연동 (이벤트)

```csharp
Enemy.Killed      += enemy => gold  += enemy.Data.GoldReward;
Enemy.ReachedGoal += enemy => lives -= enemy.Data.LifeDamage;
WaveSpawner.WaveStarted     += waveNumber => { };
WaveSpawner.AllWavesCleared += () => Win();
```

## 타워 쪽에서 쓰는 것

| 멤버 | 용도 |
|---|---|
| `Enemy.Active`, `Enemy.FindInRadius(c, r, list)` | 살아있는 적 / 범위 내 적 |
| `enemy.RemainingDistance` | 출구까지 남은 거리 — **작을수록 앞선 적** (추월돼도, 경로가 달라도 정확) |
| `enemy.Progress` | 지나온 거리 |
| `enemy.TakeDamage(amount, DamageType)` | 피해 (물리 → 방어력, 마법 → 마법 저항 적용) |
| `enemy.IsFlying` | 공중 유닛 (병영이 붙잡을 수 없음) |
| `enemy.TryEngage(this)` / `enemy.Release(this)` | 병영 1:1 붙잡기 / 놓기 |
| `enemy.IsBlocked`, `enemy.Blocker` | 붙잡힌 상태 / 붙잡은 대상 |
| `enemy.Data` | 체력·속도·방어력·마법저항·보상·공중 여부 등 (`EnemyData`) |

- 적은 자신을 붙잡은 대상이 `IDamageable`이면 반격한다 (병사가 구현)
- 프리팹에 트리거 충돌체가 있어서 타워 사거리 트리거(`OnTriggerEnter2D`)로 감지 가능

## 이동 방식 — 차선(lane)

부모로 묶지 않고 적마다 `Lane`(-1~1)을 갖는다. `위치 = route.GetPosition(Progress, Lane, 반지름)`
→ 코너에서도 길 폭 안에서 같이 돈다. 붙잡혔다 풀려나면 원래 차선으로 걸어서 합류한 뒤 다시 진행.

## 웨이브 그룹 (`WaveSpawner.SpawnGroup`)

| 필드 | 의미 |
|---|---|
| `Routes` | 경로 이름들, 적마다 랜덤 (위 입구 = `Path_TopMiddle`, `Path_TopLoop`) |
| `Count`, `Interval`, `StartDelay` | 총 수 / 무리 사이 간격 / 시작 지연 |
| `PackSize` | 한 번에 같이 나오는 수 |
| `LaneSpread` | 차선 폭 사용 비율 (0 = 한 줄, 1 = 길 폭 전체) |
| `PackDepth` | 무리 안 앞뒤 흩어짐 |
