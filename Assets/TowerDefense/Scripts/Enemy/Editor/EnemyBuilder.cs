// AI-assisted code start
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > TD > Enemy > Build Placeholder Enemies
/// 적 종류별 스탯 SO(Data/Enemy)와 임시 그래픽 프리팹(Prefabs/Enemy)을 만든다.
/// 이미 있는 스탯 SO는 덮어쓰지 않는다 (밸런싱 값 보존). 프리팹은 다시 만든다.
/// </summary>
public static class EnemyBuilder {
    /// <summary>만들어진 적 프리팹들.</summary>
    public readonly struct Result {
        // 2. 필드
        private readonly Enemy grunt;
        private readonly Enemy runner;
        private readonly Enemy armored;

        public Enemy Grunt => grunt;
        public Enemy Runner => runner;
        public Enemy Armored => armored;

        // 3. 생성자
        public Result(Enemy gruntPrefab, Enemy runnerPrefab, Enemy armoredPrefab) {
            grunt = gruntPrefab;
            runner = runnerPrefab;
            armored = armoredPrefab;
        }
    }

    /// <summary>적 종류 하나의 기본값.</summary>
    private readonly struct EnemySpec {
        public string Id { get; }
        public string DisplayName { get; }
        public Color Color { get; }
        public float Size { get; }
        public float MaxHp { get; }
        public float Speed { get; }
        public float Armor { get; }
        public int GoldReward { get; }
        public int LifeDamage { get; }

        public EnemySpec(string id, string displayName, Color color, float size,
                         float maxHp, float speed, float armor, int goldReward, int lifeDamage) {
            Id = id;
            DisplayName = displayName;
            Color = color;
            Size = size;
            MaxHp = maxHp;
            Speed = speed;
            Armor = armor;
            GoldReward = goldReward;
            LifeDamage = lifeDamage;
        }
    }

    // 1. 상수
    private const string DATA_DIR = "Assets/TowerDefense/Data/Enemy";
    private const string PREFAB_DIR = "Assets/TowerDefense/Prefabs/Enemy";
    private const float RADIUS_PER_SIZE = 0.45f;
    private const float HALF = 0.5f;
    private const float HEALTH_BAR_GAP = 0.12f;
    private const float HEALTH_BAR_WIDTH = 0.4f;
    private const float HEALTH_BAR_HEIGHT = 0.06f;
    private const int HEALTH_BAR_SORTING_ORDER = 20;

    private static readonly EnemySpec GRUNT = new("Grunt", "졸개", new Color(0.85f, 0.3f, 0.25f), 0.4f, 40f, 1.6f, 0f, 8, 1);
    private static readonly EnemySpec RUNNER = new("Runner", "날쌘돌이", new Color(0.95f, 0.6f, 0.2f), 0.32f, 25f, 2.6f, 0f, 6, 1);
    private static readonly EnemySpec ARMORED = new("Armored", "중장갑", new Color(0.45f, 0.45f, 0.55f), 0.5f, 120f, 1.0f, 0.5f, 20, 2);

    // 4. 메서드 (public)
    [MenuItem("Tools/TD/Enemy/Build Placeholder Enemies")]
    public static void BuildFromMenu() {
        Build();
    }

    /// <summary>스탯 SO와 프리팹을 만들고 프리팹들을 돌려준다.</summary>
    public static Result Build() {
        Directory.CreateDirectory(DATA_DIR);
        Directory.CreateDirectory(PREFAB_DIR);
        var result = new Result(CreateEnemy(GRUNT), CreateEnemy(RUNNER), CreateEnemy(ARMORED));
        AssetDatabase.SaveAssets();
        return result;
    }

    // 4. 메서드 (private)
    private static EnemyData CreateOrLoadData(EnemySpec spec) {
        string path = $"{DATA_DIR}/{spec.Id}.asset";
        EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
        if (data != null) {
            return data;
        }
        data = ScriptableObject.CreateInstance<EnemyData>();
        var serialized = new SerializedObject(data);
        serialized.FindProperty("displayName").stringValue = spec.DisplayName;
        serialized.FindProperty("maxHp").floatValue = spec.MaxHp;
        serialized.FindProperty("speed").floatValue = spec.Speed;
        serialized.FindProperty("armor").floatValue = spec.Armor;
        serialized.FindProperty("goldReward").intValue = spec.GoldReward;
        serialized.FindProperty("lifeDamage").intValue = spec.LifeDamage;
        serialized.FindProperty("radius").floatValue = spec.Size * RADIUS_PER_SIZE;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    private static Enemy CreateEnemy(EnemySpec spec) {
        EnemyData data = CreateOrLoadData(spec);
        Sprite circle = MapAuthoring.Circle;
        Sprite square = MapAuthoring.Square;

        var enemyObject = new GameObject($"Enemy_{spec.Id}");
        var bodyObject = new GameObject("Sprite");
        bodyObject.transform.SetParent(enemyObject.transform);
        bodyObject.transform.localScale = Vector3.one * spec.Size;
        bodyObject.transform.localPosition = new Vector3(0f, spec.Size * HALF, 0f); // 발바닥이 pivot → 깊이 정렬 기준
        SpriteRenderer body = bodyObject.AddComponent<SpriteRenderer>();
        body.sprite = circle;
        body.color = spec.Color;

        Transform fill = CreateHealthBar(enemyObject.transform, square, spec.Size + HEALTH_BAR_GAP);

        // 타워 사거리 트리거로 감지되도록 (타워 쪽 OnTriggerEnter2D)
        CircleCollider2D hitBox = enemyObject.AddComponent<CircleCollider2D>();
        hitBox.isTrigger = true;
        hitBox.radius = spec.Size * HALF;
        hitBox.offset = new Vector2(0f, spec.Size * HALF);
        Rigidbody2D body2D = enemyObject.AddComponent<Rigidbody2D>();
        body2D.bodyType = RigidbodyType2D.Kinematic;
        body2D.gravityScale = 0f;

        Enemy enemy = enemyObject.AddComponent<Enemy>();
        var serialized = new SerializedObject(enemy);
        serialized.FindProperty("data").objectReferenceValue = data;
        serialized.FindProperty("healthBarFill").objectReferenceValue = fill;
        serialized.FindProperty("body").objectReferenceValue = body;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemyObject, $"{PREFAB_DIR}/Enemy_{spec.Id}.prefab");
        Object.DestroyImmediate(enemyObject);
        return prefab.GetComponent<Enemy>();
    }

    /// <summary>체력바를 만들고 채움(fill) Transform을 돌려준다.</summary>
    private static Transform CreateHealthBar(Transform parent, Sprite sprite, float height) {
        var background = new GameObject("HealthBarBG");
        background.transform.SetParent(parent);
        background.transform.localPosition = new Vector3(0f, height, 0f);
        background.transform.localScale = new Vector3(HEALTH_BAR_WIDTH, HEALTH_BAR_HEIGHT, 1f);
        SpriteRenderer backgroundRenderer = background.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = sprite;
        backgroundRenderer.color = Color.black;
        backgroundRenderer.sortingOrder = HEALTH_BAR_SORTING_ORDER;

        var fill = new GameObject("Fill");
        fill.transform.SetParent(background.transform, false);
        SpriteRenderer fillRenderer = fill.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = sprite;
        fillRenderer.color = Color.green;
        fillRenderer.sortingOrder = HEALTH_BAR_SORTING_ORDER + 1;
        return fill.transform;
    }
}
// AI-assisted code end
