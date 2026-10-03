// AI-assisted code start
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 맵 오브젝트(경로/노드/슬롯) 생성과 경로 SO 굽기 공용 헬퍼. Map01Builder와 Map Editor 창이 같이 쓴다.
/// 모든 생성은 Undo에 등록된다.
/// </summary>
public static class MapAuthoring {
    // 1. 상수
    public const string ART_DIR = "Assets/TowerDefense/Art/Map";
    public const string DATA_DIR = "Assets/TowerDefense/Data/Map";

    private const string PATHS_ROOT_NAME = "Paths";
    private const string SLOTS_ROOT_NAME = "TowerSlots";
    private const string LABEL_NAME = "Label";
    private const string ROAD_MATERIAL_PATH = ART_DIR + "/Road.mat";
    private const string PAD_SPRITE_PATH = ART_DIR + "/Pad.png";
    private const string ROAD_SHADER = "Universal Render Pipeline/2D/Sprite-Unlit-Default";
    private const string FALLBACK_SHADER = "Sprites/Default";
    private const string LABEL_FONT = "LegacyRuntime.ttf";
    private const float PAD_PIXELS_PER_UNIT = 60f;   // 픽셀아트 1px = 1텍셀 (맵과 같은 밀도)
    private const int PAD_SORTING_ORDER = -6;
    private const int LABEL_SORTING_ORDER = -4;
    private const float SLOT_LABEL_CHAR_SIZE = 0.045f;
    private const float SLOT_LABEL_OFFSET_Y = 0.02f;
    private const int LABEL_FONT_SIZE = 48;
    private const int GENERATED_SPRITE_SIZE = 64;
    private const float HALF = 0.5f;
    private const float POSITION_SQR_TOLERANCE = 1e-6f;

    // 2. 필드 (프로퍼티)
    public static Sprite Circle => CreateOrLoadSprite("Circle", true);
    public static Sprite Square => CreateOrLoadSprite("Square", false);

    /// <summary>타워 자리 패드 (픽셀아트). 없으면 원으로 대체.</summary>
    public static Sprite Pad => LoadSprite(PAD_SPRITE_PATH, PAD_PIXELS_PER_UNIT, filter: FilterMode.Point) ?? Circle;

