// AI-assisted code start
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 아래 작은 정보 바 (킹덤러시처럼 한 줄). 선택한 대상(IInfoSource: 적, 타워)의 초상화·이름과
/// 능력치를 "아이콘 + 값"으로 가로로 늘어놓는다. 체력이 있으면 맨 앞에 하트 칸.
/// 열려 있는 동안 매 프레임 갱신하고, 대상이 죽거나 사라지면 자동으로 닫힌다.
/// 아이콘은 정보 줄 이름(InfoRow.Label)으로 고른다. 아이콘이 없는 줄은 "이름 값"으로 보여준다.
/// </summary>
public class InfoPanel : MonoBehaviour {
    // 1. 상수
    private static readonly Color NO_PORTRAIT_COLOR = new(0.16f, 0.12f, 0.27f, 1f);

    /// <summary>정보 줄 이름 → 아이콘.</summary>
    [Serializable]
    public class StatIcon {
        // 2. 필드
        [SerializeField] private string label;
        [SerializeField] private Sprite icon;

        public string Label => label;
        public Sprite Icon => icon;

        // 3. 생성자
        public StatIcon(string rowLabel, Sprite rowIcon) {
            label = rowLabel;
            icon = rowIcon;
        }
    }

    // 2. 필드
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Image portrait;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image[] cellIcons;
    [SerializeField] private TMP_Text[] cellValues;
    [SerializeField] private Button closeButton;
    [SerializeField] private Sprite healthIcon;
    [SerializeField] private Sprite fallbackIcon;
    [SerializeField] private List<StatIcon> statIcons = new();

    private readonly List<InfoRow> rows = new();
    private IInfoSource current;

    private static InfoPanel instance;

    /// <summary>씬의 InfoPanel. 플레이 중 스크립트를 다시 컴파일해 static이 비면 다시 찾는다.</summary>
    public static InfoPanel Instance => instance ? instance : instance = FindAnyObjectByType<InfoPanel>();
    public IInfoSource Current => current;
    public bool IsOpen => current != null;

    // 4. 메서드 (public)
    /// <summary>대상 정보를 연다. null이면 닫는다.</summary>
    public void Show(IInfoSource source) {
        if (source == null || !source.IsInfoValid) {
            Hide();
            return;
        }
        current = source;
        panelRoot.SetActive(true);
        Refresh();
    }

    public void Hide() {
        current = null;
        if (panelRoot) {
            panelRoot.SetActive(false);
        }
    }

    // 4. 메서드 (private)
    private void Awake() {
        instance = this;
        if (closeButton) {
            closeButton.onClick.AddListener(Hide);
        }
        Hide();
    }

    private void Update() {
        if (current == null) {
            return;
        }
        if (!current.IsInfoValid) {
            Hide(); // 죽었거나 출구로 나감
            return;
        }
        Refresh();
    }

    private void Refresh() {
        nameText.text = current.InfoName;

        Sprite sprite = current.InfoPortrait;
        portrait.sprite = sprite;
        portrait.color = sprite ? Color.white : NO_PORTRAIT_COLOR;

        int cell = 0;
        if (current.HasInfoHealth) {
            float max = Mathf.Max(1f, current.InfoMaxHp);
            SetCell(cell++, healthIcon, $"{Mathf.CeilToInt(current.InfoHp)}/{Mathf.CeilToInt(max)}");
        }
        current.FillInfoRows(rows);
        foreach (InfoRow row in rows) {
            if (cell >= cellIcons.Length) {
                break;
            }
            Sprite icon = FindIcon(row.Label);
            SetCell(cell++, icon ? icon : fallbackIcon, icon ? row.Value : $"{row.Label} {row.Value}");
        }
        for (int i = cell; i < cellIcons.Length; i++) {
            cellIcons[i].transform.parent.gameObject.SetActive(false);
        }
    }

    private void SetCell(int index, Sprite icon, string value) {
        GameObject cellObject = cellIcons[index].transform.parent.gameObject;
        if (!cellObject.activeSelf) {
            cellObject.SetActive(true);
        }
        cellIcons[index].sprite = icon;
        if (cellValues[index].text != value) {
            cellValues[index].text = value;
        }
    }

    private Sprite FindIcon(string label) {
        foreach (StatIcon entry in statIcons) {
            if (entry.Label == label) {
                return entry.Icon;
            }
        }
        return null;
    }
}
// AI-assisted code end
