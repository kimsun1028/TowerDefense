// AI-assisted code start
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tools > TD > Build Game Scene
/// 실제로 플레이하는 게임 씬(Scenes/Game.unity)을 만든다. 빌드 설정은 메인 화면(0번) → 게임 씬(1번).
///   맵: Map01 프리팹 + 카메라(CameraFit)
///   게임: 타워 팀 GameManager · TowerBuilder(타워 4종), 집결지 도구, WaveSpawner(15웨이브 + 보스)
///   UI: GameHud, InfoPanel, EventSystem + 연결(GameHudBinding, TowerBuildMenuAdapter)
/// 메인 화면 씬도 함께 다시 만든다 ("게임 시작" → 이 씬).
/// 팀 코드(Assembly-CSharp)와 우리 모듈(TD.*)을 둘 다 써야 해서 asmdef 없는 Editor 폴더에 둔다.
/// </summary>
public static class GameSceneBuilder {
    // 1. 상수
    private const string SCENE_PATH = "Assets/TowerDefense/Scenes/" + MainMenu.DEFAULT_GAME_SCENE + ".unity";
    private const string MAIN_MENU_PATH = "Assets/TowerDefense/Scenes/" + MainMenu.SCENE_NAME + ".unity";
    private const string MAP_PREFAB_PATH = "Assets/TowerDefense/Prefabs/Map/Map01.prefab";
    private const string TOWER_PREFAB_DIR = "Assets/TowerDefense/Prefabs/Tower";
    private const string ENEMY_PREFAB_DIR = "Assets/TowerDefense/Prefabs/Enemy";
    private const string GAME_HUD_PREFAB_PATH = "Assets/TowerDefense/Prefabs/UI/GameHud.prefab";
    private const string INFO_PANEL_PREFAB_PATH = "Assets/TowerDefense/Prefabs/UI/InfoPanel.prefab";
    private const string GAME_ROOT_NAME = "Game";
    private const string TOP = "Top";
    private const string RIGHT = "Right";
    private const string BOSS_ID = "Veznan";

    // TowerBuilder.towerPrefabs 순서 = 0 아처 / 1 마법사 / 2 포병 / 3 병영 (타워↔UI 약속)
    private static readonly (string prefab, string label, string icon)[] TOWERS = {
        ("ArcherTower", "궁수 타워", "Arrow"), ("MagicTower", "마법 타워", "Crystal"),
        ("ArtilleryTower", "포병 타워", "Bolt"), ("BarracksTower", "병영", "Shield"),
    };

    // 입구 ID → 경로 (위 입구는 갈림길이라 두 경로 중 랜덤)
    private static readonly (string id, string[] routes)[] SPAWNS = {
        (TOP, new[] { "Path_TopMiddle", "Path_TopLoop" }), (RIGHT, new[] { "Path_Right" }),
    };

    /// <summary>웨이브 무리 한 줄: 적 ID, 입구, 마리 수, 간격(초), 시작 지연(초), 무리 크기.</summary>
    private readonly struct Group {
        // 2. 필드
        public readonly string Enemy;
        public readonly string Spawn;
        public readonly int Count;
        public readonly float Interval;
        public readonly float Delay;
        public readonly int Pack;

        // 3. 생성자
        public Group(string enemy, string spawn, int count, float interval, float delay = 0f, int pack = 1) {
            Enemy = enemy;
            Spawn = spawn;
            Count = count;
            Interval = interval;
            Delay = delay;
            Pack = pack;
        }
    }

