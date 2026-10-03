// AI-assisted code start
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 입구 옆 "다음 웨이브" 버튼 (킹덤러시의 해골 버튼 자리, 우리는 블랙홀 배지).
/// 다음 웨이브를 기다리는 동안, 그 웨이브에 적이 나오는 입구(SpawnPoint.WaveIcon 위치)에만 뜬다.
/// 누르면 바로 다음 웨이브를 시작한다. 바깥 고리는 저절로 시작될 때까지 남은 시간.
/// 버튼 원본(template)을 입구 수만큼 복제해서 쓴다.
/// </summary>
public class WaveCallButtons : MonoBehaviour {
    // 1. 상수
    private const float PULSE_SPEED = 5f;
    private const float PULSE_AMOUNT = 0.08f;

    // 2. 필드
    [SerializeField] private RectTransform template;
    [SerializeField] private WaveSpawner spawner;

    private readonly List<(SpawnPoint spawn, RectTransform button, Image ring)> buttons = new();
    private RectTransform area;
    private Camera cam;

    // 4. 메서드 (private)
    private void Start() {
        area = (RectTransform)transform;
        cam = Camera.main;
        template.gameObject.SetActive(false);
        if (TDMap.Instance == null) {
            return;
        }
        foreach (SpawnPoint spawn in TDMap.Instance.Spawns) {
            RectTransform button = Instantiate(template, area);
            button.name = $"WaveCall_{spawn.SpawnId}";
            button.GetComponent<Button>().onClick.AddListener(CallNextWave);
            Image ring = button.Find("Ring").GetComponent<Image>();
            buttons.Add((spawn, button, ring));
        }
    }

    private void Update() {
        if (spawner == null) {
            spawner = FindAnyObjectByType<WaveSpawner>();
        }
        bool waiting = spawner != null && spawner.WaitingForNextWave && spawner.CurrentWave < spawner.TotalWaves;
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * PULSE_SPEED) * PULSE_AMOUNT;
        float progress = 1f;
        if (waiting && spawner.NextWaveTime > 0f && spawner.TimeBetweenWaves > 0f) {
            progress = 1f - Mathf.Clamp01((spawner.NextWaveTime - Time.time) / spawner.TimeBetweenWaves);
        }
        foreach ((SpawnPoint spawn, RectTransform button, Image ring) in buttons) {
            bool visible = waiting && spawn != null && spawner.UsesSpawn(spawner.CurrentWave, spawn.SpawnId);
            if (button.gameObject.activeSelf != visible) {
                button.gameObject.SetActive(visible);
            }
            if (!visible) {
                continue;
            }
            if (cam == null) {
                cam = Camera.main;
            }
            Vector2 screen = cam.WorldToScreenPoint(spawn.WaveIconPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, null, out Vector2 local);
            button.anchoredPosition = local;
            button.localScale = Vector3.one * pulse;
            ring.fillAmount = progress;
        }
    }

    private void CallNextWave() {
        if (spawner != null) {
            spawner.StartNextWave();
        }
    }
}
// AI-assisted code end
