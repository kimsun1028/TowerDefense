// AI-assisted code start
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 웨이브 단위로 적을 스폰한다. 경로는 맵의 MapRouteData에서 이름으로 고른다.
///
/// 그룹마다 Routes에 경로 이름을 여러 개 넣으면 적마다 그중 하나를 랜덤으로 고른다
/// (예: 위 입구 = Path_TopMiddle, Path_TopLoop). 웨이브마다 다른 경로 목록을 줄 수 있다.
/// PackSize / LaneSpread로 한 줄이 아니라 무리로 퍼져 나오게 한다.
/// 게임 연동: WaveStarted, AllWavesCleared 이벤트.
/// </summary>
public class WaveSpawner : MonoBehaviour {
    /// <summary>한 종류의 적을 같은 경로 후보로 내보내는 묶음.</summary>
    [Serializable]
    public class SpawnGroup {
        // 1. 상수
        private const float DEFAULT_INTERVAL = 0.8f;
        private const float DEFAULT_LANE_SPREAD = 0.7f;
        private const float DEFAULT_PACK_DEPTH = 0.4f;

        // 2. 필드
        [SerializeField] private Enemy enemyPrefab;
        [Tooltip("이 그룹이 쓸 경로 이름들. 적마다 랜덤으로 하나")]
        [SerializeField] private List<string> routes = new();
        [SerializeField] private int count = 5;
        [Tooltip("무리(pack) 사이 간격 (초)")]
        [SerializeField] private float interval = DEFAULT_INTERVAL;
        [SerializeField] private float startDelay = 0f;

        [Header("무리 퍼짐")]
        [Tooltip("한 번에 같이 나오는 수")]
        [Min(1)] [SerializeField] private int packSize = 1;
        [Tooltip("차선을 얼마나 넓게 쓸지 (0 = 중앙 한 줄, 1 = 길 폭 전체)")]
        [Range(0, 1)] [SerializeField] private float laneSpread = DEFAULT_LANE_SPREAD;
        [Tooltip("같은 무리 안에서 앞뒤로 흩어지는 거리")]
        [SerializeField] private float packDepth = DEFAULT_PACK_DEPTH;

        public Enemy EnemyPrefab => enemyPrefab;
        public IReadOnlyList<string> Routes => routes;
        public int Count => count;
        public float Interval => interval;
        public float StartDelay => startDelay;
        public int PackSize => packSize;
        public float LaneSpread => laneSpread;
        public float PackDepth => packDepth;

        // 3. 생성자
        /// <summary>직렬화용 기본 생성자.</summary>
        public SpawnGroup() {
        }

        public SpawnGroup(Enemy prefab, List<string> routeNames, int spawnCount, float spawnInterval,
                          float delay = 0f, int pack = 1, float spread = DEFAULT_LANE_SPREAD, float depth = DEFAULT_PACK_DEPTH) {
            enemyPrefab = prefab;
            routes = routeNames;
            count = spawnCount;
            interval = spawnInterval;
            startDelay = delay;
            packSize = Mathf.Max(1, pack);
            laneSpread = Mathf.Clamp01(spread);
            packDepth = depth;
        }
    }

    /// <summary>동시에 진행되는 그룹들의 묶음.</summary>
    [Serializable]
    public class Wave {
        // 2. 필드
        [SerializeField] private List<SpawnGroup> groups = new();

        public IReadOnlyList<SpawnGroup> Groups => groups;

        // 3. 생성자
        /// <summary>직렬화용 기본 생성자.</summary>
        public Wave() {
        }

        public Wave(List<SpawnGroup> waveGroups) {
            groups = waveGroups;
        }
    }

    // 1. 상수
    private const float DEFAULT_TIME_BETWEEN_WAVES = 5f;
    private const float LANE_JITTER = 0.1f;
    private const float PACK_SLOT_CENTER = 0.5f;

    // 2. 필드
    /// <summary>웨이브 시작 (웨이브 번호, 1부터)</summary>
    public static event Action<int> WaveStarted;
    /// <summary>마지막 웨이브의 적이 모두 사라짐</summary>
    public static event Action AllWavesCleared;

