// AI-assisted code start
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > TD > Map Editor
/// 씬 뷰 클릭으로 경로/타워 자리를 배치하고, 집결지를 일괄 계산하고, 경로 SO를 굽고, 맵을 검증한다.
/// </summary>
public class MapEditorWindow : EditorWindow {
    private enum Mode { None, PlaceSlot, DrawPath }

    // 1. 상수
    private const string WINDOW_TITLE = "Map Editor";
    private const float MIN_ROAD_WIDTH = 0.2f;
    private const float MAX_ROAD_WIDTH = 2f;
    private const float SLOT_PREVIEW_RADIUS = 0.28f;
    private const float NODE_PREVIEW_RADIUS = 0.12f;
    private const float PREVIEW_DOTTED_SPACE = 4f;
    private const float PREVIEW_LABEL_OFFSET = 0.4f;
    private const int MIN_PATH_NODES = 2;
    private const int LEFT_MOUSE_BUTTON = 0;
    private const string ERROR_ICON = "<color=#ff6060>●</color> ";
    private const string WARNING_ICON = "<color=#ffc040>●</color> ";
    private static readonly Color SLOT_PREVIEW_COLOR = new(1f, 0.8f, 0.3f);
    private static readonly Color NODE_PREVIEW_COLOR = Color.yellow;
    private static readonly Color ACTIVE_BUTTON_COLOR = new(0.5f, 1f, 0.5f);

    // 2. 필드
    private Mode mode;
    private EnemyPath drawingPath;
    private List<MapValidator.Issue> issues = new();
    private bool validated;
    private Vector2 scroll;

    // 4. 메서드 (private)
    [MenuItem("Tools/TD/Map Editor")]
    private static void Open() {
        GetWindow<MapEditorWindow>(WINDOW_TITLE);
    }

