// AI-assisted code start
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tools > TD > Map > Build Map01 : 16:9 화면(960x540 px) 레이아웃으로 맵 프리팹을 만든다.
///   Prefabs/Map/Map01.prefab        ← 팀원은 이 프리팹을 자기 씬에 넣기만 하면 된다
///   Data/Map/Map01_Routes.asset      ← 경로 SO (함께 구워진다)
///   Scenes/Map01_View.unity          ← 맵만 띄워보는 확인용 씬 (카메라 + 맵)
/// 그림(Art/Map)과 좌표(아래 맵 데이터 구역)는 Tools/gen_map01.py가 함께 만든다. 길 그림 = 경로 중심선을 일정 폭으로 그린 것.
/// 주의: 다시 실행하면 프리팹을 새로 만들어 Map Editor로 고친 내용이 사라진다.
/// </summary>
public static class Map01Builder {
    // 1. 상수
    private const string PREFAB_PATH = "Assets/TowerDefense/Prefabs/Map/Map01.prefab";
    private const string VIEW_SCENE_PATH = "Assets/TowerDefense/Scenes/Map01_View.unity";
    private const string ART_DIR = "Assets/TowerDefense/Art/Map";
    private const string ROAD_TEXTURE_PATH = ART_DIR + "/Map01_Road.png";
    private const string BACKGROUND_TEXTURE_PATH = ART_DIR + "/Map01_Background.png";
    private const string PORTAL_TOP_TEXTURE_PATH = ART_DIR + "/Portal_Top.png";
    private const string PORTAL_RIGHT_TEXTURE_PATH = ART_DIR + "/Portal_Right.png";
    private const string MAP_NAME = "Map01";

    // 스케일: 60px = 1 unit. 화면(VIEW_PX) 960x540 = 16 x 9 unit, 그림은 여백까지 포함
    private const float PIXELS_PER_UNIT = 60f;
    private const float HALF = 0.5f;

    private const int BACKGROUND_SORTING_ORDER = -20;
    private const int ROAD_SORTING_ORDER = -10;
    private const int PORTAL_SORTING_ORDER = -9;  // 길 위 → 길이 블랙홀에서 나오는 것처럼 보인다
    private const int MARKER_SORTING_ORDER = -8;
    private const int MARKER_LABEL_SORTING_ORDER = -7;
    private const float MARKER_LABEL_SIZE = 0.07f;
    private const float GOAL_MARKER_SIZE = 0.8f;
    private const float CAMERA_Z = -10f;
    private static readonly Color BACKGROUND_COLOR = new(0.1f, 0.075f, 0.18f); // 그림 바깥 (바닥 아래쪽 색)
    private static readonly Color GOAL_COLOR = new(0.25f, 0.45f, 0.9f, 0.85f);

    private const string SPAWN_TOP_ID = "Top";
    private const string SPAWN_RIGHT_ID = "Right";
    private const float GOAL_RADIUS = 0.6f;

