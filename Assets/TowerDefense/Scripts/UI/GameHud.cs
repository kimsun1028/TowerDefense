// AI-assisted code start
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 위 HUD: 왼쪽 위 목숨·골드, 웨이브(WAVE 3/15), 오른쪽 위 일시정지·배속 버튼.
/// 목숨·골드는 게임 쪽(게임 매니저)이 SetLives / SetGold로 넣어준다.
/// 웨이브는 씬의 WaveSpawner를 직접 읽는다.
/// 일시정지·배속은 SpeedRequested 이벤트로 알린다 (구독자가 없으면 Time.timeScale을 직접 바꾼다).
/// </summary>
public class GameHud : MonoBehaviour {
    // 1. 상수
    private const float SPEED_NORMAL = 1f;
    private const float SPEED_FAST = 2f;
    private const string WAVE_FORMAT = "WAVE {0}/{1}";

    // 2. 필드
    /// <summary>원하는 시간 배율 (0 = 일시정지, 1, 2).</summary>
    public event Action<float> SpeedRequested;

    [SerializeField] private TMP_Text livesText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Image pauseIcon;
    [SerializeField] private Sprite pauseSprite;
    [SerializeField] private Sprite playSprite;
    [SerializeField] private Button speedButton;
    [SerializeField] private Image speedIcon;
    [SerializeField] private Sprite normalSpeedSprite;
    [SerializeField] private Sprite fastSpeedSprite;
    [SerializeField] private WaveSpawner spawner;

    private bool paused;
    private float speed = SPEED_NORMAL;
    private int shownWave = -1;
    private int shownTotal = -1;

    private static GameHud instance;

    /// <summary>씬의 GameHud. 플레이 중 스크립트를 다시 컴파일해 static이 비면 다시 찾는다.</summary>
    public static GameHud Instance => instance ? instance : instance = FindAnyObjectByType<GameHud>();
    public bool IsPaused => paused;
    public float Speed => speed;

    // 4. 메서드 (public)
    public void SetLives(int lives) {
        livesText.text = lives.ToString();
    }

    public void SetGold(int gold) {
        goldText.text = gold.ToString();
    }

    // 4. 메서드 (private)
    private void Awake() {
        instance = this;
        pauseButton.onClick.AddListener(TogglePause);
        speedButton.onClick.AddListener(ToggleSpeed);
        UpdateIcons();
    }

    private void Update() {
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

    private void TogglePause() {
        paused = !paused;
        Apply();
    }

    private void ToggleSpeed() {
        speed = Mathf.Approximately(speed, SPEED_NORMAL) ? SPEED_FAST : SPEED_NORMAL;
        paused = false;
        Apply();
    }

    private void Apply() {
        float scale = paused ? 0f : speed;
        if (SpeedRequested != null) {
            SpeedRequested.Invoke(scale);
        } else {
            Time.timeScale = scale;
        }
        UpdateIcons();
    }

    private void UpdateIcons() {
        pauseIcon.sprite = paused ? playSprite : pauseSprite;
        speedIcon.sprite = Mathf.Approximately(speed, SPEED_FAST) ? fastSpeedSprite : normalSpeedSprite;
    }
}
// AI-assisted code end
