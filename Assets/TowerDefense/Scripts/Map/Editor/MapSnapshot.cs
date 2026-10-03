// AI-assisted code start
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 맵 저장/불러오기. 맵 프리팹과 그것이 쓰는 그림·경로 SO를 Saved 폴더에 통째로 복사해서,
/// 나중에 Map01Builder로 맵을 다시 만들어도 저장한 맵은 그대로 남는다.
///   저장: Prefabs/Map/Saved/&lt;이름&gt;.prefab, Art/Map/Saved/&lt;이름&gt;/, Data/Map/Saved/&lt;이름&gt;/
///   불러오기: 열린 씬의 맵(TDMap)을 저장한 맵으로 바꾸고, 씬에서 옛 맵(경로 SO 포함)을 가리키던 참조를 새 맵으로 옮긴다.
/// </summary>
public static class MapSnapshot {
    // 1. 상수
    public const string SAVED_PREFAB_DIR = "Assets/TowerDefense/Prefabs/Map/Saved";
    private const string SAVED_ART_DIR = "Assets/TowerDefense/Art/Map/Saved";
    private const string SAVED_DATA_DIR = "Assets/TowerDefense/Data/Map/Saved";
    private const string ART_ROOT = "Assets/TowerDefense/Art/Map/";
    private const string DATA_ROOT = "Assets/TowerDefense/Data/Map/";

    // 4. 메서드 (public)
    /// <summary>맵 프리팹을 이름으로 저장한다. 같은 이름이 있으면 덮어쓴다. 저장된 프리팹을 돌려준다.</summary>
    public static GameObject Save(GameObject mapPrefab, string snapshotName) {
        string sourcePath = AssetDatabase.GetAssetPath(mapPrefab);
        if (string.IsNullOrEmpty(sourcePath) || mapPrefab.GetComponent<TDMap>() == null) {
            Debug.LogError("[MapSnapshot] TDMap이 붙은 맵 프리팹만 저장할 수 있습니다.");
            return null;
        }
        string prefabPath = $"{SAVED_PREFAB_DIR}/{snapshotName}.prefab";
        string artDir = $"{SAVED_ART_DIR}/{snapshotName}";
        string dataDir = $"{SAVED_DATA_DIR}/{snapshotName}";
        DeleteIfExists(prefabPath);
        DeleteIfExists(artDir);
        DeleteIfExists(dataDir);
        Directory.CreateDirectory(SAVED_PREFAB_DIR);
        Directory.CreateDirectory(artDir);
        Directory.CreateDirectory(dataDir);

        // 맵이 쓰는 그림·머티리얼·경로 SO 복사 (원본 → 사본 대응표)
        var remap = new Dictionary<Object, Object>();
        foreach (string dependency in AssetDatabase.GetDependencies(sourcePath, false)) {
            string targetDir = dependency.StartsWith(ART_ROOT) ? artDir : dependency.StartsWith(DATA_ROOT) ? dataDir : null;
            if (targetDir == null || dependency.Contains("/Saved/")) {
                continue;
            }
            string copyPath = $"{targetDir}/{Path.GetFileName(dependency)}";
            AssetDatabase.CopyAsset(dependency, copyPath);
            AddSubAssetPairs(dependency, copyPath, remap);
        }

        AssetDatabase.CopyAsset(sourcePath, prefabPath);
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        foreach (Component component in root.GetComponentsInChildren<Component>(true)) {
            RemapReferences(component, remap);
        }
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();
        Debug.Log($"[MapSnapshot] 저장: {prefabPath}");
        return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
    }