    // Path_TopMiddle : 위 블랙홀 → 가운데 길 → 지키는 곳
    // Path_TopLoop   : 위 블랙홀 → 큰 섬 위/왼쪽으로 크게 돌아 → 지키는 곳
    // Path_Right     : 오른쪽 블랙홀 → S자로 아래 섬을 돌아 → 지키는 곳
    // ── 맵 데이터 (Tools/gen_map01.py가 만든다. 직접 고치지 말고 생성기를 고칠 것) ──
    private const float IMAGE_WIDTH = 1120f;
    private const float IMAGE_HEIGHT = 620f;
    private static readonly Rect VIEW_PX = Rect.MinMaxRect(80, 40, 1040, 580);
    private static readonly Vector2 GOAL_PX = new(172.0f, 381.8f);
    private static readonly Vector2 SPAWN_TOP_PX = new(666.0f, 98.0f);
    private static readonly Vector2 SPAWN_RIGHT_PX = new(984.0f, 293.0f);
    private static readonly Vector2 WAVE_ICON_TOP_PX = new(722.0f, 98.0f);
    private static readonly Vector2 WAVE_ICON_RIGHT_PX = new(984.0f, 237.0f);
    private static readonly Dictionary<string, Vector2[]> PATHS_PX = new() {
        ["Path_TopMiddle"] = new Vector2[] { new(666.0f, 98.0f), new(663.7f, 119.8f), new(657.2f, 140.7f), new(645.5f, 159.3f), new(630.2f, 175.0f), new(613.2f, 188.9f), new(596.0f, 202.5f), new(583.0f, 220.1f), new(574.7f, 240.4f), new(568.3f, 261.4f), new(562.6f, 282.6f), new(556.7f, 303.8f), new(548.8f, 324.3f), new(536.9f, 342.6f), new(519.7f, 356.2f), new(499.6f, 364.8f), new(477.9f, 368.6f), new(456.1f, 370.5f), new(434.2f, 372.0f), new(412.4f, 374.8f), new(390.8f, 378.7f), new(369.2f, 383.1f), new(347.5f, 386.1f), new(325.6f, 387.5f), new(303.6f, 387.4f), new(281.7f, 386.0f), new(259.8f, 384.1f), new(237.9f, 382.8f), new(215.9f, 382.0f), new(194.0f, 381.8f), new(172.0f, 381.8f) },
        ["Path_TopLoop"] = new Vector2[] { new(666.0f, 98.0f), new(662.5f, 119.9f), new(652.8f, 139.7f), new(636.8f, 155.0f), new(617.3f, 165.5f), new(596.0f, 171.8f), new(574.0f, 174.8f), new(551.8f, 175.5f), new(529.6f, 175.4f), new(507.4f, 174.5f), new(485.2f, 173.3f), new(463.1f, 171.8f), new(440.9f, 170.5f), new(418.7f, 169.4f), new(396.5f, 168.7f), new(374.3f, 168.4f), new(352.1f, 169.1f), new(330.0f, 171.6f), new(308.4f, 176.6f), new(288.3f, 186.0f), new(273.0f, 201.7f), new(265.9f, 222.7f), new(269.8f, 244.1f), new(286.5f, 258.4f), new(306.2f, 268.5f), new(327.0f, 276.2f), new(347.3f, 285.3f), new(364.2f, 299.5f), new(376.9f, 317.7f), new(387.3f, 337.3f), new(385.9f, 358.7f), new(369.8f, 373.5f), new(349.0f, 381.2f), new(327.2f, 385.4f), new(305.1f, 387.3f), new(282.9f, 386.1f), new(260.8f, 384.1f), new(238.6f, 382.8f), new(216.4f, 382.1f), new(194.2f, 381.8f), new(172.0f, 381.8f) },
        ["Path_Right"] = new Vector2[] { new(984.0f, 293.0f), new(961.9f, 292.8f), new(939.8f, 292.7f), new(917.6f, 292.6f), new(895.5f, 292.5f), new(873.4f, 292.4f), new(851.3f, 292.3f), new(829.2f, 292.2f), new(807.1f, 292.2f), new(784.9f, 292.2f), new(762.8f, 292.2f), new(740.7f, 292.2f), new(718.6f, 293.1f), new(697.1f, 297.9f), new(679.2f, 310.6f), new(670.5f, 330.6f), new(668.8f, 352.6f), new(672.9f, 374.2f), new(687.7f, 390.2f), new(707.9f, 398.9f), new(729.3f, 404.7f), new(750.3f, 411.5f), new(768.8f, 423.4f), new(782.3f, 440.8f), new(789.3f, 461.6f), new(782.7f, 482.2f), new(765.6f, 496.0f), new(744.8f, 503.2f), new(722.9f, 506.3f), new(700.8f, 507.9f), new(678.7f, 508.7f), new(656.6f, 509.0f), new(634.5f, 508.9f), new(612.4f, 508.6f), new(590.3f, 507.8f), new(568.2f, 506.3f), new(546.2f, 504.3f), new(524.2f, 501.9f), new(502.2f, 499.4f), new(480.3f, 496.7f), new(458.4f, 493.8f), new(436.5f, 490.7f), new(414.6f, 487.4f), new(392.9f, 483.3f), new(372.3f, 475.7f), new(358.7f, 458.5f), new(349.0f, 438.6f), new(337.7f, 419.7f), new(322.6f, 403.6f), new(303.4f, 392.8f), new(282.3f, 386.2f), new(260.4f, 382.9f), new(238.3f, 381.7f), new(216.2f, 381.4f), new(194.1f, 381.5f), new(172.0f, 381.8f) },
    };
    private static readonly Vector2[] SLOTS_PX = { new(349.0f, 223.0f), new(420.0f, 221.0f), new(494.0f, 225.0f), new(432.0f, 269.0f), new(469.0f, 315.0f), new(306.0f, 330.0f), new(592.0f, 391.0f), new(414.0f, 429.0f), new(490.0f, 429.0f), new(561.0f, 451.0f), new(642.0f, 452.0f), new(702.0f, 456.0f), new(734.0f, 345.0f), new(459.0f, 547.0f), new(744.0f, 229.0f), new(662.0f, 227.0f) };
    // ── 맵 데이터 끝 ──

