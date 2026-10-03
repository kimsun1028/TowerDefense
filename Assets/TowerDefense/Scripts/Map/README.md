# Map (맵 모듈)

담당: 이수 · 게임 코드(타워·적·GameManager)에 **의존하지 않는다** — `TD.Map` asmdef가 컴파일 단계에서 강제.

## 파일 위치 (`Assets/TowerDefense/` 아래)

| 위치 | 내용 |
|---|---|
| `Scripts/Map/` (TD.Map) | `TDMap`, `MapRouteData`, `EnemyPath`, `Waypoint`, `TowerSlot`, `IRallyPointUser`, `RallyPointTool`, `CameraFit` |
| `Scripts/Map/Editor/` (TD.Map.Editor) | Map Editor 창, 경로/슬롯/TDMap 인스펙터, 맵 검증, Map01 빌더 |
| `Art/Map/` | 길 그림(= 길 판정 마스크), 타워 자리 패드, 임시 스프라이트, 선 머티리얼 |
| `Data/Map/Map01_Routes.asset` | 경로 데이터 SO |
| `Prefabs/Map/Map01.prefab` | 맵 본체 |
| `Scenes/Map01_View.unity` | 맵만 띄워보는 확인용 씬 |

## 쓰는 법

1. `Prefabs/Map/Map01.prefab`을 씬의 **(0, 0, 0)**에 넣는다.
2. 메인 카메라에 `CameraFit` (Fit To Map) → 화면 비율에 맞춰 맵을 꽉 채운다.
3. `Settings/Renderer2D` → Transparency Sort Mode = **Custom Axis (0, 1, 0)** (아래쪽 유닛이 앞에 그려짐)

## 경로 데이터 — `MapRouteData` (ScriptableObject)

```csharp
[SerializeField] private MapRouteData routes;   // Data/Map/Map01_Routes.asset (또는 TDMap.Instance.RouteData)

MapRouteData.Route route = routes.GetRoute("Path_Right");
route.Waypoints                    // 원본 웨이포인트 (0번 = 스폰, 마지막 = 출구)
route.Points / route.Length        // 등간격(0.25) 포인트 / 전체 길이
route.GetPosition(d)               // 시작점에서 d만큼 간 중앙선 위치
route.GetPosition(d, lane, radius) // 차선 위치. lane -1(오른쪽)~0~1(왼쪽), 길 밖으로 안 나감
route.GetDirection(d)              // 진행 방향
route.GetRemainingDistance(d)      // 출구까지 남은 거리 → 경로 길이가 달라도 "누가 더 앞섰나" 비교 가능
```

| 경로 | 입구 | 노드 | 길이 |
|---|---|---|---|
| `Path_TopMiddle` | 위 | 34 | 15.95 |
| `Path_TopLoop` | 위 | 41 | 19.11 |
| `Path_Right` | 오른쪽 | 50 | 25.20 |

- 길 폭은 굽기 때 길 그림에서 측정 (최대 0.6, 앞뒤 2칸 최솟값). 전 구간 14,735곳 검사 → 차선이 길 밖으로 나가는 곳 0
- 경로를 고치면 **TDMap 인스펙터 / Map Editor → "경로 SO로 굽기"** (같은 에셋을 덮어써서 참조 유지)

## 맵 API — `TDMap.Instance`

| 멤버 | 용도 |
|---|---|
| `RouteData`, `GetPath(name)` | 구운 경로 SO / 편집용 경로 |
| `Slots`, `Goal`, `ViewRect` | 타워 자리 목록 / 지키는 곳 / 카메라 영역 |
| `IsOnRoad(p)`, `OverlapsRoad(c, r)` | 길 판정 |
| `FindClosestPointOnRoad(p)` | 가장 가까운 길 중앙선 지점 |
| `TryGetRallyPoint(origin, desired, range, out p)` | 범위 안 길 위에서 desired에 가장 가까운 점 |

## 타워 자리 — `TowerSlot` (= BuildSpot)

맵은 자리와 집결지만 제공한다. 클릭 · 건설 · 골드는 타워/게임 쪽 담당.

```csharp
if (hit.TryGetComponent(out TowerSlot slot)) { OpenBuildMenu(slot); }   // 슬롯에 Collider2D 있음

Tower tower = Instantiate(prefab, slot.transform.position, Quaternion.identity, slot.transform);
slot.SetOccupant(tower);   // 번호 표지판 숨김
slot.ClearOccupant();      // 판매 후
```

- `IsOccupied`, `Occupant`, `SlotNumber`
- `RallyPointWorld`, `RallyRange` : 병영 기본 집결지 / 이동 가능 거리

## 병영 · 영웅 집결지 — `IRallyPointUser` + `RallyPointTool`

```csharp
public class BarracksTower : Tower, IRallyPointUser {
    public float RallyRange => slot.RallyRange;
    public Vector2 RallyPoint { get; private set; }
    public void SetRallyPoint(Vector2 point) { RallyPoint = point; /* 병사 이동 */ }
}

RallyPointTool.Instance.Begin(barracks, barracks.transform.position);         // "집결지 설정" 버튼
if (RallyPointTool.Instance.IsActive) { RallyPointTool.Instance.HandleClick(worldPosition); } // 좌클릭 전달
```

범위 밖·길 밖을 찍어도 맵이 길 위·범위 안으로 보정해서 `SetRallyPoint`를 호출한다.
씬에 `RallyPointTool` 하나 필요 (`LineMaterial` = `Art/Map/Road.mat`).

## 맵 편집 (맵 담당)

- **Tools > TD > Map Editor** : 타워 자리 놓기, 경로 그리기, 집결지 일괄 계산, 경로 SO 굽기, 맵 검증
  - `Map01.prefab`을 더블클릭(프리팹 모드)한 상태에서 쓰면 프리팹이 바로 수정된다
- EnemyPath 선택 : 노드 드래그 / Shift+클릭 추가 / Ctrl(Cmd)+클릭 삭제
- TowerSlot 선택 : 주황 원(집결지)과 범위 원 드래그
- **Tools > TD > Map > Build Map01** : 참고 좌표로 프리팹을 처음부터 다시 생성 (**수동 편집 내용 덮어씀**)