    /// <summary>열린 씬의 맵을 savedPrefab으로 바꾼다. 씬의 다른 오브젝트가 옛 맵을 가리키던 참조도 새 맵으로 옮긴다.</summary>
    public static TDMap Load(GameObject savedPrefab) {
        TDMap oldMap = Object.FindAnyObjectByType<TDMap>();
        var created = (GameObject)PrefabUtility.InstantiatePrefab(savedPrefab);
        Undo.RegisterCreatedObjectUndo(created, "Load Saved Map");
        TDMap newMap = created.GetComponent<TDMap>();
        if (oldMap != null) {
            created.transform.SetParent(oldMap.transform.parent, false);
            created.transform.SetSiblingIndex(oldMap.transform.GetSiblingIndex());
            created.transform.SetPositionAndRotation(oldMap.transform.position, oldMap.transform.rotation);

            var remap = new Dictionary<Object, Object>();
            if (oldMap.RouteData != null && newMap.RouteData != null) {
                remap[oldMap.RouteData] = newMap.RouteData;
            }
            AddHierarchyPairs(oldMap.transform, newMap.transform, remap);
            foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                if (behaviour != null && !behaviour.transform.IsChildOf(oldMap.transform) && !behaviour.transform.IsChildOf(created.transform)) {
                    RemapReferences(behaviour, remap);
                }
            }
            Undo.DestroyObjectImmediate(oldMap.gameObject);
        }
        newMap.Refresh();
        EditorSceneManager.MarkSceneDirty(created.scene);
        Selection.activeGameObject = created;
        return newMap;
    }

    /// <summary>저장된 맵 프리팹 목록 (이름순).</summary>
    public static List<GameObject> FindSaved() {
        var result = new List<GameObject>();
        if (!AssetDatabase.IsValidFolder(SAVED_PREFAB_DIR)) {
            return result;
        }
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { SAVED_PREFAB_DIR })) {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (prefab != null && prefab.GetComponent<TDMap>() != null) {
                result.Add(prefab);
            }
        }
        result.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return result;
    }

    // 4. 메서드 (private)
    private static void DeleteIfExists(string path) {
        if (AssetDatabase.LoadMainAssetAtPath(path) != null || AssetDatabase.IsValidFolder(path)) {
            AssetDatabase.DeleteAsset(path);
        }
    }

    /// <summary>원본 에셋과 사본 에셋의 하위 에셋(텍스처·스프라이트 등)을 이름·타입으로 짝짓는다.</summary>
    private static void AddSubAssetPairs(string sourcePath, string copyPath, Dictionary<Object, Object> remap) {
        Object[] copies = AssetDatabase.LoadAllAssetsAtPath(copyPath);
        foreach (Object source in AssetDatabase.LoadAllAssetsAtPath(sourcePath)) {
            if (source == null) {
                continue;
            }
            foreach (Object copy in copies) {
                if (copy != null && copy.GetType() == source.GetType() && copy.name == source.name) {
                    remap[source] = copy;
                    break;
                }
            }
        }
    }

    /// <summary>옛 맵과 새 맵에서 같은 자리(형제 순서)의 오브젝트·컴포넌트를 짝짓는다.</summary>
    private static void AddHierarchyPairs(Transform oldNode, Transform newNode, Dictionary<Object, Object> remap) {
        remap[oldNode.gameObject] = newNode.gameObject;
        Component[] oldComponents = oldNode.GetComponents<Component>();
        Component[] newComponents = newNode.GetComponents<Component>();
        for (int i = 0; i < Mathf.Min(oldComponents.Length, newComponents.Length); i++) {
            if (oldComponents[i] != null && newComponents[i] != null && oldComponents[i].GetType() == newComponents[i].GetType()) {
                remap[oldComponents[i]] = newComponents[i];
            }
        }
        for (int i = 0; i < Mathf.Min(oldNode.childCount, newNode.childCount); i++) {
            AddHierarchyPairs(oldNode.GetChild(i), newNode.GetChild(i), remap);
        }
    }

    private static void RemapReferences(Object target, Dictionary<Object, Object> remap) {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.GetIterator();
        bool changed = false;
        while (property.Next(true)) {
            if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null
                && remap.TryGetValue(property.objectReferenceValue, out Object replacement)) {
                property.objectReferenceValue = replacement;
                changed = true;
            }
        }
        if (changed) {
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
// AI-assisted code end
