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
