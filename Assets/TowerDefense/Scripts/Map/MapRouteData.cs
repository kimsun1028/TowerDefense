// AI-assisted code start
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 맵의 적 경로 데이터 (ScriptableObject). 씬/프리팹의 EnemyPath를 "굽기" 버튼으로 저장한 결과물.
/// 적·웨이브 담당은 씬을 몰라도 이 에셋만 참조하면 경로를 쓸 수 있다.
///
///   MapRouteData.Route route = routeData.GetRoute("Path_Right");
///   route.Waypoints                  원본 웨이포인트 (0번 = 스폰, 마지막 = 출구)
///   route.Points                     등간격(SampleSpacing)으로 다시 찍은 포인트
///   route.Length                     경로 전체 길이
///   route.GetPosition(d, lane, r)    시작점에서 d만큼 간 위치. lane(-1~1)만큼 좌우로 비켜서, 길 밖으로는 안 나감
///   route.GetDirection(d)            그 지점의 진행 방향
///   route.GetRemainingDistance(d)    출구까지 남은 거리 (경로 길이가 달라도 비교 가능)
///
/// 좌표는 월드 좌표다 (맵 프리팹이 원점에 놓인다는 전제).
/// 직접 고치지 말고 맵을 수정한 뒤 다시 구울 것.
/// </summary>
[CreateAssetMenu(fileName = "MapRoutes", menuName = "TD/Map Route Data")]
public class MapRouteData : ScriptableObject {
    /// <summary>경로 하나 (구운 결과).</summary>
    [Serializable]
    public class Route {
        // 1. 상수
        private const float EPSILON = 1e-6f;
        private const float MIN_SPACING = 1e-4f;
        private const int DIRECTION_SMOOTH_RANGE = 2;

        // 2. 필드
        [SerializeField] private string name;
        [Tooltip("원본 웨이포인트 (0번 = 스폰, 마지막 = 출구)")]
        [SerializeField] private List<Vector3> waypoints = new();
        [Tooltip("waypoints를 sampleSpacing 간격으로 다시 찍은 포인트. 첫 점과 마지막 점은 원본과 같다.")]
        [SerializeField] private List<Vector3> points = new();
        [Tooltip("points[i]까지의 경로 거리")]
        [SerializeField] private List<float> distances = new();
        [Tooltip("points[i]에서 진행 방향 기준 왼쪽으로 길 끝까지의 거리")]
        [SerializeField] private List<float> leftWidth = new();
        [Tooltip("points[i]에서 진행 방향 기준 오른쪽으로 길 끝까지의 거리")]
        [SerializeField] private List<float> rightWidth = new();
        [Tooltip("경로 전체 길이 (월드 단위)")]
        [SerializeField] private float length;

        public string Name => name;
        public IReadOnlyList<Vector3> Waypoints => waypoints;
        public IReadOnlyList<Vector3> Points => points;
        public IReadOnlyList<float> Distances => distances;
        public IReadOnlyList<float> LeftWidth => leftWidth;
        public IReadOnlyList<float> RightWidth => rightWidth;
        public float Length => length;
        public Vector3 Start => waypoints.Count > 0 ? waypoints[0] : Vector3.zero;
        public Vector3 End => waypoints.Count > 0 ? waypoints[waypoints.Count - 1] : Vector3.zero;

        // 3. 생성자
        /// <summary>직렬화용 기본 생성자.</summary>
        public Route() {
        }

        public Route(string routeName, List<Vector3> routeWaypoints, List<Vector3> routePoints, List<float> routeDistances) {
            name = routeName;
            waypoints = routeWaypoints;
            points = routePoints;
            distances = routeDistances;
            length = routeDistances.Count > 0 ? routeDistances[routeDistances.Count - 1] : 0f;
        }

        // 4. 메서드 (public)
        public float GetRemainingDistance(float distance) {
            return Mathf.Max(0f, length - distance);
        }

