// AI-assisted code start
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 맵 전체에 대한 질의 API (싱글톤). 타워·적 쪽에서는 이 클래스만 보면 된다.
///
///   TDMap.Instance.IsOnRoad(p)                       이 위치가 길 위인가
///   TDMap.Instance.FindClosestPointOnRoad(p)         가장 가까운 길 중앙선 위의 점
///   TDMap.Instance.TryGetRallyPoint(o, want, r, out) 원점 o에서 r 이내, want에 가장 가까운 길 위 지점
///   TDMap.Instance.GetPath("Path_Right")             이름으로 경로 찾기 (편집용 EnemyPath)
///   TDMap.Instance.RouteData                         구운 경로 데이터 (게임용)
///   TDMap.Instance.GetSpawn("Top") / Goal            입구 / 지키는 곳
///
/// 길 = 길 그림 마스크(roadMask). 마스크가 없으면 EnemyPath 선분 + 폭(roadWidth).
/// </summary>
[ExecuteAlways]
public class TDMap : MonoBehaviour {
    // 1. 상수
    private const float MASK_ALPHA_THRESHOLD = 0.5f;
    private const float HALF = 0.5f;
    private const int OVERLAP_SAMPLE_COUNT = 16;
    private const float EPSILON = 1e-8f;
    private const int ROAD_LINE_CORNER_VERTICES = 6;
    private const int ROAD_LINE_SORTING_ORDER = -10;
    private static readonly Color VIEW_RECT_GIZMO_COLOR = Color.white;
    private static readonly Color RALLY_GIZMO_COLOR = new(1f, 0.6f, 0f, 0.6f);

    // 2. 필드
    private static TDMap instance;

    [Header("Road")]
    [Tooltip("길 마스크가 없을 때 쓰는 길 폭 (경로 중앙선 기준)")]
    [Min(0.1f)] [SerializeField] private float roadWidth = 0.75f;
    [SerializeField] private Color roadColor = new(0.82f, 0.72f, 0.5f);
    [SerializeField] private Material roadMaterial;
    [Tooltip("경로를 LineRenderer로 그린다. 길 그림(roadMask)을 쓰면 끈다.")]
    [SerializeField] private bool drawRoadLines = true;

    [Header("Road Mask (선택)")]
    [Tooltip("알파 > 0.5인 곳을 길로 판정. Read/Write가 켜진 텍스처여야 한다. 길 그림 스프라이트와 같은 텍스처를 쓰면 된다.")]
    [SerializeField] private Texture2D roadMask;
    [Tooltip("roadMask가 덮는 월드 영역")]
    [SerializeField] private Rect roadMaskRect;

    [Header("Refs")]
    [SerializeField] private GoalPoint goal;
    [Tooltip("경로를 구운 SO. TDMap 인스펙터나 Map Editor의 '경로 SO로 굽기'로 만든다.")]
    [SerializeField] private MapRouteData routeData;
    [Tooltip("카메라가 꽉 채워 보여줄 월드 영역 (CameraFit의 Fit To Map이 사용)")]
    [SerializeField] private Rect viewRect = new(-6.8f, -4.2f, 13.6f, 9.1f);

    private readonly List<EnemyPath> paths = new();
    private readonly List<TowerSlot> slots = new();
    private readonly List<SpawnPoint> spawns = new();

    public static TDMap Instance {
        get {
            if (instance == null) {
                instance = FindAnyObjectByType<TDMap>();
            }
            return instance;
        }
    }

    public float RoadWidth { get => roadWidth; set => roadWidth = value; }
    public Color RoadColor { get => roadColor; set => roadColor = value; }
    public Material RoadMaterial { get => roadMaterial; set => roadMaterial = value; }
    public bool DrawRoadLines { get => drawRoadLines; set => drawRoadLines = value; }
    public Texture2D RoadMask { get => roadMask; set => roadMask = value; }
    public Rect RoadMaskRect { get => roadMaskRect; set => roadMaskRect = value; }
    public GoalPoint Goal { get => goal; set => goal = value; }
    public MapRouteData RouteData { get => routeData; set => routeData = value; }
    public Rect ViewRect { get => viewRect; set => viewRect = value; }

    public IReadOnlyList<EnemyPath> Paths => paths;
    public IReadOnlyList<TowerSlot> Slots => slots;
    public IReadOnlyList<SpawnPoint> Spawns => spawns;

