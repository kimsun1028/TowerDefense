// AI-assisted code start
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타워 팀 TowerBuilder ↔ 우리 링 메뉴(RingMenu) 연결 (타워↔UI 약속).
///   SlotSelected(빈 자리) → 타워 종류 버튼: 가격(GetBuildCost), 골드 부족이면 흐리게(CanBuild), 누르면 Build(i)
///   TowerSelected(지은 타워) → 강화(위) / 판매(아래) / 집결지(오른쪽, 병영만)
///   SelectionCleared → 메뉴 닫기
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

    private readonly List<RingMenuOption> options = new();

    // 4. 메서드 (private)
    private void Awake() {
        if (builder == null) {
            builder = FindAnyObjectByType<TowerBuilder>();
        }
    }

    private void OnEnable() {
        if (builder == null) {
            return;
        }
        builder.SlotSelected += OnSlotSelected;
        builder.TowerSelected += OnTowerSelected;
        builder.SelectionCleared += OnSelectionCleared;
    }

    private void OnDisable() {
        if (builder == null) {
            return;
        }
        builder.SlotSelected -= OnSlotSelected;
        builder.TowerSelected -= OnTowerSelected;
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
    }

    private void OnSlotSelected(TowerSlot slot) {
        options.Clear();
        for (int i = 0; i < towerButtons.Count; i++) {
            int index = i;
            TowerButton button = towerButtons[i];
            options.Add(new RingMenuOption(button.Icon, button.Label, () => builder.Build(index),
                builder.GetBuildCost(index), () => builder.CanBuild(index), shortcut: button.Shortcut));
        }
        Open(slot.transform.position);
    }

    private void OnTowerSelected(Tower tower) {
        if (tower == null) {
            return;
        }
        options.Clear();
        if (tower.Level < tower.MaxLevel) {
            options.Add(new RingMenuOption(upgradeIcon, string.Format(UPGRADE_LABEL_FORMAT, tower.Level + 1),
                () => Upgrade(tower), tower.UpgradeCost, () => tower.CanUpgrade, RingPosition.Top));
        } else {
            options.Add(new RingMenuOption(null, MAX_LEVEL_LABEL, null, optionPosition: RingPosition.Top, isLocked: true));
        }
        if (tower.HasRallyPoint) {
            options.Add(new RingMenuOption(rallyIcon, RALLY_LABEL, tower.BeginRallyPoint, optionPosition: RingPosition.Right));
        }
        options.Add(new RingMenuOption(sellIcon, SELL_LABEL, tower.Sell, tower.SellPrice,
            optionPosition: RingPosition.Bottom, isSell: true));
        Open(tower.transform.position);
    }

    /// <summary>강화하면 바뀐 가격·레벨로 메뉴를 다시 띄운다.</summary>
    private void Upgrade(Tower tower) {
        tower.Upgrade();
        OnTowerSelected(tower);
    }

    private void OnSelectionCleared() {
        RingMenu.Instance?.Close();
    }

    private void Open(Vector2 position) {
        RingMenu ring = RingMenu.Instance;
        if (ring != null) {
            ring.Open(position, options);
        }
    }
}
// AI-assisted code end