    [SerializeField] private MapRouteData routeData;
    [SerializeField] private List<Wave> waves = new();
    [SerializeField] private float timeBetweenWaves = DEFAULT_TIME_BETWEEN_WAVES;

    private int aliveGroups;

    public MapRouteData RouteData { get => routeData; set => routeData = value; }
    public IReadOnlyList<Wave> Waves => waves;
    public int CurrentWave { get; private set; }
    public int TotalWaves => waves.Count;
    public bool WaitingForNextWave { get; private set; } = true;

    // 4. 메서드 (public)
    /// <summary>웨이브 목록을 통째로 바꾼다.</summary>
    public void SetWaves(List<Wave> newWaves) {
        waves = newWaves;
    }

    /// <summary>다음 웨이브를 바로 시작 (UI 버튼에서 호출).</summary>
    public void StartNextWave() {
        if (!WaitingForNextWave || CurrentWave >= waves.Count) {
            return;
        }
        StartCoroutine(RunWave(waves[CurrentWave]));
    }

    /// <summary>적 하나를 경로 위에 스폰한다.</summary>
    public static Enemy Spawn(Enemy prefab, MapRouteData.Route route, float lane = 0f, float startDistance = 0f) {
        Enemy enemy = Instantiate(prefab, route.GetPosition(startDistance), Quaternion.identity);
        enemy.Initialize(route, lane, startDistance);
        return enemy;
    }

    // 4. 메서드 (private)
    private void Awake() {
        if (routeData == null && TDMap.Instance != null) {
            routeData = TDMap.Instance.RouteData;
        }
    }

    private IEnumerator RunWave(Wave wave) {
        WaitingForNextWave = false;
        CurrentWave++;
        WaveStarted?.Invoke(CurrentWave);
        aliveGroups = wave.Groups.Count;
        foreach (SpawnGroup group in wave.Groups) {
            StartCoroutine(RunGroup(group));
        }
        yield return new WaitUntil(() => aliveGroups == 0);

        if (CurrentWave >= waves.Count) {
            // 마지막 웨이브: 남은 적이 다 사라지면 끝
            yield return new WaitUntil(() => Enemy.Active.Count == 0);
            AllWavesCleared?.Invoke();
            yield break;
        }

        WaitingForNextWave = true;
        yield return new WaitForSeconds(timeBetweenWaves);
        if (WaitingForNextWave) {
            StartNextWave();
        }
    }

    private IEnumerator RunGroup(SpawnGroup group) {
        yield return new WaitForSeconds(group.StartDelay);
        int spawned = 0;
        while (spawned < group.Count) {
            int pack = Mathf.Min(group.PackSize, group.Count - spawned);
            for (int k = 0; k < pack; k++) {
                MapRouteData.Route route = PickRoute(group);
                if (route == null) {
                    break;
                }
                float ahead = UnityEngine.Random.Range(0f, group.PackDepth);
                Spawn(group.EnemyPrefab, route, PickLane(group, k, pack), ahead);
            }
            spawned += pack;
            yield return new WaitForSeconds(group.Interval);
        }
        aliveGroups--;
    }

    /// <summary>무리 안에서 차선을 고르게 나눠 갖고 약간 흔든다.</summary>
    private static float PickLane(SpawnGroup group, int indexInPack, int packCount) {
        if (packCount == 1) {
            return UnityEngine.Random.Range(-group.LaneSpread, group.LaneSpread);
        }
        float slot = (indexInPack + PACK_SLOT_CENTER) / packCount;
        return Mathf.Lerp(-group.LaneSpread, group.LaneSpread, slot) + UnityEngine.Random.Range(-LANE_JITTER, LANE_JITTER);
    }

    private MapRouteData.Route PickRoute(SpawnGroup group) {
        if (routeData == null || group.Routes.Count == 0) {
            Debug.LogWarning("[WaveSpawner] routeData 또는 경로 이름이 비어 있습니다.");
            return null;
        }
        string routeName = group.Routes[UnityEngine.Random.Range(0, group.Routes.Count)];
        MapRouteData.Route route = routeData.GetRoute(routeName);
        if (route == null) {
            Debug.LogWarning($"[WaveSpawner] 경로 '{routeName}'을(를) 찾을 수 없습니다.");
        }
        return route;
    }
}
// AI-assisted code end
