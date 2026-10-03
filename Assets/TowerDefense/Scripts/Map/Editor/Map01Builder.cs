// AI-assisted code start
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tools > TD > Map > Build Map01 : 참고 이미지(820x622 px) 레이아웃으로 맵 프리팹을 만든다.
///   Prefabs/Map/Map01.prefab        ← 팀원은 이 프리팹을 자기 씬에 넣기만 하면 된다
///   Data/Map/Map01_Routes.asset      ← 경로 SO (함께 구워진다)
///   Scenes/Map01_View.unity          ← 맵만 띄워보는 확인용 씬 (카메라 + 맵)
/// 길 모양은 Map01_Road.png(참고 이미지의 흙길 마스크), 경로/타워 자리는 같은 이미지의 px 좌표.
/// 주의: 다시 실행하면 프리팹을 새로 만들어 Map Editor로 고친 내용이 사라진다.
/// </summary>
public static class Map01Builder {
    // 1. 상수
    private const string PREFAB_PATH = "Assets/TowerDefense/Prefabs/Map/Map01.prefab";
    private const string VIEW_SCENE_PATH = "Assets/TowerDefense/Scenes/Map01_View.unity";
    private const string ROAD_TEXTURE_PATH = "Assets/TowerDefense/Art/Map/Map01_Road.png";
    private const string MAP_NAME = "Map01";

    // 이미지 크기와 스케일: 60px = 1 unit → 맵 13.7 x 10.4 unit
    private const float IMAGE_WIDTH = 820f;
    private const float IMAGE_HEIGHT = 622f;
    private const float PIXELS_PER_UNIT = 60f;
    // 길 그림은 좌우로 160px씩 연장돼 있다 (넓은 화면에서도 길이 화면 밖까지 이어지도록)
    private const float ROAD_PAD_PX = 160f;
    private const float HALF = 0.5f;

    // 경로 끝 연장 (화면 밖에서 걸어 들어와 화면 밖으로 나가도록)
    private const float EDGE_TOLERANCE_PX = 10f;
    private const float TOP_ENTRY_MAX_Y_PX = 15f;
    private const float TOP_OFFSCREEN_Y_PX = -40f;
    private const float SIDE_OFFSCREEN_INSET_PX = 20f;

    private const int ROAD_SORTING_ORDER = -10;
    private const int MARKER_SORTING_ORDER = -8;
    private const int MARKER_LABEL_SORTING_ORDER = -7;
    private const float MARKER_LABEL_SIZE = 0.07f;
    private const float GOAL_MARKER_SIZE = 0.8f;
    private const float SPAWN_MARKER_SIZE = 0.6f;
    private const float CAMERA_Z = -10f;
    private static readonly Color GRASS_COLOR = new(0.42f, 0.62f, 0.27f);
    private static readonly Color GOAL_COLOR = new(0.25f, 0.45f, 0.9f, 0.85f);
    private static readonly Color SPAWN_COLOR = new(0.7f, 0.15f, 0.15f, 0.85f);

    // 카메라가 꽉 채워 보여줄 영역 (이미지 px). 위 입구 ~ 아래 패드까지
    private static readonly Rect VIEW_PX = Rect.MinMaxRect(0, 15, 820, 560);

    // ── 맵 데이터 (이미지 픽셀 좌표) ─────────────────────────────
    private static readonly Vector2 GOAL_PX = new(10, 378);
    private static readonly Vector2 SPAWN_TOP_PX = new(530, 48);
    private static readonly Vector2 SPAWN_RIGHT_PX = new(792, 255);

