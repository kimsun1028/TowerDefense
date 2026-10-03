// AI-assisted code start
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 웨이브 단위로 적을 스폰한다. 경로는 맵의 MapRouteData에서 이름으로 고른다.
///
/// 그룹마다 SpawnId(입구)를 정하면 SpawnPoints 표에서 그 입구의 경로 중 하나를 적마다 랜덤으로 고른다
/// (예: "Top" = Path_TopMiddle, Path_TopLoop). Routes에 경로 이름을 직접 넣으면 그 목록을 우선 쓴다.
/// 웨이브마다 다른 입구/경로를 줄 수 있다.
/// PackSize / LaneSpread로 한 줄이 아니라 무리로 퍼져 나오게 한다.
/// 보스: BossPrefab이 있으면 마지막 웨이브의 적이 모두 사라진 뒤 BossDelay초 후 등장한다.
/// 게임 연동: WaveStarted, BossSpawned, AllWavesCleared 이벤트.
/// </summary>
public class WaveSpawner : MonoBehaviour {
    /// <summary>한 종류의 적을 같은 경로 후보로 내보내는 묶음.</summary>
    [Serializable]
    public class SpawnGroup {
        // 1. 상수
        private const int DEFAULT_COUNT = 5;
        private const float DEFAULT_INTERVAL = 0.8f;
        private const float DEFAULT_LANE_SPREAD = 0.7f;
        private const float DEFAULT_PACK_DEPTH = 0.4f;

        // 2. 필드
        [SerializeField] private Enemy enemyPrefab;
        [Tooltip("입구 ID (예: Top, Right). Routes가 비어 있으면 이 입구의 경로 중 랜덤")]
        [SerializeField] private string spawnId;
        [Tooltip("직접 지정할 경로 이름들 (비우면 입구의 경로 사용). 적마다 랜덤으로 하나")]
        [SerializeField] private List<string> routes = new();
        [SerializeField] private int count = DEFAULT_COUNT;
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
        public string SpawnId => spawnId;
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

        /// <summary>입구 ID로 스폰하는 그룹.</summary>
        public SpawnGroup(Enemy prefab, string spawnPointId, int spawnCount, float spawnInterval,
                          float delay = 0f, int pack = 1, float spread = DEFAULT_LANE_SPREAD, float depth = DEFAULT_PACK_DEPTH)
            : this(prefab, new List<string>(), spawnCount, spawnInterval, delay, pack, spread, depth) {
            spawnId = spawnPointId;
        }

        /// <summary>경로 이름을 직접 지정하는 그룹.</summary>
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

    /// <summary>입구 ID → 그 입구에서 출발하는 경로 이름들.</summary>
    [Serializable]
    public class SpawnPointEntry {
        // 2. 필드
        [SerializeField] private string spawnId;
        [SerializeField] private List<string> routeNames = new();

        public string SpawnId => spawnId;
        public IReadOnlyList<string> RouteNames => routeNames;

        // 3. 생성자
        /// <summary>직렬화용 기본 생성자.</summary>
        public SpawnPointEntry() {
        }

