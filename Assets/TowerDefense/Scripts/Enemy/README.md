# Enemy (적 모듈)

담당: 이수 · 맵 모듈(`TD.Map`)만 참조하고 타워·GameManager는 모른다 — `TD.Enemy` asmdef가 강제.

## 파일 위치 (`Assets/TowerDefense/` 아래)

| 위치 | 내용 |
|---|---|
| `Scripts/Enemy/Enemy.cs` | 경로 거리 + 차선 이동, 붙잡힘/반격, 교전 겹침 방지, 이벤트 |
| `Scripts/Enemy/EnemyData.cs` | 적 종류별 스탯 SO |
| `Scripts/Enemy/WaveSpawner.cs` | 웨이브/그룹 스폰, 경로 랜덤 선택, 무리(pack) 퍼짐 |
| `Scripts/Enemy/EnemyTargets.cs`, `EnemyProjectile.cs` | 원거리 적의 공격 대상 목록 / 적이 쏘는 화살 |
| `Scripts/Enemy/EllipseRange.cs` | 2.5D 타원 사거리 판정 (타워 쪽 `Util.IsInEllipse`와 같은 공식, 카메라 45°) |
| `Scripts/Enemy/DamageType.cs`, `AttackKind.cs`, `StatusEffect.cs`, `IDamageable.cs` | 전투 공용 타입 (타워 담당도 사용). DamageType = 물리/마법, AttackKind = 근접/원거리, StatusEffect = 독/둔화/기절/화상 |
| `Scripts/Enemy/Editor/EnemyBuilder.cs` | Tools > TD > Enemy > Build Placeholder Enemies |
| `Data/Enemy/*.asset` | 스탯 SO: Grunt, Runner, Armored, Goblin, Wolf, Spider, Gargoyle, ShadowArcher, Veznan, VeznanDemon |
| `Prefabs/Enemy/Enemy_*.prefab` | 임시 그래픽 프리팹 (트리거 Collider2D + Kinematic Rigidbody2D 포함) |

## 게임 쪽 연동 (이벤트)

```csharp
Enemy.Killed      += enemy => gold  += enemy.Data.GoldReward;
Enemy.ReachedGoal += enemy => lives -= enemy.Data.LifeDamage;
WaveSpawner.WaveStarted     += waveNumber => { };
WaveSpawner.BossSpawned     += boss => { };   // 마지막 웨이브 뒤 보스 등장 (보스 체력바 UI 등)
Enemy.FormChanged           += enemy => { };  // 보스 변신 (연출용)
WaveSpawner.AllWavesCleared += () => Win();
```

## 타워 쪽에서 쓰는 것

| 멤버 | 용도 |
|---|---|
| `Enemy.Active`, `Enemy.FindInRadius(c, r, list)` | 살아있는 적 / 범위(2.5D 타원) 내 적 |
| `Enemy.FindMostAdvanced(후보들)` | 후보 중 **가장 앞선 적** (출구까지 남은 거리 최소). 타겟 목록에서 고를 때 사용 |
| `enemy.RemainingDistance` | 출구까지 남은 거리 — **작을수록 앞선 적** (추월돼도, 경로가 달라도 정확) |
| `enemy.Progress` | 지나온 거리 |
| `enemy.TakeDamage(amount)` / `(amount, DamageType)` / `(amount, DamageType, AttackKind)` | 피해 (물리 → 방어력, 마법 → 마법 저항). 종류 생략 = 물리, 방식 생략 = 원거리. **마법은 `DamageType.Magic`, 근접은 `AttackKind.Melee`를 넘길 것**. 맞으면 true, 회피·사망 상태면 false |
| `Enemy.Evaded` | 근접 공격 회피 이벤트 ("Miss" 표시용) |
| `enemy.IsFlying` | 공중 유닛 (병영이 붙잡을 수 없음) |
| `enemy.IsImmuneTo(StatusEffect.Poison)` | 상태이상 면역 여부. 독화살 등 **효과를 걸기 전에 확인** (거미 = 독 면역) |
| `enemy.TryEngage(this)` / `enemy.Release(this)` | 병영 1:1 붙잡기 / 놓기 |
| `enemy.IsBlocked`, `enemy.Blocker` | 붙잡힌 상태 / 붙잡은 대상 |
| `enemy.Data` | 체력·속도·방어력·마법저항·보상·공중 여부 등 (`EnemyData`) |