        public bool IsFinished(float distance) {
            return distance >= length;
        }

        /// <summary>distance가 속한 points 구간 [i, i+1]과 그 안의 비율 t.</summary>
        public int FindSegment(float distance, out float t) {
            int count = points.Count;
            if (count < 2) {
                t = 0f;
                return 0;
            }
            distance = Mathf.Clamp(distance, 0f, length);
            // 등간격이라 바로 추정한 뒤 앞뒤로 보정
            float spacing = length / Mathf.Max(1, count - 1);
            int index = Mathf.Clamp((int)(distance / Mathf.Max(spacing, MIN_SPACING)), 0, count - 2);
            while (index > 0 && distances[index] > distance) {
                index--;
            }
            while (index < count - 2 && distances[index + 1] < distance) {
                index++;
            }
            float segmentLength = distances[index + 1] - distances[index];
            t = segmentLength > EPSILON ? (distance - distances[index]) / segmentLength : 0f;
            return index;
        }

        /// <summary>경로 중앙선 위 위치.</summary>
        public Vector3 GetPosition(float distance) {
            if (points.Count == 0) {
                return Vector3.zero;
            }
            if (points.Count == 1) {
                return points[0];
            }
            int index = FindSegment(distance, out float t);
            return Vector3.Lerp(points[index], points[index + 1], t);
        }

        /// <summary>
        /// 차선 위치. lane = -1(오른쪽 끝) ~ 0(중앙) ~ 1(왼쪽 끝), 진행 방향 기준.
        /// radius = 유닛 반지름: 길 가장자리에서 이만큼 안쪽까지만 간다.
        /// 길 폭이 좁아지는 곳에서는 같은 lane 값이라도 자동으로 중앙 쪽으로 모인다.
        /// </summary>
        public Vector3 GetPosition(float distance, float lane, float radius = 0f) {
            Vector3 center = GetPosition(distance);
            if (Mathf.Approximately(lane, 0f) || points.Count < 2 || leftWidth.Count != points.Count) {
                return center;
            }
            int index = FindSegment(distance, out float t);
            float width = lane > 0f
                ? Mathf.Lerp(leftWidth[index], leftWidth[index + 1], t)
                : Mathf.Lerp(rightWidth[index], rightWidth[index + 1], t);
            float offset = Mathf.Clamp01(Mathf.Abs(lane)) * Mathf.Max(0f, width - radius) * Mathf.Sign(lane);
            return center + (Vector3)(GetNormal(distance) * offset);
        }

        /// <summary>진행 방향 (단위 벡터). 앞뒤 포인트를 같이 봐서 코너에서 부드럽게 바뀐다.</summary>
        public Vector2 GetDirection(float distance) {
            if (points.Count < 2) {
                return Vector2.right;
            }
            int index = FindSegment(distance, out float t);
            Vector2 from = GetDirectionAtPoint(index);
            Vector2 to = GetDirectionAtPoint(index + 1);
            Vector2 direction = Vector2.Lerp(from, to, t);
            return direction.sqrMagnitude > EPSILON ? direction.normalized : from;
        }

        /// <summary>진행 방향의 왼쪽 법선.</summary>
        public Vector2 GetNormal(float distance) {
            Vector2 direction = GetDirection(distance);
            return new Vector2(-direction.y, direction.x);
        }

        /// <summary>임의 위치에서 가장 가까운 경로 지점의 거리 (경로 위로 다시 합류할 때 사용).</summary>
        public float FindClosestDistance(Vector2 position) {
            float bestDistance = 0f;
            float bestSqr = float.MaxValue;
            for (int i = 0; i + 1 < points.Count; i++) {
                Vector2 start = points[i];
                Vector2 segment = (Vector2)points[i + 1] - start;
                float sqrLength = segment.sqrMagnitude;
                float t = sqrLength < EPSILON ? 0f : Mathf.Clamp01(Vector2.Dot(position - start, segment) / sqrLength);
                float sqr = (start + segment * t - position).sqrMagnitude;
                if (sqr < bestSqr) {
                    bestSqr = sqr;
                    bestDistance = Mathf.Lerp(distances[i], distances[i + 1], t);
                }
            }
            return bestDistance;
        }

