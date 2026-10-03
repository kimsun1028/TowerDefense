## 담당 Task

맵 + 적 (Map / Enemy) 모듈

- 브랜치 : `feature/map` (맵) → `feature/enemy` (적, map 위에 쌓음) — PR은 map 먼저
- 상세 사용법 : `Assets/TowerDefense/Scripts/Map/README.md`, `Assets/TowerDefense/Scripts/Enemy/README.md`

## 구조

```
Map (TD.Map asmdef — 게임 코드에 의존 X)
├── TDMap            맵 질의 싱글톤 (길 판정, 집결지 계산, 경로/슬롯 목록)
├── MapRouteData     경로 SO (웨이포인트, 등간격 포인트, 길 폭, 위치/방향/남은 거리 조회)
├── EnemyPath        편집용 경로 (Waypoint 리스트) → "경로 SO로 굽기"
├── TowerSlot        타워 자리 (= BuildSpot) + 병영 기본 집결지
├── IRallyPointUser  병영/영웅이 구현 → RallyPointTool로 집결지 설정
└── CameraFit        화면 비율에 맞춰 맵을 꽉 채움

Enemy (TD.Enemy asmdef — Map만 참조)
├── Enemy            경로 거리 + 차선 이동, 붙잡힘/반격, 이벤트(Killed, ReachedGoal)
├── EnemyData (SO)   체력, 속도, 방어력, 마법저항, 공중 여부, 보상, 반격
├── WaveSpawner      웨이브/그룹 스폰, 경로 랜덤 선택, 무리 퍼짐
└── DamageType / IDamageable   전투 공용 타입
```

## Todo

### 0. 사전 협의
- [ ] 타워 담당과 연동 확인: `TowerSlot` 건설 흐름, `IRallyPointUser`, `DamageType`/`IDamageable` 공용 사용
- [ ] 타워 브랜치의 임시 `Scripts/Enemy/Enemy.cs` → `feature/enemy`의 `Enemy`로 교체 (같은 경로라 머지 시 `Enemy.cs`, `Enemy.cs.meta`, `Scripts/Enemy.meta` 3개 충돌 → 우리 버전 선택)
- [ ] 골드/목숨(GameManager) 담당 확인 → `Enemy.Killed`, `Enemy.ReachedGoal`, `WaveSpawner.AllWavesCleared` 구독

### 1. 맵
- [x] Map01 레이아웃 (경로 3개, 타워 자리 16곳, 입구 2 / 출구 1)
- [x] 길 판정 (길 그림 마스크), 맵 검증 툴, Map Editor 창
- [x] 경로 SO 굽기 (`MapRouteData`)
- [x] 병영 집결지 계산 + `RallyPointTool`
- [ ] 입구(스폰 지점) 데이터: 입구별 경로 목록
- [ ] 웨이브별 경로 켜기/끄기 (다리 개념)
- [ ] 지키는 곳 포인트 오브젝트 + 도착 이벤트
- [ ] 타워 자리 2개 추가 (위치 확인 필요)
- [ ] 맵 스케일 조정 (에셋 기준 1칸 = 40px)
- [ ] 배경 에셋 적용 (플럼 동방 컨셉) + 길 마스크 재생성

### 2. 적
- [x] `EnemyData` SO (체력, 속도, 방어력, 마법저항, 공중 여부, 보상, 반격)
- [x] 경로 거리 + 차선 이동 (무리로 퍼져도 길 밖으로 안 나감)
- [x] 진행도 (`Progress`, `RemainingDistance`) — 추월/경로 길이 차이 대응
- [x] 병영 1:1 붙잡기 (`TryEngage`/`Release`), 붙잡혔을 때 반격, 교전 겹침 방지
- [x] `WaveSpawner` (그룹, 경로 랜덤, 무리 퍼짐, 이벤트)
- [ ] 적 종류 추가 (단체 공격, 보스)
- [ ] 캐릭터 에셋 적용 (Tiny RPG Soldier & Orc)
