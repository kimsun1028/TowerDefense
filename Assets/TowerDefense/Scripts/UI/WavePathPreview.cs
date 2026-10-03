// AI-assisted code start
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 다음 웨이브 예상 경로: 다음 웨이브를 기다리는 동안, 그 웨이브의 적이 걸을 경로 위로 꺾쇠 화살표가 흘러간다.
/// 갈림길처럼 경로가 겹치는 구간은 화살표를 한 번만 그린다. 길 그림 위, 블랙홀·타워 자리 아래에 그려진다.
/// 일시정지 중에도 움직인다 (unscaledTime).
/// </summary>
public class WavePathPreview : MonoBehaviour {
    // 1. 상수
    private const float SPACING = 0.6f;           // 화살표 간격 (칸)
    private const float FLOW_SPEED = 1.1f;        // 흐르는 속도 (칸/초)
    private const float START_OFFSET = 0.7f;      // 블랙홀(반지름 약 0.6칸) 밖에서 시작
    private const float EDGE_FADE = 0.9f;         // 시작·끝에서 흐려지는 길이
    private const float FADE_IN_TIME = 0.4f;
    private const float DUPLICATE_SQR = 0.04f;    // 0.2칸 안에 이미 화살표가 있으면 건너뜀
    private const float WAVE_LENGTH = 3.2f;       // 밝기 물결
    private const float MIN_BRIGHTNESS = 0.65f;
    private const int SORTING_ORDER = -9;         // 길(-10) 위

    // 2. 필드
    [SerializeField] private Sprite arrowSprite;
    [SerializeField] private WaveSpawner spawner;

    private readonly List<SpriteRenderer> pool = new();
    private readonly List<MapRouteData.Route> routes = new();
    private readonly List<Vector2> placed = new();
    private Transform container;
    private float visibleTime;

    // 4. 메서드 (private)
    private void Awake() {
        container = new GameObject("WavePathArrows").transform; // 월드 오브젝트 (UI 캔버스 밖)
    }

    private void OnDestroy() {
        if (container) {
            Destroy(container.gameObject);
        }
    }

    private void Update() {
        if (spawner == null) {
            spawner = FindAnyObjectByType<WaveSpawner>();
        }
        bool waiting = spawner != null && spawner.WaitingForNextWave && spawner.CurrentWave < spawner.TotalWaves;
        int used = 0;
        if (waiting) {
            visibleTime += Time.unscaledDeltaTime;
            spawner.GetRoutes(spawner.CurrentWave, routes);
            used = PlaceArrows();
        } else {
            visibleTime = 0f;
        }
        for (int i = used; i < pool.Count; i++) {
            if (pool[i].enabled) {
                pool[i].enabled = false;
            }
        }
    }

    private int PlaceArrows() {
        placed.Clear();
        int used = 0;
        float fadeIn = Mathf.Clamp01(visibleTime / FADE_IN_TIME);
        float phase = Mathf.Repeat(Time.unscaledTime * FLOW_SPEED, SPACING);
        foreach (MapRouteData.Route route in routes) {
            for (float d = START_OFFSET + phase; d < route.Length; d += SPACING) {
                Vector2 position = route.GetPosition(d);
                if (IsPlaced(position)) {
                    continue; // 다른 경로와 겹치는 구간
                }
                placed.Add(position);
                SpriteRenderer arrow = GetArrow(used++);
                Vector2 direction = route.GetDirection(d);
                arrow.transform.SetPositionAndRotation(position,
                    Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));
                float edge = Mathf.Clamp01((d - START_OFFSET) / EDGE_FADE) * Mathf.Clamp01((route.Length - d) / EDGE_FADE);
                float brightness = Mathf.Lerp(MIN_BRIGHTNESS, 1f,
                    0.5f + 0.5f * Mathf.Sin((d - Time.unscaledTime * FLOW_SPEED * 2f) / WAVE_LENGTH * Mathf.PI * 2f));
                arrow.color = new Color(1f, 1f, 1f, edge * fadeIn * brightness);
            }
        }
        return used;
    }

    private bool IsPlaced(Vector2 position) {
        foreach (Vector2 other in placed) {
            if ((other - position).sqrMagnitude < DUPLICATE_SQR) {
                return true;
            }
        }
        return false;
    }

    private SpriteRenderer GetArrow(int index) {
        while (pool.Count <= index) {
            var arrowObject = new GameObject("PathArrow");
            arrowObject.transform.SetParent(container, false);
            SpriteRenderer renderer = arrowObject.AddComponent<SpriteRenderer>();
            renderer.sprite = arrowSprite;
            renderer.sortingOrder = SORTING_ORDER;
            pool.Add(renderer);
        }
        SpriteRenderer arrow = pool[index];
        arrow.enabled = true;
        return arrow;
    }
}
// AI-assisted code end