        // 4. 메서드 (internal / private)
        internal void SetWidths(List<float> left, List<float> right) {
            leftWidth = left;
            rightWidth = right;
        }

        /// <summary>points[i]에서의 방향: 앞뒤 2칸을 평균내서 부드럽게.</summary>
        private Vector2 GetDirectionAtPoint(int index) {
            int from = Mathf.Max(0, index - DIRECTION_SMOOTH_RANGE);
            int to = Mathf.Min(points.Count - 1, index + DIRECTION_SMOOTH_RANGE);
            Vector2 delta = points[to] - points[from];
            return delta.sqrMagnitude > EPSILON ? delta.normalized : Vector2.right;
        }
    }

    // 1. 상수
    private const float DEFAULT_SAMPLE_SPACING = 0.25f;
    private const float MIN_SAMPLE_SPACING = 0.05f;
    private const float HALF = 0.5f;
    private const float WIDTH_MEASURE_STEP = 0.02f;
    private const float DEFAULT_HALF_WIDTH = 0.4f;
    private const float MAX_HALF_WIDTH = 0.6f;
    private const int WIDTH_WINDOW = 2;

    // 2. 필드
    [SerializeField] private string mapName;
    [Tooltip("등간격 포인트 사이 거리 (월드 단위)")]
    [Min(MIN_SAMPLE_SPACING)] [SerializeField] private float sampleSpacing = DEFAULT_SAMPLE_SPACING;
    [SerializeField] private List<Route> routes = new();

    public string MapName { get => mapName; set => mapName = value; }
    public float SampleSpacing { get => sampleSpacing; set => sampleSpacing = Mathf.Max(MIN_SAMPLE_SPACING, value); }
    public IReadOnlyList<Route> Routes => routes;

    // 4. 메서드 (public)
    /// <summary>이름으로 경로를 찾는다. 없으면 null.</summary>
    public Route GetRoute(string routeName) {
        foreach (Route route in routes) {
            if (route.Name == routeName) {
                return route;
            }
        }
        return null;
    }

    /// <summary>구운 경로 목록을 통째로 바꾼다 (굽기 도구 전용).</summary>
    public void SetRoutes(List<Route> bakedRoutes) {
        routes = bakedRoutes;
    }

    /// <summary>폴리라인 길이.</summary>
    public static float CalculatePolylineLength(IReadOnlyList<Vector3> polyline) {
        float total = 0f;
        for (int i = 1; i < polyline.Count; i++) {
            total += Vector3.Distance(polyline[i - 1], polyline[i]);
        }
        return total;
    }

    /// <summary>폴리라인을 spacing 간격으로 다시 찍는다. 첫 점과 마지막 점은 항상 포함.</summary>
    public static List<Vector3> Resample(IReadOnlyList<Vector3> polyline, float spacing) {
        var result = new List<Vector3>();
        if (polyline.Count == 0) {
            return result;
        }
        result.Add(polyline[0]);
        if (polyline.Count == 1 || spacing <= 0f) {
            return result;
        }

        float carry = 0f; // 직전 샘플 이후 이미 지나온 거리
        for (int i = 1; i < polyline.Count; i++) {
            Vector3 start = polyline[i - 1];
            Vector3 end = polyline[i];
            float segmentLength = Vector3.Distance(start, end);
            float next = spacing - carry;
            while (next <= segmentLength) {
                result.Add(Vector3.Lerp(start, end, next / segmentLength));
                next += spacing;
            }
            carry = segmentLength - (next - spacing);
        }

        // 마지막 점: 직전 샘플과 너무 가까우면 그 샘플을 끝점으로 대체
        Vector3 last = polyline[polyline.Count - 1];
        if (result.Count > 1 && Vector3.Distance(result[result.Count - 1], last) < spacing * HALF) {
            result[result.Count - 1] = last;
        } else {
            result.Add(last);
        }
        return result;
    }