    // 4. 메서드 (public)
    [MenuItem("Tools/TD/Map/Build Map01")]
    public static void Build() {
        Directory.CreateDirectory(Path.GetDirectoryName(PREFAB_PATH));
        Directory.CreateDirectory(Path.GetDirectoryName(VIEW_SCENE_PATH));

        TDMap map = new GameObject(MAP_NAME).AddComponent<TDMap>();
        map.RoadMaterial = MapAuthoring.RoadMaterial;
        Vector3 viewMin = ConvertPxToWorld(VIEW_PX.xMin, VIEW_PX.yMax);
        Vector3 viewMax = ConvertPxToWorld(VIEW_PX.xMax, VIEW_PX.yMin);
        map.ViewRect = Rect.MinMaxRect(viewMin.x, viewMin.y, viewMax.x, viewMax.y);

        CreateBackground(map);
        CreateRoad(map);
        foreach (KeyValuePair<string, Vector2[]> entry in PATHS_PX) {
            MapAuthoring.CreatePath(map, entry.Key, ConvertToWorld(entry.Value));
        }
        map.RebuildRoadVisuals();
        CreateMarkers(map);

        // 슬롯 생성 시 집결지가 가장 가까운 길로 자동 설정된다
        for (int i = 0; i < SLOTS_PX.Length; i++) {
            MapAuthoring.CreateSlot(map, ConvertPxToWorld(SLOTS_PX[i].x, SLOTS_PX[i].y), i + 1);
        }

        // 경로 SO 굽기 (기존 에셋이 있으면 덮어써서 참조 유지)
        MapAuthoring.BakeRoutes(map);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(map.gameObject, PREFAB_PATH);
        Object.DestroyImmediate(map.gameObject);
        CreateViewScene(prefab);
        Debug.Log($"[Map01Builder] {PREFAB_PATH} 생성 완료");
    }

    /// <summary>어두운 배경색 + 맵 영역에 맞추는 카메라.</summary>
    public static Camera CreateCamera() {
        var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
        cameraObject.transform.position = new Vector3(0f, 0f, CAMERA_Z);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = BACKGROUND_COLOR;
        cameraObject.AddComponent<CameraFit>().FitToMap = true;
        return camera;
    }

    // 4. 메서드 (private)
    /// <summary>이미지 중심이 월드 원점.</summary>
    private static Vector3 ConvertPxToWorld(float x, float y) {
        return new Vector3((x - IMAGE_WIDTH * HALF) / PIXELS_PER_UNIT, (IMAGE_HEIGHT * HALF - y) / PIXELS_PER_UNIT, 0f);
    }

    private static Vector3[] ConvertToWorld(Vector2[] pixels) {
        var world = new Vector3[pixels.Length];
        for (int i = 0; i < pixels.Length; i++) {
            world[i] = ConvertPxToWorld(pixels[i].x, pixels[i].y);
        }
        return world;
    }

    /// <summary>배경 그림 (기판, 장식). 길 그림과 같은 크기·위치. 없으면 건너뛴다.</summary>
    private static void CreateBackground(TDMap map) {
        Sprite backgroundSprite = MapAuthoring.LoadSprite(BACKGROUND_TEXTURE_PATH, PIXELS_PER_UNIT, filter: FilterMode.Point);
        if (backgroundSprite == null) {
            return;
        }
        var backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(map.transform, false);
        SpriteRenderer renderer = backgroundObject.AddComponent<SpriteRenderer>();
        renderer.sprite = backgroundSprite;
        renderer.sortingOrder = BACKGROUND_SORTING_ORDER;
    }