    // 4. 메서드 (public)
    /// <summary>하위의 경로/슬롯 목록을 다시 수집한다. 에디터에서 구조를 바꾼 뒤 호출.</summary>
    public void Refresh() {
        paths.Clear();
        slots.Clear();
        spawns.Clear();
        GetComponentsInChildren(true, paths);
        GetComponentsInChildren(true, slots);
        GetComponentsInChildren(true, spawns);
    }

    /// <summary>입구 ID로 입구를 찾는다. 없으면 null.</summary>
    public SpawnPoint GetSpawn(string spawnId) {
        if (spawns.Count == 0) {
            Refresh();
        }
        foreach (SpawnPoint spawn in spawns) {
            if (spawn != null && spawn.SpawnId == spawnId) {
                return spawn;
            }
        }
        return null;
    }

    /// <summary>이름으로 편집용 경로(EnemyPath)를 찾는다. 없으면 null.</summary>
    public EnemyPath GetPath(string pathName) {
        if (paths.Count == 0) {
            Refresh();
        }
        foreach (EnemyPath path in paths) {
            if (path != null && path.name == pathName) {
                return path;
            }
        }
        return null;
    }

    /// <summary>p가 길 위인가.</summary>
    public bool IsOnRoad(Vector2 point) {
        if (roadMask != null) {
            if (!roadMaskRect.Contains(point)) {
                return false;
            }
            Vector2 uv = Rect.PointToNormalized(roadMaskRect, point);
            return roadMask.GetPixelBilinear(uv.x, uv.y).a > MASK_ALPHA_THRESHOLD;
        }
        return GetDistanceToRoad(point) <= roadWidth * HALF;
    }

    /// <summary>원(center, radius)이 길에 걸치는가. 타워 자리 배치 검사용.</summary>
    public bool OverlapsRoad(Vector2 center, float radius) {
        if (IsOnRoad(center)) {
            return true;
        }
        for (int i = 0; i < OVERLAP_SAMPLE_COUNT; i++) {
            float angle = i * Mathf.PI * 2f / OVERLAP_SAMPLE_COUNT;
            Vector2 edge = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            if (IsOnRoad(edge)) {
                return true;
            }
        }
        return false;
    }

    /// <summary>길 중앙선까지의 거리.</summary>
    public float GetDistanceToRoad(Vector2 point) {
        return Vector2.Distance(point, FindClosestPointOnRoad(point));
    }

    public Vector2 FindClosestPointOnRoad(Vector2 point) {
        return FindClosestPointOnRoad(point, out _, out _);
    }

    /// <summary>가장 가까운 길 중앙선 위의 점.</summary>
    /// <param name="path">그 점이 속한 경로</param>
    /// <param name="distanceAlong">경로 시작점부터 그 점까지 거리</param>
    public Vector2 FindClosestPointOnRoad(Vector2 point, out EnemyPath path, out float distanceAlong) {
        Vector2 best = point;
        float bestSqrDistance = float.MaxValue;
        path = null;
        distanceAlong = 0f;

        foreach (EnemyPath enemyPath in paths) {
            if (enemyPath == null) {
                continue;
            }
            float along = 0f;
            for (int i = 1; i < enemyPath.Count; i++) {
                Vector2 start = enemyPath[i - 1];
                Vector2 end = enemyPath[i];
                Vector2 closest = FindClosestOnSegment(start, end, point, out float t);
                float sqrDistance = (closest - point).sqrMagnitude;
                float segmentLength = Vector2.Distance(start, end);
                if (sqrDistance < bestSqrDistance) {
                    bestSqrDistance = sqrDistance;
                    best = closest;
                    path = enemyPath;
                    distanceAlong = along + segmentLength * t;
                }
                along += segmentLength;
            }
        }
        return best;
    }

    /// <summary>
    /// 병영 집결지 계산. origin(타워 위치)에서 range 이내이면서 길 위인 점 중 desired에 가장 가까운 점.
    /// desired가 이미 조건을 만족하면 그대로 돌려준다. 범위 안에 길이 전혀 없으면 false.
    /// </summary>
    public bool TryGetRallyPoint(Vector2 origin, Vector2 desired, float range, out Vector2 result) {
        if (IsOnRoad(desired) && Vector2.Distance(origin, desired) <= range) {
            result = desired;
            return true;
        }

        result = origin;
        float bestSqrDistance = float.MaxValue;
        bool found = false;
        foreach (EnemyPath enemyPath in paths) {
            if (enemyPath == null) {
                continue;
            }
            for (int i = 1; i < enemyPath.Count; i++) {
                // 선분 중 원 안에 들어오는 구간만 남긴 뒤, 그 구간에서 desired에 가장 가까운 점
                if (!ClipSegmentToCircle(enemyPath[i - 1], enemyPath[i], origin, range, out Vector2 clipStart, out Vector2 clipEnd)) {
                    continue;
                }
                Vector2 closest = FindClosestOnSegment(clipStart, clipEnd, desired, out _);
                float sqrDistance = (closest - desired).sqrMagnitude;
                if (sqrDistance < bestSqrDistance) {
                    bestSqrDistance = sqrDistance;
                    result = closest;
                    found = true;
                }
            }
        }
        return found;
    }

