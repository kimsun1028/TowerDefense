// AI-assisted code start
using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 화면 위 HUD: 왼쪽 위 목숨·골드, 웨이브(WAVE 3/15), 오른쪽 위 배속 버튼 하나 + 메뉴 버튼.
///   배속 버튼: 누를 때마다 1배속 → 2배속 → 정지 → 1배속 (아이콘이 지금 상태)
///   메뉴 버튼(톱니, Esc): 가운데 메뉴 (계속하기 / 설정 / 메인 메뉴). 열려 있는 동안 멈추고, 닫으면 원래 배속으로.
/// 목숨·골드는 게임 쪽(게임 매니저)이 SetLives / SetGold로 넣어준다. 웨이브는 씬의 WaveSpawner를 직접 읽는다.
/// 배속은 SpeedRequested 이벤트로 알린다 (구독자가 없으면 Time.timeScale을 직접 바꾼다).
/// 설정은 화면이 아직 없어서 이벤트(SettingsRequested)만 보낸다.
/// 메인 메뉴: MainMenuRequested 구독자가 없으면 메인 화면 씬(MainMenu)으로 직접 간다 (빌드 설정에 있을 때).
/// 승리/패배: ShowResult(won)로 결과 창 (다시 하기 = 이 씬 다시, 메인 메뉴). 게임 쪽(GameManager.GameWon/GameLost)이 부른다.
/// </summary>
public class GameHud : MonoBehaviour {
    // 1. 상수
    private const float SPEED_PAUSED = 0f;
    private const float SPEED_NORMAL = 1f;
    private const float SPEED_FAST = 2f;
    private const string WAVE_FORMAT = "WAVE {0}/{1}";
    private const string WIN_TEXT = "승리!";
    private const string LOSE_TEXT = "패배...";
    private static readonly Color WIN_COLOR = new(0.55f, 1f, 0.65f, 1f);
    private static readonly Color LOSE_COLOR = new(1f, 0.4f, 0.5f, 1f);

    // 2. 필드
    /// <summary>원하는 시간 배율 (0 = 정지, 1, 2).</summary>
    public event Action<float> SpeedRequested;
    public event Action SettingsRequested;
    public event Action MainMenuRequested;

    [SerializeField] private TMP_Text livesText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private Button speedButton;
    [SerializeField] private Image speedIcon;
    [SerializeField] private Sprite pausedSprite;
    [SerializeField] private Sprite normalSpeedSprite;
    [SerializeField] private Sprite fastSpeedSprite;
    [SerializeField] private Button menuButton;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitle;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button resultMainMenuButton;
    [SerializeField] private WaveSpawner spawner;

    private static GameHud instance;
    private float speed = SPEED_NORMAL;
    private int shownWave = -1;
    private int shownTotal = -1;

    /// <summary>씬의 GameHud. 플레이 중 스크립트를 다시 컴파일해 static이 비면 다시 찾는다.</summary>
    public static GameHud Instance => instance ? instance : instance = FindAnyObjectByType<GameHud>();
    public float Speed => speed;
    public bool IsMenuOpen => menuPanel.activeSelf;
    public bool IsResultShown => resultPanel.activeSelf;
    /// <summary>일시 정지 메뉴나 결과 창이 열려 있음 → 그 창 말고는 게임과 상호작용하면 안 됨 (건설·선택 등).</summary>
    public bool IsBlockingGame => IsMenuOpen || IsResultShown;

    // 4. 메서드 (public)
    public void SetLives(int lives) {
        livesText.text = lives.ToString();
    }

    public void SetGold(int gold) {
        goldText.text = gold.ToString();
    }

    public void OpenMenu() {
        RingMenu.Instance?.Close(); // 열려 있던 타워 메뉴는 닫는다
        menuPanel.SetActive(true);
        Request(SPEED_PAUSED);
    }

    public void CloseMenu() {
        menuPanel.SetActive(false);
        Request(speed);
    }

    /// <summary>승리/패배 창을 띄우고 게임을 멈춘다.</summary>
    public void ShowResult(bool won) {
        menuPanel.SetActive(false);
        RingMenu.Instance?.Close();
        resultTitle.text = won ? WIN_TEXT : LOSE_TEXT;
        resultTitle.color = won ? WIN_COLOR : LOSE_COLOR;
        resultPanel.SetActive(true);
        Request(SPEED_PAUSED);
    }

    // 4. 메서드 (private)
    private void Awake() {
        instance = this;
        speedButton.onClick.AddListener(CycleSpeed);
        menuButton.onClick.AddListener(OpenMenu);
        resumeButton.onClick.AddListener(CloseMenu);
        settingsButton.onClick.AddListener(() => SettingsRequested?.Invoke());
        mainMenuButton.onClick.AddListener(GoToMainMenu);
        retryButton.onClick.AddListener(Retry);
        resultMainMenuButton.onClick.AddListener(GoToMainMenu);
        menuPanel.SetActive(false);
        resultPanel.SetActive(false);
        UpdateSpeedIcon();
    }

    private void Update() {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && !IsResultShown) {
            if (IsMenuOpen) {
                CloseMenu();
            } else {
                OpenMenu();
            }
        }
        UpdateWaveText();
    }

    private void GoToMainMenu() {
        if (MainMenuRequested != null) {
            MainMenuRequested.Invoke();
            return;
        }
        Time.timeScale = SPEED_NORMAL;
        if (Application.CanStreamedLevelBeLoaded(MainMenu.SCENE_NAME)) {
            SceneManager.LoadScene(MainMenu.SCENE_NAME);
        } else {
            Retry(); // 메인 화면 씬이 빌드 설정에 없으면 이 씬을 다시 시작
        }
    }

    private void Retry() {
        Time.timeScale = SPEED_NORMAL;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void UpdateWaveText() {
        if (spawner == null) {
            spawner = FindAnyObjectByType<WaveSpawner>();
            if (spawner == null) {
                return;
            }
        }
        if (spawner.CurrentWave != shownWave || spawner.TotalWaves != shownTotal) {
            shownWave = spawner.CurrentWave;
            shownTotal = spawner.TotalWaves;
            waveText.text = string.Format(WAVE_FORMAT, shownWave, shownTotal);
        }
    }

    /// <summary>1배속 → 2배속 → 정지 → 1배속.</summary>
    private void CycleSpeed() {
        speed = speed switch {
            SPEED_NORMAL => SPEED_FAST,
            SPEED_FAST => SPEED_PAUSED,
            _ => SPEED_NORMAL,
        };
        Request(speed);
        UpdateSpeedIcon();
    }

    private void Request(float scale) {
        if (SpeedRequested != null) {
            SpeedRequested.Invoke(scale);
        } else {
            Time.timeScale = scale;
        }
    }

    private void UpdateSpeedIcon() {
        speedIcon.sprite = speed switch {
            SPEED_PAUSED => pausedSprite,
            SPEED_FAST => fastSpeedSprite,
            _ => normalSpeedSprite,
        };
    }
}
// AI-assisted code end