    /// <summary>길 그림 = 길 판정 마스크.</summary>
    private static void CreateRoad(TDMap map) {
        Sprite roadSprite = MapAuthoring.LoadSprite(ROAD_TEXTURE_PATH, PIXELS_PER_UNIT, readable: true, filter: FilterMode.Point);
        var roadObject = new GameObject("Road");
        roadObject.transform.SetParent(map.transform, false);
        SpriteRenderer renderer = roadObject.AddComponent<SpriteRenderer>();
        renderer.sprite = roadSprite;
        renderer.sortingOrder = ROAD_SORTING_ORDER;

        map.RoadMask = roadSprite.texture;
        Vector3 maskMin = ConvertPxToWorld(0f, IMAGE_HEIGHT);
        Vector3 maskMax = ConvertPxToWorld(IMAGE_WIDTH, 0f);
        map.RoadMaskRect = Rect.MinMaxRect(maskMin.x, maskMin.y, maskMax.x, maskMax.y);
        map.DrawRoadLines = false;
    }

    private static void CreateMarkers(TDMap map) {
        Transform markers = MapAuthoring.GetOrCreateChild(map.transform, "Markers");
        Transform goalMarker = CreateMarker("Goal (지키는곳)", MapAuthoring.Circle, GOAL_COLOR, GOAL_PX, GOAL_MARKER_SIZE, "지키는곳", markers);
        GoalPoint goal = goalMarker.gameObject.AddComponent<GoalPoint>();
        goal.Radius = GOAL_RADIUS;
        map.Goal = goal;

        // 입구 = 블랙홀 (길이 구멍에서 나온다). 위 입구는 갈림길 → 경로 2개, 오른쪽 입구 → 경로 1개
        CreateSpawn(CreatePortal("Spawn_Top", PORTAL_TOP_TEXTURE_PATH, SPAWN_TOP_PX, markers),
            SPAWN_TOP_ID, new List<string> { "Path_TopMiddle", "Path_TopLoop" }, WAVE_ICON_TOP_PX);
        CreateSpawn(CreatePortal("Spawn_Right", PORTAL_RIGHT_TEXTURE_PATH, SPAWN_RIGHT_PX, markers),
            SPAWN_RIGHT_ID, new List<string> { "Path_Right" }, WAVE_ICON_RIGHT_PX);
        map.Refresh();
    }

    private static Transform CreatePortal(string portalName, string texturePath, Vector2 pixel, Transform parent) {
        var portalObject = new GameObject(portalName);
        portalObject.transform.SetParent(parent);
        portalObject.transform.position = ConvertPxToWorld(pixel.x, pixel.y);
        SpriteRenderer renderer = portalObject.AddComponent<SpriteRenderer>();
        renderer.sprite = MapAuthoring.LoadSprite(texturePath, PIXELS_PER_UNIT, filter: FilterMode.Point) ?? MapAuthoring.Circle;
        renderer.sortingOrder = PORTAL_SORTING_ORDER;
        return portalObject.transform;
    }

    private static void CreateSpawn(Transform marker, string spawnId, List<string> routeNames, Vector2 waveIconPx) {
        SpawnPoint spawn = marker.gameObject.AddComponent<SpawnPoint>();
        spawn.SpawnId = spawnId;
        spawn.SetRouteNames(routeNames);
        var icon = new GameObject("WaveIcon");
        icon.transform.SetParent(marker, true);
        icon.transform.position = ConvertPxToWorld(waveIconPx.x, waveIconPx.y);
        spawn.WaveIcon = icon.transform;
    }

    private static void CreateViewScene(GameObject mapPrefab) {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera();
        PrefabUtility.InstantiatePrefab(mapPrefab);
        EditorSceneManager.SaveScene(scene, VIEW_SCENE_PATH);
        AssetDatabase.SaveAssets();
    }

    private static Transform CreateMarker(string markerName, Sprite sprite, Color color, Vector2 pixel, float size,
                                          string label, Transform parent) {
        var markerObject = new GameObject(markerName);
        markerObject.transform.SetParent(parent);
        markerObject.transform.position = ConvertPxToWorld(pixel.x, pixel.y);
        markerObject.transform.localScale = Vector3.one * size;
        SpriteRenderer renderer = markerObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = MARKER_SORTING_ORDER;
        MapAuthoring.AddLabel(markerObject.transform, label, MARKER_LABEL_SIZE / size, MARKER_LABEL_SORTING_ORDER);
        return markerObject.transform;
    }
}
// AI-assisted code end
