// AI-assisted code start
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 타워 팀 TowerBuilder ↔ 우리 링 메뉴(RingMenu) 연결 — 숫자키 건설 단계용 임시 연결.
///   클릭 판정·건설은 TowerBuilder가 한다 (빈 자리 클릭 → 자리 선택, 숫자키 1~4 → Build).
///   여기서는 TowerBuilder가 고른 자리를 매 프레임 "읽기만" 해서, 고르면 그 자리에 링 메뉴(숫자키 표시)를 띄우고
///   풀리면(지었거나 다른 곳 클릭) 닫는다.
/// 타워↔UI 약속의 TowerBuilder.SlotSelected / SelectionCleared 이벤트가 생기면, Update의 읽기 부분을
/// 이벤트 구독으로 바꾸고 리플렉션을 지운다 (메뉴 내용·모양은 그대로).
/// UI 어셈블리(TD.UI)는 타워 코드를 참조할 수 없어서 이 연결은 asmdef 없는 폴더에 둔다.
/// 일시 정지 메뉴·결과 창이 열려 있는 동안(GameHud.IsBlockingGame)은 TowerBuilder를 꺼서 클릭·숫자키 건설을 막는다.
/// </summary>
public class TowerBuildMenuAdapter : MonoBehaviour {
    // 1. 상수
    private const string SELECTED_SLOT_FIELD = "selectedSlot";

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

    private readonly List<RingMenuOption> options = new();
    private FieldInfo selectedSlotField;
    private TowerSlot shownSlot;

    // 4. 메서드 (private)
    private void Awake() {
        if (builder == null) {
            builder = FindAnyObjectByType<TowerBuilder>();
        }
        selectedSlotField = typeof(TowerBuilder).GetField(SELECTED_SLOT_FIELD, BindingFlags.Instance | BindingFlags.NonPublic);
        if (selectedSlotField == null) {
            Debug.LogWarning("[TowerBuildMenuAdapter] TowerBuilder.selectedSlot이 없습니다. 이벤트(SlotSelected)로 바꿀 때가 됐는지 확인하세요.");
        }
    }

    private void Update() {
        if (builder == null || selectedSlotField == null) {
            return;
        }
        GameHud hud = GameHud.Instance;
        bool blocked = hud != null && hud.IsBlockingGame;
        if (builder.enabled == blocked) {
            builder.enabled = !blocked; // 메뉴가 열려 있으면 TowerBuilder의 클릭·숫자키 입력을 멈춘다
        }
        if (blocked) {
            if (shownSlot != null) {
                shownSlot = null; // 메뉴를 닫은 뒤 자리가 아직 골라져 있으면 링 메뉴를 다시 띄운다
                OnSelectionCleared();
            }
            return;
        }
        var selected = selectedSlotField.GetValue(builder) as TowerSlot;
        if (selected == shownSlot) {
            return;
        }
        shownSlot = selected;
        if (selected != null) {
            OnSlotSelected(selected);
        } else {
            OnSelectionCleared();
        }
    }

    // 약속의 SlotSelected(TowerSlot)에 해당
    private void OnSlotSelected(TowerSlot slot) {
        RingMenu ring = RingMenu.Instance;
        if (ring == null) {
            return;
        }
        options.Clear();
        foreach (TowerButton button in towerButtons) {
            // 지금은 숫자키로만 짓는다 (버튼 클릭은 TowerBuilder가 UI 위 클릭도 선택 해제로 처리해서 못 씀)
            options.Add(new RingMenuOption(button.Icon, $"{button.Label}  [{button.Shortcut}]", null, shortcut: button.Shortcut));
        }
        ring.Open(slot.transform.position, options);
    }

    // 약속의 SelectionCleared()에 해당
    private void OnSelectionCleared() {
        RingMenu.Instance?.Close();
    }
}
// AI-assisted code end
