# UI (정보 바 모듈)

담당: 이수 · 적·타워를 클릭하면 화면 아래에 정보 바가 뜬다. `TD.UI` asmdef (TD.Map, TD.Enemy 참조).

## 파일

| 위치 | 내용 |
|---|---|
| `Scripts/UI/InfoPanel.cs` | 아래 정보 바: 초상화, 이름, 체력바, 정보 두 열, 설명, 닫기. 매 프레임 갱신, 대상이 죽으면 자동으로 닫힘 |
| `Scripts/UI/InfoSelector.cs` | 클릭 선택 (적 우선 → 타워 자리) + 발밑 선택 원 (2.5D 타원) |
| `Scripts/UI/Editor/InfoPanelBuilder.cs` | Tools > TD > UI > Build Info Panel → `Prefabs/UI/InfoPanel.prefab` |
| `Scripts/Enemy/IInfoSource.cs` | 정보 바에 나올 수 있는 대상의 약속 (적·타워 공용) |
| `Scripts/Enemy/EnemyInfo.cs` | 적의 정보 (EnemyData에서 자동 생성) |
| `Scripts/Enemy/StatRating.cs` | 수치 → 단계 글자 (방어/마저: 없음·낮음·보통·높음·매우 높음, 속도: 느림·보통·빠름) |

## 씬에 넣기

1. `Prefabs/UI/InfoPanel.prefab`을 씬에 넣는다
2. 씬에 **EventSystem**이 하나 있어야 한다 (`InputSystemUIInputModule` 사용)

## 적 정보 (자동)

| 항목 | 예 (암흑기사) |
|---|---|
| 체력 | 260 / 300 (체력바) |
| 근접 공격 | 15~25 (1초마다) / 보스는 ", 범위 최대 8명" |
| 원거리 공격 | 21~30 (1.4초마다, 사거리 2) — 있을 때만 |
| 방어력 / 마법 저항 | 높음 (80%) / 없음 |
| 속도 | 보통 |
| 라이프 | -1 |
| 특성 | 보스, 비행, 근접 회피 30%, 독 면역, 변신, 최후의 저항 — 있을 때만 |
| 설명 | `EnemyData`의 Description |

새 적은 `EnemyData`(설명, 초상화 포함)만 채우면 정보 바에도 그대로 나온다.

## 타워 쪽에서 할 것 (선택)

타워가 정보 바에 나오려면 타워 클래스가 `IInfoSource`를 구현하면 된다. 타워 자리(`TowerSlot`) 클릭 → `slot.Occupant`가 `IInfoSource`면 표시.

```csharp
public class AttackTower : Tower, IInfoSource {
    public bool IsInfoValid => this != null;
    public string InfoName => data.towerName;
    public Sprite InfoPortrait => null;
    public bool HasInfoHealth => false;
    public float InfoHp => 0f;
    public float InfoMaxHp => 0f;
    public string InfoDescription => "";
    public Vector2 InfoFootPosition => transform.position;

    public void FillInfoRows(List<InfoRow> rows) {
        rows.Clear();
        rows.Add(new InfoRow("레벨", $"{level} / {data.MaxLevel}"));
        rows.Add(new InfoRow("공격력", CurrentStats.damage.ToString()));
        rows.Add(new InfoRow("사거리", CurrentStats.range.ToString("0.#")));
    }
}
```

## 참고

- 글자는 Unity 기본 폰트(LegacyRuntime) + OS 한글 폰트 대체로 표시된다. 정식 폰트가 정해지면 `InfoPanelBuilder`의 폰트만 바꾸면 된다.
- 초상화가 없으면 회색 상자로 나온다 (`EnemyData.Portrait`에 넣으면 표시).

## 게임 UI (HUD · 웨이브 · 링 메뉴 · 메인 화면)

- HUD (`GameHud`): 목숨·골드, `WAVE n/15`, 배속 한 버튼(1배속 → 2배속 → 정지), 톱니/Esc 일시 정지 메뉴(계속하기 · 설정 · 메인 메뉴), 승리/패배 창(다시 하기 · 메인 메뉴)
- 웨이브: 쉬는 시간(15초)에만 입구 옆 다음 웨이브 버튼(누르면 바로 시작, 고리 = 남은 시간) + 길 위 예상 경로 화살표 (`WaveCallButtons`, `WavePathPreview`)
- 웨이브 끝 = 필드의 마지막 적이 죽거나 지키는 곳에 들어갔을 때 (`WaveSpawner`)
- 타워 링 메뉴 (`RingMenu`): `RingMenu.Instance.Open(위치, List<RingMenuOption>)` — 가격표, 골드 부족 시 흐리게, 잠금, 판매, 단축키 표시, 화면 끝에서 안쪽으로
- 정보 바: 한 줄(초상화 · 이름 탭 · 아이콘 + 값)
- 메인 화면 (`MainMenu`, `Scenes/MainMenu.unity`): 게임 시작 · 설정(준비 중) · 종료
- 폰트: 나눔고딕(SIL OFL 1.1) + 어두운 외곽선, TMP 기본 리소스 포함

### 게임 쪽 연결 (`Scripts/Integration`, asmdef 없음)
- `GameHudBinding`: `GameManager`의 `GoldChanged` / `LivesChanged` → HUD, `GameWon` / `GameLost` → 결과 창
- `TowerBuildMenuAdapter`: `TowerBuilder`가 고른 자리에 링 메뉴(숫자키 1~4 표시). 일시 정지·결과 창 중에는 `TowerBuilder`를 멈춘다. 타워 쪽 `SlotSelected` 등 이벤트가 생기면 구독으로 바꾼다

### 씬에 붙이는 법
- `Prefabs/UI/GameHud`, `Prefabs/UI/InfoPanel` + EventSystem(`InputSystemUIInputModule`)
- 같은 씬에 `GameManager`, `TowerBuilder`가 있으면 `GameHudBinding`, `TowerBuildMenuAdapter`를 붙이면 된다