- **비행 적(`IsFlying`) 규칙** — 타워 쪽에서 지켜야 함
  - 병영/영웅: 병사가 적을 찾을 때 `IsFlying`인 적은 **건너뛸 것** (`TryEngage`는 어차피 false. 안 건너뛰면 병사가 비행 적만 계속 노리다 지상 적을 놓침)
  - 포병: 비행 적을 **조준하지 말고**, 폭발 범위 피해도 비행 적에게 **주지 말 것**
  - 아처·마법사: 공격 가능
- 적은 자신을 붙잡은 대상이 `IDamageable`이면 반격한다 (병사가 구현)
- **원거리 적(그림자 궁수 등)의 공격 대상이 되려면** 병사·영웅이 `IDamageable`(`IsAlive`, `Position`, `TakeDamage`)을 구현하고 등록해야 함
  ```csharp
  private void OnEnable() { EnemyTargets.Register(this); }
  private void OnDisable() { EnemyTargets.Unregister(this); }
  ```
  원거리 적은 사거리 안에 등록된 대상이 있으면 멈춰서 쏘고(`enemy.IsShooting`), 붙잡히면 근접전으로 바뀐다
- 프리팹에 트리거 충돌체가 있어서 타워 사거리 트리거(`OnTriggerEnter2D`)로 감지 가능

## 이동 방식 — 차선(lane)

부모로 묶지 않고 적마다 `Lane`(-1~1)을 갖는다. `위치 = route.GetPosition(Progress, Lane, 반지름)`
→ 코너에서도 길 폭 안에서 같이 돈다. 붙잡혔다 풀려나면 원래 차선으로 걸어서 합류한 뒤 다시 진행.

## 보스 (`EnemyData` Boss 항목)

| 필드 | 의미 |
|---|---|
| `isBoss` | 보스 등급 (`enemy.IsBoss`) |
| `meleeTargetCount`, `meleeAreaRadius` | 범위 근접 공격: 붙잡은 병사 + 반경 안 병사, 최대 N명 (베즈난 8명, 0.9칸) |
| `nextForm` | 죽을 피해를 받으면 이 데이터로 변신, 체력 가득 (베즈난 → 베즈난 (악마)) |
| `killAllOnDeath` | 죽으면 전장의 다른 적도 모두 죽음 (최후의 저항, 처치로 처리되어 골드 지급) |
| `formTint` | 변신했을 때 몸 색 |

보스 등장: `WaveSpawner`의 Boss 항목(또는 `SetBoss(prefab, spawnId)`)에 넣으면 **마지막 웨이브의 적이 다 사라진 뒤** 등장하고,
보스까지 사라져야 `AllWavesCleared`가 나온다.

## 입구 표 (`WaveSpawner.SpawnPoints`)

| 입구 ID | 경로 (Map01) |
|---|---|
| `Top` | `Path_TopMiddle`, `Path_TopLoop` (갈림길, 랜덤) |
| `Right` | `Path_Right` |

## 웨이브 그룹 (`WaveSpawner.SpawnGroup`)

| 필드 | 의미 |
|---|---|
| `SpawnId` | 입구 ID (`Top`, `Right`). `WaveSpawner`의 **SpawnPoints 표**에서 그 입구의 경로 중 적마다 랜덤 |
| `Routes` | 경로 이름 직접 지정 (비우면 `SpawnId`의 경로 사용) |
| `Count`, `Interval`, `StartDelay` | 총 수 / 무리 사이 간격 / 시작 지연 |
| `PackSize` | 한 번에 같이 나오는 수 |
| `LaneSpread` | 차선 폭 사용 비율 (0 = 한 줄, 1 = 길 폭 전체) |
| `PackDepth` | 무리 안 앞뒤 흩어짐 |