    // Path_TopMiddle : 위 입구 → 가운데 길 → 출구
    // Path_TopLoop   : 위 입구 → 큰 섬 위/왼쪽으로 크게 돌아 → 출구
    // Path_Right     : 오른쪽 진영 → S자로 아래 섬을 돌아 → 출구
    private static readonly Dictionary<string, Vector2[]> PATHS_PX = new() {
        ["Path_TopMiddle"] = new Vector2[] { new(530.0f, 10.0f), new(530.0f, 36.2f), new(528.2f, 69.8f), new(519.1f, 97.6f), new(504.2f, 120.0f), new(487.9f, 137.9f), new(471.7f, 153.4f), new(460.8f, 172.9f), new(453.7f, 197.6f), new(446.4f, 231.4f), new(442.6f, 251.6f), new(435.9f, 282.0f), new(426.2f, 305.8f), new(414.8f, 320.2f), new(397.6f, 333.5f), new(372.0f, 339.8f), new(339.1f, 342.1f), new(321.2f, 345.2f), new(302.4f, 349.6f), new(282.6f, 355.4f), new(262.2f, 358.9f), new(241.2f, 360.3f), new(219.6f, 359.5f), new(197.4f, 356.5f), new(173.3f, 354.5f), new(147.4f, 353.5f), new(119.8f, 353.5f), new(90.2f, 354.5f), new(65.6f, 356.8f), new(45.7f, 360.2f), new(20.4f, 371.0f), new(5.0f, 380.0f) },
        ["Path_TopLoop"] = new Vector2[] { new(530.0f, 10.0f), new(530.0f, 36.2f), new(527.2f, 69.5f), new(513.5f, 95.8f), new(490.3f, 114.4f), new(462.4f, 123.5f), new(428.1f, 124.6f), new(406.9f, 123.9f), new(382.9f, 122.2f), new(356.1f, 119.8f), new(331.5f, 117.9f), new(309.0f, 116.8f), new(288.6f, 116.4f), new(270.4f, 116.6f), new(240.1f, 121.6f), new(218.0f, 132.6f), new(203.8f, 148.9f), new(197.4f, 170.1f), new(195.9f, 188.3f), new(201.8f, 206.2f), new(221.6f, 222.8f), new(239.8f, 232.8f), new(260.2f, 241.6f), new(278.4f, 259.3f), new(292.9f, 289.2f), new(301.5f, 313.2f), new(294.4f, 336.9f), new(272.1f, 352.0f), new(239.5f, 359.8f), new(219.6f, 359.5f), new(197.4f, 356.5f), new(173.3f, 354.5f), new(147.4f, 353.5f), new(119.8f, 353.5f), new(90.2f, 354.5f), new(65.6f, 356.8f), new(45.7f, 360.2f), new(20.4f, 371.0f), new(5.0f, 380.0f) },
        ["Path_Right"] = new Vector2[] { new(815.0f, 255.0f), new(790.8f, 254.8f), new(766.6f, 254.6f), new(734.4f, 254.4f), new(705.6f, 254.2f), new(680.4f, 254.1f), new(658.6f, 254.0f), new(640.4f, 254.0f), new(607.2f, 254.0f), new(578.6f, 254.0f), new(556.8f, 258.1f), new(537.6f, 276.6f), new(532.6f, 304.5f), new(532.4f, 323.5f), new(536.2f, 350.8f), new(553.6f, 368.4f), new(576.5f, 376.6f), new(604.2f, 386.8f), new(623.0f, 407.4f), new(632.8f, 434.2f), new(633.5f, 454.2f), new(622.2f, 474.9f), new(599.2f, 489.4f), new(558.5f, 494.1f), new(529.5f, 494.9f), new(503.2f, 494.9f), new(479.6f, 494.3f), new(458.6f, 493.0f), new(440.4f, 491.0f), new(419.1f, 488.1f), new(394.7f, 484.4f), new(367.2f, 479.8f), new(336.8f, 474.2f), new(313.1f, 469.4f), new(286.1f, 461.5f), new(274.5f, 441.1f), new(263.6f, 408.4f), new(248.8f, 380.9f), new(229.4f, 365.4f), new(207.2f, 356.8f), new(182.8f, 353.5f), new(145.0f, 353.0f), new(119.8f, 353.5f), new(90.2f, 354.5f), new(65.6f, 356.8f), new(45.7f, 360.2f), new(20.4f, 371.0f), new(5.0f, 380.0f) },
    };

