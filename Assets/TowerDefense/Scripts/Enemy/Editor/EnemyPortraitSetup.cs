// AI-assisted code start
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > TD > Enemy > Set Portraits From Sprites
/// 정보 바의 둥근 초상화(EnemyData.portrait)를 적 스프라이트 시트의 첫 프레임(걷기·날기)으로 채운다.
/// 이미 초상화가 들어 있는 적은 건드리지 않는다 (따로 그린 초상화를 넣었으면 그대로).
/// </summary>
public static class EnemyPortraitSetup {
    // 1. 상수
    private const string DATA_DIR = "Assets/TowerDefense/Data/Enemy";
    private const string SPRITE_DIR = "Assets/Sprites/Enemies";
    private const string PORTRAIT_FIELD = "portrait";

    /// <summary>EnemyData 이름 → 첫 프레임을 쓸 시트 (SPRITE_DIR 기준).</summary>
    private static readonly (string data, string sheet)[] PORTRAITS = {
        ("Goblin", "Goblin/Enemy_Goblin_Walk"),
        ("Wolf", "Wolf/Enemy_Wolf_Walk"),
        ("Spider", "Spider/Enemy_Spider_Walk"),
        ("Gargoyle", "Gargoyle/Enemy_Gargoyle_Fly"),
        ("ShadowArcher", "ShadowArcher/Enemy_ShadowArcher_Walk"),
        ("DarkKnight", "DarkKnight/Enemy_DarkKnight_Walk"),
        ("Veznan", "VeznanDemon/Enemy_Veznan_Walk"),
        ("VeznanDemon", "VeznanDemon/Enemy_VeznanDemon_Walk"),
    };

    // 4. 메서드 (public)
    [MenuItem("Tools/TD/Enemy/Set Portraits From Sprites")]
    public static void Run() {
        int filled = 0;
        foreach ((string dataName, string sheet) in PORTRAITS) {
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>($"{DATA_DIR}/{dataName}.asset");
            Sprite sprite = LoadFirstFrame($"{SPRITE_DIR}/{sheet}.png");
            if (data == null || sprite == null) {
                Debug.LogWarning($"[EnemyPortraitSetup] 건너뜀: {dataName} (데이터 또는 스프라이트 없음)");
                continue;
            }
            var serialized = new SerializedObject(data);
            SerializedProperty portrait = serialized.FindProperty(PORTRAIT_FIELD);
            if (portrait.objectReferenceValue != null) {
                continue;
            }
            portrait.objectReferenceValue = sprite;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            filled++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[EnemyPortraitSetup] 초상화 {filled}개 채움");
    }

    // 4. 메서드 (private)
    /// <summary>가로로 잘린 시트의 첫 프레임 (가장 왼쪽 조각).</summary>
    private static Sprite LoadFirstFrame(string path) {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(sprite => sprite.rect.x).FirstOrDefault();
    }
}
// AI-assisted code end