    /// <summary>
    /// 경로 하나를 굽는다. isOnRoad가 주어지면 포인트마다 좌우 길 폭을 측정한다 (최대 MAX_HALF_WIDTH).
    /// 길 판정 영역 밖(화면 밖으로 연장된 구간 등)은 가장 가까운 측정값으로 채운다.
    /// </summary>
    public static Route BakeRoute(string routeName, IReadOnlyList<Vector3> waypoints, float spacing,
                                  Func<Vector2, bool> isOnRoad = null, float defaultHalfWidth = DEFAULT_HALF_WIDTH) {
        List<Vector3> resampled = Resample(waypoints, spacing);
        var cumulative = new List<float>(resampled.Count);
        float accumulated = 0f;
        for (int i = 0; i < resampled.Count; i++) {
            if (i > 0) {
                accumulated += Vector3.Distance(resampled[i - 1], resampled[i]);
            }
            cumulative.Add(accumulated);
        }
        var route = new Route(routeName, new List<Vector3>(waypoints), resampled, cumulative);

        int count = resampled.Count;
        var left = new float[count];
        var right = new float[count];
        var measured = new bool[count];
        for (int i = 0; i < count; i++) {
            Vector2 center = resampled[i];
            if (isOnRoad == null || !isOnRoad(center)) {
                continue;
            }
            Vector2 normal = route.GetNormal(cumulative[i]);
            left[i] = MeasureWidth(center, normal, isOnRoad);
            right[i] = MeasureWidth(center, -normal, isOnRoad);
            measured[i] = true;
        }
        FillUnmeasured(left, right, measured, defaultHalfWidth);

        // 앞뒤 2칸 중 가장 좁은 값 사용: 넓은 합류부 바로 옆의 좁은 코너에서
        // 보간된 폭과 방향 때문에 바깥 차선이 길 밖으로 튀어나가지 않게
        route.SetWidths(ApplyWindowMin(left, WIDTH_WINDOW), ApplyWindowMin(right, WIDTH_WINDOW));
        return route;
    }

    // 4. 메서드 (private)
    /// <summary>측정 못 한 포인트는 가장 가까운 측정값으로 채운다.</summary>
    private static void FillUnmeasured(float[] left, float[] right, bool[] measured, float defaultHalfWidth) {
        int count = measured.Length;
        for (int i = 0; i < count; i++) {
            if (measured[i]) {
                continue;
            }
            int nearest = -1;
            for (int k = 1; k < count && nearest < 0; k++) {
                if (i - k >= 0 && measured[i - k]) {
                    nearest = i - k;
                } else if (i + k < count && measured[i + k]) {
                    nearest = i + k;
                }
            }
            left[i] = nearest >= 0 ? left[nearest] : defaultHalfWidth;
            right[i] = nearest >= 0 ? right[nearest] : defaultHalfWidth;
        }
    }

    private static List<float> ApplyWindowMin(float[] values, int window) {
        var result = new List<float>(values.Length);
        for (int i = 0; i < values.Length; i++) {
            float min = values[i];
            int from = Mathf.Max(0, i - window);
            int to = Mathf.Min(values.Length - 1, i + window);
            for (int j = from; j <= to; j++) {
                min = Mathf.Min(min, values[j]);
            }
            result.Add(min);
        }
        return result;
    }

    private static float MeasureWidth(Vector2 center, Vector2 direction, Func<Vector2, bool> isOnRoad) {
        float width = 0f;
        while (width + WIDTH_MEASURE_STEP <= MAX_HALF_WIDTH && isOnRoad(center + direction * (width + WIDTH_MEASURE_STEP))) {
            width += WIDTH_MEASURE_STEP;
        }
        return width;
    }
}
// AI-assisted code end
