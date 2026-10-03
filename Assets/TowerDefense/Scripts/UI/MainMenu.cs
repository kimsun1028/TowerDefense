// AI-assisted code start
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 메인 화면: 게임 시작 / 설정 / 종료. 제목은 위아래로 천천히 떠 있다.
///   게임 시작 → gameSceneName 씬을 연다
///   설정 → SettingsRequested 이벤트 (설정 화면이 생기면 구독). 구독자가 없으면 "준비 중" 창
///   종료 → 에디터에서는 플레이 종료, 빌드에서는 게임 종료
/// </summary>
public class MainMenu : MonoBehaviour {
    // 1. 상수
    public const string SCENE_NAME = "MainMenu";
    private const float TITLE_FLOAT_HEIGHT = 8f;
    private const float TITLE_FLOAT_SPEED = 1.6f;

    // 2. 필드
    public event Action SettingsRequested;

    [SerializeField] private string gameSceneName = "Map01_Playtest";
    [SerializeField] private RectTransform title;
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject notReadyPanel;
    [SerializeField] private Button notReadyCloseButton;

    private Vector2 titleBasePosition;

    public string GameSceneName { get => gameSceneName; set => gameSceneName = value; }

    // 4. 메서드 (private)
    private void Awake() {
        Time.timeScale = 1f; // 게임 중 일시 정지 상태로 넘어와도 메뉴는 움직이게
        startButton.onClick.AddListener(StartGame);
        settingsButton.onClick.AddListener(OpenSettings);
        quitButton.onClick.AddListener(Quit);
        notReadyCloseButton.onClick.AddListener(() => notReadyPanel.SetActive(false));
        notReadyPanel.SetActive(false);
        titleBasePosition = title.anchoredPosition;
    }

    private void Update() {
        title.anchoredPosition = titleBasePosition
            + Vector2.up * (Mathf.Sin(Time.unscaledTime * TITLE_FLOAT_SPEED) * TITLE_FLOAT_HEIGHT);
    }

    private void StartGame() {
        SceneManager.LoadScene(gameSceneName);
    }

    private void OpenSettings() {
        if (SettingsRequested != null) {
            SettingsRequested.Invoke();
        } else {
            notReadyPanel.SetActive(true);
        }
    }

    private static void Quit() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
// AI-assisted code end