    // 15웨이브: 약한 적 → 특수 적(회피·마저·비행·원거리·중갑) 순으로 섞어서 늘린다. 15웨이브 뒤 베즈난(위 입구)
    private static readonly Group[][] WAVES = {
        new Group[] { new("Goblin", TOP, 6, 1.2f, pack: 3) },
        new Group[] { new("Goblin", RIGHT, 6, 1.2f, pack: 3), new("Goblin", TOP, 4, 1.2f, 3f, 2) },
        new Group[] { new("Wolf", RIGHT, 4, 1.4f, pack: 2), new("Goblin", TOP, 6, 1.0f, pack: 3) },
        new Group[] { new("Grunt", TOP, 6, 1.4f, pack: 2), new("Wolf", RIGHT, 4, 1.4f, 2f, 2) },
        new Group[] { new("Spider", TOP, 3, 1.8f), new("Goblin", RIGHT, 8, 1.0f, pack: 4) },
        new Group[] { new("Gargoyle", RIGHT, 3, 1.6f), new("Grunt", TOP, 8, 1.2f, pack: 4) },
        new Group[] { new("ShadowArcher", RIGHT, 2, 2.0f), new("Wolf", TOP, 6, 1.2f, 2f, 3) },
        new Group[] { new("Armored", TOP, 3, 2.0f), new("Spider", RIGHT, 4, 1.6f), new("Goblin", TOP, 10, 0.8f, 4f, 5) },
        new Group[] { new("Gargoyle", TOP, 4, 1.4f), new("Runner", RIGHT, 10, 0.8f, pack: 5) },
        new Group[] { new("ShadowArcher", TOP, 3, 1.8f), new("DarkKnight", RIGHT, 2, 2.5f), new("Wolf", RIGHT, 6, 1.0f, 3f, 3) },
        new Group[] { new("Spider", TOP, 6, 1.4f, pack: 2), new("Gargoyle", RIGHT, 5, 1.4f) },
        new Group[] { new("ShadowArcher", RIGHT, 4, 1.6f), new("Grunt", TOP, 12, 1.0f, pack: 4), new("Runner", TOP, 8, 0.8f, 5f, 4) },
        new Group[] { new("DarkKnight", TOP, 4, 2.0f), new("Spider", RIGHT, 6, 1.4f, pack: 2), new("Gargoyle", TOP, 5, 1.4f, 4f) },
        new Group[] { new("ShadowArcher", TOP, 4, 1.6f), new("ShadowArcher", RIGHT, 4, 1.6f), new("Wolf", TOP, 10, 0.8f, pack: 5) },
        new Group[] {
            new("DarkKnight", RIGHT, 4, 1.8f, pack: 2), new("Spider", TOP, 8, 1.2f, pack: 2), new("Gargoyle", RIGHT, 6, 1.2f, 3f),
            new("ShadowArcher", TOP, 5, 1.4f, 5f), new("Goblin", RIGHT, 16, 0.6f, 2f, 4),
        },
    };

