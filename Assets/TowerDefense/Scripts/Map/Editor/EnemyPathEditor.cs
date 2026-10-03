// AI-assisted code start
using UnityEditor;
using UnityEngine;

/// <summary>
/// EnemyPath 선택 시 씬 뷰에서:
///  - 노드 드래그로 이동
///  - Shift+클릭: 가장 가까운 구간에 노드 삽입 (끝 근처면 끝에 추가)
///  - Ctrl/Cmd+클릭(노드 위): 노드 삭제
/// 변경하면 길 그래픽이 바로 다시 그려진다. 다 고친 뒤 '경로 SO로 굽기'를 해야 게임에 반영된다.
/// </summary>
[CustomEditor(typeof(EnemyPath))]
public class EnemyPathEditor : Editor {
    // 1. 상수
    private const float NODE_HANDLE_SIZE = 0.08f;
    private const float LABEL_OFFSET_SCALE = 2f;
    private const int LEFT_MOUSE_BUTTON = 0;
    private static readonly Color SPAWN_NODE_COLOR = Color.red;
    private static readonly Color GOAL_NODE_COLOR = new(0.3f, 0.5f, 1f);
    private static readonly Color NODE_COLOR = Color.yellow;

    // 2. 필드
    private EnemyPath Path => (EnemyPath)target;

    // 4. 메서드 (public)
    public override void OnInspectorGUI() {
        EditorGUILayout.HelpBox("씬 뷰: 노드 드래그 이동 / Shift+클릭 노드 추가 / Ctrl(Cmd)+클릭 노드 삭제\n첫 노드 = 스폰, 마지막 노드 = 골", MessageType.Info);
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck()) {
            RebuildVisuals();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"노드 {Path.Count}개 · 길이 {Path.TotalLength:F1}");
        using (new EditorGUILayout.HorizontalScope()) {
            if (GUILayout.Button("끝에 노드 추가")) {
                Vector3 position = Path.Count > 0 ? Path[Path.Count - 1] + Vector3.right : Path.transform.position;
                MapAuthoring.AddNode(Path, position, Path.Count);
                RebuildVisuals();
            }
            if (GUILayout.Button("방향 뒤집기")) {
                Undo.RecordObject(Path, "Reverse Path");
                Path.Nodes.Reverse();
                MapAuthoring.RenameNodes(Path);
                RebuildVisuals();
            }
        }
    }

    // 4. 메서드 (private)
    private void OnSceneGUI() {
        EnemyPath path = Path;
        Event current = Event.current;

        for (int i = 0; i < path.Count; i++) {
            if (HandleNode(path, i, current)) {
                return; // 노드가 삭제됨
            }
        }

        bool addClick = current.type == EventType.MouseDown && current.button == LEFT_MOUSE_BUTTON && current.shift;
        if (addClick) {
            Vector3 worldPosition = HandleUtility.GUIPointToWorldRay(current.mousePosition).origin;
            worldPosition.z = 0f;
            MapAuthoring.AddNode(path, worldPosition, FindInsertIndex(path, worldPosition));
            RebuildVisuals();
            current.Use();
        }

        if (current.type == EventType.Layout && current.shift) {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }
    }

    /// <summary>노드 하나의 드래그/삭제 처리. 삭제했으면 true.</summary>
    private static bool HandleNode(EnemyPath path, int index, Event current) {
        Waypoint node = path.Nodes[index];
        if (!node) {
            return false;
        }
        Handles.color = index == 0 ? SPAWN_NODE_COLOR : index == path.Count - 1 ? GOAL_NODE_COLOR : NODE_COLOR;
        float size = HandleUtility.GetHandleSize(node.transform.position) * NODE_HANDLE_SIZE;

        EditorGUI.BeginChangeCheck();
        Vector3 moved = Handles.FreeMoveHandle(node.transform.position, size, Vector3.zero, Handles.SphereHandleCap);
        if (EditorGUI.EndChangeCheck()) {
            Undo.RecordObject(node.transform, "Move Node");
            moved.z = 0f;
            node.transform.position = moved;
            RebuildVisuals();
        }
        Handles.Label(node.transform.position + Vector3.up * size * LABEL_OFFSET_SCALE, index.ToString());

        bool deleteClick = current.type == EventType.MouseDown && current.button == LEFT_MOUSE_BUTTON
            && (current.control || current.command)
            && HandleUtility.DistanceToCircle(node.transform.position, size) < 1f;
        if (deleteClick) {
            MapAuthoring.RemoveNode(path, index);
            RebuildVisuals();
            current.Use();
            return true;
        }
        return false;
    }

    /// <summary>가장 가까운 구간 사이에 삽입. 양 끝 노드가 더 가까우면 앞/뒤에 붙인다.</summary>
    private static int FindInsertIndex(EnemyPath path, Vector3 position) {
        if (path.Count < 2) {
            return path.Count;
        }
        int bestIndex = path.Count;
        float bestDistance = Vector3.Distance(position, path[path.Count - 1]);
        float startDistance = Vector3.Distance(position, path[0]);
        if (startDistance < bestDistance) {
            bestIndex = 0;
            bestDistance = startDistance;
        }
        for (int i = 1; i < path.Count; i++) {
            float distance = HandleUtility.DistancePointLine(position, path[i - 1], path[i]);
            if (distance < bestDistance) {
                bestDistance = distance;
                bestIndex = i;
            }
        }
        return bestIndex;
    }

    private static void RebuildVisuals() {
        TDMap map = MapAuthoring.FindMap();
        if (map) {
            map.RebuildRoadVisuals();
        }
        SceneView.RepaintAll();
    }
}
// AI-assisted code end
