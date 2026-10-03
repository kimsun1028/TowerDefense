// AI-assisted code start
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타워 팀 TowerBuilder ↔ 우리 링 메뉴(RingMenu) 연결 (타워↔UI 약속).
///   SlotSelected(빈 자리) → 타워 종류 버튼: 가격(GetBuildCost), 골드 부족이면 흐리게(CanBuild), 누르면 Build(i)
///   TowerSelected(지은 타워) → 강화(위) / 판매(아래) / 집결지(오른쪽, 병영만)
///   SelectionCleared → 메뉴 닫기. 단, 방금 누른 곳이 지은 타워의 몸통(자리 밖으로 솟은 그림)이면 그 타워 메뉴를 연다
/// 타워 메뉴의 링 중심은 타워 그림 가운데 (자리 바닥이 아니라).
/// 버튼에 마우스를 올리면 지었을 때(강화하면 다음 레벨) 사거리를 땅에 희미하게 보여준다 (RangePreview). 병영은 집결지 거리.
/// 클릭 판정·골드 계산은 타워 쪽이 하고, 여기서는 메뉴만 만든다.
/// 일시 정지 메뉴·결과 창이 열려 있는 동안(GameHud.IsBlockingGame)은 TowerBuilder를 멈춰 숫자키 건설도 막는다.
/// UI 어셈블리(TD.UI)는 타워 코드를 참조할 수 없어서 이 연결은 asmdef 없는 폴더에 둔다.
/// </summary>
public class TowerBuildMenuAdapter : MonoBehaviour {
    // 1. 상수
    private const string UPGRADE_LABEL_FORMAT = "강화 Lv{0}";
    private const string MAX_LEVEL_LABEL = "최대 강화";
    private const string SELL_LABEL = "판매";
    private const string RALLY_LABEL = "집결지 설정";
    private const int LEFT_MOUSE_BUTTON = 0;
    private const float HALF = 0.5f;
    // 타워에 그림(SpriteRenderer)이 없을 때 쓰는 클릭 영역 (자리 바닥 기준, 유닛)
    private const float FALLBACK_TOWER_WIDTH = 1f;
    private const float FALLBACK_TOWER_HEIGHT = 1.4f;
    private const string RANGE_INDICATOR_NAME = "RangeIndicator"; // Tower.ShowRange가 만드는 사거리 원 (몸통이 아님)

    /// <summary>TowerBuilder.towerPrefabs와 같은 순서의 버튼 (0 아처 / 1 마법사 / 2 포병 / 3 병영).</summary>
    [Serializable]
    public class TowerButton {
        // 2. 필드
        [SerializeField] private string label;
        [SerializeField] private Sprite icon;
        [SerializeField] private string shortcut;

        public string Label => label;
        public Sprite Icon => icon;
        public string Shortcut => shortcut;

        // 3. 생성자
        public TowerButton(string buttonLabel, Sprite buttonIcon, string buttonShortcut) {
            label = buttonLabel;
            icon = buttonIcon;
            shortcut = buttonShortcut;
        }
    }

    // 2. 필드
    [SerializeField] private TowerBuilder builder;
    [SerializeField] private List<TowerButton> towerButtons = new();
    [SerializeField] private Sprite upgradeIcon;
    [SerializeField] private Sprite sellIcon;
    [SerializeField] private Sprite rallyIcon;
    [Tooltip("TowerBuilder.towerPrefabs와 같은 순서. 버튼에 마우스를 올렸을 때 사거리를 읽는다")]
    [SerializeField] private Tower[] towerPrefabs;

    private readonly List<RingMenuOption> options = new();
    private RangePreview rangePreview;
    private Tower bodyClickedTower;   // 몸통 클릭으로 메뉴를 연 타워 (사거리 표시를 여기서 끈다)

    // 4. 메서드 (private)
    private void Awake() {
        if (builder == null) {
            builder = FindAnyObjectByType<TowerBuilder>();
        }
        rangePreview = RangePreview.Instance;
    }