    // 4. 메서드 (public)
    [MenuItem("Tools/TD/Build Game Scene")]
    public static void Build() {
        var mapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MAP_PREFAB_PATH);
        if (mapPrefab == null) {
            Map01Builder.Build();
            mapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MAP_PREFAB_PATH);
        }
        Directory.CreateDirectory(Path.GetDirectoryName(SCENE_PATH));
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 맵 + 카메라
        Map01Builder.CreateCamera();
        var map = ((GameObject)PrefabUtility.InstantiatePrefab(mapPrefab)).GetComponent<TDMap>();
        map.Refresh();

        // 게임 (타워 팀 GameManager · TowerBuilder + 우리 WaveSpawner · 집결지 도구)
        var gameRoot = new GameObject(GAME_ROOT_NAME);
        gameRoot.AddComponent<GameManager>();
        gameRoot.AddComponent<RallyPointTool>().LineMaterial = MapAuthoring.RoadMaterial;
        TowerBuilder builder = CreateTowerBuilder(gameRoot);
        CreateWaveSpawner(gameRoot, map);

        // UI + 연결
        GameObject hud = InstantiateUi(GAME_HUD_PREFAB_PATH, GameHudBuilder.Build);
        SetField(hud.GetComponentInChildren<RingMenu>(true), "closeOnOutsideClick", false); // 선택은 TowerBuilder가 정한다
        InstantiateUi(INFO_PANEL_PREFAB_PATH, InfoPanelBuilder.Build);
        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        gameRoot.AddComponent<GameHudBinding>();
        CreateMenuAdapter(gameRoot, builder);

        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(SCENE_PATH, true) };
        MainMenuBuilder.Build(); // 메인 화면을 0번에 넣고 게임 씬은 그 뒤
        EditorSceneManager.OpenScene(SCENE_PATH);
        AssetDatabase.SaveAssets();
        Debug.Log($"[GameSceneBuilder] {SCENE_PATH} 생성 완료 (빌드 설정: {MAIN_MENU_PATH} → {SCENE_PATH})");
    }

    // 4. 메서드 (private)
    private static TowerBuilder CreateTowerBuilder(GameObject root) {
        TowerBuilder builder = root.AddComponent<TowerBuilder>();
        var serialized = new SerializedObject(builder);
        SerializedProperty prefabs = serialized.FindProperty("towerPrefabs");
        prefabs.arraySize = TOWERS.Length;
        for (int i = 0; i < TOWERS.Length; i++) {
            var tower = AssetDatabase.LoadAssetAtPath<Tower>($"{TOWER_PREFAB_DIR}/{TOWERS[i].prefab}.prefab");
            if (tower == null) {
                Debug.LogError($"[GameSceneBuilder] 타워 프리팹이 없습니다: {TOWERS[i].prefab}");
            }
            prefabs.GetArrayElementAtIndex(i).objectReferenceValue = tower;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return builder;
    }

    private static void CreateMenuAdapter(GameObject root, TowerBuilder builder) {
        TowerBuildMenuAdapter adapter = root.AddComponent<TowerBuildMenuAdapter>();
        var serialized = new SerializedObject(adapter);
        serialized.FindProperty("builder").objectReferenceValue = builder;
        SerializedProperty buttons = serialized.FindProperty("towerButtons");
        buttons.arraySize = TOWERS.Length;
        for (int i = 0; i < TOWERS.Length; i++) {
            SerializedProperty button = buttons.GetArrayElementAtIndex(i);
            button.FindPropertyRelative("label").stringValue = TOWERS[i].label;
            button.FindPropertyRelative("icon").objectReferenceValue = UiArt.LoadIcon(TOWERS[i].icon);
            button.FindPropertyRelative("shortcut").stringValue = (i + 1).ToString(); // 숫자키 1~4
        }
        serialized.FindProperty("upgradeIcon").objectReferenceValue = UiArt.LoadIcon("Up");
        serialized.FindProperty("sellIcon").objectReferenceValue = UiArt.LoadIcon("Sell");
        serialized.FindProperty("rallyIcon").objectReferenceValue = UiArt.LoadIcon("Flag");
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateWaveSpawner(GameObject root, TDMap map) {
        WaveSpawner spawner = root.AddComponent<WaveSpawner>();
        spawner.RouteData = map.RouteData;
        var spawnPoints = new List<WaveSpawner.SpawnPointEntry>();
        foreach ((string id, string[] routes) in SPAWNS) {
            spawnPoints.Add(new WaveSpawner.SpawnPointEntry(id, new List<string>(routes)));
        }
        spawner.SetSpawnPoints(spawnPoints);

        var waves = new List<WaveSpawner.Wave>();
        foreach (Group[] groups in WAVES) {
            var spawnGroups = new List<WaveSpawner.SpawnGroup>();
            foreach (Group group in groups) {
                spawnGroups.Add(new WaveSpawner.SpawnGroup(LoadEnemy(group.Enemy), group.Spawn, group.Count,
                    group.Interval, group.Delay, group.Pack));
            }
            waves.Add(new WaveSpawner.Wave(spawnGroups));
        }
        spawner.SetWaves(waves);
        spawner.SetBoss(LoadEnemy(BOSS_ID), TOP);
    }

    private static Enemy LoadEnemy(string id) {
        var enemy = AssetDatabase.LoadAssetAtPath<Enemy>($"{ENEMY_PREFAB_DIR}/Enemy_{id}.prefab");
        if (enemy == null) {
            Debug.LogError($"[GameSceneBuilder] 적 프리팹이 없습니다: Enemy_{id}");
        }
        return enemy;
    }

    /// <summary>UI 프리팹이 있으면 그대로, 없으면 빌더로 만든 뒤 씬에 넣는다.</summary>
    private static GameObject InstantiateUi(string path, System.Func<GameObject> build) {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) {
            prefab = build();
        }
        return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
    }

    private static void SetField(Object target, string field, bool value) {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).boolValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
// AI-assisted code end
