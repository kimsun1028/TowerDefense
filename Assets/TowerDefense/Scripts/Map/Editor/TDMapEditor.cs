// AI-assisted code start
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>TDMap 인스펙터: 경로 SO 상태 표시 + 굽기 버튼.</summary>
[CustomEditor(typeof(TDMap))]
public class TDMapEditor : Editor {
    // 1. 상수
    private const string WINDOW_TITLE = "Map Editor";

    // 4. 메서드 (public)
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        var map = (TDMap)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("경로 데이터 (SO)", EditorStyles.boldLabel);
        string mismatch = MapAuthoring.FindRouteDataMismatch(map);
        if (mismatch != null) {
            EditorGUILayout.HelpBox(mismatch, MessageType.Warning);
        } else {
            EditorGUILayout.HelpBox(CreateSummary(map.RouteData), MessageType.Info);
        }

        using (new EditorGUILayout.HorizontalScope()) {
            if (GUILayout.Button("경로 SO로 굽기")) {
                EditorGUIUtility.PingObject(MapAuthoring.BakeRoutes(map));
            }
            if (GUILayout.Button("Map Editor 열기")) {
                EditorWindow.GetWindow<MapEditorWindow>(WINDOW_TITLE);
            }
        }
    }

    // 4. 메서드 (private)
    private static string CreateSummary(MapRouteData data) {
        var summary = new StringBuilder($"최신 상태 · 경로 {data.Routes.Count}개 · 간격 {data.SampleSpacing}");
        foreach (MapRouteData.Route route in data.Routes) {
            summary.Append($"\n  {route.Name}: 노드 {route.Waypoints.Count} / 등간격 {route.Points.Count} / 길이 {route.Length:F2}");
        }
        return summary.ToString();
    }
}
// AI-assisted code end
