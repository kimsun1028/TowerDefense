// AI-assisted code start
using UnityEditor;
using UnityEngine;

/// <summary>
/// TowerSlot 선택 시 씬 뷰에서 기본 집결지(주황 깃발)와 범위를 드래그로 편집.
/// 길 밖이나 범위 밖으로 끌면 빨간색으로 경고한다.
/// </summary>
[CustomEditor(typeof(TowerSlot)), CanEditMultipleObjects]
public class TowerSlotEditor : Editor {
    // 1. 상수
    private const float RANGE_HANDLE_ALPHA = 0.35f;
    private const float DOTTED_LINE_SPACE = 3f;
    private const float RALLY_HANDLE_SIZE = 0.09f;
    private const float LABEL_OFFSET_SCALE = 2f;
    private static readonly Color OK_COLOR = new(1f, 0.6f, 0f);
    private static readonly Color ERROR_COLOR = Color.red;

    // 4. 메서드 (public)
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        TDMap map = MapAuthoring.FindMap();
        if (!map) {
            EditorGUILayout.HelpBox("씬에 TDMap이 없습니다.", MessageType.Warning);
            return;
        }

        if (targets.Length == 1) {
            string issue = MapValidator.CheckRally(map, (TowerSlot)target);
            if (issue != null) {
                EditorGUILayout.HelpBox(issue, MessageType.Warning);
            }
        }

        if (GUILayout.Button("집결지 자동 계산 (가장 가까운 길)")) {
            map.Refresh();
            foreach (Object selected in targets) {
                MapAuthoring.AssignAutoRally(map, (TowerSlot)selected);
            }
        }
    }

    // 4. 메서드 (private)
    private void OnSceneGUI() {
        var slot = (TowerSlot)target;
        TDMap map = MapAuthoring.FindMap();
        Vector3 center = slot.transform.position;
        Vector3 rallyPoint = slot.RallyPointWorld;
        bool valid = map == null || MapValidator.CheckRally(map, slot) == null;
        Color color = valid ? OK_COLOR : ERROR_COLOR;

        DrawRangeHandle(slot, center, color);
        DrawRallyHandle(slot, center, rallyPoint, color);
    }

    private static void DrawRangeHandle(TowerSlot slot, Vector3 center, Color color) {
        Handles.color = new Color(color.r, color.g, color.b, RANGE_HANDLE_ALPHA);
        EditorGUI.BeginChangeCheck();
        float radius = Handles.RadiusHandle(Quaternion.identity, center, slot.RallyRange);
        if (EditorGUI.EndChangeCheck()) {
            Undo.RecordObject(slot, "Rally Range");
            slot.RallyRange = radius;
        }
    }

    private static void DrawRallyHandle(TowerSlot slot, Vector3 center, Vector3 rallyPoint, Color color) {
        Handles.color = color;
        Handles.DrawDottedLine(center, rallyPoint, DOTTED_LINE_SPACE);
        float size = HandleUtility.GetHandleSize(rallyPoint) * RALLY_HANDLE_SIZE;
        EditorGUI.BeginChangeCheck();
        Vector3 moved = Handles.FreeMoveHandle(rallyPoint, size, Vector3.zero, Handles.CircleHandleCap);
        if (EditorGUI.EndChangeCheck()) {
            Undo.RecordObject(slot, "Move Rally Point");
            slot.RallyOffset = (Vector2)(moved - center);
        }
        Handles.Label(rallyPoint + Vector3.up * size * LABEL_OFFSET_SCALE, "집결지");
    }
}
// AI-assisted code end