        public SpawnPointEntry(string id, List<string> names) {
            spawnId = id;
            routeNames = names;
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
    private const float DEFAULT_TIME_BETWEEN_WAVES = 15f; // 웨이브 사이 준비 시간 (입구 버튼으로 일찍 부를 수 있음)
    private const float DEFAULT_BOSS_DELAY = 3f;
    private const float LANE_JITTER = 0.1f;
    private const float PACK_SLOT_CENTER = 0.5f;

    // 2. 필드
    /// <summary>웨이브 시작 (웨이브 번호, 1부터)</summary>
    public static event Action<int> WaveStarted;
    /// <summary>보스 등장 (마지막 웨이브가 끝난 뒤)</summary>
    public static event Action<Enemy> BossSpawned;
    /// <summary>마지막 웨이브와 보스까지 모든 적이 사라짐</summary>
    public static event Action AllWavesCleared;

    [SerializeField] private MapRouteData routeData;
    [Tooltip("입구 ID별 경로 목록 (Map01: Top = Path_TopMiddle, Path_TopLoop / Right = Path_Right)")]
    [SerializeField] private List<SpawnPointEntry> spawnPoints = new();
    [SerializeField] private List<Wave> waves = new();
    [SerializeField] private float timeBetweenWaves = DEFAULT_TIME_BETWEEN_WAVES;

    [Header("Boss (마지막 웨이브 뒤 등장)")]
    [Tooltip("비우면 보스 없음")]
    [SerializeField] private Enemy bossPrefab;
    [Tooltip("보스가 나올 입구 ID")]
    [SerializeField] private string bossSpawnId;
    [Tooltip("마지막 웨이브 적이 다 사라진 뒤 보스가 나오기까지 (초)")]
    [SerializeField] private float bossDelay = DEFAULT_BOSS_DELAY;

    private int aliveGroups;
    private int waveRun;   // 웨이브마다 증가 → 일찍 부른 뒤 남은 옛 타이머가 다음 웨이브를 또 시작하지 않게

    public MapRouteData RouteData { get => routeData; set => routeData = value; }
    public IReadOnlyList<Wave> Waves => waves;
    public IReadOnlyList<SpawnPointEntry> SpawnPoints => spawnPoints;
    public int CurrentWave { get; private set; }
    public int TotalWaves => waves.Count;
    public bool WaitingForNextWave { get; private set; } = true;
    /// <summary>다음 웨이브가 저절로 시작되는 시각 (Time.time). 첫 웨이브 전처럼 자동 시작이 없으면 -1.</summary>
    public float NextWaveTime { get; private set; } = -1f;
    public float TimeBetweenWaves => timeBetweenWaves;
    public Enemy Boss { get; private set; }

    // 4. 메서드 (public)
    /// <summary>입구 표를 통째로 바꾼다.</summary>
    public void SetSpawnPoints(List<SpawnPointEntry> entries) {
        spawnPoints = entries;
    }

    /// <summary>입구의 경로 중 하나를 랜덤으로 고른다. 입구나 경로가 없으면 null.</summary>
    public MapRouteData.Route PickRandomRoute(string spawnId) {
        if (routeData == null) {
            return null;
        }
        foreach (SpawnPointEntry entry in spawnPoints) {
            if (entry.SpawnId == spawnId && entry.RouteNames.Count > 0) {
                return routeData.GetRoute(entry.RouteNames[UnityEngine.Random.Range(0, entry.RouteNames.Count)]);
            }
        }
        return null;
    }

    /// <summary>웨이브 목록을 통째로 바꾼다.</summary>
    public void SetWaves(List<Wave> newWaves) {
        waves = newWaves;
    }

    /// <summary>보스를 정한다. prefab이 null이면 보스 없음.</summary>
    public void SetBoss(Enemy prefab, string spawnId, float delay = DEFAULT_BOSS_DELAY) {
        bossPrefab = prefab;
        bossSpawnId = spawnId;
        bossDelay = delay;
    }

    /// <summary>waveIndex(0부터) 웨이브에 이 입구에서 나오는 무리가 있는가 (UI의 "다음 웨이브" 버튼 표시용).</summary>
    public bool UsesSpawn(int waveIndex, string spawnId) {
        if (waveIndex < 0 || waveIndex >= waves.Count) {
            return false;
        }
        foreach (SpawnGroup group in waves[waveIndex].Groups) {
            if (group.SpawnId == spawnId) {
                return true;
            }
            if (string.IsNullOrEmpty(group.SpawnId) && SpawnHasRoute(spawnId, group.Routes)) {
                return true;
            }
        }
        return false;
    }

    /// <summary>waveIndex(0부터) 웨이브의 적이 걸을 수 있는 경로를 모두 담는다 (예상 경로 표시용, 중복 없음).</summary>
    public void GetRoutes(int waveIndex, List<MapRouteData.Route> result) {
        result.Clear();
        if (routeData == null || waveIndex < 0 || waveIndex >= waves.Count) {
            return;
        }
        foreach (SpawnGroup group in waves[waveIndex].Groups) {
            if (!string.IsNullOrEmpty(group.SpawnId)) {
                foreach (SpawnPointEntry entry in spawnPoints) {
                    if (entry.SpawnId == group.SpawnId) {
                        AddRoutes(entry.RouteNames, result);
                    }
                }
            } else {
                AddRoutes(group.Routes, result);
            }
        }
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

    private void AddRoutes(IReadOnlyList<string> routeNames, List<MapRouteData.Route> result) {
        foreach (string routeName in routeNames) {
            MapRouteData.Route route = routeData.GetRoute(routeName);
            if (route != null && !result.Contains(route)) {
                result.Add(route);
            }
        }
    }

    private bool SpawnHasRoute(string spawnId, IReadOnlyList<string> routeNames) {
        foreach (SpawnPointEntry entry in spawnPoints) {
            if (entry.SpawnId != spawnId) {
                continue;
            }
            foreach (string routeName in routeNames) {
                foreach (string entryRoute in entry.RouteNames) {
                    if (entryRoute == routeName) {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private IEnumerator RunWave(Wave wave) {
        int run = ++waveRun;
        WaitingForNextWave = false;
        NextWaveTime = -1f;
        CurrentWave++;
        WaveStarted?.Invoke(CurrentWave);
        aliveGroups = wave.Groups.Count;
        foreach (SpawnGroup group in wave.Groups) {
            StartCoroutine(RunGroup(group));
        }
        // 웨이브 끝 = 다 나온 뒤 필드의 마지막 적이 죽거나 지키는 곳에 들어갔을 때
        yield return new WaitUntil(() => aliveGroups == 0);
        yield return new WaitUntil(() => Enemy.Active.Count == 0);

        if (CurrentWave >= waves.Count) {
            // 마지막 웨이브: 보스 등장 → 보스까지 사라지면 끝
            if (bossPrefab != null) {
                yield return new WaitForSeconds(bossDelay);
                SpawnBoss();
                yield return new WaitUntil(() => Enemy.Active.Count == 0);
            }
            AllWavesCleared?.Invoke();
            yield break;
        }

        WaitingForNextWave = true;
        NextWaveTime = Time.time + timeBetweenWaves;
        yield return new WaitForSeconds(timeBetweenWaves);
        if (WaitingForNextWave && run == waveRun) {
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

    private void SpawnBoss() {
        MapRouteData.Route route = PickRandomRoute(bossSpawnId);
        if (route == null) {
            Debug.LogWarning($"[WaveSpawner] 보스 입구 '{bossSpawnId}'의 경로를 찾을 수 없습니다.");
            return;
        }
        Boss = Spawn(bossPrefab, route);
        BossSpawned?.Invoke(Boss);
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
        if (routeData == null) {
            Debug.LogWarning("[WaveSpawner] routeData가 비어 있습니다.");
            return null;
        }
        if (group.Routes.Count == 0) {
            MapRouteData.Route fromSpawn = PickRandomRoute(group.SpawnId);
            if (fromSpawn == null) {
                Debug.LogWarning($"[WaveSpawner] 입구 '{group.SpawnId}'의 경로를 찾을 수 없습니다.");
            }
            return fromSpawn;
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
