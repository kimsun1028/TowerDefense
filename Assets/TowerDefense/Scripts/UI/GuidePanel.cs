// AI-assisted code start
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 게임 설명 창 (메인 화면 "?"): 여러 장을 한 장씩 넘겨 본다.
///   건너뛰기 → 바로 닫기 / 다음 → 다음 장, 마지막 장에서는 "알겠습니다" → 닫기 / Esc → 닫기
/// 장 내용(그림·설명)은 빌더(GuidePanelBuilder)가 만들고, 여기서는 넘기기만 한다.
/// </summary>
public class GuidePanel : MonoBehaviour {
    // 1. 상수
    private const string NEXT_TEXT = "다음";
    private const string DONE_TEXT = "알겠습니다";
    private const string PAGE_FORMAT = "{0} / {1}";

    // 2. 필드
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private GameObject[] pages;
    [SerializeField] private Button skipButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text nextText;
    [SerializeField] private TMP_Text pageText;

    private int current;

    public bool IsOpen => panelRoot.activeSelf;

    // 4. 메서드 (public)
    public void Open() {
        panelRoot.SetActive(true);
        ShowPage(0);
    }

    public void Close() {
        panelRoot.SetActive(false);
    }

    // 4. 메서드 (private)
    private void Awake() {
        skipButton.onClick.AddListener(Close);
        nextButton.onClick.AddListener(Next); // 처음에 닫혀 있는 건 빌더가 정한다 (꺼진 채로 Awake가 늦게 불려도 안 닫히게)
    }

    private void Update() {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) {
            Close();
        }
    }

    private void Next() {
        if (current >= pages.Length - 1) {
            Close();
        } else {
            ShowPage(current + 1);
        }
    }

    private void ShowPage(int index) {
        current = Mathf.Clamp(index, 0, pages.Length - 1);
        for (int i = 0; i < pages.Length; i++) {
            pages[i].SetActive(i == current);
        }
        bool last = current == pages.Length - 1;
        nextText.text = last ? DONE_TEXT : NEXT_TEXT;
        skipButton.gameObject.SetActive(!last);
        pageText.text = string.Format(PAGE_FORMAT, current + 1, pages.Length);
    }
}
// AI-assisted code end