    private void OnEnable() {
        if (builder == null) {
            return;
        }
        builder.SlotSelected += OnSlotSelected;
        builder.TowerSelected += OnBuilderTowerSelected;
        builder.SelectionCleared += OnSelectionCleared;
    }

    private void OnDisable() {
        if (builder == null) {
            return;
        }
        builder.SlotSelected -= OnSlotSelected;
        builder.TowerSelected -= OnBuilderTowerSelected;
        builder.SelectionCleared -= OnSelectionCleared;
    }

    private void Update() {
        if (builder == null) {
            return;
        }
        GameHud hud = GameHud.Instance;
        bool blocked = hud != null && hud.IsBlockingGame;
        if (builder.enabled == blocked) {
            builder.enabled = !blocked; // 메뉴가 열려 있으면 TowerBuilder의 클릭·숫자키 입력을 멈춘다
        }
        RingMenu ring = RingMenu.Instance;
        if (bodyClickedTower != null && (ring == null || !ring.IsOpen)) {
            HideBodyClickedRange(); // 링 메뉴가 닫히면 사거리 표시도 끈다 (TowerBuilder가 직접 연 경우와 같게)
        }
    }

    private void OnSlotSelected(TowerSlot slot) {
        HideBodyClickedRange();
        options.Clear();
        for (int i = 0; i < towerButtons.Count; i++) {
            int index = i;
            TowerButton button = towerButtons[i];
            options.Add(new RingMenuOption(button.Icon, button.Label, () => builder.Build(index),
                builder.GetBuildCost(index), () => builder.CanBuild(index), shortcut: button.Shortcut,
                hovered: isHovered => PreviewBuildRange(slot, index, isHovered)));
        }
        Open(slot.transform.position);
    }

    /// <summary>TowerBuilder가 타워를 골랐다 (자리 클릭). 사거리 표시는 TowerBuilder가 한다.</summary>
    private void OnBuilderTowerSelected(Tower tower) {
        HideBodyClickedRange();
        OnTowerSelected(tower);
    }

    private void OnTowerSelected(Tower tower) {
        if (tower == null) {
            return;
        }
        options.Clear();
        if (tower.Level < tower.MaxLevel) {
            options.Add(new RingMenuOption(upgradeIcon, string.Format(UPGRADE_LABEL_FORMAT, tower.Level + 1),
                () => Upgrade(tower), tower.UpgradeCost, () => tower.CanUpgrade, RingPosition.Top,
                hovered: isHovered => PreviewUpgradeRange(tower, isHovered)));
        } else {
            options.Add(new RingMenuOption(null, MAX_LEVEL_LABEL, null, optionPosition: RingPosition.Top, isLocked: true));
        }
        if (tower.HasRallyPoint) {
            options.Add(new RingMenuOption(rallyIcon, RALLY_LABEL, tower.BeginRallyPoint, optionPosition: RingPosition.Right));
        }
        options.Add(new RingMenuOption(sellIcon, SELL_LABEL, tower.Sell, tower.SellPrice,
            optionPosition: RingPosition.Bottom, isSell: true));
        Open(GetTowerBounds(tower).center);
    }

    /// <summary>강화하면 바뀐 가격·레벨로 메뉴를 다시 띄운다.</summary>
    private void Upgrade(Tower tower) {
        tower.Upgrade();
        OnTowerSelected(tower);
    }

    private void OnSelectionCleared() {
        if (TryGetClickedTower(out Tower tower)) {
            HideBodyClickedRange();
            bodyClickedTower = tower;
            tower.ShowRange(true); // 자리 클릭 때 TowerBuilder가 보여주는 사거리와 같은 표시
            OnTowerSelected(tower); // 타워 자리 밖으로 솟은 몸통을 눌렀다
            return;
        }
        RingMenu.Instance?.Close();
    }

