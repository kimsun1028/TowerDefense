// AI-assisted code start
using System.Collections.Generic;
using UnityEngine;

/// <summary>맵 데이터 검증. 문제 목록을 돌려준다.</summary>
public static class MapValidator {
    /// <summary>검증 결과 하나.</summary>
    public readonly struct Issue {
        // 2. 필드
        private readonly Object target;
        private readonly string message;
        private readonly bool isError;

        public Object Target => target;
        public string Message => message;
        public bool IsError => isError;

        // 3. 생성자
        public Issue(Object issueTarget, string issueMessage, bool error) {
            target = issueTarget;
            message = issueMessage;
            isError = error;
        }
    }

    // 1. 상수
    private const float SLOT_RADIUS = 0.36f;      // 패드 반지름 (Pad 스프라이트 0.8 x 0.53)
    private const float MIN_SLOT_SPACING = 0.75f;
    private const float MAX_GOAL_DISTANCE = 3f;   // 마지막 노드가 골에서 이보다 멀면 경고
    private const int MIN_NODE_COUNT = 2;

    // 4. 메서드 (public)
    /// <summary>슬롯 집결지에 문제가 있으면 설명, 없으면 null.</summary>
    public static string CheckRally(TDMap map, TowerSlot slot) {
        map.Refresh();
        Vector2 rallyPoint = slot.RallyPointWorld;
        if (Vector2.Distance(slot.transform.position, rallyPoint) > slot.RallyRange) {
            return "집결지가 범위(RallyRange) 밖에 있습니다.";
        }
        if (!map.IsOnRoad(rallyPoint)) {
            return "집결지가 길 위가 아닙니다.";
        }
        return null;
    }

    /// <summary>맵 전체를 검사해서 문제 목록을 돌려준다.</summary>
    public static List<Issue> Validate(TDMap map) {
        var issues = new List<Issue>();
        map.Refresh();

        if (map.Paths.Count == 0) {
            issues.Add(new Issue(map, "경로(EnemyPath)가 없습니다.", true));
        }
        if (map.Goal == null) {
            issues.Add(new Issue(map, "TDMap.Goal(지키는곳)이 비어 있습니다.", false));
        }
        string routeIssue = MapAuthoring.FindRouteDataMismatch(map);
        if (routeIssue != null) {
            issues.Add(new Issue(map.RouteData != null ? map.RouteData : map, routeIssue, false));
        }

        ValidatePaths(map, issues);
        ValidateSlots(map, issues);
        return issues;
    }

    // 4. 메서드 (private)
    private static void ValidatePaths(TDMap map, List<Issue> issues) {
        foreach (EnemyPath path in map.Paths) {
            if (path.Count < MIN_NODE_COUNT) {
                issues.Add(new Issue(path, $"{path.name}: 노드가 2개 미만입니다.", true));
                continue;
            }
            if (path.Nodes.Exists(node => node == null)) {
                issues.Add(new Issue(path, $"{path.name}: 비어 있는 노드 참조가 있습니다.", true));
            } else if (map.Goal && Vector2.Distance(path[path.Count - 1], map.Goal.position) > MAX_GOAL_DISTANCE) {
                issues.Add(new Issue(path, $"{path.name}: 마지막 노드가 골에서 멉니다. 방향이 반대일 수 있습니다.", false));
            }
        }
    }

    private static void ValidateSlots(TDMap map, List<Issue> issues) {
        var numbers = new HashSet<int>();
        for (int i = 0; i < map.Slots.Count; i++) {
            TowerSlot slot = map.Slots[i];
            if (!numbers.Add(slot.SlotNumber)) {
                issues.Add(new Issue(slot, $"Slot {slot.SlotNumber}: 번호가 중복됩니다.", true));
            }
            if (map.Paths.Count > 0 && map.OverlapsRoad(slot.transform.position, SLOT_RADIUS)) {
                issues.Add(new Issue(slot, $"Slot {slot.SlotNumber}: 길과 겹칩니다.", false));
            }
            string rallyIssue = CheckRally(map, slot);
            if (rallyIssue != null) {
                issues.Add(new Issue(slot, $"Slot {slot.SlotNumber}: {rallyIssue}", false));
            }
            for (int j = i + 1; j < map.Slots.Count; j++) {
                TowerSlot other = map.Slots[j];
                if (Vector2.Distance(slot.transform.position, other.transform.position) < MIN_SLOT_SPACING) {
                    issues.Add(new Issue(slot, $"Slot {slot.SlotNumber} ↔ {other.SlotNumber}: 너무 가깝습니다 (타워가 겹침).", false));
                }
            }
        }
    }
}
// AI-assisted code end