    /// <summary>길/선 그리기용 머티리얼. 없으면 만든다.</summary>
    public static Material RoadMaterial {
        get {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ROAD_MATERIAL_PATH);
            if (material) {
                return material;
            }
            Directory.CreateDirectory(ART_DIR);
            Shader shader = Shader.Find(ROAD_SHADER) ?? Shader.Find(FALLBACK_SHADER);
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, ROAD_MATERIAL_PATH);
            return material;
        }
    }

    // 4. 메서드 (public)
    /// <summary>PNG를 스프라이트로 임포트해서 불러온다. readable = 픽셀 읽기 허용 (길 마스크용), filter = 픽셀아트는 Point.</summary>
    public static Sprite LoadSprite(string path, float pixelsPerUnit, bool readable = false, FilterMode filter = FilterMode.Bilinear) {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) {
            return null;
        }
        bool needsReimport = importer.textureType != TextureImporterType.Sprite
            || !Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit)
            || importer.isReadable != readable
            || importer.textureCompression != TextureImporterCompression.Uncompressed
            || importer.filterMode != filter;
        if (needsReimport) {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.isReadable = readable;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = filter;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    /// <summary>프리팹 편집 모드면 그 프리팹의 TDMap, 아니면 씬의 TDMap.</summary>
    public static TDMap FindMap() {
        PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null) {
            TDMap inPrefab = stage.prefabContentsRoot.GetComponentInChildren<TDMap>(true);
            if (inPrefab) {
                return inPrefab;
            }
        }
        return Object.FindAnyObjectByType<TDMap>();
    }

    public static Transform GetOrCreateChild(Transform parent, string childName) {
        Transform child = parent.Find(childName);
        if (child) {
            return child;
        }
        var childObject = new GameObject(childName);
        Undo.RegisterCreatedObjectUndo(childObject, "Create " + childName);
        childObject.transform.SetParent(parent, false);
        return childObject.transform;
    }

    // ── 경로 ──

    public static EnemyPath CreatePath(TDMap map, string pathName, Vector3[] worldPoints) {
        var pathObject = new GameObject(pathName);
        Undo.RegisterCreatedObjectUndo(pathObject, "Create Path");
        pathObject.transform.SetParent(GetOrCreateChild(map.transform, PATHS_ROOT_NAME), false);
        EnemyPath path = pathObject.AddComponent<EnemyPath>();
        foreach (Vector3 point in worldPoints) {
            AddNode(path, point, path.Count);
        }
        return path;
    }

    public static Waypoint AddNode(EnemyPath path, Vector3 worldPosition, int index) {
        Waypoint node = new GameObject("Node").AddComponent<Waypoint>();
        Undo.RegisterCreatedObjectUndo(node.gameObject, "Add Node");
        node.transform.SetParent(path.transform, false);
        node.transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);
        Undo.RecordObject(path, "Add Node");
        path.Nodes.Insert(Mathf.Clamp(index, 0, path.Count), node);
        RenameNodes(path);
        return node;
    }

    public static void RemoveNode(EnemyPath path, int index) {
        if (index < 0 || index >= path.Count) {
            return;
        }
        Waypoint node = path.Nodes[index];
        Undo.RecordObject(path, "Remove Node");
        path.Nodes.RemoveAt(index);
        if (node) {
            Undo.DestroyObjectImmediate(node.gameObject);
        }
        RenameNodes(path);
    }

    public static void RenameNodes(EnemyPath path) {
        for (int i = 0; i < path.Count; i++) {
            Waypoint node = path.Nodes[i];
            if (!node) {
                continue;
            }
            node.name = $"Node_{i}";
            node.transform.SetSiblingIndex(i);
        }
    }

    // ── 경로 SO 굽기 ──

    /// <summary>
    /// 맵의 모든 EnemyPath를 MapRouteData SO로 굽는다.
    /// map.RouteData가 있으면 그 에셋을 덮어쓰고(참조 유지), 없으면 Data 폴더에 새로 만든다.
    /// </summary>
    public static MapRouteData BakeRoutes(TDMap map) {
        map.Refresh();
        MapRouteData data = map.RouteData;
        if (data == null) {
            data = CreateOrLoadRouteAsset(map.gameObject.name);
            Undo.RecordObject(map, "Assign Route Data");
            map.RouteData = data;
            EditorUtility.SetDirty(map);
        }

        Undo.RecordObject(data, "Bake Routes");
        data.MapName = map.gameObject.name;
        var baked = new List<MapRouteData.Route>();
        foreach (EnemyPath path in map.Paths) {
            if (path == null) {
                continue;
            }
            var points = new List<Vector3>();
            foreach (Waypoint node in path.Nodes) {
                if (node) {
                    points.Add(node.transform.position);
                }
            }
            // 길 폭은 길 판정(길 그림 마스크)으로 측정
            baked.Add(MapRouteData.BakeRoute(path.name, points, data.SampleSpacing, map.IsOnRoad, map.RoadWidth * HALF));
        }
        data.SetRoutes(baked);
        data.SetSpawns(CreateSpawnData(map));
        data.SetGoal(map.Goal != null, map.Goal != null ? map.Goal.transform.position : Vector3.zero);
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssetIfDirty(data);
        return data;
    }

    /// <summary>SO 내용이 현재 경로와 다르면 이유를, 같으면 null.</summary>
    public static string FindRouteDataMismatch(TDMap map) {
        MapRouteData data = map.RouteData;
        if (data == null) {
            return "경로 SO(RouteData)가 없습니다. '경로 SO로 굽기'를 실행하세요.";
        }
        map.Refresh();
        if (data.Routes.Count != map.Paths.Count) {
            return "경로 SO의 경로 개수가 맵과 다릅니다. 다시 구우세요.";
        }
        foreach (EnemyPath path in map.Paths) {
            string issue = FindPathMismatch(path, data.GetRoute(path.name));
            if (issue != null) {
                return issue;
            }
        }
        string spawnIssue = FindSpawnMismatch(map, data);
        if (spawnIssue != null) {
            return spawnIssue;
        }
        if (map.transform.position != Vector3.zero) {
            return "맵이 원점에 있지 않습니다. 경로 SO는 월드 좌표라 맵을 (0,0,0)에 두어야 합니다.";
        }
        return null;
    }

    // ── 슬롯 ──

    public static TowerSlot CreateSlot(TDMap map, Vector3 worldPosition, int number) {
        var slotObject = new GameObject($"Slot_{number}");
        Undo.RegisterCreatedObjectUndo(slotObject, "Create Slot");
        slotObject.transform.SetParent(GetOrCreateChild(map.transform, SLOTS_ROOT_NAME), false);
        slotObject.transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);

        // 흙 패드 (길과 확실히 구분되는 진한 테두리)
        SpriteRenderer renderer = slotObject.AddComponent<SpriteRenderer>();
        renderer.sprite = Pad;
        renderer.sortingOrder = PAD_SORTING_ORDER;

        CapsuleCollider2D collider = slotObject.AddComponent<CapsuleCollider2D>();
        collider.direction = CapsuleDirection2D.Horizontal;
        collider.size = renderer.sprite.bounds.size;

        TowerSlot slot = slotObject.AddComponent<TowerSlot>();
        slot.SlotNumber = number;

        // 번호 표지판 (타워를 지으면 숨겨짐)
        AddLabel(slotObject.transform, number.ToString(), SLOT_LABEL_CHAR_SIZE, LABEL_SORTING_ORDER);
        slotObject.transform.Find(LABEL_NAME).localPosition = new Vector3(0f, SLOT_LABEL_OFFSET_Y, 0f);

        map.Refresh();
        AssignAutoRally(map, slot);
        return slot;
    }

    public static int GetNextSlotNumber(TDMap map) {
        map.Refresh();
        int max = 0;
        foreach (TowerSlot slot in map.Slots) {
            max = Mathf.Max(max, slot.SlotNumber);
        }
        return max + 1;
    }

    /// <summary>슬롯의 기본 집결지를 가장 가까운 길 지점으로 설정.</summary>
    public static bool AssignAutoRally(TDMap map, TowerSlot slot) {
        if (!map.TryGetDefaultRallyPoint(slot, out Vector2 rallyPoint)) {
            return false;
        }
        Undo.RecordObject(slot, "Auto Rally Point");
        slot.RallyOffset = rallyPoint - (Vector2)slot.transform.position;
        EditorUtility.SetDirty(slot);
        return true;
    }

    public static void AddLabel(Transform parent, string text, float characterSize, int sortingOrder) {
        var labelObject = new GameObject(LABEL_NAME);
        labelObject.transform.SetParent(parent, false);
        TextMesh textMesh = labelObject.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = LABEL_FONT_SIZE;
        textMesh.characterSize = characterSize;
        textMesh.color = Color.white;
        Font font = Resources.GetBuiltinResource<Font>(LABEL_FONT);
        textMesh.font = font;
        MeshRenderer meshRenderer = labelObject.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = font.material;
        meshRenderer.sortingOrder = sortingOrder;
    }

    // 4. 메서드 (private)
    private static MapRouteData CreateOrLoadRouteAsset(string mapName) {
        Directory.CreateDirectory(DATA_DIR);
        string assetPath = $"{DATA_DIR}/{mapName}_Routes.asset";
        MapRouteData data = AssetDatabase.LoadAssetAtPath<MapRouteData>(assetPath);
        if (data == null) {
            data = ScriptableObject.CreateInstance<MapRouteData>();
            AssetDatabase.CreateAsset(data, assetPath);
        }
        return data;
    }

    private static List<MapRouteData.Spawn> CreateSpawnData(TDMap map) {
        var result = new List<MapRouteData.Spawn>();
        foreach (SpawnPoint spawn in map.Spawns) {
            if (spawn == null) {
                continue;
            }
            result.Add(new MapRouteData.Spawn(spawn.SpawnId, spawn.transform.position,
                new List<string>(spawn.RouteNames), spawn.WaveIconPosition));
        }
        return result;
    }

    private static string FindSpawnMismatch(TDMap map, MapRouteData data) {
        if (data.Spawns.Count != map.Spawns.Count) {
            return "경로 SO의 입구 개수가 맵과 다릅니다. 다시 구우세요.";
        }
        foreach (SpawnPoint spawn in map.Spawns) {
            MapRouteData.Spawn baked = data.GetSpawn(spawn.SpawnId);
            bool same = baked != null
                && (baked.Position - spawn.transform.position).sqrMagnitude <= POSITION_SQR_TOLERANCE
                && ((Vector2)baked.WaveIconPosition - spawn.WaveIconPosition).sqrMagnitude <= POSITION_SQR_TOLERANCE
                && baked.RouteNames.Count == spawn.RouteNames.Count;
            for (int i = 0; same && i < spawn.RouteNames.Count; i++) {
                same = baked.RouteNames[i] == spawn.RouteNames[i];
            }
            if (!same) {
                return $"입구 {spawn.SpawnId}: 경로 SO와 다릅니다. 다시 구우세요.";
            }
        }
        bool goalSame = data.HasGoal == (map.Goal != null)
            && (map.Goal == null || (data.GoalPosition - map.Goal.transform.position).sqrMagnitude <= POSITION_SQR_TOLERANCE);
        return goalSame ? null : "지키는 곳 위치가 경로 SO와 다릅니다. 다시 구우세요.";
    }

    private static string FindPathMismatch(EnemyPath path, MapRouteData.Route route) {
        if (route == null) {
            return $"경로 SO에 {path.name}이(가) 없습니다. 다시 구우세요.";
        }
        if (route.Waypoints.Count != path.Count) {
            return $"{path.name}: 노드 개수가 경로 SO와 다릅니다. 다시 구우세요.";
        }
        if (route.Distances.Count != route.Points.Count || route.LeftWidth.Count != route.Points.Count) {
            return $"{path.name}: 예전 형식의 경로 SO입니다 (길 폭 데이터 없음). 다시 구우세요.";
        }
        for (int i = 0; i < path.Count; i++) {
            Waypoint node = path.Nodes[i];
            if (node && (route.Waypoints[i] - node.transform.position).sqrMagnitude > POSITION_SQR_TOLERANCE) {
                return $"{path.name}: 노드 위치가 경로 SO와 다릅니다. 다시 구우세요.";
            }
        }
        return null;
    }

    private static Sprite CreateOrLoadSprite(string spriteName, bool circle) {
        string path = $"{ART_DIR}/{spriteName}.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing) {
            return existing;
        }

        Directory.CreateDirectory(ART_DIR);
        int size = GENERATED_SPRITE_SIZE;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radius = size * HALF;
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float alpha = 1f;
                if (circle) {
                    float distance = Vector2.Distance(new Vector2(x + HALF, y + HALF), new Vector2(radius, radius));
                    alpha = Mathf.Clamp01(radius - distance);
                }
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = size;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
// AI-assisted code end