    /// <summary>이번 프레임 마우스 클릭(건설 후 정리 등 클릭이 아닌 SelectionCleared는 제외)이 지은 타워 그림 위인가.</summary>
    private static bool TryGetClickedTower(out Tower clicked) {
        clicked = null;
        Camera cam = Camera.main;
        if (!Input.GetMouseButtonDown(LEFT_MOUSE_BUTTON) || cam == null) {
            return false;
        }
        Vector2 point = cam.ScreenToWorldPoint(Input.mousePosition);
        float bestY = float.MaxValue;
        foreach (Tower tower in FindObjectsByType<Tower>(FindObjectsSortMode.None)) {
            Bounds bounds = GetTowerBounds(tower);
            bounds.extents = new Vector3(bounds.extents.x, bounds.extents.y, float.MaxValue);
            if (bounds.Contains(point) && tower.transform.position.y < bestY) {
                bestY = tower.transform.position.y; // 겹치면 앞(화면 아래)에 있는 타워
                clicked = tower;
            }
        }
        return clicked != null;
    }

    /// <summary>타워 그림 영역 (병사·사거리 원 제외). 그림이 없으면 자리 위 기본 크기.</summary>
    private static Bounds GetTowerBounds(Tower tower) {
        bool found = false;
        Bounds bounds = default;
        foreach (SpriteRenderer renderer in tower.GetComponentsInChildren<SpriteRenderer>()) {
            if (renderer.sprite == null || renderer.name == RANGE_INDICATOR_NAME || renderer.GetComponentInParent<Soldier>() != null) {
                continue;
            }
            if (found) {
                bounds.Encapsulate(renderer.bounds);
            } else {
                bounds = renderer.bounds;
                found = true;
            }
        }
        if (!found) {
            Vector3 bottom = tower.transform.position;
            bounds = new Bounds(bottom + Vector3.up * (FALLBACK_TOWER_HEIGHT * HALF),
                new Vector3(FALLBACK_TOWER_WIDTH, FALLBACK_TOWER_HEIGHT, 0f));
        }
        return bounds;
    }

    /// <summary>건설 버튼에 마우스: 그 타워 Lv1 사거리 (병영은 집결지 거리).</summary>
    private void PreviewBuildRange(TowerSlot slot, int index, bool isHovered) {
        if (!isHovered || towerPrefabs == null || index >= towerPrefabs.Length || towerPrefabs[index] == null) {
            HideRange();
            return;
        }
        ShowRange(towerPrefabs[index].Data, 0, slot);
    }

    /// <summary>강화 버튼에 마우스: 다음 레벨 사거리.</summary>
    private void PreviewUpgradeRange(Tower tower, bool isHovered) {
        if (!isHovered || tower == null) {
            HideRange();
            return;
        }
        ShowRange(tower.Data, tower.Level, tower.GetComponentInParent<TowerSlot>());
    }

    /// <summary>levelIndex 0 = Lv1. 공격 타워는 사거리, 병영·영웅은 자리 기준 집결지 거리 (둘 다 2.5D 타원).</summary>
    private void ShowRange(TowerData data, int levelIndex, TowerSlot slot) {
        if (rangePreview == null || slot == null) {
            return;
        }
        Vector2 center = slot.transform.position;
        if (data is AttackTowerData attack && levelIndex < attack.levels.Length) {
            rangePreview.ShowEllipse(center, attack.levels[levelIndex].range);
        } else if (data is BarracksTowerData) {
            rangePreview.ShowEllipse(center, slot.RallyRange); // RallyPointTool과 같은 타원
        } else {
            rangePreview.Hide();
        }
    }

    private void HideBodyClickedRange() {
        if (bodyClickedTower != null) {
            bodyClickedTower.ShowRange(false);
        }
        bodyClickedTower = null;
    }

    private void HideRange() {
        if (rangePreview != null) {
            rangePreview.Hide();
        }
    }

    private void Open(Vector2 position) {
        RingMenu ring = RingMenu.Instance;
        if (ring != null) {
            ring.Open(position, options);
        }
    }
}
// AI-assisted code end