    // 타워 자리 (흙 패드 중심)
    private static readonly Vector2[] SLOTS_PX = { new(258f, 175f), new(325f, 173f), new(387f, 177f), new(335f, 228f), new(366f, 279f), new(234f, 289f), new(469f, 364f), new(320f, 406f), new(384f, 406f), new(443f, 430f), new(510f, 431f), new(577f, 432f), new(585f, 310f), new(358f, 524f), new(595f, 184f), new(527f, 182f) };

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

        CreateRoad(map);
        foreach (KeyValuePair<string, Vector2[]> entry in PATHS_PX) {
            MapAuthoring.CreatePath(map, entry.Key, ConvertToWorld(ExtendEnds(entry.Value)));
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

    /// <summary>잔디 배경 + 맵 영역에 맞추는 카메라.</summary>
    public static Camera CreateCamera() {
        var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
        cameraObject.transform.position = new Vector3(0f, 0f, CAMERA_Z);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = GRASS_COLOR;
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

    /// <summary>길 그림 = 길 판정 마스크.</summary>
    private static void CreateRoad(TDMap map) {
        Sprite roadSprite = MapAuthoring.LoadSprite(ROAD_TEXTURE_PATH, PIXELS_PER_UNIT, readable: true);
        var roadObject = new GameObject("Road");
        roadObject.transform.SetParent(map.transform, false);
        SpriteRenderer renderer = roadObject.AddComponent<SpriteRenderer>();
        renderer.sprite = roadSprite;
        renderer.sortingOrder = ROAD_SORTING_ORDER;

        map.RoadMask = roadSprite.texture;
        Vector3 maskMin = ConvertPxToWorld(-ROAD_PAD_PX, IMAGE_HEIGHT);
        Vector3 maskMax = ConvertPxToWorld(IMAGE_WIDTH + ROAD_PAD_PX, 0f);
        map.RoadMaskRect = Rect.MinMaxRect(maskMin.x, maskMin.y, maskMax.x, maskMax.y);
        map.DrawRoadLines = false;
    }

    private static void CreateMarkers(TDMap map) {
        Transform markers = MapAuthoring.GetOrCreateChild(map.transform, "Markers");
        Sprite circle = MapAuthoring.Circle;
        map.Goal = CreateMarker("Goal (지키는곳)", circle, GOAL_COLOR, GOAL_PX, GOAL_MARKER_SIZE, "지키는곳", markers);
        CreateMarker("Spawn_Top", circle, SPAWN_COLOR, SPAWN_TOP_PX, SPAWN_MARKER_SIZE, "적", markers);
        CreateMarker("Spawn_Right", circle, SPAWN_COLOR, SPAWN_RIGHT_PX, SPAWN_MARKER_SIZE, "적", markers);
    }

    private static void CreateViewScene(GameObject mapPrefab) {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera();
        PrefabUtility.InstantiatePrefab(mapPrefab);
        EditorSceneManager.SaveScene(scene, VIEW_SCENE_PATH);
        AssetDatabase.SaveAssets();
    }

    /// <summary>시작/끝 노드를 화면 밖으로 연장: 적이 화면 밖에서 걸어 들어와 화면 밖으로 나간다.</summary>
    private static Vector2[] ExtendEnds(Vector2[] pixels) {
        var extended = new List<Vector2>(pixels);
        Vector2 first = pixels[0];
        Vector2 last = pixels[pixels.Length - 1];
        if (first.x >= IMAGE_WIDTH - EDGE_TOLERANCE_PX) {
            extended.Insert(0, new Vector2(IMAGE_WIDTH + ROAD_PAD_PX - SIDE_OFFSCREEN_INSET_PX, first.y)); // 오른쪽 진영
        } else if (first.y <= TOP_ENTRY_MAX_Y_PX) {
            extended.Insert(0, new Vector2(first.x, TOP_OFFSCREEN_Y_PX)); // 위 입구
        }
        if (last.x <= EDGE_TOLERANCE_PX) {
            extended.Add(new Vector2(-ROAD_PAD_PX + SIDE_OFFSCREEN_INSET_PX, last.y)); // 왼쪽 출구
        }
        return extended.ToArray();
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
