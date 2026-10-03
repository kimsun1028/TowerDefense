// AI-assisted code start
using UnityEditor;
using UnityEngine;

/// <summary>Tools > TD > Map > Saved Maps : 맵 저장 / 저장한 맵 불러오기 창.</summary>
public class MapSnapshotWindow : EditorWindow {
    // 1. 상수
    private const string DEFAULT_SOURCE_PATH = "Assets/TowerDefense/Prefabs/Map/Map01.prefab";
    private const float BUTTON_WIDTH = 90f;

    // 2. 필드
    private GameObject source;
    private string snapshotName = "Map01_Saved";
    private Vector2 scroll;

    // 4. 메서드 (public)
    [MenuItem("Tools/TD/Map/Saved Maps")]
    public static void Open() {
        GetWindow<MapSnapshotWindow>("Saved Maps");
    }

    // 4. 메서드 (private)
    private void OnEnable() {
        if (source == null) {
            source = AssetDatabase.LoadAssetAtPath<GameObject>(DEFAULT_SOURCE_PATH);
        }
    }

    private void OnGUI() {
        EditorGUILayout.LabelField("맵 저장", EditorStyles.boldLabel);
        source = (GameObject)EditorGUILayout.ObjectField("저장할 맵 프리팹", source, typeof(GameObject), false);
        snapshotName = EditorGUILayout.TextField("이름", snapshotName);
        using (new EditorGUI.DisabledScope(source == null || string.IsNullOrWhiteSpace(snapshotName))) {
            if (GUILayout.Button("저장 (같은 이름은 덮어씀)")) {
                MapSnapshot.Save(source, snapshotName.Trim());
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("저장한 맵", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("불러오기 = 지금 열린 씬의 맵을 바꾼다 (Ctrl+Z로 되돌리기 가능).", MessageType.None);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (GameObject saved in MapSnapshot.FindSaved()) {
            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.ObjectField(saved, typeof(GameObject), false);
                if (GUILayout.Button("씬에 불러오기", GUILayout.Width(BUTTON_WIDTH))) {
                    MapSnapshot.Load(saved);
                }
            }
        }
        EditorGUILayout.EndScrollView();
    }
}
// AI-assisted code end