    /// <summary>슬롯 기준 기본 집결지: 슬롯에서 가장 가까운 길 지점.</summary>
    public bool TryGetDefaultRallyPoint(TowerSlot slot, out Vector2 result) {
        return TryGetRallyPoint(slot.transform.position, slot.transform.position, slot.RallyRange, out result);
    }

    /// <summary>각 EnemyPath에 LineRenderer로 길을 그린다 (임시 그래픽). drawRoadLines가 꺼져 있으면 숨긴다.</summary>
    public void RebuildRoadVisuals() {
        Refresh();
        foreach (EnemyPath enemyPath in paths) {
            if (enemyPath == null) {
                continue;
            }
            if (!drawRoadLines) {
                if (enemyPath.TryGetComponent(out LineRenderer oldLine)) {
                    oldLine.enabled = false;
                }
                continue;
            }
            if (!enemyPath.TryGetComponent(out LineRenderer line)) {
                line = enemyPath.gameObject.AddComponent<LineRenderer>();
            }
            line.enabled = true;
            var positions = new Vector3[enemyPath.Count];
            for (int i = 0; i < enemyPath.Count; i++) {
                positions[i] = enemyPath[i];
            }
            line.positionCount = positions.Length;
            line.SetPositions(positions);
            line.widthMultiplier = roadWidth;
            line.numCornerVertices = ROAD_LINE_CORNER_VERTICES;
            line.numCapVertices = ROAD_LINE_CORNER_VERTICES;
            line.startColor = roadColor;
            line.endColor = roadColor;
            line.sortingOrder = ROAD_LINE_SORTING_ORDER;
            if (roadMaterial) {
                line.sharedMaterial = roadMaterial;
            }
        }
    }

    // 4. 메서드 (private)
    private void OnEnable() {
        instance = this;
        Refresh();
    }

    private static Vector2 FindClosestOnSegment(Vector2 start, Vector2 end, Vector2 point, out float t) {
        Vector2 segment = end - start;
        float sqrLength = segment.sqrMagnitude;
        t = sqrLength < EPSILON ? 0f : Mathf.Clamp01(Vector2.Dot(point - start, segment) / sqrLength);
        return start + segment * t;
    }

    /// <summary>선분 start-end 중 원(center, radius) 안에 있는 부분 [clipStart, clipEnd]. 없으면 false.</summary>
    private static bool ClipSegmentToCircle(Vector2 start, Vector2 end, Vector2 center, float radius,
                                            out Vector2 clipStart, out Vector2 clipEnd) {
        clipStart = start;
        clipEnd = start;
        Vector2 direction = end - start;
        Vector2 fromCenter = start - center;
        float a = Vector2.Dot(direction, direction);
        if (a < EPSILON) {
            return fromCenter.sqrMagnitude <= radius * radius;
        }
        float b = 2f * Vector2.Dot(fromCenter, direction);
        float c = Vector2.Dot(fromCenter, fromCenter) - radius * radius;
        float discriminant = b * b - 4f * a * c;
        if (discriminant < 0f) {
            return false;
        }
        float sqrtDiscriminant = Mathf.Sqrt(discriminant);
        float t0 = Mathf.Max(0f, (-b - sqrtDiscriminant) / (2f * a));
        float t1 = Mathf.Min(1f, (-b + sqrtDiscriminant) / (2f * a));
        if (t0 > t1) {
            return false;
        }
        clipStart = start + direction * t0;
        clipEnd = start + direction * t1;
        return true;
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = VIEW_RECT_GIZMO_COLOR;
        Gizmos.DrawWireCube(viewRect.center, viewRect.size);
        Gizmos.color = RALLY_GIZMO_COLOR;
        foreach (TowerSlot slot in slots) {
            if (slot == null) {
                continue;
            }
            Gizmos.DrawLine(slot.transform.position, slot.RallyPointWorld);
        }
    }
}
// AI-assisted code end