    private void OnEnable() {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable() {
        SceneView.duringSceneGui -= OnSceneGUI;
        mode = Mode.None;
    }

    private void OnGUI() {
        TDMap map = MapAuthoring.FindMap();
        if (!map) {
            DrawCreateMap();
            return;
        }

        map.Refresh();
        EditorGUILayout.LabelField($"경로 {map.Paths.Count}개 · 타워 자리 {map.Slots.Count}개", EditorStyles.boldLabel);
        DrawRoadSettings(map);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("배치", EditorStyles.boldLabel);
        DrawModeButton(Mode.PlaceSlot, "타워 자리 놓기", "씬 뷰를 클릭할 때마다 다음 번호의 타워 자리가 생깁니다. (ESC 종료)");
        DrawModeButton(Mode.DrawPath, "새 경로 그리기", "스폰 지점부터 골까지 순서대로 클릭하세요. (Enter/ESC 완료)");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("병영 집결지", EditorStyles.boldLabel);
        if (GUILayout.Button("모든 슬롯 집결지 자동 계산")) {
            AssignAllRallyPoints(map);
        }

        DrawRouteDataSection(map);

        EditorGUILayout.Space();
        if (GUILayout.Button("길 그래픽 다시 그리기")) {
            map.RebuildRoadVisuals();
        }
        if (GUILayout.Button("맵 검증")) {
            RunValidation(map);
        }
        DrawIssues();
    }

    private static void DrawCreateMap() {
        EditorGUILayout.HelpBox("씬에 TDMap이 없습니다.", MessageType.Warning);
        if (GUILayout.Button("TDMap 만들기")) {
            var mapObject = new GameObject("Map");
            Undo.RegisterCreatedObjectUndo(mapObject, "Create Map");
            mapObject.AddComponent<TDMap>().RoadMaterial = MapAuthoring.RoadMaterial;
        }
    }

    private static void DrawRoadSettings(TDMap map) {
        EditorGUI.BeginChangeCheck();
        float width = EditorGUILayout.Slider("길 폭", map.RoadWidth, MIN_ROAD_WIDTH, MAX_ROAD_WIDTH);
        Color color = EditorGUILayout.ColorField("길 색", map.RoadColor);
        if (EditorGUI.EndChangeCheck()) {
            Undo.RecordObject(map, "Road");
            map.RoadWidth = width;
            map.RoadColor = color;
            map.RebuildRoadVisuals();
        }
    }

    private void AssignAllRallyPoints(TDMap map) {
        int failed = 0;
        foreach (TowerSlot slot in map.Slots) {
            if (!MapAuthoring.AssignAutoRally(map, slot)) {
                failed++;
            }
        }
        if (failed > 0) {
            Debug.LogWarning($"[Map Editor] {failed}개 슬롯은 범위 안에 길이 없어 집결지를 못 정했습니다.");
        }
        RunValidation(map);
    }

    private static void DrawRouteDataSection(TDMap map) {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("경로 데이터 (SO)", EditorStyles.boldLabel);
        string mismatch = MapAuthoring.FindRouteDataMismatch(map);
        EditorGUILayout.ObjectField("Route Data", map.RouteData, typeof(MapRouteData), false);
        if (mismatch != null) {
            EditorGUILayout.HelpBox(mismatch, MessageType.Warning);
        }
        if (GUILayout.Button("경로 SO로 굽기")) {
            EditorGUIUtility.PingObject(MapAuthoring.BakeRoutes(map));
        }
    }

    private void DrawIssues() {
        if (issues.Count == 0 && !validated) {
            return;
        }
        EditorGUILayout.Space();
        string header = issues.Count == 0 ? "✔ 문제 없음" : $"문제 {issues.Count}개 (클릭하면 선택)";
        EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        var style = new GUIStyle(EditorStyles.helpBox) { richText = true };
        foreach (MapValidator.Issue issue in issues) {
            string icon = issue.IsError ? ERROR_ICON : WARNING_ICON;
            if (GUILayout.Button(icon + issue.Message, style) && issue.Target) {
                Selection.activeObject = issue.Target;
                SceneView.lastActiveSceneView?.FrameSelected();
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void RunValidation(TDMap map) {
        issues = MapValidator.Validate(map);
        validated = true;
    }

    private void DrawModeButton(Mode buttonMode, string label, string help) {
        bool active = mode == buttonMode;
        Color previous = GUI.backgroundColor;
        if (active) {
            GUI.backgroundColor = ACTIVE_BUTTON_COLOR;
        }
        if (GUILayout.Button(active ? label + " (진행 중)" : label)) {
            StopMode();
            if (!active) {
                mode = buttonMode;
                SceneView.lastActiveSceneView?.Focus();
            }
        }
        GUI.backgroundColor = previous;
        if (active) {
            EditorGUILayout.HelpBox(help, MessageType.None);
        }
    }

    private void StopMode() {
        if (mode == Mode.DrawPath && drawingPath != null) {
            if (drawingPath.Count < MIN_PATH_NODES) {
                Undo.DestroyObjectImmediate(drawingPath.gameObject);
            } else {
                Selection.activeObject = drawingPath;
            }
        }
        drawingPath = null;
        mode = Mode.None;
        Repaint();
    }

    private void OnSceneGUI(SceneView view) {
        if (mode == Mode.None) {
            return;
        }
        TDMap map = MapAuthoring.FindMap();
        if (!map) {
            return;
        }

        Event current = Event.current;
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        Vector3 worldPosition = HandleUtility.GUIPointToWorldRay(current.mousePosition).origin;
        worldPosition.z = 0f;

        DrawPreview(map, worldPosition);
        view.Repaint();

        bool finishKey = current.type == EventType.KeyDown
            && (current.keyCode == KeyCode.Escape || current.keyCode == KeyCode.Return);
        if (finishKey) {
            StopMode();
            current.Use();
            return;
        }
        if (current.type != EventType.MouseDown || current.button != LEFT_MOUSE_BUTTON || current.alt) {
            return;
        }

        if (mode == Mode.PlaceSlot) {
            TowerSlot slot = MapAuthoring.CreateSlot(map, worldPosition, MapAuthoring.GetNextSlotNumber(map));
            EditorUtility.SetDirty(slot);
        } else if (mode == Mode.DrawPath) {
            if (drawingPath == null) {
                map.Refresh();
                drawingPath = MapAuthoring.CreatePath(map, $"Path_{map.Paths.Count + 1}", new Vector3[0]);
            }
            MapAuthoring.AddNode(drawingPath, worldPosition, drawingPath.Count);
            map.RebuildRoadVisuals();
        }
        current.Use();
    }

    private void DrawPreview(TDMap map, Vector3 worldPosition) {
        bool placingSlot = mode == Mode.PlaceSlot;
        Handles.color = placingSlot ? SLOT_PREVIEW_COLOR : NODE_PREVIEW_COLOR;
        Handles.DrawWireDisc(worldPosition, Vector3.forward, placingSlot ? SLOT_PREVIEW_RADIUS : NODE_PREVIEW_RADIUS);
        if (mode == Mode.DrawPath && drawingPath != null && drawingPath.Count > 0) {
            Handles.DrawDottedLine(drawingPath[drawingPath.Count - 1], worldPosition, PREVIEW_DOTTED_SPACE);
        }
        if (placingSlot && map.Paths.Count > 0) {
            string label = map.IsOnRoad(worldPosition) ? "길 위!" : $"#{MapAuthoring.GetNextSlotNumber(map)}";
            Handles.Label(worldPosition + Vector3.up * PREVIEW_LABEL_OFFSET, label);
        }
    }
}
// AI-assisted code end
